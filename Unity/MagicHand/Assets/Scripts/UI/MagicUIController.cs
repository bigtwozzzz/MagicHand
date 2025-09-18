using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 魔法UI控制器
/// 管理魔法栏位的图标显示和冷却效果，以及魔法技能池系统
/// </summary>
public class MagicUIController : MonoBehaviour
{
    #region 字段定义
    
    [Header("魔法图标管理")]
    [SerializeField] private Image[] magicIconImages = new Image[5]; // 魔法图标Image组件数组
    [SerializeField] private Image[] magicMaskImages = new Image[5];  // 魔法冷却遮罩Image组件数组
    
    [Header("魔法栏位配置")]
    [SerializeField] private int[] magicSlots = new int[5]; // 魔法栏位数组，存储每个栏位对应的魔法ID
    
    [Header("魔法技能池系统")]
    [SerializeField] private List<int> unlockedMagics = new List<int>(); // 已解锁的魔法列表
    [SerializeField] private List<int> magicPool = new List<int>(); // 未解锁的魔法池
    
    [Header("协程引用")]
    private Coroutine cooldownUpdateCoroutine; // 冷却更新协程引用
    
    [Header("内部引用")]
    [SerializeField] private Transform magicRoot; // Magic根物体
    private Coroutine updateCoroutine; // 冷却更新协程引用
    
    #endregion
    
    private void Awake()
    {
        // 初始化魔法图标系统
        InitializeMagicIcons();
        
        // 初始化魔法技能池系统
        InitializeMagicPool();
        
        // 订阅魔法冷却事件
        MagicEventSystem.OnMagicCooldownStart += OnMagicCooldownStart;
        MagicEventSystem.OnMagicCooldownEnd += OnMagicCooldownEnd;
    }
    
    private void OnEnable()
    {
        // 启动魔法冷却更新
        if (updateCoroutine == null)
        {
            updateCoroutine = StartCoroutine(UpdateMagicCooldowns());
        }
    }
    
    private void OnDisable()
    {
        // 停止冷却更新协程
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }
    
    private void OnDestroy()
    {
        // 取消魔法冷却事件订阅
        MagicEventSystem.OnMagicCooldownStart -= OnMagicCooldownStart;
        MagicEventSystem.OnMagicCooldownEnd -= OnMagicCooldownEnd;
    }
    
    #region 魔法图标管理
    
    /// <summary>
    /// 初始化魔法图标系统
    /// </summary>
    private void InitializeMagicIcons()
    {
        // 自动查找Magic子物体
        Transform magicTransform = transform.Find("Magic");
        if (magicTransform == null)
        {
            Debug.LogError("[MagicUIController] 未找到Magic子物体");
            return;
        }
        
        magicRoot = magicTransform;
        
        // 初始化数组
        magicIconImages = new Image[5];
        magicMaskImages = new Image[5];
        magicSlots = new int[5] { 24, 0, 0, 0, 23 }; // 栏位1固定24号，栏位5固定23号
        
        // 自动查找魔法图标组件
        for (int i = 0; i < 5; i++)
        {
            string magicImgName = $"MagicImg{i + 1}";
            Transform magicImgTransform = magicTransform.Find(magicImgName);
            
            if (magicImgTransform != null)
            {
                // 获取魔法图标的Image组件
                magicIconImages[i] = magicImgTransform.GetComponent<Image>();
                
                // 查找Mask子物体的Image组件
                Transform maskTransform = magicImgTransform.Find("Mask");
                if (maskTransform != null)
                {
                    magicMaskImages[i] = maskTransform.GetComponent<Image>();
                    if (magicMaskImages[i] != null)
                    {
                        magicMaskImages[i].fillAmount = 0f; // 初始化为无冷却状态
                    }
                }
                else
                {
                    Debug.LogError($"[MagicUIController] {magicImgName}下未找到Mask子物体");
                }
            }
            else
            {
                Debug.LogError($"[MagicUIController] 未找到{magicImgName}子物体");
            }
            
        }
        
        // 为固定栏位设置魔法图标（使用强制设置，因为这些是默认解锁的魔法）
        SetMagicToSlotWithUnlockCheck(0, 24, true); // 栏位1固定为24号魔法
        SetMagicToSlotWithUnlockCheck(4, 23, true); // 栏位5固定为23号魔法
        
        Debug.Log("[MagicUIController] 魔法图标系统自动初始化完成");
    }
    
