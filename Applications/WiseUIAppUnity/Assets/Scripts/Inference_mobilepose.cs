//#define WEBCAM

using UnityEngine;
using Unity.Barracuda;
using OpenCvSharp;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using static UnityEngine.GraphicsBuffer;
using System.Collections;

#if WEBCAM
using System;
using System.Linq;
#endif

public class Inference_mobilepose : MonoBehaviour
{
    private Model m_RuntimeModel;
    private IWorker m_Worker;
    Thread thread;
    object lock_object = new object();
#if (WEBCAM)
    private WebCamTexture m_WebcamTexture;
#else
    private Tensor m_Input;
    public Texture2D inputImage;
#endif

    public NNModel inputModel;
    public Material preprocessMaterial;
    public Material postprocessMaterial;

    public int inputResolutionY = 32;
    public int inputResolutionX = 32;

    Tensor result;
    
    void Start()
    {
        //Application.targetFrameRate = 60;
		
        m_RuntimeModel = ModelLoader.Load(inputModel, false);
        //m_Worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel, false);
        m_Worker = WorkerFactory.CreateWorker(m_RuntimeModel, WorkerFactory.Device.GPU, false);



#if (WEBCAM)
        m_WebcamTexture = new WebCamTexture();
        m_WebcamTexture.Play();
#else
        //Setting texture for previsualizing input
        preprocessMaterial.mainTexture = inputImage;

        //Creating a rendertexture for the output render
        var targetRT = RenderTexture.GetTemporary(inputResolutionX, inputResolutionY, 0);
        Graphics.Blit(inputImage, targetRT, postprocessMaterial);

        m_Input = new Tensor(targetRT, 3);

        //m_Input = new Tensor(1, inputResolutionY, inputResolutionX, 3);
#endif //!(WEBCAM)
        StartCoroutine(DetectionLoop());

    }

    IEnumerator DetectionLoop()
    {
        while(true)
        {
            double start_time = Time.realtimeSinceStartupAsDouble;
#if (WEBCAM)
        var targetRT = RenderTexture.GetTemporary(inputResolutionX, inputResolutionY, 0);
        Graphics.Blit(m_WebcamTexture, targetRT, postprocessMaterial);

        Tensor input = new Tensor(targetRT, 3);
#else
            Tensor input = m_Input;

#endif //!(WEBCAM)
            m_Worker.Execute(input);
            Tensor result = m_Worker.PeekOutput("Identity");
            Debug.Log(result.shape.ToString());
            double spent_time = Time.realtimeSinceStartupAsDouble - start_time;
            DebugText.Instance.lines["barracuda :"] = spent_time.ToString();

            start_time = Time.realtimeSinceStartupAsDouble;
            RenderTexture resultMask = new RenderTexture(inputResolutionX, inputResolutionY, 0);
            resultMask.enableRandomWrite = true;
            resultMask.Create();
            //result.ToRenderTexture(resultMask);
            spent_time = Time.realtimeSinceStartupAsDouble - start_time;
            postprocessMaterial.mainTexture = resultMask;
            DebugText.Instance.lines["make rt:"] = spent_time.ToString();
#if (WEBCAM)
        preprocessMaterial.mainTexture = targetRT;
#endif
            yield return new WaitForSeconds(0.03f);
        }
    }


}
  