using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
//using PointCloudExporter;

//[System.Serializable]
public class PointCloudInfo : MonoBehaviour{
    int _vertexCount;
    public int vertexCount
    {
        set
        {
            _vertexCount = value;
        }
        get
        {
            return _vertexCount;
        }
    }


    public Vector3[] GetVertices()
    {
        Vector3[] destVertices = new Vector3[vertexCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3[] vertices = transform.GetChild(i).GetComponent<MeshFilter>().sharedMesh.vertices;
            Array.Copy(vertices, 0,  destVertices, vertices.Length*i, vertices.Length);
        }
        Debug.Log("2:" + destVertices.Length.ToString());   
        return destVertices;
    }


}




