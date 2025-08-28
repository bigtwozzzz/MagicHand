using Base;        // CharacterBase 来自这里
using Broadcast;
using Character;
using Common;
using Enemy;
using Globalrandom;
using Scene;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 全局数据管理器，用于存储玩家数据、游戏状态等
/// </summary>
public class DataMgr : BaseManager<DataMgr>
{
    // 全局随机数种子
    private int _globalRandomSeed = 0;
    public int GlobalRandomSeed => _globalRandomSeed;

    private bool _isRandomSeedReceived = false;
    public bool IsRandomSeedReady => _isRandomSeedReceived;

    // 当前登录的用户信息
    private string _userId = "";
    private string _loginStatus = "OffLine";

    // 存储所有玩家的角色信息
    private Dictionary<string, CharacterBase> _allCharacterInfos = new Dictionary<string, CharacterBase>();
    // 新增：记录 player_id 到 role_id 的映射
    private Dictionary<string, string> _playerToRoleMap = new Dictionary<string, string>();
    private Dictionary<string, string> _playerNameDict = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> PlayerNameDict => _playerNameDict;
    // 只读属性，供外部访问
    public IReadOnlyDictionary<string, string> PlayerToRoleMap => _playerToRoleMap;
    // 记录每个玩家是否已经生成了角色（防止重复生成）
    public HashSet<string> _spawnedPlayers = new HashSet<string>();

    // 角色详细信息（来自服务器广播）
    private CharacterBase _characterInfo;
    private bool isMainUIReady = false;


    // 只读属性，供外部访问
    public string UserId => _userId;
    public string LoginStatus => _loginStatus;
    public bool IsLoggedIn => !string.IsNullOrEmpty(_userId);

    // 提供角色信息的只读访问
    public CharacterBase CharacterInfo => _characterInfo;

    private SceneData _SceneData;
    public SceneData SceneData => _SceneData;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        EventCenter.GetInstance().AddEventListener<LoginResponse>(
            E_EventType.Event_Login_Success,
            OnLoginSuccess);

        EventCenter.GetInstance().AddEventListener<CharacterBase>(
            E_EventType.Event_Character_Info_Update,
            OnCharacterInfoUpdate);

