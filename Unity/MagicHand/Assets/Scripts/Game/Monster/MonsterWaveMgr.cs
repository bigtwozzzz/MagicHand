using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怪物生成波次管理器
/// 负责管理怪物的波次生成逻辑
/// </summary>
public class MonsterWaveMgr : MonoBehaviour
{
    private static MonsterWaveMgr _instance;
    public static MonsterWaveMgr Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MonsterWaveMgr>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MonsterWaveMgr");
                    _instance = go.AddComponent<MonsterWaveMgr>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    [Header("波次配置")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float autoSwitchTime = 120f; // 自动切换时间（秒）
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 当前波次状态
    private int currentWave = 1;
    private bool isWaveActive = false;
    private float waveStartTime;
    private List<int> waveOrder;
    
    // 当前波次的生成事件
    private List<SpawnEvent> currentWaveEvents;
    private HashSet<int> completedEvents = new HashSet<int>();
    
    // 协程引用
    private Coroutine waveCoroutine;
    
    // OnNextWave事件处理
    private bool isWaitingForNextWave = false;
    private bool shouldSkipWaiting = false;
    
    // 事件
    public System.Action<int> OnWaveStart;      // 波次开始事件
    public System.Action<int> OnWaveComplete;   // 波次完成事件
    public System.Action OnAllWavesComplete;    // 所有波次完成事件
    
    private void Awake()
    {
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
    
    private void Start()
    {
        // 订阅OnNextWave事件
        SubscribeToNextWaveEvent();
        
        // 等待队列加载完成
        if (MonsterQueue.Instance != null)
        {
            MonsterQueue.Instance.OnQueueLoaded += OnQueueLoaded;
            
            // 如果已经加载完成，直接初始化
            if (MonsterQueue.Instance.GetWaveCount() > 0)
            {
                OnQueueLoaded();
            }
        }
    }
    
    /// <summary>
    /// 手动初始化波次管理器
    /// </summary>
    public void Initialize()
    {
        if (MonsterQueue.Instance != null)
        {
            OnQueueLoaded();
        }
        else
        {
            Debug.LogError("[MonsterWaveMgr] MonsterQueue实例不存在，无法初始化");
        }
    }
    
    /// <summary>
    /// 队列加载完成回调
    /// </summary>
    private void OnQueueLoaded()
    {
        waveOrder = MonsterQueue.Instance.GetWaveOrder();
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterWaveMgr] 波次管理器初始化完成，总波次数: {waveOrder.Count}");
        }
        
        if (autoStart && waveOrder.Count > 0)
        {
            StartWave(waveOrder[0]);
        }
    }
    
