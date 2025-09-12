using System.Collections.Generic;
using UnityEngine;
using System.Collections;

/// <summary>
/// 魔法特效配置数据
/// </summary>
[System.Serializable]
public class MagicEffectConfig
{
    [Header("特效配置")]
    [Tooltip("魔法编号")]
    public int magicId;
    
    [Tooltip("特效预制体")]
    public GameObject effectPrefab;
    
    [Header("变换参数")]
    [Tooltip("位置偏移")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("旋转偏移（欧拉角）")]
    public Vector3 rotationOffset = Vector3.zero;
    
    [Tooltip("缩放倍数")]
    public Vector3 scaleMultiplier = Vector3.one;
    
    [Header("对象池配置")]
    [Tooltip("预生成数量")]
    public int preSpawnCount = 3;
    
    [Tooltip("最大对象数量")]
    public int maxPoolSize = 10;
    
    [Tooltip("自动回收时间（秒）")]
    public float autoRecycleTime = 3f;
    
    [Header("其他设置")]
    [Tooltip("是否启用此特效")]
    public bool isEnabled = true;
    
    [Tooltip("是否自动回收到对象池")]
    public bool autoRecycle = true;
    
    /// <summary>
    /// 重置为默认值（在Inspector中右键选择Reset时调用）
    /// </summary>
    public void Reset()
    {
        magicId = 0;
        effectPrefab = null;
        positionOffset = Vector3.zero;
        rotationOffset = Vector3.zero;
        scaleMultiplier = Vector3.one;
        preSpawnCount = 1;
        maxPoolSize = 1;
        autoRecycleTime = 3f;
        isEnabled = true;
        autoRecycle = true;
    }
}

/// <summary>
/// 魔法对象池管理器
/// 管理所有魔法特效的生成、激活和回收
/// </summary>
public class MagicEffectPool : MonoBehaviour
{
    [Header("对象池配置")]
    [Tooltip("魔法特效配置列表")]
    public List<MagicEffectConfig> effectConfigs = new List<MagicEffectConfig>();
    
    [Tooltip("每种特效的预生成数量")]
    public int preInstantiateCount = 3;
    
    [Tooltip("对象池父节点")]
    public Transform poolParent;
    
