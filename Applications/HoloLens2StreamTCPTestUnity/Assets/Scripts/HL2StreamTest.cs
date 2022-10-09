using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_WINMD_SUPPORT
using HoloLens2Stream;
#endif
public class HL2StreamTest : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    DefaultStream defaultStream;
#endif

    //
    private int frameIdx=0;

    // Image
    public Text debugText;
    public GameObject pvImagePlane = null;
    private Material pvImagMaterial = null;
    private Texture2D pvImageTexture = null;
    private byte[] pvImageData = null;

    // TCP-IP
    [SerializeField]
    string hostIPAddress, port;
    public ImageCompression imageCompression = ImageCompression.None;
    public int jpgQuality = 75;
    TCPClient socket;
    // Start is called before the first frame update
    void Start()
    {
        debugText = GameObject.Find("DebugText").GetComponent<Text>();
        
        socket = new TCPClient();

        try
        {
            socket.Connect(hostIPAddress, int.Parse(port));
            debugText.text = "TCP Connection OK.";
        }
        catch (Exception e)
        {
            Debug.LogError("On client connect exception " + e);
        }


        pvImagePlane = GameObject.Find("PVImagePlane");
        pvImagMaterial = pvImagePlane.GetComponent<MeshRenderer>().material;
        pvImageTexture = new Texture2D(760, 428, TextureFormat.BGRA32, false);
        pvImagMaterial.mainTexture = pvImageTexture;

#if ENABLE_WINMD_SUPPORT
        defaultStream = new DefaultStream();
        _ = defaultStream.InitializePVCamera();
#endif
        
        debugText.text = debugText.text += "\n Init OK.";
    }

    void Update()
    {
#if ENABLE_WINMD_SUPPORT
        byte[] frameTexture = defaultStream.GetPVCameraBuffer();
#else
        byte[] frameTexture = new byte[760*428*4]; //dumy value for testing
#endif

        if (pvImagePlane != null)
        {
            if (frameTexture.Length > 0)
            {
                if (pvImageData == null)
                {
                    pvImageData = frameTexture;
                    //text.text = text.text+"1";
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, pvImageData, 0, pvImageData.Length);
                    //text.text = text.text+"2";
                }
                //text.text = "frameTexture :" + frameTexture.Length.ToString();
                //debugText.text = "PV image data size :" + pvImageData.Length.ToString();


                pvImageTexture.LoadRawTextureData(pvImageData);
                pvImageTexture.Apply();

                byte[] bData = EEncodeImageData(frameIdx++, pvImageTexture, 4, imageCompression, jpgQuality);
                socket.SendMessage(bData);

                //debugText.text = debugText.text + "\n OK.";
            }
        }
       
    }



    byte[] EEncodeImageData(int frameID, Texture2D texture, int dim, ImageCompression comp, int jpgQuality)
    {
        //string EOF = "!@#$%^";
        //Debug.Log(comp.ToString());

        var now = DateTime.Now.ToLocalTime();
        var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

        var header = new HL2StreamHeaderInfo();
        header.frameID = frameID;

        header.width = texture.width;
        header.height = texture.height;
        header.dim = dim;
        header.dataType = DataType.PV;
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

#if ENABLE_WINMD_SUPPORT
        _ = defaultStream.StopPVCamera();
#endif
    }
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }

    private void OnDestroy()
    {
        if(socket != null && socket.isConnected)
            socket.Disconnect();
    }
}