        EventCenter.GetInstance().AddEventListener<GlobalRandomNum>(
            E_EventType.Event_Global_Random_Seed,
            OnGlobalRandomSeedReceived);
        EventCenter.GetInstance().AddEventListener<PlayerOnlineNotify>(
            E_EventType.Event_Player_Online,
            OnPlayerOnline);
        EventCenter.GetInstance().AddEventListener<SceneData>(
            E_EventType.Event_Scene_Data_Update,
            OnSceneDataUpdate);
    }
    public void SetMainUIReady()
    {
        isMainUIReady = true;
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Scene_Data_Update_UI, _SceneData);
    }
    public void SetPlayerName(string playerId, string playerName)
    {
        if (string.IsNullOrEmpty(playerId)) return;

        _playerNameDict[playerId] = playerName ?? string.Empty;
        Debug.Log($"[DataMgr] 已映射 playerId: {playerId} -> playerName: {playerName}");
    }
    // 获取玩家名字
    public string GetPlayerName(string playerId)
    {
        return _playerNameDict.TryGetValue(playerId, out string name) ? name : null;
    }

    private void OnSceneDataUpdate(Scene.SceneData sceneData)
    {
        if (isMainUIReady)
        {
            EventCenter.GetInstance().EventTrigger(E_EventType.Event_Scene_Data_Update_UI, _SceneData);
        }
        else
        {
            // 缓存数据，等待 MainUI 准备好
            _SceneData = sceneData;
        }
    }
    /// <summary>
    /// 处理玩家上线通知
    /// </summary>
    private void OnPlayerOnline(PlayerOnlineNotify onlineNotify)
    {
        if (onlineNotify == null)
        {
            Debug.LogError("[DataMgr] 收到空的玩家上线通知！");
            return;
        }

        string playerId = onlineNotify.PlayerId;
        string roleId = onlineNotify.RoleId;

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[DataMgr] 上线通知中 PlayerId 为空！");
            return;
        }

        if (string.IsNullOrEmpty(roleId))
        {
            Debug.LogError("[DataMgr] 上线通知中 RoleId 为空！");
            return;
        }

        // 更新映射
        _playerToRoleMap[playerId] = roleId;

        Debug.Log($"[DataMgr] 玩家上线映射已记录 - PlayerId: {playerId} → RoleId: {roleId}");
    }
    public List<MonsterBase> GetMonsterList()
    {
        if (_SceneData == null)
            return new List<MonsterBase>();

        return _SceneData.Monsters.ToList();
    }

    protected void OnDestroy()
    {
        var eventCenter = EventCenter.GetInstance();
        if (eventCenter != null)
        {
            eventCenter.RemoveEventListener<LoginResponse>(E_EventType.Event_Login_Success, OnLoginSuccess);
            eventCenter.RemoveEventListener<CharacterBase>(E_EventType.Event_Character_Info_Update, OnCharacterInfoUpdate);
            eventCenter.RemoveEventListener<GlobalRandomNum>(E_EventType.Event_Global_Random_Seed, OnGlobalRandomSeedReceived);
            eventCenter.RemoveEventListener<PlayerOnlineNotify>(E_EventType.Event_Player_Online, OnPlayerOnline);
            eventCenter.RemoveEventListener<SceneData>(E_EventType.Event_Scene_Data_Update, OnSceneDataUpdate);
        }

        ClearUserData();
        Debug.Log("[DataMgr] 资源已释放。");
    }

    /// <summary>
    /// 收到全局随机种子
    /// </summary>
    public void OnGlobalRandomSeedReceived(GlobalRandomNum seed)
    {
        if (seed == null)
        {
            Debug.LogError("[DataMgr] GlobalRandomNum 为 null！");
            return;
        }

        this._globalRandomSeed = seed.Seed;
        this._isRandomSeedReceived = true; //  修复：使用正确的字段

        Debug.Log($" [DataMgr] 收到全局随机种子: {seed.Seed}，尝试生成所有角色...");

        // 尝试生成所有已知角色（包括自己和他人）
        foreach (var character in _allCharacterInfos.Values)
        {
            TrySpawnCharacterForPlayer(character);
        }
    }

    /// <summary>
    /// 根据玩家 ID 获取角色 ID
    /// </summary>
    public string GetRoleIdByPlayerId(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return null;

        _playerToRoleMap.TryGetValue(playerId, out string roleId);
        return roleId;
    }
    /// <summary>
    /// 尝试生成角色（供外部调用）
    /// </summary>
    public void TrySpawnCharacter()
    {
        Debug.Log("[DataMgr] 外部请求尝试生成角色（无参）...");
        // 可选：触发无参事件，但建议使用带 roleId 的方式
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Character_Spawn_Ready);
    }

    /// <summary>
    /// 处理登录成功事件
    /// </summary>
    private void OnLoginSuccess(LoginResponse loginResponse)
    {
        if (loginResponse == null)
        {
            Debug.LogError("[DataMgr] LoginResponse is null!");
            return;
        }

        _userId = loginResponse.UserId ?? "";
        _loginStatus = loginResponse.Status ?? "Unknown";

        Debug.Log($"[DataMgr] 登录成功！用户ID: {_userId}, 状态: {_loginStatus}");
    }

    /// <summary>
    /// 处理角色信息更新
    /// </summary>
    private void OnCharacterInfoUpdate(CharacterBase characterBase)
    {
        if (characterBase == null)
        {
            Debug.LogError("[DataMgr] 收到空的角色信息！");
            return;
        }

        // 更新或添加角色信息
        _allCharacterInfos[characterBase.RoleId] = characterBase;

        Debug.Log($"[DataMgr] 收到角色信息更新：{characterBase.RoleName} (ID: {characterBase.RoleId})");
        _playerNameDict[characterBase.PlayerId] = characterBase.PlayerName;
        // 打印角色信息
        PrintCharacterInfo(characterBase);

        // 尝试生成该角色
        TrySpawnCharacterForPlayer(characterBase);
    }

    /// <summary>
    /// 尝试为指定玩家生成角色
    /// </summary>
    public void TrySpawnCharacterForPlayer(CharacterBase characterBase)
    {
        if (characterBase == null)
        {
            Debug.LogError("[DataMgr] 尝试生成空角色！");
            return;
        }

        string roleId = characterBase.RoleId;
        if (string.IsNullOrEmpty(roleId))
        {
            Debug.LogError("[DataMgr] 角色 RoleId 为空，无法生成！");
            return;
        }

        // 检查是否已生成
        if (_spawnedPlayers.Contains(roleId))
        {
            Debug.Log($"[DataMgr] 角色 {characterBase.RoleName} 已生成，跳过。");
            return;
        }

        // 检查随机种子是否准备好
        if (!IsRandomSeedReady)
        {
            Debug.Log($"[DataMgr] 随机种子未就绪，延迟生成角色 {characterBase.RoleName}...");
            return;
        }

        // 触发生成事件，传入 roleId
        Debug.Log($"[DataMgr] 准备生成角色: {characterBase.RoleName} (ID: {roleId})");
        EventCenter.GetInstance().EventTrigger(E_EventType.Event_Character_Spawn_Ready, roleId);
    }

    /// <summary>
    /// 打印角色信息
    /// </summary>
    private void PrintCharacterInfo(CharacterBase characterBase)
    {
        if (characterBase == null) return;

        Debug.Log($"========== 角色信息 ==========");
        Debug.Log($"角色ID:       {characterBase.RoleId}");
        Debug.Log($"角色名称:     {characterBase.RoleName}");
        Debug.Log($"当前血量:     {characterBase.CurrentHp}/{characterBase.MaxHp}");
        Debug.Log($"等级:         {characterBase.Level}");
        Debug.Log($"经验值:       {characterBase.Exp}");
        Debug.Log($"位置:         ({characterBase.PosX:F2}, {characterBase.PosY:F2})");
        Debug.Log($"朝向:         {characterBase.Direction}");
        Debug.Log($"状态:         {characterBase.Status}");
        Debug.Log($"用户名:       { characterBase.PlayerName} ");
        Debug.Log($"技能列表:");
        if (characterBase.Skills != null)
        {
            foreach (var skill in characterBase.Skills)
            {
                Debug.Log($"  技能ID: {skill.SkillId}, 冷却: {skill.CurrentCooldown}");
            }
        }
        else
        {
            Debug.Log("  技能列表: null");
        }
        Debug.Log($"==============================");
    }
    public void RemoveFromSpawnedPlayers(string roleId)
    {
        if (string.IsNullOrEmpty(roleId)) return;

        bool removed = _spawnedPlayers.Remove(roleId);
        if (removed)
        {
            Debug.Log($"[DataMgr] 已从已生成列表移除角色: {roleId}");
        }
    }
    /// <summary>
    /// 更新或添加角色信息（公共方法，可被外部调用）
    /// </summary>
    /// <param name="characterBase">角色数据</param>
    public void UpdateCharacterInfo(CharacterBase characterBase)
    {
        if (characterBase == null)
        {
            Debug.LogError("[DataMgr] 无法更新空的角色信息！");
            return;
        }

        string roleId = characterBase.RoleId;
        if (string.IsNullOrEmpty(roleId))
        {
            Debug.LogError("[DataMgr] 角色 RoleId 为空，无法更新！");
            return;
        }

        // 1. 存储角色信息
        _allCharacterInfos[roleId] = characterBase;

        // 2. 更新 player_id -> role_id 映射
        if (!string.IsNullOrEmpty(characterBase.PlayerId))
        {
            _playerToRoleMap[characterBase.PlayerId] = roleId;
            Debug.Log($"[DataMgr] 角色映射更新 - PlayerId: {characterBase.PlayerId} → RoleId: {roleId}");
        }

        Debug.Log($"[DataMgr] 角色信息已更新：{characterBase.RoleName} (ID: {roleId})");

        // 可选：打印详细信息（可关闭发布时）
        // PrintCharacterInfo(characterBase);

        // 3. 尝试生成该角色（如果条件满足）
        TrySpawnCharacterForPlayer(characterBase);
    }
    /// <summary>
    /// 获取所有角色信息
    /// </summary>
    public IEnumerable<CharacterBase> AllCharacterInfos => _allCharacterInfos.Values;

    /// <summary>
    /// 根据 ID 获取角色
    /// </summary>
    public CharacterBase GetCharacterInfo(string roleId)
    {
        _allCharacterInfos.TryGetValue(roleId, out CharacterBase info);
        return info;
    }

    /// <summary>
    /// 标记角色已生成
    /// </summary>
    public void MarkPlayerAsSpawned(string roleId)
    {
        if (!string.IsNullOrEmpty(roleId))
        {
            _spawnedPlayers.Add(roleId);
            Debug.Log($"[DataMgr] 角色 {roleId} 已标记为已生成。");
        }
    }

    /// <summary>
    /// 清除用户数据
    /// </summary>
    public void ClearUserData()
    {
        _userId = "";
        _loginStatus = "OffLine";
        _characterInfo = null;
        _allCharacterInfos.Clear();
        _spawnedPlayers.Clear();

        Debug.Log("[DataMgr] 用户数据已清除。");
    }

    private void OnApplicationQuit()
    {
        ClearUserData();
    }
}