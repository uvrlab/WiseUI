using NUnit.Framework;
using System.Text;
using System.Threading;
using UnityEditor.MemoryProfiler;
using UnityEditor.PackageManager;
using UnityEngine;


public class TCPIPTest 
{
    int port = 7125;
    int countReceive;
    
    [SetUp]
    public void SetUp()
    {
        //server open
       
    }
    [TearDown]
    public void TearDown()
    {
       
    }

    [Test]
    public void CommunicationTest()
    {
        //server open
        TCPServer server = new TCPServer();
        server.Open(port, 64);
        Thread.Sleep(100);

        ////connect clients to server.
        var client = new TCPClient();
        client.Connect("127.0.0.1", port);
        client.BeginReceive(OnDataReceive);
        
        Thread.Sleep(200);
        Assert.AreEqual(1, server.connectedClients.Count);
        countReceive = 0;
        
        //send message to clients.
        server.BroadCast(Encoding.UTF8.GetBytes("Hello"));
        Thread.Sleep(100);

        ////close clients
        //client.Disconnect();
        //Assert.IsFalse(client.isConnected);
        //Thread.Sleep(100);
        //Assert.AreEqual(0, server.connectedClients.Count);

        //////server close.
        server.Close();
        Assert.IsFalse(server.isConnected);
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
