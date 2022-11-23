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

    //server receive
    void OnDataReceive(byte[] buffer)
    {
        lock(lock_object)
        {
            list_received_buffer.Add(buffer);
        }
    }

}
