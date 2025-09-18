using UnityEngine;
using Skill;

/// <summary>
/// 魔法网络发送器
/// 专门负责订阅本地魔法触发事件，并向服务器发送魔法施放信息
/// </summary>
public class MagicNetworkSender : MonoBehaviour
{
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;
    
    [Header("网络设置")]
    [SerializeField] private uint magicCastMessageId = 8; // 魔法施放消息ID，可根据服务器协议调整
    
    private Encoder encoder;
    private int localPlayerId = 1; // 本地玩家ID，可从PlayerManager获取
    
    void Start()
    {
        // 获取编码器实例
        encoder = Encoder.GetInstance();
        if (encoder == null)
        {
            Debug.LogError("[MagicNetworkSender] 无法获取Encoder实例！");
            return;
        }
        
        // 订阅魔法触发事件
        MagicEventSystem.OnMagicTriggered += OnLocalMagicTriggered;
        
        // 获取本地玩家ID
        InitializeLocalPlayerId();
        
        LogDebug("[MagicNetworkSender] 魔法网络发送器已初始化");
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (MagicEventSystem.OnMagicTriggered != null)
        {
            MagicEventSystem.OnMagicTriggered -= OnLocalMagicTriggered;
        }
    }
    
    /// <summary>
    /// 初始化本地玩家ID
    /// </summary>
    private void InitializeLocalPlayerId()
    {
        // 从MagicManager获取本地玩家ID
        if (MagicManager.Instance != null)
        {
            localPlayerId = MagicManager.Instance.GetLocalPlayerId();
            LogDebug($"[MagicNetworkSender] 从MagicManager获取本地玩家ID: {localPlayerId}");
        }
        else
        {
            // 如果MagicManager不可用，使用默认值
            localPlayerId = 1;
            LogDebug($"[MagicNetworkSender] MagicManager不可用，使用默认玩家ID: {localPlayerId}");
        }
    }
    
    /// <summary>
    /// 处理本地魔法触发事件
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <param name="magicData">魔法数据</param>
    /// <param name="playerId">触发魔法的玩家ID</param>
    private void OnLocalMagicTriggered(int magicId, MagicData magicData, int playerId)
    {
        // 只处理本地玩家的魔法触发
        if (playerId != localPlayerId)
        {
            LogDebug($"[MagicNetworkSender] 跳过非本地玩家{playerId}的魔法触发");
            return;
        }
        
        // 检查网络连接
        if (!NetManager.GetInstance().IsConnected)
        {
            Debug.LogWarning("[MagicNetworkSender] 网络未连接，无法发送魔法信息");
            return;
        }
        
        // 发送魔法施放信息到服务器
        SendMagicCastToServer(magicId, playerId);
    }
    
    /// <summary>
    /// 向服务器发送魔法施放信息
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <param name="playerId">玩家ID</param>
    private void SendMagicCastToServer(int magicId, int playerId)
    {
        try
        {
            // 创建技能信息请求消息
            // 注意：这里使用SkillInfoRequest作为示例，实际项目中可能需要创建专门的MagicCast消息类型
            var magicCastMessage = new SkillInfoRequest
            {
                PlayerId = playerId.ToString(),
                SkillId = magicId.ToString()
            };
            
            // 发送到服务器
            encoder.Send(magicCastMessageId, magicCastMessage);
            
            LogDebug($"[MagicNetworkSender] 已发送魔法施放信息到服务器 - 玩家ID: {playerId}, 魔法ID: {magicId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MagicNetworkSender] 发送魔法信息失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 手动测试发送魔法信息
    /// </summary>
    [ContextMenu("测试发送魔法信息")]
    public void TestSendMagicInfo()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[MagicNetworkSender] 请在运行时测试");
            return;
        }
        
        // 发送测试魔法信息
        SendMagicCastToServer(23, localPlayerId); // 测试治疗魔法
        LogDebug("[MagicNetworkSender] 测试魔法信息发送完成");
    }
    
    /// <summary>
    /// 调试日志输出
    /// </summary>
    /// <param name="message">日志信息</param>
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// 设置本地玩家ID（供外部调用）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void SetLocalPlayerId(int playerId)
    {
        localPlayerId = playerId;
        LogDebug($"[MagicNetworkSender] 本地玩家ID更新为: {localPlayerId}");
    }
    
    /// <summary>
    /// 设置魔法施放消息ID（供外部调用）
    /// </summary>
    /// <param name="messageId">消息ID</param>
    public void SetMagicCastMessageId(uint messageId)
    {
        magicCastMessageId = messageId;
        LogDebug($"[MagicNetworkSender] 魔法施放消息ID更新为: {magicCastMessageId}");
    }
}