using System;
using UnityEngine;

/// <summary>
/// 游戏状态管理器
/// 控制游戏的暂停/恢复状态，管理Time.timeScale和自定义暂停逻辑
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("游戏状态配置")]
    [SerializeField] private bool isPaused = true; // 游戏开始时默认暂停
    [SerializeField] private bool startPaused = true; // 是否在游戏开始时暂停
    [SerializeField] private float previousTimeScale = 1f;
    
    // 单例实例
    private static GameStateManager _instance;
    public static GameStateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameStateManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameStateManager");
                    _instance = go.AddComponent<GameStateManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    // 游戏状态事件
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    
    // 属性
    public bool IsPaused => isPaused;
    public float CurrentTimeScale => Time.timeScale;
    
    private void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 如果设置为开始时暂停，则初始化为暂停状态
            if (startPaused)
            {
                InitializePausedState();
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化
        previousTimeScale = Time.timeScale;
    }
    
    private void Start()
    {
        Debug.Log("[GameStateManager] 游戏状态管理器已初始化");
    }
    
    /// <summary>
    /// 初始化暂停状态
    /// </summary>
    private void InitializePausedState()
    {
        Debug.Log("[GameStateManager] 初始化为暂停状态，等待OK手势启动游戏");
        
        // 保存当前时间缩放，确保不为0
        if (Time.timeScale <= 0f)
        {
            previousTimeScale = 1f; // 如果当前时间缩放异常，设置为默认值
        }
        else
        {
            previousTimeScale = Time.timeScale;
        }
        
        // 设置时间缩放为0（暂停物理、动画、协程等）
        Time.timeScale = 0f;
        
        // 设置暂停状态
        isPaused = true;
        
        // 触发暂停事件
        OnGamePaused?.Invoke();
    }
    
    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;
        
        Debug.Log("[GameStateManager] 暂停游戏");
        
        // 保存当前时间缩放
        previousTimeScale = Time.timeScale;
        
        // 设置时间缩放为0（暂停物理、动画、协程等）
        Time.timeScale = 0f;
        
        // 设置暂停状态
        isPaused = true;
        
        // 触发暂停事件
        OnGamePaused?.Invoke();
    }
    
    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        Debug.Log("[GameStateManager] 恢复游戏");
        
        // 恢复时间缩放，确保不为0
        if (previousTimeScale <= 0f)
        {
            previousTimeScale = 1f; // 默认正常速度
            Debug.Log("[GameStateManager] 检测到异常的时间缩放值，重置为1.0");
        }
        Time.timeScale = previousTimeScale;
        
        // 设置运行状态
        isPaused = false;
        
        // 触发恢复事件
        OnGameResumed?.Invoke();
    }
    
    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    /// <summary>
    /// 设置自定义时间缩放（不影响暂停状态）
    /// </summary>
    /// <param name="timeScale">时间缩放值</param>
    public void SetTimeScale(float timeScale)
    {
        if (!isPaused)
        {
            Time.timeScale = timeScale;
            previousTimeScale = timeScale;
            Debug.Log($"[GameStateManager] 设置时间缩放: {timeScale}");
        }
        else
        {
            previousTimeScale = timeScale;
            Debug.Log($"[GameStateManager] 游戏暂停中，保存时间缩放: {timeScale}");
        }
    }
    
    /// <summary>
    /// 获取实时时间（不受Time.timeScale影响）
    /// </summary>
    /// <returns>实时时间</returns>
    public float GetRealTime()
    {
        return Time.realtimeSinceStartup;
    }
    
    /// <summary>
    /// 获取实时增量时间（不受Time.timeScale影响）
    /// </summary>
    /// <returns>实时增量时间</returns>
    public float GetRealDeltaTime()
    {
        return Time.unscaledDeltaTime;
    }
    
    private void Update()
    {
        // 测试用：按ESC键切换暂停
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    // 调试信息
    private void OnGUI()
    {
        if (Application.isEditor)
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"游戏状态: {(isPaused ? "暂停" : "运行")}");
            GUILayout.Label($"时间缩放: {Time.timeScale:F2}");
            GUILayout.Label($"实时时间: {Time.realtimeSinceStartup:F2}");
            
            if (GUILayout.Button(isPaused ? "恢复游戏" : "暂停游戏"))
            {
                TogglePause();
            }
            GUILayout.EndArea();
        }
    }
}