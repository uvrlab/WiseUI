using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ARRCObjectronHelper
{
    public static Texture PrepareTextureForInput(Texture2D src, Shader shader)
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
    public static Texture PrepareTextureForInput(Texture2D src,  Material preprocessMaterial, Material postprocessMaterial)
    {
        //Material norm_material = new Material(shader);
        //Debug.Log(preprocessMaterial.shader.name);

        var targetRT = RenderTexture.GetTemporary(src.width, src.height, 0);
        var targetRT2 = RenderTexture.GetTemporary(src.width, src.height, 0);
        RenderTexture.active = targetRT2;
        Graphics.Blit(src, targetRT2, preprocessMaterial); //4channel to 3channel
        RenderTexture.active = targetRT;
        Graphics.Blit(targetRT2, targetRT, postprocessMaterial);

        var result = new Texture2D(targetRT.width, targetRT.height, TextureFormat.RGBAHalf, false);
        result.ReadPixels(new Rect(0, 0, targetRT.width, targetRT.height), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        return result;
    }
}
