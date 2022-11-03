#define USE_OPENCV
namespace SensorStream
{

#if USE_OPENCV
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using OpenCvSharp;

    using System.Threading;
    using System;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    
    public class WebcamOpenCVStream
    {
        VideoCapture capture;
        Thread thread;
        byte[] latestBuffer;
        bool isNewFrame;
        readonly object lock_object = new object();

        public void InitializePVCamera(int width, int height)
        {
            capture = new VideoCapture(0);
            capture.FrameWidth = width;
            capture.FrameHeight = height;

            thread = new Thread(CaptureLoop);
            thread.Start();

        }
        public void StopCamera()
        {
            if (thread != null)
            {
                thread.Interrupt();
                thread = null;
            }

        }
        public bool IsNewFrame
        {
            get
            {
                lock (lock_object)
                {
                    return isNewFrame;
                }

            }

        }
        void CaptureLoop()
        {
            try
            {
                while (true)
                {
                    using (Mat frame = new Mat())
                    {
                        capture.Read(frame);
                        //change bgr to rgb.
                        Cv2.CvtColor(frame, frame, ColorConversionCodes.BGR2RGB);

                        if (frame.Empty())
                            continue;

                        lock (lock_object)
                        {
                            isNewFrame = true;
                            var bufferSize = frame.Cols * frame.Rows * frame.ElemSize();
                            if (latestBuffer == null || latestBuffer.Length != bufferSize)
                            {
                                latestBuffer = new byte[bufferSize];
                            }
                            Marshal.Copy(frame.Data, latestBuffer, 0, bufferSize);
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (ThreadInterruptedException e)
            {
                Debug.LogFormat(e.Message);
            }
        }
        public byte[] GetPVCameraBuffer()
        {
            byte[] output;
            lock (lock_object)
            {
                isNewFrame = false;
                output = new byte[latestBuffer.Length];
                Array.Copy(latestBuffer, output, output.Length);
            }
            return output;
        }
    }
#endif
}
