using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
    public int imageQulaity = 75;
    public long data_length;
    public int width;
    public int height;
    public int dim;
}


