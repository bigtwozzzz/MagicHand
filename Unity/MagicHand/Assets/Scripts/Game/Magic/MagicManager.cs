using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 魔法管理器
/// 监听手势事件，处理魔法触发逻辑
/// </summary>
public class MagicManager : MonoBehaviour
{
    [Header("复合手势配置")]
    [SerializeField] private int maxGestureHistory = 5;
    [SerializeField] private float gestureTimeWindow = 2f;
    
    // 单例实例
    public static MagicManager Instance { get; private set; }
    
    // 手势历史记录
    private List<GestureRecord> gestureHistory = new List<GestureRecord>();
    
    // 复合手势映射
    private Dictionary<string, int> comboGestureMap = new Dictionary<string, int>();
    
    // 功能性手势ID列表（需要跳过的手势）
    private HashSet<int> functionalGestures = new HashSet<int> { 0, 1, 2, 5, 6, 7, 8, 9, 10 };
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeComboGestures();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 订阅手势检测事件
        SubscribeToGestureEvents();
    }
    
    void OnDestroy()
    {
        // 取消订阅
        UnsubscribeFromGestureEvents();
    }
    
    /// <summary>
    /// 订阅手势事件
    /// </summary>
    private void SubscribeToGestureEvents()
    {
        // 订阅手势检测事件
        GestureEventManager.SubscribeToGesture(OnGestureDetected);
    }
    
    /// <summary>
    /// 取消订阅手势事件
    /// </summary>
    private void UnsubscribeFromGestureEvents()
    {
        GestureEventManager.UnsubscribeFromGesture(OnGestureDetected);
    }
    
    /// <summary>
    /// 初始化复合手势映射
    /// </summary>
    private void InitializeComboGestures()
    {
        // 魔法22：握拳(3) -> 张掌(4)
        comboGestureMap["3,4"] = 22;
        
        Debug.Log("[MagicManager] 复合手势映射初始化完成");
    }
    
    /// <summary>
    /// 手势检测事件处理
    /// </summary>
    private void OnGestureDetected(int gestureId)
    {
        // 检查游戏是否暂停
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
        {
            return;
        }
        
        // 跳过功能性手势
        if (functionalGestures.Contains(gestureId))
        {
            Debug.Log($"[MagicManager] 跳过功能性手势: {gestureId}");
            return;
        }
        
        // 触发手势检测事件
        MagicEventSystem.DetectGesture(gestureId);
        
        // 记录手势历史
        RecordGesture(gestureId);
        
        // 检查复合手势
        if (CheckComboGestures())
        {
            return; // 如果触发了复合手势，就不处理单一手势
        }
        
        // 尝试触发单一手势魔法
        TryTriggerMagic(gestureId);
    }
    
    /// <summary>
    /// 记录手势历史
    /// </summary>
    private void RecordGesture(int gestureId)
    {
        // 添加新手势记录
        gestureHistory.Add(new GestureRecord(gestureId, Time.time));
        
        // 清理过期的手势记录
        float currentTime = Time.time;
        gestureHistory.RemoveAll(record => currentTime - record.timestamp > gestureTimeWindow);
        
        // 限制历史记录数量
        while (gestureHistory.Count > maxGestureHistory)
        {
            gestureHistory.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// 检查复合手势
    /// </summary>
    private bool CheckComboGestures()
    {
        if (gestureHistory.Count < 2) return false;
        
        // 构建手势序列字符串
        List<string> gestureSequence = new List<string>();
        foreach (var record in gestureHistory)
        {
            gestureSequence.Add(record.gestureId.ToString());
        }
        
        // 检查所有可能的复合手势组合
        for (int i = 0; i < gestureSequence.Count - 1; i++)
        {
            for (int j = i + 1; j < gestureSequence.Count; j++)
            {
                string comboKey = string.Join(",", gestureSequence.GetRange(i, j - i + 1));
                
                if (comboGestureMap.ContainsKey(comboKey))
                {
                    int magicId = comboGestureMap[comboKey];
                    Debug.Log($"[MagicManager] 检测到复合手势: {comboKey} -> 魔法 {magicId}");
                    
                    // 触发复合手势魔法
                    TryTriggerMagic(magicId);
                    
                    // 清空手势历史，避免重复触发
                    gestureHistory.Clear();
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 尝试触发魔法
    /// </summary>
    private void TryTriggerMagic(int magicId)
    {
        // 检查游戏是否暂停
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
        {
            Debug.Log($"[MagicManager] 游戏暂停中，无法触发魔法 {magicId}");
            return;
        }
        
        // 检查本地玩家是否死亡
        if (IsLocalPlayerDead())
        {
            Debug.Log($"[MagicManager] 玩家已死亡，无法触发魔法 {magicId}");
            return;
        }
        
        // 检查魔法配置是否存在
        if (MagicConfigLoader.Instance == null || !MagicConfigLoader.Instance.IsConfigLoaded)
        {
            Debug.LogWarning($"[MagicManager] 魔法配置未加载，无法触发魔法 {magicId}");
            return;
        }
        
        MagicData magicData = MagicConfigLoader.Instance.GetMagicData(magicId);
        if (magicData == null)
        {
            Debug.LogWarning($"[MagicManager] 魔法 {magicId} 不存在");
            return;
        }
        
        // 检查魔法是否启用
        if (!magicData.isEnabled)
        {
            Debug.Log($"[MagicManager] 魔法 {magicData.magicName} 已禁用");
            return;
        }
        
        // 检查冷却状态
        if (MagicCooldown.Instance != null && MagicCooldown.Instance.IsOnCooldown(magicId))
        {
            float remainingTime = MagicCooldown.Instance.GetRemainingCooldown(magicId);
            Debug.Log($"[MagicManager] 魔法 {magicData.magicName} 冷却中，剩余时间: {remainingTime:F1}s");
            
            // 显示冷却提示UI
            if (MagicUIMgr.Instance != null)
            {
                MagicUIMgr.Instance.ShowCooldownTip(magicId, remainingTime);
            }
            
            return;
        }
        
        // 触发魔法
        TriggerMagic(magicId, magicData);
    }
    
    // 魔法施放事件
    public static event System.Action<int, MagicData> OnMagicCast;
    
    /// <summary>
    /// 触发魔法
    /// </summary>
    private void TriggerMagic(int magicId, MagicData magicData)
    {
        Debug.Log($"[MagicManager] 触发魔法: {magicData.magicName} (ID: {magicId})");
        
        // 获取本地玩家ID（默认为1号玩家）
        int localPlayerId = GetLocalPlayerId();
        
        // 触发魔法事件
        MagicEventSystem.TriggerMagic(magicId, magicData, localPlayerId);
        
        // 触发魔法施放事件
        OnMagicCast?.Invoke(magicId, magicData);
        
        // 开始冷却
        if (MagicCooldown.Instance != null)
        {
            MagicCooldown.Instance.StartCooldown(magicId, magicData.cooldownTime);
        }
    }
    
    /// <summary>
    /// 手动触发魔法（用于测试）
    /// </summary>
    public void ManualTriggerMagic(int magicId)
    {
        TryTriggerMagic(magicId);
    }
    
    /// <summary>
    /// 获取本地玩家ID
    /// </summary>
    /// <returns>本地玩家ID，默认为1</returns>
    private int GetLocalPlayerId()
    {
        // 查找主玩家
        if (PlayerManager.Instance != null)
        {
            var players = PlayerManager.Instance.GetActivePlayers();
            foreach (var player in players)
            {
                PlayerIdentity identity = player.playerObject.GetComponent<PlayerIdentity>();
                if (identity != null && identity.IsMainPlayer)
                {
                    return identity.PlayerId;
                }
            }
        }
        
        // 默认返回1号玩家
        return 1;
    }
    
    /// <summary>
    /// 检查本地玩家是否死亡
    /// </summary>
    /// <returns>如果本地玩家死亡返回true，否则返回false</returns>
    private bool IsLocalPlayerDead()
    {
        if (PlayerManager.Instance != null)
        {
            var players = PlayerManager.Instance.GetActivePlayers();
            foreach (var player in players)
            {
                PlayerIdentity identity = player.playerObject.GetComponent<PlayerIdentity>();
                if (identity != null && identity.IsMainPlayer)
                {
                    // 获取玩家的生命值管理器
                    PlayerHealthManager healthManager = player.playerObject.GetComponent<PlayerHealthManager>();
                    if (healthManager != null)
                    {
                        return healthManager.IsDead();
                    }
                }
            }
        }
        
        // 如果找不到玩家或生命值管理器，默认认为玩家未死亡
        return false;
    }
    
    /// <summary>
    /// 添加功能性手势
    /// </summary>
    public void AddFunctionalGesture(int gestureId)
    {
        functionalGestures.Add(gestureId);
        Debug.Log($"[MagicManager] 添加功能性手势: {gestureId}");
    }
    
    /// <summary>
    /// 移除功能性手势
    /// </summary>
    public void RemoveFunctionalGesture(int gestureId)
    {
        functionalGestures.Remove(gestureId);
        Debug.Log($"[MagicManager] 移除功能性手势: {gestureId}");
    }
    
    /// <summary>
    /// 清空手势历史
    /// </summary>
    public void ClearGestureHistory()
    {
        gestureHistory.Clear();
        Debug.Log("[MagicManager] 清空手势历史");
    }
}

/// <summary>
/// 手势记录结构
/// </summary>
[System.Serializable]
public struct GestureRecord
{
    public int gestureId;
    public float timestamp;
    
    public GestureRecord(int id, float time)
    {
        gestureId = id;
        timestamp = time;
    }
}