using UnityEngine;

/// <summary>
/// 魔法选择管理器
/// 负责管理波次结束时的游戏暂停、魔法选择UI激活和游戏恢复逻辑
/// </summary>
public class MagicSelectManager : MonoBehaviour
{
    #region 字段定义
    
    [Header("UI配置")]
    [SerializeField] private GameObject magicSelectUIObject; // 魔法选择UI对象
    [SerializeField] private MagicSelectUI magicSelectUI; // 魔法选择UI脚本
    
    [Header("游戏状态配置")]
    [SerializeField] private bool pauseGameOnMagicSelect = true; // 是否在魔法选择时暂停游戏
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 游戏状态管理器引用
    private GameStateManager gameStateManager;
    private MonsterWaveMgr waveManager;
    
    // 魔法选择状态
    private bool isMagicSelectionActive = false;
    
    #endregion
    
    #region 生命周期
    
    private void Awake()
    {
        // 获取必要的组件引用
        InitializeComponents();
    }
    
    private void Start()
    {
        // 订阅波次事件
        SubscribeToWaveEvents();
        
        // 订阅魔法选择事件
        SubscribeToMagicSelectEvents();
        
        // 初始化UI状态
        InitializeUIState();
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        UnsubscribeFromWaveEvents();
        UnsubscribeFromMagicSelectEvents();
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitializeComponents()
    {
        // 获取游戏状态管理器
        gameStateManager = FindObjectOfType<GameStateManager>();
        if (gameStateManager == null)
        {
            Debug.LogError("[MagicSelectManager] 未找到GameStateManager实例");
        }
        
        // 获取波次管理器
        waveManager = FindObjectOfType<MonsterWaveMgr>();
        if (waveManager == null)
        {
            Debug.LogError("[MagicSelectManager] 未找到MonsterWaveMgr实例");
        }
        
        // 自动查找魔法选择UI
        if (magicSelectUIObject == null)
        {
            GameObject uiObject = GameObject.Find("MagicSelectUI");
            if (uiObject != null)
            {
                magicSelectUIObject = uiObject;
                if (enableDebugLog)
                {
                    Debug.Log("[MagicSelectManager] 自动找到MagicSelectUI对象");
                }
            }
            else
            {
                Debug.LogWarning("[MagicSelectManager] 未找到MagicSelectUI对象，请手动设置");
            }
        }
        
        // 获取魔法选择UI脚本
        if (magicSelectUIObject != null && magicSelectUI == null)
        {
            magicSelectUI = magicSelectUIObject.GetComponent<MagicSelectUI>();
            if (magicSelectUI == null)
            {
                Debug.LogError("[MagicSelectManager] MagicSelectUI对象上未找到MagicSelectUI脚本");
            }
        }
        
        Debug.Log("[MagicSelectManager] 组件初始化完成");
    }
    
    /// <summary>
    /// 初始化UI状态
    /// </summary>
    private void InitializeUIState()
    {
        // 确保魔法选择UI初始状态为隐藏
        if (magicSelectUIObject != null)
        {
            magicSelectUIObject.SetActive(false);
        }
        
        isMagicSelectionActive = false;
        
        if (enableDebugLog)
        {
            Debug.Log("[MagicSelectManager] UI状态初始化完成");
        }
    }
    
    #endregion
    
    #region 事件订阅
    
    /// <summary>
    /// 订阅波次事件
    /// </summary>
    private void SubscribeToWaveEvents()
    {
        if (waveManager != null)
        {
            // 订阅波次完成事件
            waveManager.OnWaveComplete += OnWaveComplete;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 已订阅波次完成事件");
            }
        }
    }
    
