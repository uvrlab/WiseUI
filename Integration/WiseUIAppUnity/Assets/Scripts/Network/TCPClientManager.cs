using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class TCPClientManager : MonoBehaviour
{
    TCPClient_WiseUI client = new TCPClient_WiseUI();

    public void Connect(string serverIP, int serverPort)
    {
        client.Connect(serverIP, serverPort);
        client.BeginReceive(OnReceiveData);
    }

    public void Disconnect()
    {
        client.Disconnect();
    }
    public void SendRGBImage(int frameID, Texture2D texture, ImageCompression comp = ImageCompression.None, int jpgQuality = 75)
    {
        client.SendRGBImage(frameID, texture, comp, jpgQuality); 
    }


    public void OnReceiveData(byte[] buffer)
    {
        var result = Encoding.UTF8.GetString(buffer);
        Debug.Log(result);
        //Debug.LogFormat("Received data : {0}", header.dataType);
    }
}
