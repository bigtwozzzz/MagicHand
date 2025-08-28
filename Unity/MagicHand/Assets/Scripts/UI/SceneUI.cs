using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneUI : BasePanel
{
    protected override void Awake()
    {
        base.Awake();
        Button btnSettings = GetControl<Button>("ButtonGroup/ButtonSettings");
        if (btnSettings == null)
        {
            Debug.LogError("ButtonSettings 未正确注册！");
        }
    }
    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case "ButtonGroup/ButtonSettings":
                Debug.Log("打开设置面板");
                EventCenter.GetInstance().EventTrigger(E_EventType.Event_Button_Setting_Click, "Button Setting");
                UIMgr.GetInstance().HidePanel("SceneUI");
                UIMgr.GetInstance().ShowPanel<SettingsPanel>("SettingsPanel", E_UI_Layer.Mid, (panel) =>
                {
                    Debug.Log("SettingsUI 面板已创建并显示");
                    panel.SetReturnTarget("SceneUI");
                });
                break;
            default:
                base.OnClick(btnName);
                break;
        }
    }
}
