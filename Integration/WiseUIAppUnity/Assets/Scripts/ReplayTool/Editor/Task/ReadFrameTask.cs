using ARRC.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ARRC.ARRCTexture;

namespace ARRC.StreamRecorderVisualizer
{
    public class ReadFrameTask : Task
    {
        string strLine;
        string recordingPath;
        float ppx, ppy;
        int imageWidth, imageHeight;

        Transform parent;
        GameObject go_PV, go_cam;
        bool removeGOonDispose;

        public ReadFrameTask(string recordingPath, float ppx, float ppy, int imageWidth, int imageHeight, string strLine, Transform parent)
        {
            this.recordingPath = recordingPath;
            this.ppx = ppx;
            this.ppy = ppy;
            this.imageWidth = imageWidth;
            this.imageHeight = imageHeight;
            this.strLine = strLine;
            this.parent = parent;
        }
        
        public override void Enter()
        {
            base.Enter();

            //Parsing string to PVFrame.
            var parts = strLine.Split(',').ToList();
            long timestamp = long.Parse(parts[0]);
            float fx = float.Parse(parts[1]);
            float fy = float.Parse(parts[2]);

            CameraIntrinsic cameraIntrinsic = new CameraIntrinsic(fx, fy, ppx, ppy, imageWidth, imageHeight);
            var PVtoWorldtransformArray = parts.GetRange(3, parts.Count - 3).Select(i => float.Parse(i)).ToArray();
            var pvFrame = new PVFrame(cameraIntrinsic, timestamp, PVtoWorldtransformArray);

            CreateImageCameraPair(pvFrame);

            isCompleted = true;
            progress = 1;
        }
     
        void CreateImageCameraPair(PVFrame pvFrame)
        {
            //Convert transform.
            var rh = ChangeHandedCoordinateSystem2(pvFrame.PVtoWorldtransform);
            Vector3 position = rh.GetColumn(3);
            Quaternion rotation = rh.rotation;
            float aspectRatio = (float) pvFrame.cameraIntrinsic.imageHeight / pvFrame.cameraIntrinsic.imageWidth;
            float imageScale = 0.1f;

            //Load texture.
            string texturePath = string.Format("{0}/PV/{1}.png", recordingPath, pvFrame.timestamp);
            var texture = TextureIO.LoadTexture(texturePath);
            //var texture = TextureIO_Editor.LoadTextureUsingAssetDataBase(texturePath, 2048, true);
            float fx  = pvFrame.cameraIntrinsic.focalLength.x;
            float fy = pvFrame.cameraIntrinsic.focalLength.y;
            //Create Image Object.
            go_PV = CreateImage(pvFrame.timestamp.ToString(), texture, position, rotation);
            go_PV.transform.parent = parent;
            go_PV.GetComponent<MeshRenderer>().enabled = false;
            go_PV.transform.position = position;
            go_PV.transform.rotation = rotation;
            go_PV.transform.Rotate(90, 0, 0);
            //go_PV.transform.Translate(Vector3.down * fx / 10000f, Space.Self);
            go_PV.transform.localScale = new Vector3(imageScale, imageScale, imageScale * aspectRatio);
            
            //Create Camera Object.
            go_cam = CreateCamera("Camera", pvFrame.cameraIntrinsic);
            go_cam.transform.parent = go_PV.transform;
            go_cam.transform.localPosition = Vector3.zero;
            go_cam.transform.localRotation = Quaternion.identity;
            go_cam.transform.Rotate(new Vector3(90, 180, 0));
            go_cam.transform.Translate(Vector3.back * fx * 2/ 1000f, Space.Self);

            go_PV.AddComponent<ImageControllerMono>();
        }
        GameObject CreateImage(string imageName, Texture2D texture, Vector3 position, Quaternion rotation)
        {
            GameObject imgGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var collider = imgGO.GetComponent<MeshCollider>();
            Object.DestroyImmediate(collider);

            imgGO.name = imageName;
            Renderer rend = imgGO.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Somian/Unlit/Transparent"));
            rend.sharedMaterial.mainTexture = texture;
            rend.sharedMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0.4f));

