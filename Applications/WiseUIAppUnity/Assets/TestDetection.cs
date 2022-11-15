using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using static Unity.Barracuda.Model;

public class TestDetection : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<ARRCObjectronDetector>().LoadModel(ARRCObjectronDetector.ModelType.MobilePoseShape_chair);
        
    }

    // Update is called once per frame
    void Update()
    {
        var temp = new Texture2D(640, 480, TextureFormat.RGBA32, false);
        GetComponent<ARRCObjectronDetector>().Run(temp);
    }
}
