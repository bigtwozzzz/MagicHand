// SceneMgr.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// �����л��������������� + ��Դ�ͷ� + ������������
/// </summary>
public class SceneMgr : BaseManager<SceneMgr>
{
    [Header("������������")]
    [SerializeField] private List<string> availableBackgroundScenes = new List<string>
    {
        "Background1",
        "Background2", 
        "Background3",
        "Background4",
        "Background5"
    };
    
    [SerializeField] private string currentBackgroundScene = "Background1";
    private string loadedBackgroundScene = null;
    
    /// <summary>
    /// ����ǰҪʹ�õı�������
    /// </summary>
    public void SetBackgroundScene(string backgroundSceneName)
    {
        if (availableBackgroundScenes.Contains(backgroundSceneName))
        {
            currentBackgroundScene = backgroundSceneName;
            Debug.Log($"[SceneMgr] ���ñ�������Ϊ: {backgroundSceneName}");
        }
        else
        {
            Debug.LogWarning($"[SceneMgr] ������� {backgroundSceneName} ������������У�");
        }
    }
    
    /// <summary>
    /// ��ȡǰ������������
    /// </summary>
    public string GetCurrentBackgroundScene()
    {
        return currentBackgroundScene;
    }
    
    /// <summary>
    /// ��ȡ���ñ������������
    /// </summary>
    public List<string> GetAvailableBackgroundScenes()
    {
        return new List<string>(availableBackgroundScenes);
    }
    /// <summary>
    /// ��ȫ���س�����ͨ�� GlobalMonoMgr Э�̣�
    /// </summary>
    public void SafeLoadScene(string name, UnityAction<bool> onLoaded = null)
    {
        GlobalMonoMgr.GetInstance().SafeStartCoroutine(LoadSceneAsync(name, onLoaded));
    }
    
    /// <summary>
    /// ��ȫ���������������ʱ���ر�������
    /// </summary>
    public void SafeLoadGameSceneWithBackground(string gameSceneName, UnityAction<bool> onLoaded = null)
    {
        GlobalMonoMgr.GetInstance().SafeStartCoroutine(LoadGameSceneWithBackgroundAsync(gameSceneName, onLoaded));
    }
    
    /// <summary>
    /// ���ӽ��س��������ж���ǰ�����
    /// </summary>
    public void SafeLoadSceneAdditive(string sceneName, UnityAction<bool> onLoaded = null)
    {
        GlobalMonoMgr.GetInstance().SafeStartCoroutine(LoadSceneAdditiveAsync(sceneName, onLoaded));
    }

    /// <summary>
    /// �첽���س��� + ��������Դ����
    /// </summary>
    public IEnumerator LoadSceneAsync(string name, UnityAction<bool> onLoaded = null)
    {
        Debug.Log($"��SceneMgr����ʼ���س���: {name}");

        // 1. ��鳡������
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("��SceneMgr����������Ϊ�գ�");
            onLoaded?.Invoke(false);
            yield break;
        }

        // 2. ��֤�Ƿ��� Build Settings ��
        if (!IsSceneInBuildSettings(name))
        {
            Debug.LogError($"��SceneMgr������δ�� Build Settings �У�{name}");
            onLoaded?.Invoke(false);
            yield break;
        }

        // 3. ��ʼ���س���
        AsyncOperation asyncOp = null;
        try
        {
            asyncOp = SceneManager.LoadSceneAsync(name);
        }
        catch (Exception e)
        {
            Debug.LogError($"��SceneMgr�����س����쳣: {e.Message}\n{e.StackTrace}");
            onLoaded?.Invoke(false);
            yield break;
        }

        if (asyncOp == null)
        {
            Debug.LogError($"��SceneMgr��LoadSceneAsync ���� null�����鳡������ Build Settings: {name}");
            onLoaded?.Invoke(false);
            yield break;
        }

        Debug.Log($"��SceneMgr���첽���������ɹ���progress={asyncOp.progress:F2}");

        // 4. ���Ƽ�����ڽ��ȿ��ƣ�
        asyncOp.allowSceneActivation = false;

