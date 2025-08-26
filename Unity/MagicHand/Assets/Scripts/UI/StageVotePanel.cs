using Broadcast;
using Common;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡投票面板
/// 接收关卡选择请求，显示投票界面，发送投票结果
/// </summary>
public class StageVotePanel : BasePanel
{
    // UI 控件
    private Text textMessage;
    private Button btnConfirm;
    private Button btnReject;
    private Text textResult;

    // 当前投票状态
    private string currentStageId = "";
    private bool isVoted = false;

    protected override void Awake()
    {
        base.Awake();

        // 查找 UI 元素
        textMessage = GetControl<Text>("Panel/Content/TextMessage");
        btnConfirm = GetControl<Button>("Panel/ButtonGroup/ButtonConfirm");
        btnReject = GetControl<Button>("Panel/ButtonGroup/ButtonReject");
        textResult = GetControl<Text>("Panel/Content/TextResult");

        // 验证是否找到
        if (textMessage == null) Debug.LogError("TextMessage 未找到！");
        if (btnConfirm == null) Debug.LogError("ButtonConfirm 未找到！");
        if (btnReject == null) Debug.LogError("ButtonReject 未找到！");
        if (textResult == null) Debug.LogError("TextResult 未找到！");

        // 绑定按钮事件
        if (btnConfirm != null)
            btnConfirm.onClick.AddListener(OnClick_Confirm);

        if (btnReject != null)
            btnReject.onClick.AddListener(OnClick_Reject);
    }

    protected override void OnClick(string btnName)
    {
        // 如果使用统一事件分发，也可在此处理
        // 当前使用直接绑定，此方法可留空或调用 base
        base.OnClick(btnName);
    }

    public override void ShowMe()
    {
        base.ShowMe();

        // 注册事件：收到投票请求
        EventCenter.GetInstance().AddEventListener<StageSelectRequestNotify>(
            E_EventType.Event_Stage_Select_Request,
            OnStageSelectRequestReceived);

        // 注册事件：收到投票结果
        EventCenter.GetInstance().AddEventListener<StageSelectResultNotify>(
            E_EventType.Event_Stage_Select_Result,
            OnStageSelectResultWrapper);

        // 初始化状态
        ResetPanel();
    }

    public override void HideMe()
    {
        // 反注册事件
        EventCenter.GetInstance().RemoveEventListener<StageSelectRequestNotify>(
            E_EventType.Event_Stage_Select_Request,
            OnStageSelectRequestReceived);

        EventCenter.GetInstance().RemoveEventListener<StageSelectResultNotify>(
            E_EventType.Event_Stage_Select_Result,
            OnStageSelectResultWrapper);

        // 清理
        CancelInvoke();
        ResetPanel();

        base.HideMe();
    }

    /// <summary>
    /// 重置面板状态
    /// </summary>
    private void ResetPanel()
    {
        currentStageId = "";
        isVoted = false;
        if (textMessage != null) textMessage.text = "";
        if (textResult != null) textResult.text = "";
        if (btnConfirm != null) btnConfirm.interactable = true;
        if (btnReject != null) btnReject.interactable = true;
    }

    /// <summary>
    /// 收到“关卡选择请求”通知
    /// </summary>
    private void OnStageSelectRequestReceived(StageSelectRequestNotify notify)
    {
        string playerId = notify.PlayerId;
        string stageId = notify.StageId;
        string stageName = string.IsNullOrEmpty(notify.StageName) ? stageId : notify.StageName;

        // 获取玩家名称（示例：可从 RoleMgr 获取）
        string playerName = GetPlayerName(playerId);

        ShowVoteRequest(playerId, playerName, stageId, stageName);
    }

    /// <summary>
    /// 显示投票请求
    /// </summary>
    public void ShowVoteRequest(string playerId, string playerName, string stageId, string stageName)
    {
        currentStageId = stageId;
        isVoted = false;

        string msg = $"玩家 <color=yellow>[{playerName}]</color> 想进入 <color=cyan>[{stageName}]</color>，你是否同意？";
        if (textMessage != null)
            textMessage.text = msg;

        if (textResult != null)
            textResult.text = "等待您的选择...";

        // 显示面板
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 玩家点击“同意”
    /// </summary>
    private void OnClick_Confirm()
    {
        if (isVoted || string.IsNullOrEmpty(currentStageId)) return;
        Vote(StageSelectState.Confirmed);
    }

    /// <summary>
    /// 玩家点击“拒绝”
    /// </summary>
    private void OnClick_Reject()
    {
        if (isVoted || string.IsNullOrEmpty(currentStageId)) return;
        Vote(StageSelectState.Rejected);
    }

    /// <summary>
    /// 发送投票
    /// </summary>
    private void Vote(StageSelectState state)
    {
        isVoted = true;

        // 禁用按钮
        SetButtonsInteractable(false);

        // 更新 UI
        if (textResult != null)
            textResult.text = state == StageSelectState.Confirmed ? " 已同意" : " 已拒绝";

        // 发送命令
        var cmd = new Gain.PlayerCommandData(E_EventType.Event_Player_Command_Confirm_Stage)
        {
            StringParam1 = currentStageId,
            IntParam1 = (int)state
        };

        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Player_Command, cmd);
    }

    /// <summary>
    /// 设置按钮是否可交互
    /// </summary>
    private void SetButtonsInteractable(bool interactable)
    {
        if (btnConfirm != null) btnConfirm.interactable = interactable;
        if (btnReject != null) btnReject.interactable = interactable;
    }

    /// <summary>
    /// 收到“投票结果”通知的包装器
    /// </summary>
    private void OnStageSelectResultWrapper(StageSelectResultNotify notify)
    {
        OnStageSelectResult(notify.IsAllConfirmed);
    }

    /// <summary>
    /// 处理投票结果
    /// </summary>
    /// <summary>
    /// 处理投票结果
    /// </summary>
    private void OnStageSelectResult(bool isAllConfirmed)
    {
        SetButtonsInteractable(false);

        if (isAllConfirmed)
        {
            if (textResult != null)
                textResult.text = " 全员同意！准备进入关卡...";

            // 延迟跳转
            Invoke(nameof(LoadStage), 1.5f);
        }
        else
        {
            if (textResult != null)
                textResult.text = " 投票未通过，关卡选择取消";

            // 延迟隐藏面板
            Invoke(nameof(HidePanel), 2.0f); // 改成 HidePanel
        }
    }

    /// <summary>
    /// 加载关卡（模拟）
    /// </summary>
    private void LoadStage()
    {
        Debug.Log($"[StageVotePanel] 正在加载关卡: {currentStageId}");
        // 实际项目中替换为场景加载
        // SceneLoader.LoadSceneAsync(currentStageId);

        HidePanel(); // 隐藏面板
    }

    /// <summary>
    /// 安全隐藏自己
    /// </summary>
    private void HidePanel()
    {
        UIMgr.GetInstance().HidePanel("StageVotePanel");
    }
    /// <summary>
    /// 获取玩家名称（示例实现）
    /// </summary>
    private string GetPlayerName(string playerId)
    {
        // 方案1：直接返回 ID 截取
        return $"玩家{playerId.Substring(playerId.Length - 6)}";

        // 方案2：从角色管理器获取（推荐）
        // var role = RoleMgr.GetInstance().GetRoleByPlayerId(playerId);
        // return role?.RoleName ?? $"玩家{playerId}";
    }
}