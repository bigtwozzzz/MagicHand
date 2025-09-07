using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// ��Ϸ������������ȷ�� Addressables �� UIMgr ��ʼ����ɺ��ټ��� UI��
/// </summary>
public class BootLoader : MonoBehaviour
{
    [Header("��������ѡ��")]
    [SerializeField] private bool skipLogin = true;
    [SerializeField] private string debugPlayerName = "DebugPlayer";
    [SerializeField] private string debugPlayerId = "debug_001";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadBootLoader()
    {
        if (FindObjectOfType<BootLoader>() == null)
        {
            GameObject bootObj = new("BootLoader");
            bootObj.AddComponent<BootLoader>();
            DontDestroyOnLoad(bootObj);
        }
    }

    private void Awake()
    {
        // ȷ��Ψһ
        BootLoader[] loaders = FindObjectsOfType<BootLoader>();
        if (loaders.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        //  �ӳ�һ֡��ȷ������ BaseManager �ѳ�ʼ��
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return null; // ��һ֡���� BaseManager ��ɳ�ʼ��

        if (UIMgr.GetInstance() == null)
        {
            Debug.LogError("[BootLoader] UIMgr ��δ��ʼ�������� BaseManager �Ƿ���ȷʵ�ֵ�����");
            yield break;
        }

        yield return StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        Debug.Log("��BootLoader����ʼ��������...");

        // Step 1: ��ʼ�� Addressables
        var initHandle = Addressables.InitializeAsync();

        if (!initHandle.IsValid())
        {
            Debug.LogError(" Addressables ��ʼ�� handle ��Ч��");
            yield break;
        }

        bool initSuccess = false;

        initHandle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log(" Addressables ��ʼ���ɹ�");
                initSuccess = true;
            }
            else
            {
                Debug.LogError($" Addressables ��ʼ��ʧ��: {op.OperationException}");
                initSuccess = false;
            }
        };

        // ֻ����һ��ȴ�
        yield return initHandle;

        //  ��ʱ initHandle ����ɣ�initSuccess �ѱ��ص�����
        if (!initSuccess)
        {
            Debug.LogError(" Addressables ��ʼ��ʧ�ܣ���Ϸ�޷�������");
            yield break;
        }
        // Step 2: �ȴ� UIMgr ��� Canvas ��ʼ��
        Debug.Log("��BootLoader���ȴ� UIMgr ��ʼ��...");
        yield return StartCoroutine(WaitForUIMgrReady());

        // Step 3: ������������StartScene��
        Debug.Log("��BootLoader����ʼ���� StartScene...");
        bool sceneLoaded = false;
        GlobalMonoMgr.GetInstance().StartCoroutine(
                SceneMgr.GetInstance().LoadSceneAsync("StartScene", (success) =>
        {
            sceneLoaded = success;
            if (success)
            {
                Debug.Log(" �������سɹ�: StartScene");
            }
            else
            {
                Debug.LogError(" ��������ʧ��: StartScene");
            }
        }));

        // �ȴ������������
        while (!sceneLoaded)
        {
            yield return null;
        }

        // Step 4: ���ݿ�������ѡ�����Ƿ�������¼
        if (skipLogin)
        {
            Debug.Log("��BootLoader����������ģʽ��������¼��ֱ�ӽ���Ϸ");
            SkipLoginAndEnterGame();
        }
        else
        {
            Debug.Log("��BootLoader��������ʾ LoginUI...");
            UIMgr.GetInstance().ShowPanel<LoginUI>("LoginUI", E_UI_Layer.Mid, (panel) =>
            {
                if (panel != null)
                {
                    Debug.Log(" LoginUI ����ѳɹ���������ʾ��");
                }
                else
                {
                    Debug.LogError(" LoginUI ��崴��ʧ�ܣ����� prefab ȱʧ��ű�δ���ء�");
                }
            });
        }

        // Step 5: ������ɣ����� BootLoader
        OnLoadOver();
        Cleanup();
        Destroy(gameObject);
    }

    /// <summary>
    /// ��������¼ֱ�ӽ���Ϸ�ķ���
    /// </summary>
    private void SkipLoginAndEnterGame()
    {
        // ���õ�����õ����������
        DataMgr.GetInstance().SetPlayerName(debugPlayerId, debugPlayerName);
        
        // ������������¼�����ģ����¼�ɹ����Ľ�Ϊ��
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Lock_Window);
        
        // ֱ�Ӽ�����Ϸ����
        SceneMgr.GetInstance().SafeLoadScene("GamingScene", OnDebugLoadOver);
    }
    
    /// <summary>
    /// ������ģʽ�³��������ɵĻص�
    /// </summary>
    /// <param name="success">�Ƿ����ɹ�</param>
    private void OnDebugLoadOver(bool success)
    {
        if (success)
        {
            Debug.Log("��BootLoader������ģʽ��GamingScene ������ɡ�");
            
            // ��ʾ������
            UIMgr.GetInstance().ShowPanel<MainUI>("MainUI", E_UI_Layer.Mid, (panel) =>
            {
                if (panel != null)
                {
                    Debug.Log("��BootLoader������ģʽ��MainUI ���������������ʾ");
                }
                else
                {
                    Debug.LogError("��BootLoader������ģʽ��MainUI ��崴��ʧ��");
                }
            });
        }
        else
        {
            Debug.LogError("��BootLoader������ģʽ��GamingScene ����ʧ��");
        }
    }

    /// <summary>
    /// �ȴ� UIMgr ��� Canvas ��ʼ��
    /// </summary>
    private IEnumerator WaitForUIMgrReady()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (UIMgr.GetInstance().canvas == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (UIMgr.GetInstance().canvas == null)
        {
            Debug.LogError(" UIMgr ��ʼ����ʱ������ Canvas Prefab ��ַ�Ƿ���ȷ��UI/Canvas");
        }
        else
        {
            Debug.Log(" UIMgr ��ʼ����ɣ�Canvas �Ѿ�����");
        }
    }

    private void OnLoadOver()
    {
        Debug.Log(" ��Ϸ������ɣ�");
    }

    private void Cleanup()
    {
        
    }

    private void OnDestroy()
    {
        Debug.Log("BootLoader �����١�");
    }
}