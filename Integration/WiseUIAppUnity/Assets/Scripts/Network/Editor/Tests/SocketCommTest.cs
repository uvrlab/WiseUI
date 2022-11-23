using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEditor.MemoryProfiler;
using UnityEditor.PackageManager;
using UnityEngine;


/// <summary>
/// server, client를 한 프로세스에서 함께 열어서 테스트 할 예정이었으나, 
/// 어떤 방법을 써도 한 프로세스에서 connect, send, receive등이 make sense하게 발생하지 않아서 클라이언트 테스트 코드만 작성함. 서버는 python으로 열어야함.
/// </summary>
public class SocketCommTest 
{
    
    int port = 9091;
    int num_client = 10;
    int countReceive;
    //SocketServer server = new SocketServer();

    List<byte[]> list_received_buffer = new List<byte[]>();
    readonly object lock_object = new object();
    
    [SetUp]
    public void SetUp()
    {
        //server open
        //server.Open(port, 64);
        //Thread.Sleep(100);
    }
    [TearDown]
    public void TearDown()
    {
        //server.Close();
    }

    [Test]
    public void CommunicationTest()
    {
        
        SocketClient_WiseUI[] clients = new SocketClient_WiseUI[num_client];
        for (int i = 0; i < clients.Length; i++)
        {
            clients[i] = new SocketClient_WiseUI();
            clients[i].Connect("127.0.0.1", port);
            clients[i].BeginReceive(OnDataReceive, 4096);
            Thread.Sleep(10);
        }

        for (int i = 0; i < clients.Length; i++)
        {
            clients[i].SendRGBImage(i, new Texture2D(320, 240, TextureFormat.RGB24, false), ImageCompression.None);
            Thread.Sleep(10);
        }
        Thread.Sleep(100);
        Assert.AreEqual(num_client, list_received_buffer.Count);

        foreach (var buffer in list_received_buffer)
        {
            string receivedDataString = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
            ResultDataPackage package = JsonUtility.FromJson<ResultDataPackage>(receivedDataString);

            Assert.IsNotNull(package.handDataPackage);
            Assert.IsNotNull(package.objectDataPackage);
            Assert.AreEqual(21, package.handDataPackage.joints.Count);
        }
      

        for (int i = 0; i < clients.Length; i++)
        {
            clients[i].Disconnect();
            Thread.Sleep(10);
        }
    }
    [Test]
    public void JSonStringDeserializeTest()
    {
        string t1 = "{\"frameInfo\": {\"frameID\": 395, \"timestamp_receive\": 1668847899.4188118, \"timestamp_send\": 1668847899.4287872}, \"objectDataPackage\": {\"objects\": [{\"keypoints\": [{\"id\": 0, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 1, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 2, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 3, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 4, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 5, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 6, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 7, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}], \"id\": 0}, {\"keypoints\": [{\"id\": 0, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 1, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 2, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 3, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 4, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 5, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 6, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 7, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}], \"id\": 1}, {\"keypoints\": [{\"id\": 0, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 1, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 2, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 3, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 4, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 5, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 6, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 7, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}], \"id\": 2}]}, \"handDataPackage\": {\"joints\": [{\"id\": 0, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 1, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 2, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 3, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 4, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 5, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 6, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 7, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 8, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 9, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 10, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 11, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 12, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 13, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 14, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 15, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 16, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 17, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 18, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 19, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}, {\"id\": 20, \"x\": 0.123, \"y\": 0.456, \"z\": 0.789}]}}";
        //Debug.Log(JsonUtility.ToJson(data));
        ResultDataPackage data = JsonUtility.FromJson<ResultDataPackage>(t1);
        Assert.AreEqual(395, data.frameInfo.frameID);
    }
    //server receive
    void OnDataReceive(byte[] buffer)
    {
        lock(lock_object)
        {
            list_received_buffer.Add(buffer);
        }
    }

}
