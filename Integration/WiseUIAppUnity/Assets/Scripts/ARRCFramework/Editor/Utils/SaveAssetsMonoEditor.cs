using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ARRC.Framework
{
    [CustomEditor(typeof(SaveAssetsMono))]
    public class SaveAssetsMonoEditor : Editor
    {
        SaveAssetsMono mono;
        private void OnEnable()
        {
            mono = (SaveAssetsMono)target;
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Write meshfilter"))
                WriteMesh(mono.GetComponent<MeshFilter>().mesh, mono.filepath);
        }
        void WriteMesh(Mesh mesh, string filepath)
        {
            AssetDatabase.CreateAsset(mesh, ARRCPaths.GetAssetPath(filepath));
            AssetDatabase.SaveAssets();
        }
    }

}
