using System;
using UnityEngine;

/// <summary>
/// 角色数据管理器 - 负责管理角色的生命值
/// </summary>
public class PlayerHealthManager : MonoBehaviour
{
    [Header("生命值设置")]
    [SerializeField] private int maxHealth = 100;  // 最大生命值
    [SerializeField] private int currentHealth;    // 当前生命值
    
    /// <summary>
    /// 血量变化事件 - 参数：当前生命值，最大生命值
    /// </summary>
    public static event Action<int, int> OnHealthChanged;
    
    /// <summary>
    /// 玩家死亡事件 - 参数：玩家ID
    /// </summary>
    public static event Action<int> OnPlayerDeath;
    
    /// <summary>
    /// 当前生命值属性
    /// </summary>
    public int CurrentHealth => currentHealth;
    
    /// <summary>
    /// 最大生命值属性
    /// </summary>
    public int MaxHealth => maxHealth;
    
    /// <summary>
    /// 生命值百分比（0-1）
    /// </summary>
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    
    private void Awake()
    {
        // 初始化当前生命值为最大生命值
        currentHealth = maxHealth;
        Debug.Log($"[PlayerHealthManager] 角色生命值初始化：{currentHealth}/{maxHealth}");
    }
    
    private void Start()
    {
        // 触发初始血量变化事件
        TriggerHealthChangedEvent();
    }
    
    /// <summary>
    /// 扣除角色生命值
    /// </summary>
    /// <param name="damage">扣除的生命值</param>
    /// <returns>是否成功扣除</returns>
    public bool TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            Debug.LogWarning($"[PlayerHealthManager] 无效的伤害值：{damage}");
            return false;
        }
        
        bool wasAlive = !IsDead();
        
        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        Debug.Log($"[PlayerHealthManager] 角色受到伤害：{damage}，生命值：{previousHealth} -> {currentHealth}");
        
        // 触发血量变化事件
        TriggerHealthChangedEvent();
        
        // 检查是否刚刚死亡
        if (wasAlive && IsDead())
        {
            // 获取玩家ID并触发死亡事件
            PlayerIdentity identity = GetComponent<PlayerIdentity>();
            if (identity != null)
            {
                OnPlayerDeath?.Invoke(identity.PlayerId);
                Debug.Log($"[PlayerHealthManager] 玩家{identity.PlayerId}已死亡");
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 恢复角色生命值
    /// </summary>
    /// <param name="healAmount">恢复的生命值</param>
    /// <returns>是否成功恢复</returns>
    public bool Heal(int healAmount)
    {
        if (healAmount <= 0)
        {
            Debug.LogWarning($"[PlayerHealthManager] 无效的治疗值：{healAmount}");
            return false;
        }
        
        if (currentHealth >= maxHealth)
        {
            Debug.Log("[PlayerHealthManager] 生命值已满，无需治疗");
            return false;
        }
        
        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        
        Debug.Log($"[PlayerHealthManager] 角色恢复生命值：{healAmount}，生命值：{previousHealth} -> {currentHealth}");
        
        // 触发血量变化事件
        TriggerHealthChangedEvent();
        
        return true;
    }
    
    /// <summary>
    /// 增加最大生命值
    /// </summary>
    /// <param name="increaseAmount">增加的最大生命值</param>
    /// <param name="healToFull">是否同时恢复到满血</param>
    /// <returns>是否成功增加</returns>
    public bool IncreaseMaxHealth(int increaseAmount, bool healToFull = false)
    {
        if (increaseAmount <= 0)
        {
            Debug.LogWarning($"[PlayerHealthManager] 无效的最大生命值增加量：{increaseAmount}");
            return false;
        }
        
        int previousMaxHealth = maxHealth;
        maxHealth += increaseAmount;
        
        if (healToFull)
        {
            currentHealth = maxHealth;
        }
        
        Debug.Log($"[PlayerHealthManager] 最大生命值增加：{increaseAmount}，最大生命值：{previousMaxHealth} -> {maxHealth}，当前生命值：{currentHealth}");
        
        // 触发血量变化事件
        TriggerHealthChangedEvent();
        
        return true;
    }
    
    /// <summary>
    /// 设置生命值（用于调试或特殊情况）
    /// </summary>
    /// <param name="newHealth">新的生命值</param>
    public void SetHealth(int newHealth)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        
        Debug.Log($"[PlayerHealthManager] 生命值设置：{previousHealth} -> {currentHealth}");
        
        // 触发血量变化事件
        TriggerHealthChangedEvent();
    }
    
    /// <summary>
    /// 设置最大生命值（用于调试或特殊情况）
    /// </summary>
    /// <param name="newMaxHealth">新的最大生命值</param>
    public void SetMaxHealth(int newMaxHealth)
    {
        if (newMaxHealth <= 0)
        {
            Debug.LogWarning($"[PlayerHealthManager] 无效的最大生命值：{newMaxHealth}");
            return;
        }
        
        int previousMaxHealth = maxHealth;
        maxHealth = newMaxHealth;
        
        // 确保当前生命值不超过新的最大生命值
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        Debug.Log($"[PlayerHealthManager] 最大生命值设置：{previousMaxHealth} -> {maxHealth}，当前生命值：{currentHealth}");
        
        // 触发血量变化事件
        TriggerHealthChangedEvent();
    }
    
    /// <summary>
    /// 检查角色是否死亡
    /// </summary>
    /// <returns>是否死亡</returns>
    public bool IsDead()
    {
        return currentHealth <= 0;
    }
    
    /// <summary>
    /// 检查角色是否满血
    /// </summary>
    /// <returns>是否满血</returns>
    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }
    
    /// <summary>
    /// 触发血量变化事件
    /// </summary>
    private void TriggerHealthChangedEvent()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 测试方法：模拟受到伤害
    /// </summary>
    [ContextMenu("测试受到伤害")]
    public void TestTakeDamage()
    {
        TakeDamage(20);
    }
    
    /// <summary>
    /// 测试方法：模拟恢复生命值
    /// </summary>
    [ContextMenu("测试恢复生命值")]
    public void TestHeal()
    {
        Heal(15);
    }
    
    /// <summary>
    /// 测试方法：增加最大生命值
    /// </summary>
    [ContextMenu("测试增加最大生命值")]
    public void TestIncreaseMaxHealth()
    {
        IncreaseMaxHealth(50, true);
    }
}