// BaseManager.cs
using UnityEngine;

public class BaseManager<T> : MonoBehaviour where T : BaseManager<T>
{
    private static T instance;
    public static T GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<T>();
            if (instance == null)
            {
                GameObject obj = new GameObject($"[{typeof(T).Name}]");
                instance = obj.AddComponent<T>();
                DontDestroyOnLoad(obj); // 必须加
            }
        }
        return instance;
    }

    protected virtual void Awake()
    {
        // 防止重复创建
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = (T)this;
        DontDestroyOnLoad(gameObject); // 再次确保
        Debug.Log($"[BaseManager] {typeof(T).Name} 已初始化");
    }
}