using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //
    public Interactable confButton;

    // Title
    public TextMeshPro stateMessage;
    public Interactable closeButton;


    // TCP
    public Interactable connectButton;
    public MRTKTMPInputField hostIPField, portField;

    
    //Capture
    public Interactable startCaptureButton;
    public InteractableToggleCollection pvToggleCollection;
    public GameObject images;

    private void Awake()
    {
        confButton = transform.Find("Setting").GetComponent<Interactable>();
        confButton.OnClick.AddListener(OnConfigurationButtonClick);
        images = transform.Find("Images").gameObject;
        images.SetActive(false);

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
            TCPClientSingleton.Instance.Disconnect();
            stateMessage.text = string.Format("Success to disconnect");
            return;
        }

        try
        {
            string ip = hostIPField.text;
            int port = int.Parse(portField.text);

            TCPClientSingleton.Instance.Connect(ip, port);
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
                HoloLens2StreamReaderSingleton.Instance.StopPVCamera();
                images.SetActive(false);
                return;
            }
      
            int idx = pvToggleCollection.CurrentIndex;
            images.SetActive(true);
            HoloLens2StreamReaderSingleton.Instance.InitializePVCamera((PVCameraType)idx);
            
        }
        catch(System.Exception e)
        {
            stateMessage.text = string.Format("Fail : {0}", e.Message);
            stateMessage.color = Color.red;
            startCaptureButton.IsToggled = false;
        }

    }
}
