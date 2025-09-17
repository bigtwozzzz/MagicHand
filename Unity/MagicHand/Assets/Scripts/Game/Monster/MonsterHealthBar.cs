using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 怪物血条脚本
/// 在UI层级显示怪物血量，支持颜色渐变和世界坐标跟随
/// </summary>
public class MonsterHealthBar : MonoBehaviour
{
    [Header("血条配置")]
    public Canvas healthBarCanvas;              // 血条画布
    public Image healthBarBackground;           // 血条背景
    public Image healthBarFill;                 // 血条填充
    public Text healthText;                     // 血量文字（可选）
    
    [Header("颜色配置")]
    public Color fullHealthColor = Color.green; // 满血颜色
    public Color lowHealthColor = Color.red;    // 残血颜色
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 背景颜色
    
    [Header("显示配置")]
    public Vector3 worldOffset = new Vector3(0, 2f, 0); // 世界坐标偏移
    public bool showHealthText = true;          // 是否显示血量文字
    public bool alwaysVisible = false;          // 是否始终可见
    public float hideDistance = 20f;            // 超过此距离隐藏血条
    
    [Header("调试配置")]
    public bool enableDebugLog = false;
    
    // 私有变量
    private MonsterRuntimeData runtimeData;
    private Camera mainCamera;
    private RectTransform canvasRectTransform;
    private bool isInitialized = false;
    
    void Awake()
    {
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // 初始化UI组件
        InitializeUI();
    }
    
