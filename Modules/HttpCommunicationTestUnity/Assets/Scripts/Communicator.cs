using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using static SystemManager; //삭제 예저ㅏㅇ
using System.IO; //삭제 예저ㅏㅇ
using System.Threading.Tasks;
using UnityEngine.UI;

public class Communicator : MonoBehaviour
{
    public DataSender sender;
    public SystemManager mSystemManager;
    public Text mText;

    //void OnApplicationQuit() {
    bool WantsToQuit() {
        //Application.CancelQuit();
        ////Device & Map store
        string addr2 = mSystemManager.AppData.Address + "/Store?keyword=DeviceDisconnect&id=0&src=" + mSystemManager.User.UserName;
        string msg2 = mSystemManager.User.UserName + "," + mSystemManager.User.MapName;
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg2);

        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();

        string addr3 = mSystemManager.AppData.Address + "/Disconnect?src=" + mSystemManager.User.UserName + "&type=device";
        UnityWebRequest request3 = new UnityWebRequest(addr3);
        request3.method = "POST";
        //UploadHandlerRaw uH3 = new UploadHandlerRaw(bdata3);
        //uH3.contentType = "application/json";
        //request3.uploadHandler = uH3;
        request3.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res3 = request3.SendWebRequest();

        while (!request.downloadHandler.isDone)//&& !request3.downloadHandler.isDone
        {
            continue;
        }

#if !UNITY_EDITOR
        System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        return true;
    }

    // Start is called before the first frame update
    void Start()
    {

        try
        {
            ////알람 서버에 등록
            ApplicationData appData = mSystemManager.AppData;
            UdpAsyncHandler.Instance.UdpSocketBegin(appData.UdpAddres, appData.UdpPort, appData.LocalPort);
            string[] keywords = mSystemManager.User.ReceiveKeywords.Split(',');
            for (int i = 0; i < keywords.Length; i += 2)
            {
                UdpAsyncHandler.Instance.Send(mSystemManager.User.UserName, keywords[i], "connect", keywords[i + 1]);
            }

            ////데이터 서버에 등록
            InitConnectData data = mSystemManager.GetConnectData();
            string msg = JsonUtility.ToJson(data);
            byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

            UnityWebRequest request = new UnityWebRequest(mSystemManager.AppData.Address + "/Connect?port=40003");
            request.method = "POST";
            UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
            request.downloadHandler = new DownloadHandlerBuffer();
            UnityWebRequestAsyncOperation res = request.SendWebRequest();

            while (!request.downloadHandler.isDone)
            {
                continue;
            }

            ////Device & Map store, 슬램 서버에 접속
            var timeSpan = DateTime.UtcNow - mSystemManager.StartTime;
            double ts = timeSpan.TotalMilliseconds;
            
            //string addr2 = SystemManager.Instance.AppData.Address + "/Store?keyword=DeviceConnect&id=0&src=" + SystemManager.Instance.User.UserName;
            string msg2 = mSystemManager.User.UserName + "," + mSystemManager.User.MapName;
            byte[] bdatab = System.Text.Encoding.UTF8.GetBytes(msg2);
            float[] fdataa = mSystemManager.IntrinsicData;
            int nByte = 5;
            byte[] bdata2 = new byte[nByte + fdataa.Length * 4 + bdatab.Length];
            bdata2[fdataa.Length * 4] = mSystemManager.User.ModeMapping ? (byte)1 : (byte)0;
            bdata2[fdataa.Length * 4 + 1] = mSystemManager.User.ModeTracking ? (byte)1 : (byte)0;
            bdata2[fdataa.Length * 4 + 2] = mSystemManager.User.UseGyro ? (byte)1 : (byte)0;
            bdata2[fdataa.Length * 4 + 3] = mSystemManager.User.bSaveTrajectory ? (byte)1 : (byte)0;
            bdata2[fdataa.Length * 4 + 4] = mSystemManager.User.ModeAsyncQualityTest ? (byte)1 : (byte)0;
            Buffer.BlockCopy(fdataa, 0, bdata2, 0, fdataa.Length * 4);
            Buffer.BlockCopy(bdatab, 0, bdata2, fdataa.Length * 4 + nByte, bdatab.Length);

            UdpData deviceConnectData = new UdpData("DeviceConnect", mSystemManager.User.UserName, 0, bdata2, ts);
            StartCoroutine(sender.SendData(deviceConnectData));
            //mText.text = "success connect!!! " + mSystemManager.FocalLengthX+" "+mSystemManager.FocalLengthY;
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }

        mSystemManager.bConnect = true;
        mSystemManager.bStart = true;

        //종료 이벤트 등록
        Application.wantsToQuit += WantsToQuit;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

}
