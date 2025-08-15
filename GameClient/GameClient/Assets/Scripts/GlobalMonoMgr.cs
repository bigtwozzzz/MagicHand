using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System;

/// <summary>
/// Mono控制基类 主要用于给没有继承Mono的对象提供 
/// 开启协程
/// 延迟函数
/// 帧更新
/// 等等
/// </summary>
public class MonoControl : MonoBehaviour
{
    /// <summary>
    /// 帧更新事件 提供给没有继承mono对象能够帧更新的事件
    /// 也可以减少Update分布在不同Mono中的数量 统一在此处管理
    /// </summary>
    [HideInInspector]
    public event Action EventUpdate;

    void Awake()
    {
       
        //过场景不移除
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        EventUpdate?.Invoke();
    }

}
/// <summary>
/// 公共Mono控制对象 管理器 用于统一处理延迟触发和协程 等
/// </summary>
public class GlobalMonoMgr : BaseManager<GlobalMonoMgr>
{
    //场景中唯一一个的MonoControl对象 
    private MonoControl _monoControl = null;
    protected override void Awake()
    {
        //为空 则新建一个空对象 该空对象 将存在于游戏程序的整个生命周期
        //造就一个至始至终都不会销毁的对象 并且是动态创建的
        if (_monoControl == null)
        {
            GameObject obj = new()
            {
                name = "MONO_MAIN"
            };
            _monoControl = obj.AddComponent<MonoControl>();
            DontDestroyOnLoad(obj);
        }
    }

    /// <summary>
    /// 获取Mono管理对象 可以再外部往上挂载需要一直存在的脚本
    /// </summary>
    public MonoControl ComponentControl
    {
        get
        {
            return _monoControl;
        }
    }

    /// <summary>
    /// 添加update帧更新事件
    /// </summary>
    /// <param name="function"></param>
    public void AddUpdateListener(Action function)
    {
        _monoControl.EventUpdate += function;
    }

    /// <summary>
    /// 移除update帧更新时间
    /// </summary>
    /// <param name="function"></param>
    public void RemoveUpdateListener(Action function)
    {
        _monoControl.EventUpdate -= function;
    }

    #region 以下为封装 协程 延迟相关的接口
    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return _monoControl.StartCoroutine(routine);
    }

    public Coroutine StartCoroutine(string methodName)
    {
        return _monoControl.StartCoroutine(methodName);
    }

    public Coroutine StartCoroutine(string methodName, [DefaultValue("null")] object value)
    {
        return _monoControl.StartCoroutine(methodName, value);
    }

    public void StopAllCoroutines()
    {
        _monoControl.StopAllCoroutines();
    }

    public void StopCoroutine(string methodName)
    {
        _monoControl.StopCoroutine(methodName);
    }

    public void StopCoroutine(IEnumerator routine)
    {
        _monoControl.StopCoroutine(routine);
    }

    public void StopCoroutine(Coroutine routine)
    {
        _monoControl.StopCoroutine(routine);
    }

    public void CancelInvoke()
    {
        _monoControl.CancelInvoke();
    }

    public void CancelInvoke(string methodName)
    {
        _monoControl.CancelInvoke(methodName);
    }

    public void Invoke(string methodName, float time)
    {
        _monoControl.Invoke(methodName, time);
    }

    public void InvokeRepeating(string methodName, float time, float repeatRate)
    {
        _monoControl.InvokeRepeating(methodName, time, repeatRate);
    }

    public bool IsInvoking()
    {
        return _monoControl.IsInvoking();
    }

    public bool IsInvoking(string methodName)
    {
        return _monoControl.IsInvoking(methodName);
    }
    public void SafeStartCoroutine(IEnumerator routine)
    {
        
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log("场景启动");
            if (_monoControl != null)
            {
               
                _monoControl.StartCoroutine(routine);
            }
        });
    }
    #endregion
}
