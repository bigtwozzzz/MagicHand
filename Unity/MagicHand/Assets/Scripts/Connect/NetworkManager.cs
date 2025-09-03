using System.Net.Sockets;
using UnityEngine;

// NetManager.cs
public class NetManager : BaseManager<NetManager>
{
    private Socket _socket;
    private bool _isConnected = false;

    // 从 ServerConnect 获取连接好的 Socket
    public void SetSocket(Socket socket)
    {
        _socket = socket;
        _isConnected = socket != null && socket.Connected;
        if (_isConnected)
        {
            Debug.Log("[NetManager] Socket 已设置，准备发送数据。");
        }
    }

    public bool IsConnected => _isConnected;

    /// <summary>
    /// 发送字节数据到服务器（指定长度）
    /// </summary>
    public void Send(byte[] packet, int length)
    {
        if (!_isConnected || _socket == null || !_socket.Connected)
        {
            Debug.LogError("[NetManager] 无法发送数据：Socket 未连接！");
            return;
        }

        try
        {
            _socket.Send(packet, 0, length, SocketFlags.None);
            Debug.Log($"[NetManager] 已发送 {length} 字节数据到服务器。");
        }
        catch (SocketException e)
        {
            Debug.LogError("[NetManager] 发送数据失败: " + e.Message);
            Disconnect();
        }
    }
    public void Disconnect()
    {
        if (_socket != null)
        {
            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException)
            {
                // 忽略，可能连接已断
            }
            finally
            {
                // 使用 using 或显式 Dispose
                _socket.Dispose();
                _socket = null;
            }
        }
        _isConnected = false;
        Debug.Log("[NetManager] 已断开连接并释放 Socket。");
    }
    private void OnApplicationQuit()
    {
        Disconnect();
    }
}