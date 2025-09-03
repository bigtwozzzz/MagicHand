using UnityEngine;

/// <summary>
/// 主场景资源加载控制器
/// 功能：异步加载平台预制体，并在可用点位上生成点位特效（如光效、标记等）
/// 后续可扩展为特效、UI标记、动画等视觉反馈
/// </summary>
public class MainScene : MonoBehaviour
{
    // 预制体资源地址
    private const string PLATFORM_ADDRESS = "Scene/Prefabs/CircularPlatform";     // 平台预制体
    private const string POSITION_EFFECT_ADDRESS = "Effect/Position";      // 点位特效预制体（原“角色”）

    [Header("运行时实例")]
    [SerializeField] private GameObject platformInstance;       // 平台实例
    // 注意：点位特效可能有多个，不单独保存引用，可通过其他方式管理

    void Start()
    {
        // 初始化 Addressables 系统
        StartCoroutine(ResMgr.GetInstance().InitializeAsync());

        // 延迟调用，确保初始化完成
        Invoke(nameof(LoadPlatform), 0.1f);
    }

    /// <summary>
    /// 加载并实例化平台
    /// </summary>
    public void LoadPlatform()
    {
        Debug.Log("开始加载平台...");

        ResMgr.GetInstance().LoadAndInstantiateAsync(
            key: PLATFORM_ADDRESS,
            parent: null,
            instantiateInWorldSpace: true,
            callback: (platformObj) =>
            {
                if (platformObj != null)
                {
                    platformInstance = platformObj;
                    platformInstance.name = "CircularPlatform_Instance";

                    Debug.Log("【MainScene】平台已加载，即将通知 DataMgr"); //  加这行

                    EventCenter.GetInstance().EventTrigger(E_EventType.Event_Platform_Loaded, platformObj);
                    LoadPositionEffectAndSpawn();
                }
                else
                {
                    Debug.LogError("【MainScene】平台加载失败！");
                }
            }
        );
    }

    /// <summary>
    /// 加载点位特效预制体，并批量生成到可用位置
    /// </summary>
    private void LoadPositionEffectAndSpawn()
    {
        Debug.Log("开始加载点位特效预制体...");

        ResMgr.GetInstance().LoadAsync<GameObject>(
            address: POSITION_EFFECT_ADDRESS,
            autoRelease: false, // 保留预制体引用，用于多次实例化
            callback: (effectPrefab) =>
            {
                if (effectPrefab != null)
                {
                    Debug.Log("点位特效预制体加载成功！");

                    // 生成多个点位特效，例如 5 个
                    int spawnCount = 5;
                    SpawnPositionEffects(effectPrefab, spawnCount);
                }
                else
                {
                    Debug.LogError("点位特效预制体加载失败！");
                }
            }
        );
    }

    /// <summary>
    /// 在可用点位上批量生成特效实例
    /// </summary>
    /// <param name="effectPrefab">特效预制体</param>
    /// <param name="count">要生成的数量</param>
    private void SpawnPositionEffects(GameObject effectPrefab, int count)
    {
        PositionManager posManager = platformInstance.GetComponentInChildren<PositionManager>();
        if (posManager == null)
        {
            Debug.LogError("未找到 PositionManager 组件！");
            return;
        }

        // 不使用 GetAvailablePositionId()，因为特效不需要“分配”
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = posManager.GetPosition(i);

            // 实例化特效
            GameObject effectInstance = Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            effectInstance.name = $"PositionEffect_{i}";

            Debug.Log($"点位特效已生成于位置 {i}，坐标：{spawnPos}");
        }

        Debug.Log($"[MainScene] 已生成 {count} 个点位特效，全部可见且不占用。");
    }
    /// <summary>
    /// 可选：销毁某个特效实例（需传入引用）
    /// </summary>
    /// <param name="effectInstance">要销毁的特效实例</param>
    private void DestroyPositionEffect(GameObject effectInstance)
    {
        if (effectInstance != null)
        {
            // 如果是 Addressables.Instantiate 生成的才需要 ReleaseInstance
            // 此处是普通 Instantiate，直接 Destroy 即可
            Destroy(effectInstance);
        }
    }

    /// <summary>
    /// 可选：销毁平台（会释放所有关联资源）
    /// </summary>
    private void DestroyPlatform()
    {
        if (platformInstance != null)
        {
            ResMgr.GetInstance().ReleaseInstance(platformInstance);
            platformInstance = null;
        }
    }
}