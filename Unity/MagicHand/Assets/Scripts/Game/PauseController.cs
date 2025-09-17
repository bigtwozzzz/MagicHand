using UnityEngine;

/// <summary>
/// 游戏暂停控制器
/// 提供简单的按键控制游戏暂停/恢复功能
/// </summary>
public class PauseController : MonoBehaviour
{
    [Header("控制配置")]
    [SerializeField] private KeyCode pauseKey = KeyCode.P; // 暂停按键
    [SerializeField] private KeyCode escapeKey = KeyCode.Escape; // ESC键也可以暂停
    [SerializeField] private bool allowPauseBeforeGameStart = false; // 是否允许在游戏启动前暂停
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    private GameStartController gameStartController;
    
    void Start()
    {
        // 获取游戏启动控制器引用
        gameStartController = FindObjectOfType<GameStartController>();
    }
    
    void Update()
    {
        // 检测暂停按键
        if (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(escapeKey))
        {
            // 检查是否允许在游戏启动前暂停
            if (!allowPauseBeforeGameStart && gameStartController != null && !gameStartController.IsGameStarted())
            {
                if (enableDebugLog)
                {
                    Debug.Log("[PauseController] 游戏尚未启动，暂停功能被禁用。请先用OK手势启动游戏。");
                }
                return;
            }
            
            TogglePause();
        }
    }
    
    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (GameStateManager.Instance.IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        GameStateManager.Instance.PauseGame();
        
        if (enableDebugLog)
        {
            Debug.Log("[PauseController] 游戏已暂停");
        }
    }
    
    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        GameStateManager.Instance.ResumeGame();
        
        if (enableDebugLog)
        {
            Debug.Log("[PauseController] 游戏已恢复");
        }
    }
    
    /// <summary>
    /// 获取当前暂停状态
    /// </summary>
    /// <returns>是否暂停</returns>
    public bool IsPaused()
    {
        return GameStateManager.Instance.IsPaused;
    }
}