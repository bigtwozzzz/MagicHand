using System.Collections;
using UnityEngine;

/// <summary>
/// ��������Դ���ؿ�����
/// ���ܣ��첽����ƽ̨Ԥ���壬���ڿ��õ�λ�����ɵ�λ��Ч�����Ч����ǵȣ�
/// ��������չΪ��Ч��UI��ǡ��������Ӿ�����
/// </summary>
public class MainScene : MonoBehaviour
{
    // Ԥ������Դ��ַ
    private const string PLATFORM_ADDRESS = "Scene/Prefabs/CircularPlatform";     // ƽ̨Ԥ����
    private const string POSITION_EFFECT_ADDRESS = "Effect/Position";      // ��λ��ЧԤ���壨ԭ����ɫ����

    [Header("����ʱʵ��")]
    [SerializeField] private GameObject platformInstance;       // ƽ̨ʵ��
    // ע�⣺��λ��Ч�����ж�����������������ã���ͨ��������ʽ����
    
    [Header("���ɿ��ƿ���")]
    [SerializeField] private bool enablePlatformGeneration = true;     // �Ƿ����ƽ̨
    [SerializeField] private bool enablePositionEffects = true;        // �Ƿ����λ����Ч
    [SerializeField] private bool enableDebugSpheres = false;          // �Ƿ������Ե���

    void Start()
    {
        // ��ʼ�� Addressables ϵͳ
        StartCoroutine(ResMgr.GetInstance().InitializeAsync());

        // �ӳٵ��ã�ȷ����ʼ�����
        Invoke(nameof(LoadPlatform), 0.1f);
    }

    /// <summary>
    /// ���ز�ʵ����ƽ̨
    /// </summary>
    public void LoadPlatform()
    {
        if (!enablePlatformGeneration)
        {
            Debug.Log("[MainScene] ƽ̨����ѽ��ã�������ء�");
            return;
        }
        
        Debug.Log("��ʼ����ƽ̨...");

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

                    Debug.Log("��MainScene��ƽ̨�Ѽ��أ�����֪ͨ DataMgr"); //  ������

                    EventCenter.GetInstance().EventTrigger(E_EventType.Event_Platform_Loaded, platformObj);
                    
                    // 设置PositionManager的调试球体开关
                    SetPositionManagerDebugMode();
                    
                    if (enablePositionEffects)
                    {
                        LoadPositionEffectAndSpawn();
                    }
                    else
                    {
                        Debug.Log("[MainScene] 位置特效生成已禁用，跳过加载。");
                    }
                }
                else
                {
                    Debug.LogError("��MainScene��ƽ̨����ʧ�ܣ�");
                }
            }
        );
    }

    /// <summary>
    /// ���ص�λ��ЧԤ���壬���������ɵ�����λ��
    /// </summary>
    private void LoadPositionEffectAndSpawn()
    {
        Debug.Log("��ʼ���ص�λ��ЧԤ����...");

        ResMgr.GetInstance().LoadAsync<GameObject>(
            address: POSITION_EFFECT_ADDRESS,
            autoRelease: false, // ����Ԥ�������ã����ڶ��ʵ����
            callback: (effectPrefab) =>
            {
                if (effectPrefab != null)
                {
                    Debug.Log("��λ��ЧԤ������سɹ���");

                    // ���ɶ����λ��Ч������ 5 ��
                    int spawnCount = 5;
                    SpawnPositionEffects(effectPrefab, spawnCount);
                }
                else
                {
                    Debug.LogError("��λ��ЧԤ�������ʧ�ܣ�");
                }
            }
        );
    }

    /// <summary>
    /// �ڿ��õ�λ������������Чʵ��
    /// </summary>
    /// <param name="effectPrefab">��ЧԤ����</param>
    /// <param name="count">Ҫ���ɵ�����</param>
    private void SpawnPositionEffects(GameObject effectPrefab, int count)
    {
        PositionManager posManager = platformInstance.GetComponentInChildren<PositionManager>();
        if (posManager == null)
        {
            Debug.LogError("δ�ҵ� PositionManager �����");
            return;
        }

        // ��ʹ�� GetAvailablePositionId()����Ϊ��Ч����Ҫ�����䡱
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = posManager.GetPosition(i);

            // ʵ������Ч
            GameObject effectInstance = Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            effectInstance.name = $"PositionEffect_{i}";

            Debug.Log($"��λ��Ч��������λ�� {i}�����꣺{spawnPos}");
        }

        Debug.Log($"[MainScene] ������ {count} ����λ��Ч��ȫ���ɼ��Ҳ�ռ�á�");
    }
    
    /// <summary>
    /// ����PositionManager�ĵ���ģʽ
    /// </summary>
    private void SetPositionManagerDebugMode()
    {
        if (platformInstance != null)
        {
            PositionManager posManager = platformInstance.GetComponentInChildren<PositionManager>();
            if (posManager != null)
            {
                posManager.enableDebugSpheres = enableDebugSpheres;
                Debug.Log($"[MainScene] ����PositionManager���Ե���忪�أ� {enableDebugSpheres}");
            }
        }
    }
    
    /// <summary>
    /// ��ѡ������ĳ����Чʵ�����贫�����ã�
    /// </summary>
    /// <param name="effectInstance">Ҫ���ٵ���Чʵ��</param>
    private void DestroyPositionEffect(GameObject effectInstance)
    {
        if (effectInstance != null)
        {
            // ����� Addressables.Instantiate ���ɵĲ���Ҫ ReleaseInstance
            // �˴�����ͨ Instantiate��ֱ�� Destroy ����
            Destroy(effectInstance);
        }
    }

    /// <summary>
    /// ��ѡ������ƽ̨�����ͷ����й�����Դ��
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