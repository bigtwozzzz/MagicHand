using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换管理器
/// </summary>
public class SceneMgr : BaseManager<SceneMgr>
{
    public void SafeLoadScene(string name, Action loadOverDo)
    {
        GlobalMonoMgr.GetInstance().SafeStartCoroutine(LoadSceneAsync(name, loadOverDo));
    }
    /// <summary>
    /// 提供给外部加载场景的方法
    /// </summary>
    /// <param name="name">场景名</param>
    /// <param name="loadOverDo">加载完成后做啥</param>
    public void LoadScene(string name, Action loadOverDo)
    {
        //开启协程加载
        GlobalMonoMgr.GetInstance().StartCoroutine(LoadSceneAsync(name, loadOverDo));
    }

    /// <summary>
    /// 协程异步加载场景
    /// </summary>
    /// <param name="name"></param>
    /// <param name="loadOverDo"></param>
    /// <returns></returns>
    private IEnumerator LoadSceneAsync(string name, Action loadOverDo)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("场景名称为空！");
            yield break;
        }

        AsyncOperation async = SceneManager.LoadSceneAsync(name);
        if (async == null)
        {
            Debug.LogError($"无法加载场景：{name}，请检查是否在 Build Settings 中");
            yield break;
        }

        async.allowSceneActivation = false;

        float timer = 0f;
        while (!async.isDone)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(async.progress * 0.9f + timer * 0.1f);
            Debug.Log($"加载进度: {progress:P}"); // 调试用

            EventCenter.GetInstance().EventTrigger(E_EventType.Event_LoadScene_Progress, progress);

            if (async.progress >= 0.9f && !async.allowSceneActivation)
            {
                async.allowSceneActivation = true;
            }

            yield return null;
        }

        Debug.Log("场景加载完成！");
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_LoadScene_Progress, 1f);
        yield return new WaitForSeconds(0.1f); // 给 UI 一点时间刷新
        loadOverDo?.Invoke();
    }
}
