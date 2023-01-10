using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class NoDataReceivedExecption : Exception
{
    public NoDataReceivedExecption(string message) : base(message)
    {

    }
    
}
public class SocketClientManager : MonoBehaviour
{
    Dictionary<int, Matrix4x4> camera_extrinsics = new Dictionary<int, Matrix4x4>();
    Dictionary<int, Matrix4x4> camera_intrinsics = new Dictionary<int, Matrix4x4>();

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
        set
        {
            lock (lock_object)
            {
                isNewHandDataReceived = value;
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
        set
        {
            lock (lock_object)
            {
                isNewObjectDataReceived = value;
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

    /* maxBufferSize
    Currently require 4096 for one hand, 8192 for 2 hands. need to optimize on server side.    
    */
    public void Connect(string serverIP, int serverPort, int maxBufferSize = 8192)
    {
        IsNewHandDataReceived = false;
        IsNewObjectDataReceived = false;
        latestResultData = null;
        queueResultData.Clear();
        client.Connect(serverIP, serverPort);
        client.BeginReceive(OnReceiveData, maxBufferSize);
    }

    public void Disconnect()
    {
        IsNewHandDataReceived = false;
        IsNewObjectDataReceived = false;
        latestResultData = null;
        queueResultData.Clear();
        client.Disconnect();
    }
    public void SendRGBImage(int frameID, Texture2D texture
        , Matrix4x4 intrinsic, Matrix4x4 extrinsic, ImageCompression comp = ImageCompression.None, int jpgQuality = 75)
    {
        ((SocketClient_WiseUI)client).SendRGBImage(frameID, texture, comp, jpgQuality);
        camera_intrinsics[frameID] = intrinsic;
        camera_extrinsics[frameID] = extrinsic;
    }


    /*
     * ���� : �Ʒ� �Լ��� main thread���� ȣ��Ǵ� �Լ��� �ƴ�.
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

            var now = DateTime.Now.ToLocalTime();
            var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            double total_delay = span.TotalSeconds - latestResultData.frameInfo.timestamp_sentFromClient;
            Debug.LogFormat("frameID : {0}, total_delay : {1}, FPS : {2}", latestResultData.frameInfo.frameID, total_delay, 1/total_delay);
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
    public Matrix4x4 GetCameraExtrinsic(int frame_id)
    {
        return camera_extrinsics[frame_id];
    }
    
    public Matrix4x4 GetCameraIntrinsic(int frame_id)
    {
        return camera_intrinsics[frame_id];
    }
    
    public void GetLatestResultData(out ResultDataPackage resultDataPackage)
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


    public void GetOldestResultData(out ResultDataPackage resultDataPackage)
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
