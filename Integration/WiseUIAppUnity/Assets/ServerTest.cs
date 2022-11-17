using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public class ServerTest : MonoBehaviour
{
    TCPServer server = new TCPServer();
    TCPClient client = new TCPClient();

    // Start is called before the first frame update
    private void Awake()
    {
        server.Open(1234, 64);
    }
    void Start()
    {
        client.Connect("127.0.0.1", 1234);
        client.BeginReceive(OnDataReceive);
    }

    // Update is called once per frame
    void Update()
    {
        client.SendMessage("hello");
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
        server.Close();
    }
}
