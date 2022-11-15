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
    public Texture PrepareTextureForInput(Texture2D src, Shader shader)
    {
        Material norm_material = new Material(shader);
        //Debug.Log(preprocessMaterial.shader.name);

        var targetRT = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGBHalf);
        RenderTexture.active = targetRT;
        Graphics.Blit(src, targetRT, norm_material);

        var result = new Texture2D(targetRT.width, targetRT.height, TextureFormat.RGBAHalf, false);
        result.ReadPixels(new Rect(0, 0, targetRT.width, targetRT.height), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        return result;
    }
}
