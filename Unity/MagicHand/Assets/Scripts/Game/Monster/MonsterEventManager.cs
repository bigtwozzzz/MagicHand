using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 怪物事件管理器
/// 定义怪物生成事件OnMonsterSpawn，用于统一管理怪物事件的触发和监听
/// </summary>
public class MonsterEventManager : MonoBehaviour
{
    [Header("怪物事件配置")]
    public bool enableDebugLog = true;
    
    // 怪物生成事件 - 包含怪物编号的UnityEvent
    [System.Serializable]
    public class MonsterSpawnEvent : UnityEvent<int> { }
    
    [Header("怪物事件")]
    public MonsterSpawnEvent OnMonsterSpawn = new MonsterSpawnEvent();
    
    // C#事件，供代码订阅使用
    public static event Action<int> OnMonsterSpawnDetected;
    public static event Action<MonsterRuntimeData> OnMonsterDeathDetected;
    
    // 单例模式，方便全局访问
    private static MonsterEventManager _instance;
    public static MonsterEventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MonsterEventManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MonsterEventManager");
                    _instance = go.AddComponent<MonsterEventManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
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
    }
    
    /// <summary>
    /// 触发怪物生成事件
    /// </summary>
    /// <param name="monsterId">怪物编号</param>
    public void TriggerMonsterSpawnEvent(int monsterId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterEventManager] 触发怪物生成事件，编号: {monsterId}");
        }
        
        // 触发UnityEvent
        OnMonsterSpawn?.Invoke(monsterId);
        
        // 触发C#事件
        OnMonsterSpawnDetected?.Invoke(monsterId);
    }
    
    /// <summary>
    /// 静态方法，方便外部调用
    /// </summary>
    /// <param name="monsterId">怪物编号</param>
    public static void TriggerMonsterSpawn(int monsterId)
    {
        Instance.TriggerMonsterSpawnEvent(monsterId);
    }
    
    /// <summary>
    /// 触发怪物死亡事件
    /// </summary>
    /// <param name="runtimeData">怪物运行时数据</param>
    public void TriggerMonsterDeath(MonsterRuntimeData runtimeData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterEventManager] 触发怪物死亡事件，编号: {runtimeData.uniqueNumber}");
        }
        
        // 触发C#事件
        OnMonsterDeathDetected?.Invoke(runtimeData);
    }
    
    /// <summary>
    /// 订阅怪物生成事件（代码方式）
    /// </summary>
    /// <param name="callback">回调函数</param>
    public static void SubscribeToMonsterSpawn(Action<int> callback)
    {
        OnMonsterSpawnDetected += callback;
    }
    
    /// <summary>
    /// 取消订阅怪物生成事件（代码方式）
    /// </summary>
    /// <param name="callback">回调函数</param>
    public static void UnsubscribeFromMonsterSpawn(Action<int> callback)
    {
        OnMonsterSpawnDetected -= callback;
    }
    
    void OnDestroy()
    {
        // 清理所有事件订阅
        OnMonsterSpawnDetected = null;
        OnMonsterDeathDetected = null;
    }
    
    // 测试方法，可在Inspector中调用
    [ContextMenu("测试生成怪物1")]
    void TestSpawnMonster1()
    {
        TriggerMonsterSpawnEvent(1);
    }
    
    [ContextMenu("测试生成怪物2")]
    void TestSpawnMonster2()
    {
        TriggerMonsterSpawnEvent(2);
    }
    
    [ContextMenu("测试生成怪物3")]
    void TestSpawnMonster3()
    {
        TriggerMonsterSpawnEvent(3);
    }
}