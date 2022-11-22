import torch
import torch.nn as nn
from torchvision import models
import torch.nn.functional as F
from utils.mano import MANO
from config import cfg
from loss import EdgeLengthLoss, NormalVectorLoss


class DepthwiseSeparableConv2d(nn.Module):
    def __init__(self, in_channel, out_channel, kernels_per_layer):
        super().__init__()
        self.depthwise = nn.Conv2d(in_channel, in_channel * kernels_per_layer,
                                   kernel_size=3, padding=1, groups=in_channel)
        self.pointwise = nn.Conv2d(in_channel * kernels_per_layer, out_channel,
                                   kernel_size=1)
    def forward(self, x):
        out = self.depthwise(x)
        out = self.pointwise(out)
        return out

class SoftHeatmap(nn.Module):
    def __init__(self, size, kp_num):
        super(SoftHeatmap, self).__init__()
        self.size = size
        self.beta = nn.Conv2d(kp_num, kp_num, 1, 1, 0, groups=kp_num, bias=False)
        self.wx = torch.arange(0.0, 1.0 * self.size, 1).view([1, self.size]).repeat([self.size, 1])
        self.wy = torch.arange(0.0, 1.0 * self.size, 1).view([self.size, 1]).repeat([1, self.size])
        self.wx = nn.Parameter(self.wx, requires_grad=False)
        self.wy = nn.Parameter(self.wy, requires_grad=False)

    def forward(self, x):
        s = list(x.size())
        scoremap = self.beta(x)
        scoremap = scoremap.view([s[0], s[1], s[2] * s[3]])
        scoremap = F.softmax(scoremap, dim=2)
        scoremap = scoremap.view([s[0], s[1], s[2], s[3]])
        scoremap_x = scoremap.mul(self.wx)
        scoremap_x = scoremap_x.view([s[0], s[1], s[2] * s[3]])
        soft_argmax_x = torch.sum(scoremap_x, dim=2)
        scoremap_y = scoremap.mul(self.wy)
        scoremap_y = scoremap_y.view([s[0], s[1], s[2] * s[3]])
        soft_argmax_y = torch.sum(scoremap_y, dim=2)
        keypoint_uv = torch.stack([soft_argmax_x, soft_argmax_y], dim=2)
        return keypoint_uv, scoremap

class GraphConv(nn.Module):
    def __init__(self, num_joint, in_features, out_features):
        super(GraphConv, self).__init__()
        self.fc = nn.Linear(in_features=in_features, out_features=out_features)
        self.adj = nn.Parameter(torch.eye(num_joint).float().to(cfg.device), requires_grad=True)

    def laplacian(self, A_hat):
        D_hat = torch.sum(A_hat, 1, keepdim=True) + 1e-5
        L = 1 / D_hat * A_hat
        return L

    def forward(self, x):
        batch = x.size(0)
        A_hat = self.laplacian(self.adj)
        A_hat = A_hat.unsqueeze(0).repeat(batch, 1, 1)
        out = self.fc(torch.matmul(A_hat, x))
        return out

class SAIGB(nn.Module):
    def __init__(self, backbone_channels, num_FMs, feature_size, num_vert, template):
        super(SAIGB, self).__init__()
        self.template = torch.Tensor(template).to(cfg.device)  # self.mano.template
        self.backbone_channels = backbone_channels
        self.feature_size = feature_size
        self.num_vert = num_vert
        self.num_FMs = num_FMs
        self.group = nn.Sequential(
            nn.Conv2d(self.backbone_channels, self.num_FMs * self.num_vert, 1),
            nn.LeakyReLU(0.1)
        )

    def forward(self, x):
        feature = self.group(x).view(-1, self.num_vert, self.feature_size * self.num_FMs)
        template = self.template.repeat(x.shape[0], 1, 1)
        init_graph = torch.cat((feature, template), dim=2)
        return init_graph

