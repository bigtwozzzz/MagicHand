// Scripts/Components/StatusComponent.cs
using UnityEngine;

public class StatusComponent : MonoBehaviour, IComponent
{
    [SerializeField] private Common.Status status = 0; // 0=Idle, 1=Moving, 2=Combat
    [SerializeField] private Animator animator;

    public Common.Status Status => status;

    public void Initialize() { }

    public void UpdateData() { }

    public void SetStatus(Common.Status s)
    {
        status = s;
        if (animator == null) return;

        switch (s)
        {
            case Common.Status.Idle:
                animator.SetBool("IsMoving", false);
                break;
            case Common.Status.Casting:
                animator.SetBool("IsMoving", true);
                break;
        }
    }
}