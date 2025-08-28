using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI层级
/// </summary>

/// <summary>
/// UI层级
/// </summary>
public enum E_UI_Layer
{
    Bot,
    Mid,
    Top,
    System,
}

/// <summary>
/// 面板基类 - 按需查找控件，避免 Awake 卡顿
/// </summary>
public class BasePanel : MonoBehaviour
{
    // 控件缓存：只缓存已查找过的
    private Dictionary<string, UIBehaviour> controlCache = new();
    protected virtual void Awake()
    {
        Debug.Log("[BasePanel] Awake called - initializing...");
    }
    public virtual void ShowMe() { }
    public virtual void HideMe() { }

    protected virtual void OnClick(string btnName) { }
    protected virtual void OnValueChanged(string toggleName, bool value) { }

    /// <summary>
    /// 按需查找控件（首次查找后缓存）
    /// </summary>
    protected T GetControl<T>(string controlName) where T : UIBehaviour
    {
        if (string.IsNullOrEmpty(controlName))
            return null;

        // 先查缓存
        if (controlCache.TryGetValue(controlName, out var ctrl))
            return ctrl as T;

        // 按路径查找（支持嵌套：Btn/Close、Input/Name 等）
        Transform tf = transform.Find(controlName);
        if (tf == null)
        {
            Debug.LogWarning($"[BasePanel] 找不到控件: {controlName} (路径可能错误)");
            return null;
        }

        T component = tf.GetComponent<T>();
        if (component == null)
        {
            Debug.LogWarning($"[BasePanel] 控件 {controlName} 缺少组件: {typeof(T).Name}");
            return null;
        }

        // 缓存
        controlCache[controlName] = component;

        // 自动注册事件（如果需要）
        if (component is Button btn)
        {
            btn.onClick.AddListener(() => OnClick(controlName));
        }
        else if (component is Toggle toggle)
        {
            toggle.onValueChanged.AddListener((value) => OnValueChanged(controlName, value));
        }

        return component;
    }

    /// <summary>
    /// 清理缓存（可选：面板销毁时调用）
    /// </summary>
    protected virtual void OnDestroy()
    {
        controlCache.Clear();
    }

    /// <summary>
    /// 添加自定义事件（静态工具）
    /// </summary>
    public static void AddCustomEventListener(UIBehaviour control, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callBack)
    {
        if (!control.TryGetComponent<EventTrigger>(out var trigger))
            trigger = control.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new() { eventID = type };
        entry.callback.AddListener(data => callBack?.Invoke(data));
        trigger.triggers.Add(entry);
    }
}
/// <summary>
/// UI管理器（安全加载 + 分帧实例化 + 正确释放）
/// </summary>
public class UIMgr : BaseManager<UIMgr>
{
    public Dictionary<string, BasePanel> panelDic = new();
    private Transform bot, mid, top, system;
    public RectTransform canvas;

    private bool isInitialized = false;

    protected override void Awake()
    {
        base.Awake();
        if (isInitialized) return;
        isInitialized = true;
        Debug.Log("[UIMgr] Awake 被调用，开始初始化...");

        GlobalMonoMgr.GetInstance().SafeStartCoroutine(InitializeAsync());
    }

    private IEnumerator InitializeAsync()
    {
        Debug.Log("[UIMgr] 开始异步初始化...");

        string canvasAddress = "UI/Canvas";
        GameObject canvasPrefab = null;
        bool loadCanvasDone = false;

        ResMgr.GetInstance().LoadAsync<GameObject>(canvasAddress, (prefab) =>
        {
            canvasPrefab = prefab;
            loadCanvasDone = true;
        }, autoRelease: false);

        while (!loadCanvasDone && canvasPrefab == null)
        {
            yield return null;
        }

        if (canvasPrefab == null)
        {
            Debug.LogError($"[UIMgr] 加载 Canvas 失败: {canvasAddress}");
            yield break;
        }

        // 实例化 Canvas（可分帧）
        yield return null;

        GameObject canvasObj = Instantiate(canvasPrefab);
        canvasObj.name = "UI-Canvas";
        DontDestroyOnLoad(canvasObj);

        canvas = canvasObj.GetComponent<RectTransform>();
        if (canvas == null)
        {
            Debug.LogError("[UIMgr] Canvas 缺少 RectTransform 组件！");
            yield break;
        }

        bot = FindChild("Bot");
        mid = FindChild("Mid");
        top = FindChild("Top");
        system = FindChild("System");

        yield return null;

        // 加载 EventSystem
        ResMgr.GetInstance().LoadAsync<GameObject>("UI/EventSystem", (eventSysPrefab) =>
        {
            if (eventSysPrefab != null)
            {
                GameObject eventSysObj = Instantiate(eventSysPrefab);
                eventSysObj.name = "UI-EventSystem";
                DontDestroyOnLoad(eventSysObj);
            }
        }, autoRelease: false);

        Debug.Log("[UIMgr] 初始化完成！");
    }

    private Transform FindChild(string name)
    {
        Transform child = canvas?.Find(name);
        if (child == null)
            Debug.LogError($"[UIMgr] 找不到子对象: {name}");
        return child;
    }

