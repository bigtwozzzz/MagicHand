using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 音效与背景音乐管理器
/// 单例模式，支持音量持久化
/// </summary>
public class MusicMgr : BaseManager<MusicMgr>
{
    // 背景音乐
    private AudioSource bkMusic = null;
    private float bkValue = 1f; // 0~1
    public float BkValue => bkValue;

    // 音效
    private GameObject soundObj = null;
    private List<AudioSource> soundList = new();
    private float soundValue = 1f; // 0~1
    public float SoundValue => soundValue;

    // 音效资源路径（可配置）
    private const string BGM_PATH = "Music/BK/";
    private const string SOUND_PATH = "Music/Sound/";

    // 默认测试音效名称（放在 Resources/Music/Sound/ 目录下）
    private const string TEST_SOUND_NAME = "TestSound"; // 例如：一个短提示音

    // 音量持久化键
    private const string BGM_VOLUME_KEY = "Music_BGM_Volume";
    private const string SOUND_VOLUME_KEY = "Music_Sound_Volume";

    public MusicMgr()
    {
        //  安全：GlobalMonoMgr.GetInstance().AddUpdateListener 是纯 C# 逻辑，可在构造函数中调用
        GlobalMonoMgr.GetInstance().AddUpdateListener(Update);
    }

    protected override void Awake()
    {
        base.Awake(); // 如果 BaseManager 有 Awake，需调用

        //  正确时机：Awake 中可以安全调用 PlayerPrefs
        LoadVolumeSettings();
    }

    private void Update()
    {
        // 清理已播放完毕的音效
        for (int i = soundList.Count - 1; i >= 0; i--)
        {
            if (!soundList[i].isPlaying)
            {
                Object.Destroy(soundList[i]);
                soundList.RemoveAt(i);
            }
        }
    }

    #region 背景音乐控制

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBkMusic(string name)
    {
        if (bkMusic == null)
        {
            GameObject obj = new GameObject { name = "BkMusic" };
            obj.transform.SetParent(GlobalMonoMgr.GetInstance().transform);
            bkMusic = obj.AddComponent<AudioSource>();
        }

        ResMgr.GetInstance().LoadAsync<AudioClip>(BGM_PATH + name, (clip) =>
        {
            if (clip == null)
            {
                Debug.LogError($"背景音乐加载失败: {BGM_PATH}{name}");
                return;
            }
            bkMusic.clip = clip;
            bkMusic.loop = true;
            bkMusic.volume = bkValue;
            bkMusic.Play();
        });
    }

    public void PauseBKMusic() => bkMusic?.Pause();
    public void ResumeBKMusic() => bkMusic?.UnPause();
    public void StopBKMusic() => bkMusic?.Stop();

    /// <summary>
    /// 调整背景音乐音量 (0~1)
    /// </summary>
    public void ChangeBKValue(float value)
    {
        bkValue = Mathf.Clamp01(value);
        if (bkMusic != null)
            bkMusic.volume = bkValue;
        SaveVolumeSettings();
    }

    #endregion

    #region 音效控制

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySound(string name, bool isLoop = false, UnityAction<AudioSource> callBack = null)
    {
        if (soundObj == null)
        {
            soundObj = new GameObject { name = "Sound" };
            soundObj.transform.SetParent(GlobalMonoMgr.GetInstance().transform);
        }

        ResMgr.GetInstance().LoadAsync<AudioClip>(SOUND_PATH + name, (clip) =>
        {
            if (clip == null)
            {
                Debug.LogError($"音效加载失败: {SOUND_PATH}{name}");
                return;
            }

            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = isLoop;
            source.volume = soundValue;
            source.Play();

            soundList.Add(source);
            callBack?.Invoke(source);
        });
    }

    /// <summary>
    /// 调整音效音量 (0~1)
    /// </summary>
    public void ChangeSoundValue(float value)
    {
        soundValue = Mathf.Clamp01(value);
        foreach (var source in soundList)
            source.volume = soundValue;
        SaveVolumeSettings();
    }

    /// <summary>
    /// 停止指定音效
    /// </summary>
    public void StopSound(AudioSource source)
    {
        if (soundList.Contains(source))
        {
            soundList.Remove(source);
            source.Stop();
            Object.Destroy(source);
        }
    }

    /// <summary>
    /// 停止所有音效
    /// </summary>
    public void StopAllSounds()
    {
        foreach (var source in soundList)
        {
            if (source != null)
                source.Stop();
            Object.Destroy(source);
        }
        soundList.Clear();
    }

    /// <summary>
    /// 播放测试音效（用于设置面板）
    /// </summary>
    public void PlayTestSound()
    {
        PlaySound(TEST_SOUND_NAME, false);
    }

    #endregion

    #region 音量持久化

    /// <summary>
    /// 保存音量设置
    /// </summary>
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bkValue);
        PlayerPrefs.SetFloat(SOUND_VOLUME_KEY, soundValue);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 加载音量设置
    /// </summary>
    private void LoadVolumeSettings()
    {
        bkValue = PlayerPrefs.HasKey(BGM_VOLUME_KEY) ? PlayerPrefs.GetFloat(BGM_VOLUME_KEY) : 1f;
        soundValue = PlayerPrefs.HasKey(SOUND_VOLUME_KEY) ? PlayerPrefs.GetFloat(SOUND_VOLUME_KEY) : 1f;
    }

    #endregion
}