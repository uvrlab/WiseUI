using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugText : MonoBehaviour
{
    [SerializeField]
    public Dictionary<string, string> lines = new Dictionary<string, string>();

    private void Update()
    {
        string content = string.Empty;
        foreach (var line in lines)
        {
            content += string.Format("{0}:{1}\n", line.Key, line.Value);

        }
        GetComponent<Text>().text = content;
    }


    static DebugText _instance;
    public static DebugText Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = GameObject.Find("DebugText");
                if (obj == null)
                {
                    obj = Instantiate(Resources.Load("DebugCanvas")) as GameObject;
                    obj.transform.parent = GameObject.FindGameObjectWithTag("MainCamera").transform;
                }
                _instance = obj.transform.GetChild(0).gameObject.GetComponent<DebugText>();
                DontDestroyOnLoad(obj);
            }

            return _instance;
        }
    }


}
