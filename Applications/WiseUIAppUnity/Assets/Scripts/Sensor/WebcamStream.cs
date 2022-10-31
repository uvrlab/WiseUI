using System.Linq;
using System.Threading;
using UnityEngine;

public enum PVCameraType {r640x360xf30 = 0, r760x428xf30 = 1, r1280x720xf30 = 2};

public class WebcamStream
{
    WebCamTexture webCamTexture;
    Texture2D targetTexture;

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
        if (webCamTexture.didUpdateThisFrame)
        {
            Color[] cdata = webCamTexture.GetPixels();
            targetTexture.SetPixels(cdata);
            targetTexture.Apply();
        }

        return targetTexture.GetRawTextureData();
    }
    public bool IsNewFrame()
    {
        return webCamTexture.didUpdateThisFrame;
    }
    public void CopyCurrentTexture(ref Texture2D dest)
    {
        Color[] cdata = webCamTexture.GetPixels();
        dest.SetPixels(cdata);
        dest.Apply();
    }


}