            return imgGO;
        }
        GameObject CreateCamera(string camName, CameraIntrinsic intrinsic)
        {
            //Set extrinsic.
            //Matrix4x4 extrinsic_rightHaned = extrinsic;// ChangeHandedCoordinateSystem2(extrinsic);
            //extrinsic_rightHaned *=  Matrix4x4.Scale(new Vector3(1, 1, 1) * 100f);

            //Create cameraObject.
            GameObject camGO = new GameObject(camName);
            Camera cam = camGO.AddComponent<Camera>();
            var radian = 2 * Mathf.Atan(intrinsic.imageWidth / (2 * intrinsic.focalLength.x));
            //cam.fieldOfView = radian * Mathf.Rad2Deg;
            cam.fieldOfView = 27;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 100f;
            return camGO;
        }

        Matrix4x4 ChangeHandedCoordinateSystem(Matrix4x4 src)
        {
            Matrix4x4 dst = src;

            dst.SetRow(1, src.GetRow(2));
            dst.SetRow(2, src.GetRow(1));

            Vector4 tempCol = dst.GetColumn(1);
            dst.SetColumn(1, dst.GetColumn(2));
            dst.SetColumn(2, tempCol);

            return dst;
        }


        Matrix4x4 ChangeHandedCoordinateSystem2(Matrix4x4 src)
        {
            Vector3 pos = src.GetColumn(3);
            Vector3 eulerAngle = src.rotation.eulerAngles;

            eulerAngle.y *= -1.0f;
            eulerAngle.z *= -1.0f;

            Vector3 eulerAngleInRad;
            eulerAngleInRad.x = Mathf.PI * eulerAngle.x / 180.0f;
            eulerAngleInRad.y = Mathf.PI * eulerAngle.y / 180.0f;
            eulerAngleInRad.z = Mathf.PI * eulerAngle.z / 180.0f;

            Matrix4x4 vX;
            vX.m00 = 1.0f; vX.m01 = 0.0f; vX.m02 = 0.0f; vX.m03 = 0.0f;
            vX.m10 = 0.0f; vX.m11 = Mathf.Cos(eulerAngleInRad.x); vX.m12 = -Mathf.Sin(eulerAngleInRad.x); vX.m13 = 0.0f;
            vX.m20 = 0.0f; vX.m21 = Mathf.Sin(eulerAngleInRad.x); vX.m22 = Mathf.Cos(eulerAngleInRad.x); vX.m23 = 0.0f;
            vX.m30 = 0.0f; vX.m31 = 0.0f; vX.m32 = 0.0f; vX.m33 = 1.0f;

            Matrix4x4 vY;
            vY.m00 = Mathf.Cos(eulerAngleInRad.y); vY.m01 = 0.0f; vY.m02 = Mathf.Sin(eulerAngleInRad.y); vY.m03 = 0.0f;
            vY.m10 = 0.0f; vY.m11 = 1.0f; vY.m12 = 0.0f; vY.m13 = 0.0f;
            vY.m20 = -Mathf.Sin(eulerAngleInRad.y); vY.m21 = 0.0f; vY.m22 = Mathf.Cos(eulerAngleInRad.y); vY.m23 = 0.0f;
            vY.m30 = 0.0f; vY.m31 = 0.0f; vY.m32 = 0.0f; vY.m33 = 1.0f;

            Matrix4x4 vZ;
            vZ.m00 = Mathf.Cos(eulerAngleInRad.z); vZ.m01 = -Mathf.Sin(eulerAngleInRad.z); vZ.m02 = 0.0f; vZ.m03 = 0.0f;
            vZ.m10 = Mathf.Sin(eulerAngleInRad.z); vZ.m11 = Mathf.Cos(eulerAngleInRad.z); vZ.m12 = 0.0f; vZ.m13 = 0.0f;
            vZ.m20 = 0.0f; vZ.m21 = 0.0f; vZ.m22 = 1.0f; vZ.m23 = 0.0f;
            vZ.m30 = 0.0f; vZ.m31 = 0.0f; vZ.m32 = 0.0f; vZ.m33 = 1.0f;

            Matrix4x4 vR3 = vY * vX * vZ;
            Vector4 vP = new Vector4(-pos.x, pos.y, pos.z, 1);

            Matrix4x4 dst = vR3;
            dst.SetColumn(3, vP);

            return dst;
        }
        public override void Start()
        {
            base.Start();

        }
        public override void Dispose()
        {
            base.Dispose();

        }

    }
    public class CameraIntrinsic
    {
        public Vector2 focalLength;
        public Vector2 principalPoint;
        public int imageWidth;
        public int imageHeight;

        public CameraIntrinsic(float fx, float fy, float ppx, float ppy, int width, int height)
        {
            focalLength = new Vector2(fx, fy);
            principalPoint = new Vector2(ppx, ppy);
            imageWidth = width;
            imageHeight = height;
        }

        public Matrix4x4 toMatrix()
        {
            //Set intrinsic.
            Matrix4x4 intrinsic = Matrix4x4.identity;
            intrinsic.m00 = (float)focalLength.x;
            intrinsic.m11 = (float)focalLength.y;
            intrinsic.m02 = (float)principalPoint.x;
            intrinsic.m12 = (float)principalPoint.y;

            return intrinsic;
        }
    }
    public class PVFrame
    {
        public long timestamp;
        public CameraIntrinsic cameraIntrinsic;
        public Matrix4x4 PVtoWorldtransform;

        public PVFrame(CameraIntrinsic cameraIntrinsic, long timestamp, float[] PVtoWorldtransformArray)
        {
            this.cameraIntrinsic = cameraIntrinsic;
            this.timestamp = timestamp;
            PVtoWorldtransform = Matrix4x4.identity;
            PVtoWorldtransform.SetRow(0, new Vector4(PVtoWorldtransformArray[0], PVtoWorldtransformArray[1], PVtoWorldtransformArray[2], PVtoWorldtransformArray[3]));
            PVtoWorldtransform.SetRow(1, new Vector4(PVtoWorldtransformArray[4], PVtoWorldtransformArray[5], PVtoWorldtransformArray[6], PVtoWorldtransformArray[7]));
            PVtoWorldtransform.SetRow(2, new Vector4(PVtoWorldtransformArray[8], PVtoWorldtransformArray[9], PVtoWorldtransformArray[10], PVtoWorldtransformArray[11]));
            PVtoWorldtransform.SetRow(3, new Vector4(PVtoWorldtransformArray[12], PVtoWorldtransformArray[13], PVtoWorldtransformArray[14], PVtoWorldtransformArray[15]));
        }

    }

}