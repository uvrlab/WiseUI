using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            var frameInfo = frameData.frameInfo;
            var handData = frameData.handDataPackage;

            /// <summary>
            /// Test
            /// </summary>
            var finger_tip = GameObject.Find("FingerTip").transform;
            
            var matrix_finger_tip = finger_tip.localToWorldMatrix;
            
            var extrinsic = tcpClientManager.GetCameraExtrinsic(frameInfo.frameID);
            var intrinsic = tcpClientManager.GetCameraIntrinsic(frameInfo.frameID);
            var x_2d = intrinsic * extrinsic * matrix_finger_tip;
            
            Debug.Log(Screen.width / 2);
            //x_2d = I*E*X_3D



        }
        catch (NoDataReceivedExecption e)
        {
            //Debug.Log(e); // 더 이상 받아올 데이터가 없을 때 발생하는 예외.
        }
    }

 
}
