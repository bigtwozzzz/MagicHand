using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 魔法管理器
/// 管理所有魔法的数据、冷却计时器和触发逻辑
/// </summary>
public class MagicManager : MonoBehaviour
{
    [Header("魔法配置")]
    [Tooltip("所有魔法数据列表")]
    public List<MagicData> magicDataList = new List<MagicData>();
    
    [Header("UI配置")]
    [Tooltip("冷却提示文本组件")]
    public TextMeshProUGUI cooldownText;
    
    [Tooltip("冷却提示显示时间（秒）")]
    public float cooldownMessageDuration = 1.5f;
    
    [Tooltip("魔法信息显示文本")]
    public TextMeshProUGUI magicInfoText;
    
    [Header("调试配置")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = true;
    
    [Tooltip("是否在Scene视图中显示魔法范围")]
    public bool showMagicRangeGizmos = true;
    
    // 魔法冷却计时器字典 <魔法编号, 剩余冷却时间>
    private Dictionary<int, float> magicCooldowns = new Dictionary<int, float>();
    
    // 魔法数据字典，用于快速查找 <魔法编号, 魔法数据>
    private Dictionary<int, MagicData> magicDataDict = new Dictionary<int, MagicData>();
    
    // 魔法事件
    public System.Action<int, MagicData> OnMagicCast;           // 魔法施放事件
    public System.Action<int, MagicData> OnMagicCooldownStart;  // 魔法冷却开始事件
    public System.Action<int, MagicData> OnMagicCooldownEnd;    // 魔法冷却结束事件
    
    [Header("复合手势配置")]
    [Tooltip("复合手势检测时间窗口（秒）")]
    public float comboGestureTimeWindow = 1.2f;
    
    [Tooltip("是否启用复合手势检测")]
    public bool enableComboGestures = true;
    
    // 复合手势相关
    private List<GestureRecord> gestureHistory = new List<GestureRecord>();
    private Dictionary<string, int> comboGestureMap = new Dictionary<string, int>();
    
    // 单例模式
    private static MagicManager _instance;
    public static MagicManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MagicManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MagicManager");
                    _instance = go.AddComponent<MagicManager>();
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
        
        InitializeMagicSystem();
    }
    
