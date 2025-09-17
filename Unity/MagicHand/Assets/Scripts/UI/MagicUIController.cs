using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 魔法UI控制器 - 负责管理魔法图标显示和冷却效果
/// 自动挂载到MainUI物体上
/// </summary>
[RequireComponent(typeof(MainUI))]
public class MagicUIController : MonoBehaviour
{
    [Header("魔法图标管理")]
    [SerializeField] private Transform magicRoot; // Magic根物体
    [SerializeField] private Image[] magicIconImages = new Image[4]; // MagicImg1~4的Image组件
    [SerializeField] private Image[] magicMaskImages = new Image[4]; // 对应的Mask组件
    
    private int[] magicSlots = new int[4] { 0, 0, 0, 0 }; // 每个栏位对应的魔法编号，0表示空
    private Coroutine updateCoroutine; // 冷却更新协程引用
    
    private void Awake()
    {
        // 初始化魔法图标系统
        InitializeMagicIcons();
        
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
        magicIconImages = new Image[4];
        magicMaskImages = new Image[4];
        magicSlots = new int[4];
        
        // 自动查找魔法图标组件
        for (int i = 0; i < 4; i++)
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
            
            // 初始化魔法栏位为空
            magicSlots[i] = 0;
        }
        
        Debug.Log("[MagicUIController] 魔法图标系统自动初始化完成");
    }
    
    /// <summary>
    /// 将魔法放置到指定栏位
    /// </summary>
    /// <param name="slotIndex">栏位索引（0-3）</param>
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
        // 把3号魔法放在栏位1
        SetMagicToSlot(0, 3);
        
        // 把22号魔法放在栏位2
        SetMagicToSlot(1, 22);
        
        Debug.Log("[MagicUIController] 测试魔法栏位设置完成：栏位1=魔法3，栏位2=魔法22");
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