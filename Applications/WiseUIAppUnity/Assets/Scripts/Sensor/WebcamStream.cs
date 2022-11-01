using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System.Threading;
using System;

public class WebcamStream 
{
    VideoCapture capture;
    Thread thread;
    byte[] latestBuffer;
    bool isNewFrame;
    object lock_object;
    
    void InitializeCamera(int width, int height)
    {
        capture = new VideoCapture(0);
        capture.FrameWidth = width;
        capture.FrameHeight = height;

        thread = new Thread(CaptureLoop);
        thread.Start();
    }
    void StopCamera()
    {
        thread.Interrupt();
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
                    Cv2.ImShow("test", frame);
                    Cv2.WaitKey(1);

                    if (frame.Empty())
                        continue;

                    lock (lock_object)
                    {
                        latestBuffer = frame.ToBytes();
                        isNewFrame = true;
                    }
                }
                Thread.Sleep(1);
            }
        }
        catch (ThreadInterruptedException e)
        {
            Debug.Log("thread interrupted.");
        }
    }
    byte[] GetPVCameraBuffer()
    {
        lock (lock_object)
        {
            if (isNewFrame)
            {
                isNewFrame = false;
                return latestBuffer;
            }
            else
                return null;
        }
    }
}


