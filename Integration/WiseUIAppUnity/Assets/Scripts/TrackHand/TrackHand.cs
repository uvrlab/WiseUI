using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class TrackHand : MonoBehaviour
{
    public SocketClientManager tcpClientManager;
    public GameObject handMesh;

     /*
        1. On Awake, instantiate hand structure as child of handMesh(consider as wrist)
        1.1 create sphere on each joints
        1.2 create bar relative on joints position

        2. On Update, update each hand joint mesh transform
        2.1 extract depth of joint[0] (wrist)
        2.2 apply relative depth of each joints
    */

    // Start is called before the first frame update
    public void Awake()
    {
        tcpClientManager = GetComponent<SocketClientManager>();
        handMesh = GameObject.Find("HandMesh").GetComponent<GameObject>();
      

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

            HandDataPackage handData = frameData.handDataPackage;
            
            Debug.LogFormat("handdata joint_0 len : {0},", frameData.handDataPackage.joints.Count);            
            Debug.LogFormat("handdata 0 x : {0},", frameData.handDataPackage.joints[0].u);
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
