using UnityEngine;

public class FullScreenManager : MonoBehaviour
{
    // 保存之前的窗口分辨率（用于退出全屏时恢复）
    private Resolution previousResolution;

    void Start()
    {
        // 记录初始的非全屏分辨率
        if (!Screen.fullScreen)
        {
            previousResolution = Screen.currentResolution;
        }
        else
        {
            // 如果是全屏启动，则设置一个默认的窗口分辨率
            // 这里可以自定义你希望的窗口大小，例如1280x720
            previousResolution = new Resolution { width = 1280, height = 720 };
        }
    }

    void Update()
    {
        // 检查是否按下了 Esc 键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleFullScreen();
        }
    }

    private void ToggleFullScreen()
    {
        // 切换全屏状态
        bool willBeFullScreen = !Screen.fullScreen;
        Screen.fullScreen = willBeFullScreen;

        if (willBeFullScreen)
        {
            // 进入全屏前记录当前的窗口分辨率
            if (!Screen.fullScreen)
            {
                previousResolution.width = Screen.width;
                previousResolution.height = Screen.height;
            }
        }
        else
        {
            // 设置为非全屏时，使用之前保存的分辨率
            Screen.SetResolution(previousResolution.width, previousResolution.height, false);
        }
    }
}