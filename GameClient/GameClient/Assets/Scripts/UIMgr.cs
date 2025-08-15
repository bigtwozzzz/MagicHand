using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;



/// <summary>
/// 面板基类 
/// 帮助我门通过代码快速的找到所有的子控件
/// 方便我们在子类中处理逻辑 
/// 节约找控件的工作量
/// </summary>
public class BasePanel : MonoBehaviour
{
    //通过里式转换原则 来存储所有的控件
    //key为对象名，value为每一个对象上的所有组件的集合
    private Dictionary<string, List<UIBehaviour>> controlDic = new();

    // Use this for initialization
    protected virtual void Awake()
    {
        FindChildrenControl<Button>();
        FindChildrenControl<Image>();
        FindChildrenControl<Text>();
        FindChildrenControl<Toggle>();
        FindChildrenControl<Slider>();
        FindChildrenControl<ScrollRect>();
        FindChildrenControl<InputField>();
        FindChildrenControl<TMP_InputField>();
    }

    /// <summary>
    /// 显示自己
    /// </summary>
    public virtual void ShowMe()
    {

    }

    /// <summary>
    /// 隐藏自己
    /// </summary>
    public virtual void HideMe()
    {

    }

    protected virtual void OnClick(string btnName)
    {

    }

    protected virtual void OnValueChanged(string toggleName, bool value)
    {

    }

    /// <summary>
    /// 得到对应名字的对应控件脚本
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="controlName"></param>
    /// <returns></returns>
    protected T GetControl<T>(string controlName) where T : UIBehaviour
    {
        if (controlDic.ContainsKey(controlName))
        {
            for (int i = 0; i < controlDic[controlName].Count; ++i)
            {
                if (controlDic[controlName][i] is T)
                    return controlDic[controlName][i] as T;
            }
        }

        return null;
    }

    /// <summary>
    /// 找到子对象的对应控件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private void FindChildrenControl<T>() where T : UIBehaviour
    {
        T[] controls = this.GetComponentsInChildren<T>();
        for (int i = 0; i < controls.Length; ++i)
        {
            string objName = controls[i].gameObject.name;
            if (controlDic.ContainsKey(objName))
                controlDic[objName].Add(controls[i]);
            else
                controlDic.Add(objName, new List<UIBehaviour>() { controls[i] });
            //如果是按钮控件
            if (controls[i] is Button)
            {
                //一种很妙的写法，运用lamdad表达式在无参函数里面实现有参函数效果
                //虽然所有的同类控件都被挂载了
                //但是可以在panel中通过对OnClick重写
                //用swithc敲定对谁起效。
                (controls[i] as Button).onClick.AddListener(() =>
                {
                    OnClick(objName);
                });
            }
            //如果是单选框或者多选框
            else if (controls[i] is Toggle)
            {
                (controls[i] as Toggle).onValueChanged.AddListener((value) =>
                {
                    OnValueChanged(objName, value);
                });
            }
        }
    }
}

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
/// UI管理器
/// 1.管理所有显示的面板
/// 2.提供给外部 显示和隐藏等等接口
/// </summary>
public class UIMgr : BaseManager<UIMgr>
{
    public Dictionary<string, BasePanel> panelDic = new();

    private Transform bot;
    private Transform mid;
    private Transform top;
    private Transform system;

    //记录我们UI的Canvas父对象 方便以后外部可能会使用它
    public RectTransform canvas;

    private bool isInitialized = false;

    protected override void Awake()
    {
        base.Awake(); // 如果 BaseManager 有 Awake，记得调用

        if (isInitialized) return;
        isInitialized = true;

        Initialize();
    }

    private void Initialize()
    {
        // 加载 Canvas
        GameObject canvasObj = ResMgr.GetInstance().Load<GameObject>("UI/Canvas");

        if (canvasObj == null)
        {
            Debug.LogError("UIMgr 初始化失败：无法加载 UI/Canvas，请检查 Resources/UI/Canvas.prefab 是否存在！");
            return;
        }
        
        canvas = canvasObj.GetComponent<RectTransform>();
        if (canvas == null)
        {
            Debug.LogError("UIMgr 初始化失败：Canvas prefab 的根对象缺少 RectTransform 组件！");
            return;
        }

        // 设置不随场景销毁
        GameObject.DontDestroyOnLoad(canvasObj);

        // 查找各层
        bot = FindChild("Bot");
        mid = FindChild("Mid");
        top = FindChild("Top");
        system = FindChild("System");

        // 加载 EventSystem
        GameObject eventSysObj = ResMgr.GetInstance().Load<GameObject>("UI/EventSystem");
        if (eventSysObj != null)
        {
            GameObject.DontDestroyOnLoad(eventSysObj);
        }
        else
        {
            Debug.LogError("UIMgr 初始化失败：无法加载 UI/EventSystem.prefab！");
        }
    }

