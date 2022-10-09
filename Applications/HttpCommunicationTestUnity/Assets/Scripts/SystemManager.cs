using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CameraParam
{
    public string name;
    public float fx;
    public float fy;
    public float cx;
    public float cy;
    public float d1;
    public float d2;
    public float d3;
    public float d4;
    public float d5;
    public float w;
    public float h;
}
public class UserData
{
    public int numCameraParam;
    public int numDataset;
    public int numDatasetFileName;
    public string UserName;
    public string MapName;
    public string SendKeywords;
    public string ReceiveKeywords;
    public string Experiments;
    public bool ModeMapping;
    public bool ModeTracking;
    public bool ModeAsyncQualityTest;
    public bool UseCamera;
    public bool UseGyro;
    public bool UseAccelerometer;
    public bool bSaveTrajectory;
    public bool bVisualizeFrame;
    //public bool bShowLog;
}

public class ApplicationData
{
    public string Address;
    public string UdpAddres;
    public int UdpPort;
    public int LocalPort;
    public int JpegQuality;
    public int numPyramids;
    public int numFeatures;
    public int numSkipFrames;
    public int numLocalMapPoints;
    public int numLocalKeyFrames;
    public string strBoW_database;
}

public class InitConnectData
{
    public InitConnectData() { }
    public InitConnectData(string _userID, string _mapName, string _sendkeyword, bool _bMapping, bool _bGyro, bool _bManager, bool _bDeviceTracking, int _skip,
        float _fx, float _fy, float _cx, float _cy,
        float _d1, float _d2, float _d3, float _d4, int _w, int _h)
    {
        userID = _userID;
        mapName = _mapName;
        bMapping = _bMapping;
        bDeviceTracking = _bDeviceTracking;
        bGyro = _bGyro;

        bManager = _bManager;

        fx = _fx;
        fy = _fy;
        cx = _cx;
        cy = _cy;
        d1 = _d1;
        d2 = _d2;
        d3 = _d3;
        d4 = _d4;
        w = _w;
        h = _h;
        type1 = "device";
        type2 = "NONE";
        //생성할 키워드
        keyword = _sendkeyword;//,Map,
        src = userID;
        capacity = 33 / _skip + 1;
    }
    public string type1, type2, keyword, src;
    public string userID, mapName;
    public float fx, fy, cx, cy;
    public float d1, d2, d3, d4;
    public int w, h;
    public bool bMapping, bGyro, bManager, bDeviceTracking;
    public int capacity;
}

