// Scripts/Utils/AutoHeadPosition.cs
using UnityEngine;

/// <summary>
/// 自动计算角色模型的“头顶”位置，用于挂载名称、血条等 UI。
/// 支持自动检测模型高度，或手动指定偏移。
/// 提供公共方法以动态调整最终偏移。
/// </summary>
public class AutoHeadPosition : MonoBehaviour
{
    [Header("检测模式")]
    [Tooltip("自动检测：使用模型的包围盒计算头顶高度")]
    [SerializeField] private bool autoDetect = true;

    [Tooltip("手动模式：从角色脚底起算的偏移量（单位：米）")]
    [SerializeField] private float manualOffset = 2.0f;

    [Header("自动检测设置")]
    [Tooltip("用于检测高度的模型物体，留空则使用自身或子物体的 Renderer")]
    [SerializeField] private Renderer referenceRenderer;

    [Tooltip("最终偏移的额外调整值（可微调），可通过 SetAdditionalOffset 动态修改")]
    [SerializeField] private Vector3 additionalOffset = new Vector3(0, 0.1f, 0);

    private bool isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化位置计算
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        // 如果未指定参考模型，尝试使用子物体的 Renderer
        if (referenceRenderer == null)
        {
            referenceRenderer = GetComponentInChildren<Renderer>(includeInactive: false);
            if (referenceRenderer == null)
            {
                Debug.Log($"[AutoHeadPosition] 未找到 Renderer，将使用默认偏移。", this);
                // 即使没有 Renderer，也设置一个默认位置并标记为已初始化
                transform.localPosition = new Vector3(additionalOffset.x, manualOffset + additionalOffset.y, additionalOffset.z);
                isInitialized = true;
                return;
            }
        }

        // 获取模型的包围盒
        Bounds bounds = referenceRenderer.bounds;
        float height = bounds.size.y;

        // 计算自动偏移 (保守取 height * 0.9)
        float autoOffset = height * 0.9f;

        // 应用自动或手动偏移
        float baseY = autoDetect ? autoOffset : manualOffset;

        // 计算最终位置
        Vector3 finalPosition = new Vector3(
            additionalOffset.x,
            baseY + additionalOffset.y,
            additionalOffset.z
        );

        transform.localPosition = finalPosition;
        isInitialized = true;

        Debug.Log($"[AutoHeadPosition] 已设置头顶位置: Y={finalPosition.y:F2}m " +
                 $"(基础偏移: {(autoDetect ? autoOffset : manualOffset):F2}m, 额外偏移: {additionalOffset.y:F2}m)", this);
    }

    /// <summary>
    /// 动态设置额外偏移量，用于适应 UI 大小变化。
    /// 调用此方法后会重新计算位置。
    /// </summary>
    /// <param name="offset">新的额外偏移</param>
    public void SetAdditionalOffset(Vector3 offset)
    {
        additionalOffset = offset;
        if (isInitialized)
        {
            Initialize(); // 重新计算位置
        }
        // 如果尚未初始化，会在 Awake 时生效
    }

    /// <summary>
    /// 获取当前的额外偏移量。
    /// </summary>
    /// <returns>额外偏移向量</returns>
    public Vector3 GetAdditionalOffset() => additionalOffset;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            isInitialized = false; // 强制重新初始化以预览
            Initialize();
        }
    }
#endif
}