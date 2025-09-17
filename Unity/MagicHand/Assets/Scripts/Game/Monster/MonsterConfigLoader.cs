using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// 怪物配置加载器
/// 负责从MonsterConfig.json中读取怪物配置数据
/// </summary>
public class MonsterConfigLoader : MonoBehaviour
{
    private static MonsterConfigLoader _instance;
    public static MonsterConfigLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MonsterConfigLoader>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MonsterConfigLoader");
                    _instance = go.AddComponent<MonsterConfigLoader>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    [Header("配置文件路径")]
    [SerializeField] private string configFilePath = "JSON/MonsterConfig.json";
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 怪物配置字典，以ID为键
    private Dictionary<int, MonsterConfig> monsterConfigs = new Dictionary<int, MonsterConfig>();
    
    // 配置加载完成事件
    public System.Action OnConfigLoaded;
    
    // 配置加载状态
    public bool IsConfigLoaded { get; private set; } = false;
    
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
        LoadMonsterConfigs();
    }
    
    /// <summary>
    /// 加载怪物配置
    /// </summary>
    public void LoadMonsterConfigs()
    {
        try
        {
            // 确保文件名包含.json扩展名
            string fileName = configFilePath;
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }
            
            string configPath = Path.Combine(Application.streamingAssetsPath, fileName);
            Debug.Log($"[MonsterConfigLoader] configFilePath原始值: {configFilePath}");
            Debug.Log($"[MonsterConfigLoader] 处理后文件名: {fileName}");
            Debug.Log($"[MonsterConfigLoader] 尝试加载配置文件: {configPath}");
            Debug.Log($"[MonsterConfigLoader] StreamingAssets路径: {Application.streamingAssetsPath}");
            
            if (!File.Exists(configPath))
            {
                // 尝试从Resources目录加载
                configPath = Path.Combine(Application.dataPath, "Resources", fileName);
                Debug.Log($"[MonsterConfigLoader] 尝试备用路径: {configPath}");
            }
            
            if (!File.Exists(configPath))
            {
                Debug.LogError($"[MonsterConfigLoader] 配置文件未找到: {configPath}");
                return;
            }
            
            string jsonContent = File.ReadAllText(configPath);
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterConfigLoader] 开始解析配置文件: {configPath}");
            }
            
            // 解析JSON
            MonsterConfigData configData = JsonUtility.FromJson<MonsterConfigData>(jsonContent);
            
            if (configData == null || configData.monsters == null)
            {
                Debug.LogError("[MonsterConfigLoader] JSON解析失败或数据为空");
                return;
            }
            
            // 清空现有配置
            monsterConfigs.Clear();
            
            // 处理每个怪物配置
            int loadedCount = 0;
            foreach (var monster in configData.monsters)
            {
                if (monster != null)
                {
                    if (monsterConfigs.ContainsKey(monster.id))
                    {
                        Debug.LogWarning($"[MonsterConfigLoader] 发现重复的怪物ID: {monster.id}，将覆盖原配置");
                    }
                    
                    monsterConfigs[monster.id] = monster;
                    loadedCount++;
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"[MonsterConfigLoader] 加载怪物配置: ID={monster.id}, Name={monster.name}");
                    }
                }
                else
                {
                    Debug.LogWarning("[MonsterConfigLoader] 发现空的怪物配置数据");
                }
            }
            
            Debug.Log($"[MonsterConfigLoader] 配置加载完成，成功加载 {loadedCount} 个怪物配置");
            
            // 设置加载状态
            IsConfigLoaded = true;
            
            // 触发加载完成事件
            OnConfigLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[MonsterConfigLoader] 加载配置时发生异常: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 根据ID获取怪物配置
    /// </summary>
    /// <param name="monsterId">怪物ID</param>
    /// <returns>怪物配置，如果不存在返回null</returns>
    public MonsterConfig GetMonsterConfig(int monsterId)
    {
        if (monsterConfigs.TryGetValue(monsterId, out MonsterConfig config))
        {
            return config;
        }
        
        Debug.LogWarning($"[MonsterConfigLoader] 未找到ID为 {monsterId} 的怪物配置");
        return null;
    }
    
    /// <summary>
    /// 获取所有怪物配置
    /// </summary>
    /// <returns>怪物配置字典</returns>
    public Dictionary<int, MonsterConfig> GetAllConfigs()
    {
        return new Dictionary<int, MonsterConfig>(monsterConfigs);
    }
    
    /// <summary>
    /// 检查是否存在指定ID的怪物配置
    /// </summary>
    /// <param name="monsterId">怪物ID</param>
    /// <returns>是否存在</returns>
    public bool HasMonsterConfig(int monsterId)
    {
        return monsterConfigs.ContainsKey(monsterId);
    }
    
    /// <summary>
    /// 获取已加载的怪物配置数量
    /// </summary>
    /// <returns>配置数量</returns>
    public int GetConfigCount()
    {
        return monsterConfigs.Count;
    }
}

/// <summary>
/// JSON配置数据结构
/// </summary>
[Serializable]
public class MonsterConfigData
{
    public MonsterConfig[] monsters;
}