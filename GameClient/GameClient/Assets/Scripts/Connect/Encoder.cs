using Base;
using Broadcast;
using Character;
using Common;
using Google.Protobuf;
using Scene;
using System;
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
        if (msgBody == null)
            msgBody = new byte[0];

        uint dataLen = (uint)msgBody.Length;
        byte[] dataLenBytes = BitConverter.GetBytes(dataLen); // 小端序
        byte[] msgIdBytes = BitConverter.GetBytes(msgId);     // 小端序

        // 创建总字节数组：8字节头部 + 消息体
        byte[] packet = new byte[8 + msgBody.Length];

        // 写入 dataLen (前4字节)
        Array.Copy(dataLenBytes, 0, packet, 0, 4);
        // 写入 msgId (第5-8字节)
        Array.Copy(msgIdBytes, 0, packet, 4, 4);
        // 写入 msgBody (第9字节开始)
        Array.Copy(msgBody, 0, packet, 8, msgBody.Length);

        return packet;
    }

    /// <summary>
    /// 快捷方法：直接发送消息（假设你有网络发送模块）
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="msgBody">消息体</param>
    public void Send(uint msgId, byte[] msgBody)
    {
        byte[] packet = Pack(msgId, msgBody);

     
        if (NetManager.GetInstance().IsConnected)
        {
            NetManager.GetInstance().Send(packet);
        }
        else
        {
            Debug.LogError("[Encoder] 网络未连接，无法发送消息！");
        }

        // 可保留日志
        Debug.Log($"[Encoder] Packaged message: ID={msgId}, Length={msgBody?.Length ?? 0}");
    }
    public void Flush()
    {
        if (netManager.IsConnected)
        {
            netManager.Flush();
        }
        else
        {
            Debug.LogWarning("[Encoder] Network is not connected, cannot flush.");
        }
    }
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
