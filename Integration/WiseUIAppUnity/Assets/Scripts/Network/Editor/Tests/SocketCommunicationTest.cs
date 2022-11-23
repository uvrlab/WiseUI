using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using UnityEditor.MemoryProfiler;
using UnityEditor.PackageManager;
using UnityEngine;


/// <summary>
/// server, client�� �� ���μ������� �Բ� ��� �׽�Ʈ �� �����̾�����, 
/// � ����� �ᵵ �� ���μ������� connect, send, receive���� make sense�ϰ� �߻����� �ʾƼ� �׽�Ʈ �ڵ� �ۼ� �ߴ�. 
/// </summary>
public class TCPIPTest 
{
    
    int port = 9091;
    int countReceive;
    //SocketServer server = new SocketServer();
    SocketClient client = new SocketClient();
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
        //client.Disconnect();
        //server.Close();
    }

    //[Test]
    public void CommunicationTest()
    {
        //server open
        ////connect clients to server.
        client.Connect("127.0.0.1", port);
        //client.BeginReceive(OnDataReceive);
        client.Send(Encoding.UTF8.GetBytes("hello"));

        Thread.Sleep(1000);

        //int time = DateTime.Now.Second;
        //while (server.IsAccepted(client.socket) == false)
        //{
        //    Thread.Sleep(10);
        //    if (DateTime.Now.Second - time > 10)
        //        break;
        //}
        

        //Thread.Sleep(200);
        //Assert.AreEqual(1, server.connectedClients.Count);
        //countReceive = 0;

        ////send message to clients.
        //server.BroadCast(Encoding.UTF8.GetBytes("Hello"));
        //Thread.Sleep(1000);

        ////close clients
        //client.Disconnect();
        //Assert.IsFalse(client.isConnected);
        //Thread.Sleep(100);
        //Assert.AreEqual(0, server.connectedClients.Count);

        //////server close.
        //server.Close();
        //Assert.IsFalse(server.isConnected);
    }

    //server receive
    void OnDataReceive(byte[] buffer)
    {
        countReceive++;
        var result = Encoding.UTF8.GetString(buffer);
        Debug.Log(result);
        Assert.AreEqual("Hello2", result);
    }

}