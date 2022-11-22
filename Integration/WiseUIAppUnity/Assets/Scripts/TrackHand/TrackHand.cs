using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackHand : MonoBehaviour
{
    public TCPClientManager tcpClientManager;
    // Start is called before the first frame update
    void Awake()
    {
        tcpClientManager = GetComponent<TCPClientManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(tcpClientManager.IsNewHandDataReceived)
        {
            FrameInfo frameInfo;
            HandDataPackage handData;
            tcpClientManager.GetHandData(out frameInfo, out handData);

            var now = DateTime.Now.ToLocalTime();
            var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            double total_delay = span.TotalSeconds - frameInfo.timestamp_sentFromClient;
            Debug.LogFormat("frameID : {0}, total_delay {1}, ", frameInfo.frameID, total_delay);
        }
    }
}
