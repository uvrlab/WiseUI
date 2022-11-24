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


class ImageFormat:
    INVALID = -1
    RGBA = 1
    BGRA = 2  # proper to numpy format.
    ARGB = 3
    RGB = 4
    U16 = 5
    U8 = 6
    Float32 = 7


class HoloLens2SensorData:
    def __init__(self, header, image):
        self.frameID = header['frameID']
        self.Intrinsic = np.zeros((3, 3))
        self.Extrinsic = np.zeros((4, 4))

        self.width = header['width']
        self.height = header['height']
        self.imageFormat = header['imageFormat']
        self.image = image

        # not used
        self.dataCompressionType = header['dataCompressionType']
        # not used
        self.imageQulaity = header['imageQulaity']

