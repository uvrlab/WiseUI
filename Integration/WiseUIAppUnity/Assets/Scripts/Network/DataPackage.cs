using Microsoft.MixedReality.Toolkit.Utilities;
using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
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
public class Joint
{
    public int id;
    public float x, y, z;
    //public float q1, q2, q3, q4; 
}


[System.Serializable]
public class HandDataPackage
{
    //필요한 거 정의
     public List<Joint> joints = new List<Joint>();
}

[System.Serializable]
public class Keypoint
{
    public int id;
    public float x, y, z;
}
[System.Serializable]
public class ObjectInfo
{
    public List<Keypoint> keypoints = new List<Keypoint>();
    public int id;
}


[System.Serializable]
public class ObjectDataPackage
{
    public List<ObjectInfo> objects = new List<ObjectInfo>();
}
[System.Serializable]
public class FrameInfo
{
    //add to instrinsic 
    //add to extrinsic
    public int frameID;
    /// <summary>
    /// //홀로렌즈에서 이미지를 서버로 보낸 시점
    /// </summary>
    public double timestamp_t1;

    /// <summary>
    ///  //서버에서 처리 결과값을 홀로렌즈로 보낸 시점
    /// </summary>
    public double timestamp_t2;
}
[System.Serializable]
public class ResultDataPackage
{
    public FrameInfo frameInfo = new FrameInfo();
    public ObjectDataPackage objectDataPackage = new ObjectDataPackage();
    public HandDataPackage handDataPackage = new HandDataPackage();
}

