using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Threading;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager;
using UnityEditor.Sprites;
using PlasticPipe.Remoting;

public class TCPIPTest : MonoBehaviour
{
    Socket serverSock;

    Thread serverReceiveThread;

    int port = 7000;

    [SetUp]
    public void SetUp()
    {
        //server open
        //listener = new TcpListener(serverIP, port);
        serverReceiveThread = new Thread(ServerReceiveLoop);

        serverSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSock.Bind(new IPEndPoint(IPAddress.Any, port));

        serverReceiveThread = new Thread(ServerReceiveLoop);
        serverReceiveThread.Start();
    }
    [TearDown]
    public void TearDown()
    {
        serverReceiveThread.Interrupt();
        serverSock.Close();
    }

    [Test]
    public void CommunicationTest()
    {
        //client open
        var go = new GameObject();
        var script = go.AddComponent<TCPClient>();

        script.Connect("127.0.0.1", port);
        Assert.IsTrue(script.isConnected);

        //echo test

        //script.SendMessage("Test Message.");

        script.Disconnect();
        Assert.IsFalse(script.isConnected);
    }

    void ServerReceiveLoop()
    {
        try
        {
            while (true)
            {
                //Socket clientSock;
                byte[] buff = new byte[1024];

                serverSock.Listen(10); // client 접속을 기다림.
                Socket clientSock = serverSock.Accept();

                Debug.Log(clientSock.LocalEndPoint.ToString() + " : " + "connected.");
                while (true)
                {
                    try
                    {
                        int receive = clientSock.Receive(buff);
                        clientSock.Send(buff);
                    }
                    catch (SocketException e)
                    {
                        Debug.Log(e.ErrorCode + ":" + e.Message);
                        break;
                    }

                }
                Thread.Sleep(1);
            }
        }
        catch (ThreadInterruptedException e)
        {
            Debug.LogFormat(e.Message);
        }
    }


}
