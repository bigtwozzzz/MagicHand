// Scripts/Utils/Billboard.cs
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera != null)
        {
            // 只绕 Y 轴旋转，保持 UI 水平
            Vector3 dir = transform.position - targetCamera.transform.position;
            dir.y = 0; // 锁定 Y，避免 UI 倾斜
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }
}