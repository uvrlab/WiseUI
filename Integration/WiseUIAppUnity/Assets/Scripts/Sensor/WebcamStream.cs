using System.Linq;
using UnityEngine;

namespace SensorStream
{
    public class WebcamStream
    {
        WebCamTexture webCamTexture;
        Texture2D targetTexture;


        public void StartPVCamera(PVCameraType pVCameraType, TextureFormat textureFormat)
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
        public bool IsNewFrame
        {
            get
            {
                return webCamTexture.didUpdateThisFrame;
            }
        }

        public void StopPVCamera()
        {
            webCamTexture.Stop();
        }

        public byte[] GetPVCameraBuffer(/*compression*/)
        {
            int width = webCamTexture.width;
            int height = webCamTexture.height;
            //save the active render texture
            RenderTexture temp = RenderTexture.active;
            
            RenderTexture copiedRenderTexture = new RenderTexture(width, height, 0);
            Graphics.Blit(webCamTexture, copiedRenderTexture, new Material(Shader.Find("FlipShader")));

            RenderTexture.active = copiedRenderTexture;
            //copy to texture 2d
            targetTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            targetTexture.Apply();

            RenderTexture.active = temp;
            
            //Color[] cdata = webCamTexture.GetPixels();
            //targetTexture.SetPixels(cdata);
            //targetTexture.Apply();
            

            return targetTexture.GetRawTextureData();

        }

    }

}
