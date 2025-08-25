// Scripts/Components/LevelComponent.cs
using UnityEngine;

public class LevelComponent : MonoBehaviour, IComponent
{
    [SerializeField] private int level = 1;
    [SerializeField] private int exp = 0;

    public int Level => level;
    public int Exp => exp;

    public void Initialize() { }

    public void UpdateData() { }

    public void SetLevel(int lvl, int experience)
    {
        level = Mathf.Max(1, lvl);
        exp = Mathf.Max(0, experience);
        Debug.Log($"[Level] 等级更新: {level}, 经验: {exp}");
    }
}