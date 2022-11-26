using ARRC.ARRCTexture;
using System;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

public class ReplayWindow : EditorWindow
{
    string dataset_path;
    string hostIP = "127.0.0.1";
    int port = 9091;
    
    Vector2 scrollPos = Vector2.zero;

    [SerializeField]
    ImageFileStream imageFileStream = new ImageFileStream();
    int current_frame_id = -1, prev_frame_id = -1, end_frame_id;

    bool send2server = true;
    bool isLoaded = false;
    bool isConnected = false;
    bool isPlaying = false;

    [Range(0, 1.0f)]
    public float transparency_texture = 0.5f;
    public int skip_frame_count = 1;

    public GameObject environment;
    public GameObject frame;
    public GameObject runner;

    private void OnGUI()
    {
        LoadGUI();
        if (isLoaded)
        {
            GUILayout.Space(10);
            SocketConnectionGUI();
            GUILayout.Space(10);
            FrameSelectionGUI();
            GUILayout.Space(10);
        }
   
    }

    void LoadGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (GUILayout.Button("Select Recording Path"))
        {
            dataset_path = EditorUtility.OpenFolderPanel("Select dataset Path", dataset_path, "");
        }

        EditorGUILayout.LabelField("Dataset Path : " + dataset_path, EditorStyles.boldLabel);

        if (!string.IsNullOrEmpty(dataset_path))
        {
            if (GUILayout.Button("Load"))
            {
                try
                {
                    imageFileStream = new ImageFileStream(dataset_path);
                    environment = imageFileStream.CreateEnvironment();

                    frame = null;
                    runner = GameObject.Find("Runner");
                    if (runner == null)
                    {
                        runner = new GameObject("Runner");
                        runner.AddComponent<SocketClientManager>();
                        runner.AddComponent<TrackHand>();
                        runner.AddComponent<ARRCObjectronDetector>();
                    }
                    isLoaded = true;

                    current_frame_id = -1;
                    prev_frame_id = -1;
                    end_frame_id = imageFileStream.imageCount - 1;
                }
                catch(Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            ScriptableObject target = this;
            SerializedObject serializedObject = new SerializedObject(target);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("environment"), true); // True means show children
            EditorGUILayout.PropertyField(serializedObject.FindProperty("runner"), true); // True means show children
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frame"), true); // True means show children
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transparency_texture"), true); // True means show children
            if (EditorGUI.EndChangeCheck())
            {
                if (frame != null)
                {
                    Renderer rend = frame.GetComponent<Renderer>();
                    rend.sharedMaterial.SetColor("_Color", new Color(1f, 1f, 1f, transparency_texture));
                }
            }
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("imageFileStream"), true); // True means show children

            serializedObject.ApplyModifiedProperties(); // Remember to apply modified properties
            serializedObject.Update();
        }

        EditorGUILayout.EndVertical();
    }

    
    void SocketConnectionGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        hostIP = EditorGUILayout.TextField("Host IP", hostIP);
        port = EditorGUILayout.IntField("Port", port);
        
        
        if ((!isConnected && GUILayout.Button("Connect")))
        {
            if (isLoaded)
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

        if(imageFileStream != null)
            EditorGUILayout.LabelField("Total frames : " + imageFileStream.imageCount, EditorStyles.boldLabel);

        send2server = EditorGUILayout.Toggle("Send data to server", send2server);
        skip_frame_count = EditorGUILayout.IntField("Skip count", skip_frame_count);

        EditorGUILayout.BeginHorizontal();
        current_frame_id = EditorGUILayout.IntField("Current frame id", current_frame_id);
        if ((Event.current.keyCode == KeyCode.A || GUILayout.Button("<")) && (current_frame_id - skip_frame_count) > -1)
            current_frame_id -= skip_frame_count;
        if ((Event.current.keyCode == KeyCode.D || GUILayout.Button(">")) && (current_frame_id + skip_frame_count) < imageFileStream.imageCount - 1)
            current_frame_id += skip_frame_count;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        end_frame_id = EditorGUILayout.IntField("End framd id", end_frame_id);
        if ((!isPlaying && GUILayout.Button("Play")) || (isPlaying && GUILayout.Button("Stop")))
            isPlaying = !isPlaying;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
    void SelectFrame()
    {
        if (!(current_frame_id > -1 && current_frame_id < imageFileStream.imageCount))
            throw new Exception("Invalid file index.");

        if (frame == null)
            frame = imageFileStream.CreateImageObject(current_frame_id, transparency_texture);
        else
            imageFileStream.ChangeImageObject(frame, current_frame_id, transparency_texture);

        prev_frame_id = current_frame_id;
        if (send2server)
        {
            var texture = frame.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
            runner.GetComponent<SocketClientManager>().SendRGBImage(current_frame_id, texture.ToTexture2D(TextureFormat.BGRA32, Shader.Find("FlipShader")));
            runner.GetComponent<TrackHand>().Awake();
            runner.GetComponent<TrackHand>().Update(); //주의 : 결과를 받기까지 지연시간이 있으므로, 바로 위에서 보낸 이미지에 대한 결과가 아님.
        }
    }
    void Update()
    {
        //check current_frame_id is changed.
        if (current_frame_id != prev_frame_id && current_frame_id > -1 && current_frame_id < imageFileStream.imageCount)
            SelectFrame();

        if (isPlaying)
        {
            if (current_frame_id + skip_frame_count < imageFileStream.imageCount - 1)
                current_frame_id += skip_frame_count;
            else
                isPlaying = false;
            
            Repaint();
        }
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

