using System.Collections;
using UnityEngine;

/// <summary>
/// Monster系统初始化管理器
/// 负责控制各个Monster组件的初始化顺序，确保依赖关系正确
/// </summary>
public class MonsterSystemInitializer : MonoBehaviour
{
    [Header("初始化配置")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [SerializeField] private bool autoStartFirstWave = false;
    [SerializeField] private int firstWaveId = 1;
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    [Header("组件引用")]
    [SerializeField] private MonsterConfigLoader configLoader;
    [SerializeField] private MonsterQueue monsterQueue;
    [SerializeField] private MonsterPoolMgr poolManager;
    [SerializeField] private MonsterEventManager eventManager;
    [SerializeField] private MonsterWaveMgr waveManager;
    
    private bool isInitialized = false;
    
    private void Start()
    {
        if (autoInitializeOnStart)
        {
            StartCoroutine(InitializeMonsterSystem());
        }
    }
    
    /// <summary>
    /// 手动初始化Monster系统
    /// </summary>
    public void InitializeSystem()
    {
        if (!isInitialized)
        {
            StartCoroutine(InitializeMonsterSystem());
        }
        else
        {
            LogDebug("Monster系统已经初始化过了");
        }
    }
    
    /// <summary>
    /// 按正确顺序初始化Monster系统的各个组件
    /// </summary>
    private IEnumerator InitializeMonsterSystem()
    {
        LogDebug("开始初始化Monster系统...");
        
        // 自动查找组件（如果没有手动指定）
        FindComponents();
        
        // 验证组件完整性
        if (!ValidateComponents())
        {
            LogError("Monster系统组件不完整，初始化失败");
            yield break;
        }
        
        // 步骤1：初始化配置加载器
        LogDebug("步骤1：初始化MonsterConfigLoader");
        if (configLoader != null)
        {
            configLoader.LoadMonsterConfigs();
            // 等待配置加载完成
            while (!configLoader.IsConfigLoaded)
            {
                yield return null;
            }
            LogDebug("MonsterConfigLoader初始化完成");
        }
        
        // 步骤2：初始化队列管理器
        LogDebug("步骤2：初始化MonsterQueue");
        if (monsterQueue != null)
        {
            monsterQueue.LoadSpawnQueue();
            // 等待队列加载完成
            while (!monsterQueue.IsQueueLoaded)
            {
                yield return null;
            }
            LogDebug("MonsterQueue初始化完成");
        }
        
        // 步骤3：初始化对象池管理器
        LogDebug("步骤3：初始化MonsterPoolMgr");
        if (poolManager != null)
        {
            poolManager.InitializePools();
            LogDebug("MonsterPoolMgr初始化完成");
        }
        
        // 步骤4：初始化事件管理器
        LogDebug("步骤4：初始化MonsterEventManager");
        if (eventManager != null)
        {
            // 事件管理器通常在Awake中自动初始化
            LogDebug("MonsterEventManager初始化完成");
        }
        
        // 步骤5：初始化波次管理器
        LogDebug("步骤5：初始化MonsterWaveMgr");
        if (waveManager != null)
        {
            waveManager.Initialize();
            LogDebug("MonsterWaveMgr初始化完成");
        }
        
        isInitialized = true;
        LogDebug("Monster系统初始化完成！");
        
        // 如果设置了自动开始第一波
        if (autoStartFirstWave && waveManager != null)
        {
            LogDebug($"自动开始第一波：{firstWaveId}");
            waveManager.StartWave(firstWaveId);
        }
    }
    
    /// <summary>
    /// 自动查找场景中的Monster组件
    /// </summary>
    private void FindComponents()
    {
        if (configLoader == null)
            configLoader = FindObjectOfType<MonsterConfigLoader>();
            
        if (monsterQueue == null)
            monsterQueue = FindObjectOfType<MonsterQueue>();
            
        if (poolManager == null)
            poolManager = FindObjectOfType<MonsterPoolMgr>();
            
        if (eventManager == null)
            eventManager = FindObjectOfType<MonsterEventManager>();
            
        if (waveManager == null)
            waveManager = FindObjectOfType<MonsterWaveMgr>();
    }
    
    /// <summary>
    /// 验证所有必需的组件是否存在
    /// </summary>
    private bool ValidateComponents()
    {
        bool isValid = true;
        
        if (configLoader == null)
        {
            LogError("缺少MonsterConfigLoader组件");
            isValid = false;
        }
        
        if (monsterQueue == null)
        {
            LogError("缺少MonsterQueue组件");
            isValid = false;
        }
        
        if (poolManager == null)
        {
            LogError("缺少MonsterPoolMgr组件");
            isValid = false;
        }
        
        if (eventManager == null)
        {
            LogError("缺少MonsterEventManager组件");
            isValid = false;
        }
        
        if (waveManager == null)
        {
            LogError("缺少MonsterWaveMgr组件");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 获取系统初始化状态
    /// </summary>
    public bool IsSystemInitialized => isInitialized;
    
    /// <summary>
    /// 手动开始指定波次
    /// </summary>
    public void StartWave(int waveId)
    {
        if (!isInitialized)
        {
            LogError("Monster系统尚未初始化，无法开始波次");
            return;
        }
        
        if (waveManager != null)
        {
            waveManager.StartWave(waveId);
        }
    }
    
    /// <summary>
    /// 停止当前波次
    /// </summary>
    public void StopCurrentWave()
    {
        if (waveManager != null)
        {
            waveManager.StopCurrentWave();
        }
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterSystemInitializer] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[MonsterSystemInitializer] {message}");
    }
    
    private void OnDestroy()
    {
        // 清理资源
        isInitialized = false;
    }
}