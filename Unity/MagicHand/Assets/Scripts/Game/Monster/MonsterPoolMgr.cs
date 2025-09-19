using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怪物对象池管理器
/// 负责管理怪物模型的预制体配置、对象池和生命周期
/// </summary>
public class MonsterPoolMgr : MonoBehaviour
{
    private static MonsterPoolMgr _instance;
    public static MonsterPoolMgr Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MonsterPoolMgr>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MonsterPoolMgr");
                    _instance = go.AddComponent<MonsterPoolMgr>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    [Header("预制体配置")]
    [SerializeField] private MonsterPrefabConfig[] prefabConfigs;
    
    [Header("对象池配置")]
    [SerializeField] private int defaultPoolSize = 10;
    [SerializeField] private Transform poolParent;
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 对象池字典
    private Dictionary<int, Queue<GameObject>> monsterPools = new Dictionary<int, Queue<GameObject>>();
    // 活跃怪物字典
    private Dictionary<string, GameObject> activeMonsters = new Dictionary<string, GameObject>();
    // 预制体字典
    private Dictionary<int, MonsterPrefabConfig> prefabDict = new Dictionary<int, MonsterPrefabConfig>();
    
    // 怪物编号计数器
    private int monsterCounter = 0;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
            
            // 订阅怪物死亡事件
            MonsterEventManager.OnMonsterDeathDetected += OnMonsterDeath;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// 初始化对象池
    /// </summary>
    public void InitializePools()
    {
        // 创建池父对象
        if (poolParent == null)
        {
            GameObject poolParentGO = new GameObject("MonsterPool");
            poolParentGO.transform.SetParent(transform);
            poolParent = poolParentGO.transform;
        }
        
        // 初始化预制体字典
        if (prefabConfigs != null)
        {
            foreach (var config in prefabConfigs)
            {
                if (config != null && config.prefab != null)
                {
                    prefabDict[config.monsterId] = config;
                    
                    // 预生成对象池
                    PreGeneratePool(config.monsterId, config.poolSize > 0 ? config.poolSize : defaultPoolSize);
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"[MonsterPoolMgr] 初始化怪物池: ID={config.monsterId}, 预制体={config.prefab.name}, 池大小={config.poolSize}");
                    }
                }
            }
        }
        
        Debug.Log($"[MonsterPoolMgr] 对象池初始化完成，共 {prefabDict.Count} 种怪物类型");
    }
    
    /// <summary>
    /// 预生成指定类型的对象池
    /// </summary>
    /// <param name="monsterId">怪物ID</param>
    /// <param name="poolSize">池大小</param>
    private void PreGeneratePool(int monsterId, int poolSize)
    {
        if (!prefabDict.TryGetValue(monsterId, out MonsterPrefabConfig config))
        {
            Debug.LogError($"[MonsterPoolMgr] 未找到ID为 {monsterId} 的预制体配置");
            return;
        }
        
        if (!monsterPools.ContainsKey(monsterId))
        {
            monsterPools[monsterId] = new Queue<GameObject>();
        }
        
        Queue<GameObject> pool = monsterPools[monsterId];
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject monster = CreateMonsterInstance(config);
            monster.SetActive(false);
            pool.Enqueue(monster);
        }
    }
    
    /// <summary>
    /// 创建怪物实例
    /// </summary>
    /// <param name="config">预制体配置</param>
    /// <returns>怪物实例</returns>
    private GameObject CreateMonsterInstance(MonsterPrefabConfig config)
    {
        GameObject monster = Instantiate(config.prefab, poolParent);
        
        // 应用配置
        monster.transform.localRotation = config.rotation;
        monster.transform.localScale = config.scale;
        
        // 确保有MonsterRuntimeData组件
        if (monster.GetComponent<MonsterRuntimeData>() == null)
        {
            monster.AddComponent<MonsterRuntimeData>();
        }
        
        // 设置Animator组件和动画控制器
        SetupAnimatorController(monster, config.monsterId);
        
        return monster;
    }
    
    /// <summary>
    /// 设置怪物的Animator组件和动画控制器
    /// </summary>
    /// <param name="monster">怪物对象</param>
    /// <param name="monsterId">怪物ID</param>
    private void SetupAnimatorController(GameObject monster, int monsterId)
    {
        // 获取或创建Animator组件
        Animator animator = monster.GetComponent<Animator>();
        if (animator == null)
        {
            animator = monster.AddComponent<Animator>();
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterPoolMgr] 为怪物 {monsterId} 添加Animator组件");
            }
        }
        
        // 加载对应编号的动画控制器
        AnimatorOverrideController overrideController = LoadAnimatorController(monsterId);
        if (overrideController != null)
        {
            animator.runtimeAnimatorController = overrideController;
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterPoolMgr] 为怪物 {monsterId} 设置动画控制器: Monster{monsterId}.overrideController");
            }
        }
        else
        {
            Debug.LogWarning($"[MonsterPoolMgr] 未找到怪物 {monsterId} 的动画控制器");
        }
    }
    
    /// <summary>
    /// 加载动画控制器
    /// </summary>
    /// <param name="monsterId">怪物ID</param>
    /// <returns>动画控制器</returns>
    private AnimatorOverrideController LoadAnimatorController(int monsterId)
    {
        string controllerName = $"Monster{monsterId}";
        AnimatorOverrideController controller = null;
        
        // 优先尝试从Resources文件夹加载（适用于打包后）
        try
        {
            controller = Resources.Load<AnimatorOverrideController>($"Animation/{controllerName}");
            if (controller != null)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[MonsterPoolMgr] 从Resources加载动画控制器: {controllerName}");
                }
                return controller;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MonsterPoolMgr] 从Resources加载动画控制器失败: {e.Message}");
        }
        
