using Base; // 确保包含 EventCenter、BasePanel、PoolMgr、Gain 等
using Broadcast;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主界面 UI 面板（显示系统消息，如玩家上线/下线）
/// </summary>
public class MainUI : BasePanel
{
    [SerializeField] private Transform contentPanel; // 消息内容容器
    [SerializeField] private GameObject messagePrefab; // 消息条目预制体

    // 存储消息及其过期时间（可选：用于自动清理）
    private Queue<(GameObject messageObj, float expireTime)> messageQueue = new();
    private const float MessageDuration = 30f; // 消息保留30秒（可根据需要调整）

    private ScrollRect scrollRect; // 缓存 ScrollRect 引用

    protected override void Awake()
    {
        base.Awake(); // 必须先调用基类，完成控件自动注册
        Button btnSettings = GetControl<Button>("ButtonGroup/ButtonSettings");
        if (btnSettings == null)
        {
            Debug.LogError("ButtonSettings 未正确注册！");
        }
        // 查找 ScrollRect
        scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect == null)
        {
            Debug.LogError($"{nameof(MainUI)}: 未找到 ScrollRect 组件！");
        }

        // 验证必要引用
        if (contentPanel == null)
        {
            Debug.LogError($"{nameof(MainUI)}: contentPanel 未赋值！");
            enabled = false;
        }

        if (messagePrefab == null)
        {
            Debug.LogError($"{nameof(MainUI)}: messagePrefab 未赋值！");
            enabled = false;
        }

    }
    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case "ButtonGroup/ButtonSettings":
                Debug.Log("打开设置面板");
                EventCenter.GetInstance().EventTrigger(E_EventType.Event_Button_Setting_Click, "Button Setting");
                UIMgr.GetInstance().HidePanel("MainUI");
                UIMgr.GetInstance().ShowPanel<SettingsPanel>("SettingsPanel", E_UI_Layer.Mid, (panel) =>
                {
                    Debug.Log("SettingsUI 面板已创建并显示");
                });


                break;

            //case "Btn_Bag":
            //    Debug.Log("打开背包");
            //    UIMgr.GetInstance().ShowPanel<BagPanel>("BagPanel");
            //    break;

            //case "Btn_Shop":
            //    Debug.Log("打开商店");
            //    UIMgr.GetInstance().ShowPanel<ShopPanel>("ShopPanel");
            //    break;

            //case "Btn_Skill":
            //    Debug.Log("打开技能面板");
            //    UIMgr.GetInstance().ShowPanel<SkillPanel>("SkillPanel");
            //    break;

            default:
                base.OnClick(btnName);
                break;
        }
    }
    public override void ShowMe()
    {
        // 面板显示时注册事件
        EventCenter.GetInstance().AddEventListener<PlayerOnlineNotify>(
            E_EventType.Event_Player_Online, OnPlayerOnline);

        EventCenter.GetInstance().AddEventListener<PlayerOfflineNotify>(
            E_EventType.Event_Player_Offline, OnPlayerOffline);

        // 可选：启动消息清理协程
        // StartCoroutine(CleanupExpiredMessages());
        // 示例：添加几个测试按钮
    }

    public override void HideMe()
    {
        // 面板隐藏时反注册事件
        EventCenter.GetInstance().RemoveEventListener<PlayerOnlineNotify>(
            E_EventType.Event_Player_Online, OnPlayerOnline);

        EventCenter.GetInstance().RemoveEventListener<PlayerOfflineNotify>(
            E_EventType.Event_Player_Offline, OnPlayerOffline);
    }

    private void OnPlayerOnline(object data)
    {
        if (data is PlayerOnlineNotify notify)
        {
            Debug.Log($"[系统消息] 玩家 <color=green>{notify.PlayerName}</color> 上线了");
            string msg = $"[系统] 玩家 <color=green>{notify.PlayerName}</color> 上线了";
            AddMessage(msg);
        }
    }

    private void OnPlayerOffline(object data)
    {
        if (data is PlayerOfflineNotify notify)
        {
            Debug.Log($"[系统消息] 玩家 <color=red>{notify.PlayerName}</color> 下线了");
            string msg = $"[系统] 玩家 <color=red>{notify.PlayerName}</color> 下线了";
            AddMessage(msg);
        }
    }

    /// <summary>
    /// 添加消息到 UI
    /// </summary>
    /// <param name="message">消息内容（支持富文本）</param>
    private void AddMessage(string message)
    {
        if (messagePrefab == null || contentPanel == null)
        {
            Debug.LogError("[AddMessage] 必要的预制体或容器为空！");
            return;
        }

        // 实例化消息对象
        GameObject msgObj = Instantiate(messagePrefab, contentPanel);
        if (msgObj.TryGetComponent<TextMeshProUGUI>(out var textComponent))
        {
            textComponent.text = message;
        }
        else
        {
            Debug.LogError("[AddMessage] messagePrefab 中未找到 TextMeshProUGUI 组件！");
        }

        // 记录消息和过期时间（用于自动清理）
        float expireTime = Time.time + MessageDuration;
        messageQueue.Enqueue((msgObj, expireTime));

        // 自动滚动到底部
        ScrollToBottom();
    }

    /// <summary>
    /// 延迟一帧滚动到底部，确保布局已更新
    /// </summary>
    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            GlobalMonoMgr.GetInstance().StartCoroutine(DelayedScroll());
        }
    }

    private IEnumerator DelayedScroll()
    {
        yield return null; // 等待一帧，确保布局重建完成
        scrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// （可选）定期清理过期消息
    /// </summary>
    private IEnumerator CleanupExpiredMessages()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // 每5秒检查一次

            float currentTime = Time.time;
            while (messageQueue.Count > 0)
            {
                var (msgObj, expireTime) = messageQueue.Peek();
                if (currentTime >= expireTime)
                {
                    messageQueue.Dequeue();
                    if (msgObj != null)
                    {
                        Destroy(msgObj); // 销毁过期消息
                    }
                }
                else
                {
                    break; // 未过期的消息无需处理
                }
            }
        }
    }
}