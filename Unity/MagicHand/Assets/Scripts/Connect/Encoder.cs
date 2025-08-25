using Base;
using Broadcast;
using Character;
using Common;
using Google.Protobuf;
using Scene;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Encoder : BaseManager<Encoder>
{
    /// <summary>
    /// 打包并返回完整的网络消息字节流
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="msgBody">消息体（已序列化，如 Protobuf 字节）</param>
    /// <returns>完整的协议字节流</returns>
    private NetManager netManager;
    public void Start()
    {
        netManager = NetManager.GetInstance();
    }
    public byte[] Pack(uint msgId, byte[] msgBody)
    {
        msgBody ??= Array.Empty<byte>();

        uint dataLen = (uint)msgBody.Length;
        byte[] dataLenBytes = BitConverter.GetBytes(dataLen);
        byte[] msgIdBytes = BitConverter.GetBytes(msgId);

        int packetSize = 8 + msgBody.Length;

        // 从共享池中租借数组（避免 new）
        byte[] packet = ArrayPool<byte>.Shared.Rent(packetSize);

        // 填充数据
        Buffer.BlockCopy(dataLenBytes, 0, packet, 0, 4);
        Buffer.BlockCopy(msgIdBytes, 0, packet, 4, 4);
        Buffer.BlockCopy(msgBody, 0, packet, 8, msgBody.Length);

        // 注意：返回的是租借的数组，调用者需负责归还！
        return packet;
    }

    /// <summary>
    /// 快捷方法：直接发送消息（假设你有网络发送模块）
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="msgBody">消息体</param>
    public void Send(uint msgId, byte[] msgBody)
    {
        byte[] packet = null;
        try
        {
            packet = Pack(msgId, msgBody);
            if (NetManager.GetInstance().IsConnected)
            {
                NetManager.GetInstance().Send(packet); // 明确长度
            }
            else
            {
                Debug.LogError("[Encoder] 网络未连接，无法发送消息！");
            }
        }
        finally
        {
            // 归还数组到池中
            if (packet != null)
                ArrayPool<byte>.Shared.Return(packet);
        }

        Debug.Log($"[Encoder] Sent message: ID={msgId}, Length={msgBody?.Length ?? 0}");
    }
    //public void Flush()
    //{
    //    if (netManager.IsConnected)
    //    {
    //        netManager.Flush();
    //    }
    //    else
    //    {
    //        Debug.LogWarning("[Encoder] Network is not connected, cannot flush.");
    //    }
    //}
    /// <summary>
    /// 使用泛型 + Protobuf 序列化发送（推荐）
    /// </summary>
    /// <typeparam name="T">实现了 IMessage 的 Protobuf 消息类型</typeparam>
    /// <param name="msgId">消息ID</param>
    /// <param name="message">Protobuf 消息对象</param>
    public void Send<T>(uint msgId, T message) where T : Google.Protobuf.IMessage
    {
        byte[] msgBody = message.ToByteArray(); // Protobuf 序列化
        Send(msgId, msgBody);
    }
}
