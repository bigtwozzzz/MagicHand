using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手势信号处理器
/// 负责监听HandGestureUDPReceiver接收到的手势信号，并触发GestureEventManager的编号事件
/// 使用方法：将此脚本挂载到与HandGestureUDPReceiver相同的GameObject上，或通过Inspector指定接收器
/// </summary>
[RequireComponent(typeof(GestureEventManager))]
public class GestureSignalHandler : MonoBehaviour
{
    [Header("手势接收器配置")]
    [Tooltip("手势UDP接收器，如果为空则自动查找同GameObject上的组件")]
    public HandGestureUDPReceiver gestureReceiver;
    
    [Header("处理配置")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = true;
    
    [Tooltip("是否只处理左手手势")]
    public bool processLeftHandOnly = false;
    
    [Tooltip("是否只处理右手手势")]
    public bool processRightHandOnly = false;
    
    [Tooltip("忽略的手势编号列表（这些手势不会触发事件）")]
    public List<int> ignoredGestureIds = new List<int> { 0 }; // 默认忽略no_gesture
    
    [Header("防抖动配置")]
    [Tooltip("是否启用防抖动（避免同一手势短时间内重复触发）")]
    public bool enableDebounce = true;
    
    [Tooltip("防抖动时间间隔（秒）")]
    public float debounceInterval = 0.8f;
    
    [Header("防误触配置")]
    [Tooltip("是否启用防误触（手势需要保持一定时间才触发）")]
    public bool enableHoldToTrigger = true;
    
    [Tooltip("手势保持时间（秒）")]
    public float holdDuration = 0.5f;
    
    // 防抖动相关
    private Dictionary<int, float> lastGestureTriggerTime = new Dictionary<int, float>();
    
    // 防误触相关
    private Dictionary<int, float> gestureStartTime = new Dictionary<int, float>();
    private Dictionary<int, Coroutine> gestureHoldCoroutines = new Dictionary<int, Coroutine>();
    private int currentGestureId = -1;
    
    // 引用组件
    private GestureEventManager eventManager;
    
    void Start()
    {
        // 获取手势事件管理器
        eventManager = GetComponent<GestureEventManager>();
        if (eventManager == null)
        {
            eventManager = GestureEventManager.Instance;
        }
        
        // 获取手势接收器
        if (gestureReceiver == null)
        {
            gestureReceiver = GetComponent<HandGestureUDPReceiver>();
        }
        
        if (gestureReceiver == null)
        {
            gestureReceiver = FindObjectOfType<HandGestureUDPReceiver>();
        }
        
        if (gestureReceiver == null)
        {
            Debug.LogError("[GestureSignalHandler] 未找到HandGestureUDPReceiver组件！请确保场景中存在该组件。");
            enabled = false;
            return;
        }
        
        // 订阅手势接收器的事件
        SubscribeToGestureEvents();
        
        if (enableDebugLog)
        {
            Debug.Log("[GestureSignalHandler] 手势信号处理器已启动，开始监听手势事件。");
        }
    }
    
    void SubscribeToGestureEvents()
    {
        // 订阅总体手势数据接收事件
        gestureReceiver.OnHandDataReceived += OnHandDataReceived;
        
        // 根据配置订阅特定手的事件
        if (!processRightHandOnly)
        {
            gestureReceiver.OnLeftHandGesture += OnLeftHandGesture;
        }
        
        if (!processLeftHandOnly)
        {
            gestureReceiver.OnRightHandGesture += OnRightHandGesture;
        }
    }
    
    void UnsubscribeFromGestureEvents()
    {
        if (gestureReceiver != null)
        {
            gestureReceiver.OnHandDataReceived -= OnHandDataReceived;
            gestureReceiver.OnLeftHandGesture -= OnLeftHandGesture;
            gestureReceiver.OnRightHandGesture -= OnRightHandGesture;
        }
    }
    
    /// <summary>
    /// 处理接收到的手势数据
    /// </summary>
    /// <param name="hands">手势数据列表</param>
    void OnHandDataReceived(List<HandGestureUDPReceiver.HandData> hands)
    {
        foreach (var hand in hands)
        {
            ProcessGestureData(hand);
        }
    }
    
    /// <summary>
    /// 处理左手手势
    /// </summary>
    /// <param name="handData">左手数据</param>
    void OnLeftHandGesture(HandGestureUDPReceiver.HandData handData)
    {
        if (!processRightHandOnly)
        {
            ProcessGestureData(handData, "左手");
        }
    }
    
    /// <summary>
    /// 处理右手手势
    /// </summary>
    /// <param name="handData">右手数据</param>
    void OnRightHandGesture(HandGestureUDPReceiver.HandData handData)
    {
        if (!processLeftHandOnly)
        {
            ProcessGestureData(handData, "右手");
        }
    }
    
    /// <summary>
    /// 处理单个手势数据
    /// </summary>
    /// <param name="handData">手势数据</param>
    /// <param name="handType">手部类型（用于日志）</param>
    void ProcessGestureData(HandGestureUDPReceiver.HandData handData, string handType = "")
    {
        if (handData == null) return;
        
        int gestureId = handData.gesture_id;
        
        // 检查是否在忽略列表中
        if (ignoredGestureIds.Contains(gestureId))
        {
            // 如果当前手势变为忽略手势，停止所有保持检测
            if (enableHoldToTrigger)
            {
                StopAllHoldCoroutines();
                currentGestureId = -1;
            }
            return;
        }
        
        // 防误触处理
        if (enableHoldToTrigger)
        {
            ProcessHoldToTrigger(gestureId, handData, handType);
        }
        else
        {
            // 防抖动检查
            if (enableDebounce && ShouldDebounce(gestureId))
            {
                return;
            }
            
            // 直接触发手势事件
            TriggerGestureEvent(gestureId, handData, handType);
        }
    }
    
    /// <summary>
    /// 检查是否应该进行防抖动
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    /// <returns>true表示应该防抖动（不触发事件）</returns>
    bool ShouldDebounce(int gestureId)
    {
        float currentTime = Time.time;
        
        if (lastGestureTriggerTime.ContainsKey(gestureId))
        {
            float timeSinceLastTrigger = currentTime - lastGestureTriggerTime[gestureId];
            if (timeSinceLastTrigger < debounceInterval)
            {
                return true; // 需要防抖动
            }
        }
        
        // 更新最后触发时间
        lastGestureTriggerTime[gestureId] = currentTime;
        return false;
    }
    
    /// <summary>
    /// 处理防误触逻辑
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    /// <param name="handData">手势数据</param>
    /// <param name="handType">手部类型</param>
    void ProcessHoldToTrigger(int gestureId, HandGestureUDPReceiver.HandData handData, string handType)
    {
        // 如果手势发生变化
        if (currentGestureId != gestureId)
        {
            // 停止之前的所有协程
            StopAllHoldCoroutines();
            
            // 更新当前手势
            currentGestureId = gestureId;
            
            // 防抖动检查
            if (enableDebounce && ShouldDebounce(gestureId))
            {
                return;
            }
            
            // 开始新的保持检测
            gestureStartTime[gestureId] = Time.time;
            
            if (enableDebugLog)
            {
                string logMessage = string.IsNullOrEmpty(handType)
                    ? $"[GestureSignalHandler] 开始保持检测 - 编号: {gestureId}, 名称: {handData.gesture_name}, 需要保持: {holdDuration}秒"
                    : $"[GestureSignalHandler] {handType}开始保持检测 - 编号: {gestureId}, 名称: {handData.gesture_name}, 需要保持: {holdDuration}秒";
                Debug.Log(logMessage);
            }
            
            // 启动协程
            Coroutine holdCoroutine = StartCoroutine(HoldGestureCoroutine(gestureId, handData, handType));
            gestureHoldCoroutines[gestureId] = holdCoroutine;
        }
        // 如果是相同手势，继续保持（不需要额外处理）
    }
    
    /// <summary>
    /// 停止所有保持检测协程
    /// </summary>
    void StopAllHoldCoroutines()
    {
        foreach (var kvp in gestureHoldCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        gestureHoldCoroutines.Clear();
    }
    
    /// <summary>
    /// 手势保持检测协程
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    /// <param name="handData">手势数据</param>
    /// <param name="handType">手部类型</param>
    System.Collections.IEnumerator HoldGestureCoroutine(int gestureId, HandGestureUDPReceiver.HandData handData, string handType)
    {
        yield return new WaitForSeconds(holdDuration);
        
        // 检查手势是否仍然保持
        if (currentGestureId == gestureId)
        {
            if (enableDebugLog)
            {
                string logMessage = string.IsNullOrEmpty(handType)
                    ? $"[GestureSignalHandler] 手势保持成功，触发事件 - 编号: {gestureId}, 名称: {handData.gesture_name}"
                    : $"[GestureSignalHandler] {handType}手势保持成功，触发事件 - 编号: {gestureId}, 名称: {handData.gesture_name}";
                Debug.Log(logMessage);
            }
            
            // 触发手势事件
            TriggerGestureEvent(gestureId, handData, handType);
        }
        
        // 清理协程记录
        if (gestureHoldCoroutines.ContainsKey(gestureId))
        {
            gestureHoldCoroutines.Remove(gestureId);
        }
    }
    
    /// <summary>
    /// 触发手势事件
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    /// <param name="handData">手势数据</param>
    /// <param name="handType">手部类型</param>
    void TriggerGestureEvent(int gestureId, HandGestureUDPReceiver.HandData handData, string handType)
    {
        if (enableDebugLog)
        {
            string logMessage = string.IsNullOrEmpty(handType) 
                ? $"[GestureSignalHandler] 触发手势事件 - 编号: {gestureId}, 名称: {handData.gesture_name}"
                : $"[GestureSignalHandler] {handType}触发手势事件 - 编号: {gestureId}, 名称: {handData.gesture_name}";
            Debug.Log(logMessage);
        }
        
        // 通过事件管理器触发事件
        if (eventManager != null)
        {
            eventManager.TriggerGestureEvent(gestureId);
        }
        else
        {
            // 如果没有事件管理器，使用静态方法
            GestureEventManager.TriggerGesture(gestureId);
        }
    }
    
    /// <summary>
    /// 手动触发手势事件（用于测试）
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    public void ManualTriggerGesture(int gestureId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GestureSignalHandler] 手动触发手势事件 - 编号: {gestureId}");
        }
        
        // 手动触发时绕过防误触和防抖动
        GestureEventManager.TriggerGesture(gestureId);
    }
    
