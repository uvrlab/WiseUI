using System.Collections;
using System.Linq;
using System.Threading;
using UnityEngine;


public class WebcamReader : BaseSensorReader
{
    public PVCameraType pvCameraType = PVCameraType.r640x360xf30;
    public TextureFormat textureFormat = TextureFormat.BGRA32; //proper to numpy data format.
    WebCamTexture webCamTexture;
    Coroutine handler;
    Texture2D latestTexture;
    bool isNewFrame;
    
    private void Start()
    {
        InitializePVCamera(pvCameraType, textureFormat);

        GameObject.Find("PVImagePlane").GetComponent<MeshRenderer>().material.mainTexture = latestTexture;
    }
    public void InitializePVCamera(PVCameraType pVCameraType, TextureFormat textureFormat)
    {
        foreach (var dev in WebCamTexture.devices)
        {
            if (dev.availableResolutions != null)
                DebugText.Instance.lines[dev.name] = dev.availableResolutions.Length.ToString();
            else
                DebugText.Instance.lines[dev.name] = "0";
        }

        if (pVCameraType == PVCameraType.r640x360xf30)
            webCamTexture = new WebCamTexture(WebCamTexture.devices.First<WebCamDevice>().name, 640, 360, 30);
        else if (pVCameraType == PVCameraType.r760x428xf30)
            webCamTexture = new WebCamTexture(WebCamTexture.devices.First<WebCamDevice>().name, 720, 428, 30);
        else
            webCamTexture = new WebCamTexture(WebCamTexture.devices.First<WebCamDevice>().name, 1280, 720, 30);

        webCamTexture.Play();

        latestTexture = new Texture2D(webCamTexture.width, webCamTexture.height, textureFormat, false);
        

        //capture_thread = new Thread(CaptureLoop);
        //capture_thread.Start();

        handler = StartCoroutine(CaptureLoop());

    }

    public override void StopCapture()
    {
        webCamTexture.Stop();
        StopCoroutine(handler);

    }
    private void OnDestroy()
    {
        StopCapture();
    }
    IEnumerator CaptureLoop() 
    {
        while(true)
        {
            if (webCamTexture.didUpdateThisFrame)
            {
                //lock (lockObject)
                {
                    Color[] cdata = webCamTexture.GetPixels();
                    latestTexture.SetPixels(cdata);
                    latestTexture.Apply();
                    isNewFrame = true;
                }
            }
            yield return null;
        }
    }

    public bool IsNewFrame()
    {
        //lock (lockObject)
        {
            return isNewFrame;
        }
    }
    public override void GrabCurrentTexture(ref Texture2D dest)
    {
        //lock (lockObject)
        {
            Graphics.CopyTexture(latestTexture, dest);
        }
    }


}
