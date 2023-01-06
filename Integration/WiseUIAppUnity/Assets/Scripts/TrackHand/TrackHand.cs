using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class TrackHand : MonoBehaviour
{
    public SocketClientManager tcpClientManager;
    
    // Start is called before the first frame update
    public void Awake()
    {
        tcpClientManager = GetComponent<SocketClientManager>();
    }

    
    public void Update()
    {
        try
        {
            ResultDataPackage frameData;
            // 1. 가장 최신 데이터를 가져오는 방법.(누락된 frame 생길 수 있지만 delay 없음.)
            tcpClientManager.GetLatestResultData(out frameData);
            
            // 2. 도착한 순서대로 데이터를 가져오는 방법. (delay 생길 수 있지만 누락된 frame 없음.)
            //tcpClientManager.GetOldestResultData(out frameData);
            // var frameInfo = frameData.frameInfo;
            // var handData = frameData.handDataPackage;

            // Debug.LogFormat("frameID : {0}, handdata : {1},", frameData.frameInfo, frameData.handDataPackage.joints.GetType());

            // HandDataPackage handData = frameData.handDataPackage;
            Debug.LogFormat("handdata : {0},", frameData.handDataPackage.joints.GetType());
            Debug.LogFormat("handdata len : {0},", frameData.handDataPackage.joints.Count);

            // string str = handData.joints[0].x.ToString();
            // Console.WriteLine(str); 
            // var joints_0 = handData["joints_0"];
            // // joints_0[0~20]['u', 'v', 'd'] 

            // if(handData.Count > 1){
            //     var joints_1 = handData["joints_1"];
            // }


            /// <summary>
            /// Test
            /// </summary>
            //var finger_tip = GameObject.Find("FingerTip").transform;
            //var matrix_finger_tip = finger_tip.localToWorldMatrix;
            //var image = tcpClientManager.GetImage(frameInfo.frameID);
            //var extrinsic = tcpClientManager.GetCameraExtrinsic(frameInfo.frameID);
            //var intrinsic = tcpClientManager.GetCameraIntrinsic(frameInfo.frameID);
            //var x_2d = intrinsic * extrinsic * matrix_finger_tip;
            
            //x_2d = I*E*X_3D



        }
        catch (NoDataReceivedExecption e)
        {
            Debug.Log(e); // 더 이상 받아올 데이터가 없을 때 발생하는 예외.
        }
    }

 
}
