using UnityEngine;

/// <summary>  
/// 键盘按键输入类型  
/// </summary>  
public enum E_KeyCode_Type
{
    Up_D,//上按下  
    Down_D,//下按下  
    Left_D,//左按下  
    Right_D,//右按下  

    Up_U,//上抬起  
    Down_U,//下抬起  
    Left_U,//左抬起  
    Right_U,//右抬起  
}

/// <summary>  
/// 鼠标输入类型  
/// </summary>  
public enum E_Mouse_Type
{
    Left,//左键  
    Left_D,//左键按下  
    Left_U,//左键抬起  

    Right,//右键  
    Right_D,//右键按下  
    Right_U,//右键抬起  

    Mid,//中键  
    Mid_D,//中键按下  
    Mid_U,//中键抬起  
}

/// <summary>
/// 输入管理器 主要作用是 统一管理输入相关 通过事件中心向外发放
/// 好处：
/// 1.如有多处需要检测输入 不需要频繁些Input检测相关代码
/// 2.哪里用哪里监听事件即可
/// 3.可以统一管理输入检测的开启与关闭
/// </summary>
public class InputMgr : BaseManager<InputMgr>
{
    private bool isStart = false;

    /// <summary>
    /// 开启输入检测 
    /// </summary>
    public void Start()
    {
        //由于InputMgr没有继承自mono 所以需要通过公共Mono来进行帧检测
        if (!isStart)
            GlobalMonoMgr.GetInstance().AddUpdateListener(CheckInput);
        isStart = true;
    }

    /// <summary>
    /// 关闭输入检测
    /// </summary>
    public void Stop()
    {
        if (isStart)
            GlobalMonoMgr.GetInstance().RemoveUpdateListener(CheckInput);
        isStart = false;
    }

    /// <summary>
    /// 每帧检测输入
    /// </summary>
    public void CheckInput()
    {
        //W键和键盘上键输入
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow))
            //注意，这里所有的EventTrigger方法都调用的是带参数的泛型方法
            //但是C#编译器会自动帮我们识别，所以我们省略了
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Keycode_Input, E_KeyCode_Type.Up_D);
        if (Input.GetKeyUp(KeyCode.W) ||
            Input.GetKeyUp(KeyCode.UpArrow))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Keycode_Input, E_KeyCode_Type.Up_U);

        //S键和键盘下键输入
        if (Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.DownArrow))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Keycode_Input, E_KeyCode_Type.Down_D);
        if (Input.GetKeyUp(KeyCode.S) ||
           Input.GetKeyUp(KeyCode.DownArrow))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Keycode_Input, E_KeyCode_Type.Down_U);

        //A键和键盘左键输入
        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Keycode_Input, E_KeyCode_Type.Left_D);
        if (Input.GetKeyUp(KeyCode.A) ||
            Input.GetKeyUp(KeyCode.LeftArrow))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Keycode_Input, E_KeyCode_Type.Left_U);

        //D键和键盘右键输入
        if (Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.RightArrow))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Keycode_Input, E_KeyCode_Type.Right_D);
        if (Input.GetKeyUp(KeyCode.D) ||
            Input.GetKeyUp(KeyCode.RightArrow))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Keycode_Input, E_KeyCode_Type.Right_U);

        //鼠标左键输入
        if (Input.GetMouseButton(0))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Left);
        //鼠标左键按下
        if (Input.GetMouseButtonDown(0))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Left_D);
        //鼠标左键抬起
        if (Input.GetMouseButtonUp(0))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Left_U);

        //鼠标右键输入
        if (Input.GetMouseButton(1))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Right);
        //鼠标右键按下
        if (Input.GetMouseButtonDown(1))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Right_D);
        //鼠标右键抬起
        if (Input.GetMouseButtonUp(1))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Right_U);

        //鼠标中键输入
        if (Input.GetMouseButton(2))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Mid);
        //鼠标中键按下
        if (Input.GetMouseButtonDown(2))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Mid_D);
        //鼠标中键抬起
        if (Input.GetMouseButtonUp(2))
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Mouse_Input, E_Mouse_Type.Mid_U);


        //鼠标移动热键检测
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_MouseX_Input, Input.GetAxis("Mouse X"));
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_MouseY_Input, Input.GetAxis("Mouse Y"));

        //键盘移动热键检测
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Horizontal_Input, Input.GetAxis("Horizontal"));
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Vertical_Input, Input.GetAxis("Vertical"));
    }
}