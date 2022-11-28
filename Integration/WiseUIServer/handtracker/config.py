import torch

class Config:
    pre = 'name'
    dataset = 'DexYCB'      # DexYCB, FreiHAND, HO3D_v2
    output_root = './output'
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    # modifiable
    flag_vis = False
    extra = True

    continue_train = False
    reInit_optimizer = False
    checkpoint = './handtracker/checkpoint/SAR_refineWeight_cross_v2_extraTrue_resnet34_Epochs50/checkpoint.pth'
    # checkpoint = './handtracker/checkpoint/SAR-R34-S2-65-67.pth'  # put the path of the trained model's weights here

    extra_width = 64
    # network
    backbone = 'resnet34'
    num_stage = 2
    num_FMs = 8
    feature_size = 64
    heatmap_size = 32
    num_vert = 778
    num_joint = 21
    # training
    batch_size = 64
    lr = 3e-4       #3e-4
    total_epoch = 30
    input_img_shape = (256, 256)
    depth_box = 0.3
    num_worker = 8
    # -------------
    save_epoch = 1
    eval_interval = 1
    print_iter = 10
    num_epoch_to_eval = 80
    # -------------
    vis = False
    # -------------
    experiment_name = pre + '_extra{}'.format(extra) + '_{}'.format(backbone) + '_Epochs{}'.format(total_epoch)

cfg = Config()