    /// <summary>
    /// 取消订阅波次事件
    /// </summary>
    private void UnsubscribeFromWaveEvents()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveComplete -= OnWaveComplete;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 已取消订阅波次完成事件");
            }
        }
    }
    
    /// <summary>
    /// 订阅魔法选择事件
    /// </summary>
    private void SubscribeToMagicSelectEvents()
    {
        if (magicSelectUI != null)
        {
            magicSelectUI.OnMagicSelected += OnMagicSelected;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 已订阅魔法选择事件");
            }
        }
    }
    
    /// <summary>
    /// 取消订阅魔法选择事件
    /// </summary>
    private void UnsubscribeFromMagicSelectEvents()
    {
        if (magicSelectUI != null)
        {
            magicSelectUI.OnMagicSelected -= OnMagicSelected;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 已取消订阅魔法选择事件");
            }
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 处理波次完成事件
    /// </summary>
    /// <param name="waveIndex">完成的波次索引</param>
    private void OnWaveComplete(int waveIndex)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MagicSelectManager] 波次 {waveIndex + 1} 完成，准备激活魔法选择UI");
        }
        
        // 检查是否有可解锁的魔法
        if (HasUnlockableMagics())
        {
            // 激活魔法选择流程
            ActivateMagicSelection();
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 没有可解锁的魔法，跳过魔法选择");
            }
            
            // 直接开始下一波
            StartNextWaveDirectly();
        }
    }
    
    /// <summary>
    /// 处理魔法选择事件
    /// </summary>
    /// <param name="selectedMagicId">选中的魔法ID</param>
    private void OnMagicSelected(int selectedMagicId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MagicSelectManager] 魔法选择完成，选中魔法ID: {selectedMagicId}");
        }
        
        // 隐藏魔法选择UI并恢复游戏
        DeactivateMagicSelection();
    }
    
    #endregion
    
    #region 魔法选择流程
    
    /// <summary>
    /// 激活魔法选择流程
    /// </summary>
    private void ActivateMagicSelection()
    {
        if (isMagicSelectionActive)
        {
            Debug.LogWarning("[MagicSelectManager] 魔法选择已经激活，忽略重复激活");
            return;
        }
        
        isMagicSelectionActive = true;
        
        // 暂停游戏
        if (pauseGameOnMagicSelect)
        {
            PauseGame();
        }
        
        // 激活魔法选择UI
        ShowMagicSelectUI();
        
        if (enableDebugLog)
        {
            Debug.Log("[MagicSelectManager] 魔法选择流程已激活");
        }
    }
    
    /// <summary>
    /// 取消激活魔法选择流程
    /// </summary>
    private void DeactivateMagicSelection()
    {
        if (!isMagicSelectionActive)
        {
            Debug.LogWarning("[MagicSelectManager] 魔法选择未激活，忽略取消激活");
            return;
        }
        
        // 隐藏魔法选择UI
        HideMagicSelectUI();
        
        // 恢复游戏
        if (pauseGameOnMagicSelect)
        {
            ResumeGame();
        }
        
        isMagicSelectionActive = false;
        
        if (enableDebugLog)
        {
            Debug.Log("[MagicSelectManager] 魔法选择流程已取消激活");
        }
        
        // 立即开始下一波次
        StartNextWaveDirectly();
    }
    
    #endregion
    
    #region 游戏状态控制
    
    /// <summary>
    /// 暂停游戏
    /// </summary>
    private void PauseGame()
    {
        if (gameStateManager != null)
        {
            gameStateManager.PauseGame();
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 游戏已暂停");
            }
        }
        else
        {
            // 备用暂停方法
            Time.timeScale = 0f;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 使用备用方法暂停游戏");
            }
        }
    }
    
    /// <summary>
    /// 恢复游戏
    /// </summary>
    private void ResumeGame()
    {
        if (gameStateManager != null)
        {
            gameStateManager.ResumeGame();
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 游戏已恢复");
            }
        }
        else
        {
            // 备用恢复方法
            Time.timeScale = 1f;
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 使用备用方法恢复游戏");
            }
        }
    }
    
    #endregion
    
    #region UI控制
    
    /// <summary>
    /// 显示魔法选择UI
    /// </summary>
    private void ShowMagicSelectUI()
    {
        if (magicSelectUIObject != null)
        {
            magicSelectUIObject.SetActive(true);
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 魔法选择UI已显示");
            }
        }
        else
        {
            Debug.LogError("[MagicSelectManager] 魔法选择UI对象为空，无法显示");
        }
    }
    
    /// <summary>
    /// 隐藏魔法选择UI
    /// </summary>
    private void HideMagicSelectUI()
    {
        if (magicSelectUIObject != null)
        {
            magicSelectUIObject.SetActive(false);
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 魔法选择UI已隐藏");
            }
        }
        else
        {
            Debug.LogError("[MagicSelectManager] 魔法选择UI对象为空，无法隐藏");
        }
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 检查是否有可解锁的魔法
    /// </summary>
    /// <returns>是否有可解锁的魔法</returns>
    private bool HasUnlockableMagics()
    {
        MagicUIController magicUIController = FindObjectOfType<MagicUIController>();
        if (magicUIController == null)
        {
            Debug.LogError("[MagicSelectManager] 未找到MagicUIController实例");
            return false;
        }
        
        // 检查魔法池是否有未解锁的魔法
        var magicPool = magicUIController.GetMagicPool();
        return magicPool != null && magicPool.Count > 0;
    }
    
    /// <summary>
    /// 直接开始下一波
    /// </summary>
    private void StartNextWaveDirectly()
    {
        if (waveManager != null)
        {
            // 触发下一波开始
            waveManager.NextWave();
            
            if (enableDebugLog)
            {
                Debug.Log("[MagicSelectManager] 直接开始下一波");
            }
        }
        else
        {
            Debug.LogError("[MagicSelectManager] 波次管理器为空，无法开始下一波");
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 获取魔法选择是否激活
    /// </summary>
    /// <returns>是否激活</returns>
    public bool IsMagicSelectionActive()
    {
        return isMagicSelectionActive;
    }
    
    /// <summary>
    /// 手动激活魔法选择（用于测试）
    /// </summary>
    [ContextMenu("测试激活魔法选择")]
    public void TestActivateMagicSelection()
    {
        ActivateMagicSelection();
    }
    
    /// <summary>
    /// 手动取消激活魔法选择（用于测试）
    /// </summary>
    [ContextMenu("测试取消激活魔法选择")]
    public void TestDeactivateMagicSelection()
    {
        DeactivateMagicSelection();
    }
    
    #endregion
}