using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SensorStream;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class UIManager : MonoBehaviour
{
    // Modules
    public HoloLens2PVCameraReader pvCameraReader;
    public TCPClient tcpClient;
    
    public Interactable confButton;

    // Title UI
    public TextMeshPro stateMessage;
    public Interactable closeButton;


    // TCP UI
    public Interactable connectButton;
    public MRTKTMPInputField hostIPField, portField;


    //Capture UI
    public Interactable startCaptureButton;
    public InteractableToggleCollection pvToggleCollection;
    
    //Camera Image planes
    public GameObject images;
    public GameObject pvImagePlane;
    Coroutine imagePlaneUpdateHandle;


    private void Awake()
    {
        pvCameraReader = GameObject.Find("Runnner").GetComponent<HoloLens2PVCameraReader>();
        tcpClient = GameObject.Find("Runnner").GetComponent<TCPClient>();
        
        confButton = transform.Find("Setting").GetComponent<Interactable>();
        confButton.OnClick.AddListener(OnConfigurationButtonClick);
        images = transform.Find("Images").gameObject;
        pvImagePlane = transform.Find("Images/PVImagePlane").gameObject;

        //transform.Find("Pannel").gameObject.SetActive(false);

        //Title state
        stateMessage = transform.Find("Pannel/TitleBar/Title").GetComponent<TextMeshPro>();
        closeButton = transform.Find("Pannel/TitleBar/TitleButton/Close").GetComponent<Interactable>();
        closeButton.OnClick.AddListener(CloseButtonClick);

        //TCP Connect
        hostIPField = transform.Find("Pannel/HostAddress/Host IP").GetComponent<MRTKTMPInputField>();
        portField = transform.Find("Pannel/HostAddress/Port").GetComponent<MRTKTMPInputField>();
        connectButton = transform.Find("Pannel/Connect").GetComponent<Interactable>();
        connectButton.OnClick.AddListener(ConnectButtonClick);


        //PV sensor resolution.
        pvToggleCollection = transform.Find("Pannel/PVSensorGroup").GetComponent<InteractableToggleCollection>();
        startCaptureButton = transform.Find("Pannel/StartCapture").GetComponent<Interactable>();
        startCaptureButton.OnClick.AddListener(OnStartCaptureButtonClick);
        
    }
    private void Start()
    {
        images.SetActive(false);
    }

    void OnConfigurationButtonClick()
    {
        ConfigurationManager.Instance.Load();
        hostIPField.text = ConfigurationManager.Instance.hostIP;
        portField.text = ConfigurationManager.Instance.port.ToString();
        pvToggleCollection.SetSelection((int)ConfigurationManager.Instance.pvCameraType);
    }
    
    void CloseButtonClick()
    {
        string ip = hostIPField.text;
        int port = int.Parse(portField.text);
        PVCameraType pVCameraType = (PVCameraType)pvToggleCollection.CurrentIndex;
        ConfigurationManager.Instance.Save(ip, port, pVCameraType);
        Debug.Log(string.Format("{0}, {1},{2}", ip, port, pVCameraType.ToString()));

    }

    void ConnectButtonClick()
    {
        Debug.Log(hostIPField.text);

        if (!connectButton.IsToggled)
        {
            tcpClient.Disconnect();
            stateMessage.text = string.Format("Success to disconnect");
            return;
        }

        try
        {
            string ip = hostIPField.text;
            int port = int.Parse(portField.text);

            tcpClient.Connect(ip, port);
            stateMessage.color = Color.white;
            stateMessage.text = string.Format("Success to connect : {0}:{1}", ip, port);
        }
        catch(System.Exception e)
        {
            stateMessage.text = string.Format("Fail to connect : {0}", e.Message);
            stateMessage.color = Color.red;
            connectButton.IsToggled = false;
        }
    }

    void OnStartCaptureButtonClick()
    {
        try
        {
            if (!startCaptureButton.IsToggled)
            {
                pvCameraReader.StopPVCamera();
                images.SetActive(false);
                
                if (imagePlaneUpdateHandle != null)
                    StopCoroutine(imagePlaneUpdateHandle);
                
                return;
            }
      
            int idx = pvToggleCollection.CurrentIndex;
            images.SetActive(true);
            pvCameraReader.StartPVCamera((PVCameraType)idx);
            
            imagePlaneUpdateHandle = StartCoroutine(UpdateImagePlaneTexutre());

        }
        catch(System.Exception e)
        {
            stateMessage.text = string.Format("Fail : {0}", e.Message);
            stateMessage.color = Color.red;
            startCaptureButton.IsToggled = false;
        }

    }

    IEnumerator UpdateImagePlaneTexutre()
    {
        while (true)
        {
            if (pvCameraReader.IsNewFrame)
                pvImagePlane.GetComponent<MeshRenderer>().material.mainTexture = pvCameraReader.GrabCurrentTexture();

            yield return new WaitForEndOfFrame();
        }
    }

}
