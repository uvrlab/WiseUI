using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Runtime.InteropServices;



public class BaseDetector
{
    protected IWorker engine;

    public BaseDetector(NNModel nnModel, WorkerFactory.Device device)
    {
        var model = ModelLoader.Load(nnModel);
        engine = WorkerFactory.CreateWorker(model, device);
    }


    void OnDestroy()
    {
        if(engine != null)
            engine.Dispose();
    }
}
