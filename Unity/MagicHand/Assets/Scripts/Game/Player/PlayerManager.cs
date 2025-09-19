using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 玩家管理器 - 负责管理所有玩家实例和编号系统
/// 支持基于怪物aiType的攻击逻辑
/// </summary>
public class PlayerManager : MonoBehaviour
{
    private static PlayerManager _instance;
    public static PlayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerManager");
                    _instance = go.AddComponent<PlayerManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("玩家管理")]
    [SerializeField] private List<PlayerData> players = new List<PlayerData>();
    
    [Header("调试信息")]
    [SerializeField] private bool enableDebugLog = true;

    /// <summary>
    /// 玩家数据结构
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public int playerId;                    // 玩家编号（从1开始）
        public GameObject playerObject;         // 玩家GameObject
        public PlayerHealthManager healthManager; // 生命值管理器
        public Vector3 spawnPosition;           // 生成位置
        public bool isActive;                   // 是否激活

        public PlayerData(int id, GameObject obj, Vector3 pos)
        {
            playerId = id;
            playerObject = obj;
            spawnPosition = pos;
            isActive = obj != null;
            
            // 获取或添加生命值管理器
            if (obj != null)
            {
                healthManager = obj.GetComponent<PlayerHealthManager>();
                if (healthManager == null)
                {
                    healthManager = obj.AddComponent<PlayerHealthManager>();
                }
            }
        }
    }

    /// <summary>
    /// 玩家受到攻击事件 - 参数：玩家ID，伤害值，攻击者信息
    /// </summary>
    public static event System.Action<int, int, string> OnPlayerTakeDamage;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 注册玩家到管理器
    /// </summary>
    /// <param name="playerObject">玩家GameObject</param>
    /// <param name="spawnPosition">生成位置</param>
    /// <returns>分配的玩家ID</returns>
    public int RegisterPlayer(GameObject playerObject, Vector3 spawnPosition)
    {
        if (playerObject == null)
        {
            Debug.LogError("[PlayerManager] 尝试注册空的玩家对象");
            return -1;
        }

        // 分配新的玩家ID
        int newPlayerId = players.Count + 1;
        
        // 创建玩家数据
        PlayerData newPlayer = new PlayerData(newPlayerId, playerObject, spawnPosition);
        players.Add(newPlayer);

        // 设置玩家对象名称
        playerObject.name = $"Player_{newPlayerId}";
        
        // 添加玩家标识组件
        PlayerIdentity identity = playerObject.GetComponent<PlayerIdentity>();
        if (identity == null)
        {
            identity = playerObject.AddComponent<PlayerIdentity>();
        }
        identity.SetPlayerId(newPlayerId);

        if (enableDebugLog)
        {
            Debug.Log($"[PlayerManager] 玩家注册成功 - ID: {newPlayerId}, 位置: {spawnPosition}");
        }

        return newPlayerId;
    }

    /// <summary>
    /// 注销玩家
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void UnregisterPlayer(int playerId)
    {
        PlayerData player = GetPlayerData(playerId);
        if (player != null)
        {
            player.isActive = false;
            if (enableDebugLog)
            {
                Debug.Log($"[PlayerManager] 玩家注销 - ID: {playerId}");
            }
        }
    }

    /// <summary>
    /// 清除所有玩家
    /// </summary>
    public void ClearAllPlayers()
    {
        foreach (var player in players)
        {
            if (player.playerObject != null)
            {
                DestroyImmediate(player.playerObject);
            }
        }
        players.Clear();
        
        // 隐藏所有玩家的血条UI
        PlayerUIController playerUIController = FindObjectOfType<PlayerUIController>();
        if (playerUIController != null)
        {
            playerUIController.ClearAllPlayerUI();
            // 隐藏所有玩家UI
            for (int i = 1; i <= 4; i++) // 假设最多支持4个玩家
            {
                playerUIController.HidePlayerUI(i);
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[PlayerManager] 所有玩家已清除，血条UI已隐藏");
        }
    }

    /// <summary>
    /// 获取玩家数据
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>玩家数据</returns>
    public PlayerData GetPlayerData(int playerId)
    {
        return players.FirstOrDefault(p => p.playerId == playerId && p.isActive);
    }

    /// <summary>
    /// 获取所有激活的玩家
    /// </summary>
    /// <returns>激活的玩家列表</returns>
    public List<PlayerData> GetActivePlayers()
    {
        return players.Where(p => p.isActive && p.playerObject != null && !IsPlayerDead(p)).ToList();
    }
    
    /// <summary>
    /// 获取所有玩家（包括死亡的玩家）
    /// </summary>
    /// <returns>所有玩家列表</returns>
    public List<PlayerData> GetAllPlayers()
    {
        return players.Where(p => p.isActive && p.playerObject != null).ToList();
    }
    
    /// <summary>
    /// 检查玩家是否死亡
    /// </summary>
    /// <param name="playerData">玩家数据</param>
    /// <returns>是否死亡</returns>
    private bool IsPlayerDead(PlayerData playerData)
    {
        if (playerData.healthManager == null) return true;
        return playerData.healthManager.IsDead();
    }

    /// <summary>
    /// 禁用玩家模型（死亡时调用）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void DisablePlayer(int playerId)
    {
        var playerData = GetPlayerData(playerId);
        if (playerData?.playerObject != null)
        {
            playerData.playerObject.SetActive(false);
            if (enableDebugLog)
            {
                Debug.Log($"[PlayerManager] 玩家{playerId}模型已禁用");
            }
        }
    }

    /// <summary>
    /// 启用玩家模型（复活时调用）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void EnablePlayer(int playerId)
    {
        var playerData = GetPlayerData(playerId);
        if (playerData?.playerObject != null)
        {
            playerData.playerObject.SetActive(true);
            if (enableDebugLog)
            {
                Debug.Log($"[PlayerManager] 玩家{playerId}模型已启用");
            }
        }
    }

    /// <summary>
    /// 检查是否所有玩家都已死亡
    /// </summary>
    /// <returns>是否全员死亡</returns>
    public bool AreAllPlayersDead()
    {
        // 如果没有玩家，返回false（避免游戏开始前就结束）
        if (players.Count == 0) return false;
        
        // 检查所有激活的玩家是否都已死亡
        foreach (var player in players)
        {
            if (player.isActive && player.playerObject != null && !IsPlayerDead(player))
            {
                return false; // 还有存活的玩家
            }
        }
        
        return true; // 所有玩家都已死亡
    }

    /// <summary>
    /// 处理玩家死亡后的游戏状态检查
    /// </summary>
    /// <param name="playerId">死亡玩家ID</param>
    public void OnPlayerDeathStateCheck(int playerId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerManager] 检查玩家{playerId}死亡后的游戏状态");
        }
        
        // 检查是否所有玩家都已死亡
        if (AreAllPlayersDead())
        {
            if (enableDebugLog)
            {
                Debug.Log("[PlayerManager] 所有玩家都已死亡，触发游戏结束");
            }
            
            // 触发游戏暂停（临时的游戏结束逻辑）
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.PauseGame();
                Debug.Log("[PlayerManager] 游戏已暂停 - 全员死亡");
            }
        }
    }

    /// <summary>
    /// 获取离指定位置最近的玩家
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <returns>最近的玩家数据</returns>
    public PlayerData GetNearestPlayer(Vector3 position)
    {
        var activePlayers = GetActivePlayers();
        if (activePlayers.Count == 0) return null;

        PlayerData nearestPlayer = null;
        float nearestDistance = float.MaxValue;

        foreach (var player in activePlayers)
        {
            float distance = Vector3.Distance(position, player.playerObject.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayer = player;
            }
        }

        return nearestPlayer;
    }

    /// <summary>
    /// 怪物攻击玩家 - 根据aiType决定攻击逻辑
    /// </summary>
    /// <param name="monsterPosition">怪物位置</param>
    /// <param name="damage">伤害值</param>
    /// <param name="aiType">怪物AI类型</param>
    /// <param name="monsterName">怪物名称</param>
    public void MonsterAttackPlayers(Vector3 monsterPosition, int damage, string aiType, string monsterName = "未知怪物")
    {
        var activePlayers = GetActivePlayers();
        if (activePlayers.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[PlayerManager] 没有激活的玩家可攻击");
            }
            return;
        }

        switch (aiType.ToLower())
        {
            case "single":
                AttackSinglePlayer(monsterPosition, damage, monsterName);
                break;
            case "multiple":
                AttackAllPlayers(damage, monsterName);
                break;
            default:
                // 默认攻击最近的玩家
                AttackSinglePlayer(monsterPosition, damage, monsterName);
                if (enableDebugLog)
                {
                    Debug.LogWarning($"[PlayerManager] 未知的aiType: {aiType}，使用默认单体攻击");
                }
                break;
        }
    }

    /// <summary>
    /// 攻击单个玩家（最近的）
    /// </summary>
    /// <param name="monsterPosition">怪物位置</param>
    /// <param name="damage">伤害值</param>
    /// <param name="monsterName">怪物名称</param>
    private void AttackSinglePlayer(Vector3 monsterPosition, int damage, string monsterName)
    {
        PlayerData targetPlayer = GetNearestPlayer(monsterPosition);
        if (targetPlayer != null && targetPlayer.healthManager != null)
        {
            targetPlayer.healthManager.TakeDamage(damage);
            
            // 触发攻击事件
            OnPlayerTakeDamage?.Invoke(targetPlayer.playerId, damage, monsterName);
            
            if (enableDebugLog)
            {
                Debug.Log($"[PlayerManager] {monsterName} 攻击玩家{targetPlayer.playerId}，造成{damage}点伤害");
            }
        }
    }

    /// <summary>
    /// 攻击所有玩家
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="monsterName">怪物名称</param>
    private void AttackAllPlayers(int damage, string monsterName)
    {
        var activePlayers = GetActivePlayers();
        foreach (var player in activePlayers)
        {
            if (player.healthManager != null)
            {
                player.healthManager.TakeDamage(damage);
                
                // 触发攻击事件
                OnPlayerTakeDamage?.Invoke(player.playerId, damage, monsterName);
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerManager] {monsterName} 攻击所有玩家({activePlayers.Count}个)，每个造成{damage}点伤害");
        }
    }

    /// <summary>
    /// 获取玩家数量
    /// </summary>
    /// <returns>激活的玩家数量</returns>
    public int GetPlayerCount()
    {
        return GetActivePlayers().Count;
    }

    /// <summary>
    /// 检查玩家是否存在
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>是否存在</returns>
    public bool HasPlayer(int playerId)
    {
        return GetPlayerData(playerId) != null;
    }
    
    /// <summary>
    /// 切换本地主玩家到指定编号
    /// </summary>
    /// <param name="newMainPlayerId">新的主玩家ID</param>
    /// <returns>是否切换成功</returns>
    public bool SwitchMainPlayer(int newMainPlayerId)
    {
        // 检查目标玩家是否存在
        PlayerData targetPlayer = GetPlayerData(newMainPlayerId);
        if (targetPlayer == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[PlayerManager] 无法切换主玩家：玩家ID {newMainPlayerId} 不存在");
            }
            return false;
        }
        
        // 取消所有玩家的主玩家状态
        foreach (var player in players)
        {
            if (player.isActive && player.playerObject != null)
            {
                PlayerIdentity identity = player.playerObject.GetComponent<PlayerIdentity>();
                if (identity != null)
                {
                    identity.SetMainPlayer(false);
                }
            }
        }
        
        // 设置新的主玩家
        PlayerIdentity targetIdentity = targetPlayer.playerObject.GetComponent<PlayerIdentity>();
        if (targetIdentity != null)
        {
            targetIdentity.SetMainPlayer(true);
            
            if (enableDebugLog)
            {
                Debug.Log($"[PlayerManager] 主玩家已切换到ID: {newMainPlayerId}");
            }
            return true;
        }
        
        if (enableDebugLog)
        {
            Debug.LogError($"[PlayerManager] 玩家ID {newMainPlayerId} 缺少PlayerIdentity组件");
        }
        return false;
    }
    
    /// <summary>
    /// 获取当前主玩家ID
    /// </summary>
    /// <returns>主玩家ID，如果没有主玩家返回-1</returns>
    public int GetMainPlayerId()
    {
        foreach (var player in players)
        {
            if (player.isActive && player.playerObject != null)
            {
                PlayerIdentity identity = player.playerObject.GetComponent<PlayerIdentity>();
                if (identity != null && identity.IsMainPlayer)
                {
                    return identity.PlayerId;
                }
            }
        }
        return -1;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // 编辑器调试信息
    #if UNITY_EDITOR
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        foreach (var player in players)
        {
            if (player.isActive && player.playerObject != null)
            {
                // 绘制玩家位置
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(player.playerObject.transform.position, 0.5f);
                
                // 绘制玩家编号
                UnityEditor.Handles.Label(player.playerObject.transform.position + Vector3.up * 2f, 
                    $"Player {player.playerId}");
            }
        }
    }
    #endif
}