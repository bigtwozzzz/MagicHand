using UnityEngine;
using UnityEngine.UI;
//// 还是没写对
/// <summary>
/// 全屏管理器：启动默认小窗口，登录前可切换，登录后锁定为全屏/最大化模式
/// 功能：
/// - 启动：默认小窗口 (1280x720)
/// - 登录前：最大化按钮 切换 小窗  最大化；Alt+Enter 切换 最大化  全屏
/// - 登录后：Alt+Enter 切换 最大化  全屏；禁止小窗口
/// 
/// 注意：自动单例管理，无需手动挂载多个实例
/// </summary>
public class FullScreenManager : MonoBehaviour
{
    // 默认窗口大小
    private const int DEFAULT_WIDTH = 1280;
    private const int DEFAULT_HEIGHT = 720;

    // 单例实例
    public static FullScreenManager Instance { get; private set; }

    [Header("UI References")]
    public Button maximizeButton; // 登录后可禁用

    private WindowMode currentMode = WindowMode.Windowed;
    private bool isLoginCompleted = false;

    // 窗口模式（internal：仅本程序集可用，不暴露给外部插件）
    internal enum WindowMode
    {
        Windowed,     // 小窗口
        Maximized,    // 最大化（无边框）
        FullScreen    // 全屏（独占）
    }

    private void Awake()
    {
        // 单例逻辑：确保只有一个实例
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[FullScreenManager] 发现重复实例，销毁 GameObject: {name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 跨场景保留

        // 初始化为小窗口
        SetWindowedMode();
        UpdateMaximizeButtonState();
        Debug.Log($"[FullScreenManager] 初始化完成，当前模式：小窗口 ({DEFAULT_WIDTH}x{DEFAULT_HEIGHT})");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Reset()
    {
        // 编辑器 Reset 时清空引用提示
        maximizeButton = null;
    }

    private void Update()
    {
        // Alt + Enter：切换 全屏  最大化
        if (Input.GetKeyDown(KeyCode.Return) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            ToggleFullScreenMaximized();
        }
    }

    /// <summary>
    /// 外部调用：标记登录已完成，禁止小窗口模式
    /// </summary>
    public void SetLoginCompleted()
    {
        isLoginCompleted = true;
        UpdateMaximizeButtonState();
        Debug.Log("[FullScreenManager] 登录完成，已锁定为全屏/最大化模式，禁止小窗口。");
    }

    /// <summary>
    /// 外部调用：设置为小窗口（仅登录前有效）
    /// </summary>
    public void SetWindowedMode()
    {
        if (isLoginCompleted)
        {
            Debug.LogWarning("[FullScreenManager] 登录后禁止切换到小窗口模式！");
            return;
        }

        Screen.SetResolution(DEFAULT_WIDTH, DEFAULT_HEIGHT, false);
        currentMode = WindowMode.Windowed;
        UpdateMaximizeButtonState();
        Debug.Log($"[FullScreenManager] 已切换到小窗口模式 ({DEFAULT_WIDTH}x{DEFAULT_HEIGHT})");
    }

    /// <summary>
    /// 外部调用：最大化按钮点击（仅登录前有效）
    /// </summary>
    public void OnMaximizeButtonClicked()
    {
        if (isLoginCompleted)
        {
            Debug.LogWarning("[FullScreenManager] 登录后禁止使用最大化按钮切换小窗口！");
            return;
        }

        if (currentMode == WindowMode.Windowed)
        {
            SetMaximizedMode();
        }
        else
        {
            SetWindowedMode();
        }
    }

    /// <summary>
    /// 设置为最大化模式（无边框）
    /// </summary>
    public void SetMaximizedMode()
    {
        Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
        Screen.fullScreen = true;
        currentMode = WindowMode.Maximized;
        UpdateMaximizeButtonState();
        Debug.Log("[FullScreenManager] 已切换到最大化窗口模式");
    }

    /// <summary>
    /// 设置为全屏模式（独占）
    /// </summary>
    public void SetFullScreenMode()
    {
        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        Screen.fullScreen = true;
        currentMode = WindowMode.FullScreen;
        UpdateMaximizeButtonState();
        Debug.Log("[FullScreenManager] 已切换到全屏模式");
    }

    /// <summary>
    /// 切换 全屏  最大化
    /// </summary>
    public void ToggleFullScreenMaximized()
    {
        if (currentMode == WindowMode.FullScreen)
        {
            SetMaximizedMode();
        }
        else
        {
            SetFullScreenMode();
        }
    }

    /// <summary>
    /// 更新最大化按钮状态
    /// </summary>
    private void UpdateMaximizeButtonState()
    {
        if (maximizeButton == null) return;

        maximizeButton.interactable = !isLoginCompleted;
        // 或者隐藏：maximizeButton.gameObject.SetActive(!isLoginCompleted);
    }

    // ---------------- 状态查询属性 ---------------- //

    public bool IsFullScreen => currentMode == WindowMode.FullScreen;
    public bool IsMaximized => currentMode == WindowMode.Maximized;
    public bool IsWindowed => currentMode == WindowMode.Windowed;
}