using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PoolData
{
    public GameObject fatherObj;           // 父节点
    public List<GameObject> poolList;      // 对象列表

    public PoolData(GameObject prefab, GameObject poolRoot)
    {
        // 创建父对象
        fatherObj = new GameObject($"[Pool] {prefab.name}");
        fatherObj.transform.SetParent(poolRoot.transform, false);

        poolList = new List<GameObject>();
        PushObj(prefab);
    }

    /// <summary>
    /// 存入对象
    /// </summary>
    public void PushObj(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(fatherObj.transform, false);
        poolList.Add(obj);
    }

    /// <summary>
    /// 取出对象
    /// </summary>
    public GameObject GetObj()
    {
        GameObject obj = poolList[0];
        poolList.RemoveAt(0);

        obj.SetActive(true);
        obj.transform.SetParent(null); // 断开父级

        return obj;
    }

    /// <summary>
    /// 销毁该抽屉所有缓存对象
    /// </summary>
    public void DestroyAll()
    {
        foreach (GameObject go in poolList)
        {
            if (go != null)
                GameObject.Destroy(go);
        }
        poolList.Clear();

        if (fatherObj != null)
            GameObject.Destroy(fatherObj);
    }
}
/// <summary>
/// 缓存池模块
/// 1.Dictionary List
/// 2.GameObject 和 Resources 两个公共类中的 API 
/// </summary>
/// <summary>
/// 对象池管理器（支持资源释放）
/// </summary>
public class PoolMgr : BaseManager<PoolMgr>
{
    private GameObject _poolRoot; // 池根对象
    private Dictionary<string, PoolData> _poolDic = new();

    /// <summary>
    /// 获取对象（异步加载）
    /// </summary>
    public void GetObj(string name, UnityAction<GameObject> callback)
    {
        if (_poolDic.TryGetValue(name, out PoolData poolData) && poolData.poolList.Count > 0)
        {
            callback(poolData.GetObj());
            return;
        }

        // 否则异步加载
        ResMgr.GetInstance().LoadAsync<GameObject>(name, (obj) =>
        {
            obj.name = name;
            callback(obj);
        });
    }

    /// <summary>
    /// 回收对象到池
    /// </summary>
    public void PushObj(string name, GameObject obj)
    {
        InitializePoolRoot();

        if (_poolDic.TryGetValue(name, out PoolData poolData))
        {
            poolData.PushObj(obj);
        }
        else
        {
            _poolDic[name] = new PoolData(obj, _poolRoot);
        }
    }

    /// <summary>
    /// 确保池根对象存在
    /// </summary>
    private void InitializePoolRoot()
    {
        if (_poolRoot != null) return;

        _poolRoot = new GameObject("[ObjectPool]");
        DontDestroyOnLoad(_poolRoot);
    }

    /// <summary>
    /// 清空指定池
    /// </summary>
    public void ClearPool(string name)
    {
        if (_poolDic.TryGetValue(name, out PoolData poolData))
        {
            poolData.DestroyAll();
            _poolDic.Remove(name);
        }
    }

    /// <summary>
    /// 清空所有池（推荐在场景切换或重启时调用）
    /// </summary>
    public void ClearAll()
    {
        foreach (var pair in _poolDic)
        {
            pair.Value.DestroyAll();
        }
        _poolDic.Clear();

        if (_poolRoot != null)
        {
            GameObject.Destroy(_poolRoot);
            _poolRoot = null;
        }

        Debug.Log("[PoolMgr] 所有对象池已清空。");
    }

    /// <summary>
    /// 安全关闭池管理器（完全释放资源）
    /// </summary>
    public void Shutdown()
    {
        ClearAll();
        //base.OnDestroy();
        Debug.Log("[PoolMgr] 已关闭，资源已释放。");
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }
}