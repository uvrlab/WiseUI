from data.processing import xyz2uvd, uvd2xyz, read_img, load_db_annotation, augmentation, cv2pil
from torch.utils.data import Dataset
import numpy as np
import torchvision.transforms as standard
from config import cfg
import json
import os
import sys
# sys.path.append("C:/Woojin/Research/WiseUI_Hand/WiseUI_Handtracking_module/handtracker")
# sys.path.append("C:/Woojin/Research/WiseUI_Hand/WiseUI_Handtracking_module/handtracker/mano_data")

sys.path.append(os.path.join(os.path.abspath(os.path.dirname(__file__)), "../mano_data"))
os.environ['DEX_YCB_DIR'] = 'C:/Woojin/dataset/DexYCB'
sys.path.append('C:/Woojin/dataset/DexYCB/dex-ycb-toolkit')
from dex_ycb_toolkit.factory import get_dataset
from data.processing import get_focal_pp, generate_extraFeature
from utils.visualize import render_mesh_multi_views, draw_2d_skeleton, draw_2d_vertex, draw_2d_skeleton_vis

import cv2
import pickle
import copy
import time
from manopth.manolayer import ManoLayer
from utils.mano import MANO
import torch
import statistics


class FreiHAND(Dataset):
    def __init__(self, root, mode):
        self.root = root
        self.mode = mode
        self.extra = cfg.extra
        assert self.mode in ['training', 'evaluation'], 'mode error'
        # load annotations
        self.anno_all = load_db_annotation(root, self.mode)
        if self.mode == 'evaluation':
            root = os.path.join(root, 'bbox_root_freihand_output.json')
            self.root_result = []
            with open(root) as f:
                annot = json.load(f)
            for i in range(len(annot)):
                self.root_result.append(np.array(annot[i]['root_cam']))
        mean_std = ([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
        self.transform = standard.Compose([standard.ToTensor(), standard.Normalize(*mean_std)])
        self.versions = ['gs', 'hom', 'sample', 'auto']

        # mano
        self.mano = MANO()
        self.face = self.mano.face

    def __getitem__(self, idx):
        if self.mode == 'training':
            version = self.versions[idx // len(self.anno_all)]
        else:
            version = 'gs'
        idx = idx % len(self.anno_all)
        img = read_img(idx, self.root, self.mode, version)
        bbox_size = 130
        bbox = [112 - bbox_size//2, 112 - bbox_size//2, bbox_size, bbox_size]
        img, img2bb_trans, bb2img_trans, _, _,  = \
            augmentation(img, bbox, self.mode, exclude_flip=True)
        img = cv2pil(img)
        img = self.transform(img)
        if self.mode == 'training':
            K, mesh_xyz, pose_xyz, scale = self.anno_all[idx]
            K, mesh_xyz, pose_xyz, scale = [np.array(x) for x in [K, mesh_xyz, pose_xyz, scale]]
            # concat mesh and pose label
            all_xyz = np.concatenate((mesh_xyz, pose_xyz), axis=0)
            all_uvd = xyz2uvd(all_xyz, K)
            # affine transform x,y coordinates
            uv1 = np.concatenate((all_uvd[:, :2], np.ones_like(all_uvd[:, :1])), 1)
            all_uvd[:, :2] = np.dot(img2bb_trans, uv1.transpose(1, 0)).transpose(1, 0)[:, :2]
            # wrist is the relative joint
            root_depth = all_uvd[cfg.num_vert:cfg.num_vert+1, 2:3].copy()
            all_uvd[:, 2:3] = (all_uvd[:, 2:3] - root_depth)
            # box to normalize depth
            all_uvd[:, 2:3] /= cfg.depth_box

            if self.extra:
                extra_uvd, extra_hm, w_aug = generate_extraFeature(all_uvd[cfg.num_vert:, :])
            # normalize uv
            all_uvd[:, :2] = all_uvd[:, :2] / (cfg.input_img_shape[0] // 2) - 1

            if self.extra:
                inputs = {'img': np.float32(img), 'extra': np.float32(extra_hm)}
                targets = {'mesh_pose_uvd': np.float32(all_uvd), 'weight_aug': np.float32(w_aug)}
            else:
                inputs = {'img': np.float32(img)}
                targets = {'mesh_pose_uvd': np.float32(all_uvd)}

            meta_info = {}

        else:
            K, scale = self.anno_all[idx]
            K, scale = [np.array(x) for x in [K, scale]]
            inputs = {'img': np.float32(img)}
            targets = {}
            meta_info = {
                'img2bb_trans': np.float32(img2bb_trans),
                'bb2img_trans': np.float32(bb2img_trans),
                'root_depth': np.float32(self.root_result[idx][2][None]),
                'K': np.float32(K),
                'scale': np.float32(scale)}
        return inputs, targets, meta_info

    def __len__(self):
        if self.mode == 'training':
            return len(self.anno_all) * 4
        else:
            return len(self.anno_all)

    def evaluate(self, outs, meta_info, cur_sample_idx):
        coords_uvd = outs['coords']
        batch = coords_uvd.shape[0]
        eval_result = {'pose_out': list(), 'mesh_out': list()}
        for i in range(batch):
            coord_uvd_crop, root_depth, img2bb_trans, bb2img_trans, K, scale = \
                coords_uvd[i], meta_info['root_depth'][i], meta_info['img2bb_trans'][i], \
                meta_info['bb2img_trans'][i], meta_info['K'][i], meta_info['scale'][i]
            coord_uvd_crop[:, 2] = coord_uvd_crop[:, 2] * cfg.depth_box + root_depth
            coord_uvd_crop[:, :2] = (coord_uvd_crop[:, :2] + 1) * cfg.input_img_shape[0] // 2
            # back to original image
            coord_uvd_full = coord_uvd_crop.copy()
            uv1 = np.concatenate((coord_uvd_full[:, :2], np.ones_like(coord_uvd_full[:, :1])), 1)
            coord_uvd_full[:, :2] = np.dot(bb2img_trans, uv1.transpose(1, 0)).transpose(1, 0)[:, :2]
            coord_xyz = uvd2xyz(coord_uvd_full, K)
            pose_xyz = coord_xyz[cfg.num_vert:]
            mesh_xyz = coord_xyz[:cfg.num_vert]
            eval_result['pose_out'].append(pose_xyz.tolist())
            eval_result['mesh_out'].append(mesh_xyz.tolist())
            if cfg.vis:
                mesh_xyz_crop = uvd2xyz(coord_uvd_crop[:cfg.num_vert], K)
                vis_root = os.path.join(cfg.output_root, 'FreiHAND_vis')
                if not os.path.exists(vis_root):
                    os.makedirs(vis_root)
                idx = cur_sample_idx + i
                img_full = read_img(idx, self.root, 'evaluation', 'gs')
                img_crop = cv2.warpAffine(img_full, img2bb_trans, cfg.input_img_shape, flags=cv2.INTER_LINEAR)
                focal, pp = get_focal_pp(K)
                cam_param = {'focal': focal, 'princpt': pp}
                img_mesh, view_1, view_2 = render_mesh_multi_views(img_crop, mesh_xyz_crop, self.face, cam_param)
                path_mesh_img = os.path.join(vis_root, 'render_mesh_img_{}.png'.format(idx))
                cv2.imwrite(path_mesh_img, img_mesh)
                path_mesh_view1 = os.path.join(vis_root, 'render_mesh_view1_{}.png'.format(idx))
                cv2.imwrite(path_mesh_view1, view_1)
                path_mesh_view2 = os.path.join(vis_root, 'render_mesh_view2_{}.png'.format(idx))
                cv2.imwrite(path_mesh_view2, view_2)
                path_joint = os.path.join(vis_root, 'joint_{}.png'.format(idx))
                vis = draw_2d_skeleton(img_crop, coord_uvd_crop[cfg.num_vert:])
                cv2.imwrite(path_joint, vis)
                path_img = os.path.join(vis_root, 'img_{}.png'.format(idx))
                cv2.imwrite(path_img, img_crop)

        return eval_result

    def print_eval_result(self, eval_result):
        output_json_save_path = os.path.join('./output/', 'pred_Frei.json')
        with open(output_json_save_path, 'w') as fo:
            json.dump([eval_result['pose_out'], eval_result['mesh_out']], fo)
        print('Dumped %d joints and %d verts predictions to %s'
              % (len(eval_result['pose_out']), len(eval_result['mesh_out']), output_json_save_path))


class DexYCB(Dataset):
    def __init__(self, root, mode=None, small_db=False, loadit=True):
        self.root = root
        self.mode = mode
        assert self.mode in ['training', 'evaluation'], 'mode error'
        self.extra = cfg.extra

        # 'train', 'val', 'test'
        if self.mode == 'training':
            self.mode = 'train'
            self._mode = 'training'
            self._name = 's0_train'
        else:
            self._mode = 'evaluation'
            self._name = 's0_test'

        mean_std = ([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
        self.transform = standard.Compose([standard.ToTensor(), standard.Normalize(*mean_std)])

        self._out_dir = os.path.join(os.path.dirname(__file__), "cfg_DexYCB")
        self._anno_file = os.path.join(self._out_dir, "cfg_anno_{}.pkl".format(self._name))
        self._pose_file = os.path.join(self._out_dir, "cfg_pose_{}.pkl".format(self._name))
        self._mesh_file = os.path.join(self._out_dir, "cfg_mesh_{}.pkl".format(self._name))

        self.IMAGE_WIDTH = 640.
        self.IMAGE_HEIGHT = 480.

        if loadit and os.path.isfile(self._anno_file):
            print('Found HPE annotation file.')
        elif loadit:
            print('Require HPE annotation file.')
        else:
            print('Generating find HPE annotation file.')
            self._dataset = get_dataset(self._name)
            self._generate_anno_file()

        self._anno, self._pose, self._mesh = self._load_anno_file()

        if small_db:
            db_len = len(self._anno)
            small_len = int(db_len/10)

            self._anno = dict(list(self._anno.items())[:small_len])
            self._pose = dict(list(self._pose.items())[:small_len])
            self._mesh = dict(list(self._mesh.items())[:small_len])

            print("activating small DB with size : ", len(self._anno))



    def _generate_anno_file(self):
        """Generates the annotation file."""
        print('Generating HPE annotation file')
        s = time.time()

        self.samples = dict()
        self.pose_list = dict()
        self.mesh_list = dict()
        idx = 0

        # Load MANO layer.
        self.mano_layer_right = ManoLayer(flat_hand_mean=False,
                                          ncomps=45,
                                          side='right',
                                          mano_root='manopth/mano/models',
                                          use_pca=True)
        self.mano_layer_left = ManoLayer(flat_hand_mean=False,
                                         ncomps=45,
                                         side='left',
                                         mano_root='manopth/mano/models',
                                         use_pca=True)
        self.mano_layer_right.cuda()
        self.mano_layer_left.cuda()

        xyz_list = list()

        for i in range(len(self._dataset)):
            if (i + 1) in np.floor(np.linspace(0, len(self._dataset), 11))[1:]:
                print('{:3.0f}%  {:6d}/{:6d}'.format(100 * i / len(self._dataset), i,
                                                     len(self._dataset)))

            sample = copy.deepcopy(self._dataset[i])

            color_name = sample['color_file']
            depth_name = sample['depth_file']

            fx = sample['intrinsics']['fx']
            fy = sample['intrinsics']['fy']
            cx = sample['intrinsics']['ppx']
            cy = sample['intrinsics']['ppy']

            camera_intrinsics = np.array([[fx, 0, cx], [0, fy, cy], [0, 0, 1]])
            # print("compare with other dataset, cam intrins = ", camera_intrinsics)

            label = np.load(sample['label_file'])
            joint_3d = label['joint_3d'].reshape(21, 3)
            if np.all(joint_3d == -1):
                continue

            joint_2d = label['joint_2d'].reshape(21, 2)
            if np.all(joint_2d == -1):
                continue

            pose_m = label['pose_m']
            if np.all(pose_m == 0):
                continue

            pose_uv = np.copy(joint_2d)
            pose_xyz = np.copy(joint_3d)

            ### generate BoundingBox ###
            u_min = int(np.min(pose_uv[:, 0]))
            u_max = int(np.max(pose_uv[:, 0]))
            v_min = int(np.min(pose_uv[:, 1]))
            v_max = int(np.max(pose_uv[:, 1]))

            bbox = [u_min, v_min, u_max, v_max]

            u_center = (u_min + u_max) / 2
            v_center = (v_min + v_max) / 2

            if self.mode == 'train':
                if u_center > 640 or v_center > 480:
                    # color = img / 255.0
                    # cv2.imshow("out-ranged img", color)
                    # cv2.waitKey(1)
                    continue

            xyz_forMKA = copy.deepcopy(pose_xyz) * 1000.
            xyz_list.append(xyz_forMKA)

            # color = img / 255.0
            # cv2.imshow("img", color)
            # cv2.waitKey(1)

            betas_save = copy.deepcopy(sample['mano_betas'])
            pose_m_save = copy.deepcopy(pose_m)

            pose = torch.from_numpy(pose_m).cuda()
            betas = torch.tensor(sample['mano_betas'], dtype=torch.float32).unsqueeze(0).cuda()
            hand_side = sample['mano_side']

            if hand_side == 'right':
                verts, _ = self.mano_layer_right(pose[:, 0:48], betas, pose[:, 48:51])
                #faces = self.mano_layer_right.th_faces.cpu().numpy()
            elif hand_side == 'left':
                verts, _ = self.mano_layer_left(pose[:, 0:48], betas, pose[:, 48:51])
                #faces = self.mano_layer_left.th_faces.cpu().numpy()
            else:
                raise KeyError('Unknown hand_side: {}'.format(hand_side))

            mesh_xyz = np.squeeze(verts.cpu().numpy()) / 1000.

            ### generate visibility value on uvd ###
            pose_uv_vis = np.round(copy.deepcopy(pose_uv).astype(np.int))
            pose_z_vis = copy.deepcopy(pose_xyz[:, -1]) * 1000.
            depth = cv2.imread(depth_name, cv2.IMREAD_ANYDEPTH)

            # depth: A uint16 numpy array of shape [H, W] containing the depth image. (mm)
            visible = []
            VISIBLE_PARAM = 40
            for i_vis in range(21):
                if pose_uv_vis[i_vis, 0] >= self.IMAGE_WIDTH or pose_uv_vis[i_vis, 1] >= self.IMAGE_HEIGHT:
                    continue
                d_img = depth[pose_uv_vis[i_vis, 1], pose_uv_vis[i_vis, 0]]
                d_gt = pose_z_vis[i_vis]
                if np.abs(d_img - d_gt) < VISIBLE_PARAM:
                    visible.append(i_vis)

            # color = cv2.imread(color_name) / 255.
            # vis = draw_2d_skeleton_vis(color, pose_uv_vis, visible)
            # cv2.imshow("vis", vis)
            # cv2.waitKey(0)

            sample_db = {
                'color': color_name,
                'frame_idx': int(i),
                'bb': bbox,
                'camMat': camera_intrinsics,
                'hand_side': hand_side,
                'pose_uv': pose_uv,
                'label_file': sample['label_file'],
                'betas': betas_save,
                'pose_m': pose_m_save,
                'vis': visible
            }
            sample_pose = {
                'pose_xyz': pose_xyz,
            }
            sample_mesh = {
                'mesh_xyz': mesh_xyz,
            }

            self.samples[idx] = sample_db
            self.pose_list[idx] = sample_pose
            self.mesh_list[idx] = sample_mesh

            idx += 1
            if idx % 1000 == 0:
                print("preprocessing idx : ", idx)

        print('# total samples: {:6d}'.format(len(self._dataset)))
        print('# valid samples: {:6d}'.format(len(self.samples)))

        self.evaluate_MKA(xyz_list)

        # with open(self._anno_file, 'wb') as f:
        #     pickle.dump(self.samples, f)
        #
        # with open(self._pose_file, 'wb') as f:
        #     pickle.dump(self.pose_list, f)
        #
        # with open(self._mesh_file, 'wb') as f:
        #     pickle.dump(self.mesh_list, f)

        e = time.time()
        print('time: {:7.2f}'.format(e - s))

    def _load_anno_file(self):
        """Loads the annotation file.

        Returns:
          A dictionary holding the loaded annotation.
        """
        with open(self._anno_file, 'rb') as f:
            anno = pickle.load(f)
            # anno = json.load(f)

        with open(self._pose_file, 'rb') as f:
            pose_list = pickle.load(f)

        with open(self._mesh_file, 'rb') as f:
            mesh_list = pickle.load(f)

        # anno['joint_3d'] = {
        #     k: v.astype(np.float64) for k, v in anno['joint_3d'].items()
        # }

        return anno, pose_list, mesh_list

    def __getitem__(self, idx):
        return self.preprocess(idx)

    def __len__(self):
        return len(self._anno)

    def preprocess(self, idx):
        """
        _anno = {
                 'color': color_name,
                'frame_idx': int(i),
                'bb': bbox,
                'camMat': camera_intrinsics,
                'hand_side': hand_side,
                'pose_uv': pose_uv,
                'label_file': sample['label_file'],
                'betas': betas_save,
                'pose_m': pose_m_save
                'vis': visible
            }
            _pose = {
                'pose_xyz': pose_xyz,
            }
            _mesh = {
                'mesh_xyz': mesh_xyz,
            }
        """
        idx = idx % len(self._anno)
        sample = copy.deepcopy(self._anno[idx])
        sample_pose = copy.deepcopy(self._pose[idx])
        sample_mesh = copy.deepcopy(self._mesh[idx])

        hand_side = sample['hand_side']
        K = sample['camMat']
        bbox = sample['bb']
        img = cv2.imread(sample['color']) # need 0~255 scale

        img_debug = np.copy(img)
        # cv2.imshow("DexYCB img before augment", img_debug)
        # cv2.waitKey(1)

        ### augmenation with bounding box ###
        u_min, v_min, u_max, v_max = bbox
        u_center = (u_max + u_min) / 2.
        v_center = (v_max + v_min) / 2.
        bbox_size = max(u_max - u_min, v_max - v_min) + 80
        u_min = u_center - bbox_size / 2.
        v_min = v_center - bbox_size / 2.

        bbox = [u_min, v_min, bbox_size, bbox_size]

        img, img2bb_trans, bb2img_trans, _, _, = \
            augmentation(img, bbox, self._mode, exclude_flip=True)

        if hand_side == 'left':
            img = cv2.flip(img, 1)

        img_debug = copy.deepcopy(img) / 255.0
        # cv2.imshow("DexYCB img after augment", img_debug)
        # cv2.waitKey(1)

        img_pil = cv2pil(img)
        img = self.transform(img_pil)

        if self.mode == 'train':
            # concat mesh and pose label
            mesh_xyz = sample_mesh['mesh_xyz']
            pose_xyz = sample_pose['pose_xyz']
            all_xyz = np.concatenate((mesh_xyz, pose_xyz), axis=0)
            all_uvd = xyz2uvd(all_xyz, K)  # (799, 3)

            # affine transform x,y coordinates
            uv1 = np.concatenate((all_uvd[:, :2], np.ones_like(all_uvd[:, :1])), 1)
            all_uvd[:, :2] = np.dot(img2bb_trans, uv1.transpose(1, 0)).transpose(1, 0)[:, :2]

            # wrist is the relative joint & box to normalize depth
            root_depth = all_uvd[cfg.num_vert:cfg.num_vert + 1, 2:3].copy()
            all_uvd[:, 2:3] = (all_uvd[:, 2:3] - root_depth)
            all_uvd[:, 2:3] /= cfg.depth_box

            # flip if left hand
            if hand_side == 'left':
                all_uvd[:, 0] = 256. - np.copy(all_uvd[:, 0])
                # handKps = np.copy(all_uvd[cfg.num_vert:, :-1])
                # verts_uv = np.copy(all_uvd[:cfg.num_vert, :-1])
                # color = np.copy(img_debug)
                # vis = draw_2d_skeleton(color, handKps)
                # vis_vert = draw_2d_vertex(color, verts_uv)
                #
                # vis = cv2.resize(vis, dsize=(416, 416), interpolation=cv2.INTER_CUBIC)
                # vis_vert = cv2.resize(vis_vert, dsize=(416, 416), interpolation=cv2.INTER_CUBIC)
                # color = cv2.resize(color, dsize=(416, 416), interpolation=cv2.INTER_CUBIC)
                # cv2.imshow("vis", vis)
                # cv2.imshow("vis_vert", vis_vert)
                # cv2.imshow("img", color)
                # cv2.waitKey(0)

            if self.extra:
                extra_uvd, extra_hm, w_aug = generate_extraFeature(all_uvd[cfg.num_vert:, :])
            # normalize uv
            all_uvd[:, :2] = all_uvd[:, :2] / (cfg.input_img_shape[0] // 2) - 1

            vis = len(sample['vis']) / 21.
            if self.extra:
                if cfg.flag_vis:
                    vis = len(sample['vis']) / 21.
                    inputs = {'img': np.float32(img), 'extra': np.float32(extra_hm), 'visible': np.float32(vis)}
                else:
                    inputs = {'img': np.float32(img), 'extra': np.float32(extra_hm)}
                targets = {'mesh_pose_uvd': np.float32(all_uvd), 'weight_aug': np.float32(w_aug)}
            else:
                inputs = {'img': np.float32(img), 'debug': np.float32(img_debug)}
                targets = {'mesh_pose_uvd': np.float32(all_uvd)}

            meta_info = {}

        else:
            pose_xyz = sample_pose['pose_xyz']
            root_depth = pose_xyz[0, 2]
            img_idx = sample['frame_idx']

            inputs = {'img': np.float32(img)}
            targets = {}
            meta_info = {
                'img2bb_trans': np.float32(img2bb_trans),
                'bb2img_trans': np.float32(bb2img_trans),
                'K': np.float32(K),
                'root_depth': np.float32(root_depth),
                'img_idx': img_idx

            }

        return inputs, targets, meta_info


    def evaluate(self, outs, meta_info, cur_sample_idx):
        coords_uvd = outs['coords']
        batch = coords_uvd.shape[0]
        eval_result = {'pose_out': list(), 'mesh_out': list(), 'img_idx': list()}
        for i in range(batch):
            coord_uvd_crop, root_depth, img2bb_trans, bb2img_trans, K = \
                coords_uvd[i], meta_info['root_depth'][i], meta_info['img2bb_trans'][i], \
                meta_info['bb2img_trans'][i], meta_info['K'][i]
            coord_uvd_crop[:, 2] = coord_uvd_crop[:, 2] * cfg.depth_box + root_depth
            coord_uvd_crop[:, :2] = (coord_uvd_crop[:, :2] + 1) * (cfg.input_img_shape[0] // 2)

            # back to original image
            coord_uvd_full = coord_uvd_crop.copy()
            uv1 = np.concatenate((coord_uvd_full[:, :2], np.ones_like(coord_uvd_full[:, :1])), 1)
            coord_uvd_full[:, :2] = np.dot(bb2img_trans, uv1.transpose(1, 0)).transpose(1, 0)[:, :2]

            coord_xyz = uvd2xyz(coord_uvd_full, K)
            pose_xyz = coord_xyz[cfg.num_vert:].flatten(order='C')  # 'C', 'F'
            mesh_xyz = coord_xyz[:cfg.num_vert]

            pose_xyz *= 1000.   # required scaling

            img_idx = int(meta_info['img_idx'][i])

            eval_result['pose_out'].append(pose_xyz.tolist())
            eval_result['mesh_out'].append(mesh_xyz.tolist())
            eval_result['img_idx'].append(img_idx)

        return eval_result

    def print_eval_result(self, eval_result):
        output_txt_save_path = os.path.join('./output/', 'pred_DexYCB.txt')
        image_idx = 1
        with open(output_txt_save_path, 'w') as fo:
            # json.dump([eval_result['pose_out'], eval_result['mesh_out']], fo)
            for line, img_idx in zip(eval_result['pose_out'], eval_result['img_idx']):
                # line : (63, ) list, img_idx : (1, ) int
                fo.write(str(img_idx))
                for data in line:
                    data = round(data, 4)
                    fo.write(','+str(data))
                fo.write('\n')

        print('Write %d joints and %d verts predictions to %s'
              % (len(eval_result['pose_out']), len(eval_result['mesh_out']), output_txt_save_path))

        xyz_pred_list = copy.deepcopy(eval_result['pose_out'])
        self.evaluate_MKA(xyz_pred_list)

    def evaluate_MKA(self, xyz_pred_list):
        # calculate mean-keypoint-acceleration (MKA)
        MKA_list = list()
        MKA_target = list()
        for xyz_pred in xyz_pred_list:
            xyz_pred = np.array(xyz_pred)
            xyz_pred = xyz_pred.reshape((21, 3))
            MKA_target.append(xyz_pred)

        for i in range(1, len(MKA_target) - 1):
            prev_xyz = np.copy(MKA_target[i - 1])   # (21,3)
            curr_xyz = np.copy(MKA_target[i])
            futu_xyz = np.copy(MKA_target[i + 1])

            acc = prev_xyz + futu_xyz - 2 * curr_xyz
            acc_3d = np.square(acc[:, 0]) + np.square(acc[:, 1]) + np.square(acc[:, 2])
            acc_3d = np.sqrt(acc_3d)  # should be [21, ]
            acc_avg = np.average(acc_3d)

            if not acc_avg > 10:
                MKA_list.append(acc_avg)

        total_MKA_avg = statistics.mean(MKA_list)
        print("total_MKA_avg : ", total_MKA_avg)


class HO3D_v2(Dataset):
    def __init__(self, root='../../dataset/HO3D_v2', mode='training', loadit=True):
        ###
        # initial setting
        # 640*480 image to 32*32*5 box
        # only control depth_discretization parameter(=5) in cfg.yaml
        # hand joint order : Mano
        ###
        self.coord_change_mat = np.array([[1., 0., 0.], [0, -1., 0.], [0., 0., -1.]], dtype=np.float32)

        self.root = root
        if mode == 'training':
            self.mode = 'train'
            self.name = 'train'
            self._mode = mode
        if mode == 'evaluation':
            self.mode = mode
            self.name = 'valid'
            self._mode = mode

        self.loadit = loadit

        mean_std = ([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
        self.transform = standard.Compose([standard.ToTensor(), standard.Normalize(*mean_std)])

        self.IMAGE_WIDTH = 640.
        self.IMAGE_HEIGHT = 480.
        self.camera_intrinsics = None

        self.jointsMapManoToSimple = [0,
                         13, 14, 15, 16,
                         1, 2, 3, 17,
                         4, 5, 6, 18,
                         10, 11, 12, 19,
                         7, 8, 9, 20]


        print('Loading HO3D_v2 dataset index ...')
        t = time.time()

        if not loadit:
            layer = ManoLayer(flat_hand_mean=True,
                side='right',
                mano_root='./manopth/mano/models',
                ncomps=45)

            layer.cuda()

            subject_path = os.path.join(root, self.mode)
            subjects = os.listdir(subject_path)

            dataset = dict()


            total_db_len = 0
            for subject in subjects:
                subject = str(subject)
                dataset[subject] = list()

                rgb_set = list(os.listdir(os.path.join(root, self.mode, subject, 'rgb')))
                frames = len(rgb_set)
                total_db_len += frames
                for i in range(frames):
                    dataset[subject].append(rgb_set[i])

            self.samples = dict()
            idx = 0
            db_idx = 0
            for subject in list(dataset):
                for frame in dataset[subject]:
                    if (db_idx + 1) in np.floor(np.linspace(0, total_db_len, 11))[1:]:
                        print('{:3.0f}%  {:6d}/{:6d}'.format(100 * db_idx / total_db_len, db_idx,
                                                             total_db_len))
                    db_idx += 1

                    sample = {
                        'subject': subject,
                        'frame_idx': frame[:-4],
                    }
                    _, _, meta = self.read_data(sample)

                    self.camera_intrinsics = copy.deepcopy(meta['camMat'])

                    # get hand model from manopth...
                    pose_params, beta, trans = meta['handPose'], meta['handBeta'], meta['handTrans']
                    pose_params = torch.from_numpy(np.expand_dims(pose_params, axis=0)).cuda()
                    beta = torch.from_numpy(np.expand_dims(beta, axis=0)).cuda()
                    trans = torch.from_numpy(np.expand_dims(trans, axis=0)).cuda()
                    verts, _ = layer(pose_params, th_betas=beta, th_trans=trans)
                    verts = verts.cpu().numpy()
                    verts = np.squeeze(verts) / 1000.

                    # isOpenGLCoords False - positive z axis  / True - negative z axis
                    # we need isOpenGLCoords False, positive z
                    verts = verts.dot(self.coord_change_mat.T)
                    mesh_xyz = copy.deepcopy(verts)

                    pose_xyz = copy.deepcopy(meta['handJoints3D'])
                    pose_xyz = pose_xyz.dot(self.coord_change_mat.T)

                    pose_xyz_debug = copy.deepcopy(pose_xyz)
                    pose_uvd = xyz2uvd(pose_xyz_debug, self.camera_intrinsics)  # (799, 3)


                    ### generate BoundingBox ###
                    u_min = int(np.min(pose_uvd[:, 0]))
                    u_max = int(np.max(pose_uvd[:, 0]))
                    v_min = int(np.min(pose_uvd[:, 1]))
                    v_max = int(np.max(pose_uvd[:, 1]))

                    bbox = [u_min, v_min, u_max, v_max]

                    u_center = (u_min + u_max) / 2
                    v_center = (v_min + v_max) / 2

                    if u_center > self.IMAGE_WIDTH or v_center > self.IMAGE_HEIGHT:
                        continue

                    new_sample = {
                        'subject': subject,
                        'frame_idx': frame[:-4],
                        'pose_xyz': pose_xyz,
                        'mesh_xyz': mesh_xyz,
                        'bb': bbox,
                        'camMat': meta['camMat']
                    }
                    self.samples[idx] = new_sample
                    idx += 1
                    if idx % 1000 == 0:
                        print("preprocessing idx : ", idx)

            self.clean_data()
            self.save_samples()
            print('Saving done, cfg_HO3D_v2')

        else:
            self.samples = self.load_samples()
            ### test meta data has missing annotation, only acquire images in 'train' folder ###

            print('Loading of %d samples done in %.2f seconds' % (len(self.samples), time.time() - t))

    def load_samples(self):
        with open('data/cfg_HO3D_v2/{}.pkl'.format(self.name), 'rb') as f:
            samples = pickle.load(f)
            return samples

    def save_samples(self):
        with open('data/cfg_HO3D_v2/{}.pkl'.format(self.name), 'wb') as f:
            pickle.dump(list(self.samples), f, pickle.HIGHEST_PROTOCOL)

    def clean_data(self):
        print("Size beforing cleaning: {}".format(len(self.samples.keys())))

        for key in list(self.samples):
            try:
                self.__getitem__(key)
            except Exception as e:
                print(e)
                print("Index failed: {}".format(key))
                del self.samples[key]

        self.samples = list(self.samples.values())
        print("Size after cleaning: {}".format(len(self.samples)))

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        return self.preprocess(idx)

    def get_image(self, sample):
        file_name = sample['frame_idx'] + '.png'
        img_path = os.path.join(self.root, 'train', sample['subject'], 'rgb', file_name)
        img = cv2.imread(img_path)
        return img

    def read_data(self, sample):
        file_name = sample['frame_idx'] + '.pkl'
        meta_path = os.path.join(self.root, 'train', sample['subject'], 'meta', file_name)
        with open(meta_path, 'rb') as f:
            meta = pickle.load(f)

        file_name = sample['frame_idx'] + '.png'
        img_path = os.path.join(self.root, 'train', sample['subject'], 'rgb', file_name)
        # _assert_exist(img_path)
        rgb = cv2.imread(img_path)

        img_path = os.path.join(self.root, 'train', sample['subject'], 'depth', file_name)
        # _assert_exist(img_path)
        depth_scale = 0.00012498664727900177
        depth = cv2.imread(img_path)

        dpt = depth[:, :, 2] + depth[:, :, 1] * 256
        dpt = dpt * depth_scale

        return rgb, dpt, meta

    def preprocess(self, idx):
        """
          new_sample = {
                         'subject': subject,
                        'frame_idx': frame[:-4],
                        'pose_xyz': pose_xyz,
                        'mesh_xyz': mesh_xyz,
                        'bb': bbox,
                        'camMat': meta['camMat']
                    }
        """
        idx = idx % len(self.samples)
        sample = self.samples[idx]
        bbox = sample['bb']
        K = sample['camMat']

        img = self.get_image(sample)
        # img_debug = np.copy(img)
        # cv2.imshow("HO3D img before augment", img_debug)
        # cv2.waitKey(1)

        x_min, y_min, x_max, y_max = bbox
        x_center = (x_max + x_min) / 2.
        y_center = (y_max + y_min) / 2.
        bbox_size = max(x_max - x_min, y_max - y_min) + 80
        x_min = x_center - bbox_size / 2.
        y_min = y_center - bbox_size / 2.
        bbox = [x_min, y_min, bbox_size, bbox_size]

        img, img2bb_trans, bb2img_trans, _, _, = \
            augmentation(img, bbox, self._mode, exclude_flip=True)

        img_debug = np.copy(img) / 255.0
        # cv2.imshow("HO3D img", img_debug)
        # cv2.waitKey(1)

        img_pil = cv2pil(img)
        img = self.transform(img_pil)

        if self.mode == 'train':
            mesh_xyz = copy.deepcopy(sample['mesh_xyz'])
            pose_xyz = copy.deepcopy(sample['pose_xyz'])

            pose_xyz = pose_xyz[self.jointsMapManoToSimple]

            all_xyz = np.concatenate((mesh_xyz, pose_xyz), axis=0)
            all_uvd = xyz2uvd(all_xyz, K)  # (799, 3)
            # affine transform x,y coordinates
            uv1 = np.concatenate((all_uvd[:, :2], np.ones_like(all_uvd[:, :1])), 1)
            all_uvd[:, :2] = np.dot(img2bb_trans, uv1.transpose(1, 0)).transpose(1, 0)[:, :2]

            # wrist is the relative joint & box to normalize depth
            root_depth = all_uvd[cfg.num_vert:cfg.num_vert + 1, 2:3].copy()
            all_uvd[:, 2:3] = (all_uvd[:, 2:3] - root_depth)
            all_uvd[:, 2:3] /= cfg.depth_box

            handKps = np.copy(all_uvd[cfg.num_vert:, :-1])
            verts_uv = np.copy(all_uvd[:cfg.num_vert, :-1])
            color = np.copy(img_debug)
            vis = draw_2d_skeleton(color, handKps)
            vis_vert = draw_2d_vertex(color, verts_uv)

            vis = cv2.resize(vis, dsize=(416, 416), interpolation=cv2.INTER_CUBIC)
            vis_vert = cv2.resize(vis_vert, dsize=(416, 416), interpolation=cv2.INTER_CUBIC)
            color = cv2.resize(color, dsize=(416, 416), interpolation=cv2.INTER_CUBIC)
            cv2.imshow("vis", vis)
            cv2.imshow("vis_vert", vis_vert)
            cv2.imshow("img", color)
            cv2.waitKey(0)

            # img normalize
            all_uvd[:, :2] = all_uvd[:, :2] / (cfg.input_img_shape[0] // 2) - 1

            inputs = {'img': np.float32(img), 'debug': np.float32(img_debug)}
            targets = {'mesh_pose_uvd': np.float32(all_uvd)}

            meta_info = {}
        else:
            K = sample['camMat']
            inputs = {'img': np.float32(img)}
            targets = {}
            meta_info = {
                'img2bb_trans': np.float32(img2bb_trans),
                'bb2img_trans': np.float32(bb2img_trans),
                'K': np.float32(K),
                'root_depth': np.float32(handJoints3D[2])
                }

        return inputs, targets, meta_info



def load_dataset(dataset, mode):
    if dataset == 'FreiHAND':
        return FreiHAND(os.path.join('../../dataset', dataset), mode)
    if dataset == 'DexYCB':
        return DexYCB(os.path.join('../../dataset', dataset), mode=mode)
    if dataset == 'HO3D_v2':
        return HO3D_v2(os.path.join('../../dataset', dataset), mode)