    /// <summary>
    /// 手动触发手势事件（支持防误触，用于测试）
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    public void ManualTriggerGestureWithHold(int gestureId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GestureSignalHandler] 手动触发手势事件（支持防误触） - 编号: {gestureId}");
        }
        
        // 创建模拟手势数据
        var mockHandData = new HandGestureUDPReceiver.HandData
        {
            gesture_id = gestureId,
            gesture_name = $"手势{gestureId}"
        };
        
        ProcessGestureData(mockHandData, "手动");
    }
    
    void OnDestroy()
    {
        StopAllHoldCoroutines();
        UnsubscribeFromGestureEvents();
    }
    
    void OnDisable()
    {
        StopAllHoldCoroutines();
        UnsubscribeFromGestureEvents();
    }
    
    // 测试方法
    [ContextMenu("手动触发手势1（直接）")]
    void TestTriggerGesture1()
    {
        ManualTriggerGesture(1);
    }
    
    [ContextMenu("手动触发手势2（直接）")]
    void TestTriggerGesture2()
    {
        ManualTriggerGesture(2);
    }
    
    [ContextMenu("手动触发手势1（支持防误触）")]
    void TestTriggerGesture1WithHold()
    {
        ManualTriggerGestureWithHold(1);
    }
    
    [ContextMenu("手动触发手势2（支持防误触）")]
    void TestTriggerGesture2WithHold()
    {
        ManualTriggerGestureWithHold(2);
    }
    
    [ContextMenu("停止当前手势保持检测")]
    void TestStopCurrentHold()
    {
        StopAllHoldCoroutines();
        currentGestureId = -1;
        if (enableDebugLog)
        {
            Debug.Log("[GestureSignalHandler] 已停止当前手势保持检测");
        }
    }
    
    [ContextMenu("清除所有缓存")]
    void ClearAllCache()
    {
        // 清除防抖动缓存
        lastGestureTriggerTime.Clear();
        
        // 清除防误触缓存
        StopAllHoldCoroutines();
        gestureStartTime.Clear();
        currentGestureId = -1;
        
        if (enableDebugLog)
        {
            Debug.Log("[GestureSignalHandler] 所有缓存已清除（防抖动 + 防误触）");
        }
    }
}