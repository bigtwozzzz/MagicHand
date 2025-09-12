using UnityEngine;
using System.Collections;

/// <summary>
/// 魔法特效自动回收组件
/// 在指定时间后自动将特效回收到对象池
/// </summary>
public class MagicEffectAutoRecycle : MonoBehaviour
{
    [Header("回收配置")]
    [Tooltip("魔法编号")]
    public int magicId;
    
    [Tooltip("自动回收时间（秒）")]
    public float recycleTime = 3f;
    
    [Tooltip("是否在启用时开始计时")]
    public bool startOnEnable = true;
    
    [Header("调试配置")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = false;
    
    // 私有变量
    private Coroutine recycleCoroutine;
    private bool isInitialized = false;
    
    // 事件
    public System.Action<GameObject> OnBeforeRecycle; // 回收前事件
    
    void Awake()
    {
        // 确保组件不会在编辑器模式下运行
        if (!Application.isPlaying)
        {
            return;
        }
    }
    
    void OnEnable()
    {
        if (!Application.isPlaying || !isInitialized)
        {
            return;
        }
        
        if (startOnEnable)
        {
            StartRecycleTimer();
        }
    }
    
    void OnDisable()
    {
        StopRecycleTimer();
    }
    
    /// <summary>
    /// 初始化自动回收组件
    /// </summary>
    /// <param name="magicId">魔法编号</param>
    /// <param name="recycleTime">回收时间</param>
    public void Initialize(int magicId, float recycleTime)
    {
        this.magicId = magicId;
        this.recycleTime = recycleTime;
        this.isInitialized = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectAutoRecycle] 初始化自动回收组件，魔法ID: {magicId}，回收时间: {recycleTime}s");
        }
    }
    
    /// <summary>
    /// 开始回收计时器
    /// </summary>
    public void StartRecycleTimer()
    {
        if (!isInitialized)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[MagicEffectAutoRecycle] 组件未初始化，无法开始计时器");
            }
            return;
        }
        
        // 停止之前的计时器
        StopRecycleTimer();
        
        // 开始新的计时器
        recycleCoroutine = StartCoroutine(RecycleCoroutine());
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectAutoRecycle] 开始回收计时器，{recycleTime}秒后回收");
        }
    }
    
    /// <summary>
    /// 停止回收计时器
    /// </summary>
    public void StopRecycleTimer()
    {
        if (recycleCoroutine != null)
        {
            StopCoroutine(recycleCoroutine);
            recycleCoroutine = null;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicEffectAutoRecycle] 停止回收计时器");
            }
        }
    }
    
    /// <summary>
    /// 重置回收计时器
    /// </summary>
    public void ResetRecycleTimer()
    {
        if (isInitialized)
        {
            StartRecycleTimer();
        }
    }
    
    /// <summary>
    /// 设置新的回收时间并重启计时器
    /// </summary>
    /// <param name="newRecycleTime">新的回收时间</param>
    public void SetRecycleTime(float newRecycleTime)
    {
        this.recycleTime = newRecycleTime;
        
        if (recycleCoroutine != null)
        {
            StartRecycleTimer(); // 重启计时器
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectAutoRecycle] 设置新的回收时间: {newRecycleTime}s");
        }
    }
    
    /// <summary>
    /// 立即回收特效
    /// </summary>
    public void RecycleNow()
    {
        StopRecycleTimer();
        RecycleEffect();
    }
    
    /// <summary>
    /// 回收协程
    /// </summary>
    /// <returns>协程</returns>
    IEnumerator RecycleCoroutine()
    {
        yield return new WaitForSeconds(recycleTime);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicEffectAutoRecycle] 回收时间到，开始回收特效");
        }
        
        RecycleEffect();
    }
    
    /// <summary>
    /// 回收特效
    /// </summary>
    void RecycleEffect()
    {
        // 触发回收前事件
        OnBeforeRecycle?.Invoke(gameObject);
        
        // 通知对象池回收此特效
        if (MagicEffectPool.Instance != null)
        {
            MagicEffectPool.Instance.RecycleEffect(gameObject);
        }
        else
        {
            // 如果对象池不存在，直接禁用对象
            gameObject.SetActive(false);
            
            if (enableDebugLog)
            {
                Debug.LogWarning("[MagicEffectAutoRecycle] 对象池不存在，直接禁用特效对象");
            }
        }
    }
    
    /// <summary>
    /// 获取剩余回收时间
    /// </summary>
    /// <returns>剩余时间（秒）</returns>
    public float GetRemainingTime()
    {
        if (recycleCoroutine == null)
        {
            return 0f;
        }
        
        // 这里返回一个估算值，实际实现可能需要更精确的计时
        return recycleTime;
    }
    
    /// <summary>
    /// 检查是否正在计时
    /// </summary>
    /// <returns>是否正在计时</returns>
    public bool IsTimerRunning()
    {
        return recycleCoroutine != null;
    }
    
    /// <summary>
    /// 暂停回收计时器
    /// </summary>
    public void PauseTimer()
    {
        if (recycleCoroutine != null)
        {
            StopCoroutine(recycleCoroutine);
            // 注意：这里简化处理，实际项目中可能需要记录暂停时的剩余时间
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicEffectAutoRecycle] 暂停回收计时器");
            }
        }
    }
    
    /// <summary>
    /// 恢复回收计时器
    /// </summary>
    public void ResumeTimer()
    {
        if (recycleCoroutine == null && isInitialized)
        {
            StartRecycleTimer();
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicEffectAutoRecycle] 恢复回收计时器");
            }
        }
    }
    
    void OnDestroy()
    {
        StopRecycleTimer();
    }
    
    // 测试方法
    [ContextMenu("测试立即回收")]
    void TestRecycleNow()
    {
        RecycleNow();
    }
    
    [ContextMenu("测试重置计时器")]
    void TestResetTimer()
    {
        ResetRecycleTimer();
    }
    
    [ContextMenu("测试暂停计时器")]
    void TestPauseTimer()
    {
        PauseTimer();
    }
    
    [ContextMenu("测试恢复计时器")]
    void TestResumeTimer()
    {
        ResumeTimer();
    }
}