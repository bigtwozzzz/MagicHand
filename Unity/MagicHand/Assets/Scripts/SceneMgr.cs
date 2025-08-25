// SceneMgr.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换管理器（防卡顿 + 资源释放）
/// </summary>
public class SceneMgr : BaseManager<SceneMgr>
{
    /// <summary>
    /// 安全加载场景（通过 GlobalMonoMgr 协程）
    /// </summary>
    public void SafeLoadScene(string name, UnityAction<bool> onLoaded = null)
    {
        GlobalMonoMgr.GetInstance().SafeStartCoroutine(LoadSceneAsync(name, onLoaded));
    }

    /// <summary>
    /// 异步加载场景 + 防卡顿资源清理
    /// </summary>
    public IEnumerator LoadSceneAsync(string name, UnityAction<bool> onLoaded = null)
    {
        Debug.Log($"【SceneMgr】开始加载场景: {name}");

        // 1. 检查场景名称
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("【SceneMgr】场景名称为空！");
            onLoaded?.Invoke(false);
            yield break;
        }

        // 2. 验证是否在 Build Settings 中
        if (!IsSceneInBuildSettings(name))
        {
            Debug.LogError($"【SceneMgr】场景未在 Build Settings 中：{name}");
            onLoaded?.Invoke(false);
            yield break;
        }

        // 3. 开始加载场景
        AsyncOperation asyncOp = null;
        try
        {
            asyncOp = SceneManager.LoadSceneAsync(name);
        }
        catch (Exception e)
        {
            Debug.LogError($"【SceneMgr】加载场景异常: {e.Message}\n{e.StackTrace}");
            onLoaded?.Invoke(false);
            yield break;
        }

        if (asyncOp == null)
        {
            Debug.LogError($"【SceneMgr】LoadSceneAsync 返回 null！请检查场景名和 Build Settings: {name}");
            onLoaded?.Invoke(false);
            yield break;
        }

        Debug.Log($"【SceneMgr】异步操作创建成功，progress={asyncOp.progress:F2}");

        // 4. 控制激活（用于进度控制）
        asyncOp.allowSceneActivation = false;

        float timer = 0f;
        const float MaxWaitTime = 15f; // 更宽容的超时
        float waitTime = 0f;

        while (!asyncOp.isDone)
        {
            waitTime += Time.deltaTime;
            if (waitTime > MaxWaitTime)
            {
                Debug.LogWarning($"【SceneMgr】加载超时({MaxWaitTime}s)，强制激活场景");
                asyncOp.allowSceneActivation = true;
                break;
            }

            timer += Time.unscaledDeltaTime; // 使用 unscaled 防止 Time.timeScale 影响

            // 平滑估算进度（0.9 来自 asyncOp.progress，0.1 来自时间）
            float progress = Mathf.Clamp01(asyncOp.progress * 0.9f + timer * 0.1f);

            // 降低日志频率（每 0.2 秒一次）
            if (Mathf.FloorToInt(timer * 5) % 1 == 0) // 每 0.2s 打一次
            {
                Debug.Log($"[SceneMgr] 加载中 | Progress: {asyncOp.progress:F2} | Est: {progress:F2}");
            }

            // 触发 UI 进度更新
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_LoadScene_Progress, progress);

            // 当加载到 90% 时，允许激活
            if (asyncOp.progress >= 0.9f && !asyncOp.allowSceneActivation)
            {
                asyncOp.allowSceneActivation = true;
                Debug.Log("【SceneMgr】allowSceneActivation = true，场景即将激活");
            }

            yield return null; // 分帧
        }

        // 5. 场景激活完成
        Debug.Log($"【SceneMgr】场景 '{name}' 加载完成！");

        // 最终进度 100%
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_LoadScene_Progress, 1f);

        // 给 UI 一点时间刷新（避免最后 0.1 秒跳变）
        yield return new WaitForEndOfFrame();

        // 6. 延迟资源清理（分帧执行，防卡）
        //yield return GlobalMonoMgr.GetInstance().StartCoroutine(FinalCleanupAsync());

        // 7. 回调成功
        Debug.Log($"【SceneMgr】场景 '{name}' 加载成功");
        onLoaded?.Invoke(true);
    }

    /// <summary>
    /// 分帧执行资源清理（避免卡顿）
    /// </summary>
    private IEnumerator FinalCleanupAsync()
    {
        Debug.Log("【SceneMgr】开始延迟资源清理...");

        // 延迟 0.5 秒，确保 UI 完全渲染
        yield return new WaitForSecondsRealtime(0.5f);

        //  第一步：卸载未使用资源（耗时操作，但只能同步）
        Debug.Log("【SceneMgr】开始 Resources.UnloadUnusedAssets...");
        yield return new WaitForEndOfFrame(); // 让 UI 先渲染一帧

       // var unloadOp = Resources.UnloadUnusedAssets();
        //while (!unloadOp.isDone)
        //{
        //    yield return null; // 分帧等待卸载完成
        //}

        Debug.Log("【SceneMgr】UnloadUnusedAssets 完成");

        //  第二步：GC 回收（可选，分帧提示）
        Debug.Log("【SceneMgr】准备执行 GC.Collect...");
        yield return new WaitForSecondsRealtime(0.2f); // 小延迟

        // 在低优先级时执行 GC（避免卡 UI）
       // System.GC.Collect();

        Debug.Log("【SceneMgr】GC.Collect 完成，资源清理结束。");
    }

    /// <summary>
    /// 检查场景是否在 Build Settings 中
    /// </summary>
    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// （可选）加载前释放旧资源（如对象池、UI 缓存等）
    /// </summary>
    private IEnumerator UnloadOldResources()
    {
        Debug.Log("【SceneMgr】释放旧资源（可选）...");

        // 示例：通知 UI 管理器释放
        // UIMgr.Instance.ReleaseAllUIResources();

        yield return null;
    }
}