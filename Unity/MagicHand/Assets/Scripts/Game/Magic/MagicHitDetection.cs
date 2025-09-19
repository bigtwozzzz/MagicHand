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
        // 订阅魔法触发事件（包含施法者ID）
        MagicEventSystem.OnMagicTriggered += OnMagicTriggered;
    }
    
    void OnDestroy()
    {
        // 取消订阅魔法触发事件
        MagicEventSystem.OnMagicTriggered -= OnMagicTriggered;
    }
    
    /// <summary>
    /// 处理魔法触发事件
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <param name="magicData">魔法数据</param>
    /// <param name="playerId">施法者玩家ID</param>
    private void OnMagicTriggered(int magicId, MagicData magicData, int playerId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 收到魔法触发事件，魔法: {magicData.magicName}，施法者: 玩家{playerId}");
        }
        
        // 检查是否为治疗魔法（魔法ID 23）
        if (magicId == 23)
        {
            ExecuteHealingMagic(magicData, playerId);
            return;
        }
        
        // 检查是否为冰风暴魔法（魔法ID 30）
        if (magicId == 30)
        {
            ExecuteIceStormMagic(magicData, playerId);
            return;
        }
        
        // 检查是否为天降甘霖魔法（魔法ID 42）
        if (magicId == 42)
        {
            ExecuteHeavenlyRainMagic(magicData, playerId);
            return;
        }
        
        // 根据施法者ID获取对应玩家位置
        Vector3 castPosition = GetCasterPosition(playerId);
        Vector3 castDirection = GetCasterDirection(playerId);
        
        // 执行命中判定
        ExecuteMagicHitDetection(magicData, castPosition, castDirection);
    }
    
    /// <summary>
    /// 获取施法者位置（忽略Y轴）
    /// </summary>
    /// <param name="playerId">施法者玩家ID</param>
    /// <returns>施法者位置</returns>
    private Vector3 GetCasterPosition(int playerId)
    {
        // 根据玩家ID获取对应玩家的位置
        PlayerManager.PlayerData playerData = PlayerManager.Instance?.GetPlayerData(playerId);
        if (playerData != null && playerData.playerObject != null)
        {
            Vector3 position = playerData.playerObject.transform.position;
            // 忽略Y轴位置，设为0
            position.y = 0;
            if (enableDebugLog)
            {
                Debug.Log($"[MagicHitDetection] 使用玩家{playerId}位置作为施法基准: {position}");
            }
            return position;
        }
        else
        {
            // 回退到摄像机位置
            Vector3 fallbackPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            fallbackPosition.y = 0; // 忽略Y轴
            Debug.LogWarning($"[MagicHitDetection] 未找到玩家{playerId}，使用摄像机位置: {fallbackPosition}");
            return fallbackPosition;
        }
    }
    
    /// <summary>
    /// 获取施法者方向
    /// </summary>
    /// <param name="playerId">施法者玩家ID</param>
    /// <returns>施法者方向</returns>
    private Vector3 GetCasterDirection(int playerId)
    {
        // 根据玩家ID获取对应玩家的方向
        PlayerManager.PlayerData playerData = PlayerManager.Instance?.GetPlayerData(playerId);
        if (playerData != null && playerData.playerObject != null)
        {
            return playerData.playerObject.transform.forward;
        }
        else
        {
            // 回退到摄像机方向
            return Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        }
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
    /// 执行治疗魔法逻辑
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    /// <param name="playerId">施法者玩家ID</param>
    private void ExecuteHealingMagic(MagicData magicData, int playerId)
    {
        if (magicData == null)
        {
            Debug.LogError("[MagicHitDetection] 治疗魔法数据为空");
            return;
        }
        
        // 获取施法者玩家数据
        PlayerManager.PlayerData playerData = PlayerManager.Instance?.GetPlayerData(playerId);
        if (playerData == null || playerData.playerObject == null)
        {
            Debug.LogWarning($"[MagicHitDetection] 未找到玩家{playerId}，无法执行治疗");
            return;
        }
        
        // 获取玩家的生命值管理器
        PlayerHealthManager healthManager = playerData.playerObject.GetComponent<PlayerHealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning($"[MagicHitDetection] 玩家{playerId}没有PlayerHealthManager组件，无法执行治疗");
            return;
        }
        
        // 计算治疗量（等于魔法的伤害值）
        int healAmount = Mathf.RoundToInt(magicData.damage);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 执行治疗魔法，玩家{playerId}恢复{healAmount}点生命值");
        }
        
        // 执行治疗
        bool healSuccess = healthManager.Heal(healAmount);
        
        if (enableDebugLog)
        {
            if (healSuccess)
            {
                Debug.Log($"[MagicHitDetection] 治疗成功，玩家{playerId}当前生命值: {healthManager.CurrentHealth}/{healthManager.MaxHealth}");
            }
            else
            {
                Debug.Log($"[MagicHitDetection] 治疗失败或无需治疗，玩家{playerId}当前生命值: {healthManager.CurrentHealth}/{healthManager.MaxHealth}");
            }
        }
    }
    
    /// <summary>
    /// 执行冰风暴魔法逻辑 - 持续2.5秒，每0.5秒造成伤害
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    /// <param name="playerId">施法者玩家ID</param>
    private void ExecuteIceStormMagic(MagicData magicData, int playerId)
    {
        if (magicData == null)
        {
            Debug.LogError("[MagicHitDetection] 冰风暴魔法数据为空");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 执行冰风暴魔法，玩家{playerId}，持续2.5秒，每0.5秒造成{magicData.damage}点伤害");
        }
        
        // 启动冰风暴协程
        StartCoroutine(IceStormCoroutine(magicData, playerId));
    }
    
    /// <summary>
    /// 冰风暴协程 - 持续伤害逻辑
    /// </summary>
    private System.Collections.IEnumerator IceStormCoroutine(MagicData magicData, int playerId)
    {
        const float totalDuration = 2.5f; // 总持续时间
        const float damageInterval = 0.5f; // 伤害间隔
        float elapsedTime = 0f;
        
        // 获取施法者位置和方向
        Vector3 casterPosition = GetCasterPosition(playerId);
        Vector3 casterDirection = GetCasterDirection(playerId);
        
        while (elapsedTime < totalDuration)
        {
            // 对范围内的怪物造成伤害
            ExecuteMagicHitDetection(magicData, casterPosition, casterDirection);
            
            if (enableDebugLog)
            {
                Debug.Log($"[MagicHitDetection] 冰风暴造成伤害，已持续{elapsedTime:F1}秒");
            }
            
            // 等待下次伤害
            yield return new WaitForSeconds(damageInterval);
            elapsedTime += damageInterval;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 冰风暴结束，总持续时间{elapsedTime:F1}秒");
        }
    }
    
    /// <summary>
    /// 执行天降甘霖魔法逻辑 - 治疗并复活所有玩家
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    /// <param name="playerId">施法者玩家ID</param>
    private void ExecuteHeavenlyRainMagic(MagicData magicData, int playerId)
    {
        if (magicData == null)
        {
            Debug.LogError("[MagicHitDetection] 天降甘霖魔法数据为空");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 执行天降甘霖魔法，玩家{playerId}，治疗并复活所有玩家");
        }
        
        // 获取所有玩家数据
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("[MagicHitDetection] PlayerManager实例为空，无法执行天降甘霖");
            return;
        }
        
        int healAmount = Mathf.RoundToInt(magicData.damage);
        int healedCount = 0;
        int revivedCount = 0;
        
        // 遍历所有玩家（包括死亡的玩家）
        var allPlayers = PlayerManager.Instance.GetAllPlayers();
        foreach (var playerData in allPlayers)
        {
            if (playerData?.playerObject == null) continue;
            
            PlayerHealthManager healthManager = playerData.playerObject.GetComponent<PlayerHealthManager>();
            if (healthManager == null) continue;
            
            // 检查玩家是否死亡
            bool wasDead = healthManager.IsDead();
            
            if (wasDead)
            {
                // 复活玩家 - 设置为满血
                healthManager.SetCurrentHealth(healthManager.MaxHealth);
                revivedCount++;
                
                if (enableDebugLog)
                {
                    Debug.Log($"[MagicHitDetection] 复活玩家{playerData.playerId}，生命值恢复至{healthManager.MaxHealth}");
                }
            }
            else
            {
                // 治疗活着的玩家
                bool healSuccess = healthManager.Heal(healAmount);
                if (healSuccess)
                {
                    healedCount++;
                }
                
                if (enableDebugLog)
                {
                    Debug.Log($"[MagicHitDetection] 治疗玩家{playerData.playerId}，恢复{healAmount}点生命值，当前生命值: {healthManager.CurrentHealth}/{healthManager.MaxHealth}");
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicHitDetection] 天降甘霖完成，治疗{healedCount}名玩家，复活{revivedCount}名玩家");
        }
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