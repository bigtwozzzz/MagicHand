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
    [SerializeField] private TextMeshProUGUI textStageRequest; // ��ѡչʾ���
    [SerializeField] private Transform monsterListContainer; // �����б�����
    [SerializeField] private GameObject monsterEntryPrefab; // ������ĿԤ����
    [SerializeField] private TextMeshProUGUI textSceneId; // 场景编号
    private Queue<(GameObject messageObj, float expireTime)> messageQueue = new();
    private const float MessageDuration = 30f;
    private ScrollRect scrollRect;

    protected override void Awake()
    {
        base.Awake();
        
        // 自动挂载魔法UI控制器
        if (GetComponent<MagicUIController>() == null)
        {
            gameObject.AddComponent<MagicUIController>();
            Debug.Log("[MainUI] 自动挂载MagicUIController组件");
        }
        
        // 自动挂载角色UI控制器
        if (GetComponent<PlayerUIController>() == null)
        {
            gameObject.AddComponent<PlayerUIController>();
            Debug.Log("[MainUI] 自动挂载PlayerUIController组件");
        }
        
        Button btnSettings = GetControl<Button>("ButtonGroup/ButtonSettings");
        if (btnSettings == null)
        {
            Debug.LogError("ButtonSettings δ��ȷע�ᣡ");
        }

        // ���� ScrollRect
        scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect == null)
        {
            Debug.LogError($"{nameof(MainUI)}: δ�ҵ� ScrollRect �����");
        }

        // ��֤��Ҫ����
        if (contentPanel == null)
        {
            Debug.LogError($"{nameof(MainUI)}: contentPanel δ��ֵ��");
            enabled = false;
        }

        if (messagePrefab == null)
        {
            Debug.LogError($"{nameof(MainUI)}: messagePrefab δ��ֵ��");
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
        string playerId = DataMgr.GetInstance().UserId; // ������� PlayerMgr
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
                Debug.Log("���������");
                EventCenter.GetInstance().EventTrigger(E_EventType.Event_Button_Setting_Click, "Button Setting");
                UIMgr.GetInstance().HidePanel("MainUI");
                UIMgr.GetInstance().ShowPanel<SettingsPanel>("SettingsPanel", E_UI_Layer.Mid, (panel) =>
                {
                    Debug.Log("SettingsUI ����Ѵ�������ʾ");
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
        // �����ʾʱע���¼�
        EventCenter.GetInstance().AddEventListener<PlayerOnlineNotify>(
            E_EventType.Event_Player_Online, OnPlayerOnline);

        EventCenter.GetInstance().AddEventListener<PlayerOfflineNotify>(
            E_EventType.Event_Player_Offline, OnPlayerOffline);
        EventCenter.GetInstance().AddEventListener<SceneData>(
            E_EventType.Event_Scene_Data_Update_UI, OnSceneDataUpdated);
        DataMgr.GetInstance().SetMainUIReady();
        // 注册事件监听投票请求
        EventCenter.GetInstance().AddEventListener<StageSelectRequestNotify>(
            E_EventType.Event_Stage_Select_Request_Notify,
            OnStageSelectRequestReceived);
    }

    public override void HideMe()
    {
        // �������ʱ��ע���¼�
        EventCenter.GetInstance().RemoveEventListener<PlayerOnlineNotify>(
            E_EventType.Event_Player_Online, OnPlayerOnline);

        EventCenter.GetInstance().RemoveEventListener<PlayerOfflineNotify>(
            E_EventType.Event_Player_Offline, OnPlayerOffline);
        EventCenter.GetInstance().RemoveEventListener<SceneData>(
            E_EventType.Event_Scene_Data_Update_UI, OnSceneDataUpdated);
        EventCenter.GetInstance().RemoveEventListener<StageSelectRequestNotify>(
            E_EventType.Event_Stage_Select_Request_Notify,
            OnStageSelectRequestReceived);
            
        // 停止所有协程
        StopAllCoroutines();
    }
    /// <summary>
    /// �յ����ؿ�ѡ������֪ͨ
    /// </summary>
   private void OnStageSelectRequestReceived(StageSelectRequestNotify notify)
{
    string playerId = notify.PlayerId;
    string stageId = notify.StageId;

    string playerName = null;
        // �Ȼ�ȡ playerName������Ƿ� null
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
            Debug.LogError("[OnStageSelectRequestReceived] StageVotePanel ����ʧ�ܣ�panel Ϊ null");
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
            Debug.LogWarning("[MainUI] ���յ��յĳ������ݣ�");
            return;
        }

        Debug.Log($"[MainUI] ���յ���������: {sceneData.SceneId}");

        // ���³���ID��ʾ
        if (textSceneId != null)
        {
            textSceneId.text = $"����ID: {sceneData.SceneId}";
        }

        ClearMonsterList(); // ��վ��б�
        DisplayMonsterList(sceneData); // ��ʾ���б�
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
            Debug.LogWarning("[MainUI] SceneData.Monsters Ϊ null");
            return;
        }

        foreach (var monster in sceneData.Monsters)
        {
            if (monster == null) continue; // �����ж�

            GameObject entry = Instantiate(monsterEntryPrefab, monsterListContainer);
            if (entry.TryGetComponent<TextMeshProUGUI>(out var text))
            {
                string info = FormatMonsterInfo(monster);
                Debug.Log("���ɵĹ�����Ϣ��\n" + info); //  �������е���
                text.text = info;
            }
            else
            {
                Debug.LogError("������ĿԤ����ȱ�� TextMeshProUGUI �����");
            }
        }
    }
    private string FormatMonsterInfo(MonsterBase monster)
    {
        return $"\n- Monster: {monster.MonsterId}\n" +
               $"  - ����: {GetMonsterTypeName(monster.Type)}\n" +
               $"  - HP: {monster.CurrentHp}/{monster.MaxHp}\n" +
               $"  - ������: {monster.AttackPower} (�ٶ�: {monster.AttackSpeed:F2})\n" +
               $"  - �ƶ��ٶ�: {monster.MoveSpeed:F2}\n" +
               $"  - λ��: ({monster.PosX:F2}, {monster.PosY:F2}, {monster.PosZ:F2})\n" +
               $"  - ����: {monster.Direction:F2}\n" +
               $"  - ������Χ: {monster.AttackRange:F2}\n" +
               $"  - ״̬: {GetMonsterStateName(monster.State)}\n" +
               $"  - ���侭��: {monster.ExpReward}";
    }

    private string GetMonsterTypeName(Common.MonsterType type)
    {
        return type switch
        {
            Common.MonsterType.ZombieBasic => "ZOMBIE_BASIC",
            Common.MonsterType.ZombieFast => "ZOMBIE_FAST",
            _ => "δ֪����",
        };
    }

    private string GetMonsterStateName(Common.MonsterState state)
    {
        return state switch
        {
            Common.MonsterState.MMove => "M_MOVE",
            _ => "δ֪״̬",
        };
    }
    private void OnPlayerOnline(object data)
    {
        if (data is PlayerOnlineNotify notify)
        {
            Debug.Log($"[ϵͳ��Ϣ] ��� <color=green>{notify.PlayerName}</color> ������");
            string msg = $"[ϵͳ] ��� <color=green>{notify.PlayerName}</color> ������";
            AddMessage(msg);
        }
    }

    private void OnPlayerOffline(object data)
    {
        if (data is PlayerOfflineNotify notify)
        {
            Debug.Log($"[ϵͳ��Ϣ] ��� <color=red>{notify.PlayerName}</color> ������");
            string msg = $"[ϵͳ] ��� <color=red>{notify.PlayerName}</color> ������";
            AddMessage(msg);
        }
    }

    /// <summary>
    /// ������Ϣ�� UI
    /// </summary>
    /// <param name="message">��Ϣ���ݣ�֧�ָ��ı���</param>
    private void AddMessage(string message)
    {
        if (messagePrefab == null || contentPanel == null)
        {
            Debug.LogError("[AddMessage] ��Ҫ��Ԥ���������Ϊ�գ�");
            return;
        }

        // ʵ������Ϣ����
        GameObject msgObj = Instantiate(messagePrefab, contentPanel);
        if (msgObj.TryGetComponent<TextMeshProUGUI>(out var textComponent))
        {
            textComponent.text = message;
        }
        else
        {
            Debug.LogError("[AddMessage] messagePrefab ��δ�ҵ� TextMeshProUGUI �����");
        }

        // ��¼��Ϣ�͹���ʱ�䣨�����Զ�������
        float expireTime = Time.time + MessageDuration;
        messageQueue.Enqueue((msgObj, expireTime));

        // �Զ��������ײ�
        ScrollToBottom();
    }

    /// <summary>
    /// �ӳ�һ֡�������ײ���ȷ�������Ѹ���
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
        yield return null; // �ȴ�һ֡��ȷ�������ؽ����
        scrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// ����ѡ����������������Ϣ
    /// </summary>
    private IEnumerator CleanupExpiredMessages()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // ÿ5����һ��

            float currentTime = Time.time;
            while (messageQueue.Count > 0)
            {
                var (msgObj, expireTime) = messageQueue.Peek();
                if (currentTime >= expireTime)
                {
                    messageQueue.Dequeue();
                    if (msgObj != null)
                    {
                        Destroy(msgObj); // ���ٹ�����Ϣ
                    }
                }
                else
                {
                    break; // δ���ڵ���Ϣ���账��
                }
            }
        }
    }
}