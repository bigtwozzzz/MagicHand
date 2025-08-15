using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();
    private static MainThreadDispatcher instance;

    public static MainThreadDispatcher Instance
    {
        get
        {
            // 双重检查，避免频繁锁
            if (instance == null)
            {
                lock (typeof(MainThreadDispatcher))
                {
                    if (instance == null)
                    {
                        CreateInstance();
                    }
                }
            }
            return instance;
        }
    }

    private static void CreateInstance()
    {
        GameObject go = new GameObject("MainThreadDispatcher");
        go.hideFlags = HideFlags.HideAndDontSave;
        instance = go.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(go);
    }

    public static void Enqueue(Action action)
    {
        if (action != null)
        {
            actionQueue.Enqueue(action); // ConcurrentQueue 线程安全
        }
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
        instance = null;
    }
}