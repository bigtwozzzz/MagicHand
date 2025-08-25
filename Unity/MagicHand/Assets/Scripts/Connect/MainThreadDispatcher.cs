using System;
using System.Collections.Concurrent;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> actionQueue = new();

    private static MainThreadDispatcher instance;

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (instance != null) return instance;

            lock (typeof(MainThreadDispatcher))
            {
                if (instance == null)
                {
                    CreateInstance();
                }
                return instance;
            }
        }
    }

    private static void CreateInstance()
    {
        GameObject go = new("MainThreadDispatcher")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        instance = go.AddComponent<MainThreadDispatcher>();
        // DontDestroyOnLoad 在 Awake 中处理
    }

    private void Awake()
    {
        // 处理多实例问题
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // 销毁重复实例
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;

        // 可选：防止向已销毁实例添加任务
        if (instance == null)
        {
           // Debug.LogWarning("[MainThreadDispatcher] 已销毁，忽略新任务。");
            return;
        }

        actionQueue.Enqueue(action);
    }

    private void Update()
    {
        while (actionQueue.TryDequeue(out Action action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void OnDestroy()
    {
        // 清空队列，防止内存泄漏
        while (actionQueue.TryDequeue(out _)) { }

        if (instance == this)
        {
            instance = null;
        }

        Debug.Log("[MainThreadDispatcher] 已销毁，资源释放完成。");
    }
}