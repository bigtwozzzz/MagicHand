using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI�㼶
/// </summary>

/// <summary>
/// UI�㼶
/// </summary>
public enum E_UI_Layer
{
    Bot,
    Mid,
    Top,
    System,
}

/// <summary>
/// ������ - ������ҿؼ������� Awake ����
/// </summary>
public class BasePanel : MonoBehaviour
{
    // �ؼ����棺ֻ�����Ѳ��ҹ���
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
    /// ������ҿؼ����״β��Һ󻺴棩
    /// </summary>
    protected T GetControl<T>(string controlName) where T : UIBehaviour
    {
        if (string.IsNullOrEmpty(controlName))
            return null;

        // �Ȳ黺��
        if (controlCache.TryGetValue(controlName, out var ctrl))
            return ctrl as T;

        // ��·�����ң�֧��Ƕ�ף�Btn/Close��Input/Name �ȣ�
        Transform tf = transform.Find(controlName);
        if (tf == null)
        {
            Debug.LogWarning($"[BasePanel] �Ҳ����ؼ�: {controlName} (·�����ܴ���)");
            return null;
        }

        T component = tf.GetComponent<T>();
        if (component == null)
        {
            Debug.LogWarning($"[BasePanel] �ؼ� {controlName} ȱ�����: {typeof(T).Name}");
            return null;
        }

        // ����
        controlCache[controlName] = component;

        // �Զ�ע���¼��������Ҫ��
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
    /// �������棨��ѡ���������ʱ���ã�
    /// </summary>
    protected virtual void OnDestroy()
    {
        controlCache.Clear();
    }

    /// <summary>
    /// �����Զ����¼�����̬���ߣ�
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
/// UI����������ȫ���� + ��֡ʵ���� + ��ȷ�ͷţ�
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
        Debug.Log("[UIMgr] Awake �����ã���ʼ��ʼ��...");

        GlobalMonoMgr.GetInstance().SafeStartCoroutine(InitializeAsync());
    }

    private IEnumerator InitializeAsync()
    {
        Debug.Log("[UIMgr] ��ʼ�첽��ʼ��...");

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
            Debug.LogError($"[UIMgr] ���� Canvas ʧ��: {canvasAddress}");
            yield break;
        }

        // ʵ���� Canvas���ɷ�֡��
        yield return null;

        GameObject canvasObj = Instantiate(canvasPrefab);
        canvasObj.name = "UI-Canvas";
        DontDestroyOnLoad(canvasObj);

        canvas = canvasObj.GetComponent<RectTransform>();
        if (canvas == null)
        {
            Debug.LogError("[UIMgr] Canvas ȱ�� RectTransform �����");
            yield break;
        }

        bot = FindChild("Bot");
        mid = FindChild("Mid");
        top = FindChild("Top");
        system = FindChild("System");

        yield return null;

        // 创建 EventSystem（检查是否已存在）
        if (FindObjectOfType<EventSystem>() == null)
        {
            ResMgr.GetInstance().LoadAsync<GameObject>("UI/EventSystem", (eventSysPrefab) =>
            {
                if (eventSysPrefab != null)
                {
                    // 再次检查，防止异步加载期间其他地方创建了EventSystem
                    if (FindObjectOfType<EventSystem>() == null)
                    {
                        GameObject eventSysObj = Instantiate(eventSysPrefab);
                        eventSysObj.name = "UI-EventSystem";
                        DontDestroyOnLoad(eventSysObj);
                        Debug.Log("[UIMgr] EventSystem 创建成功");
                    }
                    else
                    {
                        Debug.Log("[UIMgr] EventSystem 已存在，跳过创建");
                    }
                }
            }, autoRelease: false);
        }
        else
        {
            Debug.Log("[UIMgr] EventSystem 已存在，跳过创建");
        }

