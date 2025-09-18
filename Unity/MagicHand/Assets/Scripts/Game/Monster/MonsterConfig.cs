using System;
using UnityEngine;

/// <summary>
/// 怪物纯配置数据类
/// 存储从JSON配置文件中读取的怪物基础属性
/// </summary>
[Serializable]
public class MonsterConfig
{
    [Header("基础信息")]
    public int id;                      // 怪物ID
    public string name;                 // 怪物名称
    public string description;          // 怪物描述
    public MonsterAIType aiType;        // AI类型
    
    [Header("属性配置")]
    public int maxHealth;               // 最大血量
    public float moveSpeed;             // 移动速度
    public int attackDamage;            // 攻击伤害
    public float attackRange;           // 攻击范围
    public float attackInterval;        // 攻击间隔
    public float detectionRange;        // 检测范围
    
    [Header("视觉配置")]
    public float scaleMultiplier = 1.0f; // 缩放倍数
    public Color tintColor = Color.white; // 着色
    public Vector3 worldOffset = new Vector3(0, 2.0f, 0); // 血条世界偏移
    
    [Header("掉落配置")]
    public DropItem[] dropItems;        // 掉落物品列表
    
    [Header("调试配置")]
    public bool enableDebugLog;         // 启用调试日志
    
    /// <summary>
    /// 获取AI类型
    /// </summary>
    public MonsterAIType GetAIType()
    {
        return aiType;
    }
    
    /// <summary>
    /// 获取实际移动速度（考虑缩放）
    /// </summary>
    public float GetActualMoveSpeed()
    {
        return moveSpeed * scaleMultiplier;
    }
    
    /// <summary>
    /// 获取攻击间隔
    /// </summary>
    public float GetAttackInterval()
    {
        return attackInterval;
    }
}

/// <summary>
/// 怪物AI类型枚举
/// </summary>
public enum MonsterAIType
{
    Passive,    // 被动型：不主动攻击
    Aggressive, // 攻击型：主动攻击玩家
    Defensive,  // 防御型：被攻击后反击
    Patrol      // 巡逻型：按路径巡逻
}

/// <summary>
/// 掉落物品配置
/// </summary>
[Serializable]
public class DropItem
{
    public int itemId;          // 物品ID
    public string itemName;     // 物品名称
    public float dropRate;      // 掉落概率 (0-1)
    public int minCount;        // 最小掉落数量
    public int maxCount;        // 最大掉落数量
}