class GBBMR(nn.Module):
    def __init__(self, in_dim, num_vert, num_joint, heatmap_size):
        super(GBBMR, self).__init__()
        self.in_dim = in_dim
        self.num_vert = num_vert
        self.num_joint = num_joint
        self.num_total = num_vert + num_joint
        self.heatmap_size = heatmap_size
        self.soft_heatmap = SoftHeatmap(self.heatmap_size, self.num_total)
        self.reg_xy = nn.Sequential(
            GraphConv(self.num_vert, self.in_dim, self.heatmap_size ** 2),
            nn.LeakyReLU(0.1),
            nn.Dropout(0.5),
            GraphConv(self.num_vert, self.heatmap_size ** 2, self.heatmap_size ** 2),
        )
        self.reg_z = nn.Sequential(
            GraphConv(self.num_vert, self.in_dim, self.heatmap_size ** 2),
            nn.LeakyReLU(0.1),
            nn.Dropout(0.5),
            GraphConv(self.num_vert, self.heatmap_size ** 2, self.heatmap_size ** 2),
        )
        self.mesh2pose_hm = nn.Linear(self.num_vert, self.num_joint)
        self.mesh2pose_dm = nn.Linear(self.num_vert, self.num_joint)

    def forward(self, x):
        init_graph = x
        heatmap_xy_mesh = self.reg_xy(init_graph).view(-1, self.num_vert, self.heatmap_size, self.heatmap_size)
        heatmap_z_mesh = self.reg_z(init_graph).view(-1, self.num_vert, self.heatmap_size, self.heatmap_size)
        heatmap_xy_joint = self.mesh2pose_hm(heatmap_xy_mesh.transpose(1, 3)).transpose(1, 3)
        heatmap_z_joint = self.mesh2pose_dm(heatmap_z_mesh.transpose(1, 3)).transpose(1, 3)
        heatmap_xy = torch.cat((heatmap_xy_mesh, heatmap_xy_joint), dim=1)
        heatmap_z = torch.cat((heatmap_z_mesh, heatmap_z_joint), dim=1)
        coord_xy, latent_heatmaps = self.soft_heatmap(heatmap_xy)
        depth_maps = latent_heatmaps * heatmap_z
        coord_z = torch.sum(
            depth_maps.view(-1, self.num_total, depth_maps.shape[2] * depth_maps.shape[3]), dim=2, keepdim=True)
        joint_coord = torch.cat((coord_xy, coord_z), 2)
        joint_coord[:, :, :2] = joint_coord[:, :, :2] / (self.heatmap_size // 2) - 1
        return joint_coord, latent_heatmaps, depth_maps

class SAR(nn.Module):
    def __init__(self):
        super(SAR, self).__init__()
        mano = MANO()
        backbone = models.__dict__[cfg.backbone](pretrained=True)
        self.extract_mid = nn.Sequential(backbone.conv1, backbone.bn1, backbone.relu,
                                         backbone.maxpool, backbone.layer1, backbone.layer2)
        self.extract_high = []
        self.saigb = []
        self.gbbmr = []
        self.fuse = []
        for i in range(cfg.num_stage):
            backbone = models.__dict__[cfg.backbone](pretrained=True)
            channel = backbone.fc.in_features
            self.extract_high.append(nn.Sequential(backbone.layer3, backbone.layer4))
            self.saigb.append(SAIGB(channel, cfg.num_FMs, cfg.feature_size, cfg.num_vert, mano.template))
            self.gbbmr.append(GBBMR(cfg.num_FMs*cfg.feature_size+3, cfg.num_vert, cfg.num_joint, cfg.heatmap_size))
            if i > 0:
                self.fuse.append(nn.Conv2d(channel // 4 + cfg.num_joint * 2, channel // 4, 1))
        self.extract_high = nn.ModuleList(self.extract_high)
        self.saigb = nn.ModuleList(self.saigb)
        self.gbbmr = nn.ModuleList(self.gbbmr)
        self.fuse = nn.ModuleList(self.fuse)

        self.coord_loss = nn.L1Loss()
        self.normal_loss = NormalVectorLoss(mano.face)
        self.edge_loss = EdgeLengthLoss(mano.face)

    def forward(self, x, target=None):
        x = x['img'].to(cfg.device)
        outs = {'coords': []}
        lhms = []
        dms = []
        feat_mid = self.extract_mid(x)
        for i in range(cfg.num_stage):
            if i > 0:
                feat = self.fuse[i-1](
                    torch.cat((feat_mid,
                               lhms[i-1][:, cfg.num_vert:],
                               dms[i-1][:, cfg.num_vert:]), dim=1))
            else:
                feat = feat_mid
            feat_high = self.extract_high[i](feat)
            init_graph = self.saigb[i](feat_high)
            coord, lhm, dm = self.gbbmr[i](init_graph)
            outs['coords'].append(coord)
            lhms.append(lhm)
            dms.append(dm)
        if self.training:
            loss = {}
            mesh_pose_uvd = target['mesh_pose_uvd'].to(cfg.device)
            for i in range(cfg.num_stage):
                loss['coord_{}'.format(i)] = self.coord_loss(outs['coords'][i], mesh_pose_uvd)
                loss['normal_{}'.format(i)] = \
                    self.normal_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean() * 0.1
                loss['edge_{}'.format(i)] = \
                    self.edge_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean()
            return loss
        else:
            outs['coords'] = outs['coords'][-1]
            return outs


class SAR_frontWeight(nn.Module):
    def __init__(self):
        super(SAR_frontWeight, self).__init__()
        mano = MANO()
        backbone = models.__dict__[cfg.backbone](pretrained=True)
        self.extract_mid = nn.Sequential(backbone.conv1, backbone.bn1, backbone.relu,
                                         backbone.maxpool, backbone.layer1, backbone.layer2)

        self.extract_prev = nn.Sequential(nn.Conv2d(1, 16, kernel_size=7, stride=2, padding=3, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),
                                           nn.Conv2d(16, 32, kernel_size=5, stride=1, padding=2, bias=False),
                                           nn.BatchNorm2d(32), nn.LeakyReLU(0.1))
        # self.extract_prev : (batch, 1, 64, 64) to (batch, 32, 32, 32)

        self.fuse_latent = nn.Sequential(nn.Conv2d(backbone.fc.in_features // 4 + 32, 32, 1),       # backbone.fc.in_features : 512
                                         nn.BatchNorm2d(32), nn.LeakyReLU(0.1))
        self.extract_weight = nn.Sequential(nn.Conv2d(32, 16, kernel_size=5, stride=2, padding=2, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),
                                           nn.MaxPool2d(4),
                                           nn.Conv2d(16, 1, kernel_size=5, stride=2, padding=2, bias=False),
                                           nn.BatchNorm2d(1), nn.LeakyReLU(0.1),
                                           nn.MaxPool2d(2))


        self.extract_high = []
        self.saigb = []
        self.gbbmr = []
        self.fuse = []

        for i in range(cfg.num_stage):
            backbone = models.__dict__[cfg.backbone](pretrained=True)
            channel = backbone.fc.in_features
            self.extract_high.append(nn.Sequential(backbone.layer3, backbone.layer4))

            self.saigb.append(SAIGB(channel, cfg.num_FMs, cfg.feature_size, cfg.num_vert, mano.template))
            self.gbbmr.append(GBBMR(cfg.num_FMs * cfg.feature_size + 3, cfg.num_vert, cfg.num_joint, cfg.heatmap_size))

        # fuse net for refinement
        self.fuse.append(nn.Conv2d(channel // 4 + 32, channel // 4, 1))
        self.fuse.append(nn.Conv2d(channel // 4 + cfg.num_joint * 2, channel // 4, 1))

        self.extract_high = nn.ModuleList(self.extract_high)
        self.saigb = nn.ModuleList(self.saigb)
        self.gbbmr = nn.ModuleList(self.gbbmr)
        self.fuse = nn.ModuleList(self.fuse)

        self.coord_loss = nn.L1Loss()
        self.normal_loss = NormalVectorLoss(mano.face)
        self.edge_loss = EdgeLengthLoss(mano.face)
        self.weight_loss = nn.L1Loss()



    def forward(self, input, target=None, dataset=None):
        prev_heatmap = input['extra'].to(cfg.device)  # check extra : (batch, 1, 64, 64) ~ latentheatmap + depthmap
        x = input['img'].to(cfg.device)     # x : (batch, 3, 256, 256)

        outs = {'coords': []}
        lhms = []
        dms = []

        feat_mid = self.extract_mid(x)  # feat_mid : (batch, 128, 32, 32)

        feat_prev = self.extract_prev(prev_heatmap)  # feat_prev : (batch, 32, 32, 32)
        feat_latent = self.fuse_latent(torch.cat((feat_mid, feat_prev), dim=1))    # feat_latent : (batch, 32, 32, 32)
        feat_weight = self.extract_weight(feat_latent)

        feat_latent = feat_latent * feat_weight.expand_as(feat_latent)

        feat_mid = self.fuse[0](torch.cat((feat_mid, feat_latent), dim=1))
        for i in range(cfg.num_stage):
            if i > 0:
                feat = self.fuse[i](
                    torch.cat((feat_mid,
                               lhms[i - 1][:, cfg.num_vert:],
                               dms[i - 1][:, cfg.num_vert:]), dim=1))   # 128 + 42 + 32
            else:
                feat = feat_mid

            feat_high = self.extract_high[i](feat)  # (batch, 128, 32, 32) >> (batch, 512, 8, 8)
            init_graph = self.saigb[i](feat_high)  # (batch, 778, 515)
            coord, lhm, dm = self.gbbmr[i](init_graph)

            outs['coords'].append(coord)  # coord : (batch, 799, 3)      # 799 = num_vert(778) + num_joint(21)
            lhms.append(lhm)  # lhm : (batch, 799, 32, 32)
            dms.append(dm)  # dms : (batch, 799, 32, 32)

        if self.training:
            loss = {}
            mesh_pose_uvd = target['mesh_pose_uvd'].to(cfg.device)
            sim_scale = target['weight_aug'].to(cfg.device)  # w = 0 if same, noise scale 1~2.5, ...
            weight_sim = 1.0 / (sim_scale * 2.0 + 1.0)  # 1 if same, smaller if different

            for i in range(cfg.num_stage):
                loss['coord_{}'.format(i)] = self.coord_loss(outs['coords'][i], mesh_pose_uvd)
                loss['normal_{}'.format(i)] = \
                    self.normal_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean() * 0.1
                loss['edge_{}'.format(i)] = \
                    self.edge_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean()
            loss['weight'] = self.weight_loss(feat_weight, weight_sim)
            return loss #, outs['coords'][-1]
        else:
            debug = None
            outs['coords'] = outs['coords'][-1]

            return outs #, debug

class SAR_refineWeight(nn.Module):
    def __init__(self):
        super(SAR_refineWeight, self).__init__()
        mano = MANO()
        backbone = models.__dict__[cfg.backbone](pretrained=True)

        self.depthwiseConv2d_0 = DepthwiseSeparableConv2d(64, 16, 4)
        self.depthwiseConv2d_1 = DepthwiseSeparableConv2d(16, 4, 2)
        self.depthwiseConv2d_2 = DepthwiseSeparableConv2d(4, 1, 2)

        self.extract_mid = nn.Sequential(backbone.conv1, backbone.bn1, backbone.relu,
                                         backbone.maxpool, backbone.layer1, backbone.layer2)

        self.extract_prev_heatmap = nn.Sequential(nn.Conv2d(1, 16, kernel_size=7, stride=2, padding=3, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),
                                           nn.Conv2d(16, 21, kernel_size=5, stride=1, padding=2, bias=False),
                                           nn.BatchNorm2d(21), nn.LeakyReLU(0.1))

        # self.extract_prev : (batch, 21, 32, 32) to (batch, 32, 32, 32)
        #self.extract_prev_featuremap = nn.Sequential(nn.Conv2d(21, 32, 1), nn.BatchNorm2d(32), nn.LeakyReLU(0.1))

        self.fuse_latent = nn.Sequential(nn.Conv2d(backbone.fc.in_features // 4 + 21, 64, 1),       # backbone.fc.in_features : 512
                                         nn.BatchNorm2d(64), nn.LeakyReLU(0.1))

        self.extract_weight = nn.Sequential(self.depthwiseConv2d_0,
                                            nn.Conv2d(16, 16, kernel_size=5, stride=4, padding=2, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),   #  (16, 8, 8)
                                            self.depthwiseConv2d_1,
                                           nn.Conv2d(4, 4, kernel_size=5, stride=4, padding=2, bias=False),
                                           nn.BatchNorm2d(4), nn.LeakyReLU(0.1),    # (4, 2, 2)
                                            self.depthwiseConv2d_2,
                                            nn.Conv2d(1, 1, kernel_size=3, stride=2, padding=1, bias=False),
                                            nn.BatchNorm2d(1), nn.LeakyReLU(0.1))     # (1, 2, 2) > (1, 1, 1)


        self.extract_high = []
        self.saigb = []
        self.gbbmr = []
        self.fuse = []

        for i in range(cfg.num_stage):
            backbone = models.__dict__[cfg.backbone](pretrained=True)
            channel = backbone.fc.in_features
            self.extract_high.append(nn.Sequential(backbone.layer3, backbone.layer4))

            self.saigb.append(SAIGB(channel, cfg.num_FMs, cfg.feature_size, cfg.num_vert, mano.template))
            self.gbbmr.append(GBBMR(cfg.num_FMs * cfg.feature_size + 3, cfg.num_vert, cfg.num_joint, cfg.heatmap_size))

        # fuse net for refinement
        self.fuse.append(nn.Conv2d(channel // 4 + cfg.num_joint * 2 + 21, channel // 4, 1))

        self.extract_high = nn.ModuleList(self.extract_high)
        self.saigb = nn.ModuleList(self.saigb)
        self.gbbmr = nn.ModuleList(self.gbbmr)
        self.fuse = nn.ModuleList(self.fuse)

        self.coord_loss = nn.L1Loss()
        self.normal_loss = NormalVectorLoss(mano.face)
        self.edge_loss = EdgeLengthLoss(mano.face)
        self.weight_loss = nn.MSELoss()
        self.heatmap_loss = nn.MSELoss()


    def forward(self, input, target=None, dataset=None):
        prev_source = input['extra'].to(cfg.device)  # check extra : (batch, 1, 64, 64) ~ latentheatmap + depthmap
        x = input['img'].to(cfg.device)     # x : (batch, 3, 256, 256)

        outs = {'coords': []}
        lhms = []
        dms = []

        feat_mid = self.extract_mid(x)  # feat_mid : (batch, 128, 32, 32)

        prev_heatmap = self.extract_prev_heatmap(prev_source)  # prev_heatmap : (batch, 21, 32, 32)

        feat_latent = self.fuse_latent(torch.cat((feat_mid, prev_heatmap), dim=1))    # feat_latent : (batch, 64, 32, 32)
        feat_weight = self.extract_weight(feat_latent)

        prev_heatmap = torch.mul(prev_heatmap, feat_weight.expand_as(prev_heatmap))
        # prev_heatmap = prev_heatmap * feat_weight.expand_as(prev_heatmap)

        for i in range(cfg.num_stage):
            if i > 0:
                feat = self.fuse[i - 1](
                    torch.cat((feat_mid,
                               lhms[i - 1][:, cfg.num_vert:],
                               dms[i - 1][:, cfg.num_vert:],
                               prev_heatmap), dim=1))   # 128 + 42 + 21
            else:
                feat = feat_mid

            feat_high = self.extract_high[i](feat)  # (batch, 128, 32, 32) >> (batch, 512, 8, 8)
            init_graph = self.saigb[i](feat_high)  # (batch, 778, 515)
            coord, lhm, dm = self.gbbmr[i](init_graph)

            outs['coords'].append(coord)  # coord : (batch, 799, 3)      # 799 = num_vert(778) + num_joint(21)
            lhms.append(lhm)  # lhm : (batch, 799, 32, 32)
            dms.append(dm)  # dms : (batch, 799, 32, 32)

            joint_lhm = lhm[:, cfg.num_vert:, :, :].clone().detach()

        if self.training:
            loss = {}
            mesh_pose_uvd = target['mesh_pose_uvd'].to(cfg.device)
            weight_sim = target['weight_aug'].to(cfg.device)  # w = 1 if optimal feature(same as currGT), w < 1 as noise scale, ...

            for i in range(cfg.num_stage):
                loss['coord_{}'.format(i)] = self.coord_loss(outs['coords'][i], mesh_pose_uvd)
                loss['normal_{}'.format(i)] = \
                    self.normal_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean() * 0.1
                loss['edge_{}'.format(i)] = \
                    self.edge_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean()
            loss['weight'] = self.weight_loss(feat_weight, weight_sim)

            flag_exist = False
            for i in range(cfg.batch_size):
                if torch.eq(weight_sim[i], 1.0):
                    if not flag_exist:
                        flag_exist = True
                        loss['heatmap'] = self.heatmap_loss(prev_heatmap[i], joint_lhm[i])
                    else:
                        loss['heatmap'] += self.heatmap_loss(prev_heatmap[i], joint_lhm[i])

            if flag_exist:
                loss['heatmap'] *= 0.2

            return loss     #, outs['coords'][-1]

        else:
            outs['coords'] = outs['coords'][-1]
            return outs

class SAR_crossWeight(nn.Module):
    def __init__(self):
        super(SAR_crossWeight, self).__init__()
        mano = MANO()
        backbone = models.__dict__[cfg.backbone](pretrained=True)

        self.depthwiseConv2d_0 = DepthwiseSeparableConv2d(64, 16, 4)
        self.depthwiseConv2d_1 = DepthwiseSeparableConv2d(16, 4, 2)
        self.depthwiseConv2d_2 = DepthwiseSeparableConv2d(4, 1, 2)

        self.extract_mid = nn.Sequential(backbone.conv1, backbone.bn1, backbone.relu,
                                         backbone.maxpool, backbone.layer1, backbone.layer2)

        self.extract_prev_heatmap = nn.Sequential(nn.Conv2d(1, 16, kernel_size=7, stride=2, padding=3, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),
                                           nn.Conv2d(16, 21, kernel_size=5, stride=1, padding=2, bias=False),
                                           nn.BatchNorm2d(21), nn.LeakyReLU(0.1))

        # self.extract_prev : (batch, 21, 32, 32) to (batch, 32, 32, 32)
        #self.extract_prev_featuremap = nn.Sequential(nn.Conv2d(21, 32, 1), nn.BatchNorm2d(32), nn.LeakyReLU(0.1))

        self.fuse_latent = nn.Sequential(nn.Conv2d(backbone.fc.in_features // 4 + 21, 64, 1),       # backbone.fc.in_features : 512
                                         nn.BatchNorm2d(64), nn.LeakyReLU(0.1))

        self.extract_weight = nn.Sequential(self.depthwiseConv2d_0,
                                            nn.Conv2d(16, 16, kernel_size=5, stride=4, padding=2, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),   #  (16, 8, 8)
                                            self.depthwiseConv2d_1,
                                           nn.Conv2d(4, 4, kernel_size=5, stride=4, padding=2, bias=False),
                                           nn.BatchNorm2d(4), nn.LeakyReLU(0.1),    # (4, 2, 2)
                                            self.depthwiseConv2d_2,
                                            nn.Conv2d(1, 1, kernel_size=3, stride=2, padding=1, bias=False),
                                            nn.BatchNorm2d(1), nn.LeakyReLU(0.1))     # (1, 2, 2) > (1, 1, 1)


        self.extract_high = []
        self.saigb = []
        self.gbbmr = []
        self.fuse = []

        for i in range(cfg.num_stage):
            backbone = models.__dict__[cfg.backbone](pretrained=True)
            channel = backbone.fc.in_features
            self.extract_high.append(nn.Sequential(backbone.layer3, backbone.layer4))

            self.saigb.append(SAIGB(channel, cfg.num_FMs, cfg.feature_size, cfg.num_vert, mano.template))
            self.gbbmr.append(GBBMR(cfg.num_FMs * cfg.feature_size + 3, cfg.num_vert, cfg.num_joint, cfg.heatmap_size))

        # fuse net for refinement
        self.fuse.append(nn.Conv2d(channel // 4 + 21, channel // 4, 1))
        self.fuse.append(nn.Conv2d(channel // 4 + cfg.num_joint * 2 + 21, channel // 4, 1))

        self.extract_high = nn.ModuleList(self.extract_high)
        self.saigb = nn.ModuleList(self.saigb)
        self.gbbmr = nn.ModuleList(self.gbbmr)
        self.fuse = nn.ModuleList(self.fuse)

        self.coord_loss = nn.L1Loss()
        self.normal_loss = NormalVectorLoss(mano.face)
        self.edge_loss = EdgeLengthLoss(mano.face)
        self.weight_loss = nn.MSELoss()
        self.heatmap_loss = nn.MSELoss()


    def forward(self, input, target=None, dataset=None):
        prev_source = input['extra'].to(cfg.device)  # check extra : (batch, 1, 64, 64) ~ latentheatmap + depthmap
        x = input['img'].to(cfg.device)     # x : (batch, 3, 256, 256)

        outs = {'coords': []}
        lhms = []
        dms = []

        feat_mid = self.extract_mid(x)  # feat_mid : (batch, 128, 32, 32)

        prev_heatmap = self.extract_prev_heatmap(prev_source)  # prev_heatmap : (batch, 21, 32, 32)
        feat_latent = self.fuse_latent(torch.cat((feat_mid, prev_heatmap), dim=1))    # feat_latent : (batch, 64, 32, 32)
        feat_weight = self.extract_weight(feat_latent)

        for i in range(cfg.num_stage):
            if i == 0:
                feat = self.fuse[i](
                    torch.cat((feat_mid,
                               prev_heatmap), dim=1))   # 128 + 21
            else:
                prev_heatmap_weighted = torch.mul(prev_heatmap, feat_weight.expand_as(prev_heatmap))

                feat = self.fuse[i](
                    torch.cat((feat_mid,
                               lhms[i - 1][:, cfg.num_vert:],
                               dms[i - 1][:, cfg.num_vert:],
                               prev_heatmap_weighted), dim=1))  # 128 + 42 + 21

            feat_high = self.extract_high[i](feat)  # (batch, 128, 32, 32) >> (batch, 512, 8, 8)
            init_graph = self.saigb[i](feat_high)  # (batch, 778, 515)
            coord, lhm, dm = self.gbbmr[i](init_graph)

            outs['coords'].append(coord)  # coord : (batch, 799, 3)      # 799 = num_vert(778) + num_joint(21)
            lhms.append(lhm)  # lhm : (batch, 799, 32, 32)
            dms.append(dm)  # dms : (batch, 799, 32, 32)

            joint_lhm = lhm[:, cfg.num_vert:, :, :].clone().detach()

        if self.training:
            loss = {}
            mesh_pose_uvd = target['mesh_pose_uvd'].to(cfg.device)
            weight_sim = target['weight_aug'].to(cfg.device)  # w = 1 if optimal feature(same as currGT), w < 1 as noise scale, ...

            for i in range(cfg.num_stage):
                loss['coord_{}'.format(i)] = self.coord_loss(outs['coords'][i], mesh_pose_uvd)
                loss['normal_{}'.format(i)] = \
                    self.normal_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean() * 0.1
                loss['edge_{}'.format(i)] = \
                    self.edge_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean()
            loss['weight'] = self.weight_loss(torch.squeeze(feat_weight), torch.squeeze(weight_sim))

            flag_exist = False
            for i in range(cfg.batch_size):
                if torch.eq(weight_sim[i], 1.0):
                    if not flag_exist:
                        flag_exist = True
                        loss['heatmap'] = self.heatmap_loss(prev_heatmap[i], joint_lhm[i])
                    else:
                        loss['heatmap'] += self.heatmap_loss(prev_heatmap[i], joint_lhm[i])

            if flag_exist:
                loss['heatmap'] *= 0.2

            return loss     #, outs['coords'][-1]

        else:
            outs['coords'] = outs['coords'][-1]
            return outs

class SAR_crossWeight_wVis(nn.Module):
    def __init__(self):
        super(SAR_crossWeight_wVis, self).__init__()
        mano = MANO()
        backbone = models.__dict__[cfg.backbone](pretrained=True)

        self.depthwiseConv2d_0 = DepthwiseSeparableConv2d(64, 16, 4)
        self.depthwiseConv2d_1 = DepthwiseSeparableConv2d(16, 4, 2)
        self.depthwiseConv2d_2 = DepthwiseSeparableConv2d(4, 1, 2)

        self.depthwiseConv2d_3 = DepthwiseSeparableConv2d(32, 8, 4)

        self.extract_mid = nn.Sequential(backbone.conv1, backbone.bn1, backbone.relu,
                                         backbone.maxpool, backbone.layer1, backbone.layer2)

        self.extract_prev_heatmap = nn.Sequential(nn.Conv2d(1, 16, kernel_size=7, stride=2, padding=3, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),
                                           nn.Conv2d(16, 21, kernel_size=5, stride=1, padding=2, bias=False),
                                           nn.BatchNorm2d(21), nn.LeakyReLU(0.1))

        # self.extract_prev : (batch, 21, 32, 32) to (batch, 32, 32, 32)
        #self.extract_prev_featuremap = nn.Sequential(nn.Conv2d(21, 32, 1), nn.BatchNorm2d(32), nn.LeakyReLU(0.1))

        self.fuse_latent = nn.Sequential(nn.Conv2d(backbone.fc.in_features // 4 + 21, 64, 1),       # backbone.fc.in_features : 512
                                         nn.BatchNorm2d(64), nn.LeakyReLU(0.1))

        self.extract_weight = nn.Sequential(self.depthwiseConv2d_0,
                                            nn.Conv2d(16, 16, kernel_size=5, stride=4, padding=2, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),   #  (16, 8, 8)
                                            self.depthwiseConv2d_1,
                                           nn.Conv2d(4, 4, kernel_size=5, stride=4, padding=2, bias=False),
                                           nn.BatchNorm2d(4), nn.LeakyReLU(0.1),    # (4, 2, 2)
                                            self.depthwiseConv2d_2,
                                            nn.Conv2d(1, 1, kernel_size=3, stride=2, padding=1, bias=False),
                                            nn.BatchNorm2d(1), nn.LeakyReLU(0.1))     # (1, 2, 2) > (1, 1, 1)

        self.extract_visib = nn.Sequential(nn.Conv2d(backbone.fc.in_features // 4, 32, 1),
                                           nn.BatchNorm2d(32), nn.LeakyReLU(0.1),
                                           self.depthwiseConv2d_3,
                                           nn.Conv2d(8, 4, kernel_size=5, stride=4, padding=2, bias=False),    #(4, 4, 4)
                                           nn.Conv2d(4, 1, kernel_size=5, stride=4, padding=2, bias=False),
                                           nn.BatchNorm2d(1), nn.LeakyReLU(0.1))        # (batch, 256, 16, 16) >> (batch, 1)

        self.extract_high_0 = []
        self.extract_high_1 = []
        self.saigb = []
        self.gbbmr = []
        self.fuse = []

        for i in range(cfg.num_stage):
            backbone = models.__dict__[cfg.backbone](pretrained=True)
            channel = backbone.fc.in_features
            self.extract_high_0.append(nn.Sequential(backbone.layer3))
            self.extract_high_1.append(nn.Sequential(backbone.layer4))

            self.saigb.append(SAIGB(channel, cfg.num_FMs, cfg.feature_size, cfg.num_vert, mano.template))
            self.gbbmr.append(GBBMR(cfg.num_FMs * cfg.feature_size + 3, cfg.num_vert, cfg.num_joint, cfg.heatmap_size))

        # fuse net for refinement
        self.fuse.append(nn.Conv2d(channel // 4 + 21, channel // 4, 1))
        self.fuse.append(nn.Conv2d(channel // 4 + cfg.num_joint * 2 + 21, channel // 4, 1))

        self.extract_high_0 = nn.ModuleList(self.extract_high_0)
        self.extract_high_1 = nn.ModuleList(self.extract_high_1)
        self.saigb = nn.ModuleList(self.saigb)
        self.gbbmr = nn.ModuleList(self.gbbmr)
        self.fuse = nn.ModuleList(self.fuse)

        self.coord_loss = nn.L1Loss()
        self.normal_loss = NormalVectorLoss(mano.face)
        self.edge_loss = EdgeLengthLoss(mano.face)
        self.weight_loss = nn.MSELoss()
        self.heatmap_loss = nn.MSELoss()


    def forward(self, input, target=None, dataset=None):
        prev_source = input['extra'].to(cfg.device)  # check extra : (batch, 1, 64, 64) ~ latentheatmap + depthmap
        x = input['img'].to(cfg.device)     # x : (batch, 3, 256, 256)

        if cfg.flag_vis:
            visib = input['visible'].to(cfg.device)

        outs = {'coords': []}
        lhms = []
        dms = []

        feat_mid = self.extract_mid(x)  # feat_mid : (batch, 128, 32, 32)

        prev_heatmap = self.extract_prev_heatmap(prev_source)  # prev_heatmap : (batch, 21, 32, 32)

        feat_latent = self.fuse_latent(torch.cat((feat_mid, prev_heatmap), dim=1))    # feat_latent : (batch, 64, 32, 32)
        feat_weight = self.extract_weight(feat_latent)

        for i in range(cfg.num_stage):
            if i == 0:
                feat = self.fuse[i](
                    torch.cat((feat_mid,
                               prev_heatmap), dim=1))   # 128 + 21
                feat_high_0 = self.extract_high_0[i](feat)      # (batch, 128, 32, 32) >> (batch, 256, 16, 16)

                if cfg.flag_vis:
                    feat_visib = self.extract_visib(feat_high_0)     # 1 if all visible, close to 0 if occluded
                    feat_weight = torch.mul(feat_weight, (1. / feat_visib))

            else:
                prev_heatmap_weighted = torch.mul(prev_heatmap, feat_weight.expand_as(prev_heatmap))

                feat = self.fuse[i](
                    torch.cat((feat_mid,
                               lhms[i - 1][:, cfg.num_vert:],
                               dms[i - 1][:, cfg.num_vert:],
                               prev_heatmap_weighted), dim=1))  # 128 + 42 + 21
                feat_high_0 = self.extract_high_0[i](feat)  # (batch, 128, 32, 32) >> (batch, 256, 16, 16)

            feat_high = self.extract_high_1[i](feat_high_0) # (batch, 512, 8, 8)
            init_graph = self.saigb[i](feat_high)  # (batch, 778, 515)
            coord, lhm, dm = self.gbbmr[i](init_graph)

            outs['coords'].append(coord)  # coord : (batch, 799, 3)      # 799 = num_vert(778) + num_joint(21)
            lhms.append(lhm)  # lhm : (batch, 799, 32, 32)
            dms.append(dm)  # dms : (batch, 799, 32, 32)

            joint_lhm = lhm[:, cfg.num_vert:, :, :].clone().detach()

        if self.training:
            loss = {}
            mesh_pose_uvd = target['mesh_pose_uvd'].to(cfg.device)
            weight_sim = target['weight_aug'].to(cfg.device)  # w = 1 if optimal feature(same as currGT), w < 1 as noise scale, ...

            for i in range(cfg.num_stage):
                loss['coord_{}'.format(i)] = self.coord_loss(outs['coords'][i], mesh_pose_uvd)
                loss['normal_{}'.format(i)] = \
                    self.normal_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean() * 0.1
                loss['edge_{}'.format(i)] = \
                    self.edge_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean()

            loss['weight'] = self.weight_loss(feat_weight, weight_sim)
            if cfg.flag_vis:
                loss['visib'] = self.weight_loss(feat_visib, visib)

            flag_exist = False
            for i in range(cfg.batch_size):
                if torch.eq(weight_sim[i], 1.0):
                    if not flag_exist:
                        flag_exist = True
                        loss['heatmap'] = self.heatmap_loss(prev_heatmap[i], joint_lhm[i])
                    else:
                        loss['heatmap'] += self.heatmap_loss(prev_heatmap[i], joint_lhm[i])
            if flag_exist:
                loss['heatmap'] *= 0.2

            return loss #, outs['coords'][-1]

        else:
            outs['coords'] = outs['coords'][-1]
            return outs


class SAR_crossWeight_wVis_light(nn.Module):
    def __init__(self):
        super(SAR_crossWeight_wVis_light, self).__init__()
        mano = MANO()
        backbone = models.__dict__[cfg.backbone](pretrained=True)

        self.depthwiseConv2d_0 = DepthwiseSeparableConv2d(64, 16, 4)
        self.depthwiseConv2d_1 = DepthwiseSeparableConv2d(16, 4, 2)
        self.depthwiseConv2d_2 = DepthwiseSeparableConv2d(4, 1, 2)

        self.depthwiseConv2d_3 = DepthwiseSeparableConv2d(32, 4, 2)

        self.extract_mid = nn.Sequential(backbone.conv1, backbone.bn1, backbone.relu,
                                         backbone.maxpool, backbone.layer1, backbone.layer2)

        self.extract_prev_heatmap = nn.Sequential(nn.Conv2d(1, 16, kernel_size=7, stride=2, padding=3, bias=False),
                                           nn.BatchNorm2d(16), nn.LeakyReLU(0.1),
                                           nn.Conv2d(16, 21, kernel_size=5, stride=1, padding=2, bias=False),
                                           nn.BatchNorm2d(21), nn.LeakyReLU(0.1))

        # self.extract_prev : (batch, 21, 32, 32) to (batch, 32, 32, 32)
        #self.extract_prev_featuremap = nn.Sequential(nn.Conv2d(21, 32, 1), nn.BatchNorm2d(32), nn.LeakyReLU(0.1))

        self.fuse_latent = nn.Sequential(nn.Conv2d(backbone.fc.in_features // 4 + 21, 64, 1),       # backbone.fc.in_features : 512
                                         nn.BatchNorm2d(64), nn.LeakyReLU(0.1))

        self.extract_weight = nn.Sequential(self.depthwiseConv2d_0, # (16, 32, 32)
                                            nn.MaxPool2d(4), nn.BatchNorm2d(16), nn.LeakyReLU(0.1),   #  (16, 8, 8)
                                            self.depthwiseConv2d_1, nn.MaxPool2d(2),# (4, 4, 4)
                                            self.depthwiseConv2d_2, nn.AvgPool2d(4),
                                            nn.BatchNorm2d(1), nn.LeakyReLU(0.1))     # (1, 2, 2) > (1, 1, 1)

        self.extract_visib = nn.Sequential(nn.Conv2d(backbone.fc.in_features // 2, 32, 1),
                                           nn.BatchNorm2d(32), nn.LeakyReLU(0.1),
                                           self.depthwiseConv2d_3,  # (4, 16, 16)
                                           nn.MaxPool2d(4),    #(4, 4, 4)
                                           nn.Conv2d(4, 1, kernel_size=3, stride=2, padding=1, bias=False), nn.AvgPool2d(2),
                                           nn.BatchNorm2d(1), nn.LeakyReLU(0.1))        # (batch, 256, 16, 16) >> (batch, 1)

        self.extract_high_0 = []
        self.extract_high_1 = []
        self.saigb = []
        self.gbbmr = []
        self.fuse = []

        for i in range(cfg.num_stage):
            backbone = models.__dict__[cfg.backbone](pretrained=True)
            channel = backbone.fc.in_features
            self.extract_high_0.append(nn.Sequential(backbone.layer3))
            self.extract_high_1.append(nn.Sequential(backbone.layer4))

            self.saigb.append(SAIGB(channel, cfg.num_FMs, cfg.feature_size, cfg.num_vert, mano.template))
            self.gbbmr.append(GBBMR(cfg.num_FMs * cfg.feature_size + 3, cfg.num_vert, cfg.num_joint, cfg.heatmap_size))

        # fuse net for refinement
        self.fuse.append(nn.Conv2d(channel // 4 + 21, channel // 4, 1))
        self.fuse.append(nn.Conv2d(channel // 4 + cfg.num_joint * 2 + 21, channel // 4, 1))

        self.extract_high_0 = nn.ModuleList(self.extract_high_0)
        self.extract_high_1 = nn.ModuleList(self.extract_high_1)
        self.saigb = nn.ModuleList(self.saigb)
        self.gbbmr = nn.ModuleList(self.gbbmr)
        self.fuse = nn.ModuleList(self.fuse)

        self.coord_loss = nn.L1Loss()
        self.normal_loss = NormalVectorLoss(mano.face)
        self.edge_loss = EdgeLengthLoss(mano.face)
        self.weight_loss = nn.MSELoss()
        self.heatmap_loss = nn.MSELoss()

        self.pi = torch.acos(torch.zeros(1)).item() * 2


    def forward(self, input, target=None, dataset=None):
        prev_source = input['extra'].to(cfg.device)  # check extra : (batch, 1, 64, 64) ~ latentheatmap + depthmap
        x = input['img'].to(cfg.device)     # x : (batch, 3, 256, 256)

        outs = {'coords': []}
        lhms = []
        dms = []

        feat_mid = self.extract_mid(x)  # feat_mid : (batch, 128, 32, 32)

        prev_heatmap = self.extract_prev_heatmap(prev_source)  # prev_heatmap : (batch, 21, 32, 32)

        feat_latent = self.fuse_latent(torch.cat((feat_mid, prev_heatmap), dim=1))    # feat_latent : (batch, 64, 32, 32)
        feat_weight = self.extract_weight(feat_latent)

        for i in range(cfg.num_stage):
            if i == 0:
                feat = self.fuse[i](
                    torch.cat((feat_mid,
                               prev_heatmap), dim=1))   # 128 + 21
                feat_high_0 = self.extract_high_0[i](feat)      # (batch, 128, 32, 32) >> (batch, 256, 16, 16)

                if cfg.flag_vis:
                    feat_visib = self.extract_visib(feat_high_0)     # 1 if all visible, close to 0 if occluded
                    # feat_weight = torch.mul(feat_weight, (1. / (1. + torch.abs(feat_visib) * 5.)))

                    feat_visib_norm = 2. * torch.cos(self.pi * torch.abs(feat_visib) / 2.)
                    feat_weight = torch.mul(feat_weight, feat_visib_norm)

            else:
                prev_heatmap_weighted = torch.mul(prev_heatmap, feat_weight.expand_as(prev_heatmap))

                feat = self.fuse[i](
                    torch.cat((feat_mid,
                               lhms[i - 1][:, cfg.num_vert:],
                               dms[i - 1][:, cfg.num_vert:],
                               prev_heatmap_weighted), dim=1))  # 128 + 42 + 21
                feat_high_0 = self.extract_high_0[i](feat)  # (batch, 128, 32, 32) >> (batch, 256, 16, 16)

            feat_high = self.extract_high_1[i](feat_high_0) # (batch, 512, 8, 8)
            init_graph = self.saigb[i](feat_high)  # (batch, 778, 515)
            coord, lhm, dm = self.gbbmr[i](init_graph)

            outs['coords'].append(coord)  # coord : (batch, 799, 3)      # 799 = num_vert(778) + num_joint(21)
            lhms.append(lhm)  # lhm : (batch, 799, 32, 32)
            dms.append(dm)  # dms : (batch, 799, 32, 32)

            joint_lhm = lhm[:, cfg.num_vert:, :, :].clone().detach()

        if self.training:
            loss = {}
            mesh_pose_uvd = target['mesh_pose_uvd'].to(cfg.device)
            weight_sim = target['weight_aug'].to(cfg.device)  # w = 1 if optimal feature(same as currGT), w < 1 as noise scale, ...

            for i in range(cfg.num_stage):
                loss['coord_{}'.format(i)] = self.coord_loss(outs['coords'][i], mesh_pose_uvd)
                loss['normal_{}'.format(i)] = \
                    self.normal_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean() * 0.1
                loss['edge_{}'.format(i)] = \
                    self.edge_loss(outs['coords'][i][:, :cfg.num_vert], mesh_pose_uvd[:, :cfg.num_vert]).mean()

            loss['weight'] = self.weight_loss(torch.squeeze(feat_weight), weight_sim)
            if cfg.flag_vis:
                visib = input['visible'].to(cfg.device)
                loss['visib'] = self.weight_loss(torch.squeeze(feat_visib), visib)

            flag_exist = False
            for i in range(cfg.batch_size):
                if torch.eq(weight_sim[i], 1.0):
                    if not flag_exist:
                        flag_exist = True
                        loss['heatmap'] = self.heatmap_loss(prev_heatmap[i], joint_lhm[i])
                    else:
                        loss['heatmap'] += self.heatmap_loss(prev_heatmap[i], joint_lhm[i])
            if flag_exist:
                loss['heatmap'] *= 0.2

            return loss #, outs['coords'][-1]

        else:
            outs['coords'] = outs['coords'][-1]
            return outs


def get_model():
    if cfg.extra:
        return SAR_crossWeight_wVis_light()
        # return SAR_crossWeight()
        # return SAR_frontWeight()
        # return SAR_refineWeight()
    else:
        return SAR()

if __name__ == '__main__':
    import torch
    input = torch.rand(2, 3, 256, 256)
    net = SAR()
    output = net(input)
    print(output)



