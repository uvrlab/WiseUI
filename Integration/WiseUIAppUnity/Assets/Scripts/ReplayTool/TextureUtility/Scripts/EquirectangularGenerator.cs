using UnityEngine;

namespace ARRC.ARRCTexture
{
    public class EquirectangularGenerator
    {
        public static RenderTexture GetEquirectangularTexture(int equirectangularWidth, int equirectangularHeight, int cubemapWidth, GameObject camObject, Shader replaceShader = null)
        {
            RenderTexture backup = RenderTexture.active;
            //Render to cubemap.
            //startTime = Time.realtimeSinceStartup;
            Cubemap cubeMap = new Cubemap(cubemapWidth, TextureFormat.RGBA32, false);

            if (replaceShader != null)
                camObject.GetComponent<Camera>().SetReplacementShader(replaceShader, null);

            camObject.GetComponent<Camera>().RenderToCubemap(cubeMap);

            //Conver to equi texture.
            Shader cubemapToEquirectangularShader = Shader.Find("Hidden/CubemapToEquirectangular");
            Material cubemapToEquirectangularMaterial = new Material(cubemapToEquirectangularShader);
            Vector3 eulerRot = camObject.transform.rotation.eulerAngles;
            cubemapToEquirectangularMaterial.SetFloat("_RotationX", eulerRot.x);
            cubemapToEquirectangularMaterial.SetFloat("_RotationY", eulerRot.y);
            cubemapToEquirectangularMaterial.SetFloat("_RotationZ", eulerRot.z);
            //Debug.Log("Time to render to cubemap: " + (Time.realtimeSinceStartup - startTime) + " sec");

            //startTime = Time.realtimeSinceStartup;
            RenderTexture rtex_equi = new RenderTexture(equirectangularWidth, equirectangularHeight, 0);
            rtex_equi.enableRandomWrite = true;
            Graphics.Blit(cubeMap, rtex_equi, cubemapToEquirectangularMaterial);
            UnityEngine.Object.DestroyImmediate(cubeMap);
            //Texture2D equiMap = new Texture2D(equirectangularWidth, equirectangularHeight, TextureFormat.ARGB32, false);
            //equiMap.ReadPixels(new Rect(0, 0, equirectangularWidth, equirectangularHeight), 0, 0, false);
            //equiMap.Apply();

            //Debug.Log("Time to built equirectangular projection: " + (Time.realtimeSinceStartup - startTime) + " sec");
            RenderTexture.active = backup;

            return rtex_equi;
        }

        public static RenderTexture GetEquirectangularUnlitColorTexture(int equirectangularWidth, int equirectangularHeight, int cubemapWidth, GameObject camObject)
        {
            //Render to cubemap.
            Shader replaceShader = Shader.Find("Unlit/Color");
            return GetEquirectangularTexture(equirectangularWidth, equirectangularHeight, cubemapWidth, camObject, replaceShader);
        }

