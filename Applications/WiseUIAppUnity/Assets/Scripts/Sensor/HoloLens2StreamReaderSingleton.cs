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


public class HoloLens2StreamReaderSingleton : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    DefaultStream pvCameraStream;
#else
    WebcamStream pvCameraStream;
#endif
    public GameObject pvImagePlane = null;

    public int frameIdx = 0;
    public bool isCapturing_PV;

    Texture2D pvImageTexture = null;
    TextureFormat textureFormat = TextureFormat.BGRA32; //proper to numpy data format.
    ImageCompression imageCompression = ImageCompression.None;
    int jpgQuality = 75;

    public void InitializePVCamera(PVCameraType cameraType)
    {
        StopPVCamera();

        frameIdx = 0;
        var pvCameraType = ConfigurationManager.Instance.pvCameraType;

        pvImagePlane = GameObject.Find("PVImagePlane");

        if (pvCameraType == PVCameraType.r640x360xf30)
            pvImageTexture = new Texture2D(640, 360, textureFormat, false);
        else if (pvCameraType == PVCameraType.r760x428xf30)
            pvImageTexture = new Texture2D(760, 428, textureFormat, false);
        else if (pvCameraType == PVCameraType.r1280x720xf30)
            pvImageTexture = new Texture2D(1280, 720, textureFormat, false);
        else
            throw (new Exception("Invalid resolution."));

        pvImagePlane.GetComponent<MeshRenderer>().material.mainTexture = pvImageTexture;


        try
        {
#if ENABLE_WINMD_SUPPORT
            pvCameraStream = new DefaultStream();
            _ = pvCameraStream.InitializePVCamera(pvImageTexture.width);
#else
            pvCameraStream = new WebcamStream();
            pvCameraStream.InitializePVCamera(pvCameraType, textureFormat);
#endif
            //DebugText.Instance.lines["Init PV camera"] = "ok.";
            isCapturing_PV = true;
        }
        catch (Exception e)
        {
            //DebugText.Instance.lines["Init PV camera"] = e.Message;
            throw e;
        }

    }
    public void StopPVCamera()
    {
        if (pvCameraStream != null)
        {
#if ENABLE_WINMD_SUPPORT
            _ = pvCameraStream.StopPVCamera();
        
#else
            pvCameraStream.StopPVCamera();
#endif
        }
        isCapturing_PV = false;
    }

    void Update()
    {
        if(isCapturing_PV)
        {
            float start_time = Time.time;
            try
            {

#if ENABLE_WINMD_SUPPORT
            byte[] frameTexture = pvCameraStream.GetPVCameraBuffer();
            if (frameTexture.Length > 0)
            {
                DebugText.Instance.lines["frameTexture.Length"] = frameTexture.Length.ToString();
                pvImageTexture.LoadRawTextureData(frameTexture);
                pvImageTexture.Apply();
            }
#else
                if (pvCameraStream.DidUpdatedPVCamera())
                {
                    byte[] frameTexture = pvCameraStream.GetPVCameraBuffer();
                    pvImageTexture.LoadRawTextureData(frameTexture);
                    pvImageTexture.Apply();

                }
#endif
            }
            catch (Exception e)
            {
                DebugText.Instance.lines["GetPVCameraBuffer"] = e.Message;
                Debug.LogError(e.ToString());
            }

            float time_to_copy_buffer = Time.time - start_time;
            //DebugText.Instance.lines["Time_to_copy"] = time_to_copy_buffer.ToString();

            if (TCPClientSingleton.Instance.isConnected)
            {
                start_time = Time.time;
                try
                {
                    byte[] bData = EEncodeImageData(frameIdx++, pvImageTexture, imageCompression, jpgQuality);
                    TCPClientSingleton.Instance.SendMessage(bData);
                    float time_to_send = Time.time - start_time;
                    //DebugText.Instance.lines["Time_to_send"] = time_to_send.ToString();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
            }
        }
    }

    ImageFormat ConvertTextureFormat2ImageFormat(TextureFormat textureFormat)
    {
        switch (textureFormat)
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

    private void OnDestroy()
    {
        StopPVCamera();
    }

    static HoloLens2StreamReaderSingleton _instance;
    public static HoloLens2StreamReaderSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = GameObject.Find("HoloLens2StreamReader");
                if (obj == null)
                {
                    obj = new GameObject("HoloLens2StreamReader");
                    _instance = obj.AddComponent<HoloLens2StreamReaderSingleton>();
                    DontDestroyOnLoad(obj);
                }
            }

            return _instance;
        }
    }
}