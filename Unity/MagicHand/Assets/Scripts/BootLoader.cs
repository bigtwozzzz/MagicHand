using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 游戏启动加载器（确保 Addressables 和 UIMgr 初始化完成后再加载 UI）
/// </summary>
public class BootLoader : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadBootLoader()
    {
        if (FindObjectOfType<BootLoader>() == null)
        {
            GameObject bootObj = new GameObject("BootLoader");
            bootObj.AddComponent<BootLoader>();
            DontDestroyOnLoad(bootObj);
        }
    }

    private void Awake()
    {
        // 确保唯一
        BootLoader[] loaders = FindObjectsOfType<BootLoader>();
        if (loaders.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        //  延迟一帧，确保所有 BaseManager 已初始化
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return null; // 等一帧，让 BaseManager 完成初始化

        if (UIMgr.GetInstance() == null)
        {
            Debug.LogError("[BootLoader] UIMgr 尚未初始化，请检查 BaseManager 是否正确实现单例。");
            yield break;
        }

        yield return StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        Debug.Log("【BootLoader】开始启动流程...");

        // Step 1: 初始化 Addressables
        var initHandle = Addressables.InitializeAsync();

        if (!initHandle.IsValid())
        {
            Debug.LogError(" Addressables 初始化 handle 无效！");
            yield break;
        }

        bool initSuccess = false;

        initHandle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log(" Addressables 初始化成功");
                initSuccess = true;
            }
            else
            {
                Debug.LogError($" Addressables 初始化失败: {op.OperationException}");
                initSuccess = false;
            }
        };

        // 只用这一句等待
        yield return initHandle;

        //  此时 initHandle 已完成，initSuccess 已被回调设置
        if (!initSuccess)
        {
            Debug.LogError(" Addressables 初始化失败，游戏无法启动。");
            yield break;
        }
        // Step 2: 等待 UIMgr 完成 Canvas 初始化
        Debug.Log("【BootLoader】等待 UIMgr 初始化...");
        yield return StartCoroutine(WaitForUIMgrReady());

        // Step 3: 加载主场景（StartScene）
        Debug.Log("【BootLoader】开始加载 StartScene...");
        bool sceneLoaded = false;
        GlobalMonoMgr.GetInstance().StartCoroutine(
                SceneMgr.GetInstance().LoadSceneAsync("StartScene", (success) =>
        {
            sceneLoaded = success;
            if (success)
            {
                Debug.Log(" 场景加载成功: StartScene");
            }
            else
            {
                Debug.LogError(" 场景加载失败: StartScene");
            }
        }));

        // 等待场景加载完成
        while (!sceneLoaded)
        {
            yield return null;
        }

        // Step 4: 显示 LoginUI 面板
        Debug.Log("【BootLoader】尝试显示 LoginUI...");
        UIMgr.GetInstance().ShowPanel<LoginUI>("LoginUI", E_UI_Layer.Mid, (panel) =>
        {
            if (panel != null)
            {
                Debug.Log(" LoginUI 面板已成功创建并显示！");
            }
            else
            {
                Debug.LogError(" LoginUI 面板创建失败，可能 prefab 缺失或脚本未挂载。");
            }
        });

        // Step 5: 启动完成，销毁 BootLoader
        OnLoadOver();
        Cleanup();
        Destroy(gameObject);
    }

    /// <summary>
    /// 等待 UIMgr 完成 Canvas 初始化
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
            Debug.LogError(" UIMgr 初始化超时！请检查 Canvas Prefab 地址是否正确：UI/Canvas");
        }
        else
        {
            Debug.Log(" UIMgr 初始化完成，Canvas 已就绪。");
        }
    }

    private void OnLoadOver()
    {
        Debug.Log(" 游戏启动完成！");
    }

    private void Cleanup()
    {
        // 如果你有事件系统，记得取消订阅
        // EventCenter.RemoveListener("OnGameStart", OnLoadOver);
    }

    private void OnDestroy()
    {
        Debug.Log("BootLoader 已销毁。");
    }
}