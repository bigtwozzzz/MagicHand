using System;
using System.Collections;
using UnityEngine;

public class ConnectMgr : BaseManager<ConnectMgr>
{
    public ServerConnect serverConnect;

    private IEnumerator Start()
    {
        // 等待 GlobalMonoMgr 初始化
        while (GlobalMonoMgr.GetInstance() == null)
        {
            yield return null;
        }

        Debug.Log("GlobalMonoMgr 已初始化完成");
        GlobalMonoMgr.GetInstance().StartCoroutine(ConnectRoutine());
    }

    private IEnumerator ConnectRoutine()
    {
        Debug.Log("开始连接服务器...");

        yield return new WaitForSeconds(1f);

        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            bool success = false;
            string errorMessage = "";

            try
            {
                serverConnect = ServerConnect.GetInstance();
                success = serverConnect.HandleConnect();
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }

            if (success)
            {
                Debug.Log("服务器连接成功！");
                GlobalMonoMgr.GetInstance().AddUpdateListener(OnUpdate);
                yield break;
            }
            else
            {
                retryCount++;
                if (string.IsNullOrEmpty(errorMessage))
                {
                    Debug.LogWarning($"连接失败，第 {retryCount} 次重试...");
                }
                else
                {
                    Debug.LogError($"连接异常: {errorMessage}");
                }
            }

            if (retryCount < maxRetries)
            {
                yield return new WaitForSeconds(2f);
            }
        }

        Debug.LogError("连接失败次数过多，放弃连接。");
    }

    private void OnUpdate()
    {
        // 防御性编程：防止对象已销毁
        if (this == null) return;

        // 每帧执行的逻辑，如心跳包、状态检测
        // Debug.Log("ConnectMgr 帧更新");
    }

    protected void OnDestroy()
    {
        // 1. 移除帧更新监听（防止内存泄漏）
        GlobalMonoMgr instance = GlobalMonoMgr.GetInstance();
        if (instance != null)
        {
            instance.RemoveUpdateListener(OnUpdate);
        }

        // 2. 断开服务器连接
        if (serverConnect != null)
        {
            try
            {
                serverConnect.Disconnect();
                Debug.Log("[ConnectMgr] 已断开服务器连接。");
            }
            catch (Exception e)
            {
                Debug.LogError($"断开连接时发生异常: {e.Message}");
            }
            finally
            {
                serverConnect = null;
            }
        }

        Debug.Log("[ConnectMgr] 资源已释放。");
    }

    // 应用退出时确保断开连接
    private void OnApplicationQuit()
    {
        OnDestroy();
    }
}