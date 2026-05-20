using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using Godot;



public class ClientUDP
{
    private const int port = 11000;
    private byte[] buffer = new byte[50000];

    private Socket s;

    private DateTime connexionTime;

    private string _message = "";

    public string Message
    {
        get { return _message; }
    }

    public void RestMessage()
    {
        _message = "";
    }



    public ClientUDP()
    {

        //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
        IPAddress ipAddress = null;
        foreach (var a in ipHostInfo.AddressList)
        {
            if (a.AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = a;
                break;
            }
        }

        IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

        s = new Socket(ipAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        s.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), s);
        GD.Print(ipAddress);
        Send("Subscribe:COMMON");
        Send("Subscribe:BML_COMMAND");
        Send("Subscribe:AGENT_PLAYER_COMMAND");
        Receive();
    }

    private void ConnectCallback(IAsyncResult ar)
    {

        connexionTime = DateTime.Now;
        try
        {
            Socket client = (Socket)ar.AsyncState;
            client.EndConnect(ar);

            Console.WriteLine("Socket connected to {0}" +
                client.RemoteEndPoint.ToString());

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public void Receive()
    {
        try
        {
            s.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }


    private void ReceiveCallback(IAsyncResult ar)
    {
        int recieved = s.EndReceive(ar);
        if (recieved <= 0)
            return;

        byte[] recData = new byte[recieved];
        Buffer.BlockCopy(buffer, 0, recData, 0, recieved);
        ////// Modification Gaylor //////
        _message = Encoding.UTF8.GetString(recData);
        /////// Avant Modification //////
        //_message = Encoding.ASCII.GetString(recData);
        /////////////////////////////////

        s.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
       
    }

    public void Send(String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

         try
        {
            // Begin sending the data to the remote device.  
            s.BeginSend(byteData, 0, byteData.Length, SocketFlags.None,
                new AsyncCallback(SendCallback), s);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }


    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            // Signal that all bytes have been sent.  
            //sendDone.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }


    void OnApplicationQuit()
    {
        if (s.Connected)
            s.Shutdown(SocketShutdown.Both);
        s.Close();
    }

    public bool isConnected()
    {
        return s.Connected;
    }

    public DateTime getConnexionTime()
    {
        return connexionTime;
    }

}
