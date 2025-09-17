using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色UI控制器 - 负责管理角色血条显示
/// 自动挂载到MainUI物体上
/// </summary>
[RequireComponent(typeof(MainUI))]
public class PlayerUIController : MonoBehaviour
{
    [Header("角色UI管理")]
    [SerializeField] private Transform playerUIRoot;           // Player UI根节点
    [SerializeField] private Image[] playerHealthFills;       // 角色血条Fill组件数组
    [SerializeField] private PlayerHealthManager[] playerHealthManagers; // 角色生命值管理器数组
    [SerializeField] private int maxPlayerCount = 4;          // 最大支持玩家数量
    
    // 玩家ID到UI索引的映射
    private Dictionary<int, int> playerIdToUIIndex = new Dictionary<int, int>();
    private Dictionary<int, PlayerHealthManager> playerHealthManagersById = new Dictionary<int, PlayerHealthManager>();
    
    private void Awake()
    {
        // 初始化角色UI组件
        InitializePlayerUI();
    }
    
    private void Start()
    {
        // 订阅血量变化事件
        PlayerHealthManager.OnHealthChanged += OnPlayerHealthChanged;
        
        // 订阅玩家管理器事件
        PlayerManager.OnPlayerTakeDamage += OnPlayerTakeDamage;
        
        // 订阅玩家身份变更事件
        PlayerIdentity.OnPlayerIdentityChanged += OnPlayerIdentityChanged;
        
        // 订阅玩家死亡事件
        PlayerHealthManager.OnPlayerDeath += OnPlayerDeath;
        
        // 初始化血条显示
        UpdateAllHealthBars();
    }
    
    private void OnDestroy()
    {
        // 取消订阅血量变化事件
        PlayerHealthManager.OnHealthChanged -= OnPlayerHealthChanged;
        
        // 取消订阅玩家管理器事件
        PlayerManager.OnPlayerTakeDamage -= OnPlayerTakeDamage;
        
        // 取消订阅玩家身份变更事件
        PlayerIdentity.OnPlayerIdentityChanged -= OnPlayerIdentityChanged;
        
        // 取消订阅玩家死亡事件
        PlayerHealthManager.OnPlayerDeath -= OnPlayerDeath;
    }
    
