using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

// 配置类保持不变
[System.Serializable]
public class NetworkConfig
{
    public string ServerIp;
    public int ServerPort;
}

/// <summary>
/// 服务器连接管理器（单例）
/// 负责连接、接收数据、错误处理，并将数据交给 Decoder 解析
/// </summary>
public class ServerConnect : BaseManager<ServerConnect>
{
    [Header("接收缓冲区大小")]
    [SerializeField] private int bufferSize = 1024;

    private bool _isConnecting = false;
    private bool _isConnected = false;

    private NetworkConfig _networkConfig;
    private Socket _socket;
    private byte[] _receiveBuffer;

    // 是否自动连接（编辑器调试用）
    [SerializeField] private bool autoConnectOnStart = true;

    protected override void Awake()
    {
        base.Awake();
        if (autoConnectOnStart && !Application.isPlaying) return;

        // 可选：自动连接
        // HandleConnect();
    }

    /// <summary>
    /// 外部调用：开始连接服务器
    /// </summary>
    public void HandleConnect()
    {
        if (_isConnecting || _isConnected)
        {
            Debug.LogWarning("[ServerConnect] 正在连接或已连接，忽略新请求。");
            return;
        }

        _isConnecting = true;
        Connect();
    }

    /// <summary>
    /// 建立连接
    /// </summary>
    private void Connect()
    {
        LoadConfig();

        if (_networkConfig == null)
        {
            Debug.LogError("[ServerConnect] 配置加载失败，无法连接。");
            _isConnecting = false;
            return;
        }

        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipPoint = new(IPAddress.Parse(_networkConfig.ServerIp), _networkConfig.ServerPort);

            Debug.Log($"[ServerConnect] 正在连接服务器 {_networkConfig.ServerIp}:{_networkConfig.ServerPort}...");

            _socket.Connect(ipPoint);

            if (_socket.Connected)
            {
                Debug.Log("[ServerConnect] 连接服务器成功！");

                _isConnected = true;
                _isConnecting = false;

                // 将 Socket 交给 NetManager（可选，用于发送）
                NetManager.GetInstance()?.SetSocket(_socket);

                // 初始化接收缓冲区
                _receiveBuffer = new byte[bufferSize];

                StartReceive();
            }
        }
        catch (SocketException e)
        {
            _isConnecting = false;
            HandleSocketError(e);
        }
        catch (Exception e)
        {
            _isConnecting = false;
            Debug.LogError("[ServerConnect] 连接异常: " + e.Message);
        }
    }

    /// <summary>
    /// 开始异步接收数据
    /// </summary>
    private void StartReceive()
    {
        if (!_isConnected || _socket == null) return;

        try
        {
            _socket.BeginReceive(
                _receiveBuffer,
                0,
                _receiveBuffer.Length,
                SocketFlags.None,
                OnReceiveCallback,
                _socket
            );
        }
        catch (Exception e)
        {
            Debug.LogError("[ServerConnect] 启动接收失败: " + e.Message);
            Disconnect();
        }
    }

    /// <summary>
    /// 接收回调：收到数据
    /// </summary>
    private void OnReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // 将操作转发到主线程
            MainThreadDispatcher.Enqueue(() =>
            {
                if (this == null || !this.isActiveAndEnabled)
                {
                    Debug.Log("[ServerConnect] Instance is null or disabled, ignoring receive callback.");
                    return;
                }

                int bytesRead = _socket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    byte[] receivedData = new byte[bytesRead];
                    Array.Copy(_receiveBuffer, receivedData, bytesRead);

                    // 在主线程上调用 Decoder
                    Decoder.GetInstance().OnDataReceived(receivedData);

                    // 继续接收下一批
                    StartReceive();
                }
                else
                {
                    // 服务器关闭连接
                    Debug.Log("[ServerConnect] 服务器断开连接。");
                    Disconnect();
                }
            });
        }
        catch (SocketException e)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (e.SocketErrorCode != SocketError.Interrupted)
                {
                    Debug.LogError("[ServerConnect] Socket 错误: " + e.Message);
                }
                Disconnect();
            });
        }
        catch (ObjectDisposedException)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                // Socket 已被关闭，正常情况
                Debug.Log("[ServerConnect] Socket 已释放。");
            });
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.LogError("[ServerConnect] 接收数据异常: " + e.Message);
                Disconnect();
            });
        }
    }

    /// <summary>
    /// 处理 Socket 异常
    /// </summary>
    private void HandleSocketError(SocketException e)
    {
        _isConnected = false;
        _isConnecting = false;

        switch (e.ErrorCode)
        {
            case 10061: // Connection refused
                Debug.LogError("[ServerConnect] 服务器拒绝连接，请检查服务器是否运行。");
                break;
            case 10060: // Timeout
                Debug.LogError("[ServerConnect] 连接超时，请检查网络或服务器地址。");
                break;
            case 11001: // Host not found
                Debug.LogError("[ServerConnect] 无法解析服务器地址，请检查 IP 是否正确。");
                break;
            default:
                Debug.LogError($"[ServerConnect] 连接失败: {e.Message} (Error Code: {e.ErrorCode})");
                break;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (!_isConnected && _socket == null) return;

        _isConnected = false;
        _isConnecting = false;

        if (_socket != null)
        {
            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                _socket.Close();
                _socket = null;
                Debug.Log("[ServerConnect] 已断开连接。");
            }
            catch (Exception e)
            {
                Debug.LogError("[ServerConnect] 关闭 Socket 时出错: " + e.Message);
            }
        }

        // 通知 NetManager
        NetManager.GetInstance()?.Disconnect();
    }

    /// <summary>
    /// 从 Resources/appsettings.json 加载配置
    /// </summary>
    private void LoadConfig()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("appsettings");
        if (textAsset == null)
        {
            Debug.LogError("[ServerConnect] 找不到 Resources/appsettings.json 文件！");
            _networkConfig = null;
            return;
        }

        try
        {
            _networkConfig = JsonUtility.FromJson<NetworkConfig>(textAsset.text);
            if (_networkConfig == null)
            {
                Debug.LogError("[ServerConnect] JSON 解析失败或配置为空！");
            }
            else
            {
                Debug.Log($"[ServerConnect] 配置加载成功: {_networkConfig.ServerIp}:{_networkConfig.ServerPort}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[ServerConnect] JSON 解析异常: " + e.Message);
            _networkConfig = null;
        }
    }

    /// <summary>
    /// 供外部查询连接状态
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// 用于发送数据（可由 NetManager 调用）
    /// </summary>
    public void Send(byte[] data)
    {
        if (!_isConnected || _socket == null || !_socket.Connected)
        {
            Debug.LogError("[ServerConnect] 无法发送：未连接到服务器。");
            return;
        }

        try
        {
            _socket.Send(data);
            // Debug.Log($"[ServerConnect] 已发送 {data.Length} 字节数据。");
        }
        catch (Exception e)
        {
            Debug.LogError("[ServerConnect] 发送失败: " + e.Message);
            Disconnect();
        }
    }

    /// <summary>
    /// 应用退出时确保断开
    /// </summary>
    private void OnApplicationQuit()
    {
        Disconnect();
    }
}