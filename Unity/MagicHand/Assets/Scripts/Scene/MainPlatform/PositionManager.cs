using System.Collections.Generic;
using UnityEngine;

public class PositionManager : MonoBehaviour
{
    public int positionCount = 5;           // 点位总数
    public float radius = 300.0f;             // 圆形半径
    public Transform platformCenter;        // 平台中心点
    public GameObject debugSpherePrefab;    // 可选：用于可视化点位的球体

    private Dictionary<int, Vector3> positionMap = new();

    void Awake()
    {
        GeneratePositions();
    }

    void GeneratePositions()
    {
        positionMap.Clear();

        float angleStep = 360.0f / positionCount;
        for (int i = 0; i < positionCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 position = new Vector3(
                platformCenter.position.x + Mathf.Cos(angle) * radius,
                platformCenter.position.y + 30,
                platformCenter.position.z + Mathf.Sin(angle) * radius
            );

            positionMap[i] = position;

            // 可选：生成可视化球体
            if (debugSpherePrefab != null)
            {
                GameObject sphere = Instantiate(debugSpherePrefab, position, Quaternion.identity);
                sphere.name = $"Slot_{i}";
                sphere.transform.SetParent(transform); // 作为 PositionManager 的子对象
            }
        }

        Debug.Log($"[PositionManager] 已生成 {positionCount} 个点位，半径 {radius}。");
    }

    // 根据 ID 获取点位坐标
    public Vector3 GetPosition(int id)
    {
        if (positionMap.TryGetValue(id, out Vector3 pos))
        {
            return pos;
        }
        Debug.LogWarning($"[PositionManager] 未找到点位 ID: {id}");
        return Vector3.zero;
    }

    // 获取所有点位 ID（用于分配）
    public List<int> GetAllPositionIds()
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < positionCount; i++)
        {
            ids.Add(i);
        }
        return ids;
    }

    // 获取可用点位（后续用于分配）
    public int GetAvailablePositionId()
    {
        for (int i = 0; i < positionCount; i++)
        {
            if (!IsPositionOccupied(i))
            {
                return i;
            }
        }
        return -1; // 无可用点位
    }

    // 可扩展：记录点位占用状态
    private HashSet<int> occupiedPositions = new HashSet<int>();
    public void OccupyPosition(int id) => occupiedPositions.Add(id);
    public void ReleasePosition(int id) => occupiedPositions.Remove(id);
    public bool IsPositionOccupied(int id) => occupiedPositions.Contains(id);
}