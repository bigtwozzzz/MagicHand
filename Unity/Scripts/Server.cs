using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Server : MonoBehaviour
{
    public string ipAddress = "127.0.0.1";
    public int ConnectPort = 1111;
    public string recvStr;
    string data = "keep";
    string gesture = "Unknown";
    Socket socket;
    EndPoint clientEnd;
    IPEndPoint ipEnd;
    string sendStr;
    byte[] recvData = new byte[1024];
    byte[] sendData = new byte[1024];
    int recvLen;
    Thread connectThread;
    //初始化
    void InitSocket()
    {
        ipEnd = new IPEndPoint(IPAddress.Parse(ipAddress), ConnectPort);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(ipEnd);
        //定义客户端
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        clientEnd = (EndPoint)sender;
        print("等待连接数据");
        //开启一个线程连接
        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
    }
    void SocketSend(string sendStr)
    {
        sendData = new byte[1024];
        sendData = Encoding.UTF8.GetBytes(sendStr);
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, clientEnd);
    }
    //服务器接收
    void SocketReceive()
    {
        while (true)
        {
            recvData = new byte[1024];
            recvLen = socket.ReceiveFrom(recvData, ref clientEnd);
            recvStr = Encoding.UTF8.GetString(recvData, 0, recvLen);
            data = recvStr;
            Debug.Log("收到得信息 " + recvStr);
        }
    }
    
    //连接关闭
    public void SocketQuit()
    {
        //关闭线程
        if (connectThread != null)
        {
            connectThread.Interrupt();
            connectThread.Abort();
        }
        //最后关闭socket
        if (socket != null)
            socket.Close();
        Debug.LogWarning("断开连接");
    }

    // Use this for initialization
    void Start()
    {
        InitSocket(); //在这里初始化server
    }

    void OnApplicationQuit()
    {
        SocketQuit();
    }

}