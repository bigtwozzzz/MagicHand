using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 魔法选择UI管理器
/// 负责管理魔法选择界面的显示和交互逻辑
/// </summary>
public class MagicSelectUI : MonoBehaviour
{
    #region 字段定义
    
    [Header("魔法选项UI组件")]
    [SerializeField] private Transform[] magicOptions = new Transform[3]; // MagicOption1~3
    [SerializeField] private Image[] magicIcons = new Image[3]; // 每个选项的MagicIcon
    [SerializeField] private TextMeshProUGUI[] magicNames = new TextMeshProUGUI[3]; // 每个选项的Name
    [SerializeField] private TextMeshProUGUI[] magicDescs = new TextMeshProUGUI[3]; // 每个选项的Desc
    
    [Header("功能型手势配置")]
    [SerializeField] private int[] selectionGestures = { 3, 6, 5 }; // 对应选项1、2、3的手势ID
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 当前显示的魔法选项
    private int[] currentMagicOptions = new int[3];
    
    // 魔法选择完成事件
    public System.Action<int> OnMagicSelected;
    
    #endregion
    
    #region 生命周期
    
    private void Awake()
    {
        // 自动查找UI组件
        InitializeUIComponents();
    }
    
    private void OnEnable()
    {
        // 订阅手势事件
        SubscribeToGestureEvents();
        
        // 刷新魔法选项显示
        RefreshMagicOptions();
    }
    
    private void OnDisable()
    {
        // 取消订阅手势事件
        UnsubscribeFromGestureEvents();
    }
    
    #endregion
    
    #region UI初始化
    
    /// <summary>
    /// 初始化UI组件
    /// </summary>
    private void InitializeUIComponents()
    {
        // 自动查找MagicOption1~3
        for (int i = 0; i < 3; i++)
        {
            string optionName = $"MagicOption{i + 1}";
            Transform optionTransform = transform.Find(optionName);
            
            if (optionTransform != null)
            {
                magicOptions[i] = optionTransform;
                
                // 查找子组件
                Transform iconTransform = optionTransform.Find("MagicIcon");
                if (iconTransform != null)
                {
                    magicIcons[i] = iconTransform.GetComponent<Image>();
                }
                
                Transform nameTransform = optionTransform.Find("Name");
                if (nameTransform != null)
                {
                    magicNames[i] = nameTransform.GetComponent<TextMeshProUGUI>();
                }
                
                Transform descTransform = optionTransform.Find("Desc");
                if (descTransform != null)
                {
                    magicDescs[i] = descTransform.GetComponent<TextMeshProUGUI>();
                }
                
                if (enableDebugLog)
                {
                    Debug.Log($"[MagicSelectUI] 找到魔法选项 {optionName}，图标: {magicIcons[i] != null}，名称: {magicNames[i] != null}，描述: {magicDescs[i] != null}");
                }
            }
            else
            {
                Debug.LogError($"[MagicSelectUI] 未找到魔法选项 {optionName}");
            }
        }
        
        Debug.Log("[MagicSelectUI] UI组件初始化完成");
    }
    
    #endregion
    
    #region 魔法选项管理
    
    /// <summary>
    /// 刷新魔法选项显示
    /// </summary>
    public void RefreshMagicOptions()
    {
        // 获取MagicUIController实例
        MagicUIController magicUIController = FindObjectOfType<MagicUIController>();
        if (magicUIController == null)
        {
            Debug.LogError("[MagicSelectUI] 未找到MagicUIController实例");
            return;
        }
        
        // 获取魔法池中的魔法
        List<int> magicPool = magicUIController.GetMagicPool();
        
        if (magicPool.Count == 0)
        {
            Debug.LogWarning("[MagicSelectUI] 魔法池为空，无法显示选项");
            HideAllOptions();
            return;
        }
        
        // 从魔法池中随机选择3个魔法（不足时从第一个开始选择）
        SelectRandomMagics(magicPool);
        
        // 更新UI显示
        UpdateMagicOptionsDisplay();
        
        if (enableDebugLog)
        {
            string optionsStr = string.Join(", ", currentMagicOptions);
            Debug.Log($"[MagicSelectUI] 魔法选项已刷新: {optionsStr}");
        }
    }
    
