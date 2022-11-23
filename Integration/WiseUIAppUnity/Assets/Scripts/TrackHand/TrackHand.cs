using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackHand : MonoBehaviour
{
    public SocketClientManager tcpClientManager;
    // Start is called before the first frame update
    void Awake()
    {
        tcpClientManager = GetComponent<SocketClientManager>();
    }


    void Update()
    {
        ResultDataPackage frameData;

        // 1. 가장 최신 데이터를 가져오는 방법.(누락된 frame 생길 수 있지만 delay 없음.)
        if (tcpClientManager.IsNewHandDataReceived)
        {
            tcpClientManager.GetLatestFrameData(out frameData);

            var frameInfo = frameData.frameInfo;
            var handData = frameData.handDataPackage;

            var now = DateTime.Now.ToLocalTime();
            var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            double total_delay = span.TotalSeconds - frameInfo.timestamp_sentFromClient;
            Debug.LogFormat("frameID : {0}, total_delay {1}, ", frameInfo.frameID, total_delay);


            tcpClientManager.IsNewHandDataReceived = false;
        }


        //// 2. 받아온 frame순서대로 데이터를 가져오는 방법. (delay 생길 수 있지만 누락된 frame 없음.)
        //try
        //{
        //    tcpClientManager.GetNextFrameData(out frameData);
        //    var frameInfo = frameData.frameInfo;
        //    var handData = frameData.handDataPackage;

        //    var now = DateTime.Now.ToLocalTime();
        //    var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
        //    double total_delay = span.TotalSeconds - frameInfo.timestamp_sentFromClient;
        //    Debug.LogFormat("frameID : {0}, total_delay {1}, ", frameInfo.frameID, total_delay);

        //    //tcpClientManager.SetHandDataReceived(false);
        //}
        //catch (Exception e)
        //{
        //    Debug.Log(e); // 더이상 받아올 데이터가 없을 때 발생하는 예외.
        //}
    }
}
