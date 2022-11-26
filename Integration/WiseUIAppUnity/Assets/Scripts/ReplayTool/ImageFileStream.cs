using ARRC.ARRCTexture;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class ImageFileStream 
{
    Transform parentTransform;
    string dataset_path;
    List<string> frameLines = new List<string>();
    Vector2 principalPoint;
    int imageWidth, imageHeight;

    public int imageCount
    {
        get
        {
            return frameLines.Count;
        }
    }
    public ImageFileStream(string dataset_path, Transform parent)
    {
        this.dataset_path = dataset_path;
        this.parentTransform = parent;
        ////Get directory name;
        DirectoryInfo dinfo = new DirectoryInfo(dataset_path);
        /*
         * Generate Point Cloud Task 
         */
        string filename_pointcloud = string.Format(@"{0}/{1}/{2}", dataset_path, "pinhole_projection", "tsdf-pc.ply");
        PointCloudGeneratorWarpper.Instance.BuildCloud(filename_pointcloud, parent);


        ////Needs to exception handling.
        string filename_pvtxt = string.Format(@"{0}/{1}_pv.txt", dataset_path, dinfo.Name);
        List<string> lines_pvtxt = System.IO.File.ReadAllLines(filename_pvtxt).ToList();

        ////Get principal points, image size
        var firstLine = lines_pvtxt[0].Split(',').ToList();
        float[] pp = firstLine.GetRange(0, 2).Select(i => float.Parse(i)).ToArray();
        principalPoint = new Vector2(pp[0], pp[1]);
        var imageSize = firstLine.GetRange(2, 2).Select(i => int.Parse(i)).ToArray();
        imageWidth = imageSize[0];
        imageHeight = imageSize[1];
        
        frameLines = lines_pvtxt.GetRange(1, lines_pvtxt.Count - 1);
    }
    public GameObject CreateImageCameraPair(Transform parent, PVFrame pvFrame, string recordingPath, float transparency_texture)
    {
        //Convert transform.
        var rh = ChangeHandedCoordinateSystem2(pvFrame.PVtoWorldtransform);
        Vector3 position = rh.GetColumn(3);
        Quaternion rotation = rh.rotation;
        float aspectRatio = (float)pvFrame.cameraIntrinsic.imageHeight / pvFrame.cameraIntrinsic.imageWidth;
        float imageScale = 0.1f;

        //Load texture.
        string texturePath = string.Format("{0}/PV/{1}.png", recordingPath, pvFrame.timestamp);
        var texture = TextureIO.LoadTexture(texturePath);
        //var texture = TextureIO_Editor.LoadTextureUsingAssetDataBase(texturePath, 2048, true);
        float fx = pvFrame.cameraIntrinsic.focalLength.x;
        float fy = pvFrame.cameraIntrinsic.focalLength.y;
        //Create Image Object.
        GameObject go_PV = CreateImage(pvFrame.timestamp.ToString(), texture, position, rotation, transparency_texture);
        go_PV.transform.parent = parent;
        //go_PV.GetComponent<MeshRenderer>().enabled = false;
        go_PV.transform.position = position;
        go_PV.transform.rotation = rotation;
        go_PV.transform.Rotate(90, 0, 0);
        //go_PV.transform.Translate(Vector3.down * fx / 10000f, Space.Self);
        go_PV.transform.localScale = new Vector3(imageScale, imageScale, imageScale * aspectRatio);

        //Create Camera Object.
        GameObject go_cam = CreateCamera("Camera", pvFrame.cameraIntrinsic);
        go_cam.transform.parent = go_PV.transform;
        go_cam.transform.localPosition = Vector3.zero;
        go_cam.transform.localRotation = Quaternion.identity;
        go_cam.transform.Rotate(new Vector3(90, 180, 0));
        go_cam.transform.Translate(Vector3.back * fx * 2 / 1000f, Space.Self);

        //go_PV.AddComponent<ImageControllerMono>();

        return go_PV;
    }
    public GameObject CreateImageObject(int frame_id, float transparency_texture)
    {
        var pvFrame = CreatePVFrame(frame_id);
        return CreateImageCameraPair(parentTransform, pvFrame, dataset_path, transparency_texture);
    }
    public void ChangeImageObject(GameObject imgObject, int frame_id, float transparency_texture)
    {
        var pvFrame = CreatePVFrame(frame_id);
        ChangeImageTransformation(imgObject, pvFrame, dataset_path, transparency_texture);
    }
    public PVFrame CreatePVFrame(int frame_id)
    {
        var strLine = frameLines[frame_id];
        var parts = strLine.Split(',').ToList();
        long timestamp = long.Parse(parts[0]);
        float fx = float.Parse(parts[1]);
        float fy = float.Parse(parts[2]);

        CameraIntrinsic cameraIntrinsic = new CameraIntrinsic(fx, fy, principalPoint.x, principalPoint.y, imageWidth, imageHeight);
        var PVtoWorldtransformArray = parts.GetRange(3, parts.Count - 3).Select(i => float.Parse(i)).ToArray();
        return new PVFrame(cameraIntrinsic, timestamp, PVtoWorldtransformArray);
    }
    public void ChangeImageTransformation(GameObject imgObject, PVFrame pvFrame, string recordingPath, float transparency_texture)
    {
        var rh = ChangeHandedCoordinateSystem2(pvFrame.PVtoWorldtransform);
        Vector3 position = rh.GetColumn(3);
        Quaternion rotation = rh.rotation;
        
        imgObject.transform.position = position;
        imgObject.transform.rotation = rotation;
        imgObject.transform.Rotate(90, 0, 0);
        
        string texturePath = string.Format("{0}/PV/{1}.png", recordingPath, pvFrame.timestamp);
        var texture = TextureIO.LoadTexture(texturePath);
        Renderer rend = imgObject.GetComponent<Renderer>();
        rend.sharedMaterial.mainTexture = texture;
        rend.sharedMaterial.SetColor("_Color", new Color(1f, 1f, 1f, transparency_texture));

        GameObject go_cam = imgObject.transform.Find("Camera").gameObject;
        float fx = pvFrame.cameraIntrinsic.focalLength.x;
        float fy = pvFrame.cameraIntrinsic.focalLength.y;
        go_cam.transform.localPosition = Vector3.zero;
        go_cam.transform.localRotation = Quaternion.identity;
        go_cam.transform.Rotate(new Vector3(90, 180, 0));
        go_cam.transform.Translate(Vector3.back * fx * 2 / 1000f, Space.Self);

    }
    GameObject CreateImage(string imageName, Texture2D texture, Vector3 position, Quaternion rotation, float transparency_texture)
    {
        GameObject imgGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var collider = imgGO.GetComponent<MeshCollider>();
        UnityEngine.Object.DestroyImmediate(collider);

        imgGO.name = imageName;
        Renderer rend = imgGO.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Somian/Unlit/Transparent"));
        rend.sharedMaterial.mainTexture = texture;
        rend.sharedMaterial.SetColor("_Color", new Color(1f, 1f, 1f, transparency_texture));

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

    static Matrix4x4 ChangeHandedCoordinateSystem(Matrix4x4 src)
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

