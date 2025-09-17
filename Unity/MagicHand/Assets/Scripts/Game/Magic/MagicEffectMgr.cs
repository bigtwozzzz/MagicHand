using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 魔法特效配置项
/// </summary>
[Serializable]
public class MagicEffectItem
{
    [Header("魔法配置")]
    public int magicId;
    public GameObject effectPrefab;
    
    [Header("对象池设置")]
    public int poolSize = 5;
    public bool expandPool = true;
}

/// <summary>
/// 魔法特效管理器
/// 负责特效对象池管理，预生成特效，激活和回收特效
/// </summary>
public class MagicEffectMgr : MonoBehaviour
{
    [Header("特效配置")]
    [SerializeField] private List<MagicEffectItem> magicEffects = new List<MagicEffectItem>();
    
    [Header("默认设置")]
    [SerializeField] private int defaultPoolSize = 5;
    [SerializeField] private Transform effectParent;
    
    [Header("玩家配置")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool usePlayerAsReference = true;
    
    // 单例实例
    public static MagicEffectMgr Instance { get; private set; }
    
    // 对象池字典：魔法ID -> 特效对象队列
    private Dictionary<int, Queue<GameObject>> effectPools = new Dictionary<int, Queue<GameObject>>();
    
    // 特效预制体字典：魔法ID -> 预制体
    private Dictionary<int, GameObject> effectPrefabs = new Dictionary<int, GameObject>();
    
    // 活跃特效列表
    private List<GameObject> activeEffects = new List<GameObject>();
    
    // 特效配置字典
    private Dictionary<int, MagicEffectItem> effectConfigs = new Dictionary<int, MagicEffectItem>();
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeEffectManager();
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
        MagicEventSystem.OnMagicEffectRecycle += RecycleEffect;
    }
    
    void OnDestroy()
    {
        // 取消订阅
        MagicEventSystem.OnMagicTriggered -= OnMagicTriggered;
        MagicEventSystem.OnMagicEffectRecycle -= RecycleEffect;
    }
    
