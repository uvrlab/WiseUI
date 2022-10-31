using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

public class MobilePoseBaseDetector : BaseDetector
{
    public MobilePoseBaseDetector(NNModel nnModel, WorkerFactory.Device device) : base(nnModel, device) { }
    public void Inference(Texture2D input_image)
    {
        var input_tensor = new Tensor(input_image, 3);
        var image = input_image;
        var input = new Tensor(image, 3);
        //Debug.LogFormat("{0}, {1}", image.width, image.height);
        //Debug.LogFormat(input.shape.ToString());
        //var input = new Tensor(1, 480, 640, 3);

        var output = engine.Execute(input).PeekOutput("550");
        //var output = engine.Execute(input);
        Debug.Log(output.shape.ToString());

        //var output20 = engine.PeekOutput("016_convolutional"); //016_convolutional = original output tensor name for 20x20 boundingBoxes
        //var output40 = engine.PeekOutput("023_convolutional"); //023_convolutional = original output tensor name for 40x40 boundingBoxes

        input.Dispose();
        Resources.UnloadUnusedAssets();
    }
}
