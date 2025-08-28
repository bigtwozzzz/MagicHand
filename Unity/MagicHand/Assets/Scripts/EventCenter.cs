using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// ͨ���¼��ӿ�
/// </summary>
public interface IEventInfo { }

/// <summary>
/// �����Ͳ������¼���װ��
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
/// �޲��¼���װ��
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
/// �¼�����ö��
/// </summary>  
public enum E_EventType
{
    // ���ָ��
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
    // ������ָ��
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

    // ϵͳָ��
    Event_Platform_Loaded,
    Event_LoadScene_Progress,
    Event_Lock_Window,

    // ��Ϸ�߼�
    Event_Character_Spawn_Ready,
    Event_Monster_Dead,
    Event_Player_Dead,
    Event_Stage_Vote_Result,
    // �����¼�
    Event_Keycode_Input,
    Event_Mouse_Input,
    Event_MouseX_Input,
    Event_MouseY_Input,
    Event_Horizontal_Input,
    Event_Vertical_Input
}

/// <summary>
/// �¼����� ���� ����ģʽ
/// ���ܣ��¼�ע�ᡢ�ַ����Ƴ�
/// ���ģʽ���۲���ģʽ + ���� + �ֵ�
/// ע�⣺
/// - ��֧�� lambda ����ʽ��Ϊ�����������޷���ȷ�Ƴ���
/// - �����л�ʱ������� Clear() �����糡������
/// </summary>
public class EventCenter : BaseManager<EventCenter>
{
    private Dictionary<E_EventType, IEventInfo> eventDic;

    protected override void Awake()
    {
        //  1. �ȵ��ø��࣬ȷ�������߼�
        base.Awake();

        //  2. ȷ��ֻ��ʼ��һ��
        if (eventDic != null) return;

        eventDic = new Dictionary<E_EventType, IEventInfo>();

        //  3. ���Ϊ���泡�����٣������� Awake �е��ã�
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// ���Ӵ��������¼�����
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
    /// �����޲��¼�����
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
    /// �Ƴ����������¼�����
    /// ע�⣺��֧�� lambda�����봫����ͬ��������
    /// </summary>
    public void RemoveEventListener<T>(E_EventType name, UnityAction<T> action)
    {
        // ���� null ���
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
    /// �Ƴ��޲��¼�����
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
    /// �������������¼�
    /// </summary>
    public void EventTrigger<T>(E_EventType name, T info)
    {
        //Debug.Log($"[EventCenter] �����¼�: {name}, ��������: {typeof(T)}, ֵ: {info}");
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
    /// �����޲��¼�
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
                    Debug.LogError($"[EventCenter] �����¼� {name} ʱ�����쳣: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// ��������¼�����������ʹ�ã�
    /// �����ڳ����л���ģ������ʱ����
    /// </summary>
    public void Clear()
    {
        if (eventDic != null)
        {
            eventDic.Clear();
            // Debug.Log("[EventCenter] �����¼������������"); 
            //  ��ѡ������ʱ����ע�͵���־������ OnDestroy ʱ��ӡʧ��
        }
    }

    protected override void OnDestroy()
    {
        //  1. �����¼�
        Clear();

        //  2. �����������絥�������ÿգ�
        base.OnDestroy();

        //  3. ��ѡ��־��ע�⣺��ʱ OnDestroy �д�ӡ��ʧ�ܣ�
        Debug.Log("[EventCenter] �����٣���Դ�ͷ���ɡ�");
    }
}