    public Transform GetLayerFather(E_UI_Layer layer) => layer switch
    {
        E_UI_Layer.Bot => bot,
        E_UI_Layer.Mid => mid,
        E_UI_Layer.Top => top,
        E_UI_Layer.System => system,
        _ => null
    };
    /// <summary>
    /// 显示面板（非泛型版本：用于只知道面板名，不关心类型时）
    /// </summary>
    public void ShowPanel(string panelName, E_UI_Layer layer = E_UI_Layer.Mid)
    {
        if (panelDic.TryGetValue(panelName, out BasePanel panel))
        {
            // 面板已存在，直接显示
            panel.ShowMe();
            Debug.Log($"[UIMgr] 面板已存在，直接显示: {panelName}");
            return;
        }

        // 面板不存在，异步加载
        string address = "UI/Prefabs/" + panelName;
        Debug.Log($"[UIMgr] 非泛型加载面板: {address}");

        ResMgr.GetInstance().LoadAsync<GameObject>(address, (prefab) =>
        {
            if (prefab == null)
            {
                Debug.LogError($"[UIMgr] 加载失败！prefab 为 null: {address}");
                return;
            }

            Transform father = GetLayerFather(layer);
            if (father == null)
            {
                Debug.LogError($"[UIMgr] 父节点为 null！layer={layer}");
                return;
            }

            // 实例化
            GameObject instance = Instantiate(prefab, father);
            instance.name = panelName;

            if (instance.TryGetComponent<RectTransform>(out var rect))
            {
                ResetRectTransform(rect);
            }
            else
            {
                ResetTransform(instance.transform);
            }

            // 获取 BasePanel 组件
            BasePanel basePanel = instance.GetComponent<BasePanel>();
            if (basePanel == null)
            {
                Debug.LogError($"[UIMgr] 实例缺少 BasePanel 脚本: {panelName}");
                Destroy(instance);
                return;
            }

            // 缓存
            panelDic[panelName] = basePanel;

            // 显示
            basePanel.ShowMe();

            Debug.Log($"[UIMgr] 非泛型面板 {panelName} 创建并显示");
        }, autoRelease: false);
    }
    /// <summary>
    /// 显示面板（异步加载 + 分帧实例化）
    /// </summary>
    public void ShowPanel<T>(string panelName, E_UI_Layer layer = E_UI_Layer.Mid, UnityAction<T> callBack = null) where T : BasePanel
    {
        if (panelDic.ContainsKey(panelName))
        {
            panelDic[panelName].ShowMe();
            callBack?.Invoke(panelDic[panelName] as T);
            Debug.Log($"[UIMgr] 面板已存在，直接显示: {panelName}");
            return;
        }

        string address = "UI/Prefabs/" + panelName;
        Debug.Log($"[UIMgr] 开始加载面板: {address}");

        ResMgr.GetInstance().LoadAsync<GameObject>(address, (prefab) =>
        {
            if (prefab == null)
            {
                Debug.LogError($"[UIMgr] 加载失败！prefab 为 null，请检查 Addressables 地址: {address}");
                return;
            }

            Transform father = GetLayerFather(layer);
            if (father == null)
            {
                Debug.LogError($"[UIMgr] 父节点为 null！layer={layer}");
                return;
            }

            // 启动分帧创建协程
            GlobalMonoMgr.GetInstance().SafeStartCoroutine(CreatePanelAsync(prefab, father, panelName, callBack));
        }, autoRelease: false);
    }

    /// <summary>
    /// 分帧实例化面板（避免单帧卡顿）
    /// </summary>
    private IEnumerator CreatePanelAsync<T>(GameObject prefab, Transform father, string panelName, UnityAction<T> callBack) where T : BasePanel
    {
        float startTime = Time.realtimeSinceStartup;

        // Step 1: 实例化
        GameObject instance = Instantiate(prefab, father);
        instance.name = panelName;
        if (instance.TryGetComponent<RectTransform>(out var rect))
        {
            ResetRectTransform(rect);
        }
        else
        {
            // 如果没有 RectTransform，至少重置 Transform
            ResetTransform(instance.transform);
        }
        yield return null; // 分帧：让 Canvas 有机会重建

        // Step 2: 获取组件
        T panel = instance.GetComponent<T>();
        if (panel == null)
        {
            Debug.LogError($"[UIMgr] 实例上缺少脚本组件: {typeof(T).Name}");
            Destroy(instance);
            yield break;
        }

        // Step 3: 缓存
        panelDic[panelName] = panel;

        yield return null;

        // Step 4: 显示
        panel.ShowMe();

        yield return null;

        // Step 5: 回调
        callBack?.Invoke(panel);

        float cost = (Time.realtimeSinceStartup - startTime) * 1000;
        Debug.Log($"[UIMgr] 面板 {panelName} 分帧创建完成！耗时: {cost:F2}ms");
    }

    /// <summary>
    /// 重置 Transform
    /// </summary>
    private void ResetTransform(Transform tf)
    {
        tf.localPosition = Vector3.zero;
        tf.localScale = Vector3.one;
        tf.localRotation = Quaternion.identity;
    }
    private void ResetRectTransform(RectTransform rectTransform)
    {
        // 1. 重置锚点：四角拉伸（填满父容器）
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;

        // 2. 重置轴心：居中
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 3. 重置位置和尺寸
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero; // sizeDelta=0 表示自动适配锚点

        // 4. 重置缩放和旋转
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }
    /// <summary>
    /// 隐藏面板（销毁实例）
    /// </summary>
    public void HidePanel(string panelName)
    {
        if (!panelDic.TryGetValue(panelName, out var panel)) return;

        panel.HideMe();
        Destroy(panel.gameObject);
        panelDic.Remove(panelName);
    }

    /// <summary>
    /// 获取已显示的面板
    /// </summary>
    public T GetPanel<T>(string name) where T : BasePanel
    {
        return panelDic.TryGetValue(name, out var panel) ? panel as T : null;
    }

    /// <summary>
    /// 释放所有 UI 资源（场景切换时调用）
    /// </summary>
    public void ReleaseAllUIResources()
    {
        Debug.Log("[UIMgr] 开始释放所有 UI 资源...");

        foreach (var pair in panelDic)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }
        panelDic.Clear();

        Debug.Log("[UIMgr] UI 资源释放完成。");
    }
}