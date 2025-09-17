using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用手势事件处理器
/// 用于处理除魔法手势之外的其他手势事件
/// 包含特殊手势的持续检测和自定义事件触发
/// </summary>
public class GeneralGestureHandler : MonoBehaviour
{
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    [Header("OK手势配置")]
    [SerializeField] private int okGestureId = 13;
    [SerializeField] private float okGestureHoldTime = 1.2f;
    [SerializeField] private string okGestureName = "ok";
    
    [Header("Timeout手势配置")]
    [SerializeField] private int timeoutGestureId = 17;
    [SerializeField] private float timeoutGestureHoldTime = 1.2f;
    [SerializeField] private string timeoutGestureName = "timeout";
    
    [Header("通用手势保持配置")]
    [SerializeField] private float gestureStableTime = 2.0f; // 手势保持稳定的时间
    [SerializeField] private bool enableAutoTrigger = true; // 是否启用自动触发
    
    [Header("手势检测稳定性配置")]
    [SerializeField] private float gestureTimeoutThreshold = 0.6f; // 手势丢失判定阈值（秒）
    [SerializeField] private float statusLogInterval = 0.5f; // 状态日志输出间隔（秒）
    
    [Header("事件配置")]
    public UnityEvent OnNextWave = new UnityEvent();
    public UnityEvent OnPause = new UnityEvent();
    
    // C#事件，供代码订阅使用
    public static event Action OnNextWaveTriggered;
    public static event Action OnPauseTriggered;
    
    // OK手势持续检测相关变量
    private bool isHoldingOkGesture = false;
    private float okGestureStartTime = 0f;
    private float lastOkGestureDetectedTime = 0f; // 最后一次检测到OK手势的时间
    private Coroutine okGestureCoroutine = null;
    
    // Timeout手势持续检测相关变量
    private bool isHoldingTimeoutGesture = false;
    private float timeoutGestureStartTime = 0f;
    private float lastTimeoutGestureDetectedTime = 0f; // 最后一次检测到Timeout手势的时间
    private Coroutine timeoutGestureCoroutine = null;
    
    // 通用手势保持检测相关变量
    private int lastDetectedGesture = -1;
    private float lastGestureChangeTime = 0f;
    private Coroutine gestureStableCoroutine = null;
    
    void Start()
    {
        SubscribeToGestureEvents();
        
        if (enableDebugLog)
        {
            Debug.Log("[GeneralGestureHandler] 通用手势处理器已启动，开始监听手势事件。");
        }
    }
    
    /// <summary>
    /// 订阅手势事件
    /// </summary>
    void SubscribeToGestureEvents()
    {
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
            Debug.Log($"[GeneralGestureHandler] 检测到手势 {gestureId}");
        }
        
        // 处理通用手势保持检测
        HandleGestureStability(gestureId);
        
