// Scripts/Components/HeadPlateComponent.cs
using Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Billboard))]
public class HeadPlateComponent : MonoBehaviour, IComponent
{
    [SerializeField] private TextMeshProUGUI nameText; // 主文本（角色名）
    [SerializeField] private TextMeshProUGUI userText;  // 新增：用户名文本

    [Header("显示设置")]
    [SerializeField] private string userPrefix = "[{0}]"; // 用户名前缀，如 "[PlayerA]"
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color friendColor = Color.green;

    [Header("布局")]
    [SerializeField] private float spacing = 5f; // 两个文本之间的间距

    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized) return;

        // 自动查找组件
        if (nameText == null)
        {
            nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        }
        if (userText == null)
        {
            userText = transform.Find("UserText")?.GetComponent<TextMeshProUGUI>();
        }

        bool missingText = false;
        if (nameText == null)
        {
            Debug.LogError("[HeadPlateComponent] 未找到 NameText！", this);
            missingText = true;
        }
        if (userText == null)
        {
            Debug.LogWarning("[HeadPlateComponent] 未找到 UserText，将只显示角色名。", this);
        }

        if (missingText)
        {
            enabled = false;
            return;
        }

        // 初始化隐藏
        nameText.text = "";
        if (userText != null) userText.text = "";
        nameText.gameObject.SetActive(true);
        if (userText != null) userText.gameObject.SetActive(true);

        isInitialized = true;
    }

    public void UpdateData() { }

    /// <summary>
    /// 并排设置用户名和角色名
    /// </summary>
    /// <param name="userName">用户名（如 PlayerA）</param>
    /// <param name="roleName">角色名（如 英雄）</param>
    public void SetNames(string userName, string roleName)
    {
        if (!isInitialized) Initialize();

        string formattedUser = string.IsNullOrEmpty(userName) ? "" : string.Format(userPrefix, userName);
        string finalRoleName = roleName ?? "";

        if (userText != null)
        {
            userText.text = formattedUser;
        }
        if (nameText != null)
        {
            nameText.text = finalRoleName;
        }

        // 手动排列两个文本的位置
        LayoutTexts();
    }

    /// <summary>
    /// 手动排列两个文本的位置（UserText 在左，NameText 在右）
    /// </summary>

    private void LayoutTexts()
    {
        if (userText == null || nameText == null) return;

        //  使用 LayoutRebuilder 强制重建 userText 的布局
        if (userText.gameObject.activeInHierarchy && userText.rectTransform != null)
        {
            // 更精确：只重建 userText 的布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(userText.rectTransform);
        }

        //  确保在重建后获取 preferredWidth
        float userWidth = userText.preferredWidth;
        Vector3 nameLocalPos = nameText.rectTransform.localPosition;

        //  关键：nameText 的 X 位置 = userWidth + spacing
        // 这会将 nameText 的左侧（因为 Pivot 是 (0, 0.5)）放在 userText 右侧 + 间距处
        nameText.rectTransform.localPosition = new Vector3(
            userWidth + spacing,
            nameLocalPos.y,
            nameLocalPos.z
        );

        Debug.Log($"[HeadPlate] 布局: UserText 宽度={userWidth:F1}px, NameText X={userWidth + spacing:F1}px");
    }

    /// <summary>
    /// 计算此头部UI所需的总垂直空间（高度）。
    /// 用于外部（如 AutoHeadPosition）调整锚点位置。
    /// </summary>
    /// <returns>所需的高度（单位：UI像素）</returns>
    public float GetRequiredHeight()
    {
        if (!isInitialized) Initialize();

        if (userText == null && nameText == null) return 0;

        float height1 = userText != null ? userText.preferredHeight : 0;
        float height2 = nameText != null ? nameText.preferredHeight : 0;

        // 返回两个文本中较高的一个，并加上一点安全间距
        return Mathf.Max(height1, height2) + 5f;
    }

    // 可选：根据角色类型设置颜色
    // public void SetRoleType(RoleType type) { ... }
}