using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCPClient : MonoBehaviour
{
    protected int bufSize = 8 * 1024;
    protected TcpClient socket;
    protected EndPoint epFrom;// = new IPEndPoint(IPAddress.Any, 0);


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

    public virtual void ReceieveCallBack(IAsyncResult aResult) 
    { 

    }

    //protected void SendData(RequestType requestType, ClientType clientType, TransformData transformData)
    //{
    //    DataPackage dataPackage = new DataPackage
    //    {
    //        requestType = requestType,
    //        clientType = clientType,
    //        data = transformData
    //    };
    //    string textToSend = JsonUtility.ToJson(dataPackage);
    //    byte[] data = Encoding.ASCII.GetBytes(textToSend);
    //    socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
    //    //Debug.Log("Sent: " + textToSend);
    //}

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

}
