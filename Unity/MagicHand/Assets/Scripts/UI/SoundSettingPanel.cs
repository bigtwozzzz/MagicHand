using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 音效设置面板
/// 支持背景音乐与音效音量调节，支持实时预览
/// </summary>
public class SoundSettingsPanel : BasePanel
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI soundValueText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private Button playTestButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;

    protected override void Awake()
    {
        base.Awake();

        // 获取控件
        bgmSlider = GetControl<Slider>("Panel/SliderBGM");
        soundSlider = GetControl<Slider>("Panel/SliderSound");
        bgmValueText = GetControl<TextMeshProUGUI>("Panel/TextBGMValue");
        soundValueText = GetControl<TextMeshProUGUI>("Panel/TextSoundValue");
        playTestButton = GetControl<Button>("Panel/ButtonPlayTest");
        applyButton = GetControl<Button>("Panel/ButtonApply");
        backButton = GetControl<Button>("Panel/ButtonBack");
        tipText = GetControl<TextMeshProUGUI>("Panel/TipText");
        if (bgmSlider == null || soundSlider == null ||
            bgmValueText == null || soundValueText == null ||
            playTestButton == null || applyButton == null || backButton == null
            || tipText == null)
        {
            Debug.LogError("[SoundSettingsPanel] 控件未找到！");
            return;
        }
    }

    public override void ShowMe()
    {
        base.ShowMe();

        // 加载当前音量
        float currentBgm = MusicMgr.GetInstance().BkValue;
        float currentSound = MusicMgr.GetInstance().SoundValue;

        bgmSlider.value = currentBgm;
        soundSlider.value = currentSound;

        UpdateBGMText(currentBgm);
        UpdateSoundText(currentSound);

        // 绑定事件（如果还没绑定）
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        soundSlider.onValueChanged.AddListener(OnSoundChanged);
        playTestButton.onClick.AddListener(OnPlayTestClick);
        applyButton.onClick.AddListener(OnApplyClick);
        backButton.onClick.AddListener(OnBackClick);
    }

    public override void HideMe()
    {
        // 移除监听（防止重复绑定）
        bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);
        soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
        playTestButton.onClick.RemoveListener(OnPlayTestClick);
        applyButton.onClick.RemoveListener(OnApplyClick);
        backButton.onClick.RemoveListener(OnBackClick);

        base.HideMe();
    }

    private void OnBGMChanged(float value)
    {
        MusicMgr.GetInstance().ChangeBKValue(value);
        UpdateBGMText(value);
    }

    private void OnSoundChanged(float value)
    {
        MusicMgr.GetInstance().ChangeSoundValue(value);
        UpdateSoundText(value);
    }

    private void OnPlayTestClick()
    {
        MusicMgr.GetInstance().PlayTestSound();
    }

    private void OnApplyClick()
    {
        ShowTip("设置成功");
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
    private void OnBackClick()
    {
        UIMgr.GetInstance().HidePanel("SoundPanel");
        UIMgr.GetInstance().ShowPanel("SettingsPanel");
    }

    private void UpdateBGMText(float value)
    {
        bgmValueText.text = $"{(value * 100):0}%";
    }

    private void UpdateSoundText(float value)
    {
        soundValueText.text = $"{(value * 100):0}%";
    }
}