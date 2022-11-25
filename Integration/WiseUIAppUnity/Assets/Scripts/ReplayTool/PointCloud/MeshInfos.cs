using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PointCloudExporter
{
	[System.Serializable]
	public class MeshInfos
	{
		public Vector3[] vertices;
		public Vector3[] normals;
		public Color[] colors;
		public int vertexCount;
		public Bounds bounds;
	}
}
