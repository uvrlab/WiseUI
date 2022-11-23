using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using TreeEditor;
using UnityEngine;

public class SocketClientManager : MonoBehaviour
{
    readonly SocketClient client = new SocketClient_WiseUI();
    ResultDataPackage resultData;
    
    readonly object lock_object = new object();

    bool isNewHandDataReceived;
    bool isNewObjectDataReceived;
    
    public bool IsNewHandDataReceived
    {
        get
        {
            lock(lock_object)
            {
                return isNewHandDataReceived;
            }
        }
    }
    public bool IsNewObjectDataReceived
    {
        get
        {
            lock (lock_object)
            {
                return isNewObjectDataReceived;
            }
        }
    }

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
        ((SocketClient_WiseUI)client).SendRGBImage(frameID, texture, comp, jpgQuality); 
    }


    /*
     * 주의 : 아래 함수는 main thread에서 호출되는 함수가 아님.
     */
    public void OnReceiveData(byte[] buffer)
    {
        lock (lock_object)
        {
            isNewHandDataReceived = true;
            isNewObjectDataReceived = true;
            string receivedDataString = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
            resultData = JsonUtility.FromJson<ResultDataPackage>(receivedDataString);
        }

        //GetComponent<TrackHand>().ReceiveHandData(resultData.handData);

        //Debug.LogFormat("{0} {1} {2} {3} {4} {5}",
        //    resultData.objectDataPackage.objects.Count,
        //    resultData.objectDataPackage.objects[1].id,
        //    resultData.objectDataPackage.objects[1].keypoints[0].id,
        //    resultData.objectDataPackage.objects[1].keypoints[0].x,
        //    resultData.objectDataPackage.objects[1].keypoints[0].y,
        //    resultData.objectDataPackage.objects[1].keypoints[0].z
        //    );

    }

    public void GetHandData(out FrameInfo frameInfo, out HandDataPackage handDataPackage)
    {
        lock (lock_object)
        {
            frameInfo = resultData.frameInfo;
            handDataPackage = resultData.handDataPackage;
            isNewHandDataReceived = false;
        }
    }

    public void GetObjectData(out FrameInfo frameInfo, out ObjectDataPackage objectDataPackage)
    {
        lock(lock_object)
        {
            frameInfo = resultData.frameInfo;
            objectDataPackage = resultData.objectDataPackage;
            isNewObjectDataReceived = false;
        }
        
    }

    private void OnDestroy()
    {
        client.Disconnect();
    }
}
