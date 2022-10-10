using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugText : MonoBehaviour
{
    public float startX = 30;
    public float startY = 30;
    public int fontSize = 50;
    public int lineSpace = 1;
    //List<string> lines = new List<string>();
    public Dictionary<string, string> lines = new Dictionary<string, string>();
    public GUIStyle labelStyle;


    void OnGUI()
    {
        int line_count = 0;

        foreach (var line in lines)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = fontSize;

            //int keyWidth = (int)(line.Key.Length * fontSize);
            //int valueWidth = (int)(line.Value.Length * fontSize);
            string content = string.Format("{0}:{1}", line.Key, line.Value);
            int lineWidth = content.Length * fontSize;

            GUI.Label(new Rect(startX, startY + line_count * fontSize, lineWidth, fontSize * 1.5f), content, labelStyle);
            //GUI.Label(new Rect(startX + keyWidth, startY + line_count * (fontSize + lineSpace), valueWidth, fontSize), line.Value, labelStyle);
            line_count++;
        }

    }


    static DebugText instance;
    public static DebugText Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("DebugText");
                instance = go.AddComponent<DebugText>();
            }
            return instance;
        }
    }

}