    /// <summary>
    /// 从魔法池中随机选择魔法
    /// </summary>
    /// <param name="magicPool">魔法池</param>
    private void SelectRandomMagics(List<int> magicPool)
    {
        // 创建魔法池的副本用于随机选择
        List<int> availableMagics = new List<int>(magicPool);
        
        for (int i = 0; i < 3; i++)
        {
            if (availableMagics.Count > 0)
            {
                // 随机选择一个魔法
                int randomIndex = Random.Range(0, availableMagics.Count);
                currentMagicOptions[i] = availableMagics[randomIndex];
                
                // 从可用列表中移除，避免重复选择
                availableMagics.RemoveAt(randomIndex);
            }
            else
            {
                // 魔法池不足，从第一个开始循环选择
                currentMagicOptions[i] = magicPool[i % magicPool.Count];
            }
        }
    }
    
    /// <summary>
    /// 更新魔法选项的UI显示
    /// </summary>
    private void UpdateMagicOptionsDisplay()
    {
        for (int i = 0; i < 3; i++)
        {
            int magicId = currentMagicOptions[i];
            
            if (magicId > 0)
            {
                // 获取魔法数据
                MagicData magicData = MagicConfigLoader.Instance?.GetMagicData(magicId);
                
                if (magicData != null)
                {
                    // 设置魔法图标
                    SetMagicIcon(i, magicId);
                    
                    // 设置魔法名称
                    if (magicNames[i] != null)
                    {
                        magicNames[i].text = magicData.magicName;
                    }
                    
                    // 设置魔法描述
                    if (magicDescs[i] != null)
                    {
                        magicDescs[i].text = magicData.description;
                    }
                    
                    // 显示选项
                    if (magicOptions[i] != null)
                    {
                        magicOptions[i].gameObject.SetActive(true);
                    }
                }
                else
                {
                    Debug.LogError($"[MagicSelectUI] 未找到魔法 {magicId} 的配置数据");
                    HideOption(i);
                }
            }
            else
            {
                HideOption(i);
            }
        }
    }
    
    /// <summary>
    /// 设置魔法图标
    /// </summary>
    /// <param name="optionIndex">选项索引</param>
    /// <param name="magicId">魔法ID</param>
    private void SetMagicIcon(int optionIndex, int magicId)
    {
        if (magicIcons[optionIndex] == null) return;
        
        // 加载魔法图标（与MagicUIController中的逻辑一致）
        string iconPath = $"UI/MagicIcon/MagicIcon{magicId}";
        Sprite iconSprite = Resources.Load<Sprite>(iconPath);
        
        if (iconSprite != null)
        {
            magicIcons[optionIndex].sprite = iconSprite;
            magicIcons[optionIndex].color = Color.white;
        }
        else
        {
            Debug.LogError($"[MagicSelectUI] 无法加载魔法图标: {iconPath}");
            magicIcons[optionIndex].sprite = null;
            magicIcons[optionIndex].color = Color.clear;
        }
    }
    
