using System;
using UnityEngine;

/// <summary>
/// 魔法范围数据
/// </summary>
[Serializable]
public class MagicRange
{
    public float left;
    public float right;
    public float forward;
    public float backward;
    
    public MagicRange()
    {
        left = right = forward = backward = 0f;
    }
    
    public MagicRange(float left, float right, float forward, float backward)
    {
        this.left = left;
        this.right = right;
        this.forward = forward;
        this.backward = backward;
    }
    
    /// <summary>
    /// 获取范围面积
    /// </summary>
    public float GetArea()
    {
        return (right - left) * (forward - backward);
    }
    
    /// <summary>
    /// 获取范围中心点
    /// </summary>
    public Vector3 GetCenter()
    {
        return new Vector3((left + right) / 2f, 0f, (forward + backward) / 2f);
    }
    
    /// <summary>
    /// 检查位置是否在范围内（忽略Y轴）
    /// </summary>
    public bool IsPositionInRange(Vector3 position, Vector3 casterPosition)
    {
        // 忽略Y轴位置，只考虑XZ平面的范围判定
        Vector3 targetPos = new Vector3(position.x, 0, position.z);
        Vector3 casterPos = new Vector3(casterPosition.x, 0, casterPosition.z);
        Vector3 relativePos = targetPos - casterPos;
        
        return relativePos.x >= left && relativePos.x <= right &&
               relativePos.z >= backward && relativePos.z <= forward;
    }
}

/// <summary>
/// 魔法特效配置数据
/// </summary>
[Serializable]
public class MagicEffectConfig
{
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 scale = Vector3.one;
    public float duration = 3f;
    public bool autoRecycle = true;
}

/// <summary>
/// 魔法数据类
/// 存储单个魔法的所有配置信息
/// </summary>
[Serializable]
public class MagicData
{
    [Header("基本信息")]
    public int magicId;
    public string magicName;
    public string description;
    public bool isEnabled = true;
    public int level = 1;
    
    [Header("伤害属性")]
    public float damage;
    public string damageType = "Magic";
    public float amplificationFactor = 1f;
    
    [Header("范围属性")]
    public MagicRange range;
    
    [Header("时间属性")]
    public float cooldownTime;
    public float castTime;
    
    [Header("特效配置")]
    public MagicEffectConfig effectConfig;
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public MagicData()
    {
        range = new MagicRange();
        effectConfig = new MagicEffectConfig();
    }
    
    /// <summary>
    /// 完整构造函数
    /// </summary>
    public MagicData(int id, string name, string desc, float dmg, string dmgType, 
                    MagicRange magicRange, float cooldown, float cast, bool enabled = true)
    {
        magicId = id;
        magicName = name;
        description = desc;
        damage = dmg;
        damageType = dmgType;
        range = magicRange ?? new MagicRange();
        cooldownTime = cooldown;
        castTime = cast;
        isEnabled = enabled;
        effectConfig = new MagicEffectConfig();
    }
    
    /// <summary>
    /// 获取范围面积
    /// </summary>
    public float GetRangeArea()
    {
        return range?.GetArea() ?? 0f;
    }
    
    /// <summary>
    /// 检查位置是否在魔法范围内（忽略Y轴）
    /// </summary>
    public bool IsPositionInRange(Vector3 position, Vector3 casterPosition)
    {
        if (range == null) return false;
        
        // 忽略Y轴位置，只考虑XZ平面的范围判定
        Vector3 targetPos = new Vector3(position.x, 0, position.z);
        Vector3 casterPos = new Vector3(casterPosition.x, 0, casterPosition.z);
        Vector3 relativePos = targetPos - casterPos;
        
        return relativePos.x >= range.left && relativePos.x <= range.right &&
               relativePos.z >= range.backward && relativePos.z <= range.forward;
    }
    
    /// <summary>
    /// 深拷贝魔法数据
    /// </summary>
    public MagicData DeepCopy()
    {
        var copy = new MagicData
        {
            magicId = this.magicId,
            magicName = this.magicName,
            description = this.description,
            isEnabled = this.isEnabled,
            level = this.level,
            damage = this.damage,
            damageType = this.damageType,
            amplificationFactor = this.amplificationFactor,
            cooldownTime = this.cooldownTime,
            castTime = this.castTime,
            range = new MagicRange(this.range.left, this.range.right, this.range.forward, this.range.backward)
        };
        
        if (this.effectConfig != null)
        {
            copy.effectConfig = new MagicEffectConfig
            {
                positionOffset = this.effectConfig.positionOffset,
                rotationOffset = this.effectConfig.rotationOffset,
                scale = this.effectConfig.scale,
                duration = this.effectConfig.duration,
                autoRecycle = this.effectConfig.autoRecycle
            };
        }
        
        return copy;
    }
}