using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// 通用事件接口
/// </summary>
public interface IEventInfo { }

/// <summary>
/// 带泛型参数的事件包装器
/// </summary>
/// <typeparam name="T"></typeparam>
public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions = action;
    }
}

/// <summary>
/// 无参事件包装器
/// </summary>
public class EventInfo : IEventInfo
{
    public UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions = action;
    }
}

/// <summary>  
/// 事件类型枚举
/// </summary>  
public enum E_EventType
{
    // 玩家指令
    Event_Login_Request,
    Event_Player_Command,
    Event_Player_Command_Login,
    Event_Player_Command_Select_Stage,
    Event_Player_Command_Confirm_Stage,
    Event_Player_Command_Move,
    Event_Player_Command_Attack,
    Event_Player_Command_UseSkill,
    Event_Player_Command_Logout,
    Event_Button_Setting_Click,
    // 服务器指令
    Event_Global_Random_Seed,
    Event_Login_Success,
    Event_Logout,
    Event_Player_Online,
    Event_Player_Offline,
    Event_Character_Info_Update,
    Event_Stage_Select_Request,
    Event_Stage_Select_Result,
    Event_Scene_Data_Update,

    // 系统指令
    Event_Platform_Loaded,
    Event_LoadScene_Progress,
    Event_Lock_Window,

    // 游戏逻辑
    Event_Character_Spawn_Ready,
    Event_Monster_Dead,
    Event_Player_Dead,

    // 输入事件
    Event_Keycode_Input,
    Event_Mouse_Input,
    Event_MouseX_Input,
    Event_MouseY_Input,
    Event_Horizontal_Input,
    Event_Vertical_Input
}

/// <summary>
/// 事件中心 —— 单例模式
/// 功能：事件注册、分发、移除
/// 设计模式：观察者模式 + 泛型 + 字典
/// 注意：
/// - 不支持 lambda 表达式作为监听函数（无法正确移除）
/// - 场景切换时建议调用 Clear() 清理跨场景残留
/// </summary>
public class EventCenter : BaseManager<EventCenter>
{
    private Dictionary<E_EventType, IEventInfo> eventDic;

    protected override void Awake()
    {
        //  1. 先调用父类，确保单例逻辑
        base.Awake();

        //  2. 确保只初始化一次
        if (eventDic != null) return;

        eventDic = new Dictionary<E_EventType, IEventInfo>();

        //  3. 标记为不随场景销毁（必须在 Awake 中调用）
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 添加带参数的事件监听
    /// </summary>
    public void AddEventListener<T>(E_EventType name, UnityAction<T> action)
    {
        if (eventDic.TryGetValue(name, out IEventInfo existing))
        {
            if (existing is EventInfo<T> eventInfo)
            {
                eventInfo.actions += action;
            }
            else
            {
                Debug.LogError($"[EventCenter] 事件 {name} 类型不匹配！");
            }
        }
        else
        {
            eventDic[name] = new EventInfo<T>(action);
        }
    }

    /// <summary>
    /// 添加无参事件监听
    /// </summary>
    public void AddEventListener(E_EventType name, UnityAction action)
    {
        if (eventDic.TryGetValue(name, out IEventInfo existing))
        {
            if (existing is EventInfo eventInfo)
            {
                eventInfo.actions += action;
            }
        }
        else
        {
            eventDic[name] = new EventInfo(action);
        }
    }

    /// <summary>
    /// 移除带参数的事件监听
    /// 注意：不支持 lambda，必须传入相同方法引用
    /// </summary>
    public void RemoveEventListener<T>(E_EventType name, UnityAction<T> action)
    {
        // 增加 null 检查
        if (eventDic == null)
        {
            Debug.LogWarning($"[EventCenter] eventDic 为 null，无法移除事件监听: {name}");
            return;
        }

        if (eventDic.TryGetValue(name, out IEventInfo existing))
        {
            if (existing is EventInfo<T> eventInfo && eventInfo.actions != null)
            {
                eventInfo.actions -= action;

                if (eventInfo.actions == null)
                {
                    eventDic.Remove(name);
                }
            }
        }
    }

    /// <summary>
    /// 移除无参事件监听
    /// </summary>
    public void RemoveEventListener(E_EventType name, UnityAction action)
    {
        if (eventDic == null)
        {
            Debug.LogWarning($"[EventCenter] eventDic 为 null，无法移除事件监听: {name}");
            return;
        }

        if (eventDic.TryGetValue(name, out IEventInfo existing))
        {
            if (existing is EventInfo eventInfo && eventInfo.actions != null)
            {
                eventInfo.actions -= action;

                if (eventInfo.actions == null)
                {
                    eventDic.Remove(name);
                }
            }
        }
    }
    /// <summary>
    /// 触发带参数的事件
    /// </summary>
    public void EventTrigger<T>(E_EventType name, T info)
    {
        //Debug.Log($"[EventCenter] 触发事件: {name}, 参数类型: {typeof(T)}, 值: {info}");
        if (eventDic.TryGetValue(name, out IEventInfo existing))
        {
            if (existing is EventInfo<T> eventInfo && eventInfo.actions != null)
            {
                try
                {
                    eventInfo.actions.Invoke(info);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EventCenter] 触发事件 {name} 时发生异常: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[EventCenter] 事件 {name} 无监听者或类型不匹配。");
            }
        }
    }

    /// <summary>
    /// 触发无参事件
    /// </summary>
    public void EventTrigger(E_EventType name)
    {
        if (eventDic.TryGetValue(name, out IEventInfo existing))
        {
            if (existing is EventInfo eventInfo && eventInfo.actions != null)
            {
                try
                {
                    eventInfo.actions.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EventCenter] 触发事件 {name} 时发生异常: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 清空所有事件监听（谨慎使用）
    /// 建议在场景切换或模块重置时调用
    /// </summary>
    public void Clear()
    {
        if (eventDic != null)
        {
            eventDic.Clear();
            // Debug.Log("[EventCenter] 所有事件监听已清除。"); 
            //  可选：发布时建议注释掉日志，避免 OnDestroy 时打印失败
        }
    }

    //protected override void OnDestroy()
    //{
    //    //  1. 清理事件
    //    Clear();

    //    //  2. 父类清理（如单例引用置空）
    //    base.OnDestroy();

    //    //  3. 可选日志（注意：有时 OnDestroy 中打印会失败）
    //    // Debug.Log("[EventCenter] 已销毁，资源释放完成。");
    //}
}