        float timer = 0f;
        const float MaxWaitTime = 15f; // �����ݵĳ�ʱ
        float waitTime = 0f;

        while (!asyncOp.isDone)
        {
            waitTime += Time.deltaTime;
            if (waitTime > MaxWaitTime)
            {
                Debug.LogWarning($"��SceneMgr�����س�ʱ({MaxWaitTime}s)��ǿ�Ƽ����");
                asyncOp.allowSceneActivation = true;
                break;
            }

            timer += Time.unscaledDeltaTime; // ʹ�� unscaled ��ֹ Time.timeScale Ӱ��

            // ƽ��������ȣ�0.9 ���� asyncOp.progress��0.1 ����ʱ�䣩
            float progress = Mathf.Clamp01(asyncOp.progress * 0.9f + timer * 0.1f);

            // ������־Ƶ�ʣ�ÿ 0.2 ��һ�Σ�
            if (Mathf.FloorToInt(timer * 5) % 1 == 0) // ÿ 0.2s ��һ��
            {
                Debug.Log($"[SceneMgr] ������ | Progress: {asyncOp.progress:F2} | Est: {progress:F2}");
            }

            // ���� UI ���ȸ���
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_LoadScene_Progress, progress);

            // �����ص� 90% ʱ����������
            if (asyncOp.progress >= 0.9f && !asyncOp.allowSceneActivation)
            {
                asyncOp.allowSceneActivation = true;
                Debug.Log("��SceneMgr��allowSceneActivation = true��������������");
            }

