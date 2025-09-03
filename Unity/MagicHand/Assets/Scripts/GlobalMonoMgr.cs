using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mono控制基类：为非MonoBehaviour对象提供协程、帧更新、延迟调用等功能
/// </summary>
public class MonoControl : MonoBehaviour
{
    /// <summary>
    /// 帧更新事件，供外部注册Update逻辑
    /// </summary>
    public event Action EventUpdate;

    public void ClearUpdateEvents()
    {
        EventUpdate = null;
    }

    //  新增：追踪所有正在运行的协程
    private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();

    private void Awake()
    {
        // 防止重复
        var existing = FindObjectsOfType<MonoControl>();
        if (existing.Length > 1)
        {
            Debug.LogWarning("发现多个 MonoControl 实例，正在销毁重复项。");
            DestroyImmediate(this.gameObject);
            return;
        }

        // 跨场景不销毁
        DontDestroyOnLoad(this.gameObject);
    }

    private void Update()
    {
        EventUpdate?.Invoke();
    }

    //  安全启动协程并加入追踪列表
    public Coroutine StartCoroutineTracked(IEnumerator routine)
    {
        if (routine == null) return null;
        var coroutine = StartCoroutine(routine);
        _runningCoroutines.Add(coroutine);
        return coroutine;
    }

    //  停止指定协程（Coroutine 版本）
    public void StopCoroutineTracked(Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            _runningCoroutines.Remove(routine);
        }
    }

    //  停止指定协程（IEnumerator 版本）
    public void StopCoroutineTracked(IEnumerator routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            // 注意：无法从 _runningCoroutines 移除对应的 Coroutine（无映射），但 StopCoroutine 会生效
        }
    }

    //  停止所有追踪的协程
    public void StopAllCoroutinesTracked()
    {
        foreach (var coroutine in _runningCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _runningCoroutines.Clear();
    }

    //  销毁时自动清理
    private void OnDestroy()
    {
        StopAllCoroutinesTracked();
    }
}

/// <summary>
/// 全局Mono管理器：单例模式，统一管理协程、帧更新、延迟调用等
/// </summary>
public class GlobalMonoMgr : BaseManager<GlobalMonoMgr>
{
    private MonoControl _monoControl = null;

    protected override void Awake()
    {
        base.Awake(); // 确保单例赋值

        if (_monoControl != null) return;

        // 检查是否已有 MONO_MAIN 存在（防止编辑器模式残留）
        GameObject existingObj = GameObject.Find("MONO_MAIN");
        if (existingObj != null)
        {
            Debug.LogWarning("发现残留 MONO_MAIN 对象，正在销毁...");
            DestroyImmediate(existingObj);
        }

        GameObject obj = new("MONO_MAIN");
        _monoControl = obj.AddComponent<MonoControl>();
        DontDestroyOnLoad(obj);

        Debug.Log("[GlobalMonoMgr] 初始化完成，MONO_MAIN 已创建。");
    }

    /// <summary>
    /// 获取底层 MonoControl 对象（用于挂载其他脚本）
    /// </summary>
    public MonoControl ComponentControl => _monoControl;

    /// <summary>
    /// 添加帧更新监听
    /// </summary>
    public void AddUpdateListener(Action action)
    {
        if (_monoControl != null && action != null)
        {
            _monoControl.EventUpdate += action;
        }
    }

    /// <summary>
    /// 移除帧更新监听
    /// </summary>
    public void RemoveUpdateListener(Action action)
    {
        if (_monoControl != null && action != null)
        {
            _monoControl.EventUpdate -= action;
        }
    }

    #region 协程与延迟调用封装

    //  主力 API：支持追踪的 IEnumerator 协程
    public Coroutine StartCoroutine(IEnumerator routine) =>
        _monoControl != null ? _monoControl.StartCoroutineTracked(routine) : null;

    //  不推荐使用 string 版本（无法追踪），保留兼容性
    [Obsolete("建议使用 IEnumerator 版本的协程，便于管理")]
    public Coroutine StartCoroutine(string methodName) =>
        _monoControl != null ? _monoControl.StartCoroutine(methodName) : null;

    [Obsolete("建议使用 IEnumerator 版本的协程")]
    public Coroutine StartCoroutine(string methodName, [System.Runtime.InteropServices.DefaultParameterValue(null)] object value) =>
        _monoControl != null ? _monoControl.StartCoroutine(methodName, value) : null;

    //  停止协程：优先使用 Coroutine 句柄（可从列表移除）
    public void StopCoroutine(Coroutine routine)
    {
        if (_monoControl != null && routine != null)
        {
            _monoControl.StopCoroutineTracked(routine);
        }
    }

    //  停止协程：IEnumerator 版本（只能停止，无法从列表移除）
    public void StopCoroutine(IEnumerator routine)
    {
        if (_monoControl != null && routine != null)
        {
            _monoControl.StopCoroutineTracked(routine);
        }
    }

    //  停止所有协程（推荐在清理时调用）
    public void StopAllCoroutines()
    {
        if (_monoControl != null)
        {
            _monoControl.StopAllCoroutinesTracked();
        }
    }

    #endregion

    #region Invoke 封装（无需修改）

    public void CancelInvoke() => _monoControl?.CancelInvoke();

    public void CancelInvoke(string methodName) => _monoControl?.CancelInvoke(methodName);

    public void Invoke(string methodName, float time) => _monoControl?.Invoke(methodName, time);

    public void InvokeRepeating(string methodName, float time, float repeatRate) =>
        _monoControl?.InvokeRepeating(methodName, time, repeatRate);

    public bool IsInvoking() => _monoControl?.IsInvoking() ?? false;

    public bool IsInvoking(string methodName) => _monoControl?.IsInvoking(methodName) ?? false;

    #endregion

    #region 安全协程启动（主线程队列）

    /// <summary>
    /// 安全启动协程（通过主线程调度器，避免跨线程问题）
    /// </summary>
    public void SafeStartCoroutine(IEnumerator routine)
    {
        if (routine == null) return;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (_monoControl != null)
            {
                _monoControl.StartCoroutineTracked(routine);
            }
            else
            {
                Debug.LogWarning("SafeStartCoroutine: MonoControl 已被销毁，无法启动协程。");
            }
        });
    }

    #endregion

    #region 资源释放与清理

    /// <summary>
    /// 彻底清理所有资源（用于游戏重启、退出、模块卸载）
    /// </summary>
    public void Shutdown()
    {
        if (_monoControl == null) return;

        _monoControl.StopAllCoroutinesTracked(); //  停止所有协程
        _monoControl.CancelInvoke();
        _monoControl.ClearUpdateEvents();

        if (_monoControl.gameObject != null)
        {
            Destroy(_monoControl.gameObject);
        }

        _monoControl = null;

        // 清理单例引用
       // base.OnDestroy();

        Debug.Log("[GlobalMonoMgr] 已关闭，资源释放。");
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }


    #endregion
}