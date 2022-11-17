using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ImageFormat
{
    INVALID = -1,
    RGBA = 1,
    BGRA = 2, //proper to numpy format.
    ARGB = 3,
    RGB = 4,
    U16 = 5,
    U8 = 6,
    Float32 = 7
}

public enum DataType
{
    PV = 1,
    Depth = 2,
    PointCloud = 3,
    IMU = 4,
    Object = 5,
    Hand = 6,
    SLAM = 7
}
public enum ImageCompression
{
    None = 0,
    JPEG = 1,
    PNG = 2
}

[System.Serializable]
public class RGBImageHeader
{
    public int frameID = -1;
    public double timestamp = -1;
    public DataType dataType;
    public ImageCompression dataCompressionType = ImageCompression.None;
    public ImageFormat imageFormat = ImageFormat.INVALID;
    public int imageQulaity = 100;
    public long data_length;
    public int width;
    public int height;
}

[System.Serializable]
public class ObjectData : ResultDataHeader
{
    public int numObjects;
    public float[] data;
}

[System.Serializable]
public class HandTrackingData : ResultDataHeader
{

    //필요한 거 정의
}
[System.Serializable]
public class ResultDataHeader
{
    public int frameID = -1;
    public double timestamp = -1;
    public DataType dataType;
    public int dataSize;
}

