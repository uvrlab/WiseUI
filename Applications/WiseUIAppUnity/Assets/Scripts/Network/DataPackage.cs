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
    PC = 3,
    Sensor = 4
}
public enum ImageCompression
{
    None = 0,
    JPEG = 1,
    PNG = 2
}


[System.Serializable]
public class HL2StreamHeaderInfo
{
    public int frameID = -1;
    public double timestamp = -1;
    public DataType dataType;
    public ImageCompression dataCompressionType = ImageCompression.None;
    public ImageFormat imageFormat = ImageFormat.INVALID;
    public int imageQulaity = 75;
    public long data_length;
    public int width;
    public int height;
    //public int dim;
}


