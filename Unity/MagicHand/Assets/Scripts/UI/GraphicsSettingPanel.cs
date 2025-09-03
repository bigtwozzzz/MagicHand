using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 图形设置面板
/// 支持分辨率、全屏模式、VSync、抗锯齿、阴影距离设置
/// 使用 TMP_Dropdown 和 Slider，支持设置保存与恢复
/// </summary>
public class GraphicsSettingsPanel : BasePanel
{
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown fullscreenDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown antiAliasingDropdown;
    [SerializeField] private Slider shadowDistanceSlider;
    [SerializeField] private TextMeshProUGUI shadowDistanceText;
    [SerializeField] private TextMeshProUGUI tipText; // 提示文本
    [SerializeField] private Button resetDefaultButton;
    private Resolution[] resolutions;
    private bool isInitialized = false; // 防止重复初始化

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("[GraphicsSettingsPanel] Awake");

        // 获取控件（按路径查找）
        resolutionDropdown = GetControl<TMP_Dropdown>("Panel/DropdownResolution");
        fullscreenDropdown = GetControl<TMP_Dropdown>("Panel/DropdownFullscreen");
        vsyncToggle = GetControl<Toggle>("Panel/ToggleVSync");
        antiAliasingDropdown = GetControl<TMP_Dropdown>("Panel/DropdownAA");
        shadowDistanceSlider = GetControl<Slider>("Panel/SliderShadowDistance");
        shadowDistanceText = GetControl<TextMeshProUGUI>("Panel/TextShadowValue");
        Button buttonApply = GetControl<Button>("Panel/ButtonApply");
        Button buttonBack = GetControl<Button>("Panel/ButtonBack");
        tipText = GetControl<TextMeshProUGUI>("Panel/TipText");
        resetDefaultButton = GetControl<Button>("Panel/ButtonResetDefault");
        if (resolutionDropdown == null ||
            fullscreenDropdown == null ||
            vsyncToggle == null ||
            antiAliasingDropdown == null ||
            shadowDistanceSlider == null ||
            shadowDistanceText == null ||
                buttonApply == null ||
                buttonBack == null ||
                tipText == null|| 
                resetDefaultButton == null)
        {
            Debug.LogError("[GraphicsSettingsPanel] 控件未找到，请检查路径是否正确！");
        }
    }

    public override void ShowMe()
    {
        base.ShowMe();
        Debug.Log("[GraphicsSettingsPanel] ShowMe");

        // 防止重复初始化
        if (!isInitialized)
        {
            InitResolutions();
            InitFullscreenOptions();
            InitAntiAliasingOptions();
            isInitialized = true;
        }

        LoadSavedSettings(); // 每次显示都加载保存的设置（但不应用）
        UpdateShadowDistanceText((int)shadowDistanceSlider.value);
    }
    /// <summary>
    /// 显示临时提示信息
    /// </summary>
    private void ShowTip(string message)
    {
        if (tipText != null)
        {
            tipText.text = message;
            tipText.gameObject.SetActive(true);

            // 2秒后自动隐藏
            Invoke(nameof(HideTip), 2f);
        }
    }

    /// <summary>
    /// 隐藏提示
    /// </summary>
    private void HideTip()
    {
        if (tipText != null)
        {
            tipText.gameObject.SetActive(false);
        }
    }
    private void InitResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            var res = resolutions[i];
            string optionText = $"{res.width} × {res.height}  ({res.refreshRateRatio.value:0}Hz)";
            options.Add(new TMP_Dropdown.OptionData(optionText));

            // 匹配当前分辨率
            if (Mathf.Approximately(res.width, Screen.currentResolution.width) &&
                Mathf.Approximately(res.height, Screen.currentResolution.height))
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
    }

    private void InitFullscreenOptions()
    {
        if (fullscreenDropdown == null) return;

        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>
        {
            new("窗口化"),
            new("无边框窗口"),
            new("全屏")
        };

        fullscreenDropdown.ClearOptions();
        fullscreenDropdown.AddOptions(options);

        // 当前全屏模式
        int mode = Screen.fullScreenMode switch
        {
            FullScreenMode.Windowed => 0,
            FullScreenMode.FullScreenWindow => 1,
            FullScreenMode.ExclusiveFullScreen => 2,
            _ => 0
        };
        fullscreenDropdown.value = mode;
    }

    private void InitAntiAliasingOptions()
    {
        if (antiAliasingDropdown == null) return;

        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>
        {
            new("关闭 (Off)"),
            new("2x"),
            new("4x"),
            new("8x")
        };

        antiAliasingDropdown.ClearOptions();
        antiAliasingDropdown.AddOptions(options);
    }

    /// <summary>
    /// 从 PlayerPrefs 加载上次保存的设置（不立即应用）
    /// </summary>
    private void LoadSavedSettings()
    {
        // 分辨率
        if (PlayerPrefs.HasKey("ResolutionIndex") &&
            resolutionDropdown != null &&
            PlayerPrefs.GetInt("ResolutionIndex") < resolutionDropdown.options.Count)
        {
            resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex");
        }

        // 全屏模式
        if (PlayerPrefs.HasKey("FullscreenMode") && fullscreenDropdown != null)
        {
            int mode = PlayerPrefs.GetInt("FullscreenMode");
            if (mode >= 0 && mode < 3)
                fullscreenDropdown.value = mode;
        }

        // VSync
        if (PlayerPrefs.HasKey("VSync") && vsyncToggle != null)
        {
            vsyncToggle.isOn = PlayerPrefs.GetInt("VSync") == 1;
        }

        // 阴影距离
        if (PlayerPrefs.HasKey("ShadowDistance") && shadowDistanceSlider != null)
        {
            float value = PlayerPrefs.GetFloat("ShadowDistance");
            shadowDistanceSlider.value = Mathf.Clamp(value, shadowDistanceSlider.minValue, shadowDistanceSlider.maxValue);
        }

        // 抗锯齿
        if (PlayerPrefs.HasKey("AntiAliasing") && antiAliasingDropdown != null)
        {
            int aa = PlayerPrefs.GetInt("AntiAliasing");
            if (aa >= 0 && aa < 4)
                antiAliasingDropdown.value = aa;
        }

        Debug.Log("[GraphicsSettingsPanel] 已加载保存的设置");
    }

    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case "Panel/ButtonBack":
                Debug.Log("返回设置主面板");
                UIMgr.GetInstance().HidePanel("GraphicPanel");
                UIMgr.GetInstance().ShowPanel("SettingsPanel");
                break;

            case "Panel/ButtonApply":
                ApplySettings();
                Debug.Log("图形设置已应用");
                break;
            case "Panel/ButtonResetDefault": // 添加这一段
                ResetToDefault();
                break;
            default:
                base.OnClick(btnName);
                break;
        }
    }
    /// <summary>
    /// 恢复到默认推荐设置（不立即应用，只设置UI）
    /// </summary>
    private void ResetToDefault()
    {
        // 推荐默认值（你可以根据项目需求调整）
        int defaultResolutionIndex = FindBestResolutionIndex(); // 自动选最佳分辨率
        int defaultFullscreenMode = 2; // 全屏
        bool defaultVSync = true;      // 开启 VSync
        int defaultAntiAliasing = 2;   // 4x 抗锯齿
        float defaultShadowDistance = 50f; // 阴影距离 50 米

        // 更新下拉菜单和滑动条（仅UI）
        if (resolutionDropdown != null && defaultResolutionIndex >= 0)
            resolutionDropdown.value = defaultResolutionIndex;

        if (fullscreenDropdown != null)
            fullscreenDropdown.value = defaultFullscreenMode;

        if (vsyncToggle != null)
            vsyncToggle.isOn = defaultVSync;

        if (antiAliasingDropdown != null)
            antiAliasingDropdown.value = defaultAntiAliasing;

        if (shadowDistanceSlider != null)
            shadowDistanceSlider.value = defaultShadowDistance;

        // 更新阴影距离文本
        UpdateShadowDistanceText(Mathf.RoundToInt(defaultShadowDistance));

        // 显示提示
        ShowTip("已恢复默认设置");

        Debug.Log("[GraphicsSettingsPanel] 已恢复默认设置（未应用，请点击应用）");
    }
    /// <summary>
    /// 找到最合适的默认分辨率（通常是当前屏幕的原生分辨率）
    /// </summary>
    private int FindBestResolutionIndex()
    {
        if (resolutions == null || resolutions.Length == 0) return 0;

        // 优先选择当前屏幕的原生分辨率
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                return i;
            }
        }

        // 找不到就返回最后一个（通常是最高分辨率）
        return resolutions.Length - 1;
    }
    private void ApplySettings()
    {
        // 1. 分辨率
        if (resolutionDropdown == null || resolutionDropdown.value >= resolutions.Length)
        {
            Debug.LogWarning("无效的分辨率选项");
            return;
        }

        var targetRes = resolutions[resolutionDropdown.value];
        FullScreenMode fullScreenMode = GetFullScreenMode();

        try
        {
            Screen.SetResolution(targetRes.width, targetRes.height, fullScreenMode);
            Debug.Log($"分辨率已设置: {targetRes.width}x{targetRes.height}, {fullScreenMode}");
        }
        catch (Exception e)
        {
            Debug.LogError("设置分辨率失败: " + e.Message);
        }

        // 2. VSync
        QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;

        // 3. 阴影距离
        if (shadowDistanceSlider != null)
            QualitySettings.shadowDistance = shadowDistanceSlider.value;

        // 4. 抗锯齿
        int aaValue = antiAliasingDropdown.value switch
        {
            0 => 1, // Off
            1 => 2,
            2 => 4,
            3 => 8,
            _ => 1
        };
        QualitySettings.antiAliasing = aaValue;

        // 5. 保存设置
        SaveSettings();

        Debug.Log("[GraphicsSettingsPanel] 所有图形设置已应用");
        ShowTip("设置成功");
    }

    private FullScreenMode GetFullScreenMode()
    {
        return fullscreenDropdown.value switch
        {
            0 => FullScreenMode.Windowed,
            1 => FullScreenMode.FullScreenWindow,
            2 => FullScreenMode.ExclusiveFullScreen,
            _ => Screen.fullScreenMode
        };
    }

    /// <summary>
    /// 滑动条回调（需在 Inspector 中绑定）
    /// </summary>
    public void OnShadowDistanceChanged(float value)
    {
        UpdateShadowDistanceText(Mathf.RoundToInt(value));
    }

    private void UpdateShadowDistanceText(int value)
    {
        if (shadowDistanceText != null)
            shadowDistanceText.text = $"{value}m";
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown?.value ?? 0);
        PlayerPrefs.SetInt("FullscreenMode", fullscreenDropdown?.value ?? 0);
        PlayerPrefs.SetInt("VSync", vsyncToggle?.isOn == true ? 1 : 0);
        PlayerPrefs.SetFloat("ShadowDistance", shadowDistanceSlider?.value ?? 50f);
        PlayerPrefs.SetInt("AntiAliasing", antiAliasingDropdown?.value ?? 0);
        PlayerPrefs.Save();

        Debug.Log("[GraphicsSettingsPanel] 设置已保存到 PlayerPrefs");
    }

    public override void HideMe()
    {
        // 退出前保存设置
        if (isInitialized)
        {
            SaveSettings();
        }
        base.HideMe();
    }

    protected override void OnDestroy()
    {
        isInitialized = false;
    }
}