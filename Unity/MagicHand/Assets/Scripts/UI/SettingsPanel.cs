using System;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 设置面板
/// </summary>
public class SettingsPanel : BasePanel
{
    // 在 Unity 编辑器中为按钮命名（例如 "Btn_Back"）
    private string returnToPanelName = "MainUI"; // 默认返回 MainUI

    // 新增一个方法，用于设置返回目标
    public void SetReturnTarget(string panelName)
    {
        returnToPanelName = panelName;
    }
    protected override void Awake()
    {
        base.Awake();
        Button btnBack = GetControl<Button>("Panel/ButtonGroup/ButtonBack");
        if (btnBack == null)
        {
            Debug.LogError("ButtonSettings 未正确注册！");
        }
        Button buttonGriphic = GetControl<Button>("Panel/ButtonGroup/ButtonGraphic");
        if (buttonGriphic == null)
        {
            Debug.LogError("ButtonGraphic 未正确注册！");
        }
        Button buttonExit = GetControl<Button>("Panel/ButtonGroup/ButtonExit");
        if (buttonExit == null) {
            Debug.LogError("ButtonExit 未正确注册！");
        }
        Button buttonSound = GetControl<Button>("Panel/ButtonGroup/ButtonSound");
        if (buttonSound == null) {
            Debug.LogError("ButtonSound 未正确注册！");
        }
    }
    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case "Panel/ButtonGroup/ButtonBack": // 返回主界面按钮
                Debug.Log("返回"  + returnToPanelName);
                UIMgr.GetInstance().HidePanel("SettingsPanel"); // 隐藏当前设置面板
                UIMgr.GetInstance().ShowPanel(returnToPanelName);
                break;
            case "Panel/ButtonGroup/ButtonGraphic": // 图形设置按钮
                Debug.Log("打开图形设置");
                // 打开图形设置子面板（如果需要）
                UIMgr.GetInstance().ShowPanel("GraphicPanel");
                UIMgr.GetInstance().HidePanel("SettingsPanel");
                break;
            case "Panel/ButtonGroup/ButtonSound":
                Debug.Log("打开音效设置");
                UIMgr.GetInstance().ShowPanel("SoundPanel");
                UIMgr.GetInstance().HidePanel("SettingsPanel");
                break;
            case "Panel/ButtonGroup/ButtonExit":
                Debug.Log("退出游戏");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // 停止编辑器播放模式
#else
        Application.Quit(); // 退出构建后的应用程序
#endif
                break;
            default:
                base.OnClick(btnName);
                break;
        }
    }

    public override void ShowMe()
    {
        base.ShowMe();
    }

    public override void HideMe()
    {
        base.HideMe();
    }

    private void OnLoadOver()
    {
        Debug.Log("设置面板已显示");
        throw new NotImplementedException();
    }

}