        public static RenderTexture GetEquirectangularPsudoDepthTexture(int equirectangularWidth, int equirectangularHeight, int cubemapWidth, GameObject camObject)
        {
            //Render to cubemap using depth shader.
            Shader replace = Shader.Find("Hidden/PseudoDepth");

            return GetEquirectangularTexture(equirectangularWidth, equirectangularHeight, cubemapWidth, camObject, replace);

        }
        public static RenderTexture GetEquirectangularTurboColorTexture(int equirectangularWidth, int equirectangularHeight, int cubemapWidth, GameObject camObject)
        {
            //Render to cubemap using depth shader.
            Shader replace = Shader.Find("Hidden/TurboColor");

            return GetEquirectangularTexture(equirectangularWidth, equirectangularHeight, cubemapWidth, camObject, replace);

        }
        public static RenderTexture GetEquirectangularContourTexture(int equirectangularWidth, int equirectangularHeight, int cubemapWidth, GameObject camObject, float thickness, Color outlineColor)
        {

            float outlineWidth = thickness * 7f * (equirectangularWidth / 2048f);

            //Render to cubemap using simple shader.
            Shader drawSimpleShader = Shader.Find("Custom/DrawSimple");
            RenderTexture equiTexture = GetEquirectangularTexture(equirectangularWidth, equirectangularHeight, cubemapWidth, camObject, drawSimpleShader);

            //conver to outline texture.
            Shader outlineShader = Shader.Find("Custom/Post Outline");
            Material outlineMaterial = new Material(outlineShader);
            outlineMaterial.SetFloat("_NumberOfIterations", outlineWidth);
            outlineMaterial.SetColor("_OutlineColor", outlineColor);

            RenderTexture backup = RenderTexture.active;

            RenderTexture rtex_outline = new RenderTexture(equirectangularWidth, equirectangularHeight, 0);
            rtex_outline.enableRandomWrite = true;

            Graphics.Blit(equiTexture, rtex_outline, outlineMaterial);
            //Debug.Log("Time to built equirectangular projection: " + (Time.realtimeSinceStartup - startTime) + " sec");

            RenderTexture.active = backup;
            return rtex_outline;
        }
        public static Texture2D GetOverayTexture(int equirectangularWidth, int equirectangularHeight, int cubemapWidth, GameObject camObject, 
            string fullpath_image, string fullpath_label, bool showWindow)
        {
            //StreetViewItem streetViewInfo = targetObject.GetComponent<StreetViewItemMono>().streetviewInfo;

            //if (streetViewInfo == null)
            //    throw new Exception("The gameobject is not include \"StreetViewItemMono\" Component. : " + targetObject.name);

            //string fullpath_image = ARRCPaths.GetColorImagePathFromStreetViewInfo(streetViewInfo.folderName, streetViewInfo.loadedZoom, streetViewInfo.panoid + ".png");
            //string fullpath_label = ARRCPaths.GetLabelImagePath(streetViewInfo.panoid + ".png");

            //int cameraLayerMask_sphere_and_building = (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("Building"));
            //int cameraLayerMask_building = (1 << LayerMask.NameToLayer("Building"));

            //GameObject cam_sphere_and_building = CreateCameraForRenderTexture(pose, cameraLayerMask_sphere_and_building, cameraNearPlane, cameraFarPlane, -1, Color.black);
            //GameObject cam_building = CreateCameraForRenderTexture(pose, cameraLayerMask_building, cameraNearPlane, cameraFarPlane, -1, Color.black);

            /*** 이미지와 빌딩 렌더링 ***/
            //1.이미지를 로드한 후 빌딩을 따로 렌더링해서 합치는 방법.
            Texture2D image_tex2d = TextureIO.LoadTexture(fullpath_image, true, equirectangularWidth);
            //Texture2D image_tex2d = OutlineDetectionWarpper.LoadTexture(fullpath_label, equirectangularWidth);
            RenderTexture hypothsis_building = GetEquirectangularUnlitColorTexture(equirectangularWidth, equirectangularHeight, cubemapWidth, camObject);
            Texture2D hypothsis_building_tex2d = TextureIO.ConvertRenderTexturetoTexture2D(hypothsis_building, TextureFormat.RGBA32);
            Texture2D background_tex2d = TextureIO.TransparentOverayTexture(image_tex2d, 1, hypothsis_building_tex2d, 0.5f, false);

            //2.building과 image를 동시에 렌더링 하는 방법.
            //RenderTexture background = EquirectangularGenerator.GetEquirectangularUnlitColorTexture(equirectangularWidth, equirectangularHeight, cubemapWidth, cam_sphere_and_building);
            //Texture2D background_tex2d = EquirectangularGenerator.ConvertRenderTexturetoTexture2D(background, TextureFormat.RGBA32);

            /*** outline 렌더링 ***/
            RenderTexture hypothsis_outline = GetEquirectangularContourTexture(equirectangularWidth, equirectangularHeight, cubemapWidth, camObject, 2, new Color(1, 0, 1, 1));
            Texture2D hypothsis_outline_tex2d = TextureIO.ConvertRenderTexturetoTexture2D(hypothsis_outline, TextureFormat.RGBA32);
            Texture2D observation_outline_tex2d = OutlineDetectionWarpper.GetOutlineTexture(fullpath_label, true, equirectangularWidth, new Color(0, 1, 1, 1), 2);


            /*** overay하여 결과 이미지 생성 ***/
            //Texture2D overay_outline = OutlineDetectionWarpper.OverayTexture(observation_outline_tex2d, 1, hypothsis_outline_tex2d,1, filename);
            Texture2D overay_outline = TextureIO.TransparentOverayTexture(observation_outline_tex2d, 1, hypothsis_outline_tex2d, 1, false);
            //Texture2D overay_segment = OutlineDetectionWarpper.OverayTexture(background_tex2d, 1, hypothsis_segment_tex2d, 0.5f, filename);
            //Texture2D overay_segment = OutlineDetectionWarpper.TransparentOverayTexture(background_tex2d, 1, hypothsis_segment_tex2d, 1, filename);

            Texture2D overay_final = TextureIO.OverayTexture(background_tex2d, 1, overay_outline, 1, showWindow, fullpath_label);
            //EquirectangularGenerator.WriteTexture(overay, filename + ".png");
            //EquirectangularGenerator.WriteEquirectangularTexture(targetObject, cameraLayerMask_overay, filename + ".png", 1024, 512, 512, cameraNearPlane, cameraFarPlane);

            // release
            hypothsis_building.Release();
            hypothsis_outline.Release();

            UnityEngine.Object.DestroyImmediate(image_tex2d);
            UnityEngine.Object.DestroyImmediate(background_tex2d);
            UnityEngine.Object.DestroyImmediate(hypothsis_outline_tex2d);
            UnityEngine.Object.DestroyImmediate(observation_outline_tex2d);
            UnityEngine.Object.DestroyImmediate(overay_outline);

            //UnityEngine.Object.DestroyImmediate(cam_sphere_and_building);
            //UnityEngine.Object.DestroyImmediate(cam_building);

            return overay_final;
        }
        public static GameObject CreateCameraForRenderTexture(CameraClearFlags clearFlag, int _cullingMask, float _cameraNearPlane, float _cameraFarPlane, int _depth, Color backgroundColor, Transform initialTransform = null)
        {
            bool cameraUseOcclusion = false;

            // Create temporary camera for rendering
            GameObject go = new GameObject("CubemapCamera");
            //go.hideFlags = HideFlags.HideAndDontSave;

            go.AddComponent<Camera>();
            go.GetComponent<Camera>().clearFlags = clearFlag;
            go.GetComponent<Camera>().backgroundColor = backgroundColor;
            go.GetComponent<Camera>().cullingMask = _cullingMask;
            go.GetComponent<Camera>().nearClipPlane = _cameraNearPlane;
            go.GetComponent<Camera>().farClipPlane = _cameraFarPlane;
            go.GetComponent<Camera>().useOcclusionCulling = cameraUseOcclusion;
            go.GetComponent<Camera>().depth = _depth;

            // place it on the object
            if(initialTransform != null)
            {
                go.transform.position = initialTransform.position;
                go.transform.rotation = initialTransform.rotation;
            }

            return go;
        }

      
    }
}
