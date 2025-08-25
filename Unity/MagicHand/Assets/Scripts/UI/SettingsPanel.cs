using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 设置面板
/// </summary>
public class SettingsPanel : BasePanel
{
    // 在 Unity 编辑器中为按钮命名（例如 "Btn_Back"）
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
    }
    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case "Panel/ButtonGroup/ButtonBack": // 返回主界面按钮
                Debug.Log("返回主界面");
                UIMgr.GetInstance().HidePanel("SettingsPanel"); // 隐藏当前设置面板
                UIMgr.GetInstance().ShowPanel<MainUI>("MainUI"); // 重新显示主界面
                break;

            //case "Btn_Volume": // 音量设置按钮
            //    Debug.Log("打开音量设置");
            //    // 打开音量设置子面板（如果需要）
            //    break;

            case "Panel/ButtonGroup/ButtonGraphic": // 图形设置按钮
                Debug.Log("打开图形设置");
                // 打开图形设置子面板（如果需要）
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
        //EventCenter.GetInstance().AddEventListener<string>(E_EventType.Event_Button_Setting_Click, OnLoadOver);
        // 可选：注册事件（如设置修改后的回调）
    }

    public override void HideMe()
    {
        //EventCenter.GetInstance().RemoveEventListener<string>(E_EventType.Event_Button_Setting_Click, OnLoadOver);
        base.HideMe();
        // 可选：反注册事件
    }

    private void OnLoadOver(string arg0)
    {
        Debug.Log("设置面板已显示");
        throw new NotImplementedException();
    }

}