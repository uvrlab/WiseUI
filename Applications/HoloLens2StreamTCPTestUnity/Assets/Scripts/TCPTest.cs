using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

public class TCPTest : MonoBehaviour
{
    [SerializeField]
    string hostIPAddress, port;
    public ImageCompression imageCompression = ImageCompression.None;
    public int jpgQuality = 75;

    TCPClient socket;

    public Texture2D tex;
    byte[] buff;
    int frameID = 0;



    void Start()
    {
        socket = new TCPClient();
        socket.Connect(hostIPAddress, int.Parse(port));

        //tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);

        var format = tex.format;


        buff = tex.GetRawTextureData();
        

  
        //pvImageMediaTexture.LoadRawTextureData(buff);
        //pvImageMediaTexture.Apply();

        //float time = Time.realtimeSinceStartup;
        //byte[] encoded = EEncodeImageData2(frameID++, buff, tex.width, tex.height, buff.Length/(tex.width*tex.height));
        //float time_encoding = Time.realtimeSinceStartup;
        //socket.SendMessage(encoded);

        //Debug.LogFormat("Time to encode {0}, send : {1}, total : {2}"
        //    , (time_encoding - time), (Time.realtimeSinceStartup - time_encoding), (Time.realtimeSinceStartup - time));
    }

    //byte[] EEncodeImageData(int frameID, Texture2D texture, int width, int height, int dim, ImageCompression comp, int jpgQuality)
    //{
    //    //string EOF = "!@#$%^";
    //    Debug.Log(comp.ToString());

    //    var now = DateTime.Now.ToLocalTime();
    //    var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

    //    var header = new HL2StreamHeaderInfo();
    //    header.frameID = frameID;

    //    header.width = width;
    //    header.height = height;
    //    header.dim = dim;
    //    header.dataType = DataType.PV;
    //    header.timestamp = span.TotalSeconds;
    //    header.dataCompressionType = comp;
    //    header.imageQulaity = jpgQuality;

    //    byte[] bImage = null;

    //    if (comp == ImageCompression.None)
    //        bImage = tex.GetRawTextureData();

    //    if (comp == ImageCompression.JPEG)
    //        bImage = tex.EncodeToJPG(jpgQuality);

    //    header.data_length = bImage.Length;

    //    string sHeader = JsonUtility.ToJson(header);

    //    byte[] bHeader = Encoding.ASCII.GetBytes(sHeader);
    //    byte[] bHeaderSize = BitConverter.GetBytes(bHeader.Length);
    //    byte[] bTotal = new byte[4 + bHeader.Length + bImage.Length];
    //    //byte[] bEOF = Encoding.UTF8.GetBytes(EOF);

    //    System.Buffer.BlockCopy(bHeaderSize, 0, bTotal, 0, 4);
    //    System.Buffer.BlockCopy(bHeader, 0, bTotal, 4, bHeader.Length);
    //    System.Buffer.BlockCopy(bImage, 0, bTotal, 4 + bHeader.Length, bImage.Length);
    //    //System.Buffer.BlockCopy(bEOF, 0, bTotal, 4 + bHeader.Length + bImage.Length, bEOF.Length);

    //    // Debug.LogFormat("Header Length : {0}, Data Length{1}", bHeader.Length, bImage.Length);

    //    return bTotal;
    //}
    byte[] EEncodeImageData(int frameID, Texture2D texture, int dim, ImageCompression comp, int jpgQuality)
    {
        //string EOF = "!@#$%^";
        Debug.Log(comp.ToString());

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
        byte[] bContentSize = BitConverter.GetBytes(contentSize);

        int totalSize = 4 + contentSize;
        byte[] bTotal = new byte[totalSize];

        System.Buffer.BlockCopy(bContentSize, 0, bTotal, 0, 4);
        System.Buffer.BlockCopy(bHeaderSize, 0, bTotal, 4, 4);
        System.Buffer.BlockCopy(bHeader, 0, bTotal, 4 + 4, bHeader.Length);
        System.Buffer.BlockCopy(bImage, 0, bTotal, 4 + 4 + bHeader.Length, bImage.Length);

        Debug.LogFormat("Header data size : {0}, Image data size : {1}, Total data size : {2}", bHeader.Length, bImage.Length, totalSize);

        return bTotal;
    }

    private void OnDestroy()
    {
        socket.Disconnect();
    }

    // Update is called once per frame
    void Update()
    {
        float time = Time.realtimeSinceStartup;

        int dim = 4;
        byte[] encoded = EEncodeImageData(frameID++, tex, dim, imageCompression, jpgQuality);
        float time_encoding = Time.realtimeSinceStartup;
    
        socket.SendMessage(encoded);

        Debug.LogFormat("encoding : {0}, sending : {1}, total : {2}"
           , (time_encoding - time), (Time.realtimeSinceStartup - time_encoding), (Time.realtimeSinceStartup - time));

        Debug.LogFormat("Total Length : {0}", encoded.Length);
    }


}
