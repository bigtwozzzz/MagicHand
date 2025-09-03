using System;
using System.Buffers;
using UnityEngine;

public class Decoder : BaseManager<Decoder>
{
    public Assign assign;

    private byte[] _buffer = ArrayPool<byte>.Shared.Rent(1024); // 初始缓冲
    private int _bufferLength = 0; // 当前有效数据长度

    private const int HeaderSize = 8;
    private const int GrowthFactor = 2;
    private const int MaxMessageSize = 1024 * 1024; // 限制单条消息最大为 1MB

    public void Start()
    {
        assign = Assign.GetInstance();
    }

    public void OnDataReceived(byte[] data)
    {
        if (data == null || data.Length == 0) return;

        EnsureBufferSize(_bufferLength + data.Length);
        Array.Copy(data, 0, _buffer, _bufferLength, data.Length);
        _bufferLength += data.Length;

        ParseMessages();
    }

    private void EnsureBufferSize(int requiredSize)
    {
        if (_buffer.Length >= requiredSize) return;

        int newSize = _buffer.Length;
        while (newSize < requiredSize)
            newSize *= GrowthFactor;

        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Array.Copy(_buffer, 0, newBuffer, 0, _bufferLength);

        ArrayPool<byte>.Shared.Return(_buffer); // 释放旧缓冲
        _buffer = newBuffer;
    }

    private void ParseMessages()
    {
        int offset = 0;
        while (_bufferLength - offset >= HeaderSize)
        {
            uint dataLen = BitConverter.ToUInt32(_buffer, offset);

            // 安全检查
            if (dataLen > MaxMessageSize)
            {
                Debug.LogError("Message too large, closing connection or resetting buffer.");
                ResetBuffer();
                return;
            }

            uint totalLen = dataLen + HeaderSize;

            if (_bufferLength - offset < totalLen)
                break; // 数据不完整，等待下一批

            uint msgId = BitConverter.ToUInt32(_buffer, offset + 4);
            byte[] msgBody = new byte[dataLen]; // 这里仍需新数组用于事件分发
            Array.Copy(_buffer, offset + 8, msgBody, 0, dataLen);
            Debug.Log("Received message: " + msgId);
            assign.DispatchNetworkEvent(msgId, msgBody);

            offset += (int)totalLen;
        }

        // 将未处理的数据前移
        if (offset > 0 && _bufferLength > offset)
        {
            Array.Copy(_buffer, offset, _buffer, 0, _bufferLength - offset);
            _bufferLength -= offset;
        }
        else if (offset >= _bufferLength)
        {
            _bufferLength = 0; // 全部处理完
        }
    }
    private void ResetBuffer()
    {
        _bufferLength = 0;
        // 可选：将缓冲区缩小回初始大小，节省内存
        if (_buffer.Length > 1024)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = ArrayPool<byte>.Shared.Rent(1024);
        }
    }
    // 主动释放资源
    public void Clear()
    {
        if (_buffer != null)
        {
            ArrayPool<byte>.Shared.Return(_buffer, true); // 清除并归还
            _buffer = null;
            _bufferLength = 0;
        }
    }

    // 可选：在 OnDestroy 中调用
    protected override void OnDestroy()
    {
        Clear();
    }
}