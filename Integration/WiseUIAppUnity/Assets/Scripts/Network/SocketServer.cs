using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

public class SocketServer
{
    Socket serverSock;

    byte[] receiveBuffer;
    long receiveBufferSize;

    public List<Socket> connectedClients = new List<Socket>();


    ~SocketServer()
    {
        //serverSock.Close();
    }
    public bool IsOpened
    {
        get
        {
            if (serverSock == null)
                return false;

            return serverSock.Connected;
        }
    }
    public bool IsAccepted(Socket client)
    {
        if (client == null)
            return false;

        var target = connectedClients.Find(x => x.RemoteEndPoint == client.LocalEndPoint);
        return target != null;
    }
    public bool Open(int port, long receiveBufferSize)
    {
        try
        {
            serverSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);
            serverSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
            serverSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            serverSock.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSock.Listen(10);
            serverSock.BeginAccept(new AsyncCallback(AcceptCallback), null);
            this.receiveBufferSize = receiveBufferSize;
            receiveBuffer = new byte[receiveBufferSize];
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }
    }

    void AcceptCallback(IAsyncResult ar)
    {
        try
        {
            Socket client = serverSock.EndAccept(ar);
            connectedClients.Add(client);
            
            receiveBuffer = new byte[this.receiveBufferSize];
            client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, OnDataReceive2, receiveBuffer);
            client.BeginDisconnect(true, DisconnectCallback, client);

            serverSock.BeginAccept(AcceptCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
    void DisconnectCallback(IAsyncResult aResult)
    {
        Socket client = (Socket)aResult.AsyncState;
        var target = connectedClients.Find(x => x.RemoteEndPoint.ToString() == client.RemoteEndPoint.ToString());
        connectedClients.Remove(target);
       
    }

    public void BroadCast(byte[] buffer)
    {
        foreach (Socket client in connectedClients)
        {
            //serverSock.Send(buffer);
            client.Send(buffer);
            //client.Send(buffer, 0, buffer.Length, SocketFlags.None);  // echo
             //client.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), client);
        }
        //sendDone.WaitOne();
    }
   
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);

            // Signal that all bytes have been sent.
            //sendDone.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    void OnDataReceive2(IAsyncResult aResult)
    {
        byte[] receivedData = (byte[])aResult.AsyncState;
    }
    
    public void Close()
    {
        //foreach (Socket socket in connectedClients)
        //{
        //    socket.Close();
        //    socket.Dispose();
        //}
        connectedClients.Clear();
        
        if (serverSock != null)
        {
            serverSock.Close();
            serverSock.Dispose();
        }
   
    }
}
