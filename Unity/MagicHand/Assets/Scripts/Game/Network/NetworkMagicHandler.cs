using Broadcast;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网络魔法处理器
/// 专门负责处理来自其他客户端的魔法广播事件
/// 遵循单一职责原则，将网络相关逻辑从MagicManager中分离
/// </summary>
public class NetworkMagicHandler : MonoBehaviour
{
    // 单例实例
    public static NetworkMagicHandler Instance { get; private set; }
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 这里可以订阅网络事件
        // 例如：EventCenter.AddListener<SkillCastNotify>("OnSkillCastReceived", OnSkillCastReceived);
        LogDebug("[NetworkMagicHandler] 网络魔法处理器已初始化");
        EventCenter.GetInstance().AddEventListener<EntityAttackNotify>(E_EventType.Event_Combat_Info, OnSkillCastReceived);
    }
    
    /// <summary>
    /// 处理接收到的技能施放广播
    /// 这个方法应该被网络消息系统调用
    /// </summary>
    /// <param name="skillCastNotify">技能施放通知消息</param>
    public void OnSkillCastReceived(EntityAttackNotify skillCastNotify)
    {
        // 这里需要根据实际的SkillCastNotify结构来解析
        // 暂时使用模拟数据进行演示
        LogDebug("[NetworkMagicHandler] 接收到技能施放广播");
        string userId = skillCastNotify.EntityId;
        string skillId = skillCastNotify.SkillId;
        // TODO: 解析skillCastNotify消息，提取玩家ID和技能ID
        // 示例代码（需要根据实际消息结构调整）：
        // int casterId = skillCastNotify.entityId;
        // string skillId = skillCastNotify.skillId.ToString();
        // HandleNetworkMagicBroadcast(skillId, casterId);
    }
    
    /// <summary>
    /// 处理网络魔法广播事件
    /// 根据广播的魔法编号和玩家编号，以对应玩家为基准释放魔法
    /// </summary>
    /// <param name="skillId">技能ID（字符串格式）</param>
    /// <param name="casterId">施法者玩家编号</param>
    public void HandleNetworkMagicBroadcast(string skillId, int casterId)
    {
        LogDebug($"[NetworkMagicHandler] 处理网络魔法广播 - 技能ID: {skillId}, 施法者: 玩家{casterId}");
        
        // 检查游戏是否暂停
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
        {
            LogDebug("[NetworkMagicHandler] 游戏暂停中，跳过网络魔法处理");
            return;
        }
        
        // 检查MagicManager是否可用
        if (MagicManager.Instance == null)
        {
            Debug.LogError("[NetworkMagicHandler] MagicManager实例不存在");
            return;
        }
        
        // 获取本地玩家ID
        int localPlayerId = MagicManager.Instance.GetLocalPlayerId();
        
        // 如果施法者就是本地玩家，跳过处理（避免重复释放）
        if (casterId == localPlayerId)
        {
            LogDebug($"[NetworkMagicHandler] 跳过本地玩家{casterId}的魔法广播，避免重复释放");
            return;
        }
        
        // 将技能ID转换为魔法ID
        int magicId = ConvertSkillIdToMagicId(skillId);
        if (magicId == -1)
        {
            Debug.LogWarning($"[NetworkMagicHandler] 未知的技能ID: {skillId}");
            return;
        }
        
        // 获取魔法数据
        MagicData magicData = MagicConfigLoader.Instance?.GetMagicData(magicId);
        if (magicData == null)
        {
            Debug.LogWarning($"[NetworkMagicHandler] 魔法 {magicId} 不存在");
            return;
        }
        
        // 检查魔法是否启用
        if (!magicData.isEnabled)
        {
            LogDebug($"[NetworkMagicHandler] 魔法 {magicData.magicName} 已禁用");
            return;
        }
        
        // 验证施法者玩家是否存在
        if (PlayerManager.Instance == null || !PlayerManager.Instance.HasPlayer(casterId))
        {
            Debug.LogWarning($"[NetworkMagicHandler] 施法者玩家{casterId}不存在，无法处理网络魔法");
            return;
        }
        
        // 触发网络魔法（不进行冷却计算）
        TriggerNetworkMagic(magicId, magicData, casterId);
    }
    
    /// <summary>
    /// 触发网络魔法（无冷却版本）
    /// 专门用于处理网络广播的魔法，不进行冷却计算但会进行伤害判定
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <param name="magicData">魔法数据</param>
    /// <param name="casterId">施法者玩家编号</param>
    private void TriggerNetworkMagic(int magicId, MagicData magicData, int casterId)
    {
        LogDebug($"[NetworkMagicHandler] 处理网络魔法: {magicData.magicName} (ID: {magicId})，施法者: 玩家{casterId}");
        
        // 触发魔法事件（以指定玩家为基准）
        MagicEventSystem.TriggerMagic(magicId, magicData, casterId);
        
        // 触发魔法施放事件（用于特效和伤害判定）
        MagicManager.TriggerMagicCastEvent(magicId, magicData);
        
        // 注意：网络魔法不进行冷却计算，因为冷却在原始施法者客户端已经计算过了
        LogDebug($"[NetworkMagicHandler] 网络魔法 {magicData.magicName} 处理完成，跳过冷却计算");
    }
    
    /// <summary>
    /// 将技能ID转换为魔法ID
    /// </summary>
    /// <param name="skillId">技能ID（字符串格式）</param>
    /// <returns>对应的魔法ID，如果未找到返回-1</returns>
    private int ConvertSkillIdToMagicId(string skillId)
    {
        // 技能ID到魔法ID的映射表
        // 根据实际的技能配置进行映射
        var skillToMagicMap = new Dictionary<string, int>
        {
            { "heal_magic", 3 },        // 治疗魔法
            { "meteor_magic", 22 },     // 流星魔法
            // 可以根据需要添加更多映射
        };
        
        // 尝试直接解析为整数（如果技能ID本身就是数字字符串）
        if (int.TryParse(skillId, out int directMagicId))
        {
            return directMagicId;
        }
        
        // 从映射表中查找
        if (skillToMagicMap.TryGetValue(skillId, out int mappedMagicId))
        {
            return mappedMagicId;
        }
        
        // 未找到对应的魔法ID
        return -1;
    }
    
    /// <summary>
    /// 测试方法：模拟接收到玩家2释放魔法4的网络广播
    /// 用于测试网络魔法广播处理功能
    /// </summary>
    [System.Obsolete("此方法仅用于测试，正式版本中应移除")]
    [ContextMenu("测试网络魔法广播 - 玩家2魔法4")]
    public void TestNetworkMagicBroadcast()
    {
        LogDebug("[NetworkMagicHandler] 开始测试网络魔法广播 - 玩家2释放魔法4");
        
        // 模拟接收到玩家2释放魔法4的广播
        int testPlayerId = 2;  // 玩家2
        string testSkillId = "4";  // 魔法4
        
        // 调用网络魔法广播处理方法
        HandleNetworkMagicBroadcast(testSkillId, testPlayerId);
        
        LogDebug("[NetworkMagicHandler] 网络魔法广播测试完成");
    }
    
    /// <summary>
    /// 调试日志输出
    /// </summary>
    /// <param name="message">日志消息</param>
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log(message);
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅网络事件
        // 例如：EventCenter.RemoveListener<SkillCastNotify>("OnSkillCastReceived", OnSkillCastReceived);
        EventCenter.GetInstance().RemoveEventListener<EntityAttackNotify>(E_EventType.Event_Combat_Info,OnSkillCastReceived);
    }
}