using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON配置文件根结构
/// </summary>
[Serializable]
public class MagicConfigRoot
{
    public List<MagicData> magics = new List<MagicData>();
}

/// <summary>
/// 魔法配置加载器
/// 负责从JSON文件加载所有魔法配置，生成供其他脚本使用的数据结构
/// </summary>
public class MagicConfigLoader : MonoBehaviour
{
    [Header("配置文件路径")]
    [SerializeField] private string configFileName = "MagicConfig.json";
    
    // 单例实例
    public static MagicConfigLoader Instance { get; private set; }
    
    // 魔法数据字典，以魔法ID为键
    private Dictionary<int, MagicData> magicDataDict = new Dictionary<int, MagicData>();
    
    // 魔法数据列表
    private List<MagicData> magicDataList = new List<MagicData>();
    
    // 配置是否已加载
    public bool IsConfigLoaded { get; private set; } = false;
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadMagicConfig();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 加载魔法配置
    /// </summary>
    public void LoadMagicConfig()
    {
        try
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "JSON", configFileName);
            
            if (!File.Exists(configPath))
            {
                Debug.LogError($"[MagicConfigLoader] 配置文件不存在: {configPath}");
                LoadDefaultConfig();
                return;
            }
            
            string jsonContent = File.ReadAllText(configPath);
            
            if (string.IsNullOrEmpty(jsonContent))
            {
                Debug.LogError($"[MagicConfigLoader] 配置文件为空: {configPath}");
                LoadDefaultConfig();
                return;
            }
            
            MagicConfigRoot configRoot = JsonUtility.FromJson<MagicConfigRoot>(jsonContent);
            
            if (configRoot == null || configRoot.magics == null)
            {
                Debug.LogError("[MagicConfigLoader] JSON解析失败或魔法列表为空");
                LoadDefaultConfig();
                return;
            }
            
            // 清空现有数据
            magicDataDict.Clear();
            magicDataList.Clear();
            
            // 加载魔法数据
            foreach (var magic in configRoot.magics)
            {
                if (magic != null)
                {
                    // 确保特效配置不为空
                    if (magic.effectConfig == null)
                    {
                        magic.effectConfig = new MagicEffectConfig();
                    }
                    
                    magicDataDict[magic.magicId] = magic;
                    magicDataList.Add(magic);
                    
                    Debug.Log($"[MagicConfigLoader] 加载魔法: {magic.magicName} (ID: {magic.magicId})");
                }
            }
            
            IsConfigLoaded = true;
            Debug.Log($"[MagicConfigLoader] 成功加载 {magicDataList.Count} 个魔法配置");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MagicConfigLoader] 加载配置时发生错误: {e.Message}");
            LoadDefaultConfig();
        }
    }
    
    /// <summary>
    /// 加载默认配置（备用方案）
    /// </summary>
    private void LoadDefaultConfig()
    {
        Debug.LogWarning("[MagicConfigLoader] 使用默认魔法配置");
        
        magicDataDict.Clear();
        magicDataList.Clear();
        
        // 创建默认魔法数据
        var defaultMagics = new List<MagicData>
        {
            new MagicData(23, "治疗魔法", "恢复自身一定生命值", 80f, "Magic", 
                         new MagicRange(-1f, 1f, 1f, -1f), 10f, 0.5f),
            new MagicData(24, "光束魔法", "释放一道向前的光束，对中路敌人造成伤害", 120f, "Magic", 
                         new MagicRange(-1f, 1f, 12f, 0f), 6f, 0.1f),
            new MagicData(32, "流星", "先握拳后张掌的复合手势魔法，释放从天而降的火球，对大范围敌人造成伤害", 100f, "Magic", 
                         new MagicRange(-5f, 5f, 10f, 0f), 15f, 0.1f)
        };
        
        // 设置魔法32（流星）的放大系数
        defaultMagics[2].amplificationFactor = 3f;
        
        foreach (var magic in defaultMagics)
        {
            magicDataDict[magic.magicId] = magic;
            magicDataList.Add(magic);
        }
        
        IsConfigLoaded = true;
        Debug.Log($"[MagicConfigLoader] 加载了 {magicDataList.Count} 个默认魔法配置");
    }
    
    /// <summary>
    /// 根据魔法ID获取魔法数据
    /// </summary>
    public MagicData GetMagicData(int magicId)
    {
        return magicDataDict.TryGetValue(magicId, out MagicData data) ? data : null;
    }
    
    /// <summary>
    /// 获取所有魔法数据
    /// </summary>
    public List<MagicData> GetAllMagicData()
    {
        return new List<MagicData>(magicDataList);
    }
    
    /// <summary>
    /// 获取所有启用的魔法数据
    /// </summary>
    public List<MagicData> GetEnabledMagicData()
    {
        return magicDataList.FindAll(magic => magic.isEnabled);
    }
    
    /// <summary>
    /// 检查魔法是否存在
    /// </summary>
    public bool HasMagic(int magicId)
    {
        return magicDataDict.ContainsKey(magicId);
    }
    
    /// <summary>
    /// 重新加载配置
    /// </summary>
    public void ReloadConfig()
    {
        IsConfigLoaded = false;
        LoadMagicConfig();
    }
    
    /// <summary>
    /// 获取魔法数量
    /// </summary>
    public int GetMagicCount()
    {
        return magicDataList.Count;
    }
}