    /// <summary>
    /// 隐藏指定选项
    /// </summary>
    /// <param name="optionIndex">选项索引</param>
    private void HideOption(int optionIndex)
    {
        if (magicOptions[optionIndex] != null)
        {
            magicOptions[optionIndex].gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 隐藏所有选项
    /// </summary>
    private void HideAllOptions()
    {
        for (int i = 0; i < 3; i++)
        {
            HideOption(i);
        }
    }
    
    #endregion
    
    #region 手势事件处理
    
    /// <summary>
    /// 订阅手势事件
    /// </summary>
    private void SubscribeToGestureEvents()
    {
        GestureEventManager.SubscribeToGesture(OnGestureDetected);
        
        if (enableDebugLog)
        {
            Debug.Log("[MagicSelectUI] 已订阅手势事件");
        }
    }
    
    /// <summary>
    /// 取消订阅手势事件
    /// </summary>
    private void UnsubscribeFromGestureEvents()
    {
        GestureEventManager.UnsubscribeFromGesture(OnGestureDetected);
        
        if (enableDebugLog)
        {
            Debug.Log("[MagicSelectUI] 已取消订阅手势事件");
        }
    }
    
    /// <summary>
    /// 处理手势检测事件
    /// </summary>
    /// <param name="gestureId">手势ID</param>
    private void OnGestureDetected(int gestureId)
    {
        // 检查是否是魔法选择手势
        for (int i = 0; i < selectionGestures.Length; i++)
        {
            if (gestureId == selectionGestures[i])
            {
                SelectMagicOption(i);
                break;
            }
        }
    }
    
    /// <summary>
    /// 选择魔法选项
    /// </summary>
    /// <param name="optionIndex">选项索引（0-2）</param>
    private void SelectMagicOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= currentMagicOptions.Length)
        {
            Debug.LogError($"[MagicSelectUI] 无效的选项索引: {optionIndex}");
            return;
        }
        
        int selectedMagicId = currentMagicOptions[optionIndex];
        
        if (selectedMagicId <= 0)
        {
            Debug.LogWarning($"[MagicSelectUI] 选项 {optionIndex + 1} 没有有效的魔法");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MagicSelectUI] 选择了选项 {optionIndex + 1}，魔法ID: {selectedMagicId}");
        }
        
        // 解锁选中的魔法
        UnlockSelectedMagic(selectedMagicId);
        
        // 触发选择完成事件
        OnMagicSelected?.Invoke(selectedMagicId);
    }
    
    /// <summary>
    /// 解锁选中的魔法并添加到栏位
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    private void UnlockSelectedMagic(int magicId)
    {
        // 获取MagicUIController实例
        MagicUIController magicUIController = FindObjectOfType<MagicUIController>();
        if (magicUIController == null)
        {
            Debug.LogError("[MagicSelectUI] 未找到MagicUIController实例");
            return;
        }
        
        // 解锁魔法
        bool unlocked = magicUIController.UnlockMagic(magicId);
        
        if (unlocked)
        {
            // 查找空闲的栏位并设置魔法
            bool slotFound = false;
            for (int i = 0; i < 5; i++)
            {
                if (magicUIController.GetMagicInSlot(i) == 0)
                {
                    // 找到空闲栏位，设置魔法
                    magicUIController.SetMagicToSlotWithUnlockCheck(i, magicId);
                    slotFound = true;
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"[MagicSelectUI] 魔法 {magicId} 已解锁并设置到栏位 {i + 1}");
                    }
                    break;
                }
            }
            
            if (!slotFound)
            {
                Debug.LogWarning($"[MagicSelectUI] 魔法 {magicId} 已解锁，但没有找到空闲栏位");
            }
        }
        else
        {
            Debug.LogError($"[MagicSelectUI] 魔法 {magicId} 解锁失败");
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 获取当前显示的魔法选项
    /// </summary>
    /// <returns>魔法ID数组</returns>
    public int[] GetCurrentMagicOptions()
    {
        return (int[])currentMagicOptions.Clone();
    }
    
    /// <summary>
    /// 手动选择魔法选项（用于测试）
    /// </summary>
    /// <param name="optionIndex">选项索引</param>
    [ContextMenu("测试选择选项1")]
    public void TestSelectOption1()
    {
        SelectMagicOption(0);
    }
    
    [ContextMenu("测试选择选项2")]
    public void TestSelectOption2()
    {
        SelectMagicOption(1);
    }
    
    [ContextMenu("测试选择选项3")]
    public void TestSelectOption3()
    {
        SelectMagicOption(2);
    }
    
    #endregion
}