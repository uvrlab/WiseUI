using System.Linq;
using UnityEngine;

public enum PVCameraType { r640x360xf30 = 0, r760x428xf30 = 1, r1280x720xf30};

public class HoloLens2PVCameraStream
{
    WebCamTexture webCamTexture;
    Texture2D targetTexture;


    public void InitPVCamera(PVCameraType pVCameraType, TextureFormat textureFormat)
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
        targetTexture = new Texture2D(webCamTexture.width, webCamTexture.height, textureFormat, false);

    }

    public void StopPVCamera()
    {
        webCamTexture.Stop();
    }

    public bool DidUpdatedPVCamera()
    {
        return webCamTexture.didUpdateThisFrame;
    }

    public byte[] GetPVCameraBuffer(/*compression*/)
    {
     
        Color[] cdata = webCamTexture.GetPixels();
        targetTexture.SetPixels(cdata);
        targetTexture.Apply();
        

        byte[] buffer = targetTexture.GetRawTextureData();
        return targetTexture.GetRawTextureData();
        
    }

}
