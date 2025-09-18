using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// ͨ事件接口
/// </summary>
public interface IEventInfo { }

/// <summary>
/// 泛型实现
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
/// 实现
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
/// 事件种类
/// </summary>  
public enum E_EventType
{
    // 用户指令
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
    Event_Player_Skill_Info_Request,
    // 服务器指令
    Event_Global_Random_Seed,
    Event_Login_Success,
    Event_Logout,
    Event_Player_Online,
    Event_Player_Offline,
    Event_Character_Info_Update,
    Event_Scene_Data_Update,
    Event_Scene_Data_Update_UI,
    Event_Stage_Select_Request_Notify,
    Event_Stage_Select_Result_Notify,
    Event_Player_Skill_Info_Notify,
    Event_Combat_Info,
    // 系统指令
    Event_Platform_Loaded,
    Event_LoadScene_Progress,
    Event_Lock_Window,

    // 场景指令
    Event_Character_Spawn_Ready,
    Event_Monster_Dead,
    Event_Player_Dead,
    Event_Stage_Vote_Result,
    // 输出指令
    Event_Keycode_Input,
    Event_Mouse_Input,
    Event_MouseX_Input,
    Event_MouseY_Input,
    Event_Horizontal_Input,
    Event_Vertical_Input
}

/// <summary>
/// 事件中心
/// </summary>
public class EventCenter : BaseManager<EventCenter>
{
    private Dictionary<E_EventType, IEventInfo> eventDic;

    protected override void Awake()
    {
        base.Awake();

        if (eventDic != null) return;

        eventDic = new Dictionary<E_EventType, IEventInfo>();
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 监听事件泛型
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
                Debug.LogError($"[EventCenter] �¼� {name} ���Ͳ�ƥ�䣡");
            }
        }
        else
        {
            eventDic[name] = new EventInfo<T>(action);
        }
    }

    /// <summary>
    /// 监听事件
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
    /// 移除监听泛型
    /// </summary>
    public void RemoveEventListener<T>(E_EventType name, UnityAction<T> action)
    {
        if (eventDic == null)
        {
            Debug.LogWarning($"[EventCenter] eventDic Ϊ null���޷��Ƴ��¼�����: {name}");
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
    /// 移除监听
    /// </summary>
    public void RemoveEventListener(E_EventType name, UnityAction action)
    {
        if (eventDic == null)
        {
            Debug.LogWarning($"[EventCenter] eventDic Ϊ null���޷��Ƴ��¼�����: {name}");
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
    /// 监听触发泛型
    /// </summary>
    public void EventTrigger<T>(E_EventType name, T info)
    {
        //Debug.Log($"[EventCenter] 监听类型: {name}, 监听信息: {typeof(T)}, ֵ: {info}");
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
                    Debug.LogError($"[EventCenter] �����¼� {name} ʱ�����쳣: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[EventCenter] �¼� {name} �޼����߻����Ͳ�ƥ�䡣");
            }
        }
    }

    /// <summary>
    /// 监听触发
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
                    Debug.LogError($"[EventCenter] 监听 {name} 错误: {e.Message}");
                }
            }
        }
    }

    public void Clear()
    {
        eventDic?.Clear();
    }

    protected override void OnDestroy()
    {
        Clear();
        base.OnDestroy();
        Debug.Log("[EventCenter] 销毁");
    }
}