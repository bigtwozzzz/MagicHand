using UnityEngine;
using System;

/// <summary>
/// 怪物运行时数据类
/// 管理怪物的运行时状态信息
/// </summary>
public class MonsterRuntimeData : MonoBehaviour
{
    [Header("运行时标识")]
    public string uniqueNumber;         // 独立编号，用于区别具体怪物对象
    public int configId;                // 对应的配置ID
    
    [Header("运行时属性")]
    public int currentHealth;           // 当前血量
    public Vector3 currentPosition;     // 当前位置坐标
    public float speedMultiplier = 1.0f; // 当前速度倍数（用于减速效果）
    public bool isAlive = true;         // 是否存活
    
    [Header("运行时状态")]
    public float spawnTime;             // 生成时间
    public bool isMoving;               // 是否在移动
    public bool isAttacking;            // 是否在攻击
    public Transform targetTransform;   // 当前目标
    
    [Header("引用配置")]
    private MonsterConfig config;       // 对应的配置数据
    
    /// <summary>
    /// 初始化运行时数据
    /// </summary>
    /// <param name="monsterConfig">怪物配置</param>
    /// <param name="spawnPosition">生成位置</param>
    /// <param name="number">独立编号</param>
    public void InitializeRuntimeData(MonsterConfig monsterConfig, Vector3 spawnPosition, string number)
    {
        if (monsterConfig == null)
        {
            Debug.LogError("[MonsterRuntimeData] 初始化失败：配置数据为空");
            return;
        }
        
        // 设置配置引用
        config = monsterConfig;
        configId = monsterConfig.id;
        uniqueNumber = number;
        
        // 从配置中初始化属性
        currentHealth = monsterConfig.maxHealth;
        speedMultiplier = 1.0f;
        isAlive = true;
        
        // 设置位置
        currentPosition = spawnPosition;
        transform.position = spawnPosition;
        
        // 初始化运行时状态
        spawnTime = Time.time;
        isMoving = false;
        isAttacking = false;
        targetTransform = null;
        
        if (config.enableDebugLog)
        {
            Debug.Log($"[MonsterRuntimeData] 怪物 {uniqueNumber} 初始化完成，配置ID: {configId}, 位置: {spawnPosition}");
        }
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        if (!isAlive) return;
        
        currentHealth -= damage;
        
        if (config != null && config.enableDebugLog)
        {
            Debug.Log($"[MonsterRuntimeData] 怪物 {uniqueNumber} 受到 {damage} 点伤害，剩余血量: {currentHealth}");
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 死亡处理
    /// </summary>
    private void Die()
    {
        isAlive = false;
        isMoving = false;
        isAttacking = false;
        
        if (config != null && config.enableDebugLog)
        {
            Debug.Log($"[MonsterRuntimeData] 怪物 {uniqueNumber} 死亡");
        }
        
        // 触发死亡事件
        if (MonsterEventManager.Instance != null)
        {
            MonsterEventManager.Instance.TriggerMonsterDeath(this);
        }
    }
    
    /// <summary>
    /// 更新位置
    /// </summary>
    /// <param name="newPosition">新位置</param>
    public void UpdatePosition(Vector3 newPosition)
    {
        currentPosition = newPosition;
        transform.position = newPosition;
    }
    
    /// <summary>
    /// 设置速度倍数（用于减速效果）
    /// </summary>
    /// <param name="multiplier">速度倍数</param>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
        
        if (config != null && config.enableDebugLog)
        {
            Debug.Log($"[MonsterRuntimeData] 怪物 {uniqueNumber} 速度倍数设置为: {speedMultiplier}");
        }
    }
    
    /// <summary>
    /// 获取当前实际移动速度
    /// </summary>
    public float GetCurrentMoveSpeed()
    {
        if (config == null) return 0f;
        return config.GetActualMoveSpeed() * speedMultiplier;
    }
    
    /// <summary>
    /// 获取配置数据
    /// </summary>
    public MonsterConfig GetConfig()
    {
        return config;
    }
    
    /// <summary>
    /// 获取最大血量
    /// </summary>
    public int GetMaxHealth()
    {
        return config != null ? config.maxHealth : 0;
    }
    
    /// <summary>
    /// 检查是否可以攻击
    /// </summary>
    public bool CanAttack()
    {
        return isAlive && !isAttacking && config != null;
    }
    
    /// <summary>
    /// 检查是否可以移动
    /// </summary>
    public bool CanMove()
    {
        return isAlive && !isAttacking;
    }
}