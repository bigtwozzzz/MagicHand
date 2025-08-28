using Base; // 确保包含 EventCenter、SceneMgr、PoolMgr、Gain 等类的命名空间
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录界面面板（基于 BasePanel 框架重构）
/// </summary>
public class LoginUI : BasePanel
{
    // 无需手动声明字段，通过 GetControl<T> 自动查找
    private TMP_InputField usernameField;
    private TMP_InputField passwordField;
    private Button loginButton;
    private string _cachedUsername;
    protected override void Awake()
    {
        // 先调用基类的 Awake，自动注册所有子控件
        base.Awake();

        // 获取控件引用（通过名字自动查找）
        usernameField = GetControl<TMP_InputField>("UsernameField");
        passwordField = GetControl<TMP_InputField>("PasswordField");
        loginButton = GetControl<Button>("LoginButton");
    }

    protected override void OnClick(string btnName)
    {
        base.OnClick(btnName);

        switch (btnName)
        {
            case "LoginButton":
                OnLoginButtonClick();
                break;
            // 其他按钮可继续添加
            default:
                Debug.Log($"未知按钮点击: {btnName}");
                break;
        }
    }

    private void OnLoginButtonClick()
    {
        string username = usernameField.text.Trim();
        string password = passwordField.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("用户名不能为空！");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("密码不能为空！");
            return;
        }
        _cachedUsername = username;

        var loginCmd = new Gain.PlayerCommandData(E_EventType.Event_Player_Command_Login)
        {
            StringParam1 = username,
            StringParam2 = password
        };

        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Player_Command, loginCmd);

        Debug.Log($"[LoginUI] 登录命令已发送: {username}");
    }

    public override void ShowMe()
    {
        // 面板显示时注册事件
        EventCenter.GetInstance().AddEventListener<LoginResponse>(
            E_EventType.Event_Login_Success,
            OnLoginSuccess);
    }

    public override void HideMe()
    {
        // 面板隐藏时反注册事件
        EventCenter.GetInstance().RemoveEventListener<LoginResponse>(
            E_EventType.Event_Login_Success,
            OnLoginSuccess);
    }

    private void OnLoginSuccess(LoginResponse response)
    {
        Debug.Log($"[LoginUI] 登录成功！用户ID: {response.UserId}, 状态: {response.Status}");
        string playerId = response.UserId;
        if (!string.IsNullOrEmpty(_cachedUsername))
        {
            DataMgr.GetInstance().SetPlayerName(playerId, _cachedUsername);
        }
        // 隐藏自己
        UIMgr.GetInstance().HidePanel("LoginUI");
        // 获取全屏管理器并锁定全屏
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Lock_Window);


        // 加载主场景
        SceneMgr.GetInstance().SafeLoadScene("GamingScene", OnLoadOver);
        
    }

    private void OnLoadOver(bool success)
    {
        if (success)
        {
            Debug.Log("GamingScene 加载完成！");

            UIMgr.GetInstance().ShowPanel<MainUI>("MainUI", E_UI_Layer.Mid, (panel) =>
            {
                Debug.Log("MainUI 面板已创建并显示");
                
            });
        }
        else
        {
            Debug.LogError("GamingScene 加载失败！");
            // 可以提示用户重试
        }
    }

}