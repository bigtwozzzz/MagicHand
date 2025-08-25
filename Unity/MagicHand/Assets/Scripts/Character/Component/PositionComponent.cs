// Scripts/Components/PositionComponent.cs
using UnityEngine;

public class PositionComponent : MonoBehaviour, IComponent
{
    [SerializeField] private Vector2 position = Vector2.zero;

    public Vector2 Position => position;

    public void Initialize()
    {
        transform.position = new Vector3(position.x, position.y, 0);
    }

    public void UpdateData()
    {
        // 同步 Transform
        transform.position = new Vector3(position.x, position.y, 0);
    }

    public void SetPosition(float x, float y)
    {
        position = new Vector2(x, y);
        Initialize(); // 立即更新位置
    }
}