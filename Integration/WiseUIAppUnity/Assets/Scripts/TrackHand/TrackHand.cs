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
            // 1. ���� �ֽ� �����͸� �������� ���.(������ frame ���� �� ������ delay ����.)
            tcpClientManager.GetLatestResultData(out frameData);
            
            // 2. ������ ������� �����͸� �������� ���. (delay ���� �� ������ ������ frame ����.)
            //tcpClientManager.GetOldestResultData(out frameData);
            var frameInfo = frameData.frameInfo;
            var handData = frameData.handDataPackage;

            /*
            var joints_0 = handData["joints_0"];
            // joints_0[0~20]['u', 'v', 'd'] 

            if(handData.Count > 1){
                var joints_1 = handData["joints_1"];
            }
            */

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
            //Debug.Log(e); // �� �̻� �޾ƿ� �����Ͱ� ���� �� �߻��ϴ� ����.
        }
    }

 
}
