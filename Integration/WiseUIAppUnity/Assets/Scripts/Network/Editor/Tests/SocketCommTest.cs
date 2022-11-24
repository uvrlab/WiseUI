using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor.MemoryProfiler;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TextCore;


/// <summary>
/// server, client를 한 프로세스에서 함께 열어서 테스트 할 예정이었으나, 
/// 어떤 방법을 써도 한 프로세스에서 connect, send, receive등이 make sense하게 발생하지 않아서 클라이언트 테스트 코드만 작성함. 서버는 python으로 열어야함.
/// </summary>
public class SocketCommTest
{
    SocketClient_WiseUI[] clients;
    int port = 9091;
    int num_client = 5;
    int countReceive;
    //SocketServer server = new SocketServer();

    static readonly float time_benchmark1;

    List<byte[]> list_received_buffer = new List<byte[]>();
    readonly object lock_object = new object();

    [SetUp]
    public void SetUp()
    {
        clients = new SocketClient_WiseUI[num_client];
        for (int i = 0; i < clients.Length; i++)
        {
            clients[i] = new SocketClient_WiseUI();
            clients[i].Connect("127.0.0.1", port);
            clients[i].BeginReceive(OnDataReceive, 4096);

        }
        Thread.Sleep(100);
    }
    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < clients.Length; i++)
            clients[i].Disconnect();
    }

    [Test]
    public void CommunicationTest()
    {
        for (int i = 0; i < clients.Length; i++)
        {
            Assert.IsTrue(clients[i].isConnected);
            clients[i].SendRGBImage(i, new Texture2D(640, 360, TextureFormat.RGB24, false), ImageCompression.None);
            //Thread.Sleep(10);
        }
        int waiting_time = 2000;
        Thread.Sleep(waiting_time); //wating to receive
        Assert.AreEqual(num_client, list_received_buffer.Count);

        // check time.
        var packages = list_received_buffer.Select(x => ConvertToJson(x)).ToList();
        var time = packages.Select(x => GetDelay(x)).Average() - waiting_time / 1000.0;
        Debug.Log(time);

        packages.ForEach(x => CheckConetents(x));

    }
    ResultDataPackage ConvertToJson(byte[] buffer)
    {
        string receivedDataString = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
        return JsonUtility.FromJson<ResultDataPackage>(receivedDataString);
    }
    void CheckConetents(ResultDataPackage package)
    {
        Assert.IsNotNull(package.handDataPackage);
        Assert.IsNotNull(package.objectDataPackage);
        Assert.AreEqual(21, package.handDataPackage.joints.Count);
    }
    double GetDelay(ResultDataPackage package)
    {
        var now = DateTime.Now.ToLocalTime();
        var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
        double total_delay = span.TotalSeconds - package.frameInfo.timestamp_sentFromClient;
        return total_delay;
    }

    //server receive
    void OnDataReceive(byte[] buffer)
    {
        lock (lock_object)
        {
            list_received_buffer.Add(buffer);
        }
    }

}
