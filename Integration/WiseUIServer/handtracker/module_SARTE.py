import copy
import os
os.environ["PYOPENGL_PLATFORM"] = "OSMesa"
import sys
sys.path.append(os.path.abspath(os.path.dirname(__file__)))     # append current dir to PATH

import torch
from tqdm import tqdm
import cv2
import time
import torchvision.transforms as standard
import numpy as np

from base import Tester
from config import cfg
from utils.visualize import draw_2d_skeleton, draw_2d_vertex
from data.processing import inference_extraHM, augmentation, cv2pil, augmentation_real
import mediapipe as mp

mp_drawing = mp.solutions.drawing_utils
mp_drawing_styles = mp.solutions.drawing_styles
mp_hands = mp.solutions.hands


class HandTracker():
    def __init__(self):
        self.tester = Tester()
        self.tester._make_model()
        # self.detector = HandDetector()

        mean_std = ([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
        self.transform = standard.Compose([standard.ToTensor(), standard.Normalize(*mean_std)])

        if cfg.extra:
            self.extra_uvd_left = np.zeros((21, 3), dtype=np.float32)
            self.extra_uvd_right = np.zeros((21, 3), dtype=np.float32)
            self.idx = 0

        self.crop_size = 320

        self.prev_bbox_list = [[0, 0,  self.crop_size,  self.crop_size]]
        self.prev_flag_flip_list = []

    def Process(self, img): # input : img_cv
        print("processing ...")
        if img.shape[-1] == 4:
            img = img[:, :, :-1]
        imgSize = (img.shape[0], img.shape[1])  # (360, 640)

        img_cv = copy.deepcopy(img)
        ### hand detection from GY (65ms)
        # bbox_list, hand_side_list = self.detector.run(img)       # bbox : [bb_x_min, bb_y_min, bb_width, bb_height]

        ### hand detection with mediapipe (60ms)
        # currently extracting only right-side hand
        bbox_list, img_crop_list, img2bb_trans_list, bb2img_trans_list, flag_flip_list = self.extract_hand(img)

        joint_uvd_list = []
        # mesh_uvd_list = []
        if len(bbox_list) == 0:
            print("no bbox, return zero joint")
            joint_uvd = np.zeros((21, 3), dtype=np.float32)
            joint_uvd_list.append(joint_uvd)
            return joint_uvd_list

        else:
            debug_i = 0
            for bbox, img_crop, img2bb_trans, bb2img_trans, flag_flip in \
                    zip(bbox_list, img_crop_list, img2bb_trans_list, bb2img_trans_list, flag_flip_list):
                # crop_name = 'crop_{}'.format(debug_i)
                # debug_i += 1
                # cv2.imshow(crop_name, img_crop/255.)
                # cv2.waitKey(1)

                # transform img
                img_pil = cv2pil(img_crop)
                img = self.transform(img_pil)
                img = torch.unsqueeze(img, 0).type(torch.float32)
                inputs = {'img': img}

                if cfg.extra:
                    if flag_flip:
                        self.extra_uvd = copy.deepcopy(self.extra_uvd_left)
                    else:
                        self.extra_uvd = copy.deepcopy(self.extra_uvd_right)

                    # affine transform x,y coordinates with current crop info
                    uv1 = np.concatenate((self.extra_uvd[:, :2], np.ones_like(self.extra_uvd[:, :1])), 1)
                    self.extra_uvd[:, :2] = np.dot(img2bb_trans, uv1.transpose(1, 0)).transpose(1, 0)[:, :2]

                    # normalize uv, depth is already relative value
                    self.extra_uvd[:, :2] = self.extra_uvd[:, :2] / (cfg.input_img_shape[0] // 2) - 1

                    extra_hm = inference_extraHM(self.extra_uvd, self.idx, reinit_num=10)
                    inputs['extra'] = torch.unsqueeze(torch.from_numpy(extra_hm), dim=0)
                    self.idx += 1

                with torch.no_grad():
                    outs = self.tester.model(inputs)

                outs = {k: v.cpu().numpy() for k, v in outs.items()}
                coords_uvd = outs['coords'][0]

                # normalized value to uv(pixel) range
                coords_uvd[:, :2] = (coords_uvd[:, :2] + 1) * (cfg.input_img_shape[0] // 2)

                # back to original image
                uv1 = np.concatenate((coords_uvd[:, :2], np.ones_like(coords_uvd[:, :1])), 1)
                coords_uvd[:, :2] = np.dot(bb2img_trans, uv1.transpose(1, 0)).transpose(1, 0)[:, :2]

                if cfg.extra:
                    if flag_flip:
                        self.extra_uvd_left = copy.deepcopy(coords_uvd[cfg.num_vert:])
                    else:
                        self.extra_uvd_right = copy.deepcopy(coords_uvd[cfg.num_vert:])

                # restore depth value after passing extra pose
                coords_uvd[:, 2] = coords_uvd[:, 2] * cfg.depth_box # + root_depth (we don't know)

                all_uvd = copy.deepcopy(coords_uvd)

                # mesh_uvd = copy.deepcopy(all_uvd[:cfg.num_vert])  # (778, 3)
                joint_uvd = copy.deepcopy(all_uvd[cfg.num_vert:])   # (21, 3)
                if flag_flip:
                    joint_uvd[:, 0] = imgSize[1] - joint_uvd[:, 0]
                    # mesh_uvd[:, 0] = imgSize[1] - mesh_uvd[:, 0]

                joint_uvd_list.append(joint_uvd)
                # mesh_uvd_list.append(mesh_uvd)
            print("processing ... done")

            ### visualize output in server ###
            img_joint = copy.deepcopy(img_cv)
            for joint_uvd in joint_uvd_list:
                img_joint = draw_2d_skeleton(img_joint, joint_uvd)
            cv2.imshow('img_cv', img_joint)
            cv2.waitKey(1)

            return joint_uvd_list


    def extract_hand(self, img):
        image_rows, image_cols, _ = img.shape
        idx_to_coord_0 = None
        idx_to_coord_1 = None

        #### extract image bounding box ####
        with mp_hands.Hands(static_image_mode=True, max_num_hands=2, min_detection_confidence=0.3) as hands:
            image = cv2.flip(img, 1)
            # Convert the BGR image to RGB before processing.
            results = hands.process(cv2.cvtColor(image, cv2.COLOR_BGR2RGB))

            hand_idx = 0
            if results.multi_hand_landmarks:
                for hand_landmarks in results.multi_hand_landmarks:
                    idx_to_coordinates = {}
                    for idx, landmark in enumerate(hand_landmarks.landmark):
                        landmark_px = mp_drawing._normalized_to_pixel_coordinates(landmark.x, landmark.y,
                                                                                  image_cols, image_rows)
                        if landmark_px:
                            # landmark_px has fliped x axis
                            orig_x = image_cols - landmark_px[0]
                            idx_to_coordinates[idx] = [orig_x, landmark_px[1]]
                    if hand_idx == 0:
                        idx_to_coord_0 = idx_to_coordinates
                        hand_idx += 1
                    else:
                        idx_to_coord_1 = idx_to_coordinates

            ### visualize mediapipe output ###
            # image.flags.writeable = True
            # # image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)
            # if results.multi_hand_landmarks:
            #     for hand_landmarks in results.multi_hand_landmarks:
            #         mp_drawing.draw_landmarks(
            #             image,
            #             hand_landmarks,
            #             mp_hands.HAND_CONNECTIONS,
            #             mp_drawing_styles.get_default_hand_landmarks_style(),
            #             mp_drawing_styles.get_default_hand_connections_style())
            # # Flip the image horizontally for a selfie-view display.
            # # already fliped x axis   cv2.imshow('MediaPipe Hands', cv2.flip(image, 1))
            # image = cv2.flip(image, 1)
            # cv2.imshow('MediaPipe Hands', image)
            # cv2.waitKey(1)

        return self.extract_roi_list(results, img, idx_to_coord_0, idx_to_coord_1, image_rows, image_cols)


    def extract_roi_list(self, results, img, idx_to_coord_0, idx_to_coord_1, image_rows, image_cols):
        bbox_list, img_crop_list, img2bb_trans_list, bb2img_trans_list, flag_flip_list = [], [], [], [], []
        # img_half = int(image_cols / 2)
        # if x_0_min > img_half:

        # if tracking fails, use the previous bbox
        if idx_to_coord_0 is None and idx_to_coord_1 is None:
            bbox_list = self.prev_bbox_list
            flag_flip_list = self.prev_flag_flip_list

            for bbox, flag_flip in zip(bbox_list, flag_flip_list):
                img_crop, img2bb_trans, bb2img_trans, _, _, = augmentation_real(img, bbox, flip=flag_flip)

                img_crop_list.append(img_crop)
                img2bb_trans_list.append(img2bb_trans)
                bb2img_trans_list.append(bb2img_trans)

        elif idx_to_coord_0 is not None and idx_to_coord_1 is not None:
            x_0_min = min(idx_to_coord_0.values(), key=lambda x: x[0])[0]
            x_1_min = min(idx_to_coord_1.values(), key=lambda x: x[0])[0]
            if x_0_min < x_1_min:
                flag_flip = True  # left 0 - right 1 // do flip left side hand
            else:
                flag_flip = False  # right 0 - left 1

            bbox = self.extract_bbox(idx_to_coord_0, image_rows, image_cols)
            img_crop, img2bb_trans, bb2img_trans, _, _, = augmentation_real(img, bbox, flip=flag_flip)
            # cv2.imshow('crop 0', img_crop / 255.)
            # cv2.waitKey(1)
            bbox_list.append(bbox)
            img_crop_list.append(img_crop)
            img2bb_trans_list.append(img2bb_trans)
            bb2img_trans_list.append(bb2img_trans)
            flag_flip_list.append(flag_flip)

            flag_flip = not flag_flip
            bbox = self.extract_bbox(idx_to_coord_1, image_rows, image_cols)
            img_crop, img2bb_trans, bb2img_trans, _, _, = augmentation_real(img, bbox, flip=flag_flip)
            # cv2.imshow('crop 1', img_crop / 255.)
            # cv2.waitKey(1)
            bbox_list.append(bbox)
            img_crop_list.append(img_crop)
            img2bb_trans_list.append(img2bb_trans)
            bb2img_trans_list.append(bb2img_trans)
            flag_flip_list.append(flag_flip)

        elif idx_to_coord_0 is not None:
            hand_side = results.multi_handedness[0].classification[0].label
            if hand_side == "Left":
                flag_flip = True
            else:
                flag_flip = False

            x_0_min = min(idx_to_coord_0.values(), key=lambda x: x[0])[0]

            bbox = self.extract_bbox(idx_to_coord_0, image_rows, image_cols)
            img_crop, img2bb_trans, bb2img_trans, _, _, = augmentation_real(img, bbox, flip=flag_flip)
            # cv2.imshow('crop single', img_crop / 255.)
            # cv2.waitKey(1)
            bbox_list.append(bbox)
            img_crop_list.append(img_crop)
            img2bb_trans_list.append(img2bb_trans)
            bb2img_trans_list.append(bb2img_trans)
            flag_flip_list.append(flag_flip)

        self.prev_bbox_list = copy.deepcopy(bbox_list)
        self.prev_flag_flip_list = copy.deepcopy(flag_flip_list)

        return bbox_list, img_crop_list, img2bb_trans_list, bb2img_trans_list, flag_flip_list


    def extract_bbox(self, idx_to_coord, image_rows, image_cols):
        x_min = min(idx_to_coord.values(), key=lambda x: x[0])[0]
        # x_max = max(idx_to_coord.values(), key=lambda x: x[0])[0]
        y_min = min(idx_to_coord.values(), key=lambda x: x[1])[1]
        # y_max = max(idx_to_coord.values(), key=lambda x: x[1])[1]

        margin = self.crop_size / 4.0
        x_min = max(0, x_min - margin)
        y_min = max(0, y_min - margin)

        if (x_min + self.crop_size) > image_cols:
            x_min = image_cols - self.crop_size
        if (y_min + self.crop_size) > image_rows:
            y_min = image_rows - self.crop_size

        bbox = [x_min, y_min, self.crop_size, self.crop_size]
        return bbox


def main():
    torch.backends.cudnn.benchmark = True
    tracker = HandTracker()
    cam_intrinsic = None
    frame = 1
    while True:
        color = _get_input(frame)
        all_uvd = tracker.run(color)
        ### if required uvd format ##
        mesh_uvd = copy.deepcopy(all_uvd[:cfg.num_vert])       # (778, 3)
        joint_uvd = copy.deepcopy(all_uvd[cfg.num_vert:])       # (21, 3)
        ### if required xyz format ###
        # all_xyz = uvd2xyz(all_uvd, cam_intrinsic)
        _visualize(color, joint_uvd)
        frame += 1



def _get_input(frame):
    ### load image from recorded files ###
    load_filepath = './recorded_files/'

    color = cv2.imread(load_filepath + 'color_%d.png' % frame)
    color = cv2.resize(color, dsize=(256, 256), interpolation=cv2.INTER_CUBIC)

    return color

def _visualize(color, coords_uvd):
    vis = draw_2d_skeleton(color, coords_uvd[cfg.num_vert:])
    vis = cv2.resize(vis, dsize=(416, 416), interpolation=cv2.INTER_CUBIC)
    color = cv2.resize(color, dsize=(416, 416), interpolation=cv2.INTER_CUBIC)
    cv2.imshow("vis", vis)
    cv2.imshow("img", color)
    cv2.waitKey(50)

def uvd2xyz(uvd, K):
    fx, fy, fu, fv = K[0, 0], K[0, 0], K[0, 2], K[1, 2]
    xyz = np.zeros_like(uvd, np.float32)
    xyz[:, 0] = (uvd[:, 0] - fu) * uvd[:, 2] / fx
    xyz[:, 1] = (uvd[:, 1] - fv) * uvd[:, 2] / fy
    xyz[:, 2] = uvd[:, 2]
    return xyz

def xyz2uvd(xyz, K):
    fx, fy, fu, fv = K[0, 0], K[0, 0], K[0, 2], K[1, 2]
    uvd = np.zeros_like(xyz, np.float32)
    uvd[:, 0] = (xyz[:, 0] * fx / xyz[:, 2] + fu)
    uvd[:, 1] = (xyz[:, 1] * fy / xyz[:, 2] + fv)
    uvd[:, 2] = xyz[:, 2]
    return uvd

if __name__ == '__main__':
    main()



