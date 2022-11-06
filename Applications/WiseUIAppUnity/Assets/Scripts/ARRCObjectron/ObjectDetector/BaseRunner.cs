using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

//using ARRCObjectronCoreUWP;

public class BaseRunner
{
    protected IWorker engine;
    
    public BaseRunner(IWorker engine)
    {
        this.engine = engine;
    }
    
    public virtual void Run(Texture2D inputTexture)
    {
        
    }
}