    void Start()
    {
        // 订阅手势事件
        SubscribeToGestureEvents();
        
        // 初始化UI
        InitializeUI();
        
        // 初始化复合手势映射
        InitializeComboGestures();
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicManager] 魔法系统已启动，共加载 {magicDataList.Count} 种魔法");
        }
    }
    
    /// <summary>
    /// 初始化魔法系统
    /// </summary>
    void InitializeMagicSystem()
    {
        // 构建魔法数据字典
        magicDataDict.Clear();
        magicCooldowns.Clear();
        
        foreach (var magicData in magicDataList)
        {
            if (magicData != null && magicData.isEnabled)
            {
                magicDataDict[magicData.magicId] = magicData;
                magicCooldowns[magicData.magicId] = 0f; // 初始化冷却时间为0
            }
        }
        
        // 如果没有配置魔法数据，创建默认魔法
        if (magicDataList.Count == 0)
        {
            CreateDefaultMagicData();
        }
    }
    
    /// <summary>
    /// 创建默认魔法数据
    /// 从JSON配置文件加载魔法数据
    /// </summary>
    void CreateDefaultMagicData()
    {
        // 从JSON配置文件加载魔法数据
        var loadedMagics = MagicConfigLoader.LoadMagicConfig();
        
        if (loadedMagics.Count == 0)
        {
            Debug.LogWarning("[MagicManager] 未能从配置文件加载魔法数据，使用备用默认数据");
            
            // 如果配置文件加载失败，使用备用的默认魔法数据
            loadedMagics = new List<MagicData>
            {
                new MagicData(1, "抓取魔法", 80f, 2f) { range = new MagicRange(-1f, 1f, 2f, 0f) },
                new MagicData(4, "指向魔法", 120f, 3f) { range = new MagicRange(-0.5f, 0.5f, 5f, 0f) },
                new MagicData(3, "治疗魔法", -50f, 5f) { range = new MagicRange(-2f, 2f, 2f, -2f) },
                new MagicData(10, "拳击魔法", 150f, 1.5f) { range = new MagicRange(-1f, 1f, 1.5f, 0f) },
                new MagicData(11, "和平魔法", 0f, 10f) { range = new MagicRange(-3f, 3f, 3f, -3f) },
                new MagicData(13, "确认魔法", 100f, 2.5f) { range = new MagicRange(-1.5f, 1.5f, 3f, 0f) }
            };
        }
        
        magicDataList.AddRange(loadedMagics);
        
        // 重新初始化字典
        foreach (var magic in loadedMagics)
        {
            magicDataDict[magic.magicId] = magic;
            magicCooldowns[magic.magicId] = 0f;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicManager] 已加载 {loadedMagics.Count} 个魔法配置");
        }
    }
    
    /// <summary>
    /// 订阅手势事件
    /// </summary>
    void SubscribeToGestureEvents()
    {
        // 订阅手势事件管理器的事件
        GestureEventManager.SubscribeToGesture(OnGestureDetected);
    }
    
    /// <summary>
    /// 取消订阅手势事件
    /// </summary>
    void UnsubscribeFromGestureEvents()
    {
        GestureEventManager.UnsubscribeFromGesture(OnGestureDetected);
    }
    
    /// <summary>
    /// 处理手势检测事件
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    void OnGestureDetected(int gestureId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MagicManager] 检测到手势 {gestureId}，尝试触发对应魔法");
        }
        
        // 记录手势历史用于复合手势检测
        if (enableComboGestures)
        {
            RecordGesture(gestureId);
            
            // 检测复合手势
            if (CheckComboGestures())
            {
                return; // 如果触发了复合手势，就不再触发单一手势
            }
        }
        
        // 尝试触发单一手势魔法
        TryTriggerMagic(gestureId);
    }
    
    /// <summary>
    /// 尝试触发魔法
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <returns>是否成功触发</returns>
    public bool TryTriggerMagic(int magicId)
    {
        // 检查魔法是否存在
        if (!magicDataDict.ContainsKey(magicId))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[MagicManager] 魔法编号 {magicId} 不存在");
            }
            return false;
        }
        
        MagicData magicData = magicDataDict[magicId];
        
        // 检查魔法是否启用
        if (!magicData.isEnabled)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[MagicManager] 魔法 {magicData.magicName} 已禁用");
            }
            return false;
        }
        
        // 检查冷却时间
        if (IsMagicOnCooldown(magicId))
        {
            ShowCooldownMessage(magicData);
            return false;
        }
        
        // 触发魔法
        TriggerMagic(magicId, magicData);
        return true;
    }
    
    /// <summary>
    /// 触发魔法
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <param name="magicData">魔法数据</param>
    void TriggerMagic(int magicId, MagicData magicData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MagicManager] 触发魔法: {magicData.magicName} (ID: {magicId}), 伤害: {magicData.damage}, 冷却: {magicData.cooldownTime}s");
        }
        
        // 开始冷却计时
        StartCooldown(magicId, magicData.cooldownTime);
        
        // 触发魔法施放事件
        OnMagicCast?.Invoke(magicId, magicData);
        
        // 更新UI显示
        UpdateMagicInfoUI(magicData);
        
        // 播放音效（如果有）
        PlayMagicSound(magicData);
        
        // 生成特效（如果有）
        SpawnMagicEffect(magicData);
    }
    
    /// <summary>
    /// 开始魔法冷却
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <param name="cooldownTime">冷却时间</param>
    void StartCooldown(int magicId, float cooldownTime)
    {
        magicCooldowns[magicId] = cooldownTime;
        
        MagicData magicData = magicDataDict[magicId];
        OnMagicCooldownStart?.Invoke(magicId, magicData);
        
        // 启动冷却协程
        StartCoroutine(CooldownCoroutine(magicId));
    }
    
    /// <summary>
    /// 冷却协程
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <returns>协程</returns>
    IEnumerator CooldownCoroutine(int magicId)
    {
        while (magicCooldowns[magicId] > 0)
        {
            magicCooldowns[magicId] -= Time.deltaTime;
            yield return null;
        }
        
        magicCooldowns[magicId] = 0f;
        
        MagicData magicData = magicDataDict[magicId];
        OnMagicCooldownEnd?.Invoke(magicId, magicData);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicManager] 魔法 {magicData.magicName} 冷却完成");
        }
    }
    
    /// <summary>
    /// 检查魔法是否在冷却中
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <returns>是否在冷却中</returns>
    public bool IsMagicOnCooldown(int magicId)
    {
        return magicCooldowns.ContainsKey(magicId) && magicCooldowns[magicId] > 0f;
    }
    
    /// <summary>
    /// 获取魔法剩余冷却时间
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <returns>剩余冷却时间</returns>
    public float GetMagicCooldownTime(int magicId)
    {
        return magicCooldowns.ContainsKey(magicId) ? magicCooldowns[magicId] : 0f;
    }
    
    /// <summary>
    /// 显示冷却提示消息
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    void ShowCooldownMessage(MagicData magicData)
    {
        float remainingTime = GetMagicCooldownTime(magicData.magicId);
        string message = $"冷却中\n{magicData.magicName}\n剩余: {remainingTime:F1}s";
        
        if (cooldownText != null)
        {
            cooldownText.text = message;
            cooldownText.gameObject.SetActive(true);
            
            // 启动隐藏协程
            StartCoroutine(HideCooldownMessage());
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicManager] {message.Replace("\n", " ")}");
        }
    }
    
    /// <summary>
    /// 隐藏冷却提示消息的协程
    /// </summary>
    /// <returns>协程</returns>
    IEnumerator HideCooldownMessage()
    {
        yield return new WaitForSeconds(cooldownMessageDuration);
        
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    void InitializeUI()
    {
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
        
        if (magicInfoText != null)
        {
            magicInfoText.text = "魔法系统就绪";
        }
    }
    
    /// <summary>
    /// 更新魔法信息UI
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    void UpdateMagicInfoUI(MagicData magicData)
    {
        if (magicInfoText != null)
        {
            string info = $"施放魔法: {magicData.magicName}\n伤害: {magicData.damage}\n范围: {magicData.range.GetArea():F1}";
            magicInfoText.text = info;
        }
    }
    
    /// <summary>
    /// 播放魔法音效
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    void PlayMagicSound(MagicData magicData)
    {
        if (magicData.castSound != null)
        {
            AudioSource.PlayClipAtPoint(magicData.castSound, transform.position);
        }
    }
    
    /// <summary>
    /// 生成魔法特效
    /// </summary>
    /// <param name="magicData">魔法数据</param>
    void SpawnMagicEffect(MagicData magicData)
    {
        if (magicData.effectPrefab != null)
        {
            Vector3 spawnPosition = transform.position + magicData.range.GetCenter();
            
            // 优先使用对象池生成特效
            if (MagicEffectPool.Instance != null)
            {
                GameObject effect = MagicEffectPool.Instance.SpawnEffect(magicData.magicId, spawnPosition, Quaternion.identity, magicData.amplificationFactor);
                if (effect == null)
                {
                    // 如果对象池没有配置，回退到直接实例化
                    effect = Instantiate(magicData.effectPrefab, spawnPosition, Quaternion.identity);
                    if (magicData.amplificationFactor != 1.0f)
                    {
                        effect.transform.localScale *= magicData.amplificationFactor;
                    }
                }
            }
            else
            {
                // 直接实例化特效
                GameObject effect = Instantiate(magicData.effectPrefab, spawnPosition, Quaternion.identity);
                if (magicData.amplificationFactor != 1.0f)
                {
                    effect.transform.localScale *= magicData.amplificationFactor;
                }
            }
        }
    }
    
    /// <summary>
    /// 获取魔法数据
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <returns>魔法数据</returns>
    public MagicData GetMagicData(int magicId)
    {
        return magicDataDict.ContainsKey(magicId) ? magicDataDict[magicId] : null;
    }
    
    /// <summary>
    /// 获取所有魔法数据
    /// </summary>
    /// <returns>魔法数据列表</returns>
    public List<MagicData> GetAllMagicData()
    {
        return new List<MagicData>(magicDataList);
    }
    
    /// <summary>
    /// 重置所有魔法冷却
    /// </summary>
    public void ResetAllCooldowns()
    {
        foreach (var key in magicCooldowns.Keys)
        {
            magicCooldowns[key] = 0f;
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[MagicManager] 所有魔法冷却已重置");
        }
    }
    
    void Update()
    {
        // 这里可以添加实时更新逻辑，如UI刷新等
    }
    
    void OnDestroy()
    {
        UnsubscribeFromGestureEvents();
    }
    
    void OnDisable()
    {
        UnsubscribeFromGestureEvents();
    }
    
    /// <summary>
    /// 初始化复合手势映射
    /// </summary>
    void InitializeComboGestures()
    {
        comboGestureMap.Clear();
        
        // 添加复合手势映射：Fist -> Palm = 魔法ID 20
        comboGestureMap["10->11"] = 20; // 假设Fist=10, Palm=11
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicManager] 已初始化 {comboGestureMap.Count} 个复合手势映射");
        }
    }
    
    /// <summary>
    /// 记录手势到历史记录
    /// </summary>
    /// <param name="gestureId">手势ID</param>
    void RecordGesture(int gestureId)
    {
        float currentTime = Time.time;
        
        // 添加新手势记录
        gestureHistory.Add(new GestureRecord(gestureId, currentTime));
        
        // 清理过期的手势记录
        gestureHistory.RemoveAll(record => currentTime - record.timestamp > comboGestureTimeWindow);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicManager] 记录手势 {gestureId}，当前历史记录数量: {gestureHistory.Count}");
        }
    }
    
    /// <summary>
    /// 检测复合手势
    /// </summary>
    /// <returns>是否检测到复合手势并成功触发</returns>
    bool CheckComboGestures()
    {
        if (gestureHistory.Count < 2) return false;
        
        // 检查最近的两个手势是否构成复合手势
        var recent = gestureHistory[gestureHistory.Count - 1];
        var previous = gestureHistory[gestureHistory.Count - 2];
        
        // 构建手势序列字符串
        string comboKey = $"{previous.gestureId}->{recent.gestureId}";
        
        if (comboGestureMap.ContainsKey(comboKey))
        {
            int comboMagicId = comboGestureMap[comboKey];
            
            if (enableDebugLog)
            {
                Debug.Log($"[MagicManager] 检测到复合手势: {comboKey}，尝试触发魔法 {comboMagicId}");
            }
            
            // 尝试触发复合魔法
            if (TryTriggerMagic(comboMagicId))
            {
                // 清理手势历史，避免重复触发
                gestureHistory.Clear();
                return true;
            }
        }
        
        return false;
    }
    
    // 在Scene视图中绘制魔法范围
    void OnDrawGizmos()
    {
        if (!showMagicRangeGizmos) return;
        
        foreach (var magicData in magicDataList)
        {
            if (magicData != null && magicData.isEnabled)
            {
                Color gizmoColor = IsMagicOnCooldown(magicData.magicId) ? Color.red : Color.green;
                gizmoColor.a = 0.3f;
                magicData.range.DrawGizmos(transform.position, gizmoColor);
            }
        }
    }
    
    // 测试方法
    [ContextMenu("测试触发魔法1")]
    void TestTriggerMagic1()
    {
        TryTriggerMagic(1);
    }
    
    [ContextMenu("测试触发魔法4")]
    void TestTriggerMagic4()
    {
        TryTriggerMagic(4);
    }
    
    [ContextMenu("重置所有冷却")]
    void TestResetCooldowns()
    {
        ResetAllCooldowns();
    }
}

/// <summary>
/// 手势记录结构
/// 用于复合手势检测
/// </summary>
[System.Serializable]
public struct GestureRecord
{
    public int gestureId;    // 手势ID
    public float timestamp;  // 检测时间戳
    
    public GestureRecord(int id, float time)
    {
        gestureId = id;
        timestamp = time;
    }
}