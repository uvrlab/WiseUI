using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;
using static TCPClient;

public class TCPClient_WiseUI : TCPClient
{
    
    public void SendRGBImage(int frameID, Texture2D texture, ImageCompression comp = ImageCompression.None, int jpgQuality = 75)
    {
        var now = DateTime.Now.ToLocalTime();
        var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

        var header = new RGBImageHeader();

        header.frameID = frameID;
        header.width = texture.width;
        header.height = texture.height;
        header.dataType = DataType.PV;
        header.imageFormat = ConvertTextureFormat2ImageFormat(texture.format);
        header.timestamp = span.TotalSeconds;
        header.dataCompressionType = comp;
        header.imageQulaity = jpgQuality;

        byte[] bImage = null;

        if (comp == ImageCompression.None)
            bImage = texture.GetRawTextureData();

        if (comp == ImageCompression.JPEG)
            bImage = texture.EncodeToJPG(jpgQuality);

        header.data_length = bImage.Length;

        string sHeader = JsonUtility.ToJson(header);
        byte[] bHeader = Encoding.ASCII.GetBytes(sHeader);
        byte[] bHeaderSize = BitConverter.GetBytes(bHeader.Length);

        int contentSize = 4 + bHeader.Length + bImage.Length;
        byte[] bContentSizeData = BitConverter.GetBytes(contentSize);

        int totalSize = 4 + contentSize;
        byte[] bTotal = new byte[totalSize];

        System.Buffer.BlockCopy(bContentSizeData, 0, bTotal, 0, 4);
        System.Buffer.BlockCopy(bHeaderSize, 0, bTotal, 4, 4);
        System.Buffer.BlockCopy(bHeader, 0, bTotal, 4 + 4, bHeader.Length);
        System.Buffer.BlockCopy(bImage, 0, bTotal, 4 + 4 + bHeader.Length, bImage.Length);

        // Debug.LogFormat("Header data size : {0}, Image data size : {1}, Total data size : {2}", bHeader.Length, bImage.Length, totalSize);

        Send(bTotal);
    }
    

    public void SendEncodedData(string message)
    {
        var content = Encoding.UTF8.GetBytes(message);

        int totalSize = 4 + content.Length;
        var contentSize = BitConverter.GetBytes(content.Length);

        byte[] buffer = new byte[totalSize];
        System.Buffer.BlockCopy(contentSize, 0, buffer, 0, 4);
        System.Buffer.BlockCopy(content, 0, buffer, 4, content.Length);

        base.Send(buffer);
    }
    ImageFormat ConvertTextureFormat2ImageFormat(TextureFormat textureFormat)
    {
        switch (textureFormat)
        {
            case TextureFormat.RGBA32:
                return ImageFormat.RGBA;
            case TextureFormat.BGRA32:
                return ImageFormat.BGRA;
            case TextureFormat.ARGB32:
                return ImageFormat.ARGB;
            case TextureFormat.RGB24:
                return ImageFormat.RGB;
        }
        return ImageFormat.INVALID;
    }

    
    //void OnHeaderReceive(IAsyncResult aResult)
    //{
    //    byte[] receivedData = (byte[])aResult.AsyncState;

    //    ResultDataHeader resultDataHeader = new ResultDataHeader();
    //    string receivedDataString = Encoding.ASCII.GetString(receivedData, 0, receivedData.Length);
    //    JsonUtility.FromJsonOverwrite(receivedDataString, resultDataHeader);

    //    var frameID = resultDataHeader.frameID;
    //    var timestamp = resultDataHeader.timestamp;
    //    var dataType = resultDataHeader.dataType;

    //    if (dataType == DataType.Object)
    //    {
    //        var dataSize = Marshal.SizeOf(typeof(ObjectData));
    //        receiveBuffer = new byte[(long)receiveBufferSize];
    //        socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEP, new AsyncCallback(OnObjectDataReceive), receiveBuffer);
    //    }
    //    else if (dataType == DataType.Hand)
    //    {
    //        var dataSize = Marshal.SizeOf(typeof(HandTrackingData));
    //        receiveBuffer = new byte[(long)receiveBufferSize];
    //        socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEP, new AsyncCallback(OnHandDataReceive), receiveBuffer);
    //    }
    //}

    //void OnObjectDataReceive(IAsyncResult aResult)
    //{
    //    byte[] receivedData = (byte[])aResult.AsyncState;
    //    ObjectData resultData = new ObjectData();
    //    string receivedDataString = Encoding.ASCII.GetString(receivedData, 0, receivedData.Length);
    //    JsonUtility.FromJsonOverwrite(receivedDataString, resultData);
    //}
    
    //void OnHandDataReceive(IAsyncResult aResult)
    //{
    //    byte[] receivedData = (byte[])aResult.AsyncState;
    //    HandTrackingData resultData = new HandTrackingData();
    //    string receivedDataString = Encoding.ASCII.GetString(receivedData, 0, receivedData.Length);
    //    JsonUtility.FromJsonOverwrite(receivedDataString, resultData);
    //}
    
    public override void Disconnect()
    {
        SendEncodedData("#Disconnect#");
        Thread.Sleep(1000);
        base.Disconnect();
    }
    
}
