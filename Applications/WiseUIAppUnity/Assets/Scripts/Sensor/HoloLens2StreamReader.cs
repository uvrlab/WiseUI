using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_WINMD_SUPPORT
using HoloLens2Stream;
#endif


public class HoloLens2StreamReader : MonoBehaviour
{
    private int frameIdx = 0;
    HoloLens2PVCameraStream holoLens2PVCameraStream;

    public GameObject pvImagePlane = null;
    private Texture2D pvImageTexture = null;
    public PVCameraType pvCameraType = PVCameraType.r640x360xf30;
    public TextureFormat textureFormat = TextureFormat.BGRA32; //proper to numpy data format.

    // TCP-IP
    [SerializeField]
    string hostIPAddress, port;
    public ImageCompression imageCompression = ImageCompression.None;
    public int jpgQuality = 75;
    TCPClient socket;


    //Debug
    //public Text debugText;

    void Start()
    {

        //debugText = GameObject.Find("DebugText").GetComponent<Text>();
        socket = new TCPClient();
        try
        {
            socket.Connect(hostIPAddress, int.Parse(port));
            DebugText.Instance.lines["TCP Connection"] = "ok.";
        }
        catch (Exception e)
        {
            Debug.LogError("On client connect exception " + e);
            DebugText.Instance.lines["TCP Connection"] = "fail.";
        }


        pvImagePlane = GameObject.Find("PVImagePlane");

        if (pvCameraType == PVCameraType.r640x360xf30)
            pvImageTexture = new Texture2D(640, 360, textureFormat, false);
        else if (pvCameraType == PVCameraType.r760x428xf30)
            pvImageTexture = new Texture2D(760, 428, textureFormat, false);
        else
            pvImageTexture = new Texture2D(1280, 720, textureFormat, false);

        pvImagePlane.GetComponent<MeshRenderer>().material.mainTexture = pvImageTexture;

        holoLens2PVCameraStream = new HoloLens2PVCameraStream();
        holoLens2PVCameraStream.InitPVCamera(pvCameraType, textureFormat);

        DebugText.Instance.lines["Init PV camera"] = "ok.";

    }
    
   

    // Update is called once per frame
    void Update()
    {
        try
        {
           if(holoLens2PVCameraStream.DidUpdatedPVCamera())
            {
                //float start_time = Time.time;
                byte[] frameTexture = holoLens2PVCameraStream.GetPVCameraBuffer();
                pvImageTexture.LoadRawTextureData(frameTexture);
                pvImageTexture.Apply();
                //Debug.LogFormat("Time to copy camera buffer : {0}", Time.time - start_time);

                byte[] bData = EEncodeImageData(frameIdx++, pvImageTexture, imageCompression, jpgQuality);
                socket.SendMessage(bData);
                
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    ImageFormat ConvertTextureFormat2ImageFormat(TextureFormat textureFormat)
    {
        switch(textureFormat)
        {
            case TextureFormat.RGBA32:
                return ImageFormat.RGBA;
            case TextureFormat.BGRA32:
                return ImageFormat.BGRA;
            case TextureFormat.ARGB32:
                return ImageFormat.ARGB;
            case TextureFormat.RGB24:
                return ImageFormat.RGB;
        }
        return ImageFormat.INVALID;
    }

    byte[] EEncodeImageData(int frameID, Texture2D texture, ImageCompression comp = ImageCompression.None, int jpgQuality = 75)
    {
        var now = DateTime.Now.ToLocalTime();
        var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

        var header = new HL2StreamHeaderInfo();
        
        header.frameID = frameID;
        header.width = texture.width;
        header.height = texture.height;
        header.dataType = DataType.PV;
        header.imageFormat = ConvertTextureFormat2ImageFormat(texture.format);
        header.timestamp = span.TotalSeconds;
        header.dataCompressionType = comp;
        header.imageQulaity = jpgQuality;

        byte[] bImage = null;

        if (comp == ImageCompression.None)
            bImage = texture.GetRawTextureData();

        if (comp == ImageCompression.JPEG)
            bImage = texture.EncodeToJPG(jpgQuality);

        header.data_length = bImage.Length;

        string sHeader = JsonUtility.ToJson(header);
        byte[] bHeader = Encoding.ASCII.GetBytes(sHeader);
        byte[] bHeaderSize = BitConverter.GetBytes(bHeader.Length);

        int contentSize = 4 + bHeader.Length + bImage.Length;
        byte[] bContentSizeData = BitConverter.GetBytes(contentSize);

        int totalSize = 4 + contentSize;
        byte[] bTotal = new byte[totalSize];

        System.Buffer.BlockCopy(bContentSizeData, 0, bTotal, 0, 4);
        System.Buffer.BlockCopy(bHeaderSize, 0, bTotal, 4, 4);
        System.Buffer.BlockCopy(bHeader, 0, bTotal, 4 + 4, bHeader.Length);
        System.Buffer.BlockCopy(bImage, 0, bTotal, 4 + 4 + bHeader.Length, bImage.Length);

        Debug.LogFormat("Header data size : {0}, Image data size : {1}, Total data size : {2}", bHeader.Length, bImage.Length, totalSize);

        return bTotal;
    }
    public void StopSensorsEvent()
    {

    }
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }

    private void OnDestroy()
    {
        holoLens2PVCameraStream.StopPVCamera();

        if (socket != null && socket.isConnected)
            socket.Disconnect();
    }
}