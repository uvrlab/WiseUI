using ARRC.Framework;
using ARRC.StreamRecorderVisualizer;
using Google.Protobuf.WellKnownTypes;
using SensorStream;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PlasticGui.PlasticTableColumn;
using static UnityEngine.GraphicsBuffer;

public class ReplayWindow : EditorWindow
{
    string dataset_path;

    string hostIP = "127.0.0.1";
    int port = 9091;
    bool send_data = true;

    Vector2 scrollPos = Vector2.zero;

    int current_frame_id = -1, prev_frame_id, start_frame_id, end_frame_id;

    bool load_ok = false;
    bool isConnected = false;
    public float transparency_texture = 0.5f;
    public int skip_frame_count = 1;
    public GameObject environment;
    public GameObject hololens2_camera;
    public GameObject runner;
    List<string> frameLines;
    float[] principalPoint;
    int[] imageSize;


    private void OnGUI()
    {
        LoadGUI();
        GUILayout.Space(10);
        SocketConnectionGUI();
        GUILayout.Space(10);
        FrameSelectionGUI();
        GUILayout.Space(10);
        FramePlayGUI();

    }

    void LoadGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (GUILayout.Button("Select Recording Path"))
        {
            dataset_path = EditorUtility.OpenFolderPanel("Select dataset Path", dataset_path, "");
        }

        EditorGUILayout.LabelField("Dataset Path : " + dataset_path, EditorStyles.boldLabel);

        
        if (GUILayout.Button("Load"))
        {
            environment = new GameObject(dataset_path);
            //hololens2_camera = new GameObject("hololens2_camera");
            //hololens2_camera.transform.parent = environment.transform;

            runner = GameObject.Find("Runner");
            if (runner == null)
                Debug.LogError("Cannot find GameObject \"Runner.\"");
            else
                load_ok = true;

            ////Get directory name;
            DirectoryInfo dinfo = new DirectoryInfo(dataset_path);
            /*
             * Generate Point Cloud Task 
             */
            string filename_pointcloud = string.Format(@"{0}/{1}/{2}", dataset_path, "pinhole_projection", "tsdf-pc.ply");
            PointCloudGeneratorWarpper.Instance.BuildCloud(filename_pointcloud, environment.transform);


            ////Needs to exception handling.
            string filename_pvtxt = string.Format(@"{0}/{1}_pv.txt", dataset_path, dinfo.Name);
            List<string> lines_pvtxt = System.IO.File.ReadAllLines(filename_pvtxt).ToList();

            ////Get principal points, image size
            var firstLine = lines_pvtxt[0].Split(',').ToList();
            principalPoint = firstLine.GetRange(0, 2).Select(i => float.Parse(i)).ToArray();
            imageSize = firstLine.GetRange(2, 2).Select(i => int.Parse(i)).ToArray();
            //strIntrinsic.ToList().ForEach(i => Debug.Log("intrinsic " + i.ToString()));

            frameLines = lines_pvtxt.GetRange(1, lines_pvtxt.Count - 1);

            current_frame_id = 0;
            start_frame_id = 0;
            end_frame_id = frameLines.Count - 1;
            ///frameLines.ForEach(i => AddTask(new ReadFrameTask(recordingDirectory, principalPoint[0], principalPoint[1], imageSize[0], imageSize[1], i, parentObject.transform)));

            //Debug.Log(frameLines.Count);
        }

