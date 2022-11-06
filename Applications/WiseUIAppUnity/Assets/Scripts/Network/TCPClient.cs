using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCPClient : MonoBehaviour
{
    protected TcpClient socket;

    public void Connect(string serverIP, int serverPort)
    {
        socket = new TcpClient(serverIP, serverPort);
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

    /// Send message to server using socket connection.     
    public void SendMessage(byte[] buffer)
    {
        if (socket == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing.             
            NetworkStream stream = socket.GetStream();
            if (stream.CanWrite)
            {
                // Write byte array to socketConnection stream.                 
                stream.Write(buffer, 0, buffer.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.LogError("Socket exception: " + socketException);
        }
    }

    public void ReceieveCallBack(IAsyncResult aResult)
    {

    }

    public void Disconnect()
    {
        //Waiting for exiting ohter thread.
        Thread.Sleep(100);
        if (socket != null && socket.Connected)
        {
            //socket.Disconnect(false);
            socket.Close();
            socket = null;
        }
    }
    private void OnDestroy()
    {
        Disconnect();
    }

}
