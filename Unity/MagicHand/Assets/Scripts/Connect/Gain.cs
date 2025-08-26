using Base;
using Broadcast;
using Character;
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gain : BaseManager<Gain>
{
    [System.Serializable]
    public class PlayerCommandData
    {
        public E_EventType CommandType;
        public string StringParam1;
        public string StringParam2;
        public float FloatParam1;
        public float FloatParam2;
        public int IntParam1;

        public PlayerCommandData(E_EventType type) => CommandType = type;
    }
    private bool isLoggingOut = false;
    private Encoder _encoder;

    protected override void Awake()
    {
        Debug.Log("[Gain] Awake called - initializing...");
        _encoder = Encoder.GetInstance();
    }

    private void Start()
    {
        Debug.Log("[Gain] Start called - Registering Event_Player_Command listener");
        EventCenter.GetInstance().AddEventListener<PlayerCommandData>(
            E_EventType.Event_Player_Command,
            HandlePlayerCommand);
    }

    /// <summary>
    /// 统一处理所有来自玩家的命令
    /// </summary>
    private void HandlePlayerCommand(PlayerCommandData commandData)
    {
        if (commandData == null)
        {
            Debug.LogError("[Gain] Received null PlayerCommandData!");
            return;
        }

        Debug.Log($"[Gain] Handling player command: {commandData.CommandType}");

        switch (commandData.CommandType)
        {
            case E_EventType.Event_Player_Command_Login:
                HandleLogin(commandData.StringParam1, commandData.StringParam2);
                break;

            case E_EventType.Event_Player_Command_Select_Stage:
                HandleSelectStage(commandData.StringParam1);
                break;

            case E_EventType.Event_Player_Command_Confirm_Stage:
                HandleConfirmStage(commandData.StringParam1, commandData.IntParam1);
                break;

            case E_EventType.Event_Player_Command_Move:
                HandleMove(commandData.StringParam1, commandData.FloatParam1, commandData.FloatParam2);
                break;

            case E_EventType.Event_Player_Command_Logout:
                HandleLogout();
                break;

            default:
                Debug.LogWarning($"[Gain] Unhandled command type: {commandData.CommandType}");
                break;
        }
    }

    /// <summary>
    /// 处理登录请求
    /// </summary>
    private void HandleLogin(string username, string password)
    {
        Debug.Log($"[Gain] Processing login for: {username}");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("[Gain] Username or password is empty!");
            return;
        }

        // 构造 Protobuf 登录请求
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };

        // 发送到服务器 (msgId = 1)
        _encoder.Send(1, loginRequest);
        Debug.Log("[Gain] Login request sent to server via Encoder.");
    }

    /// <summary>
    /// 处理选择关卡
    /// </summary>
    private void HandleSelectStage(string stageId)
    {
        if (string.IsNullOrEmpty(DataMgr.GetInstance().UserId))
        {
            Debug.LogError("[ERROR] You must be logged in to select a stage.");
            return;
        }

        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogError("[ERROR] Stage ID is empty!");
            return;
        }
        if (DataMgr.GetInstance().IsLoggedIn)
        {
            Debug.Log($"当前用户ID: {DataMgr.GetInstance().UserId}");
            var req = new PlayerSelectStageRequest
            {
                PlayerId = DataMgr.GetInstance().UserId,
                StageId = stageId
            };
            _encoder.Send(5, req); // msgId = 5
            Debug.Log($"[DEBUG] Sent stage select request: {stageId}");
        }
        else
        {
            Debug.Log("用户未登录");
        }
    }

    /// <summary>
    /// 处理确认关卡
    /// </summary>
    private void HandleConfirmStage(string stageId, int state)
    {
        if (string.IsNullOrEmpty(DataMgr.GetInstance().UserId))
        {
            Debug.LogError("[ERROR] You must be logged in to confirm a stage.");
            return;
        }

        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogError("[ERROR] Stage ID is empty!");
            return;
        }

        // 假设 StageSelectState 是一个 enum，0=CONFIRMED, 1=REJECTED
        var stateEnum = (StageSelectState)state;
        if (stateEnum != StageSelectState.Confirmed && stateEnum != StageSelectState.Rejected)
        {
            Debug.LogError("[ERROR] Invalid state. Use CONFIRMED (0) or REJECTED (1).");
            return;
        }
        if (DataMgr.GetInstance().IsLoggedIn)
        {
            var resp = new PlayerConfirmStageResponse
            {
                PlayerId = DataMgr.GetInstance().UserId,
                StageId = stageId,
                State = stateEnum
            };
            _encoder.Send(6, resp); // msgId = 6
            Debug.Log($"[DEBUG] Sent stage confirm response: {stageId} {stateEnum}");
        }
        else
        {
            Debug.Log("用户未登录");
        }
    }

    /// <summary>
    /// 处理角色移动
    /// </summary>
    private void HandleMove(string roleId, float targetX, float targetY)
    {
        if (string.IsNullOrEmpty(roleId))
        {
            Debug.LogError("[ERROR] Role ID is empty!");
            return;
        }

        var moveRequest = new MoveRequest
        {
            RoleId = roleId,
            TargetX = targetX,
            TargetY = targetY
        };
        _encoder.Send(3, moveRequest); // msgId = 3
        Debug.Log($"[DEBUG] Sent move request: Role={roleId}, Target=({targetX}, {targetY})");
    }

    /// <summary>
    /// 处理登出
    /// </summary>
    private void HandleLogout()
    {
        if (isLoggingOut)
        {
            Debug.Log("[Gain] Logout already in progress, skip.");
            return;
        }

        if (!DataMgr.GetInstance().IsLoggedIn)
        {
            Debug.LogWarning("[Gain] Logout ignored: not logged in."); 
            return;
        }

        isLoggingOut = true;

        var logoutRequest = new LogoutRequest
        {
            UserId = DataMgr.GetInstance().UserId,
        };

        _encoder.Send(2, logoutRequest);
        Debug.Log("[Gain] Logout request sent.");
    }
    protected  void OnDestroy()
    {
        //  安全移除事件监听
        var eventCenter = EventCenter.GetInstance();
        if (eventCenter != null)
        {
            eventCenter.RemoveEventListener<PlayerCommandData>(
                E_EventType.Event_Player_Command,
                HandlePlayerCommand);
        }

        Debug.Log("[Gain] 已注销事件监听，准备销毁。");
    }
    protected  void OnApplicationQuit()
    {
        SyncLogoutAndShutdown();
    }

    public void SyncLogoutAndShutdown()
    {
        if (!DataMgr.GetInstance().IsLoggedIn)
            return;

        Debug.Log("[Gain] SyncLogout: Starting logout and flush...");

        var req = new LogoutRequest { UserId = DataMgr.GetInstance().UserId };
        _encoder.Send(2, req);

        // 如果有 Flush 方法，确保调用
        // _encoder.Flush();

        // 可选：短暂停顿（不推荐在主线程 Sleep）
        // 更好的方式是使用协程 + yield，但在 OnApplicationQuit 中协程可能不执行

        Debug.Log("[Gain] SyncLogout: Completed.");
    }
}