    /// <summary>
    /// 初始化角色UI系统
    /// </summary>
    private void InitializePlayerUI()
    {
        // 自动查找Player子物体
        Transform playerTransform = transform.Find("Player");
        if (playerTransform == null)
        {
            Debug.LogError("[PlayerUIController] 未找到Player子物体");
            return;
        }
        
        playerUIRoot = playerTransform;
        
        // 初始化数组
        playerHealthFills = new Image[2];
        playerHealthManagers = new PlayerHealthManager[2];
        
        // 自动查找Player1和Player2的UI组件
        for (int i = 0; i < 2; i++)
        {
            string playerName = $"Player{i + 1}";
            Transform playerUITransform = playerTransform.Find(playerName);
            
            if (playerUITransform != null)
            {
                // 查找HealthBar子物体
                Transform healthBarTransform = playerUITransform.Find("HealthBar");
                if (healthBarTransform != null)
                {
                    // 查找Fill子物体的Image组件
                    Transform fillTransform = healthBarTransform.Find("Fill");
                    if (fillTransform != null)
                    {
                        playerHealthFills[i] = fillTransform.GetComponent<Image>();
                        if (playerHealthFills[i] != null)
                        {
                            playerHealthFills[i].fillAmount = 1f; // 初始化为满血状态
                            Debug.Log($"[PlayerUIController] {playerName}血条组件初始化成功");
                        }
                        else
                        {
                            Debug.LogError($"[PlayerUIController] {playerName}的Fill物体上未找到Image组件");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[PlayerUIController] {playerName}的HealthBar下未找到Fill子物体");
                    }
                }
                else
                {
                    Debug.LogError($"[PlayerUIController] {playerName}下未找到HealthBar子物体");
                }
                
                // 查找或创建PlayerHealthManager组件
                playerHealthManagers[i] = playerUITransform.GetComponent<PlayerHealthManager>();
                if (playerHealthManagers[i] == null)
                {
                    // 如果没有找到，尝试在场景中查找对应的角色对象
                    GameObject playerObject = GameObject.Find(playerName);
                    if (playerObject != null)
                    {
                        playerHealthManagers[i] = playerObject.GetComponent<PlayerHealthManager>();
                        if (playerHealthManagers[i] == null)
                        {
                            // 如果角色对象上也没有，则自动添加组件
                            playerHealthManagers[i] = playerObject.AddComponent<PlayerHealthManager>();
                            Debug.Log($"[PlayerUIController] 为{playerName}自动添加PlayerHealthManager组件");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerUIController] 未找到{playerName}角色对象，无法关联生命值管理器");
                    }
                }
            }
            else
            {
                Debug.LogError($"[PlayerUIController] 未找到{playerName}子物体");
            }
        }
        
        Debug.Log("[PlayerUIController] 角色UI系统自动初始化完成");
    }
    
    /// <summary>
    /// 隐藏指定玩家的UI
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void HidePlayerUI(int playerId)
    {
        if (playerIdToUIIndex.TryGetValue(playerId, out int uiIndex))
        {
            string playerName = $"Player{uiIndex + 1}";
            Transform playerUITransform = playerUIRoot.Find(playerName);
            if (playerUITransform != null)
            {
                playerUITransform.gameObject.SetActive(false);
                Debug.Log($"[PlayerUIController] 玩家{playerId}的UI已隐藏");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerUIController] 未找到玩家{playerId}的UI映射");
        }
    }
    
    /// <summary>
    /// 显示指定玩家的UI
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void ShowPlayerUI(int playerId)
    {
        if (playerIdToUIIndex.TryGetValue(playerId, out int uiIndex))
        {
            string playerName = $"Player{uiIndex + 1}";
            Transform playerUITransform = playerUIRoot.Find(playerName);
            if (playerUITransform != null)
            {
                playerUITransform.gameObject.SetActive(true);
                Debug.Log($"[PlayerUIController] 玩家{playerId}的UI已显示");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerUIController] 未找到玩家{playerId}的UI映射");
        }
    }
    
    /// <summary>
    /// 玩家身份变更事件处理
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="playerName">玩家名称</param>
    private void OnPlayerIdentityChanged(int playerId, string playerName)
    {
        // 为玩家分配UI索引
        int uiIndex = playerId - 1; // 玩家ID从1开始，UI索引从0开始
        
        if (uiIndex >= 0 && uiIndex < playerHealthFills.Length)
        {
            playerIdToUIIndex[playerId] = uiIndex;
            
            // 查找并关联玩家的健康管理器
            GameObject playerObject = GameObject.Find($"Player_{playerId}");
            if (playerObject != null)
            {
                PlayerHealthManager healthManager = playerObject.GetComponent<PlayerHealthManager>();
                if (healthManager != null)
                {
                    playerHealthManagersById[playerId] = healthManager;
                    
                    // 更新血条显示
                    UpdatePlayerHealthBarById(playerId, healthManager.CurrentHealth, healthManager.MaxHealth);
                    
                    Debug.Log($"[PlayerUIController] 玩家{playerId}({playerName})已关联到UI索引{uiIndex}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerUIController] 玩家ID{playerId}超出UI支持范围(1-{playerHealthFills.Length})");
        }
    }
    
    /// <summary>
    /// 玩家受伤事件处理
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="damage">伤害值</param>
    /// <param name="attackerName">攻击者名称</param>
    private void OnPlayerTakeDamage(int playerId, int damage, string attackerName)
    {
        if (playerHealthManagersById.ContainsKey(playerId))
        {
            PlayerHealthManager healthManager = playerHealthManagersById[playerId];
            
            // 让健康管理器处理伤害
            healthManager.TakeDamage(damage);
            
            // 更新对应的血条
            UpdatePlayerHealthBarById(playerId, healthManager.CurrentHealth, healthManager.MaxHealth);
            
            Debug.Log($"[PlayerUIController] 玩家{playerId}受到{damage}点伤害，来自{attackerName}");
        }
        else
        {
            Debug.LogWarning($"[PlayerUIController] 未找到玩家{playerId}的健康管理器");
        }
    }
    
    /// <summary>
    /// 根据玩家ID更新血条显示
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="currentHealth">当前血量</param>
    /// <param name="maxHealth">最大血量</param>
    public void UpdatePlayerHealthBarById(int playerId, int currentHealth, int maxHealth)
    {
        if (playerIdToUIIndex.ContainsKey(playerId))
        {
            int uiIndex = playerIdToUIIndex[playerId];
            
            if (uiIndex >= 0 && uiIndex < playerHealthFills.Length && playerHealthFills[uiIndex] != null)
            {
                float healthPercentage = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
                playerHealthFills[uiIndex].fillAmount = healthPercentage;
                
                Debug.Log($"[PlayerUIController] 玩家{playerId}血条更新: {currentHealth}/{maxHealth} ({healthPercentage:P0})");
            }
        }
    }
    
    /// <summary>
    /// 清除所有玩家UI映射
    /// </summary>
    public void ClearAllPlayerUI()
    {
        playerIdToUIIndex.Clear();
        playerHealthManagersById.Clear();
        
        // 重置所有血条
        for (int i = 0; i < playerHealthFills.Length; i++)
        {
            if (playerHealthFills[i] != null)
            {
                playerHealthFills[i].fillAmount = 1f;
            }
        }
        
        Debug.Log($"[PlayerUIController] 所有玩家UI映射已清除");
    }
    
    /// <summary>
    /// 玩家死亡事件处理
    /// </summary>
    /// <param name="playerId">死亡玩家ID</param>
    private void OnPlayerDeath(int playerId)
    {
        // 通知PlayerManager处理模型禁用
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.DisablePlayer(playerId);
            // 检查游戏状态（是否全员死亡）
            PlayerManager.Instance.OnPlayerDeathStateCheck(playerId);
        }
        
        // UI控制器只处理UI相关逻辑
        if (playerIdToUIIndex.ContainsKey(playerId))
        {
            int uiIndex = playerIdToUIIndex[playerId];
            if (uiIndex >= 0 && uiIndex < playerHealthFills.Length && playerHealthFills[uiIndex] != null)
            {
                // 确保血条为空
                playerHealthFills[uiIndex].fillAmount = 0f;
                Debug.Log($"[PlayerUIController] 玩家{playerId}血条已清空");
            }
            
            // 隐藏玩家UI
            HidePlayerUI(playerId);
        }
    }
    
    /// <summary>
    /// 角色血量变化事件处理
    /// </summary>
    /// <param name="currentHealth">当前生命值</param>
    /// <param name="maxHealth">最大生命值</param>
    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        // 更新所有角色的血条显示
        UpdateAllHealthBars();
    }
    
    /// <summary>
    /// 更新所有角色的血条显示
    /// </summary>
    private void UpdateAllHealthBars()
    {
        for (int i = 0; i < playerHealthManagers.Length; i++)
        {
            UpdateHealthBar(i);
        }
    }
    
    /// <summary>
    /// 更新指定角色的血条显示
    /// </summary>
    /// <param name="playerIndex">角色索引（0-1）</param>
    private void UpdateHealthBar(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerHealthFills.Length)
        {
            Debug.LogError($"[PlayerUIController] 无效的角色索引: {playerIndex}");
            return;
        }
        
        if (playerHealthFills[playerIndex] == null)
        {
            Debug.LogWarning($"[PlayerUIController] Player{playerIndex + 1}的血条Fill组件为空");
            return;
        }
        
        if (playerHealthManagers[playerIndex] == null)
        {
            Debug.LogWarning($"[PlayerUIController] Player{playerIndex + 1}的生命值管理器为空");
            return;
        }
        
        // 计算血量百分比并更新Fill Amount
        float healthPercentage = playerHealthManagers[playerIndex].HealthPercentage;
        playerHealthFills[playerIndex].fillAmount = healthPercentage;
        
        Debug.Log($"[PlayerUIController] Player{playerIndex + 1}血条更新：{playerHealthManagers[playerIndex].CurrentHealth}/{playerHealthManagers[playerIndex].MaxHealth} ({healthPercentage:P0})");
    }
    
