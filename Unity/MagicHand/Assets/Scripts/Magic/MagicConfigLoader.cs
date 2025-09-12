using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 魔法配置加载器
/// 从JSON文件加载魔法配置数据
/// </summary>
public static class MagicConfigLoader
{
    /// <summary>
    /// 从JSON文件加载魔法配置
    /// </summary>
    /// <param name="configPath">配置文件路径（相对于Assets目录）</param>
    /// <returns>魔法数据列表</returns>
    public static List<MagicData> LoadMagicConfig(string configPath = "Scripts/Magic/MagicConfig.json")
    {
        List<MagicData> magicDataList = new List<MagicData>();
        
        try
        {
            // 构建完整路径
            string fullPath = Path.Combine(Application.dataPath, configPath);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[MagicConfigLoader] 配置文件不存在: {fullPath}");
                return magicDataList;
            }
            
            // 读取JSON文件
            string jsonContent = File.ReadAllText(fullPath);
            
            // 解析JSON
            MagicConfigData configData = JsonUtility.FromJson<MagicConfigData>(jsonContent);
            
            if (configData?.magics == null)
            {
                Debug.LogWarning("[MagicConfigLoader] JSON格式错误或magics数组为空");
                return magicDataList;
            }
            
            // 转换为MagicData对象
            foreach (var magicJson in configData.magics)
            {
                MagicData magicData = ConvertToMagicData(magicJson);
                if (magicData != null)
                {
                    magicDataList.Add(magicData);
                }
            }
            
            Debug.Log($"[MagicConfigLoader] 成功加载 {magicDataList.Count} 个魔法配置");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MagicConfigLoader] 加载配置文件失败: {e.Message}");
        }
        
        return magicDataList;
    }
    
    /// <summary>
    /// 将JSON魔法数据转换为MagicData对象
    /// </summary>
    /// <param name="magicJson">JSON魔法数据</param>
    /// <returns>MagicData对象</returns>
    private static MagicData ConvertToMagicData(MagicJsonData magicJson)
    {
        try
        {
            MagicData magicData = new MagicData()
            {
                magicId = magicJson.magicId,
                magicName = magicJson.magicName,
                description = magicJson.description,
                damage = magicJson.damage,
                damageType = ParseDamageType(magicJson.damageType),
                range = new MagicRange(
                    magicJson.range.left,
                    magicJson.range.right,
                    magicJson.range.forward,
                    magicJson.range.backward
                ),
                cooldownTime = magicJson.cooldownTime,
                castTime = magicJson.castTime,
                isEnabled = magicJson.isEnabled,
                level = magicJson.level,
                amplificationFactor = magicJson.amplificationFactor
            };
            
            return magicData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[MagicConfigLoader] 转换魔法数据失败 (ID: {magicJson.magicId}): {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 解析伤害类型字符串
    /// </summary>
    /// <param name="damageTypeStr">伤害类型字符串</param>
    /// <returns>伤害类型枚举</returns>
    private static DamageType ParseDamageType(string damageTypeStr)
    {
        if (Enum.TryParse<DamageType>(damageTypeStr, true, out DamageType result))
        {
            return result;
        }
        
        Debug.LogWarning($"[MagicConfigLoader] 未知的伤害类型: {damageTypeStr}，使用默认值 Magic");
        return DamageType.Magic;
    }
    
    /// <summary>
    /// 保存魔法配置到JSON文件
    /// </summary>
    /// <param name="magicDataList">魔法数据列表</param>
    /// <param name="configPath">配置文件路径</param>
    public static void SaveMagicConfig(List<MagicData> magicDataList, string configPath = "Scripts/Magic/MagicConfig.json")
    {
        try
        {
            MagicConfigData configData = new MagicConfigData();
            configData.magics = new List<MagicJsonData>();
            
            foreach (var magicData in magicDataList)
            {
                MagicJsonData magicJson = new MagicJsonData()
                {
                    magicId = magicData.magicId,
                    magicName = magicData.magicName,
                    description = magicData.description,
                    damage = magicData.damage,
                    damageType = magicData.damageType.ToString(),
                    range = new MagicRangeJson()
                    {
                        left = magicData.range.left,
                        right = magicData.range.right,
                        forward = magicData.range.forward,
                        backward = magicData.range.backward
                    },
                    cooldownTime = magicData.cooldownTime,
                    castTime = magicData.castTime,
                    isEnabled = magicData.isEnabled,
                    level = magicData.level,
                    amplificationFactor = magicData.amplificationFactor
                };
                
                configData.magics.Add(magicJson);
            }
            
            string jsonContent = JsonUtility.ToJson(configData, true);
            string fullPath = Path.Combine(Application.dataPath, configPath);
            
            File.WriteAllText(fullPath, jsonContent);
            Debug.Log($"[MagicConfigLoader] 配置已保存到: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MagicConfigLoader] 保存配置文件失败: {e.Message}");
        }
    }
}

/// <summary>
/// JSON配置数据结构
/// </summary>
[Serializable]
public class MagicConfigData
{
    public List<MagicJsonData> magics;
}

/// <summary>
/// JSON魔法数据结构
/// </summary>
[Serializable]
public class MagicJsonData
{
    public int magicId;
    public string magicName;
    public string description;
    public float damage;
    public string damageType;
    public MagicRangeJson range;
    public float cooldownTime;
    public float castTime;
    public bool isEnabled;
    public int level;
    public float amplificationFactor;
}

/// <summary>
/// JSON范围数据结构
/// </summary>
[Serializable]
public class MagicRangeJson
{
    public float left;
    public float right;
    public float forward;
    public float backward;
}