using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Decoder : BaseManager<Decoder>
{
    public Assign assign;

    private byte[] _buffer = new byte[0];

    public void Start()
    {
        assign = Assign.GetInstance();
    }
    /// <summary>
    /// 接收到新数据时调用此方法
    /// </summary>
    /// <param name="data">从网络接收到的字节数据</param>
    public void OnDataReceived(byte[] data)
    {
        // 将新数据追加到缓冲区
        byte[] newBuffer = new byte[_buffer.Length + data.Length];
        Array.Copy(_buffer, 0, newBuffer, 0, _buffer.Length);
        Array.Copy(data, 0, newBuffer, _buffer.Length, data.Length);
        _buffer = newBuffer;

        // 尝试解析完整的消息
        ParseMessages();
    }

    /// <summary>
    /// 解析缓冲区中的消息
    /// </summary>
    private void ParseMessages()
    {

        while (_buffer.Length >= 8) // 消息头长度为 8 字节
        {
            // 读取消息长度（前4字节，小端序）
            uint dataLen = BitConverter.ToUInt32(_buffer, 0);
            uint totalLength = dataLen + 8; // 总长度 = 数据长度 + 8 字节头部

            if (_buffer.Length < totalLength)
            {
                // 缓冲区中没有足够的数据构成完整消息，等待更多数据
                break;
            }

            // 读取消息ID（第5-8字节，小端序）
            uint msgId = BitConverter.ToUInt32(_buffer, 4);

            // 提取消息体（第9字节开始，长度为 dataLen）
            byte[] msgBody = new byte[dataLen];
            Array.Copy(_buffer, 8, msgBody, 0, dataLen);

            // 处理消息
            assign.DispatchNetworkEvent(msgId, msgBody);

            // 移除已处理的消息
            byte[] remainingBuffer = new byte[_buffer.Length - totalLength];
            Array.Copy(_buffer, totalLength, remainingBuffer, 0, remainingBuffer.Length);
            _buffer = remainingBuffer;
        }
    }

}