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
using static ARRCObjectronDetector;

public class UIManager : MonoBehaviour
{
    // Modules
    public HoloLens2PVCameraReader pvCameraReader;
    public TCPClient_WiseUI tcpClient;
    public ARRCObjectronDetector objectDetector;

    public Interactable confButton;

    // Title UI
    public TextMeshPro stateMessage;
    public Interactable closeButton;


    // TCP UI
    public Interactable connectButton;
    public MRTKTMPInputField hostIPField, portField;

    public GameObject images;
    
    //Capture UI
    public Interactable startCaptureButton;
    public InteractableToggleCollection pvCamToggles;
    public GameObject pvImagePlane;
    Coroutine cameraTextureUpdateHandle;
    
    //Detection UI
    public Interactable startDetectionButton;
    public InteractableToggleCollection detectionToggles;
    public GameObject detectedImagePlane;
    Coroutine detectionUpdateHandle;
    //Camera Image planes
    
    Coroutine sendImageDataHandle;

    private void Awake()
    {
        pvCameraReader = GameObject.Find("Runnner").GetComponent<HoloLens2PVCameraReader>();
        tcpClient = GameObject.Find("Runnner").GetComponent<TCPClient_WiseUI>();
        objectDetector = GameObject.Find("Runnner").GetComponent<ARRCObjectronDetector>();
        
        confButton = transform.Find("Setting").GetComponent<Interactable>();
        confButton.OnClick.AddListener(OnConfigurationButtonClick);
        images = transform.Find("Images").gameObject;
        pvImagePlane = transform.Find("Images/PVImagePlane").gameObject;
        detectedImagePlane = transform.Find("Images/DetectedImagePlane").gameObject;
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
        pvCamToggles = transform.Find("Pannel/PVSensorGroup").GetComponent<InteractableToggleCollection>();
        startCaptureButton = transform.Find("Pannel/StartCapture").GetComponent<Interactable>();
        startCaptureButton.OnClick.AddListener(OnStartCaptureButtonClick);

        //Detection target.
        detectionToggles = transform.Find("Pannel/ModelGroup").GetComponent<InteractableToggleCollection>();
        startDetectionButton = transform.Find("Pannel/StartDetection").GetComponent<Interactable>();
        startDetectionButton.OnClick.AddListener(OnStartDetectionButtonClick);

        
    }
    private void Start()
    {
        LoadUIContents();
        //images.SetActive(false);
        pvImagePlane.SetActive(false);
        detectedImagePlane.SetActive(false);
    }
    void LoadUIContents()
    {
        ConfigurationManager.Instance.Load();
        hostIPField.text = ConfigurationManager.Instance.hostIP;
        portField.text = ConfigurationManager.Instance.port.ToString();
        pvCamToggles.SetSelection((int)ConfigurationManager.Instance.pvCameraType);
    }
    void SaveUIContents()
    {
        string ip = hostIPField.text;
        int port = int.Parse(portField.text);
        PVCameraType pVCameraType = (PVCameraType)pvCamToggles.CurrentIndex;
        ConfigurationManager.Instance.Save(ip, port, pVCameraType);
        Debug.Log(string.Format("{0}, {1},{2}", ip, port, pVCameraType.ToString()));
    }
    void OnConfigurationButtonClick()
    {
        LoadUIContents();
    }
    
    void CloseButtonClick()
    {
        SaveUIContents();
    }
    private void OnDestroy()
    {
        SaveUIContents();
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
                pvImagePlane.SetActive(false);
                
                if (cameraTextureUpdateHandle != null)
                    StopCoroutine(cameraTextureUpdateHandle);
                
                return;
            }
      
            int idx = pvCamToggles.CurrentIndex;
            pvImagePlane.SetActive(true);
            pvCameraReader.StartPVCamera((PVCameraType)idx);
            cameraTextureUpdateHandle = StartCoroutine(UpdateCameraTexutre());

        }
        catch(System.Exception e)
        {
            stateMessage.text = string.Format("Fail : {0}", e.Message);
            stateMessage.color = Color.red;
            startCaptureButton.IsToggled = false;
        }

    }

    void OnStartDetectionButtonClick()
    {
        try
        {
            if (!startDetectionButton.IsToggled)
            {
                detectedImagePlane.SetActive(false);

                if (detectionUpdateHandle != null)
                    StopCoroutine(detectionUpdateHandle);
                return;
            }
            
            detectedImagePlane.SetActive(true);
            int idx = detectionToggles.CurrentIndex;
            detectionUpdateHandle = StartCoroutine(UpdateDetection());


            objectDetector.LoadModel((ModelType)idx);
            stateMessage.text = string.Format("Load Model OK.");
        }
        catch (System.Exception e)
        {
            stateMessage.text = string.Format("Fail : {0}", e.Message);
            stateMessage.color = Color.red;
            startDetectionButton.IsToggled = false;
        }
    }
    IEnumerator UpdateCameraTexutre()
    {
        while (true)
        {
            if (pvCameraReader.IsNewFrame)
            {
                pvCameraReader.UpdateCameraTexture();
                Texture2D latestTexture = pvCameraReader.GetCurrentTexture();
                pvImagePlane.GetComponent<MeshRenderer>().material.mainTexture = latestTexture;
                
                if(connectButton.IsToggled)
                    tcpClient.SendEEncodeImageData(pvCameraReader.FrameID, latestTexture, ImageCompression.None);

                if (startDetectionButton.IsToggled)
                {
                    objectDetector.Run(latestTexture);
                }
                    
                //tcpClient.SendEEncodeImageData(pvCameraReader.FrameID, latestTexture, ImageCompression.None);
                //float time_to_send = Time.time - start_time;
                //DebugText.Instance.lines["Time_to_send"] = time_to_send.ToString();
            }
            
            yield return new WaitForEndOfFrame();
        }
    }
    IEnumerator UpdateDetection()
    {
        while (true)
        {
        
            yield return new WaitForEndOfFrame();
        }
    }
}

