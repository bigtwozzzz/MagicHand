using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ConnectMgr : BaseManager<ConnectMgr>
{
    public ServerConnect serverConnect;

    void Start()
    {

        // 等待 GlobalMonoMgr 初始化完成
        if (GlobalMonoMgr.GetInstance() != null)
        { 
            Debug.Log("GlobalMonoMgr 已初始化完成");
            GlobalMonoMgr.GetInstance().StartCoroutine(ConnectRoutine());
        }
    }

    IEnumerator ConnectRoutine()
    {
        Debug.Log("开始连接服务器...");

        // 示例：延迟 1 秒再连接
        yield return new WaitForSeconds(1f);

        serverConnect = ServerConnect.GetInstance();
        serverConnect.HandleConnect();

        GlobalMonoMgr.GetInstance().AddUpdateListener(OnUpdate);
    }

    void OnUpdate()
    {
        // 每帧执行的逻辑，比如心跳包、状态检测
        // Debug.Log("ConnectMgr 帧更新");
    }

    void OnDestroy()
    {
        // 清理监听，防止内存泄漏
        GlobalMonoMgr.GetInstance()?.RemoveUpdateListener(OnUpdate);
    }
}