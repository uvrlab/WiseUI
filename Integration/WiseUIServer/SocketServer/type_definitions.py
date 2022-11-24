from datetime import time
import time
import cv2
import numpy as np

class DataType:
    PV = 1
    Depth = 2
    PC = 3
    IMU = 4


class ImageCompression:
    None_ = 0
    JPEG = 1
    PNG = 2


class DataFormat:
    INVALID = -1
    RGBA = 1
    BGRA = 2  # proper to numpy format.
    ARGB = 3
    RGB = 4
    U16 = 5
    U8 = 6
    Float32 = 7


class HoloLens2SensorData:
    def __init__(self, header, data):
        self.data = data
        self.frameID = header['frameID']
        self.timestamp_sentfromClient = header['timestamp']
        self.dataFormat = header['dataFormat']  # U16
    def encode_frame_info(self):
        frame_info = dict()
        frame_info['frameID'] = self.frameID
        frame_info['timestamp_sentFromClient'] = self.timestamp_sentfromClient
        frame_info['timestamp_sentFromServer'] = float(time.time())  # 서버에서 홀로렌즈로 처리 결과를 보낸 시간

        return frame_info

class HoloLens2PVImageData(HoloLens2SensorData):
    def __init__(self, header, raw_data):
        width = header['width']
        height = header['height']
        dataFormat = header['dataFormat']
        dim = get_dimension(dataFormat)
        np_img = np.frombuffer(raw_data, np.uint8).reshape((height, width, dim))
        self.intrinsic = np.zeros((3, 3))
        self.extrinsic = np.zeros((4, 4))

        #cv2.imshow("pvimage", np_img)
        #cv2.waitKey(1)

        super().__init__(header, np_img)
    def encode_frame_info(self):
        return super().encode_frame_info()

class HoloLens2DepthImageData(HoloLens2SensorData):
    def __init__(self, header, raw_data):
        self.extrinsic = np.zeros((4, 4))
        self.width = header['width']
        self.height = header['height']
        self.timestamp_sentfromClient = header['timestamp']

        super().__init__(header, raw_data)

    def encode_frame_info(self):
        return super().encode_frame_info()

class HoloLens2PointCloudData(HoloLens2SensorData):
    def __init__(self, header, data):
        super().__init__(header, data)

    def encode_frame_info(self):
        return super().encode_frame_info()

def get_dimension(dataFormat: DataFormat):
    if dataFormat == DataFormat.RGBA or dataFormat == DataFormat.BGRA \
            or dataFormat == DataFormat.ARGB or dataFormat == DataFormat.Float32:
        return 4
    elif dataFormat == DataFormat.RGB:
        return 3
    elif dataFormat == DataFormat.U16:
        return 2
    elif dataFormat == DataFormat.U8:
        return 1
    else:
        raise (Exception("Invalid DataFormat Error."))
