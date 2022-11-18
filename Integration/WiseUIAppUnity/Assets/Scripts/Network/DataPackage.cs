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
public class ObjectData
{
    public int numObjects;
   
}

[System.Serializable]
public class HandData
{
    public int numJoints;
    public float[,] joints = new float[27, 3];

    //필요한 거 정의
}
[System.Serializable]
public class ResultData
{
    //add to instrinsic 
    //add to extrinsic
    public int frameID;
    public double timestamp_receive;
    public double timestamp_send;
    public HandData handData = new HandData();
    public ObjectData objectData = new ObjectData();
}

