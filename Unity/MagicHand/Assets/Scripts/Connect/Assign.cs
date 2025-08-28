// Assign.cs
using Base;
using Broadcast;
using Character;
using Globalrandom;
using Scene;
using System;
using UnityEngine;

public class Assign : BaseManager<Assign>
{
    public void DispatchNetworkEvent(uint msgId, byte[] msgBody)
    {
        try
        {
            switch (msgId)
            {
                case 101: // 登录响应
                    var loginResponse = LoginResponse.Parser.ParseFrom(msgBody);
                    // EventCenter.GetInstance().EventTrigger(E_EventType.Event_Login_Success, loginResponse);

                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Login_Success, loginResponse);
                    });
                    break;

                case 102: // 登出响应
                    var logoutResponse = LogoutResponse.Parser.ParseFrom(msgBody);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Logout, logoutResponse);
                    });
                    break;

                case 201: // 玩家上线广播
                    var onlineNotify = PlayerOnlineNotify.Parser.ParseFrom(msgBody);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Player_Online, onlineNotify);
                    });
                    break;

                case 202: // 玩家下线广播
                    var offlineNotify = PlayerOfflineNotify.Parser.ParseFrom(msgBody);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Player_Offline, offlineNotify);
                    });
                    break;

                case 302: // 角色信息广播
                    var characterInfo = CharacterBase.Parser.ParseFrom(msgBody);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Character_Info_Update, characterInfo);
                    });
                    break;

                case 203: // 关卡选择请求通知
                    var stageSelectNotify = StageSelectRequestNotify.Parser.ParseFrom(msgBody);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Stage_Select_Request_Notify, stageSelectNotify);
                    });
                    break;

                case 204: // 关卡选择结果通知
                    var stageResultNotify = StageSelectResultNotify.Parser.ParseFrom(msgBody);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Stage_Select_Result_Notify, stageResultNotify);
                    });
                    break;

                case 301: // 场景信息广播
                    var sceneData = SceneData.Parser.ParseFrom(msgBody);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Scene_Data_Update, sceneData);
                    });
                    break;
                case 401: // 随机种子广播
                    var randomSeed = GlobalRandomNum .Parser.ParseFrom(msgBody);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Global_Random_Seed, randomSeed);
                    });
                    break;
                default:
                    Debug.LogWarning($"[Decoder] Unknown message ID: {msgId}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Decoder] Failed to parse message ID {msgId}: {e.Message}\n{e.StackTrace}");
        }
    }
}