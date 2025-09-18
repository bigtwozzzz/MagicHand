using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 魔法冷却数据
/// </summary>
[System.Serializable]
public class CooldownData
{
    public int magicId;
    public float totalCooldownTime;
    public float remainingTime;
    public bool isOnCooldown;
    public Coroutine cooldownCoroutine;
    
    public CooldownData(int id, float totalTime)
    {
        magicId = id;
        totalCooldownTime = totalTime;
        remainingTime = totalTime;
        isOnCooldown = true;
        cooldownCoroutine = null;
    }
}

/// <summary>
/// 魔法冷却管理器
/// 负责处理魔法冷却逻辑，在冷却完成时触发事件
/// </summary>
public class MagicCooldown : MonoBehaviour
{
    [Header("冷却设置")]
    [SerializeField] private bool pauseCooldownWhenGamePaused = true;
    
    // 单例实例
    public static MagicCooldown Instance { get; private set; }
    
    // 冷却数据字典
    private Dictionary<int, CooldownData> cooldownDict = new Dictionary<int, CooldownData>();
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 订阅魔法事件
        MagicEventSystem.OnMagicTriggered += OnMagicTriggered;
    }
    
    void OnDestroy()
    {
        // 取消订阅
        MagicEventSystem.OnMagicTriggered -= OnMagicTriggered;
        
        // 停止所有冷却协程
        StopAllCooldowns();
    }
    
    /// <summary>
    /// 魔法触发时的处理
    /// </summary>
    private void OnMagicTriggered(int magicId, MagicData magicData, int playerId)
    {
        // 只对本地玩家的魔法进行冷却计算
        int localPlayerId = MagicManager.Instance?.GetLocalPlayerId() ?? 1;
        if (playerId == localPlayerId)
        {
            // 魔法触发后自动开始冷却
            StartCooldown(magicId, magicData.cooldownTime);
        }
        else
        {
            // 远程玩家的魔法不进行冷却计算
            Debug.Log($"[MagicCooldown] 跳过远程玩家{playerId}的魔法{magicId}冷却计算");
        }
    }
    
    /// <summary>
    /// 开始魔法冷却
    /// </summary>
    public void StartCooldown(int magicId, float cooldownTime)
    {
        // 如果已经在冷却中，先停止之前的冷却
        if (cooldownDict.ContainsKey(magicId))
        {
            StopCooldown(magicId);
        }
        
        // 创建新的冷却数据
        CooldownData cooldownData = new CooldownData(magicId, cooldownTime);
        cooldownDict[magicId] = cooldownData;
        
        // 开始冷却协程
        cooldownData.cooldownCoroutine = StartCoroutine(CooldownCoroutine(cooldownData));
        
        // 触发冷却开始事件
        MagicEventSystem.StartMagicCooldown(magicId, cooldownTime);
        
        Debug.Log($"[MagicCooldown] 魔法 {magicId} 开始冷却，时长: {cooldownTime}s");
    }
    
    /// <summary>
    /// 冷却协程
    /// </summary>
    private IEnumerator CooldownCoroutine(CooldownData cooldownData)
    {
        while (cooldownData.remainingTime > 0)
        {
            // 检查游戏是否暂停
            if (pauseCooldownWhenGamePaused && GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
            {
                yield return null; // 暂停时不减少冷却时间
                continue;
            }
            
            // 减少冷却时间
            cooldownData.remainingTime -= Time.deltaTime;
            
            yield return null;
        }
        
        // 冷却完成
        OnCooldownComplete(cooldownData.magicId);
    }
    
    /// <summary>
    /// 冷却完成处理
    /// </summary>
    private void OnCooldownComplete(int magicId)
    {
        if (cooldownDict.ContainsKey(magicId))
        {
            cooldownDict[magicId].isOnCooldown = false;
            cooldownDict[magicId].remainingTime = 0f;
            cooldownDict[magicId].cooldownCoroutine = null;
            
            // 触发冷却结束事件
            MagicEventSystem.EndMagicCooldown(magicId);
            
            Debug.Log($"[MagicCooldown] 魔法 {magicId} 冷却完成");
            
            // 移除冷却数据
            cooldownDict.Remove(magicId);
        }
    }
    
    /// <summary>
    /// 停止指定魔法的冷却
    /// </summary>
    public void StopCooldown(int magicId)
    {
        if (cooldownDict.ContainsKey(magicId))
        {
            CooldownData cooldownData = cooldownDict[magicId];
            
            if (cooldownData.cooldownCoroutine != null)
            {
                StopCoroutine(cooldownData.cooldownCoroutine);
                cooldownData.cooldownCoroutine = null;
            }
            
            cooldownDict.Remove(magicId);
            
            Debug.Log($"[MagicCooldown] 停止魔法 {magicId} 的冷却");
        }
    }
    
    /// <summary>
    /// 停止所有冷却
    /// </summary>
    public void StopAllCooldowns()
    {
        foreach (var kvp in cooldownDict)
        {
            if (kvp.Value.cooldownCoroutine != null)
            {
                StopCoroutine(kvp.Value.cooldownCoroutine);
            }
        }
        
        cooldownDict.Clear();
        Debug.Log("[MagicCooldown] 停止所有魔法冷却");
    }
    
    /// <summary>
    /// 检查魔法是否在冷却中
    /// </summary>
    public bool IsOnCooldown(int magicId)
    {
        return cooldownDict.ContainsKey(magicId) && cooldownDict[magicId].isOnCooldown;
    }
    
    /// <summary>
    /// 获取魔法剩余冷却时间
    /// </summary>
    public float GetRemainingCooldown(int magicId)
    {
        if (cooldownDict.ContainsKey(magicId))
        {
            return Mathf.Max(0f, cooldownDict[magicId].remainingTime);
        }
        return 0f;
    }
    
    /// <summary>
    /// 获取魔法冷却进度（0-1）
    /// </summary>
    public float GetCooldownProgress(int magicId)
    {
        if (cooldownDict.ContainsKey(magicId))
        {
            CooldownData data = cooldownDict[magicId];
            if (data.totalCooldownTime > 0)
            {
                return 1f - (data.remainingTime / data.totalCooldownTime);
            }
        }
        return 1f; // 没有冷却或冷却完成
    }
    
    /// <summary>
    /// 重置指定魔法的冷却
    /// </summary>
    public void ResetCooldown(int magicId)
    {
        if (cooldownDict.ContainsKey(magicId))
        {
            StopCooldown(magicId);
            MagicEventSystem.EndMagicCooldown(magicId);
            Debug.Log($"[MagicCooldown] 重置魔法 {magicId} 的冷却");
        }
    }
    
    /// <summary>
    /// 重置所有魔法的冷却
    /// </summary>
    public void ResetAllCooldowns()
    {
        List<int> magicIds = new List<int>(cooldownDict.Keys);
        
        foreach (int magicId in magicIds)
        {
            ResetCooldown(magicId);
        }
        
        Debug.Log("[MagicCooldown] 重置所有魔法冷却");
    }
    
    /// <summary>
    /// 获取所有冷却中的魔法ID
    /// </summary>
    public List<int> GetCooldownMagicIds()
    {
        return new List<int>(cooldownDict.Keys);
    }
    
    /// <summary>
    /// 获取冷却中的魔法数量
    /// </summary>
    public int GetCooldownCount()
    {
        return cooldownDict.Count;
    }
    
    /// <summary>
    /// 立即完成指定魔法的冷却
    /// </summary>
    public void CompleteCooldown(int magicId)
    {
        if (cooldownDict.ContainsKey(magicId))
        {
            cooldownDict[magicId].remainingTime = 0f;
            Debug.Log($"[MagicCooldown] 立即完成魔法 {magicId} 的冷却");
        }
    }
    
    /// <summary>
    /// 修改魔法的剩余冷却时间
    /// </summary>
    public void ModifyRemainingCooldown(int magicId, float timeModification)
    {
        if (cooldownDict.ContainsKey(magicId))
        {
            cooldownDict[magicId].remainingTime = Mathf.Max(0f, cooldownDict[magicId].remainingTime + timeModification);
            Debug.Log($"[MagicCooldown] 修改魔法 {magicId} 的剩余冷却时间: {timeModification}s");
        }
    }
}