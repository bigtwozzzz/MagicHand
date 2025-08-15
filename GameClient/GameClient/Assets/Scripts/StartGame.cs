using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        string startSceneName = "LoginScene";
        SceneMgr.GetInstance().LoadScene(startSceneName, OnLoadOver);
        UIMgr.GetInstance().ShowPanel<LoginUI>("LoginUI", E_UI_Layer.Mid, (panel) =>
        {
            // 可选：面板创建完成后做一些初始化
            Debug.Log("LoginUI 面板已创建并显示");
        });
    }

    public static void OnLoadOver()
    {
        Debug.Log("游戏启动");
    }
}