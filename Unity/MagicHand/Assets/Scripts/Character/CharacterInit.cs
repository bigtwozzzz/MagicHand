// Scripts/Components/DataSyncComponent.cs
using Character;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(LevelComponent))]
[RequireComponent(typeof(PositionComponent))]
[RequireComponent(typeof(DirectionComponent))]
[RequireComponent(typeof(StatusComponent))]
public class CharacterInit : MonoBehaviour, IComponent
{
    private HealthComponent health;
    private LevelComponent level;
    private PositionComponent position;
    private DirectionComponent direction;
    private StatusComponent status;

    private void Awake()
    {
        // 获取所有组件
        health = GetComponent<HealthComponent>();
        level = GetComponent<LevelComponent>();
        position = GetComponent<PositionComponent>();
        direction = GetComponent<DirectionComponent>();
        status = GetComponent<StatusComponent>();
    }

    public void Initialize() { }
    public void UpdateData() { }

    public void ApplyData(CharacterBase data)
    {
        if (data == null)
        {
            Debug.LogError("[DataSync] 无法应用空数据！");
            return;
        }

        // 初始化基础数据
        health.SetHealth(data.CurrentHp, data.MaxHp);
        level.SetLevel(data.Level, (int)data.Exp);
        direction.SetDirection(data.Direction);
        status.SetStatus(data.Status);
        
        // --- 设置头顶名称 ---
        if (TryGetComponent<HeadPlateComponent>(out var headPlate))
        {
            headPlate.Initialize(); // 确保 HeadPlate 初始化
            headPlate.SetNames(data.PlayerName, data.RoleName); // 设置文本

            // --- 关键：协调 AutoHeadPosition 和 HeadPlate ---
            if (TryGetComponent<AutoHeadPosition>(out var autoHead))
            {
                // 获取 HeadPlate 需要的垂直空间
                float requiredHeight = headPlate.GetRequiredHeight();
                // 将所需高度转换为世界单位（这是一个估算，需要根据你的 Canvas 的 Reference Pixels Per Unit 调整）
                // 假设 100 UI 像素 ≈ 0.1 米 (这个比例需要你根据实际项目调整)
                float heightInMeters = requiredHeight * 0.001f; // 示例转换因子

                Vector3 currentOffset = autoHead.GetAdditionalOffset();
                // 只调整 Y 轴，保留 X/Z 的手动微调
                Vector3 newOffset = new(currentOffset.x, heightInMeters, currentOffset.z);
                autoHead.SetAdditionalOffset(newOffset);
            }
        }


        //Debug.Log($"[DataSync] 已应用角色数据: {userName} ({playerId}) → {data.RoleName}");
    }
}