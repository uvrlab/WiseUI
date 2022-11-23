using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public class ServerTest : MonoBehaviour
{
    //SocketServer server = new SocketServer();
    SocketClient client = new SocketClient();

    // Start is called before the first frame update
    private void Awake()
    {
        //server.Open(1234, 64);
        client.Connect("127.0.0.1", 9091);
        client.BeginReceive(OnDataReceive);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var content = Encoding.UTF8.GetBytes("hello");
        
        int totalSize = 4 + content.Length;
        var contentSize = BitConverter.GetBytes(content.Length);
        
        byte[] buffer = new byte[totalSize];
        System.Buffer.BlockCopy(contentSize, 0, buffer, 0, 4);
        System.Buffer.BlockCopy(content, 0, buffer, 4, content.Length);

        client.Send(buffer);
        //server.BroadCast(Encoding.UTF8.GetBytes("Hello"));
    }

    void OnDataReceive(byte[] buffer)
    {
        var result = Encoding.UTF8.GetString(buffer);
        Debug.Log(result);
        //Assert.AreEqual("Hello2", result);
    }
    private void OnDestroy()
    {
        client.Disconnect();
        //server.Close();
    }
}