            yield return null; // ��֡
        }

        // 5. �����������
        Debug.Log($"��SceneMgr������ '{name}' ������ɣ�");

        // ���ս��� 100%
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_LoadScene_Progress, 1f);

        // �� UI һ��ʱ��ˢ�£�������� 0.1 �����䣩
        yield return new WaitForEndOfFrame();

        // 6. �ӳ���Դ��������ִ֡�У�������
        //yield return GlobalMonoMgr.GetInstance().StartCoroutine(FinalCleanupAsync());

        // 7. �ص��ɹ�
        Debug.Log($"��SceneMgr������ '{name}' ���سɹ�");
        onLoaded?.Invoke(true);
    }
    
    /// <summary>
    /// �첽���������������ʱ���ر�������
    /// </summary>
    public IEnumerator LoadGameSceneWithBackgroundAsync(string gameSceneName, UnityAction<bool> onLoaded = null)
    {
        Debug.Log($"[SceneMgr] ��ʼ���������ͱ�������: {gameSceneName} + {currentBackgroundScene}");
        
        bool backgroundLoaded = false;
        bool gameSceneLoaded = false;
        bool hasError = false;
        
        // �ȼ��ر������������ǰ������
        yield return StartCoroutine(LoadBackgroundSceneReplaceAsync((success) => 
        {
            backgroundLoaded = true;
            if (!success)
            {
                Debug.LogWarning($"[SceneMgr] ������������ʧ�ܣ������������������");
            }
        }));
        
        // Ȼ�����ӽ��������
        yield return StartCoroutine(LoadSceneAdditiveAsync(gameSceneName, (success) => 
        {
            gameSceneLoaded = true;
            if (!success)
            {
                hasError = true;
                Debug.LogError($"[SceneMgr] ������������ʧ��: {gameSceneName}");
            }
        }));
        
        // �ȴ������������������
        while (!backgroundLoaded || !gameSceneLoaded)
        {
            yield return null;
        }
        
        Debug.Log($"[SceneMgr] ������ͱ������������");
        onLoaded?.Invoke(!hasError);
    }
    
    /// <summary>
    /// �첽���ӽ��س���
    /// </summary>
    public IEnumerator LoadSceneAdditiveAsync(string sceneName, UnityAction<bool> onLoaded = null)
    {
        Debug.Log($"[SceneMgr] ��ʼ���ӽ��س���: {sceneName}");
        
        // ��鳡���Ƿ��Ѿ����
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.Log($"[SceneMgr] ���� {sceneName} �Ѿ����أ�����");
            onLoaded?.Invoke(true);
            yield break;
        }
        
        // ��鳡������
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneMgr] ��������Ϊ��");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        // ��֤�Ƿ��� Build Settings ��
        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogError($"[SceneMgr] ������δ�� Build Settings �У�{sceneName}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        // ���ӽ��س���
        AsyncOperation asyncOp = null;
        try
        {
            asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneMgr] ���ӽ��س����쳣: {e.Message}\n{e.StackTrace}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        if (asyncOp == null)
        {
            Debug.LogError($"[SceneMgr] LoadSceneAsync����null: {sceneName}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        // �ȴ����������
        while (!asyncOp.isDone)
        {
            yield return null;
        }
        
        Debug.Log($"[SceneMgr] ���� {sceneName} ���ӽ�������");
        onLoaded?.Invoke(true);
    }
    
    /// <summary>
    /// �첽���ر��������ӵ�ģʽ��
    /// </summary>
    private IEnumerator LoadBackgroundSceneAsync(UnityAction<bool> onLoaded = null)
    {
        // ���ǰ�����Ѿ����أ��ȶ���
        if (!string.IsNullOrEmpty(loadedBackgroundScene) && loadedBackgroundScene != currentBackgroundScene)
        {
            yield return StartCoroutine(UnloadBackgroundSceneAsync());
        }
        
        // ���Ҫ���ص�������Ѿ���ǰ���ص�ֱ�ӷ��سɹ�
        if (loadedBackgroundScene == currentBackgroundScene && SceneManager.GetSceneByName(currentBackgroundScene).isLoaded)
        {
            Debug.Log($"[SceneMgr] ������� {currentBackgroundScene} �Ѿ����");
            onLoaded?.Invoke(true);
            yield break;
        }
        
        Debug.Log($"[SceneMgr] ��ʼ�ӵӼ���ر�������: {currentBackgroundScene}");
        
        // ��֤�����Ƿ��� Build Settings ��
        if (!IsSceneInBuildSettings(currentBackgroundScene))
        {
            Debug.LogError($"[SceneMgr] ������δ�� Build Settings �У�{currentBackgroundScene}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        // �ӵӼ���ر�������
        AsyncOperation asyncOp = null;
        try
        {
            asyncOp = SceneManager.LoadSceneAsync(currentBackgroundScene, LoadSceneMode.Additive);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneMgr] ���ر����쳣: {e.Message}\n{e.StackTrace}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        if (asyncOp == null)
        {
            Debug.LogError($"[SceneMgr] ������LoadSceneAsync����null: {currentBackgroundScene}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        // �ȴ����������
        while (!asyncOp.isDone)
        {
            yield return null;
        }
        
        loadedBackgroundScene = currentBackgroundScene;
        Debug.Log($"[SceneMgr] ������� {currentBackgroundScene} �ӵӼ���������");
        onLoaded?.Invoke(true);
    }
    
    /// <summary>
    /// �첽���ر��������滻ģʽ����ж��ǰ���г�����
    /// </summary>
    private IEnumerator LoadBackgroundSceneReplaceAsync(UnityAction<bool> onLoaded = null)
    {
        Debug.Log($"[SceneMgr] ��ʼ�滻���ر�������: {currentBackgroundScene}");
        
        // ��֤�����Ƿ��� Build Settings ��
        if (!IsSceneInBuildSettings(currentBackgroundScene))
        {
            Debug.LogError($"[SceneMgr] ������δ�� Build Settings �У�{currentBackgroundScene}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        // �滻���ر��������������ж��ǰ���г�����
        AsyncOperation asyncOp = null;
        try
        {
            asyncOp = SceneManager.LoadSceneAsync(currentBackgroundScene, LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneMgr] �滻���ر����쳣: {e.Message}\n{e.StackTrace}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        if (asyncOp == null)
        {
            Debug.LogError($"[SceneMgr] ������LoadSceneAsync����null: {currentBackgroundScene}");
            onLoaded?.Invoke(false);
            yield break;
        }
        
        // �ȴ����������
        while (!asyncOp.isDone)
        {
            yield return null;
        }
        
        loadedBackgroundScene = currentBackgroundScene;
        Debug.Log($"[SceneMgr] ������� {currentBackgroundScene} �滻��������");
        onLoaded?.Invoke(true);
    }
    
    /// <summary>
    /// �첽ж�ر�������
    /// </summary>
    private IEnumerator UnloadBackgroundSceneAsync()
    {
        if (string.IsNullOrEmpty(loadedBackgroundScene))
        {
            yield break;
        }
        
        Debug.Log($"[SceneMgr] ��ʼж�ر�������: {loadedBackgroundScene}");
        
        UnityEngine.SceneManagement.Scene backgroundScene = SceneManager.GetSceneByName(loadedBackgroundScene);
        if (backgroundScene.isLoaded)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(backgroundScene);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone)
                {
                    yield return null;
                }
                Debug.Log($"[SceneMgr] ������� {loadedBackgroundScene} ж������");
            }
        }
        
        loadedBackgroundScene = null;
    }
    
    /// <summary>
    /// �л�������
    /// </summary>
    public void SwitchBackgroundScene(string newBackgroundScene, UnityAction<bool> onSwitched = null)
    {
        if (!availableBackgroundScenes.Contains(newBackgroundScene))
        {
            Debug.LogError($"[SceneMgr] ������� {newBackgroundScene} ������������У�");
            onSwitched?.Invoke(false);
            return;
        }
        
        SetBackgroundScene(newBackgroundScene);
        GlobalMonoMgr.GetInstance().SafeStartCoroutine(SwitchBackgroundSceneAsync(onSwitched));
    }
    
    /// <summary>
    /// �첽�л�������
    /// </summary>
    private IEnumerator SwitchBackgroundSceneAsync(UnityAction<bool> onSwitched = null)
    {
        bool success = false;
        yield return StartCoroutine(LoadBackgroundSceneAsync((result) => success = result));
        onSwitched?.Invoke(success);
    }

    /// <summary>
    /// ��ִ֡����Դ���������⿨�٣�
    /// </summary>
    private IEnumerator FinalCleanupAsync()
    {
        Debug.Log("��SceneMgr����ʼ�ӳ���Դ����...");

        // �ӳ� 0.5 �룬ȷ�� UI ��ȫ��Ⱦ
        yield return new WaitForSecondsRealtime(0.5f);

        //  ��һ����ж��δʹ����Դ����ʱ��������ֻ��ͬ����
        Debug.Log("��SceneMgr����ʼ Resources.UnloadUnusedAssets...");
        yield return new WaitForEndOfFrame(); // �� UI ����Ⱦһ֡

       // var unloadOp = Resources.UnloadUnusedAssets();
        //while (!unloadOp.isDone)
        //{
        //    yield return null; // ��֡�ȴ�ж�����
        //}

        Debug.Log("��SceneMgr��UnloadUnusedAssets ���");

        //  �ڶ�����GC ���գ���ѡ����֡��ʾ��
        Debug.Log("��SceneMgr��׼��ִ�� GC.Collect...");
        yield return new WaitForSecondsRealtime(0.2f); // С�ӳ�

        // �ڵ����ȼ�ʱִ�� GC�����⿨ UI��
       // System.GC.Collect();

        Debug.Log("��SceneMgr��GC.Collect ��ɣ���Դ����������");
    }

    /// <summary>
    /// ��鳡���Ƿ��� Build Settings ��
    /// </summary>
    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// ����ѡ������ǰ�ͷž���Դ�������ء�UI ����ȣ�
    /// </summary>
    private IEnumerator UnloadOldResources()
    {
        Debug.Log("��SceneMgr���ͷž���Դ����ѡ��...");

        // ʾ����֪ͨ UI �������ͷ�
        // UIMgr.Instance.ReleaseAllUIResources();

        yield return null;
    }
}