    /// <summary>
    /// 将魔法放置到指定栏位
    /// </summary>
    /// <param name="slotIndex">栏位索引（0-4）</param>
    /// <param name="magicId">魔法编号</param>
    public void SetMagicToSlot(int slotIndex, int magicId)
    {
        if (slotIndex < 0 || slotIndex >= magicSlots.Length)
        {
            Debug.LogError($"[MagicUIController] 无效的栏位索引: {slotIndex}");
            return;
        }
        
        // 更新栏位记录
        magicSlots[slotIndex] = magicId;
        
        // 更新图标显示
        if (magicIconImages[slotIndex] != null)
        {
            if (magicId > 0)
            {
                // 加载魔法图标
                string iconPath = $"UI/MagicIcon/MagicIcon{magicId}";
                Sprite iconSprite = Resources.Load<Sprite>(iconPath);
                
                if (iconSprite != null)
                {
                    magicIconImages[slotIndex].sprite = iconSprite;
                    magicIconImages[slotIndex].color = Color.white;
                    Debug.Log($"[MagicUIController] 栏位{slotIndex + 1}设置魔法{magicId}成功");
                }
                else
                {
                    Debug.LogError($"[MagicUIController] 无法加载魔法图标: {iconPath}");
                }
            }
            else
            {
                // 清空栏位
                magicIconImages[slotIndex].sprite = null;
                magicIconImages[slotIndex].color = Color.clear;
                Debug.Log($"[MagicUIController] 清空栏位{slotIndex + 1}");
            }
        }
    }
    
    /// <summary>
    /// 魔法冷却开始事件处理
    /// </summary>
    private void OnMagicCooldownStart(int magicId, float cooldownTime)
    {
        // 找到对应的栏位并开始显示冷却
        for (int i = 0; i < magicSlots.Length; i++)
        {
            if (magicSlots[i] == magicId && magicMaskImages[i] != null)
            {
                magicMaskImages[i].fillAmount = 1f;
                Debug.Log($"[MagicUIController] 魔法{magicId}在栏位{i + 1}开始冷却，时长{cooldownTime}s");
                break;
            }
        }
    }
    
    /// <summary>
    /// 魔法冷却结束事件处理
    /// </summary>
    private void OnMagicCooldownEnd(int magicId)
    {
        // 找到对应的栏位并结束冷却显示
        for (int i = 0; i < magicSlots.Length; i++)
        {
            if (magicSlots[i] == magicId && magicMaskImages[i] != null)
            {
                magicMaskImages[i].fillAmount = 0f;
                Debug.Log($"[MagicUIController] 魔法{magicId}在栏位{i + 1}冷却结束");
                break;
            }
        }
    }
    
    /// <summary>
    /// 更新魔法冷却显示的协程
    /// </summary>
    private IEnumerator UpdateMagicCooldowns()
    {
        while (true)
        {
            // 更新每个栏位的冷却显示
            for (int i = 0; i < magicSlots.Length; i++)
            {
                int magicId = magicSlots[i];
                if (magicId > 0 && magicMaskImages[i] != null)
                {
                    if (MagicCooldown.Instance != null && MagicCooldown.Instance.IsOnCooldown(magicId))
                    {
                        // 获取冷却进度（0-1，0表示冷却完成，1表示刚开始冷却）
                        float progress = MagicCooldown.Instance.GetCooldownProgress(magicId);
                        // fillAmount应该从1减少到0，所以用1-progress
                        magicMaskImages[i].fillAmount = 1f - progress;
                    }
                    else
                    {
                        // 没有冷却或冷却完成
                        magicMaskImages[i].fillAmount = 0f;
                    }
                }
            }
            
            yield return new WaitForSeconds(0.1f); // 每0.1秒更新一次
        }
    }
    
    /// <summary>
    /// 测试方法：设置测试魔法到栏位
    /// </summary>
    [ContextMenu("测试魔法栏位")]
    public void TestMagicSlots()
    {
        // 栏位1固定为24号魔法（光束魔法）
        SetMagicToSlotWithUnlockCheck(0, 24, true);
        
        // 先解锁其他魔法，然后设置到栏位
        UnlockMagic(32); // 解锁流星魔法
        SetMagicToSlotWithUnlockCheck(1, 32);
        
        // 尝试设置未解锁的魔法（应该失败）
        SetMagicToSlotWithUnlockCheck(2, 35);
        
        // 解锁后再设置
        UnlockMagic(35);
        SetMagicToSlotWithUnlockCheck(2, 35);
        
        // 栏位5固定为23号魔法（治疗魔法）
        SetMagicToSlotWithUnlockCheck(4, 23, true);
        
        Debug.Log("[MagicUIController] 测试魔法栏位设置完成");
    }
    
    /// <summary>
    /// 获取魔法池（未解锁的魔法列表）
    /// </summary>
    /// <returns>魔法池列表</returns>
    public List<int> GetMagicPool()
    {
        return new List<int>(magicPool);
    }
    
