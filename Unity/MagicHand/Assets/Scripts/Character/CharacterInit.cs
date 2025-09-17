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
        // ��ȡ�������
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
            Debug.LogError("[DataSync] �޷�Ӧ�ÿ����ݣ�");
            return;
        }

        // ��ʼ����������
        health.SetHealth(data.CurrentHp, data.MaxHp);
        level.SetLevel(data.Level, (int)data.Exp);
        direction.SetDirection(data.Direction);
        status.SetStatus(data.Status);
        
        // --- ����ͷ������ ---
        if (TryGetComponent<HeadPlateComponent>(out var headPlate))
        {
            headPlate.Initialize(); // ȷ�� HeadPlate ��ʼ��
            headPlate.SetNames(data.PlayerName, data.RoleName); // �����ı�

            // --- �ؼ���Э�� AutoHeadPosition �� HeadPlate ---
            if (TryGetComponent<AutoHeadPosition>(out var autoHead))
            {
                // ��ȡ HeadPlate ��Ҫ�Ĵ�ֱ�ռ�
                float requiredHeight = headPlate.GetRequiredHeight();
                // ������߶�ת��Ϊ���絥λ������һ�����㣬��Ҫ������� Canvas �� Reference Pixels Per Unit ������
                // ���� 100 UI ���� �� 0.1 �� (���������Ҫ�����ʵ����Ŀ����)
                float heightInMeters = requiredHeight * 0.001f; // ʾ��ת������

                Vector3 currentOffset = autoHead.GetAdditionalOffset();
                // ֻ���� Y �ᣬ���� X/Z ���ֶ�΢��
                Vector3 newOffset = new(currentOffset.x, heightInMeters, currentOffset.z);
                autoHead.SetAdditionalOffset(newOffset);
            }
        }


        //Debug.Log($"[DataSync] ��Ӧ�ý�ɫ����: {userName} ({playerId}) �� {data.RoleName}");
    }
}