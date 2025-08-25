// Scripts/Components/HealthComponent.cs
using UnityEngine;

public class HealthComponent : MonoBehaviour, IComponent
{
    [SerializeField] private float maxHp = 100;
    [SerializeField] private float currentHp = 100;

    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;

    public void Initialize() { }

    public void UpdateData()
    {
        // 可用于每帧同步 UI
    }

    public void SetHealth(float current, float max)
    {
        maxHp = max;
        currentHp = Mathf.Clamp(current, 0, max);
        Debug.Log($"[Health] 血量更新: {currentHp}/{maxHp}");
        // 可触发事件 EventCenter.EventTrigger(E_EventType.Event_Health_Changed);
    }

    public void TakeDamage(float damage)
    {
        SetHealth(currentHp - damage, maxHp);
    }
}