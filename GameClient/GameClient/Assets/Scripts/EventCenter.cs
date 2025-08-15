using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IEventInfo
{

}

public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}

public class EventInfo : IEventInfo
{
    public UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}
/// <summary>  
/// 事件类型 只要新加一种事件类型 就在枚举中添加 这样比直接用string安全一些也可以避免重复事件  
/// </summary>  
public enum E_EventType
{

    //玩家指令
    Event_Login_Request,
    Event_Player_Command,             // 所有玩家命令都走这个事件
    Event_Player_Command_Login,
    Event_Player_Command_Select_Stage,       // 发起关卡选择
    Event_Player_Command_Confirm_Stage,      // 回应关卡确认
    Event_Player_Command_Move,               // 角色移动
    Event_Player_Command_Attack,
    Event_Player_Command_UseSkill,
    Event_Player_Command_Logout,             // 主动登出
    //服务器指令
    Event_Login_Success,
    Event_Logout,
    Event_Player_Online,
    Event_Player_Offline,
    Event_Character_Info_Update,
    Event_Stage_Select_Request,
    Event_Stage_Select_Result,
    Event_Scene_Data_Update,
    //系统指令
    Event_LoadScene_Progress,
    

    //系统设置变更

    //资源变更

    //数据变更




    //怪物死亡  
    Event_Monster_Dead,
    //玩家死亡  
    Event_Player_Dead,
    //场景加载进度  
   
    //输入有关事件  
    Event_Keycode_Input,
    Event_Mouse_Input,
    Event_MouseX_Input,
    Event_MouseY_Input,
    Event_Horizontal_Input,
    Event_Vertical_Input
}

/// <summary>
/// 事件中心 单例模式对象
/// 1.Dictionary
/// 2.委托
/// 3.观察者设计模式
/// 4.泛型
/// </summary>
public class EventCenter : BaseManager<EventCenter>
{
    //key —— 事件的名字（比如：怪物死亡，玩家死亡，通关 等等）
    //value —— 对应的是 监听这个事件 对应的委托函数们
    private Dictionary<E_EventType, IEventInfo> eventDic;

    protected override void Awake()
    {
        base.Awake();
        eventDic = new();
    }
    /// <summary>
    /// 添加事件监听
    /// </summary>
    /// <param name="name">事件的名字</param>
    /// <param name="action">准备用来处理事件 的委托函数</param>
    public void AddEventListener<T>(E_EventType name, UnityAction<T> action)
    {
        //有没有对应的事件监听
        //有的情况
        if (eventDic.ContainsKey(name))
        {
            (eventDic[name] as EventInfo<T>).actions += action;
        }
        //没有的情况
        else
        {
            eventDic.Add(name, new EventInfo<T>(action));
        }
    }

    /// <summary>
    /// 监听不需要参数传递的事件
    /// </summary>
    /// <param name="name"></param>
    /// <param name="action"></param>
    public void AddEventListener(E_EventType name, UnityAction action)
    {
        //有没有对应的事件监听
        //有的情况
        if (eventDic.ContainsKey(name))
        {
            (eventDic[name] as EventInfo).actions += action;
        }
        //没有的情况
        else
        {
            eventDic.Add(name, new EventInfo(action));
        }
    }


    /// <summary>
    /// 移除对应的事件监听
    /// </summary>
    /// <param name="name">事件的名字</param>
    /// <param name="action">对应之前添加的委托函数</param>
    public void RemoveEventListener<T>(E_EventType name, UnityAction<T> action)
    {
        if (eventDic.ContainsKey(name))
            (eventDic[name] as EventInfo<T>).actions -= action;
    }

    /// <summary>
    /// 移除不需要参数的事件
    /// </summary>
    /// <param name="name"></param>
    /// <param name="action"></param>
    public void RemoveEventListener(E_EventType name, UnityAction action)
    {
        if (eventDic.ContainsKey(name))
            (eventDic[name] as EventInfo).actions -= action;
    }

    /// <summary>
    /// 事件触发
    /// </summary>
    /// <param name="name">哪一个名字的事件触发了</param>
    public void EventTrigger<T>(E_EventType name, T info)
    {
      //  Debug.Log("触发事件：" + name);
        //有没有对应的事件监听
        //有的情况
        if (eventDic.ContainsKey(name))
        {
            //eventDic[name]();
            Debug.Log(name);
            Debug.Log((eventDic[name] as EventInfo<T>).actions);
            (eventDic[name] as EventInfo<T>).actions?.Invoke(info);
            //eventDic[name].Invoke(info);
        }
    }

    /// <summary>
    /// 事件触发（不需要参数的）
    /// </summary>
    /// <param name="name"></param>
    public void EventTrigger(E_EventType name)
    {
        
        //有没有对应的事件监听
        //有的情况
        if (eventDic.ContainsKey(name))
        {
            //eventDic[name]();
            (eventDic[name] as EventInfo).actions?.Invoke();
            //eventDic[name].Invoke(info);
        }
    }

    /// <summary>
    /// 清空事件中心
    /// 主要用在 场景切换时
    /// </summary>
    public void Clear()
    {
        eventDic.Clear();
    }
}
