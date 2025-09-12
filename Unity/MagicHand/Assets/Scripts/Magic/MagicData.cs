using UnityEngine;

/// <summary>
/// 魔法数据结构
/// 定义每种魔法的基本属性信息
/// </summary>
[System.Serializable]
public class MagicData
{
    [Header("基本信息")]
    [Tooltip("魔法编号，与手势编号一一对应")]
    public int magicId;
    
    [Tooltip("魔法名称")]
    public string magicName;
    
    [Tooltip("魔法描述")]
    [TextArea(2, 4)]
    public string description;
    
    [Header("伤害属性")]
    [Tooltip("魔法伤害值")]
    public float damage = 100f;
    
    [Tooltip("伤害类型")]
    public DamageType damageType = DamageType.Magic;
    
    [Header("范围属性")]
    [Tooltip("魔法作用范围（矩形区域）")]
    public MagicRange range = new MagicRange();
    
    [Header("时间属性")]
    [Tooltip("冷却时间（秒）")]
    public float cooldownTime = 3f;
    
    [Tooltip("施法时间（秒）")]
    public float castTime = 0.5f;
    

    
    [Header("视觉效果")]
    [Tooltip("魔法特效预制体")]
    public GameObject effectPrefab;
    
    [Tooltip("施法音效")]
    public AudioClip castSound;
    
    [Tooltip("魔法图标")]
    public Sprite magicIcon;
    
    [Header("其他属性")]
    [Tooltip("是否启用此魔法")]
    public bool isEnabled = true;
    
    [Tooltip("魔法等级")]
    public int level = 1;
    
    [Tooltip("增幅系数（影响伤害、范围和特效缩放）")]
    public float amplificationFactor = 1.0f;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public MagicData()
    {
        range = new MagicRange();
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">魔法编号</param>
    /// <param name="name">魔法名称</param>
    /// <param name="damage">伤害值</param>
    /// <param name="cooldown">冷却时间</param>
    public MagicData(int id, string name, float damage, float cooldown)
    {
        this.magicId = id;
        this.magicName = name;
        this.damage = damage;
        this.cooldownTime = cooldown;
        this.range = new MagicRange();
    }
    
    /// <summary>
    /// 获取魔法的总范围面积
    /// </summary>
    /// <returns>范围面积</returns>
    public float GetRangeArea()
    {
        return range.GetArea();
    }
    
    /// <summary>
    /// 检查位置是否在魔法范围内
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <param name="casterPosition">施法者位置</param>
    /// <returns>是否在范围内</returns>
    public bool IsInRange(Vector3 position, Vector3 casterPosition)
    {
        return range.IsPositionInRange(position, casterPosition);
    }
    
    /// <summary>
    /// 创建魔法数据的深拷贝
    /// </summary>
    /// <returns>拷贝的魔法数据</returns>
    public MagicData Clone()
    {
        MagicData clone = new MagicData()
        {
            magicId = this.magicId,
            magicName = this.magicName,
            description = this.description,
            damage = this.damage,
            damageType = this.damageType,
            range = this.range.Clone(),
            cooldownTime = this.cooldownTime,
            castTime = this.castTime,
            effectPrefab = this.effectPrefab,
            castSound = this.castSound,
            magicIcon = this.magicIcon,
            isEnabled = this.isEnabled,
            level = this.level,
            amplificationFactor = this.amplificationFactor
        };
        return clone;
    }
}

/// <summary>
/// 魔法范围结构
/// 定义矩形范围的四个边界值
/// </summary>
[System.Serializable]
public class MagicRange
{
    [Tooltip("左边界距离（负值）")]
    public float left = -2f;
    
    [Tooltip("右边界距离（正值）")]
    public float right = 2f;
    
    [Tooltip("前边界距离（正值）")]
    public float forward = 3f;
    
    [Tooltip("后边界距离（负值）")]
    public float backward = -1f;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public MagicRange()
    {
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="left">左边界</param>
    /// <param name="right">右边界</param>
    /// <param name="forward">前边界</param>
    /// <param name="backward">后边界</param>
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
    /// <returns>面积值</returns>
    public float GetArea()
    {
        float width = right - left;
        float depth = forward - backward;
        return width * depth;
    }
    
    /// <summary>
    /// 获取范围中心点（相对于施法者）
    /// </summary>
    /// <returns>中心点偏移</returns>
    public Vector3 GetCenter()
    {
        float centerX = (left + right) / 2f;
        float centerZ = (backward + forward) / 2f;
        return new Vector3(centerX, 0, centerZ);
    }
    
    /// <summary>
    /// 检查位置是否在范围内
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <param name="casterPosition">施法者位置</param>
    /// <returns>是否在范围内</returns>
    public bool IsPositionInRange(Vector3 position, Vector3 casterPosition)
    {
        Vector3 relativePos = position - casterPosition;
        
        return relativePos.x >= left && relativePos.x <= right &&
               relativePos.z >= backward && relativePos.z <= forward;
    }
    
    /// <summary>
    /// 获取范围的四个角点（世界坐标）
    /// </summary>
    /// <param name="casterPosition">施法者位置</param>
    /// <returns>四个角点数组</returns>
    public Vector3[] GetCorners(Vector3 casterPosition)
    {
        return new Vector3[]
        {
            casterPosition + new Vector3(left, 0, backward),   // 左后
            casterPosition + new Vector3(right, 0, backward),  // 右后
            casterPosition + new Vector3(right, 0, forward),   // 右前
            casterPosition + new Vector3(left, 0, forward)     // 左前
        };
    }
    
    /// <summary>
    /// 创建范围的深拷贝
    /// </summary>
    /// <returns>拷贝的范围数据</returns>
    public MagicRange Clone()
    {
        return new MagicRange(left, right, forward, backward);
    }
    
    /// <summary>
    /// 在Scene视图中绘制范围（仅编辑器模式）
    /// </summary>
    /// <param name="casterPosition">施法者位置</param>
    /// <param name="color">绘制颜色</param>
    public void DrawGizmos(Vector3 casterPosition, Color color)
    {
        #if UNITY_EDITOR
        Gizmos.color = color;
        Vector3[] corners = GetCorners(casterPosition);
        
        // 绘制矩形边框
        for (int i = 0; i < corners.Length; i++)
        {
            int nextIndex = (i + 1) % corners.Length;
            Gizmos.DrawLine(corners[i], corners[nextIndex]);
        }
        
        // 绘制中心点
        Vector3 center = casterPosition + GetCenter();
        Gizmos.DrawWireSphere(center, 0.2f);
        #endif
    }
}

/// <summary>
/// 伤害类型枚举
/// </summary>
public enum DamageType
{
    Physical,   // 物理伤害
    Magic,      // 魔法伤害
    Fire,       // 火焰伤害
    Ice,        // 冰霜伤害
    Lightning,  // 雷电伤害
    Poison,     // 毒素伤害
    Holy,       // 神圣伤害
    Dark        // 暗黑伤害
}