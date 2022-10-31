using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigurationManager : MonoBehaviour
{

    public string hostIP;
    public int port;

    public PVCameraType pvCameraType;



    string default_hostIP = "169.254.176.210";
    int default_port = 9091;

    PVCameraType defaultPVCameraType = PVCameraType.r640x360xf30;

    public void Load()
    {
        if (PlayerPrefs.HasKey("hostIP"))
            hostIP = PlayerPrefs.GetString("hostIP");
        else
            hostIP = default_hostIP;

        if (PlayerPrefs.HasKey("port"))
            port = PlayerPrefs.GetInt("port");
        else
            port = default_port;

        if (PlayerPrefs.HasKey("pvCameraType"))
            pvCameraType = (PVCameraType)PlayerPrefs.GetInt("pvCameraType");
        
        else
            pvCameraType = defaultPVCameraType;
    }

    public void Reset()
    {
        PlayerPrefs.DeleteKey("hostIP");
        PlayerPrefs.DeleteKey("port");
        PlayerPrefs.DeleteKey("pvCameraType");

    }
    public void Save(string hostIP, int port, PVCameraType cameraType)
    {
        PlayerPrefs.SetString("hostIP", hostIP);
        PlayerPrefs.SetInt("port", port);
        PlayerPrefs.SetInt("pvCameraType", (int)cameraType);

        PlayerPrefs.Save();
    }


    private static ConfigurationManager instance;

    public static ConfigurationManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = GameObject.Find("ConfigurationManager");
                if (obj == null)
                {
                    obj = new GameObject("ConfigurationManager");
                    instance = obj.AddComponent<ConfigurationManager>();
                }
                else
                {
                    instance = obj.GetComponent<ConfigurationManager>();
                }
                DontDestroyOnLoad(obj);
            }

            return instance;
        }
    }

}