#if UNITY_EDITOR
        // 编辑器模式下，尝试从Assets路径加载
        try
        {
            string assetPath = $"Assets/Resources/Animation/{controllerName}.overrideController";
            controller = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(assetPath);
            if (controller != null)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[MonsterPoolMgr] 从编辑器路径加载动画控制器: {assetPath}");
                }
                return controller;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MonsterPoolMgr] 从编辑器路径加载动画控制器失败: {e.Message}");
        }
#endif
        
        return null;
    }
    
    /// <summary>
    /// 设置怪物动画管理器
    /// </summary>
    /// <param name="monster">怪物对象</param>
    private void SetupMonsterAnimeMgr(GameObject monster)
    {
        if (monster == null) return;
        
        // 检查是否已有MonsterAnimeMgr组件
        MonsterAnimeMgr animeMgr = monster.GetComponent<MonsterAnimeMgr>();
        if (animeMgr == null)
        {
            // 添加MonsterAnimeMgr组件
            animeMgr = monster.AddComponent<MonsterAnimeMgr>();
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterPoolMgr] 为怪物添加MonsterAnimeMgr组件");
            }
        }
        
        // 设置animator引用
        Animator animator = monster.GetComponent<Animator>();
        if (animator != null && animeMgr != null)
        {
            // 使用反射设置私有字段animator
            var animatorField = typeof(MonsterAnimeMgr).GetField("animator", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (animatorField != null)
            {
                animatorField.SetValue(animeMgr, animator);
                
                if (enableDebugLog)
                {
                    Debug.Log($"[MonsterPoolMgr] 设置MonsterAnimeMgr的animator引用");
                }
            }
        }
    }
    
    /// <summary>
    /// 激活一个指定类型的怪物模型
    /// </summary>
    /// <param name="monsterId">怪物类型ID</param>
    /// <param name="position">生成位置</param>
    /// <returns>怪物的独立编号，失败返回null</returns>
    public string ActivateMonster(int monsterId, Vector3 position)
    {
        if (!prefabDict.TryGetValue(monsterId, out MonsterPrefabConfig config))
        {
            Debug.LogError($"[MonsterPoolMgr] 未找到ID为 {monsterId} 的预制体配置");
            return null;
        }
        
        // 获取怪物配置
        MonsterConfig monsterConfig = MonsterConfigLoader.Instance?.GetMonsterConfig(monsterId);
        if (monsterConfig == null)
        {
            Debug.LogError($"[MonsterPoolMgr] 未找到ID为 {monsterId} 的怪物配置");
            return null;
        }
        
        GameObject monster = GetMonsterFromPool(monsterId);
        if (monster == null)
        {
            Debug.LogError($"[MonsterPoolMgr] 无法从对象池获取怪物: {monsterId}");
            return null;
        }
        
        // 生成独立编号
        string uniqueNumber = GenerateUniqueNumber();
        
        // 初始化运行时数据
        MonsterRuntimeData runtimeData = monster.GetComponent<MonsterRuntimeData>();
        if (runtimeData != null)
        {
            runtimeData.InitializeRuntimeData(monsterConfig, position, uniqueNumber);
        }
        
        // 应用缩放倍数
        float scaleMultiplier = monsterConfig.scaleMultiplier;
        Vector3 originalScale = config.scale;
        monster.transform.localScale = originalScale * scaleMultiplier;
        
        // 初始化血条组件
        MonsterHealthBar healthBar = monster.GetComponent<MonsterHealthBar>();
        if (healthBar == null)
        {
            healthBar = monster.AddComponent<MonsterHealthBar>();
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterPoolMgr] 为怪物添加血条组件: {uniqueNumber}");
            }
        }
        
        // 设置血条的世界坐标偏移
        healthBar.worldOffset = monsterConfig.worldOffset;
        
        // 血条组件现在会自动处理缩放调整，无需额外处理
        
        // 设置动画管理器
        SetupMonsterAnimeMgr(monster);
        
        // 重置运行时数据状态（确保从初始状态开始）
        MonsterRuntimeData resetRuntimeData = monster.GetComponent<MonsterRuntimeData>();
        if (resetRuntimeData != null)
        {
            resetRuntimeData.ResetRuntimeData();
        }
        
        // 重置动画状态（确保从初始状态开始）
        MonsterAnimeMgr animeMgr = monster.GetComponent<MonsterAnimeMgr>();
        if (animeMgr != null)
        {
            animeMgr.ResetAnimationState();
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterPoolMgr] 重置怪物 {uniqueNumber} 的状态和动画");
            }
        }
        
        // 激活怪物
        monster.SetActive(true);
        monster.transform.position = position;
        
        // 添加到活跃列表
        activeMonsters[uniqueNumber] = monster;
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterPoolMgr] 激活怪物: 编号={uniqueNumber}, ID={monsterId}, 位置={position}");
        }
        
        // 触发生成事件
        if (MonsterEventManager.Instance != null)
        {
            MonsterEventManager.Instance.TriggerMonsterSpawnEvent(monsterId);
        }
        
        return uniqueNumber;
    }
    
    /// <summary>
    /// 从对象池获取怪物
    /// </summary>
    /// <param name="monsterId">怪物ID</param>
    /// <returns>怪物对象</returns>
    private GameObject GetMonsterFromPool(int monsterId)
    {
        if (!monsterPools.TryGetValue(monsterId, out Queue<GameObject> pool))
        {
            Debug.LogError($"[MonsterPoolMgr] 未找到ID为 {monsterId} 的对象池");
            return null;
        }
        
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        
        // 池中没有可用对象，创建新的
        if (prefabDict.TryGetValue(monsterId, out MonsterPrefabConfig config))
        {
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterPoolMgr] 对象池不足，创建新怪物: {monsterId}");
            }
            return CreateMonsterInstance(config);
        }
        
        return null;
    }
    
    /// <summary>
    /// 隐藏指定编号的怪物模型
    /// </summary>
    /// <param name="uniqueNumber">怪物独立编号</param>
    public void HideMonster(string uniqueNumber)
    {
        if (string.IsNullOrEmpty(uniqueNumber))
        {
            Debug.LogWarning("[MonsterPoolMgr] 怪物编号为空");
            return;
        }
        
        if (activeMonsters.TryGetValue(uniqueNumber, out GameObject monster))
        {
            // 获取运行时数据
            MonsterRuntimeData runtimeData = monster.GetComponent<MonsterRuntimeData>();
            int monsterId = runtimeData != null ? runtimeData.configId : 0;
            
            // 隐藏怪物
            monster.SetActive(false);
            
            // 从活跃列表移除
            activeMonsters.Remove(uniqueNumber);
            
            // 返回对象池
            if (monsterId > 0 && monsterPools.TryGetValue(monsterId, out Queue<GameObject> pool))
            {
                pool.Enqueue(monster);
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterPoolMgr] 隐藏怪物: 编号={uniqueNumber}, ID={monsterId}");
            }
        }
        else
        {
            Debug.LogWarning($"[MonsterPoolMgr] 未找到编号为 {uniqueNumber} 的活跃怪物");
        }
    }
    
    /// <summary>
    /// 销毁所有怪物模型
    /// </summary>
    public void DestroyAllMonsters()
    {
        // 隐藏所有活跃怪物
        var activeNumbers = new List<string>(activeMonsters.Keys);
        foreach (string number in activeNumbers)
        {
            HideMonster(number);
        }
        
        // 销毁对象池中的所有对象
        foreach (var pool in monsterPools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject monster = pool.Dequeue();
                if (monster != null)
                {
                    Destroy(monster);
                }
            }
        }
        
        monsterPools.Clear();
        activeMonsters.Clear();
        
        Debug.Log("[MonsterPoolMgr] 所有怪物模型已销毁");
    }
    
    /// <summary>
    /// 生成独立编号
    /// </summary>
    /// <returns>独立编号</returns>
    private string GenerateUniqueNumber()
    {
        monsterCounter++;
        return $"10000{monsterCounter:D3}";
    }
    
    /// <summary>
    /// 获取活跃怪物数量
    /// </summary>
    /// <returns>活跃怪物数量</returns>
    public int GetActiveMonsterCount()
    {
        return activeMonsters.Count;
    }
    
    /// <summary>
    /// 获取指定编号的怪物运行时数据
    /// </summary>
    /// <param name="uniqueNumber">怪物编号</param>
    /// <returns>运行时数据</returns>
    public MonsterRuntimeData GetMonsterRuntimeData(string uniqueNumber)
    {
        if (activeMonsters.TryGetValue(uniqueNumber, out GameObject monster))
        {
            return monster.GetComponent<MonsterRuntimeData>();
        }
        return null;
    }
    
    /// <summary>
    /// 获取所有活跃怪物的副本
    /// </summary>
    /// <returns>活跃怪物字典的副本</returns>
    public Dictionary<string, GameObject> GetAllActiveMonsters()
    {
        return new Dictionary<string, GameObject>(activeMonsters);
    }
    
    /// <summary>
    /// 处理怪物死亡事件
    /// </summary>
    /// <param name="runtimeData">怪物运行时数据</param>
    private void OnMonsterDeath(MonsterRuntimeData runtimeData)
    {
        if (runtimeData == null) return;
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterPoolMgr] 收到怪物死亡事件，编号: {runtimeData.uniqueNumber}");
        }
        
        // 延迟回收怪物，给死亡动画时间播放
        StartCoroutine(DelayedHideMonster(runtimeData.uniqueNumber));
    }
    
    /// <summary>
    /// 检查是否所有怪物都已被击败
    /// </summary>
    private void CheckAllMonstersDefeated()
    {
        // 延迟检查，确保怪物已从活跃列表中移除
        StartCoroutine(DelayedCheckAllMonstersDefeated());
    }
    
    /// <summary>
    /// 延迟检查所有怪物是否被击败
    /// </summary>
    /// <returns>协程</returns>
    private System.Collections.IEnumerator DelayedCheckAllMonstersDefeated()
    {
        // 等待一帧，确保怪物状态更新完成
        yield return null;
        
        // 检查活跃怪物数量
        int activeCount = GetActiveMonsterCount();
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterPoolMgr] 当前活跃怪物数量: {activeCount}");
        }
        
        // 如果没有活跃怪物，通知波次管理器
        if (activeCount == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log("[MonsterPoolMgr] 所有怪物已被击败，通知波次管理器完成当前波次");
            }
            
            // 通知波次管理器所有怪物已被击败
            MonsterWaveMgr.Instance?.OnAllMonstersDefeated();
        }
    }
    
    /// <summary>
    /// 延迟隐藏怪物
    /// </summary>
    /// <param name="uniqueNumber">怪物编号</param>
    /// <returns>协程</returns>
    private System.Collections.IEnumerator DelayedHideMonster(string uniqueNumber)
    {
        // 等待一段时间让死亡动画播放
        yield return new WaitForSeconds(2.0f);
        
        // 回收怪物到对象池
        HideMonster(uniqueNumber);
        
        // 在怪物真正从活跃列表移除后，检查是否所有怪物都已死亡
        CheckAllMonstersDefeated();
    }
    

    
    void OnDestroy()
    {
        // 取消订阅事件
        MonsterEventManager.OnMonsterDeathDetected -= OnMonsterDeath;
    }
}

/// <summary>
/// 怪物预制体配置
/// </summary>
[Serializable]
public class MonsterPrefabConfig
{
    [Header("基础配置")]
    public int monsterId;           // 怪物ID
    public GameObject prefab;       // 预制体
    
    [Header("变换配置")]
    public Quaternion rotation = Quaternion.identity;  // 旋转
    public Vector3 scale = Vector3.one;                // 缩放
    
    [Header("对象池配置")]
    public int poolSize = 10;       // 对象池大小
}