    /// <summary>
    /// 设置玩家Transform引用
    /// </summary>
    /// <param name="player">玩家Transform</param>
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
        Debug.Log($"[MagicEffectMgr] 已设置玩家Transform: {(player != null ? player.name : "null")}");
    }
    
    /// <summary>
    /// 设置是否以玩家为基准
    /// </summary>
    /// <param name="usePlayer">是否使用玩家为基准</param>
    public void SetUsePlayerAsReference(bool usePlayer)
    {
        usePlayerAsReference = usePlayer;
        Debug.Log($"[MagicEffectMgr] 设置以玩家为基准: {usePlayer}");
    }
    
    /// <summary>
    /// 初始化特效管理器
    /// </summary>
    private void InitializeEffectManager()
    {
        // 创建特效父对象
        if (effectParent == null)
        {
            GameObject parentObj = new GameObject("MagicEffects");
            parentObj.transform.SetParent(transform);
            effectParent = parentObj.transform;
        }
        
        // 初始化特效配置
        foreach (var effectItem in magicEffects)
        {
            if (effectItem.effectPrefab != null)
            {
                effectConfigs[effectItem.magicId] = effectItem;
                effectPrefabs[effectItem.magicId] = effectItem.effectPrefab;
                
                // 预生成特效对象池
                CreateEffectPool(effectItem.magicId, effectItem.poolSize);
                
                Debug.Log($"[MagicEffectMgr] 为魔法 {effectItem.magicId} 创建了 {effectItem.poolSize} 个特效对象");
            }
        }
        
        Debug.Log($"[MagicEffectMgr] 特效管理器初始化完成，共配置 {effectConfigs.Count} 个魔法特效");
    }
    
    /// <summary>
    /// 创建特效对象池
    /// </summary>
    private void CreateEffectPool(int magicId, int poolSize)
    {
        if (!effectPrefabs.ContainsKey(magicId))
        {
            Debug.LogWarning($"[MagicEffectMgr] 魔法 {magicId} 没有配置特效预制体");
            return;
        }
        
        Queue<GameObject> pool = new Queue<GameObject>();
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject effectObj = Instantiate(effectPrefabs[magicId], effectParent);
            effectObj.SetActive(false);
            pool.Enqueue(effectObj);
        }
        
        effectPools[magicId] = pool;
    }
    
    /// <summary>
    /// 魔法触发时的处理
    /// </summary>
    private void OnMagicTriggered(int magicId, MagicData magicData, int playerId)
    {
        // 计算特效位置和旋转
        Vector3 position;
        
        // 根据玩家ID获取对应玩家的位置
        PlayerManager.PlayerData playerData = PlayerManager.Instance?.GetPlayerData(playerId);
        if (playerData != null && playerData.playerObject != null)
        {
            // 以指定玩家为基准计算位置
            position = playerData.playerObject.transform.position;
            Debug.Log($"[MagicEffectMgr] 使用玩家{playerId}位置作为特效基准: {position}");
        }
        else if (usePlayerAsReference && playerTransform != null)
        {
            // 回退到默认玩家位置
            position = playerTransform.position;
            Debug.LogWarning($"[MagicEffectMgr] 未找到玩家{playerId}，使用默认玩家位置");
        }
        else
        {
            // 最后回退到摄像机位置
            position = Camera.main.transform.position;
            Debug.LogWarning($"[MagicEffectMgr] 未找到玩家{playerId}，使用摄像机位置");
        }
        
        Quaternion rotation = Quaternion.identity;
        
        // 应用特效配置的偏移
        if (magicData.effectConfig != null)
        {
            position += magicData.effectConfig.positionOffset;
            rotation = Quaternion.Euler(magicData.effectConfig.rotationOffset);
        }
        
        // 播放特效
        PlayEffect(magicId, position, rotation, magicData.effectConfig?.duration ?? 3f);
    }
    
    /// <summary>
    /// 播放魔法特效
    /// </summary>
    public void PlayEffect(int magicId, Vector3 position, Quaternion rotation, float duration = 3f)
    {
        GameObject effectObj = GetEffectFromPool(magicId);
        
        if (effectObj == null)
        {
            Debug.LogWarning($"[MagicEffectMgr] 无法获取魔法 {magicId} 的特效对象");
            return;
        }
        
        // 设置特效位置和旋转
        effectObj.transform.position = position;
        effectObj.transform.rotation = rotation;
        
        // 应用魔法数据中的特效配置
        MagicData magicData = MagicConfigLoader.Instance?.GetMagicData(magicId);
        if (magicData?.effectConfig != null)
        {
            effectObj.transform.localScale = magicData.effectConfig.scale;
        }
        
        // 激活特效
        effectObj.SetActive(true);
        activeEffects.Add(effectObj);
        
        // 播放音效（如果有AudioSource组件）
        AudioSource audioSource = effectObj.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
        
        // 自动回收特效
        StartCoroutine(AutoRecycleEffect(effectObj, duration));
        
        Debug.Log($"[MagicEffectMgr] 播放魔法特效: {magicId} at {position}");
    }
    
    /// <summary>
    /// 从对象池获取特效对象
    /// </summary>
    private GameObject GetEffectFromPool(int magicId)
    {
        if (!effectPools.ContainsKey(magicId))
        {
            Debug.LogWarning($"[MagicEffectMgr] 魔法 {magicId} 没有对应的特效池");
            return null;
        }
        
        Queue<GameObject> pool = effectPools[magicId];
        
        // 如果池中有可用对象，直接返回
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        
        // 如果池为空且允许扩展，创建新对象
        if (effectConfigs.ContainsKey(magicId) && effectConfigs[magicId].expandPool)
        {
            GameObject newEffect = Instantiate(effectPrefabs[magicId], effectParent);
            newEffect.SetActive(false);
            Debug.Log($"[MagicEffectMgr] 扩展魔法 {magicId} 的特效池");
            return newEffect;
        }
        
        return null;
    }
    
    /// <summary>
    /// 自动回收特效
    /// </summary>
    private IEnumerator AutoRecycleEffect(GameObject effectObj, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if (effectObj != null && effectObj.activeInHierarchy)
        {
            RecycleEffect(effectObj);
        }
    }
    
    /// <summary>
    /// 回收特效对象
    /// </summary>
    public void RecycleEffect(GameObject effectObj)
    {
        if (effectObj == null) return;
        
        // 停止音效
        AudioSource audioSource = effectObj.GetComponent<AudioSource>();
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // 停用特效
        effectObj.SetActive(false);
        
        // 从活跃列表移除
        activeEffects.Remove(effectObj);
        
        // 找到对应的魔法ID并放回池中
        foreach (var kvp in effectPrefabs)
        {
            if (effectObj.name.Contains(kvp.Value.name))
            {
                if (effectPools.ContainsKey(kvp.Key))
                {
                    effectPools[kvp.Key].Enqueue(effectObj);
                    Debug.Log($"[MagicEffectMgr] 回收魔法 {kvp.Key} 的特效对象");
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// 回收所有活跃特效
    /// </summary>
    public void RecycleAllEffects()
    {
        var effectsToRecycle = new List<GameObject>(activeEffects);
        foreach (var effect in effectsToRecycle)
        {
            RecycleEffect(effect);
        }
        
        Debug.Log($"[MagicEffectMgr] 回收了 {effectsToRecycle.Count} 个活跃特效");
    }
    
    /// <summary>
    /// 获取活跃特效数量
    /// </summary>
    public int GetActiveEffectCount()
    {
        return activeEffects.Count;
    }
}