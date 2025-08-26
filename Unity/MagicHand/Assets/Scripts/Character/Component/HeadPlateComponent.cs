// Scripts/Components/HeadPlateComponent.cs
using TMPro;
using UnityEngine;

public class HeadPlateComponent : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI userText;  // 可在 Inspector 拖入
    [SerializeField] private TextMeshProUGUI nameText;  // 可在 Inspector 拖入

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // 确保它是一个 World Space UI 元素
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchorMin = Vector2.one * 0.5f;
            rectTransform.anchorMax = Vector2.one * 0.5f;
            rectTransform.pivot = Vector2.one * 0.5f;
            rectTransform.localScale = Vector3.one;
        }
    }

    public void Initialize()
    {
        if (userText != null) userText.enabled = true;
        if (nameText != null) nameText.enabled = true;
    }

    public void SetNames(string playerName, string roleName)
    {
        if (userText != null) userText.text = playerName;
        if (nameText != null) nameText.text = roleName;
    }

    /// <summary>
    /// 获取需要的总高度（用于 AutoHeadPosition 动态调整）
    /// </summary>
    public float GetRequiredHeight()
    {
        float height = 0;
        if (userText != null) height += 30; // 估算每行高度
        if (nameText != null) height += 30;
        return height; // 返回像素值
    }
}