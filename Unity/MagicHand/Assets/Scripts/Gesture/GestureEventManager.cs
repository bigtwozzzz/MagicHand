using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 手势事件管理器
/// 定义手势编号事件OnGesture，用于统一管理手势事件的触发和监听
/// </summary>
public class GestureEventManager : MonoBehaviour
{
    [Header("手势事件配置")]
    public bool enableDebugLog = true;
    
    // 手势事件 - 包含手势编号的UnityEvent
    [System.Serializable]
    public class GestureEvent : UnityEvent<int> { }
    
    [Header("手势事件")]
    public GestureEvent OnGesture = new GestureEvent();
    
    // C#事件，供代码订阅使用
    public static event Action<int> OnGestureDetected;
    
    // 单例模式，方便全局访问
    private static GestureEventManager _instance;
    public static GestureEventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GestureEventManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GestureEventManager");
                    _instance = go.AddComponent<GestureEventManager>();
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
    /// 触发手势事件
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    public void TriggerGestureEvent(int gestureId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GestureEventManager] 触发手势事件，编号: {gestureId}");
        }
        
        // 触发UnityEvent
        OnGesture?.Invoke(gestureId);
        
        // 触发C#事件
        OnGestureDetected?.Invoke(gestureId);
    }
    
    /// <summary>
    /// 静态方法，方便外部调用
    /// </summary>
    /// <param name="gestureId">手势编号</param>
    public static void TriggerGesture(int gestureId)
    {
        Instance.TriggerGestureEvent(gestureId);
    }
    
    /// <summary>
    /// 订阅手势事件（代码方式）
    /// </summary>
    /// <param name="callback">回调函数</param>
    public static void SubscribeToGesture(Action<int> callback)
    {
        OnGestureDetected += callback;
    }
    
    /// <summary>
    /// 取消订阅手势事件（代码方式）
    /// </summary>
    /// <param name="callback">回调函数</param>
    public static void UnsubscribeFromGesture(Action<int> callback)
    {
        OnGestureDetected -= callback;
    }
    
    void OnDestroy()
    {
        // 清理所有事件订阅
        OnGestureDetected = null;
    }
    
    // 测试方法，可在Inspector中调用
    [ContextMenu("测试触发手势1")]
    void TestTriggerGesture1()
    {
        TriggerGestureEvent(1);
    }
    
    [ContextMenu("测试触发手势2")]
    void TestTriggerGesture2()
    {
        TriggerGestureEvent(2);
    }
    
    [ContextMenu("测试触发手势3(one)")]
    void TestTriggerGesture3()
    {
        // 手势3现在对应one手势
        TriggerGestureEvent(3);
    }
}