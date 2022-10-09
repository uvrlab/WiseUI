using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestCode : MonoBehaviour
{
    public SystemManager mSystemManager;
    public DataSender mSender;
    public RawImage image;
    public Texture2D tex;
    [HideInInspector]
    public byte[] data;

    // Start is called before the first frame update
    void Start()
    {

        //tex = new Texture2D(640, 480, TextureFormat.ARGB32, false);
        //
        try {
            //tex = Texture2D.CreateExternalTexture(
            //          image.texture.width,
            //          image.texture.height,
            //          TextureFormat.RGB24,
            //          false, false,
            //          image.texture.GetNativeTexturePtr());
            //tex.Apply();
            //Debug.Log(image.texture.width+" "+image.texture.height);

            
            var texture = image.texture;
            tex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
            Graphics.Blit(texture, renderTexture);

            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);

            data = tex.EncodeToJPG(50);
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
        }
        
    }

    // Update is called once per frame
    int frameID = 0;
    void Update()
    {
        try { 
            var timeSpan = DateTime.UtcNow - mSystemManager.StartTime;
            double ts = timeSpan.TotalMilliseconds;
            UdpData idata = new UdpData("TestImage", mSystemManager.User.UserName, frameID++, data, ts);
            StartCoroutine(mSender.SendData(idata));
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }
}