        // 处理OK手势（13号手势）
        if (gestureId == okGestureId)
        {
            HandleOkGesture();
        }
        // 处理Timeout手势（17号手势）
        else if (gestureId == timeoutGestureId)
        {
            HandleTimeoutGesture();
        }
        else
        {
            // 如果检测到其他手势，停止OK手势和Timeout手势的持续检测
            StopOkGestureDetection();
            StopTimeoutGestureDetection();
        }
    }
    
    /// <summary>
    /// 处理OK手势
    /// </summary>
    void HandleOkGesture()
    {
        // 更新最后一次检测到OK手势的时间（使用不受暂停影响的时间）
        lastOkGestureDetectedTime = Time.unscaledTime;
        
        if (!isHoldingOkGesture)
        {
            // 开始持续检测OK手势
            StartOkGestureDetection();
        }
        else
        {
            // OK手势持续中，更新最后检测时间但不重置开始时间
            // 这样可以让协程继续计算从最初开始的持续时间
            if (enableDebugLog)
            {
                float currentHoldTime = Time.unscaledTime - okGestureStartTime;
                Debug.Log($"[GeneralGestureHandler] OK手势持续中，当前持续时间: {currentHoldTime:F2}秒");
            }
        }
    }
    
    /// <summary>
    /// 处理Timeout手势
    /// </summary>
    void HandleTimeoutGesture()
    {
        // 更新最后一次检测到Timeout手势的时间（使用不受暂停影响的时间）
        lastTimeoutGestureDetectedTime = Time.unscaledTime;
        
        if (!isHoldingTimeoutGesture)
        {
            // 开始持续检测Timeout手势
            StartTimeoutGestureDetection();
        }
        else
        {
            // Timeout手势持续中，更新最后检测时间但不重置开始时间
            // 这样可以让协程继续计算从最初开始的持续时间
            if (enableDebugLog)
            {
                float currentHoldTime = Time.unscaledTime - timeoutGestureStartTime;
                Debug.Log($"[GeneralGestureHandler] Timeout手势持续中，当前持续时间: {currentHoldTime:F2}秒");
            }
        }
    }
    
    /// <summary>
    /// 开始OK手势持续检测
    /// </summary>
    void StartOkGestureDetection()
    {
        isHoldingOkGesture = true;
        okGestureStartTime = Time.unscaledTime;
        lastOkGestureDetectedTime = Time.unscaledTime;
        
        // 停止之前的协程（如果存在）
        if (okGestureCoroutine != null)
        {
            StopCoroutine(okGestureCoroutine);
        }
        
        // 启动新的检测协程
        okGestureCoroutine = StartCoroutine(OkGestureHoldCoroutine());
        
        if (enableDebugLog)
        {
            Debug.Log($"[GeneralGestureHandler] 开始检测OK手势持续时间，需要保持 {okGestureHoldTime} 秒");
        }
    }
    
    /// <summary>
    /// 开始Timeout手势持续检测
    /// </summary>
    void StartTimeoutGestureDetection()
    {
        isHoldingTimeoutGesture = true;
        timeoutGestureStartTime = Time.unscaledTime;
        lastTimeoutGestureDetectedTime = Time.unscaledTime;
        
        // 停止之前的协程（如果存在）
        if (timeoutGestureCoroutine != null)
        {
            StopCoroutine(timeoutGestureCoroutine);
        }
        
        // 启动新的检测协程
        timeoutGestureCoroutine = StartCoroutine(TimeoutGestureHoldCoroutine());
        
        if (enableDebugLog)
        {
            Debug.Log($"[GeneralGestureHandler] 开始检测Timeout手势持续时间，需要保持 {timeoutGestureHoldTime} 秒");
        }
    }
    
    /// <summary>
    /// 停止OK手势持续检测
    /// </summary>
    void StopOkGestureDetection()
    {
        if (isHoldingOkGesture)
        {
            isHoldingOkGesture = false;
            
            if (okGestureCoroutine != null)
            {
                StopCoroutine(okGestureCoroutine);
                okGestureCoroutine = null;
            }
            
            if (enableDebugLog)
            {
                float heldTime = Time.unscaledTime - okGestureStartTime;
                Debug.Log($"[GeneralGestureHandler] OK手势检测停止，持续时间: {heldTime:F2} 秒");
            }
        }
    }
    
    /// <summary>
    /// 停止Timeout手势持续检测
    /// </summary>
    void StopTimeoutGestureDetection()
    {
        if (isHoldingTimeoutGesture)
        {
            isHoldingTimeoutGesture = false;
            
            if (timeoutGestureCoroutine != null)
            {
                StopCoroutine(timeoutGestureCoroutine);
                timeoutGestureCoroutine = null;
            }
            
            if (enableDebugLog)
            {
                float heldTime = Time.unscaledTime - timeoutGestureStartTime;
                Debug.Log($"[GeneralGestureHandler] Timeout手势检测停止，持续时间: {heldTime:F2} 秒");
            }
        }
    }
    
    /// <summary>
    /// OK手势持续检测协程
    /// </summary>
    IEnumerator OkGestureHoldCoroutine()
    {
        float checkInterval = 0.1f; // 每0.1秒检查一次
        
        while (isHoldingOkGesture)
        {
            // 如果游戏暂停，暂停超时检测但继续持续时间计算
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
            {
                // 游戏暂停时，更新最后检测时间以避免超时
                lastOkGestureDetectedTime = Time.unscaledTime;
                
                // 检查是否达到了要求的持续时间（暂停状态下也可以触发）
                float pausedHeldTime = Time.unscaledTime - okGestureStartTime;
                if (pausedHeldTime >= okGestureHoldTime)
                {
                    TriggerNextWaveEvent();
                    StopOkGestureDetection();
                    yield break;
                }
                
                yield return new WaitForSecondsRealtime(checkInterval);
                continue;
            }
            
            float heldTime = Time.unscaledTime - okGestureStartTime;
            float timeSinceLastDetection = Time.unscaledTime - lastOkGestureDetectedTime;
            
            // 检查手势是否丢失（超过阈值时间没有检测到OK手势）
            if (timeSinceLastDetection > gestureTimeoutThreshold)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[GeneralGestureHandler] OK手势丢失，持续时间: {heldTime:F2}秒，最后检测间隔: {timeSinceLastDetection:F2}秒，停止检测");
                }
                StopOkGestureDetection();
                yield break;
            }
            
            // 检查是否达到了要求的持续时间
            if (heldTime >= okGestureHoldTime)
            {
                TriggerNextWaveEvent();
                StopOkGestureDetection();
                yield break;
            }
            
            // 添加调试信息，帮助了解检测状态
            if (enableDebugLog && (int)(heldTime / statusLogInterval) != (int)((heldTime - checkInterval) / statusLogInterval))
            {
                Debug.Log($"[GeneralGestureHandler] OK手势检测中，持续时间: {heldTime:F2}秒，距离上次检测: {timeSinceLastDetection:F2}秒");
            }
            
            // 使用不受暂停影响的等待
            yield return new WaitForSecondsRealtime(checkInterval);
        }
    }
    
    /// <summary>
    /// Timeout手势持续检测协程
    /// </summary>
    IEnumerator TimeoutGestureHoldCoroutine()
    {
        float checkInterval = 0.1f; // 每0.1秒检查一次
        
        while (isHoldingTimeoutGesture)
        {
            // 如果游戏暂停，暂停超时检测但继续持续时间计算
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
            {
                // 游戏暂停时，更新最后检测时间以避免超时
                lastTimeoutGestureDetectedTime = Time.unscaledTime;
                
                // 检查是否达到了要求的持续时间（暂停状态下也可以触发）
                float pausedHeldTime = Time.unscaledTime - timeoutGestureStartTime;
                if (pausedHeldTime >= timeoutGestureHoldTime)
                {
                    TriggerPauseEvent();
                    StopTimeoutGestureDetection();
                    yield break;
                }
                
                yield return new WaitForSecondsRealtime(checkInterval);
                continue;
            }
            
            float heldTime = Time.unscaledTime - timeoutGestureStartTime;
            float timeSinceLastDetection = Time.unscaledTime - lastTimeoutGestureDetectedTime;
            
            // 检查手势是否丢失（超过阈值时间没有检测到Timeout手势）
            if (timeSinceLastDetection > gestureTimeoutThreshold)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[GeneralGestureHandler] Timeout手势丢失，持续时间: {heldTime:F2}秒，最后检测间隔: {timeSinceLastDetection:F2}秒，停止检测");
                }
                StopTimeoutGestureDetection();
                yield break;
            }
            
            // 检查是否达到了要求的持续时间
            if (heldTime >= timeoutGestureHoldTime)
            {
                TriggerPauseEvent();
                StopTimeoutGestureDetection();
                yield break;
            }
            
            // 添加调试信息，帮助了解检测状态
            if (enableDebugLog && (int)(heldTime / statusLogInterval) != (int)((heldTime - checkInterval) / statusLogInterval))
            {
                Debug.Log($"[GeneralGestureHandler] Timeout手势检测中，持续时间: {heldTime:F2}秒，距离上次检测: {timeSinceLastDetection:F2}秒");
            }
            
            // 使用不受暂停影响的等待
            yield return new WaitForSecondsRealtime(checkInterval);
        }
    }
    
    /// <summary>
    /// 触发NextWave事件
    /// </summary>
    void TriggerNextWaveEvent()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GeneralGestureHandler] OK手势持续 {okGestureHoldTime} 秒，触发 OnNextWave 事件！");
        }
        
        // 触发UnityEvent
        OnNextWave?.Invoke();
        
        // 触发C#事件
        OnNextWaveTriggered?.Invoke();
        
        // 恢复游戏（如果游戏处于暂停状态）
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
        {
            GameStateManager.Instance.ResumeGame();
        }
    }
    
    /// <summary>
    /// 触发Pause事件
    /// </summary>
    void TriggerPauseEvent()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GeneralGestureHandler] Timeout手势持续 {timeoutGestureHoldTime} 秒，触发 OnPause 事件！");
        }
        
        // 触发UnityEvent
        OnPause?.Invoke();
        
        // 触发C#事件
        OnPauseTriggered?.Invoke();
        
        // 暂停游戏（只有在游戏未暂停时才暂停）
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPaused)
        {
            GameStateManager.Instance.PauseGame();
        }
    }
    
    /// <summary>
    /// 静态方法：订阅NextWave事件
    /// </summary>
    /// <param name="callback">回调函数</param>
    public static void SubscribeToNextWave(Action callback)
    {
        OnNextWaveTriggered += callback;
    }
    
    /// <summary>
    /// 静态方法：取消订阅NextWave事件
    /// </summary>
    /// <param name="callback">回调函数</param>
    public static void UnsubscribeFromNextWave(Action callback)
    {
        OnNextWaveTriggered -= callback;
    }
    
    /// <summary>
    /// 静态方法：订阅Pause事件
    /// </summary>
    /// <param name="callback">回调函数</param>
    public static void SubscribeToPause(Action callback)
    {
        OnPauseTriggered += callback;
    }
    
    /// <summary>
    /// 静态方法：取消订阅Pause事件
    /// </summary>
    /// <param name="callback">回调函数</param>
    public static void UnsubscribeFromPause(Action callback)
    {
        OnPauseTriggered -= callback;
    }
    
    /// <summary>
    /// 获取当前OK手势持续时间
    /// </summary>
    /// <returns>持续时间（秒）</returns>
    public float GetCurrentOkGestureHoldTime()
    {
        if (isHoldingOkGesture)
        {
            return Time.time - okGestureStartTime;
        }
        return 0f;
    }
    
    /// <summary>
    /// 获取当前Timeout手势持续时间
    /// </summary>
    /// <returns>持续时间（秒）</returns>
    public float GetCurrentTimeoutGestureHoldTime()
    {
        if (isHoldingTimeoutGesture)
        {
            return Time.time - timeoutGestureStartTime;
        }
        return 0f;
    }
    
    /// <summary>
    /// 检查是否正在持续OK手势
    /// </summary>
    /// <returns>是否正在持续</returns>
    public bool IsHoldingOkGesture()
    {
        return isHoldingOkGesture;
    }
    
    /// <summary>
    /// 检查是否正在持续Timeout手势
    /// </summary>
    /// <returns>是否正在持续</returns>
    public bool IsHoldingTimeoutGesture()
    {
        return isHoldingTimeoutGesture;
    }
    
    /// <summary>
    /// 处理手势稳定性检测
    /// </summary>
    /// <param name="gestureId">当前检测到的手势ID</param>
    void HandleGestureStability(int gestureId)
    {
        if (!enableAutoTrigger) return;
        
        // 如果手势发生变化
        if (gestureId != lastDetectedGesture)
        {
            lastDetectedGesture = gestureId;
            lastGestureChangeTime = Time.time;
            
            // 停止之前的稳定检测协程
            if (gestureStableCoroutine != null)
            {
                StopCoroutine(gestureStableCoroutine);
            }
            
            // 只对OK手势启动稳定检测（避免与现有OK手势逻辑冲突）
            if (gestureId == okGestureId)
            {
                gestureStableCoroutine = StartCoroutine(GestureStabilityCoroutine(gestureId));
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"[GeneralGestureHandler] 手势变化: {lastDetectedGesture} -> {gestureId}, 开始稳定性检测");
            }
        }
        else
        {
            // 手势保持不变，重置时间
            lastGestureChangeTime = Time.time;
        }
    }
    
    /// <summary>
    /// 手势稳定性检测协程
    /// </summary>
    /// <param name="gestureId">要检测的手势ID</param>
    IEnumerator GestureStabilityCoroutine(int gestureId)
    {
        float checkInterval = 0.1f;
        
        while (true)
        {
            float stableTime = Time.time - lastGestureChangeTime;
            
            // 如果手势保持稳定超过设定时间，且是OK手势
            if (stableTime >= gestureStableTime && gestureId == okGestureId)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[GeneralGestureHandler] 手势 {gestureId} 保持稳定 {gestureStableTime} 秒，自动触发事件");
                }
                
                // 触发事件（复用现有的OK手势事件）
                TriggerNextWaveEvent();
                
                // 重置检测，避免重复触发
                lastGestureChangeTime = Time.time;
                yield break;
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromGestureEvents();
        
        // 停止所有协程
        if (okGestureCoroutine != null)
        {
            StopCoroutine(okGestureCoroutine);
        }
        
        if (gestureStableCoroutine != null)
        {
            StopCoroutine(gestureStableCoroutine);
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[GeneralGestureHandler] 通用手势处理器已销毁，取消所有事件订阅。");
        }
    }
    
    void OnDisable()
    {
        UnsubscribeFromGestureEvents();
        StopOkGestureDetection();
    }
    
    // 测试方法，可在Inspector中调用
    [ContextMenu("测试触发NextWave事件")]
    void TestTriggerNextWave()
    {
        TriggerNextWaveEvent();
    }
    
    [ContextMenu("测试OK手势检测")]
    void TestOkGestureDetection()
    {
        HandleOkGesture();
    }
}