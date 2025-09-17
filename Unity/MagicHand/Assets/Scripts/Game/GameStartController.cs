using UnityEngine;

/// <summary>
/// 游戏启动控制器
/// 监听OK手势的OnNextWave事件来启动游戏
/// </summary>
public class GameStartController : MonoBehaviour
{
    [Header("启动配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    private bool gameStarted = false;
    
    void Start()
    {
        // 直接订阅静态事件，无需组件引用
        GeneralGestureHandler.OnNextWaveTriggered += OnNextWaveTriggered;
        
        if (enableDebugLog)
        {
            Debug.Log("[GameStartController] 已订阅OnNextWave事件，等待OK手势启动游戏");
        }
    }
    
    /// <summary>
    /// 处理OnNextWave事件
    /// </summary>
    void OnNextWaveTriggered()
    {
        if (gameStarted)
        {
            if (enableDebugLog)
            {
                Debug.Log("[GameStartController] 游戏已经启动，忽略此次OnNextWave事件");
            }
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[GameStartController] 检测到OK手势，启动游戏！");
        }
        
        StartGame();
    }
    
    /// <summary>
    /// 启动游戏
    /// </summary>
    void StartGame()
    {
        if (gameStarted) return;
        
        gameStarted = true;
        
        // 恢复游戏状态
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResumeGame();
            
            if (enableDebugLog)
            {
                Debug.Log("[GameStartController] 游戏已启动，所有系统开始运行");
            }
        }
        else
        {
            Debug.LogError("[GameStartController] 未找到GameStateManager，无法启动游戏");
        }
    }
    
    /// <summary>
    /// 手动启动游戏（供调试使用）
    /// </summary>
    [ContextMenu("手动启动游戏")]
    public void ManualStartGame()
    {
        if (enableDebugLog)
        {
            Debug.Log("[GameStartController] 手动启动游戏");
        }
        
        StartGame();
    }
    
    /// <summary>
    /// 重置游戏启动状态
    /// </summary>
    [ContextMenu("重置启动状态")]
    public void ResetStartState()
    {
        gameStarted = false;
        
        if (enableDebugLog)
        {
            Debug.Log("[GameStartController] 游戏启动状态已重置");
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        GeneralGestureHandler.OnNextWaveTriggered -= OnNextWaveTriggered;
    }
    
    /// <summary>
    /// 获取游戏是否已启动
    /// </summary>
    /// <returns>是否已启动</returns>
    public bool IsGameStarted()
    {
        return gameStarted;
    }
}