    /// <summary>
    /// 获取指定角色的生命值管理器
    /// </summary>
    /// <param name="playerIndex">角色索引（0-1）</param>
    /// <returns>生命值管理器</returns>
    public PlayerHealthManager GetPlayerHealthManager(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerHealthManagers.Length)
        {
            Debug.LogError($"[PlayerUIController] 无效的角色索引: {playerIndex}");
            return null;
        }
        
        return playerHealthManagers[playerIndex];
    }
    
    /// <summary>
    /// 获取指定角色的血条Fill组件
    /// </summary>
    /// <param name="playerIndex">角色索引（0-1）</param>
    /// <returns>血条Fill组件</returns>
    public Image GetPlayerHealthFill(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerHealthFills.Length)
        {
            Debug.LogError($"[PlayerUIController] 无效的角色索引: {playerIndex}");
            return null;
        }
        
        return playerHealthFills[playerIndex];
    }
    
    /// <summary>
    /// 手动刷新所有血条显示
    /// </summary>
    [ContextMenu("刷新血条显示")]
    public void RefreshHealthBars()
    {
        UpdateAllHealthBars();
        Debug.Log("[PlayerUIController] 手动刷新血条显示完成");
    }
    
    /// <summary>
    /// 测试方法：模拟Player1受到伤害
    /// </summary>
    [ContextMenu("测试Player1受伤")]
    public void TestPlayer1TakeDamage()
    {
        if (playerHealthManagers[0] != null)
        {
            playerHealthManagers[0].TakeDamage(25);
        }
    }
    
    /// <summary>
    /// 测试方法：模拟Player2恢复生命值
    /// </summary>
    [ContextMenu("测试Player2恢复")]
    public void TestPlayer2Heal()
    {
        if (playerHealthManagers[1] != null)
        {
            playerHealthManagers[1].Heal(30);
        }
    }
}