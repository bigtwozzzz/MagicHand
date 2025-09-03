using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 资源加载模块（基于 Addressables）
/// 功能：
/// 1. 安全异步加载（自动释放/手动释放可选）
/// 2. 实例化管理（Addressables.ReleaseInstance）
/// 3. 避免 Resources.UnloadUnusedAssets 误伤 UI
/// 4. 错误处理与日志
/// 5. 不推荐同步加载（仅用于调试）
/// </summary>
public class ResMgr : BaseManager<ResMgr>
{
    // ========================
    // 异步加载资源（推荐）
    // autoRelease: 是否在加载完成后自动释放 handle
    // ========================
    public void LoadAsync<T>(string address, UnityAction<T> callback, bool autoRelease = true)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[ResMgr] 加载地址为空！");
            callback?.Invoke(default);
            return;
        }

        var handle = Addressables.LoadAssetAsync<T>(address);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                callback?.Invoke(op.Result);
            }
            else
            {
                Debug.LogError($"[ResMgr] 加载失败: {address}, 错误: {op.OperationException}");
                callback?.Invoke(default);
            }

            // 自动释放（除非调用者需要长期持有）
            if (autoRelease)
            {
                Addressables.Release(handle);
            }
        };
    }

    // ========================
    // 同步加载（仅用于启动或调试，生产环境避免使用）
    // ========================
    public T Load<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[ResMgr] 同步加载失败：key 为空！");
            return null;
        }

        // 警告：同步加载会阻塞主线程
        Debug.LogWarning($"[ResMgr] 同步加载资源: {key}。建议使用异步方式。");

        var op = Addressables.LoadAssetAsync<T>(key);
        op.WaitForCompletion();

        if (op.Status == AsyncOperationStatus.Succeeded)
        {
            T result = op.Result;

            if (result is GameObject prefab)
            {
                // 如果是 Prefab，实例化后释放原始资源
                GameObject instance = GameObject.Instantiate(prefab);
                Addressables.Release(op); // 释放 prefab 资源
                return instance as T;
            }
            else
            {
                // 非 GameObject，直接返回（调用者需手动 Release）
                return result;
            }
        }
        else
        {
            Debug.LogError($"[ResMgr] 同步加载失败: {key}, 错误: {op.OperationException}");
            Addressables.Release(op);
            return null;
        }
    }

    // ========================
    // 异步加载并实例化（用于 Prefab）
    // ========================
    public void LoadAndInstantiateAsync(
        string key,
        Transform parent = null,
        bool instantiateInWorldSpace = false,
        UnityAction<GameObject> callback = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[ResMgr] 实例化失败：key 为空！");
            callback?.Invoke(null);
            return;
        }

        var handle = Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                callback?.Invoke(op.Result);
            }
            else
            {
                Debug.LogError($"[ResMgr] 实例化失败: {key}, 错误: {op.OperationException}");
                callback?.Invoke(null);
            }

            //  Addressables 会自动管理 InstantiateAsync 的 handle
            // 不需要手动 Release(handle)
        };
    }

    // ========================
    // 释放 Addressables 加载的资源（非 GameObject）
    // 例如：Sprite、AudioClip、ScriptableObject 等
    // ========================
    public void Release<T>(T asset) where T : class
    {
        if (asset != null)
        {
            Addressables.Release(asset);
        }
    }

    public void ReleaseAsync(AsyncOperationHandle handle)
    {
        Addressables.Release(handle);
    }

    // ========================
    // 销毁由 Addressables 实例化的 GameObject
    // 必须调用此方法，否则内存泄漏！
    // ========================
    public void ReleaseInstance(GameObject instance)
    {
        if (instance != null)
        {
            Addressables.ReleaseInstance(instance);
        }
    }

    // ========================
    // 资源清理（仅调试使用！避免误卸载 UI 资源）
    // ========================
    [ContextMenu("强制清理未使用资源（调试用）")]
    public void ForceCleanUnusedAssets()
    {
        Debug.LogWarning("[ResMgr] 正在调用 Resources.UnloadUnusedAssets()，可能影响 UI 显示！");
        var asyncOp = Resources.UnloadUnusedAssets();
        asyncOp.completed += (op) =>
        {
            Debug.Log("[ResMgr] 未使用的资源已卸载。");
            System.GC.Collect();
        };
    }

    // ========================
    // 初始化 Addressables（建议在启动时调用）
    // ========================
    public IEnumerator InitializeAsync()
    {
        var init = Addressables.InitializeAsync();
        while (!init.IsDone)
        {
            yield return null;
        }

        if (init.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("Addressables 初始化完成");
        }
        else
        {
            Debug.LogError("Addressables 初始化失败: " + init.OperationException);
        }
    }

    // ========================
    // 应用退出清理
    // ========================
    //protected override void OnApplicationQuit()
    //{
    //    // 可选：清理未使用资源
    //    // 但确保所有 Addressables 资源已 Release
    //    ForceCleanUnusedAssets();
    //}

    // ========================
    // 调试工具
    // ========================
    [ContextMenu("打印 ResMgr 状态")]
    private void LogStatus()
    {
        Debug.Log("[ResMgr] 正在运行，Addressables 系统正常。");
    }
}