public class SystemManager : MonoBehaviour
{
    public Text mText;
    void Awake()
    {
        
        StartTime = new DateTime(2022, 1, 1, 0, 0, 0);

        //var dirPath = Application.persistentDataPath + "/../../../../Download/ARFoundation/data";
        var dirPath = Application.persistentDataPath + "/data";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        try
        {
            Debug.Log(Application.persistentDataPath);
            string strIntrinsics = File.ReadAllText(dirPath + "/CameraIntrinsics.json");
            CamParam = JsonUtility.FromJson<CameraParam>(strIntrinsics);
        }
        catch (Exception)
        {
            CamParam = new CameraParam();
            CamParam.name = "S20+_Camera_Portrait";
            CamParam.fx = 476.6926f;
            CamParam.fy = 485.7888f;
            CamParam.cx = 328.5845f;
            CamParam.cy = 172.9118f;
            CamParam.d1 = 0.0919f;
            CamParam.d2 = -0.0314f;
            CamParam.d3 = -0.0242f;
            CamParam.d4 = 0.0023f;
            CamParam.d5 = 0.0f;
            CamParam.w = 640f;
            CamParam.h = 360f;
            File.WriteAllText(dirPath + "/CameraIntrinsics.json", JsonUtility.ToJson(CamParam));
        }

        try
        {
            string strAddData = File.ReadAllText(dirPath + "/UserData.json");
            User = JsonUtility.FromJson<UserData>(strAddData);

            //////
            //bMapping = appData.bMapping;
            //bDeviceTracking = appData.bTracking;
            //bGyro = appData.bGyro;
            //bAcc = appData.bAcc;
    
        }
        catch (Exception e)
        {
            User = new UserData();
            User.numCameraParam = 0;
            User.numDataset = 0;
            User.numDatasetFileName = 0;
            User.UserName = "zkrkwleks20a";
            User.MapName = "TestMap";
            User.SendKeywords = "TestImage,ARFoundationMPs,Image,Gyro,Accelerometer,DeviceConnect,DeviceDisconnect,DevicePosition,ContentGeneration,VO.SELECTION";
            User.ReceiveKeywords = "TestImageReturn,single,ReferenceFrame,single,ObjectDetection,single,PlaneLine,single,LocalContent,single";
            User.Experiments = "ReferenceFrame,Tracking,Content,ObjectDetection,Segmentation";
            User.ModeAsyncQualityTest = false;
            User.ModeMapping = true;
            File.WriteAllText(dirPath + "/UserData.json", JsonUtility.ToJson(User));
            mText.text = e.ToString();
        }

        try
        {
            string strAddData = File.ReadAllText(dirPath + "/AppData.json");
            AppData = JsonUtility.FromJson<ApplicationData>(strAddData);
        }
        catch (Exception)
        {
            AppData = new ApplicationData();
            AppData.Address = "http://143.248.6.143:35005";
            AppData.UdpAddres = "143.248.6.143";
            AppData.UdpPort = 35001;
            AppData.LocalPort = 40003;
            AppData.JpegQuality = 50;
            AppData.numSkipFrames = 3;
            AppData.numPyramids = 4;
            AppData.numFeatures = 800;
            AppData.numLocalMapPoints = 600;
            AppData.numLocalKeyFrames = 50;
            File.WriteAllText(dirPath + "/AppData.json", JsonUtility.ToJson(AppData));
        }

        IntrinsicData = new float[13];
        int nidx = 0;
        IntrinsicData[nidx++] = (float)CamParam.w;
        IntrinsicData[nidx++] = (float)CamParam.h;
        IntrinsicData[nidx++] = CamParam.fx;
        IntrinsicData[nidx++] = CamParam.fy;
        IntrinsicData[nidx++] = CamParam.cx;
        IntrinsicData[nidx++] = CamParam.cy;
        IntrinsicData[nidx++] = CamParam.d1;
        IntrinsicData[nidx++] = CamParam.d2;
        IntrinsicData[nidx++] = CamParam.d3;
        IntrinsicData[nidx++] = CamParam.d4;
        IntrinsicData[nidx++] = CamParam.d5;
        IntrinsicData[nidx++] = AppData.JpegQuality;
        IntrinsicData[nidx++] = AppData.numSkipFrames;
    }

    void Start()
    {
        enabled = false;        
    }
    void Update()
    {
        
    }

    
    /// <summary>
    /// 알림 서버에 내가 받을 키워드를 알림
    /// UdpConnect와 UdpDisconnect 참조
    /// 모든 키워드를 받을지 아니면 특정 아이디만 받을지 선택이 가능함. 근데 이것을 없앨까 생각중.
    /// </summary>

    public InitConnectData GetConnectData()
    {
        return new InitConnectData(User.UserName, User.MapName, User.SendKeywords, User.ModeMapping, User.UseGyro, bManager, User.ModeTracking, AppData.numSkipFrames, 
            CamParam.fx, CamParam.fy, CamParam.cx, CamParam.cy, CamParam.d1, CamParam.d2, CamParam.d3, CamParam.d4, (int)CamParam.w, (int)CamParam.h);
    }
    [HideInInspector]
    public DateTime StartTime;
    [HideInInspector]
    public float[] IntrinsicData;
    [HideInInspector]
    public int ImageWidth;
    [HideInInspector]
    public int ImageHeight;
    [HideInInspector]
    public float FocalLengthX;
    [HideInInspector]
    public float FocalLengthY;
    [HideInInspector]
    public float PrincipalPointX;
    [HideInInspector]
    public float PrincipalPointY;
    [HideInInspector]
    public Matrix3x3 K;
    [HideInInspector]
    public float DisplayScale = 1f;
    [HideInInspector]
    public bool bConnect = false;
    [HideInInspector]
    public bool bStart = false;
    [HideInInspector]
    public bool bManager = false;
    [HideInInspector]
    public int SensorSpeed = 0;
    [HideInInspector]
    public bool UseAccelerometer = false;
    [HideInInspector]
    public UserData User;
    [HideInInspector]
    public ApplicationData AppData;
    [HideInInspector]
    public CameraParam CamParam;

    [HideInInspector]
    public string[] MapNameList;
    [HideInInspector]
    public String[] DataLists;
    [HideInInspector]
    public String[] FileLists;
    [HideInInspector]
    public List<string> Trajectory;

    //public ProcessTime ReferenceTime, TrackingTime, ContentGenerationTime, JpegTime;

}
