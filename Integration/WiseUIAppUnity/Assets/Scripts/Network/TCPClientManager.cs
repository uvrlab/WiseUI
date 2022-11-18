using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class TCPClientManager : MonoBehaviour
{
    readonly TCPClient client = new TCPClient_WiseUI();

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
        ((TCPClient_WiseUI)client).SendRGBImage(frameID, texture, comp, jpgQuality); 
    }


    public void OnReceiveData(byte[] buffer)
    {
        string receivedDataString = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
        //Debug.Log(receivedDataString);

        ResultData resultData = new ResultData();
        JsonUtility.FromJsonOverwrite(receivedDataString, resultData);

        GetComponent<TrackHand>().ReceiveHandData(resultData.handData);

        Debug.LogFormat("{0}\n{1}\n{2}\n{3}\n{4}\n{5}", resultData.frameID, resultData.timestamp_receive, resultData.timestamp_send, resultData.handData.numJoints, resultData.handData.joints.Length, resultData.handData.joints[0,1]);

    }

    private void OnDestroy()
    {
        client.Disconnect();
    }
}
