using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Barracuda;
using UnityEngine;
using static Unity.Barracuda.Model;

public class MobilePoseBaseRunner : BaseRunner
{
    public MobilePoseBaseRunner(IWorker engine) : base(engine){}

    public override void Run(Texture2D _inputImage)
    {
        DebugText.Instance.lines["barracuda :"] = "Preparing";
        var inputImage = new Texture2D(640, 480, TextureFormat.BGRA32, false);
        double start_time = Time.realtimeSinceStartupAsDouble;
        var inputTensor = new Tensor(ARRCObjectronHelper.PrepareTextureForInput(inputImage, Shader.Find("ML/NormalizeAndSwapRB_MobilePose")), 3);
        var output_barrcuda = engine.Execute(inputTensor);
        DebugText.Instance.lines["barracuda :"] = "excute done.";
        
        var heatmap_barrcuda = output_barrcuda.PeekOutput("output");
        var offsetmap_barracuda = output_barrcuda.PeekOutput("550");
        double spent_time = Time.realtimeSinceStartupAsDouble - start_time;
        DebugText.Instance.lines["barracuda :"] = spent_time.ToString();

        inputTensor.Dispose();
        Resources.UnloadUnusedAssets();
    }
}

