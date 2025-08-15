using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Base;
using Common;
/// <summary>
/// 全局数据管理器，用于存储玩家数据、游戏状态等
/// </summary>
public class DataMgr : BaseManager<DataMgr>
{

    // 当前登录的用户信息
    private string _userId;
    private string _loginStatus; // 假设 LoginResponse 中有 Status 字段，类型为 LoginStatus

    // 只读属性，供外部访问
    public string UserId => _userId;
    public string LoginStatus => _loginStatus;
    public bool IsLoggedIn => _userId != ""; // 简单判断是否已登录

    private void Awake()
    {
    }

    private void Start()
    {
        EventCenter.GetInstance().AddEventListener<LoginResponse>(
            E_EventType.Event_Login_Success,
            OnLoginSuccess);
    }

    private void OnDestroy()
    {
        EventCenter.GetInstance().RemoveEventListener<LoginResponse>(
            E_EventType.Event_Login_Success,
            OnLoginSuccess);
    }

    /// <summary>
    /// 处理登录成功事件，保存用户数据
    /// </summary>
    private void OnLoginSuccess(LoginResponse loginResponse)
    {
        if (loginResponse == null)
        {
            Debug.LogError("[DataMgr] LoginResponse is null!");
            return;
        }

        _userId = loginResponse.UserId;
        _loginStatus = loginResponse.Status;

        Debug.Log($"[DataMgr] 登录成功！用户ID: {_userId}, 状态: {_loginStatus}");

        // 可选：触发一个事件，通知其他模块用户数据已加载
        // EventCenter.GetInstance().EventTrigger(E_EventType.Event_User_Data_Loaded);
    }

    /// <summary>
    /// 提供一个方法用于登出时清除数据
    /// </summary>
    public void ClearUserData()
    {
        _userId = "";
        _loginStatus = "OffLine"; // 或其他默认状态

        Debug.Log("[DataMgr] 用户数据已清除。");
    }
}