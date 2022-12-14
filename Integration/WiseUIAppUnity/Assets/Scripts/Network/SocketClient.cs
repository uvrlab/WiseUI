using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class SocketClient
{
    //protected TcpClient socket;
    public Socket socket;
    protected byte[] receiveBuffer;

    protected EndPoint remoteEP;
    public delegate void ReceiveCallback(byte[] buffer);
    public ReceiveCallback receiveCallback;

    public virtual void Connect(string serverIP, int serverPort)
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);

        remoteEP = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

        //socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), null);
        socket.Connect(remoteEP);
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
        socket.EndConnect(ar);
        //try
        //{
            socket.EndConnect(ar);
        //}
        //catch(SocketException e)
        //{
        //    Debug.LogError(e.Message);
        //}
    }

    /// Send message to server using socket connection.     
    public void SendString(string message)
    {
        Send(Encoding.UTF8.GetBytes(message));
    }
    
    public void Send(byte[] buffer)
    {
        if (socket != null)
        {
            try
            {
                socket.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), null);
            }
            catch (SocketException e)
            {
                Debug.LogException(e);
            }
            catch (ObjectDisposedException e)
            {
                //Debug.LogException(e);
            }
            
        }
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Complete sending the data to the remote device.
            int bytesSent = socket.EndSend(ar);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public virtual void BeginReceive(ReceiveCallback callback, int maxBufferSize)
    {
        receiveBuffer = new byte[maxBufferSize];
        socket.BeginReceiveFrom(receiveBuffer, 0, maxBufferSize, SocketFlags.None, ref remoteEP, OnDataReceive, null);
        receiveCallback = callback;
    }
    void OnDataReceive(IAsyncResult ar)
    {
        try
        {
            int bytesRead = socket.EndReceive(ar);
            if (bytesRead > 0)
            {
                byte[] buffer = new byte[bytesRead];
                Array.Copy(receiveBuffer, buffer, bytesRead);
                receiveCallback(buffer);
            }
            socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEP, OnDataReceive, null);
        }
        catch (SocketException e)
        {
            //Debug.LogException(e);
        }
        catch(ObjectDisposedException e)
        {

        }
        
    }

    public virtual void Disconnect()
    {
        if (socket != null)
        {
            socket.Close();
            socket.Dispose();
        }
    }

   

}
