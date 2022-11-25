using ARRC.Framework;
using ARRC.StreamRecorderVisualizer;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ReplayWindow : EditorWindow
{
    string directoryPath = @"G:\HoloLense2_RecordingData\KI_AHAT\static";
    string hostIP = "127.0.0.1";
    int port = 9091;
    Vector2 scrollPos = Vector2.zero;

    int frame_id;


    private void OnGUI()
    {
        SelectFolderGUI();
        GUILayout.Space(10);
        
        hostIP = EditorGUILayout.TextField("Host IP", hostIP);
        port = EditorGUILayout.IntField("Port", port);
        if (GUILayout.Button("Connect"))
        {
            
        }

        frame_id = EditorGUILayout.IntField("FrameID", frame_id);


    }

    void SelectFolderGUI()
    {
        if (GUILayout.Button("Select Recording Path"))
        {
            //EditorUtility.DisplayDialog("Select Texture", "You must select a texture first!", "OK");
            directoryPath = EditorUtility.OpenFolderPanel("Select Recording Path", Application.streamingAssetsPath, "");
        }

        EditorGUILayout.LabelField("Recording directory : " + directoryPath, EditorStyles.boldLabel);
    }
    
    [MenuItem("WiseUI/Simulator")]
    public static ReplayWindow OpenWindow()
    {
        return Instance;
    }

    static ReplayWindow instance;

    public static ReplayWindow Instance
    {
        get
        {
            if (instance)
                return instance;
            else
            {
                instance = GetWindow<ReplayWindow>(false, "WiseUI Simulator");
                return instance;
            }
        }
    }


}
