using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源加载模块
/// 1.异步加载
/// 2.委托和 lambda表达式
/// 3.协程
/// 4.泛型
/// </summary>
public class ResMgr : BaseManager<ResMgr>
{
    //同步加载资源
    public T Load<T>(string name) where T : Object
    {
        T res = Resources.Load<T>(name);
        //如果对象是一个GameObject类型的 我把他实例化后 再返回出去 外部 直接使用即可
        if (res is GameObject)
            return GameObject.Instantiate(res);
        else//例如TextAsset AudioClip类型，不需要实例化
            return res;
    }


    //异步加载资源
    public void LoadAsync<T>(string name, UnityAction<T> callback) where T : Object
    {
        //开启异步加载的协程
        GlobalMonoMgr.GetInstance().StartCoroutine(ReallyLoadAsync(name, callback));
    }

    //真正的协同程序函数  用于 开启异步加载对应的资源
    private IEnumerator ReallyLoadAsync<T>(string name, UnityAction<T> callback) where T : Object
    {
        ResourceRequest r = Resources.LoadAsync<T>(name);
        yield return r;

        if (r.asset is GameObject)
            callback(GameObject.Instantiate(r.asset) as T);
        else
            callback(r.asset as T);
    }
    private void OnApplicationQuit()
    {
        // 清理 DontDestroyOnLoad 的对象
        CleanupSingletons();
    }

    private void CleanupSingletons()
    {
        // 销毁 MONO_MAIN
        var monoMain = GameObject.Find("MONO_MAIN");
        if (monoMain != null)
        {
            GameObject.DestroyImmediate(monoMain);
        }

        // 销毁 GlobalMonoMgr
        var globalMonoMgr = GlobalMonoMgr.GetInstance()?.gameObject;
        if (globalMonoMgr != null)
        {
            GameObject.DestroyImmediate(globalMonoMgr);
        }

        // 如果还有其他类似的持久化对象，也在这里一并处理
    }
}