        Debug.Log("[UIMgr] ��ʼ����ɣ�");
    }

    private Transform FindChild(string name)
    {
        Transform child = canvas?.Find(name);
        if (child == null)
            Debug.LogError($"[UIMgr] �Ҳ����Ӷ���: {name}");
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
    /// ��ʾ��壨�Ƿ��Ͱ汾������ֻ֪�������������������ʱ��
    /// </summary>
    public void ShowPanel(string panelName, E_UI_Layer layer = E_UI_Layer.Mid)
    {
        if (panelDic.TryGetValue(panelName, out BasePanel panel))
        {
            // ����Ѵ��ڣ�ֱ����ʾ
            panel.ShowMe();
            Debug.Log($"[UIMgr] ����Ѵ��ڣ�ֱ����ʾ: {panelName}");
            return;
        }

        // ��岻���ڣ��첽����
        string address = "UI/Prefabs/" + panelName;
        Debug.Log($"[UIMgr] �Ƿ��ͼ������: {address}");

        ResMgr.GetInstance().LoadAsync<GameObject>(address, (prefab) =>
        {
            if (prefab == null)
            {
                Debug.LogError($"[UIMgr] ����ʧ�ܣ�prefab Ϊ null: {address}");
                return;
            }

            Transform father = GetLayerFather(layer);
            if (father == null)
            {
                Debug.LogError($"[UIMgr] ���ڵ�Ϊ null��layer={layer}");
                return;
            }

            // ʵ����
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

            // ��ȡ BasePanel ���
            BasePanel basePanel = instance.GetComponent<BasePanel>();
            if (basePanel == null)
            {
                Debug.LogError($"[UIMgr] ʵ��ȱ�� BasePanel �ű�: {panelName}");
                Destroy(instance);
                return;
            }

            // ����
            panelDic[panelName] = basePanel;

            // ��ʾ
            basePanel.ShowMe();

            Debug.Log($"[UIMgr] �Ƿ������ {panelName} ��������ʾ");
        }, autoRelease: false);
    }
    /// <summary>
    /// ��ʾ��壨�첽���� + ��֡ʵ������
    /// </summary>
    public void ShowPanel<T>(string panelName, E_UI_Layer layer = E_UI_Layer.Mid, UnityAction<T> callBack = null) where T : BasePanel
    {
        if (panelDic.ContainsKey(panelName))
        {
            panelDic[panelName].ShowMe();
            callBack?.Invoke(panelDic[panelName] as T);
            Debug.Log($"[UIMgr] ����Ѵ��ڣ�ֱ����ʾ: {panelName}");
            return;
        }

        string address = "UI/Prefabs/" + panelName;
        Debug.Log($"[UIMgr] ��ʼ�������: {address}");

        ResMgr.GetInstance().LoadAsync<GameObject>(address, (prefab) =>
        {
            if (prefab == null)
            {
                Debug.LogError($"[UIMgr] ����ʧ�ܣ�prefab Ϊ null������ Addressables ��ַ: {address}");
                return;
            }

            Transform father = GetLayerFather(layer);
            if (father == null)
            {
                Debug.LogError($"[UIMgr] ���ڵ�Ϊ null��layer={layer}");
                return;
            }

            // ������֡����Э��
            GlobalMonoMgr.GetInstance().SafeStartCoroutine(CreatePanelAsync(prefab, father, panelName, callBack));
        }, autoRelease: false);
    }

    /// <summary>
    /// ��֡ʵ������壨���ⵥ֡���٣�
    /// </summary>
    private IEnumerator CreatePanelAsync<T>(GameObject prefab, Transform father, string panelName, UnityAction<T> callBack) where T : BasePanel
    {
        float startTime = Time.realtimeSinceStartup;

        // Step 1: ʵ����
        GameObject instance = Instantiate(prefab, father);
        instance.name = panelName;
        if (instance.TryGetComponent<RectTransform>(out var rect))
        {
            ResetRectTransform(rect);
        }
        else
        {
            // ���û�� RectTransform���������� Transform
            ResetTransform(instance.transform);
        }
        yield return null; // ��֡���� Canvas �л����ؽ�

        // Step 2: ��ȡ���
        T panel = instance.GetComponent<T>();
        if (panel == null)
        {
            Debug.LogError($"[UIMgr] ʵ����ȱ�ٽű����: {typeof(T).Name}");
            Destroy(instance);
            yield break;
        }

        // Step 3: ����
        panelDic[panelName] = panel;

        yield return null;

        // Step 4: ��ʾ
        panel.ShowMe();

        yield return null;

        // Step 5: �ص�
        callBack?.Invoke(panel);

        float cost = (Time.realtimeSinceStartup - startTime) * 1000;
        Debug.Log($"[UIMgr] ��� {panelName} ��֡������ɣ���ʱ: {cost:F2}ms");
    }

    /// <summary>
    /// ���� Transform
    /// </summary>
    private void ResetTransform(Transform tf)
    {
        tf.localPosition = Vector3.zero;
        tf.localScale = Vector3.one;
        tf.localRotation = Quaternion.identity;
    }
    private void ResetRectTransform(RectTransform rectTransform)
    {
        // 1. ����ê�㣺�Ľ����죨������������
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;

        // 2. �������ģ�����
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 3. ����λ�úͳߴ�
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero; // sizeDelta=0 ��ʾ�Զ�����ê��

        // 4. �������ź���ת
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }
    /// <summary>
    /// ������壨����ʵ����
    /// </summary>
    public void HidePanel(string panelName)
    {
        if (!panelDic.TryGetValue(panelName, out var panel)) return;

        panel.HideMe();
        Destroy(panel.gameObject);
        panelDic.Remove(panelName);
    }

    /// <summary>
    /// ��ȡ����ʾ�����
    /// </summary>
    public T GetPanel<T>(string name) where T : BasePanel
    {
        return panelDic.TryGetValue(name, out var panel) ? panel as T : null;
    }

    /// <summary>
    /// �ͷ����� UI ��Դ�������л�ʱ���ã�
    /// </summary>
    public void ReleaseAllUIResources()
    {
        Debug.Log("[UIMgr] ��ʼ�ͷ����� UI ��Դ...");

        foreach (var pair in panelDic)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }
        panelDic.Clear();

        Debug.Log("[UIMgr] UI ��Դ�ͷ���ɡ�");
    }
}