    /// <summary>
    /// 开始指定波次
    /// </summary>
    /// <param name="waveNumber">波次编号</param>
    public void StartWave(int waveNumber)
    {
        if (isWaveActive)
        {
            Debug.LogWarning($"[MonsterWaveMgr] 波次 {currentWave} 正在进行中，无法开始新波次");
            return;
        }
        
        WaveData waveData = MonsterQueue.Instance?.GetWaveData(waveNumber);
        if (waveData == null)
        {
            Debug.LogError($"[MonsterWaveMgr] 未找到波次 {waveNumber} 的数据");
            return;
        }
        
        currentWave = waveNumber;
        isWaveActive = true;
        waveStartTime = Time.time;
        
        // 准备当前波次的生成事件
        currentWaveEvents = new List<SpawnEvent>(waveData.spawnEvents);
        completedEvents.Clear();
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterWaveMgr] 开始波次 {waveNumber}: {waveData.description}, 事件数量: {currentWaveEvents.Count}");
        }
        
        // 触发波次开始事件
        OnWaveStart?.Invoke(waveNumber);
        
        // 开始波次协程
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }
        waveCoroutine = StartCoroutine(WaveCoroutine());
    }
    
    /// <summary>
    /// 波次协程
    /// </summary>
    private IEnumerator WaveCoroutine()
    {
        while (isWaveActive)
        {
            // 检查游戏是否暂停
            if (GameStateManager.Instance.IsPaused)
            {
                yield return null;
                continue;
            }
            
            float elapsedTime = Time.time - waveStartTime;
            
            // 检查生成事件
            CheckSpawnEvents(elapsedTime);
            
            // 检查波次生成事件是否完成（不再自动切换波次，等待所有怪物死亡）
            if (IsWaveComplete())
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[MonsterWaveMgr] 波次 {currentWave} 所有生成事件已完成，等待所有怪物被击败");
                }
                
                // 波次生成完成，但不立即切换，等待所有怪物死亡
                // 通过MonsterPoolMgr的OnAllMonstersDefeated回调来完成波次
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 检查生成事件
    /// </summary>
    /// <param name="elapsedTime">已过时间</param>
    private void CheckSpawnEvents(float elapsedTime)
    {
        if (currentWaveEvents == null) return;
        
        foreach (var spawnEvent in currentWaveEvents)
        {
            if (spawnEvent.enabled && !completedEvents.Contains(spawnEvent.eventId))
            {
                if (elapsedTime >= spawnEvent.triggerTime)
                {
                    ExecuteSpawnEvent(spawnEvent);
                    completedEvents.Add(spawnEvent.eventId);
                }
            }
        }
    }
    
    /// <summary>
    /// 执行生成事件
    /// </summary>
    /// <param name="spawnEvent">生成事件</param>
    private void ExecuteSpawnEvent(SpawnEvent spawnEvent)
    {
        if (MonsterPoolMgr.Instance == null)
        {
            Debug.LogError("[MonsterWaveMgr] MonsterPoolMgr实例不存在");
            return;
        }
        
        for (int i = 0; i < spawnEvent.spawnCount; i++)
        {
            // 计算生成位置：第一个怪物在原位置，后续每个怪物z坐标+1
            Vector3 adjustedPosition = spawnEvent.spawnPosition;
            if (i > 0)
            {
                adjustedPosition.z += i;
            }
            
            string monsterNumber = MonsterPoolMgr.Instance.ActivateMonster(spawnEvent.id, adjustedPosition);
            
            if (enableDebugLog && !string.IsNullOrEmpty(monsterNumber))
            {
                Debug.Log($"[MonsterWaveMgr] 生成怪物: 波次={currentWave}, 事件ID={spawnEvent.eventId}, 怪物编号={monsterNumber}, 位置={adjustedPosition}");
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterWaveMgr] 执行生成事件: {spawnEvent.description}, 生成数量: {spawnEvent.spawnCount}");
        }
    }
    
    /// <summary>
    /// 检查波次是否完成
    /// </summary>
    /// <returns>是否完成</returns>
    private bool IsWaveComplete()
    {
        if (currentWaveEvents == null) return true;
        
        foreach (var spawnEvent in currentWaveEvents)
        {
            if (spawnEvent.enabled && !completedEvents.Contains(spawnEvent.eventId))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 切换到下一波次
    /// </summary>
    public void NextWave()
    {
        // 如果当前有活跃波次，先完成它
        if (isWaveActive)
        {
            CompleteCurrentWave();
        }
        
        // 查找下一波次
        int nextWaveIndex = waveOrder.IndexOf(currentWave) + 1;
        
        if (nextWaveIndex < waveOrder.Count)
        {
            // 开始下一波次
            int nextWave = waveOrder[nextWaveIndex];
            StartWave(nextWave);
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterWaveMgr] 开始下一波次: {nextWave}");
            }
        }
        else
        {
            // 所有波次完成
            if (enableDebugLog)
            {
                Debug.Log("[MonsterWaveMgr] 所有波次已完成");
            }
            
            OnAllWavesComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// 完成当前波次
    /// </summary>
    private void CompleteCurrentWave()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterWaveMgr] 波次 {currentWave} 完成");
        }
        
        // 触发波次完成事件
        OnWaveComplete?.Invoke(currentWave);
        
        // 重置状态
        isWaveActive = false;
        
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
    }
    
    /// <summary>
    /// 处理所有怪物被击败的情况
    /// </summary>
    public void OnAllMonstersDefeated()
    {
        if (!isWaveActive)
        {
            if (enableDebugLog)
            {
                Debug.Log("[MonsterWaveMgr] 当前没有活跃波次，忽略怪物击败事件");
            }
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[MonsterWaveMgr] 所有怪物已被击败，立即完成当前波次");
        }
        
        // 立即完成当前波次
        CompleteCurrentWave();
    }
    
    /// <summary>
    /// 手动切换到指定波次
    /// </summary>
    /// <param name="waveNumber">目标波次编号</param>
    public void SwitchToWave(int waveNumber)
    {
        if (isWaveActive)
        {
            CompleteCurrentWave();
        }
        
        StartWave(waveNumber);
    }
    
    /// <summary>
    /// 停止当前波次
    /// </summary>
    public void StopCurrentWave()
    {
        if (isWaveActive)
        {
            CompleteCurrentWave();
        }
    }
    
    /// <summary>
    /// 重置波次管理器
    /// </summary>
    public void ResetWaveManager()
    {
        StopCurrentWave();
        currentWave = 1;
        
        if (enableDebugLog)
        {
            Debug.Log("[MonsterWaveMgr] 波次管理器已重置");
        }
    }
    
    /// <summary>
    /// 获取当前波次编号
    /// </summary>
    /// <returns>当前波次编号</returns>
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    /// <summary>
    /// 获取波次是否活跃
    /// </summary>
    /// <returns>是否活跃</returns>
    public bool IsWaveActive()
    {
        return isWaveActive;
    }
    
    /// <summary>
    /// 获取当前波次剩余时间
    /// </summary>
    /// <returns>剩余时间（秒）</returns>
    public float GetWaveRemainingTime()
    {
        if (!isWaveActive) return 0f;
        
        float elapsedTime = Time.time - waveStartTime;
        return Mathf.Max(0f, autoSwitchTime - elapsedTime);
    }
    
    /// <summary>
    /// 获取当前波次进度（0-1）
    /// </summary>
    /// <returns>波次进度</returns>
    public float GetWaveProgress()
    {
        if (!isWaveActive || currentWaveEvents == null || currentWaveEvents.Count == 0)
        {
            return 0f;
        }
        
        return (float)completedEvents.Count / currentWaveEvents.Count;
    }
    
    /// <summary>
    /// 订阅OnNextWave事件
    /// </summary>
    private void SubscribeToNextWaveEvent()
    {
        GeneralGestureHandler.SubscribeToNextWave(OnNextWaveReceived);
        
        if (enableDebugLog)
        {
            Debug.Log("[MonsterWaveMgr] 已订阅OnNextWave事件");
        }
    }
    
    /// <summary>
    /// 取消订阅OnNextWave事件
    /// </summary>
    private void UnsubscribeFromNextWaveEvent()
    {
        GeneralGestureHandler.UnsubscribeFromNextWave(OnNextWaveReceived);
        
        if (enableDebugLog)
        {
            Debug.Log("[MonsterWaveMgr] 已取消订阅OnNextWave事件");
        }
    }
    
    /// <summary>
    /// 处理OnNextWave事件
    /// </summary>
    private void OnNextWaveReceived()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterWaveMgr] 收到OnNextWave事件，当前状态：isWaveActive={isWaveActive}, isWaitingForNextWave={isWaitingForNextWave}");
        }
        
        // 只有在波次完成后的等待期间才响应OnNextWave事件
        if (isWaitingForNextWave)
        {
            shouldSkipWaiting = true;
            
            if (enableDebugLog)
            {
                Debug.Log("[MonsterWaveMgr] OnNextWave事件生效，将跳过等待时间直接进入下一波次");
            }
        }
        else if (isWaveActive)
        {
            if (enableDebugLog)
            {
                Debug.Log("[MonsterWaveMgr] 当前波次尚未完成，OnNextWave事件被忽略");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log("[MonsterWaveMgr] 当前没有活跃波次，OnNextWave事件被忽略");
            }
        }
    }
    
    /// <summary>
    /// 检查是否正在等待下一波次
    /// </summary>
    /// <returns>是否正在等待</returns>
    public bool IsWaitingForNextWave()
    {
        return isWaitingForNextWave;
    }
    
    void OnDestroy()
    {
        UnsubscribeFromNextWaveEvent();
    }
    
    void OnDisable()
    {
        UnsubscribeFromNextWaveEvent();
    }
}