    void Start()
    {
        // 获取怪物运行时数据
        runtimeData = GetComponent<MonsterRuntimeData>();
        if (runtimeData == null)
        {
            Debug.LogError($"[MonsterHealthBar] 未找到MonsterRuntimeData组件: {gameObject.name}");
            enabled = false;
            return;
        }
        
        // 激活怪物时直接显示血条
        alwaysVisible = true;
        
        // 订阅血量变化事件
        if (runtimeData != null)
        {
            // 这里可以订阅血量变化事件，如果MonsterRuntimeData有的话
            // runtimeData.OnHealthChanged += UpdateHealthBar;
        }
        
        isInitialized = true;
        
        // 初始更新血条
        UpdateHealthBar();
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterHealthBar] 血条初始化完成: {gameObject.name}");
        }
    }
    
    void Update()
    {
        if (!isInitialized || runtimeData == null || healthBarCanvas == null) return;
        
        // 检查游戏是否暂停
        if (GameStateManager.Instance.IsPaused)
        {
            return; // 暂停时不更新血条
        }
        
        // 更新血条位置
        UpdateHealthBarPosition();
        
        // 更新血条显示
        UpdateHealthBar();
        
        // 检查可见性
        UpdateVisibility();
    }
    
    /// <summary>
    /// 初始化UI组件
    /// </summary>
    private void InitializeUI()
    {
        // 如果没有指定画布，创建一个
        if (healthBarCanvas == null)
        {
            CreateHealthBarUI();
        }
        
        // 设置背景颜色
        if (healthBarBackground != null)
        {
            healthBarBackground.color = backgroundColor;
        }
        
        // 获取画布RectTransform
        if (healthBarCanvas != null)
        {
            canvasRectTransform = healthBarCanvas.GetComponent<RectTransform>();
        }
        
        // 调整worldOffset以适应怪物缩放
        AdjustWorldOffsetForScale();
    }
    
    /// <summary>
    /// 创建血条UI
    /// </summary>
    private void CreateHealthBarUI()
    {
        // 创建画布
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform);
        
        healthBarCanvas = canvasGO.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.worldCamera = mainCamera;
        healthBarCanvas.sortingOrder = 100; // 确保在最上层
        
        // 添加CanvasScaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        
        // 设置画布大小和位置
        canvasRectTransform = canvasGO.GetComponent<RectTransform>();
        canvasRectTransform.sizeDelta = new Vector2(100, 10);
        
        // 获取怪物的缩放倍数并应用反向缩放
        float monsterScale = GetMonsterScaleMultiplier();
        Vector3 baseScale = Vector3.one * 0.01f; // 基础缩放
        canvasRectTransform.localScale = baseScale / monsterScale; // 反向缩放
        
        // 创建背景
        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(canvasGO.transform, false);
        
        healthBarBackground = backgroundGO.AddComponent<Image>();
        healthBarBackground.color = backgroundColor;
        
        RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // 创建填充
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(backgroundGO.transform, false);
        
        healthBarFill = fillGO.AddComponent<Image>();
        healthBarFill.color = fullHealthColor;
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        // 创建文字（可选）
        if (showHealthText)
        {
            GameObject textGO = new GameObject("HealthText");
            textGO.transform.SetParent(canvasGO.transform, false);
            
            healthText = textGO.AddComponent<Text>();
            healthText.text = "100/100";
            healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthText.fontSize = 24;
            healthText.color = Color.white;
            healthText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
    }
    
    /// <summary>
    /// 更新血条位置
    /// </summary>
    private void UpdateHealthBarPosition()
    {
        if (mainCamera == null || canvasRectTransform == null) return;
        
        // 血条是怪物的子物体，使用相对位置
        canvasRectTransform.localPosition = worldOffset;
        
        // 让血条始终面向摄像机
        Vector3 worldPosition = transform.position + worldOffset;
        Vector3 directionToCamera = mainCamera.transform.position - worldPosition;
        canvasRectTransform.rotation = Quaternion.LookRotation(-directionToCamera);
    }
    
    /// <summary>
    /// 更新血条显示
    /// </summary>
    private void UpdateHealthBar()
    {
        if (runtimeData == null || healthBarFill == null) return;
        
        // 获取最大血量（从配置中获取）
        int maxHealth = runtimeData.GetMaxHealth();
        if (maxHealth <= 0) return;
        
        // 计算血量百分比
        float healthPercentage = (float)runtimeData.currentHealth / maxHealth;
        healthPercentage = Mathf.Clamp01(healthPercentage);
        
        // 更新填充量
        healthBarFill.fillAmount = healthPercentage;
        
        // 更新Fill组件的right值实现血条缩短效果
        // 满血时right=0，血量为0时right=100
        RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
        if (fillRect != null)
        {
            float rightValue = (1f - healthPercentage) * 100f;
            fillRect.offsetMax = new Vector2(-rightValue, fillRect.offsetMax.y);
        }
        
        // 更新颜色（线性插值）
        Color currentColor = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
        healthBarFill.color = currentColor;
        
        // 更新文字
        if (healthText != null && showHealthText)
        {
            healthText.text = $"{runtimeData.currentHealth}/{maxHealth}";
        }
    }
    
    /// <summary>
    /// 更新可见性
    /// </summary>
    private void UpdateVisibility()
    {
        if (healthBarCanvas == null || mainCamera == null) return;
        
        bool shouldShow = alwaysVisible;
        
        // 检查怪物是否激活和存活
        if (runtimeData != null)
        {
            if (!gameObject.activeInHierarchy || !runtimeData.isAlive)
            {
                shouldShow = false;
            }
        }
        else
        {
            shouldShow = false;
        }
        
        // 检查距离
        if (shouldShow && !alwaysVisible)
        {
            float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
            shouldShow = distance <= hideDistance;
        }
        
        // 设置可见性
        if (healthBarCanvas.gameObject.activeSelf != shouldShow)
        {
            healthBarCanvas.gameObject.SetActive(shouldShow);
        }
    }
    
    /// <summary>
    /// 设置血条可见性
    /// </summary>
    /// <param name="visible">是否可见</param>
    public void SetVisible(bool visible)
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 强制更新血条
    /// </summary>
    public void ForceUpdateHealthBar()
    {
        UpdateHealthBar();
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (runtimeData != null)
        {
            // runtimeData.OnHealthChanged -= UpdateHealthBar;
        }
        
        // 销毁UI
        if (healthBarCanvas != null && healthBarCanvas.gameObject != null)
        {
            Destroy(healthBarCanvas.gameObject);
        }
    }
    
    void OnDisable()
    {
        // 隐藏血条
        SetVisible(false);
    }
    
    void OnEnable()
    {
        // 显示血条
        if (isInitialized)
        {
            SetVisible(true);
        }
    }
    
    /// <summary>
    /// 获取当前血量百分比
    /// </summary>
    /// <returns>血量百分比（0-1）</returns>
    public float GetHealthPercentage()
    {
        if (runtimeData == null) return 1f;
        
        int maxHealth = runtimeData.GetMaxHealth();
        if (maxHealth <= 0) return 1f;
        
        return (float)runtimeData.currentHealth / maxHealth;
    }
    
    /// <summary>
    /// 获取怪物的缩放倍数
    /// </summary>
    /// <returns>缩放倍数</returns>
    private float GetMonsterScaleMultiplier()
    {
        // 尝试从MonsterRuntimeData获取配置信息
        if (runtimeData != null && runtimeData.GetConfig() != null)
        {
            return runtimeData.GetConfig().scaleMultiplier;
        }
        
        // 如果无法获取配置，尝试从transform的缩放推算
        // 假设原始缩放是1，当前缩放除以原始缩放得到倍数
        Vector3 currentScale = transform.localScale;
        float averageScale = (currentScale.x + currentScale.y + currentScale.z) / 3f;
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterHealthBar] 获取缩放倍数: 配置={runtimeData?.GetConfig()?.scaleMultiplier ?? -1}, 计算={averageScale}");
        }
        
        return averageScale > 0 ? averageScale : 1f;
     }
     
     /// <summary>
     /// 调整worldOffset以适应怪物缩放
     /// </summary>
     private void AdjustWorldOffsetForScale()
     {
         float monsterScale = GetMonsterScaleMultiplier();
         
         if (monsterScale != 1.0f)
         {
             // 血条的worldOffset需要反向处理，除以缩放系数
             Vector3 adjustedOffset = worldOffset;
             adjustedOffset.y /= monsterScale; // 主要调整Y轴偏移
             worldOffset = adjustedOffset;
             
             if (enableDebugLog)
             {
                 Debug.Log($"[MonsterHealthBar] 调整血条偏移: 原始Y={adjustedOffset.y * monsterScale}, 调整后Y={adjustedOffset.y}, 缩放={monsterScale}");
             }
         }
     }
 }