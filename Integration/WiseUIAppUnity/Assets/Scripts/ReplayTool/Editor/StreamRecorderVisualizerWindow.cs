using ARRC.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ARRC.StreamRecorderVisualizer
{
  
    public class StreamRecorderVisualizerWindow : EditorWindow
    {
        SRVTaskManager taskManager = SRVTaskManager.Instance;

        string directoryPath = @"G:\HoloLense2_RecordingData\KI_AHAT\static";
        string outputFolderName = "test";
        Vector2 scrollPos = Vector2.zero;

        //Play Option.
        void OnGUI()
        {
            if (taskManager.IsEmpty)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                SelectFolderGUI();
                EditorGUILayout.EndVertical();
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Start"))
                    StartCapture();
                GUILayout.EndHorizontal();
            }
            else
                OnGenerate();
        }
        void SelectFolderGUI()
        {
            outputFolderName = EditorGUILayout.TextField("Output Dir Name", outputFolderName);

            string path_data = ARRCPaths.GetResultFolder_Root(outputFolderName);
            EditorGUILayout.LabelField("Output directory : " + path_data, EditorStyles.boldLabel);

            if (GUILayout.Button("Select Recording Path"))
            {
                //EditorUtility.DisplayDialog("Select Texture", "You must select a texture first!", "OK");
                directoryPath = EditorUtility.OpenFolderPanel("Select Recording Path", Application.streamingAssetsPath, "");
            }

            EditorGUILayout.LabelField("Recording directory : " + directoryPath, EditorStyles.boldLabel);
        }

        void StartCapture()
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                Debug.LogError("Wrong directory path");
                return;
            }
            if (string.IsNullOrWhiteSpace(outputFolderName))
            {
                Debug.LogError("Wrong output folder");
                return;
            }

            //Task를 구성.
            taskManager = new SRVTaskManager();
            taskManager.Initialize(directoryPath, Instance);

            //PointCloudGeneratorWarpper.Instance.BuildCloud(directoryPath, parent.transform);
        }
        void OnGenerate()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            int phaseCount = taskManager.TaskCount;
            float progress = 0;

            for (int i = 0; i < phaseCount; i++)
            {
                if (i < taskManager.ActiveIndex)
                    progress = 1;

                else if (i == taskManager.ActiveIndex)
                    progress = taskManager.ProgressOfActiveTask;

                else if (i > taskManager.ActiveIndex)
                    progress = 0;

                float totalSize = taskManager.TotalSizeOf(i);
                int completedSize = Mathf.FloorToInt(totalSize * progress);
                string phaseState = taskManager.StateOf(i);
                TaskType taskType = taskManager.TaskTypeOf(i);

                GUILayout.Label(phaseState);
                string strProgress = Mathf.FloorToInt(progress * 100f).ToString() + "%";

                Rect r = EditorGUILayout.BeginVertical();
                EditorGUI.ProgressBar(r, progress, strProgress);
                GUILayout.Space(16);
                EditorGUILayout.EndVertical();
            }
            GUILayout.Space(16);
            GUILayout.EndScrollView();

            if (!taskManager.IsCompleted && GUILayout.Button("Cancel"))
            {
                taskManager.Dispose();
            }

            if (taskManager.IsCompleted && GUILayout.Button("Done"))
            {
                taskManager.Dispose();
            }

            GUILayout.Label("Warning: Keep this window open.");
            Repaint();
        }
        void Update()
        {
            taskManager.Update();
        }

        [MenuItem("WiseUI/Stream Recorder Visualizer")]
        public static StreamRecorderVisualizerWindow OpenWindow()
        {
            return Instance;
        }

        static StreamRecorderVisualizerWindow instance;

        public static StreamRecorderVisualizerWindow Instance
        {
            get
            {
                if (instance)
                    return instance;
                else
                {
                    instance = GetWindow<StreamRecorderVisualizerWindow>(false, "Stream Recorder Visualizer");
                    return instance;
                }
            }
        }

    }
    public enum ResearchModeSensorType
    {
        LEFT_FRONT,
        LEFT_LEFT,
        RIGHT_FRONT,
        RIGHT_RIGHT,
        DEPTH_AHAT,
        DEPTH_LONG_THROW,
        IMU_ACCEL,
        IMU_GYRO,
        IMU_MAG
    };
    public enum StreamTypes
    {
        PV,  // RGB
        EYE  // Eye gaze tracking
             // Hands captured by default
    };
}
