using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Runtime.InteropServices;



public class MobilePoseShapeRunner : BaseRunner
{

    public MobilePoseShapeRunner(IWorker engine) : base(engine) { }


    public override void Run(Texture2D _inputImage)
    {
        DebugText.Instance.lines["barracuda :"] = "Preparing";
        var inputImage = new Texture2D(480, 640, TextureFormat.BGRA32, false);
        double start_time = Time.realtimeSinceStartupAsDouble;
        var inputTensor = new Tensor(ARRCObjectronHelper.PrepareTextureForInput(inputImage, Shader.Find("ML/NormalizeAndSwapRB_MobilePose")), 3);
        var output_barrcuda = engine.Execute(inputTensor);
        DebugText.Instance.lines["barracuda :"] = "excute done.";
        
        var heatmap_barrcuda = output_barrcuda.PeekOutput("Identity");
        //var offsetmap_barracuda = output_barrcuda.PeekOutput("550");
        double spent_time = Time.realtimeSinceStartupAsDouble - start_time;
        DebugText.Instance.lines["barracuda :"] = spent_time.ToString();

        inputTensor.Dispose();
        Resources.UnloadUnusedAssets();
    }
}
