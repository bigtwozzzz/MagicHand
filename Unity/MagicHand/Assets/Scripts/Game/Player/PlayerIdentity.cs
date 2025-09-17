using UnityEngine;

/// <summary>
/// 玩家身份标识组件
/// 用于标记每个玩家的唯一ID和相关信息
/// </summary>
public class PlayerIdentity : MonoBehaviour
{
    [Header("玩家身份信息")]
    [SerializeField] private int playerId = -1;           // 玩家ID（从1开始）
    [SerializeField] private string playerName = "";      // 玩家名称
    [SerializeField] private bool isMainPlayer = false;   // 是否为主玩家
    
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;
    
    /// <summary>
    /// 玩家ID属性
    /// </summary>
    public int PlayerId => playerId;
    
    /// <summary>
    /// 玩家名称属性
    /// </summary>
    public string PlayerName => playerName;
    
    /// <summary>
    /// 是否为主玩家属性
    /// </summary>
    public bool IsMainPlayer => isMainPlayer;
    
    /// <summary>
    /// 玩家身份变更事件 - 参数：玩家ID，玩家名称
    /// </summary>
    public static event System.Action<int, string> OnPlayerIdentityChanged;
    
    private void Awake()
    {
        // 如果没有设置玩家名称，使用默认名称
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = $"Player_{playerId}";
        }
    }
    
    private void Start()
    {
        // 触发身份变更事件
        if (playerId > 0)
        {
            OnPlayerIdentityChanged?.Invoke(playerId, playerName);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerIdentity] 玩家身份初始化 - ID: {playerId}, 名称: {playerName}, 主玩家: {isMainPlayer}");
        }
    }
    
    /// <summary>
    /// 设置玩家ID
    /// </summary>
    /// <param name="id">玩家ID</param>
    public void SetPlayerId(int id)
    {
        if (id <= 0)
        {
            Debug.LogError($"[PlayerIdentity] 无效的玩家ID: {id}");
            return;
        }
        
        int oldId = playerId;
        playerId = id;
        
        // 更新玩家名称
        if (string.IsNullOrEmpty(playerName) || playerName == $"Player_{oldId}")
        {
            playerName = $"Player_{playerId}";
        }
        
        // 更新GameObject名称
        gameObject.name = $"Player_{playerId}";
        
        // 触发身份变更事件
        OnPlayerIdentityChanged?.Invoke(playerId, playerName);
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerIdentity] 玩家ID已更新: {oldId} -> {playerId}");
        }
    }
    
    /// <summary>
    /// 设置玩家名称
    /// </summary>
    /// <param name="name">玩家名称</param>
    public void SetPlayerName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning($"[PlayerIdentity] 尝试设置空的玩家名称");
            return;
        }
        
        string oldName = playerName;
        playerName = name;
        
        // 触发身份变更事件
        OnPlayerIdentityChanged?.Invoke(playerId, playerName);
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerIdentity] 玩家名称已更新: {oldName} -> {playerName}");
        }
    }
    
    /// <summary>
    /// 设置是否为主玩家
    /// </summary>
    /// <param name="isMain">是否为主玩家</param>
    public void SetMainPlayer(bool isMain)
    {
        bool oldValue = isMainPlayer;
        isMainPlayer = isMain;
        
        if (showDebugInfo && oldValue != isMainPlayer)
        {
            Debug.Log($"[PlayerIdentity] 主玩家状态已更新: {oldValue} -> {isMainPlayer}");
        }
    }
    
    /// <summary>
    /// 获取玩家完整信息
    /// </summary>
    /// <returns>玩家信息字符串</returns>
    public string GetPlayerInfo()
    {
        return $"ID: {playerId}, 名称: {playerName}, 主玩家: {isMainPlayer}";
    }
    
    /// <summary>
    /// 检查是否为有效玩家
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValidPlayer()
    {
        return playerId > 0 && !string.IsNullOrEmpty(playerName);
    }
    
    /// <summary>
    /// 重置玩家身份
    /// </summary>
    public void ResetIdentity()
    {
        playerId = -1;
        playerName = "";
        isMainPlayer = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerIdentity] 玩家身份已重置");
        }
    }
    
    // 编辑器调试显示
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || playerId <= 0) return;
        
        // 绘制玩家标识
        Gizmos.color = isMainPlayer ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // 显示玩家信息
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, 
            $"Player {playerId}\n{playerName}\n{(isMainPlayer ? "[主玩家]" : "[普通玩家]")}");
    }
    #endif
    
    private void OnDestroy()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerIdentity] 玩家{playerId}身份组件已销毁");
        }
    }
}