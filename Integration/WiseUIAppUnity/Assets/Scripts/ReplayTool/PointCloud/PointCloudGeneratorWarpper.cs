using PointCloudExporter;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PointCloudGeneratorWarpper 
{

    static PointCloudGeneratorWarpper instance;
    public static PointCloudGeneratorWarpper Instance
    {
        get
        {
            if (instance == null)
                instance = new PointCloudGeneratorWarpper();

            return instance;
        }
    }

    string filepath;

    public GameObject CreatePointCloud(string filepath)
	{
        //get filename
        string filename = Path.GetFileName(filepath);
		GameObject pointCloudObj = new GameObject(filename);

        MeshInfos pointCloudInfo;
        LoadPointCloudFromXML(pointCloudObj, filepath, out pointCloudInfo);
        pointCloudObj.AddComponent<PointCloudInfo>();
        pointCloudObj.GetComponent<PointCloudInfo>().vertexCount = pointCloudInfo.vertexCount;

        int count = pointCloudInfo.vertexCount;
        Debug.LogFormat("The number of points of \"{0}\" : {1}", filepath, count);
        
        return pointCloudObj;
        //pointCloudObj.transform.localPosition = new Vector3 (0, 0, 0);
        //pointCloudObj.transform.localRotation = Quaternion.Euler (0, 180, 180);
    }

    void LoadPointCloudFromXML(GameObject pointCloudObj, string filepath, out MeshInfos pointcloudInfo)
    {
        PointCloudGenerator ptGen = new PointCloudGenerator();
        Material ptMaterial = new Material(Shader.Find("Unlit/PointCloud"));
        Texture2D sprite = (Texture2D)Resources.Load("Textures/Circle");
        //ptMaterial.mainTexture = sprite as Texture;
        ptMaterial.SetTexture("_MainTex", sprite);

        ptGen.Generate(filepath, ptMaterial, pointCloudObj, out pointcloudInfo);
    }
}
