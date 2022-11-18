using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.tvOS;

public class TCPClient
{
    //protected TcpClient socket;
    public Socket socket;
    
    protected byte[] receiveBuffer;
    public readonly int receiveBufferSize = 1024;

    protected EndPoint remoteEP;
    public delegate void RunDelegate(byte[] buffer);
    RunDelegate runDelegate;
    
    
    public virtual void Connect(string serverIP, int serverPort)
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);

        remoteEP = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

        socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), null);
    }

    ~ TCPClient()
    {
        //Disconnect();
    }

    public bool isConnected
    {
        get
        {
            if (socket == null)
                return false;

            return socket.Connected;
        }
    }
    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Complete the connection.
            socket.EndConnect(ar);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    /// Send message to server using socket connection.     
    public void SendMessage(string message)
    {
        Send(Encoding.UTF8.GetBytes(message));
    }
    
    public void Send(byte[] buffer)
    {
        //socket.Send(buffer);
        socket.BeginSend(buffer, 0, buffer.Length, 0,
           new AsyncCallback(SendCallback), null);
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            //Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = socket.EndSend(ar);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public void BeginReceive(RunDelegate callback)
    {
        runDelegate = callback;
        receiveBuffer = new byte[(long)receiveBufferSize];
        socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEP, new AsyncCallback(OnDataReceive), receiveBuffer);
    }

    void OnDataReceive(IAsyncResult aResult)
    {
        byte[] receivedData = (byte[])aResult.AsyncState;
        runDelegate(receivedData);
        socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEP, new AsyncCallback(OnDataReceive), receiveBuffer);
    }
    
    public void Disconnect()
    {
        //Waiting for exiting ohter thread.
        if (socket != null)
        {
            socket.Disconnect(false);
            socket.Close();
            socket.Dispose();
            //socket.BeginDisconnect(true, DisconnectCallback, null);
            //socket = null;
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }

}
