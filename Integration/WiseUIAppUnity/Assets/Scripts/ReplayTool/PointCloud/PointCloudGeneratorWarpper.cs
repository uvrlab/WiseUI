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

    public void BuildCloud(string filepath, Transform parent = null)
	{
        //get filename
        string filename = Path.GetFileName(filepath);
		GameObject pointCloudObj = new GameObject(filename);
		CreatePointCloud (pointCloudObj, filepath);
        if(parent != null)
            pointCloudObj.transform.parent = parent;
        //pointCloudObj.transform.localPosition = new Vector3 (0, 0, 0);
        //pointCloudObj.transform.localRotation = Quaternion.Euler (0, 180, 180);
	}

	void CreatePointCloud(GameObject pointCloudObj, string filepath)
	{
		MeshInfos pointCloudInfo;
		LoadPointCloudFromXML (pointCloudObj, filepath, out pointCloudInfo);
        pointCloudObj.AddComponent<PointCloudInfo>();
        pointCloudObj.GetComponent<PointCloudInfo>().vertexCount = pointCloudInfo.vertexCount;
       
        int count = pointCloudInfo.vertexCount;
        Debug.Log ("1:"+count.ToString());
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
