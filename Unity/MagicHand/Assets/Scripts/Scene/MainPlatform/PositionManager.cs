using System.Collections.Generic;
using UnityEngine;

public class PositionManager : MonoBehaviour
{
    public int positionCount = 5;           // 位置数量
    public float radius = 300.0f;             // 圆形半径
    public Transform platformCenter;        // 平台中心点
    public GameObject debugSpherePrefab;    // 可选：用于可视化的位置球体
    
    [Header("调试控制")]
    public bool enableDebugSpheres = false;  // 是否生成调试球体

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
            Vector3 position = new(
                platformCenter.position.x + Mathf.Cos(angle) * radius,
                platformCenter.position.y + 30,
                platformCenter.position.z + Mathf.Sin(angle) * radius
            );

            positionMap[i] = position;

            // 可选：生成可视化球体
            if (enableDebugSpheres && debugSpherePrefab != null)
            {
                GameObject sphere = Instantiate(debugSpherePrefab, position, Quaternion.identity);
                sphere.name = $"Slot_{i}";
                sphere.transform.SetParent(transform); // 设为 PositionManager 的子对象
                Debug.Log($"[PositionManager] 生成调试球体: Slot_{i}");
            }
        }

        Debug.Log($"[PositionManager] ������ {positionCount} ����λ���뾶 {radius}��");
    }

    // ���� ID ��ȡ��λ����
    public Vector3 GetPosition(int id)
    {
        if (positionMap.TryGetValue(id, out Vector3 pos))
        {
            return pos;
        }
        Debug.LogWarning($"[PositionManager] δ�ҵ���λ ID: {id}");
        return Vector3.zero;
    }

    // ��ȡ���е�λ ID�����ڷ��䣩
    public List<int> GetAllPositionIds()
    {
        List<int> ids = new();
        for (int i = 0; i < positionCount; i++)
        {
            ids.Add(i);
        }
        return ids;
    }

    // ��ȡ���õ�λ���������ڷ��䣩
    public int GetAvailablePositionId()
    {
        for (int i = 0; i < positionCount; i++)
        {
            if (!IsPositionOccupied(i))
            {
                return i;
            }
        }
        return -1; // �޿��õ�λ
    }

    // ����չ����¼��λռ��״̬
    private HashSet<int> occupiedPositions = new();
    public void OccupyPosition(int id) => occupiedPositions.Add(id);
    public void ReleasePosition(int id) => occupiedPositions.Remove(id);
    public bool IsPositionOccupied(int id) => occupiedPositions.Contains(id);
}