using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using TreeEditor;
using UnityEngine;

public class NoDataReceivedExecption : Exception
{
    public NoDataReceivedExecption(string message) : base(message)
    {

    }
    
}
public class SocketClientManager : MonoBehaviour
{
    readonly SocketClient client = new SocketClient_WiseUI();
    
    ResultDataPackage latestResultData;
    List<ResultDataPackage> queueResultData = new List<ResultDataPackage>();
    
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

    public void SetHandDataReceived(bool value)
    {
        lock (lock_object)
        {
            isNewHandDataReceived = value;
        }
    }

    public void Connect(string serverIP, int serverPort, int maxBufferSize = 4096)
    {
        client.Connect(serverIP, serverPort);
        client.BeginReceive(OnReceiveData, maxBufferSize);
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
            latestResultData = JsonUtility.FromJson<ResultDataPackage>(receivedDataString);
            queueResultData.Add(latestResultData);
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

    public void GetLatestFrameData(out ResultDataPackage resultDataPackage)
    {
        lock (lock_object)
        {
            if (isNewHandDataReceived)
            {
                resultDataPackage = latestResultData;
                isNewHandDataReceived = false;
            }
            else
            {
                throw new NoDataReceivedExecption("No new data received.");
            }
        }
    }


    public void GetNextFrameData(out ResultDataPackage resultDataPackage)
    {
        lock (lock_object)
        {
            if (queueResultData.Count > 1)
            {
                resultDataPackage = queueResultData[1];
                queueResultData.RemoveAt(0);
            }
            else
            {
                throw new NoDataReceivedExecption("No new data received.");
            }
        }
    }

    private void OnDestroy()
    {
        client.Disconnect();
    }
}