    [Header("调试配置")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = true;
    
    [Tooltip("是否在Scene视图中显示特效范围")]
    public bool showEffectGizmos = false;
    
    // 对象池字典 <魔法编号, 特效对象列表>
    private Dictionary<int, Queue<GameObject>> effectPools = new Dictionary<int, Queue<GameObject>>();
    
    // 配置字典，用于快速查找 <魔法编号, 特效配置>
    private Dictionary<int, MagicEffectConfig> configDict = new Dictionary<int, MagicEffectConfig>();
    
    // 活跃特效列表 <特效对象, 魔法编号>
    private Dictionary<GameObject, int> activeEffects = new Dictionary<GameObject, int>();
    
    // 单例模式
    private static MagicEffectPool _instance;
    public static MagicEffectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MagicEffectPool>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MagicEffectPool");
                    _instance = go.AddComponent<MagicEffectPool>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    // 事件
    public System.Action<int, GameObject> OnEffectSpawned;   // 特效生成事件
    public System.Action<int, GameObject> OnEffectRecycled; // 特效回收事件
    
    void Awake()
    {
        // 确保只有一个实例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePool();
    }
    
    void Start()
    {
        SubscribeToMagicEvents();
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectPool] 魔法特效对象池已启动，共配置 {effectConfigs.Count} 种特效");
        }
    }
    
    /// <summary>
    /// 初始化对象池
    /// </summary>
    void InitializePool()
    {
        // 设置对象池父节点
        if (poolParent == null)
        {
            GameObject poolRoot = new GameObject("EffectPool");
            poolRoot.transform.SetParent(transform);
            poolParent = poolRoot.transform;
        }
        
        // 构建配置字典
        configDict.Clear();
        effectPools.Clear();
        
        foreach (var config in effectConfigs)
        {
            if (config != null && config.effectPrefab != null && config.isEnabled)
            {
                configDict[config.magicId] = config;
                effectPools[config.magicId] = new Queue<GameObject>();
                
                // 预生成特效对象
                PreInstantiateEffects(config);
            }
        }
        
        // 如果没有配置特效，创建默认配置
        if (effectConfigs.Count == 0)
        {
            CreateDefaultEffectConfigs();
        }
    }
    
    /// <summary>
    /// 预生成特效对象
    /// </summary>
    /// <param name="config">特效配置</param>
    void PreInstantiateEffects(MagicEffectConfig config)
    {
        var pool = effectPools[config.magicId];
        
        for (int i = 0; i < preInstantiateCount; i++)
        {
            GameObject effectObj = CreateEffectObject(config);
            effectObj.SetActive(false);
            pool.Enqueue(effectObj);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectPool] 为魔法 {config.magicId} 预生成了 {preInstantiateCount} 个特效对象");
        }
    }
    
    /// <summary>
    /// 创建特效对象
    /// </summary>
    /// <param name="config">特效配置</param>
    /// <returns>特效对象</returns>
    GameObject CreateEffectObject(MagicEffectConfig config)
    {
        GameObject effectObj = Instantiate(config.effectPrefab, poolParent);
        effectObj.name = $"MagicEffect_{config.magicId}";
        
        // 应用变换参数
        ApplyTransformConfig(effectObj, config);
        
        // 添加自动回收组件（如果需要）
        if (config.autoRecycle && config.autoRecycleTime > 0)
        {
            var autoRecycle = effectObj.GetComponent<MagicEffectAutoRecycle>();
            if (autoRecycle == null)
            {
                autoRecycle = effectObj.AddComponent<MagicEffectAutoRecycle>();
            }
            autoRecycle.Initialize(config.magicId, config.autoRecycleTime);
        }
        
        return effectObj;
    }
    
    /// <summary>
    /// 应用变换配置
    /// </summary>
    /// <param name="effectObj">特效对象</param>
    /// <param name="config">特效配置</param>
    void ApplyTransformConfig(GameObject effectObj, MagicEffectConfig config)
    {
        Transform effectTransform = effectObj.transform;
        
        // 应用位移偏移
        effectTransform.localPosition = config.positionOffset;
        
        // 应用旋转偏移
        effectTransform.localRotation = Quaternion.Euler(config.rotationOffset);
        
        // 应用缩放倍数
        effectTransform.localScale = config.scaleMultiplier;
    }
    
    /// <summary>
    /// 创建默认特效配置
    /// </summary>
    void CreateDefaultEffectConfigs()
    {
        // 这里可以创建一些默认的特效配置
        // 实际项目中应该由设计师配置具体的特效预制体
        
        if (enableDebugLog)
        {
            Debug.LogWarning("[MagicEffectPool] 未配置特效，请在Inspector中添加特效配置");
        }
    }
    
    /// <summary>
    /// 处理魔法施放事件
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <param name="magicData">魔法数据</param>
    void OnMagicCast(int magicId, MagicData magicData)
    {
        SpawnEffect(magicId, transform.position, transform.rotation, magicData.amplificationFactor);
    }
    
    /// <summary>
    /// 生成魔法特效
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <param name="position">生成位置</param>
    /// <param name="rotation">生成旋转</param>
    /// <returns>生成的特效对象</returns>
    public GameObject SpawnEffect(int magicId, Vector3 position, Quaternion rotation)
    {
        return SpawnEffect(magicId, position, rotation, 1.0f);
    }
    
    /// <summary>
    /// 生成魔法特效（带增幅系数）
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <param name="position">生成位置</param>
    /// <param name="rotation">生成旋转</param>
    /// <param name="amplificationFactor">增幅系数</param>
    /// <returns>生成的特效对象</returns>
    public GameObject SpawnEffect(int magicId, Vector3 position, Quaternion rotation, float amplificationFactor)
    {
        // 检查配置是否存在
        if (!configDict.ContainsKey(magicId))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[MagicEffectPool] 魔法编号 {magicId} 没有配置特效");
            }
            return null;
        }
        
        MagicEffectConfig config = configDict[magicId];
        
        // 检查特效是否启用
        if (!config.isEnabled)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[MagicEffectPool] 魔法编号 {magicId} 的特效已禁用");
            }
            return null;
        }
        
        GameObject effectObj = GetEffectFromPool(magicId);
        if (effectObj == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError($"[MagicEffectPool] 无法获取魔法编号 {magicId} 的特效对象");
            }
            return null;
        }
        
        // 设置世界位置和旋转
        effectObj.transform.position = position + config.positionOffset;
        effectObj.transform.rotation = rotation * Quaternion.Euler(config.rotationOffset);
        
        // 应用增幅系数到特效缩放
        if (amplificationFactor != 1.0f)
        {
            effectObj.transform.localScale = config.scaleMultiplier * amplificationFactor;
        }
        else
        {
            effectObj.transform.localScale = config.scaleMultiplier;
        }
        
        // 添加或更新自动回收组件
        var autoRecycle = effectObj.GetComponent<MagicEffectAutoRecycle>();
        if (autoRecycle == null)
        {
            autoRecycle = effectObj.AddComponent<MagicEffectAutoRecycle>();
        }
        
        // 初始化自动回收组件
         autoRecycle.Initialize(magicId, config.autoRecycleTime);
         autoRecycle.enableDebugLog = enableDebugLog;
        
        // 激活特效
        effectObj.SetActive(true);
        
        // 开始自动回收计时器
        autoRecycle.StartRecycleTimer();
        
        // 记录活跃特效
        activeEffects[effectObj] = magicId;
        
        // 触发事件
        OnEffectSpawned?.Invoke(magicId, effectObj);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectPool] 生成魔法 {magicId} 的特效，位置: {effectObj.transform.position}");
        }
        
        return effectObj;
    }
    
    /// <summary>
    /// 从对象池获取特效对象
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <returns>特效对象</returns>
    GameObject GetEffectFromPool(int magicId)
    {
        if (!effectPools.ContainsKey(magicId))
        {
            return null;
        }
        
        var pool = effectPools[magicId];
        
        // 如果池中有可用对象，直接使用
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        
        // 如果池中没有可用对象，动态创建新对象
        MagicEffectConfig config = configDict[magicId];
        GameObject newEffectObj = CreateEffectObject(config);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectPool] 动态创建魔法 {magicId} 的新特效对象");
        }
        
        return newEffectObj;
    }
    
    /// <summary>
    /// 回收特效到对象池
    /// </summary>
    /// <param name="effectObj">特效对象</param>
    public void RecycleEffect(GameObject effectObj)
    {
        if (effectObj == null || !activeEffects.ContainsKey(effectObj))
        {
            return;
        }
        
        int magicId = activeEffects[effectObj];
        activeEffects.Remove(effectObj);
        
        // 重置特效状态
        effectObj.SetActive(false);
        effectObj.transform.SetParent(poolParent);
        
        // 重新应用变换配置
        if (configDict.ContainsKey(magicId))
        {
            ApplyTransformConfig(effectObj, configDict[magicId]);
        }
        
        // 放回对象池
        if (effectPools.ContainsKey(magicId))
        {
            effectPools[magicId].Enqueue(effectObj);
        }
        
        // 触发事件
        OnEffectRecycled?.Invoke(magicId, effectObj);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectPool] 回收魔法 {magicId} 的特效对象");
        }
    }
    
    /// <summary>
    /// 回收所有活跃特效
    /// </summary>
    public void RecycleAllActiveEffects()
    {
        var activeEffectsList = new List<GameObject>(activeEffects.Keys);
        
        foreach (var effectObj in activeEffectsList)
        {
            RecycleEffect(effectObj);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectPool] 回收了 {activeEffectsList.Count} 个活跃特效");
        }
    }
    
    /// <summary>
    /// 获取指定魔法的活跃特效数量
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <returns>活跃特效数量</returns>
    public int GetActiveEffectCount(int magicId)
    {
        int count = 0;
        foreach (var kvp in activeEffects)
        {
            if (kvp.Value == magicId)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// 获取对象池中指定魔法的可用特效数量
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <returns>可用特效数量</returns>
    public int GetPooledEffectCount(int magicId)
    {
        if (effectPools.ContainsKey(magicId))
        {
            return effectPools[magicId].Count;
        }
        return 0;
    }
    
    /// <summary>
    /// 清空指定魔法的对象池
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    public void ClearPool(int magicId)
    {
        if (!effectPools.ContainsKey(magicId))
        {
            return;
        }
        
        var pool = effectPools[magicId];
        while (pool.Count > 0)
        {
            GameObject effectObj = pool.Dequeue();
            if (effectObj != null)
            {
                Destroy(effectObj);
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectPool] 清空了魔法 {magicId} 的对象池");
        }
    }
    
    /// <summary>
    /// 清空所有对象池
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var magicId in effectPools.Keys)
        {
            ClearPool(magicId);
        }
        
        RecycleAllActiveEffects();
        
        if (enableDebugLog)
        {
            Debug.Log("[MagicEffectPool] 清空了所有对象池");
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromMagicEvents();
        
        ClearAllPools();
    }
    
    void OnDisable()
    {
        UnsubscribeFromMagicEvents();
    }
    
    /// <summary>
    /// 订阅魔法事件
    /// </summary>
    void SubscribeToMagicEvents()
    {
        if (MagicManager.Instance != null)
        {
            MagicManager.Instance.OnMagicCast += OnMagicCast;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicEffectPool] 已订阅魔法触发事件");
            }
        }
    }
    
    /// <summary>
    /// 取消订阅魔法事件
    /// </summary>
    void UnsubscribeFromMagicEvents()
    {
        if (MagicManager.Instance != null)
        {
            MagicManager.Instance.OnMagicCast -= OnMagicCast;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicEffectPool] 已取消订阅魔法触发事件");
            }
        }
    }
    
    // 在Scene视图中绘制特效范围
    void OnDrawGizmos()
    {
        if (!showEffectGizmos) return;
        
        foreach (var kvp in activeEffects)
        {
            GameObject effectObj = kvp.Key;
            int magicId = kvp.Value;
            
            if (effectObj != null && configDict.ContainsKey(magicId))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(effectObj.transform.position, 0.5f);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(effectObj.transform.position, 
                               effectObj.transform.position + effectObj.transform.forward * 2f);
            }
        }
    }
    
    // 测试方法
    [ContextMenu("测试生成魔法1特效")]
    void TestSpawnMagic1Effect()
    {
        SpawnEffect(1, transform.position, transform.rotation);
    }
    
    [ContextMenu("回收所有特效")]
    void TestRecycleAllEffects()
    {
        RecycleAllActiveEffects();
    }
    
    [ContextMenu("显示对象池状态")]
    void TestShowPoolStatus()
    {
        foreach (var kvp in effectPools)
        {
            int magicId = kvp.Key;
            int pooledCount = kvp.Value.Count;
            int activeCount = GetActiveEffectCount(magicId);
            
            Debug.Log($"[MagicEffectPool] 魔法 {magicId}: 池中 {pooledCount} 个，活跃 {activeCount} 个");
        }
    }
}