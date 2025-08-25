using System.Collections;
using System.Collections.Generic;
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
    /// 发送字节数据到服务器
    /// </summary>
    public void Send(byte[] packet)
    {
        if (!_isConnected || _socket == null || !_socket.Connected)
        {
            Debug.LogError("[NetManager] 无法发送数据：Socket 未连接！");
            return;
        }

        try
        {
            _socket.Send(packet);
            Debug.Log($"[NetManager] 已发送 {packet.Length} 字节数据到服务器。");
        }
        catch (SocketException e)
        {
            Debug.LogError("[NetManager] 发送数据失败: " + e.Message);
            Disconnect();
        }
    }
    //public void Flush()
    //{
    //    // 如果是 TCP，可以调用 NetworkStream.Flush()
    //    // 如果是 Socket，可以尝试发送一个空包或等待
    //    try
    //    {
    //        if (_socket != null && _socket.Connected)
    //        {
    //            // 小延迟确保发送完成（实际项目可用更优雅方式）
    //            System.Threading.Thread.Sleep(100);
    //            Debug.Log("[NetManager] Flush: Sent all pending data.");
    //        }
    //    }
    //    catch
    //    {
    //        // 忽略异常，因为正在关闭
    //    }
    //}
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