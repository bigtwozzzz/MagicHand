using Broadcast;
using Enemy;
using Scene;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : BasePanel
{
    [SerializeField] private Transform contentPanel;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private TextMeshProUGUI textStageRequest; // 可选展示面板
    [SerializeField] private Transform monsterListContainer; // 怪物列表容器
    [SerializeField] private GameObject monsterEntryPrefab; // 怪物条目预制体
    [SerializeField] private TextMeshProUGUI textSceneId; // 新增引用
    private Queue<(GameObject messageObj, float expireTime)> messageQueue = new();
    private const float MessageDuration = 30f;
    private ScrollRect scrollRect;

    protected override void Awake()
    {
        base.Awake();
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
        Button btnVote = GetControl<Button>("StageVote");
        // 初始化按钮点击事件
        if (btnVote == null)
        {
            Debug.LogError("Btn_Stage_Vote 未注册");
        }
        
    }


    private void RequestStageChange()
    {
        string playerId = DataMgr.GetInstance().UserId; // 假设存在 PlayerMgr
        string stageId = DataMgr.GetInstance().SceneData.SceneId;

        var selectStageCmd = new Gain.PlayerCommandData(E_EventType.Event_Player_Command_Select_Stage)
        {
            StringParam1 = playerId,
            StringParam2 = stageId
        };
        Debug.Log($"[DEBUG] Sent stage select request: {stageId}");
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Player_Command, selectStageCmd);
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
                    panel.SetReturnTarget("MainUI");
                });
                break;
            case "StageVote":
                RequestStageChange();
                break;
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
        EventCenter.GetInstance().AddEventListener<SceneData>(
            E_EventType.Event_Scene_Data_Update_UI, OnSceneDataUpdated);
        DataMgr.GetInstance().SetMainUIReady();
        // 注册事件：收到投票请求
        EventCenter.GetInstance().AddEventListener<StageSelectRequestNotify>(
            E_EventType.Event_Stage_Select_Request_Notify,
            OnStageSelectRequestReceived);
    }

    public override void HideMe()
    {
        // 面板隐藏时反注册事件
        EventCenter.GetInstance().RemoveEventListener<PlayerOnlineNotify>(
            E_EventType.Event_Player_Online, OnPlayerOnline);

        EventCenter.GetInstance().RemoveEventListener<PlayerOfflineNotify>(
            E_EventType.Event_Player_Offline, OnPlayerOffline);
        EventCenter.GetInstance().RemoveEventListener<SceneData>(
            E_EventType.Event_Scene_Data_Update_UI, OnSceneDataUpdated);
        EventCenter.GetInstance().RemoveEventListener<StageSelectRequestNotify>(
            E_EventType.Event_Stage_Select_Request_Notify,
            OnStageSelectRequestReceived);

    }
    /// <summary>
    /// 收到“关卡选择请求”通知
    /// </summary>
   private void OnStageSelectRequestReceived(StageSelectRequestNotify notify)
{
    string playerId = notify.PlayerId;
    string stageId = notify.StageId;

    string playerName = null;
        // 先获取 playerName，检查是否 null
    var characterInfo = DataMgr.GetInstance().GetCharacterInfo(playerId);
    if (characterInfo == null)
    {
        playerName = DataMgr.GetInstance().GetPlayerName(playerId);
    } 
    else
    {
        playerName = characterInfo.PlayerName;
    }
         


   UIMgr.GetInstance().ShowPanel<StageVotePanel>("StageVotePanel", E_UI_Layer.Top, (panel) =>
    {
        if (panel == null)
        {
            Debug.LogError("[OnStageSelectRequestReceived] StageVotePanel 创建失败，panel 为 null");
            return;
        }

        panel.ShowVoteRequest(playerId, playerName, stageId, stageId.ToLower());

        UIMgr.GetInstance().HidePanel("MainUI");

    });
}

    private void OnSceneDataUpdated(SceneData sceneData)
    {
        if (sceneData == null)
        {
            Debug.LogWarning("[MainUI] 接收到空的场景数据！");
            return;
        }

        Debug.Log($"[MainUI] 接收到场景数据: {sceneData.SceneId}");

        // 更新场景ID显示
        if (textSceneId != null)
        {
            textSceneId.text = $"场景ID: {sceneData.SceneId}";
        }

        ClearMonsterList(); // 清空旧列表
        DisplayMonsterList(sceneData); // 显示新列表
    }

    private void ClearMonsterList()
    {
        foreach (Transform child in monsterListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void DisplayMonsterList(SceneData sceneData)
    {
        if (sceneData.Monsters == null)
        {
            Debug.LogWarning("[MainUI] SceneData.Monsters 为 null");
            return;
        }

        foreach (var monster in sceneData.Monsters)
        {
            if (monster == null) continue; // 防空判断

            GameObject entry = Instantiate(monsterEntryPrefab, monsterListContainer);
            if (entry.TryGetComponent<TextMeshProUGUI>(out var text))
            {
                string info = FormatMonsterInfo(monster);
                Debug.Log("生成的怪物信息：\n" + info); //  添加这行调试
                text.text = info;
            }
            else
            {
                Debug.LogError("怪物条目预制体缺少 TextMeshProUGUI 组件！");
            }
        }
    }
    private string FormatMonsterInfo(MonsterBase monster)
    {
        return $"\n- Monster: {monster.MonsterId}\n" +
               $"  - 类型: {GetMonsterTypeName(monster.Type)}\n" +
               $"  - HP: {monster.CurrentHp}/{monster.MaxHp}\n" +
               $"  - 攻击力: {monster.AttackPower} (速度: {monster.AttackSpeed:F2})\n" +
               $"  - 移动速度: {monster.MoveSpeed:F2}\n" +
               $"  - 位置: ({monster.PosX:F2}, {monster.PosY:F2}, {monster.PosZ:F2})\n" +
               $"  - 方向: {monster.Direction:F2}\n" +
               $"  - 攻击范围: {monster.AttackRange:F2}\n" +
               $"  - 状态: {GetMonsterStateName(monster.State)}\n" +
               $"  - 掉落经验: {monster.ExpReward}";
    }

    private string GetMonsterTypeName(Common.MonsterType type)
    {
        return type switch
        {
            Common.MonsterType.ZombieBasic => "ZOMBIE_BASIC",
            Common.MonsterType.ZombieFast => "ZOMBIE_FAST",
            _ => "未知类型",
        };
    }

    private string GetMonsterStateName(Common.MonsterState state)
    {
        return state switch
        {
            Common.MonsterState.MMove => "M_MOVE",
            _ => "未知状态",
        };
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