    // 辅助方法：查找子对象并报错
    private Transform FindChild(string name)
    {
        Transform child = canvas.Find(name);
        if (child == null)
        {
            Debug.LogError($"UIMgr 初始化失败：Canvas 下找不到名为 '{name}' 的子对象！");
        }
        return child;
    }
    /// <summary>
    /// 通过层级枚举 得到对应层级的父对象
    /// </summary>
    /// <param name="layer"></param>
    /// <returns></returns>
    public Transform GetLayerFather(E_UI_Layer layer)
    {
        return layer switch
        {
            E_UI_Layer.Bot => this.bot,
            E_UI_Layer.Mid => this.mid,
            E_UI_Layer.Top => this.top,
            E_UI_Layer.System => this.system,
            _ => null,
        };
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    /// <typeparam name="T">面板脚本类型</typeparam>
    /// <param name="panelName">面板名</param>
    /// <param name="layer">显示在哪一层</param>
    /// <param name="callBack">当面板预设体创建成功后 你想做的事</param>
    public void ShowPanel<T>(string panelName, E_UI_Layer layer = E_UI_Layer.Mid, UnityAction<T> callBack = null) where T : BasePanel
    {
        if (panelDic.ContainsKey(panelName))
        {
            panelDic[panelName].ShowMe();
            // 处理面板创建完成后的逻辑
            callBack?.Invoke(panelDic[panelName] as T);
            //避免面板重复加载 如果存在该面板 即直接显示 调用回调函数后  直接return 不再处理后面的异步加载逻辑
            return;
        }

        ResMgr.GetInstance().LoadAsync<GameObject>("UI/Prefabs/" + panelName, (obj) =>
        {
            //把他作为 Canvas的子对象
            //并且 要设置它的相对位置
            //找到父对象 你到底显示在哪一层
            Transform father = bot;
            switch (layer)
            {
                case E_UI_Layer.Mid:
                    father = mid;
                    break;
                case E_UI_Layer.Top:
                    father = top;
                    break;
                case E_UI_Layer.System:
                    father = system;
                    break;
            }
            //设置父对象  设置相对位置和大小
            obj.transform.SetParent(father);

            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;

            (obj.transform as RectTransform).offsetMax = Vector2.zero;
            (obj.transform as RectTransform).offsetMin = Vector2.zero;

            //得到预设体身上的面板脚本
            T panel = obj.GetComponent<T>();
            // 处理面板创建完成后的逻辑
            //你除了可以在面板本身的awake里面修改面板，还可以通过这里进行修改
            //这里的修改会发生在面板的时间函数修改过后
            callBack?.Invoke(panel);

            panel.ShowMe();

            //把面板存起来
            panelDic.Add(panelName, panel);
        });
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    /// <param name="panelName"></param>
    public void HidePanel(string panelName)
    {
        if (panelDic.ContainsKey(panelName))
        {
            panelDic[panelName].HideMe();
            GameObject.Destroy(panelDic[panelName].gameObject);
            panelDic.Remove(panelName);
        }
    }

    /// <summary>
    /// 得到某一个已经显示的面板 方便外部使用
    /// </summary>
    public T GetPanel<T>(string name) where T : BasePanel
    {
        if (panelDic.ContainsKey(name))
            return panelDic[name] as T;
        return null;
    }

    /// <summary>
    /// 创建一个公共的静态方法，给某个面板的控件添加自定义事件监听
    /// </summary>
    /// <param name="control">控件对象</param>
    /// <param name="type">事件类型</param>
    /// <param name="callBack">事件的响应函数</param>
    public static void AddCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> callBack)
    {
        if (!control.TryGetComponent<EventTrigger>(out var trigger))
            trigger = control.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new()
        {
            eventID = type
        };
        entry.callback.AddListener(callBack);

        trigger.triggers.Add(entry);
    }

}
