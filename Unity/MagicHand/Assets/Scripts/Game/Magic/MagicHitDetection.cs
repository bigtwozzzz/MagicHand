using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 魔法命中判定脚本
/// 处理魔法攻击的命中判定、伤害计算和怪物死亡逻辑
/// </summary>
public class MagicHitDetection : MonoBehaviour
{
    [Header("调试配置")]
    public bool enableDebugLog = true;
    public bool enableDebugDraw = false;
    
    void Awake()
    {
        // 订阅魔法施放事件
        MagicManager.OnMagicCast += OnMagicCast;
    }
    
    void OnDestroy()
    {
        // 取消订阅魔法施放事件
        MagicManager.OnMagicCast -= OnMagicCast;
    }
    
    /// <summary>
    /// 处理魔法施放事件
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <param name="magicData">魔法数据</param>
    private void OnMagicCast(int magicId, MagicData magicData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 收到魔法施放事件，魔法: {magicData.magicName}");
        }
        
        // 获取施法位置（这里使用玩家位置，实际项目中可能需要更精确的位置）
        Vector3 castPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        Vector3 castDirection = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        
        // 执行命中判定
        ExecuteMagicHitDetection(magicData, castPosition, castDirection);
    }
    
    /// <summary>
    /// 执行魔法命中判定
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    /// <param name="castPosition">施法位置</param>
    /// <param name="castDirection">施法方向</param>
    /// <returns>命中的怪物数量</returns>
    public int ExecuteMagicHitDetection(MagicData magicData, Vector3 castPosition, Vector3 castDirection)
    {
        if (magicData == null)
        {
            Debug.LogError("[MagicHitDetection] 魔法数据为空");
            return 0;
        }
        
        if (MonsterPoolMgr.Instance == null)
        {
            Debug.LogError("[MagicHitDetection] MonsterPoolMgr实例不存在");
            return 0;
        }
        
        int hitCount = 0;
        List<string> hitMonsters = new List<string>();
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 开始执行魔法命中判定，魔法: {magicData.magicName}, 施法位置: {castPosition}");
        }
        
        // 获取所有活跃怪物并进行命中判定
        var activeMonsters = GetAllActiveMonsters();
        
        foreach (var monsterPair in activeMonsters)
        {
            string uniqueNumber = monsterPair.Key;
            GameObject monsterObj = monsterPair.Value;
            
            if (monsterObj == null) continue;
            
            MonsterRuntimeData runtimeData = monsterObj.GetComponent<MonsterRuntimeData>();
            if (runtimeData == null || !runtimeData.isAlive) continue;
            
            // 检查怪物是否在魔法攻击范围内
            if (IsMonsterInMagicRange(magicData, castPosition, castDirection, runtimeData.currentPosition))
            {
                hitMonsters.Add(uniqueNumber);
                
                // 计算伤害并应用
                int damage = CalculateDamage(magicData, runtimeData);
                int remainingHealth = runtimeData.currentHealth - damage;
                
                if (enableDebugLog)
                {
                    Debug.Log($"[MagicHitDetection] 怪物 {uniqueNumber} 被命中，伤害: {damage}, 剩余血量: {remainingHealth}");
                }
                
                // 触发怪物受击动画
                MonsterAnimeMgr animeMgr = monsterObj.GetComponent<MonsterAnimeMgr>();
                if (animeMgr != null)
                {
                    animeMgr.TriggerHit();
                }
                
                // 应用伤害
                runtimeData.TakeDamage(damage);
                
                // 死亡处理由MonsterRuntimeData.TakeDamage()内部触发事件，MonsterPoolMgr会自动回收
                
                hitCount++;
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 魔法命中判定完成，命中怪物数量: {hitCount}");
        }
        
        return hitCount;
    }
    
    /// <summary>
    /// 获取所有活跃怪物
    /// </summary>
    /// <returns>活跃怪物字典</returns>
    private Dictionary<string, GameObject> GetAllActiveMonsters()
    {
        return MonsterPoolMgr.Instance.GetAllActiveMonsters();
    }
    
    /// <summary>
    /// 检查怪物是否在魔法攻击范围内
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    /// <param name="castPosition">施法位置</param>
    /// <param name="castDirection">施法方向</param>
    /// <param name="monsterPosition">怪物位置</param>
    /// <returns>是否在范围内</returns>
    private bool IsMonsterInMagicRange(MagicData magicData, Vector3 castPosition, Vector3 castDirection, Vector3 monsterPosition)
    {
        if (magicData.range == null)
        {
            Debug.LogWarning("[MagicHitDetection] 魔法范围数据为空");
            return false;
        }
        
        // 使用MagicRange的IsPositionInRange方法进行判定
        return magicData.range.IsPositionInRange(monsterPosition, castPosition);
    }
    
    /// <summary>
    /// 计算对怪物造成的伤害
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    /// <param name="runtimeData">怪物运行时数据</param>
    /// <returns>伤害值</returns>
    private int CalculateDamage(MagicData magicData, MonsterRuntimeData runtimeData)
    {
        int baseDamage = Mathf.RoundToInt(magicData.damage);
        
        // 这里可以根据需要添加更复杂的伤害计算逻辑
        // 例如：根据怪物类型、魔法类型、距离等因素调整伤害
        
        // 简单的伤害计算：直接使用魔法基础伤害
        int finalDamage = baseDamage;
        
        // 确保伤害不为负数
        return Mathf.Max(0, finalDamage);
    }
    

    
    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!enableDebugDraw) return;
        
        // 这里可以添加魔法范围的可视化绘制
        // 例如绘制最近一次施法的攻击范围
    }
}