using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 魔法UI管理器
/// 负责处理魔法相关的UI显示，包括冷却提示等
/// </summary>
public class MagicUIMgr : MonoBehaviour
{
    [Header("冷却提示UI")]
    [SerializeField] private GameObject cooldownTipPanel;
    [SerializeField] private TextMeshProUGUI cooldownTipText;
    [SerializeField] private float tipDisplayDuration = 2f;
    
    // 单例实例
    public static MagicUIMgr Instance { get; private set; }
    
    // 当前显示的协程
    private Coroutine currentTipCoroutine;
    
    // 魔法释放后的保护时间（防止误触冷却提示）
    private Dictionary<int, float> magicCastProtectionTime = new Dictionary<int, float>();
    private const float PROTECTION_DURATION = 1f; // 1秒保护时间
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 订阅魔法冷却事件
        MagicEventSystem.OnMagicCooldownStart += OnMagicCooldownStart;
        // 订阅魔法触发事件
        MagicEventSystem.OnMagicTriggered += OnMagicTriggered;
        
        // 初始化UI状态
        if (cooldownTipPanel != null)
        {
            cooldownTipPanel.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅
        MagicEventSystem.OnMagicCooldownStart -= OnMagicCooldownStart;
        MagicEventSystem.OnMagicTriggered -= OnMagicTriggered;
    }
    
    /// <summary>
    /// 魔法触发时的处理
    /// </summary>
    private void OnMagicTriggered(int magicId, MagicData magicData, int playerId)
    {
        // 记录魔法释放时间，用于保护期判断
        magicCastProtectionTime[magicId] = Time.time;
    }
    
    /// <summary>
    /// 魔法开始冷却时的处理
    /// </summary>
    private void OnMagicCooldownStart(int magicId, float cooldownTime)
    {
        // 检查是否在保护期内
        if (magicCastProtectionTime.ContainsKey(magicId))
        {
            float timeSinceCast = Time.time - magicCastProtectionTime[magicId];
            if (timeSinceCast < PROTECTION_DURATION)
            {
                Debug.Log($"[MagicUIMgr] 魔法{magicId}在保护期内({timeSinceCast:F2}s < {PROTECTION_DURATION}s)，跳过冷却提示");
                return;
            }
        }
        
        ShowCooldownTip(magicId, cooldownTime);
    }
    
    /// <summary>
    /// 显示冷却提示
    /// </summary>
    public void ShowCooldownTip(int magicId, float remainingTime)
    {
        if (cooldownTipPanel == null || cooldownTipText == null)
        {
            Debug.LogWarning("[MagicUIMgr] 冷却提示UI组件未配置");
            return;
        }
        
        // 获取魔法名称
        string magicName = GetMagicName(magicId);
        
        // 更新提示文本
        string tipMessage = $"{magicName}魔法冷却中，剩余{remainingTime:F1}秒";
        if (cooldownTipText != null)
        {
            cooldownTipText.text = tipMessage;
            // 确保文本组件被激活
            cooldownTipText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("[MagicUIMgr] cooldownTipText组件为空，无法显示文本");
        }
        
        // 显示提示面板
        cooldownTipPanel.SetActive(true);
        
        // 停止之前的协程
        if (currentTipCoroutine != null)
        {
            StopCoroutine(currentTipCoroutine);
        }
        
        // 启动新的隐藏协程
        currentTipCoroutine = StartCoroutine(HideTipAfterDelay());
        
        Debug.Log($"[MagicUIMgr] 显示冷却提示: {tipMessage}");
    }
    
    /// <summary>
    /// 延迟隐藏提示
    /// </summary>
    private IEnumerator HideTipAfterDelay()
    {
        yield return new WaitForSeconds(tipDisplayDuration);
        
        if (cooldownTipPanel != null)
        {
            cooldownTipPanel.SetActive(false);
        }
        
        currentTipCoroutine = null;
    }
    
    /// <summary>
    /// 获取魔法名称
    /// </summary>
    private string GetMagicName(int magicId)
    {
        if (MagicConfigLoader.Instance != null)
        {
            MagicData magicData = MagicConfigLoader.Instance.GetMagicData(magicId);
            if (magicData != null && !string.IsNullOrEmpty(magicData.magicName))
            {
                return magicData.magicName;
            }
        }
        
        return $"魔法{magicId}";
    }
    
    /// <summary>
    /// 手动显示冷却提示（供外部调用）
    /// </summary>
    public void ShowManualCooldownTip(int magicId)
    {
        if (MagicCooldown.Instance != null && MagicCooldown.Instance.IsOnCooldown(magicId))
        {
            float remainingTime = MagicCooldown.Instance.GetRemainingCooldown(magicId);
            ShowCooldownTip(magicId, remainingTime);
        }
    }
    
    /// <summary>
    /// 立即隐藏冷却提示
    /// </summary>
    public void HideCooldownTip()
    {
        if (currentTipCoroutine != null)
        {
            StopCoroutine(currentTipCoroutine);
            currentTipCoroutine = null;
        }
        
        if (cooldownTipPanel != null)
        {
            cooldownTipPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 设置提示显示时长
    /// </summary>
    public void SetTipDisplayDuration(float duration)
    {
        tipDisplayDuration = Mathf.Max(0.1f, duration);
        Debug.Log($"[MagicUIMgr] 设置提示显示时长: {tipDisplayDuration}秒");
    }
}