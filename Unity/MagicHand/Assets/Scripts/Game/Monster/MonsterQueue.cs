using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// 怪物生成队列加载器
/// 负责从MonsterSpawnQueue.json中读取波次数据
/// </summary>
public class MonsterQueue : MonoBehaviour
{
    private static MonsterQueue _instance;
    public static MonsterQueue Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MonsterQueue>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MonsterQueue");
                    _instance = go.AddComponent<MonsterQueue>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    [Header("配置文件路径")]
    [SerializeField] private string configFilePath = "JSON/MonsterSpawnQueue.json";
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 波次队列数据
    private Dictionary<int, WaveData> waves = new Dictionary<int, WaveData>();
    private List<int> waveOrder = new List<int>();
    
    // 队列加载完成事件
    public System.Action OnQueueLoaded;
    
    // 队列加载状态
    public bool IsQueueLoaded { get; private set; } = false;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        LoadSpawnQueue();
    }
    
    /// <summary>
    /// 加载生成队列
    /// </summary>
    public void LoadSpawnQueue()
    {
        try
        {
            // 确保文件名包含.json扩展名
            string fileName = configFilePath;
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }
            
            string queuePath = Path.Combine(Application.streamingAssetsPath, fileName);
            Debug.Log($"[MonsterQueue] configFilePath原始值: {configFilePath}");
            Debug.Log($"[MonsterQueue] 处理后文件名: {fileName}");
            Debug.Log($"[MonsterQueue] 尝试加载队列文件: {queuePath}");
            Debug.Log($"[MonsterQueue] StreamingAssets路径: {Application.streamingAssetsPath}");
            
            if (!File.Exists(queuePath))
            {
                // 尝试从Resources目录加载
                queuePath = Path.Combine(Application.dataPath, "Resources", fileName);
                Debug.Log($"[MonsterQueue] 尝试备用路径: {queuePath}");
            }
            
            if (!File.Exists(queuePath))
            {
                Debug.LogError($"[MonsterQueue] 队列文件未找到: {queuePath}");
                return;
            }
            
            string jsonContent = File.ReadAllText(queuePath);
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterQueue] 开始解析队列文件: {queuePath}");
            }
            
            // 解析JSON
            SpawnQueueData queueData = JsonUtility.FromJson<SpawnQueueData>(jsonContent);
            
            if (queueData == null || queueData.spawnQueue == null)
            {
                Debug.LogError("[MonsterQueue] JSON解析失败或数据为空");
                return;
            }
            
            // 清空现有数据
            waves.Clear();
            waveOrder.Clear();
            
            // 处理波次数据
            int waveCount = 0;
            if (queueData.spawnQueue.waves != null)
            {
                // 解析波次数据
                var waveFields = typeof(WavesData).GetFields();
                foreach (var field in waveFields)
                {
                    if (field.FieldType == typeof(WaveData))
                    {
                        WaveData waveData = (WaveData)field.GetValue(queueData.spawnQueue.waves);
                        if (waveData != null && waveData.spawnEvents != null && waveData.spawnEvents.Length > 0)
                        {
                            // 提取波次编号
                            string waveName = field.Name; // wave1, wave2, etc.
                            if (waveName.StartsWith("wave") && int.TryParse(waveName.Substring(4), out int waveNumber))
                            {
                                waves[waveNumber] = waveData;
                                waveOrder.Add(waveNumber);
                                waveCount++;
                                
                                if (enableDebugLog)
                                {
                                    Debug.Log($"[MonsterQueue] 加载波次 {waveNumber}: {waveData.description}, 事件数量: {waveData.spawnEvents.Length}");
                                }
                            }
                        }
                    }
                }
            }
            
            // 排序波次
            waveOrder.Sort();
            
            Debug.Log($"[MonsterQueue] 队列加载完成，成功加载 {waveCount} 个波次");
            
            // 设置加载状态
            IsQueueLoaded = true;
            
            // 触发加载完成事件
            OnQueueLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[MonsterQueue] 加载队列时发生异常: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 获取指定波次的数据
    /// </summary>
    /// <param name="waveNumber">波次编号</param>
    /// <returns>波次数据</returns>
    public WaveData GetWaveData(int waveNumber)
    {
        if (waves.TryGetValue(waveNumber, out WaveData waveData))
        {
            return waveData;
        }
        
        Debug.LogWarning($"[MonsterQueue] 未找到波次 {waveNumber} 的数据");
        return null;
    }
    
    /// <summary>
    /// 获取总波次数量
    /// </summary>
    /// <returns>波次数量</returns>
    public int GetWaveCount()
    {
        return waves.Count;
    }
    
    /// <summary>
    /// 获取所有波次编号（按顺序）
    /// </summary>
    /// <returns>波次编号列表</returns>
    public List<int> GetWaveOrder()
    {
        return new List<int>(waveOrder);
    }
    
    /// <summary>
    /// 检查是否存在指定波次
    /// </summary>
    /// <param name="waveNumber">波次编号</param>
    /// <returns>是否存在</returns>
    public bool HasWave(int waveNumber)
    {
        return waves.ContainsKey(waveNumber);
    }
}

/// <summary>
/// JSON数据结构
/// </summary>
[Serializable]
public class SpawnQueueData
{
    public SpawnQueueInfo spawnQueue;
}

[Serializable]
public class SpawnQueueInfo
{
    public string description;
    public string version;
    public WavesData waves;
}

[Serializable]
public class WavesData
{
    public WaveData wave1;
    public WaveData wave2;
    // 可以根据需要添加更多波次
}

[Serializable]
public class WaveData
{
    public string description;
    public SpawnEvent[] spawnEvents;
}

[Serializable]
public class SpawnEvent
{
    public int eventId;
    public float triggerTime;
    public int id;
    public int spawnCount;
    public Vector3 spawnPosition;
    public string description;
    public bool enabled;
}