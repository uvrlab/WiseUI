//#define USE_OPENCV

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;


#if ENABLE_WINMD_SUPPORT
using HoloLens2Stream;
#endif

namespace SensorStream
{
    //#define ENABLE_WINMD_SUPPORT 
    public class HoloLens2PVCameraReader : BaseSensorReader
    {
       
        public int FrameID
        {
            get { return frameID; }
            set { frameID = value; }
        }
        [SerializeField]
        public PVCameraType pvCameraType = PVCameraType.r640x360xf30;

        [SerializeField]
        public TextureFormat textureFormat = TextureFormat.BGRA32; //proper to numpy data format.
        
        Texture2D latestTexture;
        
        [SerializeField]
        int frameID;
#if ENABLE_WINMD_SUPPORT
    DefaultStream pvCameraStream;
#elif USE_OPENCV
    WebcamOpenCVStream pvCameraStream;
#else
        WebcamStream pvCameraStream;
#endif


        public void StartPVCamera(PVCameraType pVCameraType)
        {
            frameID = -1;

            int width, height;
            if (pvCameraType == PVCameraType.r640x360xf30)
            {
                width = 640;
                height = 360;
            }


            else if (pvCameraType == PVCameraType.r760x428xf30)
            {
                width = 760;
                height = 428;
            }
            else
                throw (new Exception("Invalid resolution."));

            //else if (pvCameraType == PVCameraType.r1280x720xf30)
            //    latestTexture = new Texture2D(1280, 720, textureFormat, false);

            DebugText.Instance.lines["Init PV camera"] = "preparing..";
            try
            {
#if ENABLE_WINMD_SUPPORT
                pvCameraStream = new DefaultStream();
                latestTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
                _ = pvCameraStream.StartPVCamera(width);
#elif USE_OPENCV
                pvCameraStream = new WebcamOpenCVStream();
                latestTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
                pvCameraStream.StartPVCamera(width, height);
#else
                pvCameraStream = new WebcamStream();
                latestTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
                pvCameraStream.StartPVCamera(pVCameraType, TextureFormat.BGRA32);
#endif
                DebugText.Instance.lines["Init PV camera"] = "ok.";
            }
            catch (Exception e)
            {
                DebugText.Instance.lines["Init PV camera"] = e.Message;
            }
        }

        public bool IsNewFrame
        {
            get
            {
                return pvCameraStream.IsNewFrame;
            }

        }

        public void UpdateCameraTexture()
        {
            frameID++;
            byte[] frameBuffer = pvCameraStream.GetPVCameraBuffer();
            DebugText.Instance.lines["frameTexture.Length"] = frameBuffer.Length.ToString();
            latestTexture.LoadRawTextureData(frameBuffer);
            latestTexture.Apply();
        }
        
        public Texture2D GetCurrentTexture()
        {
            //만약 
            if(frameID == -1)
            {
                UpdateCameraTexture();
            }
            return latestTexture;
        }

        public override void StopPVCamera()
        {


            if (pvCameraStream != null)
            {
#if ENABLE_WINMD_SUPPORT
            _ = pvCameraStream.StopPVCamera();
#else
                pvCameraStream.StopPVCamera();
#endif
            }
        }

        //    void Update()
        //    {
        //        try
        //        {
        //            float start_time = Time.time;
        //            lock(lockObject)
        //            {
        //#if ENABLE_WINMD_SUPPORT
        //                if(pvCameraStream.IsNewFrame())
        //                {
        //                    byte[] frameTexture = pvCameraStream.GetPVCameraBuffer();
        //                    if (frameTexture.Length > 0)
        //                    {
        //                        DebugText.Instance.lines["frameTexture.Length"] = frameTexture.Length.ToString();
        //                        pvImageTexture.LoadRawTextureData(frameTexture);
        //                        pvImageTexture.Apply();
        //                    }
        //                }
        //#else
        //                if (pvCameraStream.IsNewFrame())
        //                {
        //                    pvCameraStream.CopyCurrentTexture(ref pvImageTexture);
        //                }
        //#endif
        //            }

        //            float time_to_copy_buffer = Time.time - start_time;
        //            DebugText.Instance.lines["Time_to_copy"] = time_to_copy_buffer.ToString();
        //            start_time = Time.time;
        //            byte[] bData = EEncodeImageData(frameIdx++, pvImageTexture, imageCompression, jpgQuality);
        //            //socket.SendMessage(bData);
        //            float time_to_send = Time.time - start_time;
        //            DebugText.Instance.lines["Time_to_send"] = time_to_send.ToString();
        //        }
        //        catch (Exception e)
        //        {
        //            DebugText.Instance.lines["GetPVCameraBuffer"] = e.Message;
        //            Debug.LogError(e.ToString());
        //        }
        //    }

        public void StopSensorsEvent()
        {
            if (pvCameraStream != null)
            {

            }
        }
        private void OnApplicationFocus(bool focus)
        {
            if (!focus) StopSensorsEvent();
        }

        private void OnDestroy()
        {

            StopPVCamera();

            //if (socket != null && socket.isConnected)
            //    socket.Disconnect();
        }


    }
}