    /// <summary>
    /// 测试解锁魔法功能
    /// </summary>
    [ContextMenu("测试解锁魔法")]
    public void TestUnlockMagic()
    {
        // 解锁魔法ID为4的魔法
        int testMagicId = 4;
        bool success = UnlockMagic(testMagicId);
        Debug.Log($"[MagicUIController] 测试解锁魔法 {testMagicId}: {(success ? "成功" : "失败")}");
        
        // 尝试设置到栏位
        if (success)
        {
            SetMagicToSlotWithUnlockCheck(2, testMagicId); // 设置到第3个栏位
        }
    }
    
    #endregion
    
    #region 魔法技能池系统
    
    /// <summary>
    /// 初始化魔法技能池系统
    /// </summary>
    private void InitializeMagicPool()
    {
        // 获取所有可用的魔法
        if (MagicConfigLoader.Instance != null && MagicConfigLoader.Instance.IsConfigLoaded)
        {
            List<MagicData> allMagics = MagicConfigLoader.Instance.GetAllMagicData();
            
            // 清空现有数据
            magicPool.Clear();
            
            // 将所有魔法添加到池中
            foreach (var magic in allMagics)
            {
                if (magic.isEnabled)
                {
                    magicPool.Add(magic.magicId);
                }
            }
            
            // 初始化时解锁默认魔法（栏位1和栏位5的固定魔法）
            UnlockMagic(24); // 光束魔法
            UnlockMagic(23); // 治疗魔法
            
            Debug.Log($"[MagicUIController] 魔法技能池初始化完成，池中魔法数量: {magicPool.Count}, 已解锁魔法数量: {unlockedMagics.Count}");
        }
        else
        {
            Debug.LogWarning("[MagicUIController] 魔法配置未加载，无法初始化技能池");
        }
    }
    
    /// <summary>
    /// 解锁魔法
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <returns>是否成功解锁</returns>
    public bool UnlockMagic(int magicId)
    {
        // 检查魔法是否在池中
        if (!magicPool.Contains(magicId))
        {
            Debug.LogWarning($"[MagicUIController] 魔法 {magicId} 不在技能池中，无法解锁");
            return false;
        }
        
        // 检查是否已经解锁
        if (unlockedMagics.Contains(magicId))
        {
            Debug.Log($"[MagicUIController] 魔法 {magicId} 已经解锁");
            return true;
        }
        
        // 从池中移除并添加到已解锁列表
        magicPool.Remove(magicId);
        unlockedMagics.Add(magicId);
        
        Debug.Log($"[MagicUIController] 成功解锁魔法 {magicId}");
        return true;
    }
    
    /// <summary>
    /// 检查魔法是否已解锁
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <returns>是否已解锁</returns>
    public bool IsMagicUnlocked(int magicId)
    {
        return unlockedMagics.Contains(magicId);
    }
    
    /// <summary>
    /// 获取已解锁的魔法列表
    /// </summary>
    /// <returns>已解锁魔法ID列表</returns>
    public List<int> GetUnlockedMagics()
    {
        return new List<int>(unlockedMagics);
    }
    
    /// <summary>
    /// 将解锁的魔法设置到栏位（重写原方法以添加解锁检查）
    /// </summary>
    /// <param name="slotIndex">栏位索引（0-4）</param>
    /// <param name="magicId">魔法编号</param>
    /// <param name="forceSet">是否强制设置（忽略解锁检查）</param>
    public bool SetMagicToSlotWithUnlockCheck(int slotIndex, int magicId, bool forceSet = false)
    {
        if (slotIndex < 0 || slotIndex >= magicSlots.Length)
        {
            Debug.LogError($"[MagicUIController] 无效的栏位索引: {slotIndex}");
            return false;
        }
        
        // 如果魔法ID为0，表示清空栏位
        if (magicId == 0)
        {
            SetMagicToSlot(slotIndex, 0);
            return true;
        }
        
        // 检查魔法是否已解锁（除非强制设置）
        if (!forceSet && !IsMagicUnlocked(magicId))
        {
            Debug.LogWarning($"[MagicUIController] 魔法 {magicId} 未解锁，无法设置到栏位 {slotIndex}");
            return false;
        }
        
        // 设置魔法到栏位
        SetMagicToSlot(slotIndex, magicId);
        return true;
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 获取指定栏位的魔法ID
    /// </summary>
    /// <param name="slotIndex">栏位索引</param>
    /// <returns>魔法ID，0表示空栏位</returns>
    public int GetMagicInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < magicSlots.Length)
        {
            return magicSlots[slotIndex];
        }
        return 0;
    }
    
    /// <summary>
    /// 清空指定栏位
    /// </summary>
    /// <param name="slotIndex">栏位索引</param>
    public void ClearSlot(int slotIndex)
    {
        SetMagicToSlot(slotIndex, 0);
    }
    
    /// <summary>
    /// 清空所有栏位
    /// </summary>
    public void ClearAllSlots()
    {
        for (int i = 0; i < magicSlots.Length; i++)
        {
            ClearSlot(i);
        }
    }
    
    #endregion
}