using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Runtime.InteropServices;



public class Tset : MonoBehaviour
{
    

    public NNModel srcModel;
    public Texture2D[] inputImage;

 

    // Start is called before the first frame update
    void Start()
    {
        var model = ModelLoader.Load(srcModel);
        var engine = WorkerFactory.CreateWorker(model, WorkerFactory.Device.GPU);
       // var engine = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);
        var image = inputImage[0];
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
        engine.Dispose();
        Resources.UnloadUnusedAssets();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