        ScriptableObject target = this;
        SerializedObject serializedObject = new SerializedObject(target);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("environment"), true); // True means show children
        EditorGUILayout.PropertyField(serializedObject.FindProperty("runner"), true); // True means show children
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hololens2_camera"), true); // True means show children

        serializedObject.ApplyModifiedProperties(); // Remember to apply modified properties
        serializedObject.Update();

        EditorGUILayout.EndVertical();
    }

    GameObject CreateImage(int frame_id, float alpah_transparent)
    {
        var strLine = frameLines[frame_id];
        var parts = strLine.Split(',').ToList();
        long timestamp = long.Parse(parts[0]);
        float fx = float.Parse(parts[1]);
        float fy = float.Parse(parts[2]);

        CameraIntrinsic cameraIntrinsic = new CameraIntrinsic(fx, fy, principalPoint[0], principalPoint[1], imageSize[0], imageSize[1]);
        var PVtoWorldtransformArray = parts.GetRange(3, parts.Count - 3).Select(i => float.Parse(i)).ToArray();
        var pvFrame = new PVFrame(cameraIntrinsic, timestamp, PVtoWorldtransformArray);

        return ImageFileStream.CreateImageCameraPair(environment.transform, pvFrame, dataset_path, transparency_texture);
    }
    void SocketConnectionGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        hostIP = EditorGUILayout.TextField("Host IP", hostIP);
        port = EditorGUILayout.IntField("Port", port);
        if ((!isConnected && GUILayout.Button("Connect")))
        {
            if (load_ok)
            {
                try
                {
                    runner.GetComponent<SocketClientManager>().Connect(hostIP, port);
                    Debug.Log("Connected");
                    isConnected = true;
                }
                catch (SocketException e)
                {
                    Debug.LogError(e);
                }
            }
            else
            {
                Debug.Log("Load dataset first.");
            }
        }
        if ((isConnected && GUILayout.Button("Disconnect")))
        {
            try
            {
                runner.GetComponent<SocketClientManager>().Disconnect();
                
                Debug.Log("Disconnect.");
                isConnected = false;
            }
            catch (SocketException e)
            {
                Debug.LogError(e);
            }
        }

        EditorGUILayout.EndVertical();
    }
    void FrameSelectionGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        if(frameLines != null)
            EditorGUILayout.LabelField("Total frames : " + frameLines.Count);

        skip_frame_count = EditorGUILayout.IntField("Skip count", skip_frame_count);
        transparency_texture = EditorGUILayout.FloatField("Transparency", transparency_texture);

        send_data = EditorGUILayout.Toggle("Send data to server", send_data);
        EditorGUILayout.BeginHorizontal();
        current_frame_id = EditorGUILayout.IntField("Current frame id", current_frame_id);
        if ((Event.current.keyCode == KeyCode.A || GUILayout.Button("<")) && (current_frame_id - skip_frame_count) > -1)
            current_frame_id -= skip_frame_count;
        if ((Event.current.keyCode == KeyCode.D || GUILayout.Button(">")) && (current_frame_id + skip_frame_count) < frameLines.Count - 1)
            current_frame_id += skip_frame_count;

        //check current_frame_id is changed.
        if (current_frame_id != prev_frame_id && current_frame_id > -1 && current_frame_id < frameLines.Count)
        {
            if (hololens2_camera != null)
                DestroyImmediate(hololens2_camera);
            hololens2_camera = CreateImage(current_frame_id, transparency_texture);
            prev_frame_id = current_frame_id;
            if (send_data)
            {
                var texture = hololens2_camera.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
                runner.GetComponent<SocketClientManager>().SendRGBImage(current_frame_id, ToTexture2D(texture));
                runner.GetComponent<TrackHand>().Awake();
                runner.GetComponent<TrackHand>().Update(); //주의 : 지연시간이 있으므로, 바로 위에서 보낸 이미지에 대한 결과를 가지고 update하는 것이 아님.
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void FramePlayGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Not implemented yet");
        start_frame_id = EditorGUILayout.IntField("Start frame id", start_frame_id);
        end_frame_id = EditorGUILayout.IntField("End framd id", end_frame_id);
        if (GUILayout.Button("Play"))
        {

        }
        EditorGUILayout.EndVertical();
    }

    public void LoadPreperence()
    {
        Debug.Log("LoadPreperence");
        if (PlayerPrefs.HasKey("hostIP"))
            hostIP = PlayerPrefs.GetString("hostIP");
        if (PlayerPrefs.HasKey("port"))
            port = PlayerPrefs.GetInt("port");
        if (PlayerPrefs.HasKey("dataset_path"))
        {
            dataset_path = PlayerPrefs.GetString("dataset_path");
            Debug.Log(dataset_path);
        }

        Repaint();

    }
    private void OnDestroy()
    {
        SavePreperence();
    }
    public void SavePreperence()
    {
        Debug.Log("SavePreperence");
        PlayerPrefs.SetString("hostIP", hostIP);
        PlayerPrefs.SetInt("port", port);
        PlayerPrefs.SetString("dataset_path", dataset_path);
        Debug.Log(dataset_path);
    }
    public Texture2D ToTexture2D(Texture texture)
    {
        int width = texture.width;
        int height = texture.height;
        //save the active render texture
        RenderTexture temp = RenderTexture.active;

        //create new render texture and copy from the target texture
        RenderTexture copiedRenderTexture = new RenderTexture(width, height, 0);
        Graphics.Blit(texture, copiedRenderTexture, new Material(Shader.Find("FlipShader")));
        //Debug.Log(copiedRenderTexture.format.ToString());
        //change active render texture
        RenderTexture.active = copiedRenderTexture;

        //copy to texture 2d
        Texture2D convertedImage = new Texture2D(width, height, TextureFormat.BGRA32, false);
        convertedImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        convertedImage.Apply();

        RenderTexture.active = temp;

        return convertedImage;

    }

    [MenuItem("WiseUI/Replay Tool")]
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
                instance.LoadPreperence();

                return instance;
            }
        }
    }
}

