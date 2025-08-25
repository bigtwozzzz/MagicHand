// Scripts/Components/DirectionComponent.cs
using UnityEngine;

public class DirectionComponent : MonoBehaviour, IComponent
{
    [SerializeField] private float direction = 0; // 0=об, 1=ср, 2=ио, 3=вС
    [SerializeField] private Animator animator;

    public float Direction => direction;

    public void Initialize() { }

    public void UpdateData() { }

    public void SetDirection(float dir)
    {
        direction = Mathf.Clamp(dir, 0, 3);
        if (animator != null)
            animator.SetFloat("Direction", direction);
    }
}