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
        self.timestamp_fromClient = header['timestamp']
        self.dataFormat = header['dataFormat']  # U16


class HoloLens2PVImageData(HoloLens2SensorData):
    def __init__(self, header, raw_data):
        width = header['width']
        height = header['height']
        dataFormat = header['dataFormat']
        dim = GetDimension(dataFormat)
        np_img = np.frombuffer(raw_data, np.uint8).reshape((height, width, dim))
        self.intrinsic = np.zeros((3, 3))
        self.extrinsic = np.zeros((4, 4))

        cv2.imshow("pvimage", np_img)
        cv2.waitKey(1)

        super().__init__(header, np_img)

class HoloLens2DepthImageData(HoloLens2SensorData):
    def __init__(self, header, raw_data):
        self.extrinsic = np.zeros((4, 4))
        self.width = header['width']
        self.height = header['height']

        super().__init__(header, raw_data)

class HoloLens2PointCloudData(HoloLens2SensorData):
    def __init__(self, header, data):
        super().__init__(header, data)

def GetDimension(dataFormat: DataFormat):
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
