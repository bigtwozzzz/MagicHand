using Broadcast;
using Character;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("配置")]
    public string characterAddress = "Character/Character001"; // ResMgr 使用的 Addressables 地址
    public Transform platformRoot; // 平台根节点

    private bool isPlatformReady = false;
    private Queue<string> pendingSpawnRequests = new Queue<string>(); // 缓存 roleId

    // 新增：存储已生成的角色实例，便于销毁
    private Dictionary<string, GameObject> spawnedCharacters = new Dictionary<string, GameObject>();
    private Dictionary<string, int> characterToPositionId = new Dictionary<string, int>();

    private void OnEnable()
    {
        EventCenter.GetInstance().AddEventListener<string>(
            E_EventType.Event_Character_Spawn_Ready,
            OnCharacterSpawnReady);

        EventCenter.GetInstance().AddEventListener<GameObject>(
            E_EventType.Event_Platform_Loaded,
            OnPlatformLoaded);

        // 监听玩家下线事件
        EventCenter.GetInstance().AddEventListener<PlayerOfflineNotify>(
            E_EventType.Event_Player_Offline,
            OnPlayerOffline);

        // 监听角色信息更新事件（包括登录时收到的旧玩家信息）
        EventCenter.GetInstance().AddEventListener<CharacterBase>(
            E_EventType.Event_Character_Info_Update,
            OnCharacterInfoUpdate);
    }

    private void OnDisable()
    {
        EventCenter.GetInstance().RemoveEventListener<string>(
            E_EventType.Event_Character_Spawn_Ready,
            OnCharacterSpawnReady);

        EventCenter.GetInstance().RemoveEventListener<GameObject>(
            E_EventType.Event_Platform_Loaded,
            OnPlatformLoaded);

        EventCenter.GetInstance().RemoveEventListener<PlayerOfflineNotify>(
            E_EventType.Event_Player_Offline,
            OnPlayerOffline);

        // 移除角色信息更新监听
        EventCenter.GetInstance().RemoveEventListener<CharacterBase>(
            E_EventType.Event_Character_Info_Update,
            OnCharacterInfoUpdate);

        pendingSpawnRequests.Clear();
        spawnedCharacters.Clear();
        characterToPositionId.Clear();
    }

    /// <summary>
    /// 处理角色信息更新（包括新玩家上线广播、登录时收到的旧玩家信息）
    /// </summary>
    private void OnCharacterInfoUpdate(CharacterBase charInfo)
    {
        if (charInfo == null || string.IsNullOrEmpty(charInfo.RoleId))
        {
            Debug.LogError("[CharacterSpawner] 收到无效的角色信息更新！");
            return;
        }

        Debug.Log($"[CharacterSpawner] 收到角色信息更新：{charInfo.RoleName} (ID: {charInfo.RoleId})");

        // 1. 保存角色信息到 DataMgr
        DataMgr.GetInstance().UpdateCharacterInfo(charInfo);

        // 2. 添加生成请求（使用 roleId）
        // 注意：这里不是直接生成，而是放入队列，等待平台加载完成
        if (!DataMgr.GetInstance()._spawnedPlayers.Contains(charInfo.RoleId))
        {
            pendingSpawnRequests.Enqueue(charInfo.RoleId);
            ProcessPendingSpawns(); // 尝试立即处理
        }
        else
        {
            Debug.Log($"[CharacterSpawner] 角色 {charInfo.RoleName} 已生成，跳过。");
        }
    }

    #region 事件回调

    private void OnPlatformLoaded(GameObject platformObj)
    {
        if (platformObj == null)
        {
            Debug.LogError("[CharacterSpawner] 接收到空平台对象！");
            return;
        }

        platformRoot = platformObj.transform;
        isPlatformReady = true;
        Debug.Log($"[CharacterSpawner] 平台已加载：{platformObj.name}");

        // 尝试处理积压的生成请求
        ProcessPendingSpawns();
    }

    private void OnCharacterSpawnReady(string roleId)
    {
        Debug.Log($"[CharacterSpawner] 收到角色生成请求，角色ID: {roleId}");

        // 缓存请求，稍后处理
        pendingSpawnRequests.Enqueue(roleId);

        // 尝试立即生成
        ProcessPendingSpawns();
    }

    /// <summary>
    /// 处理玩家下线事件
    /// </summary>
    private void OnPlayerOffline(PlayerOfflineNotify offlineNotify)
    {
        if (offlineNotify == null || string.IsNullOrEmpty(offlineNotify.PlayerId))
        {
            Debug.LogError("[CharacterSpawner] 收到无效的下线通知！");
            return;
        }

        // 通过 player_id 查 role_id
        string roleId = DataMgr.GetInstance().GetRoleIdByPlayerId(offlineNotify.PlayerId);
        Debug.Log($"[CharacterSpawner] 收到玩家下线通知：{roleId}");

        RemoveCharacter(roleId);
    }

    #endregion

    #region 核心生成流程

    /// <summary>
    /// 处理所有等待中的角色生成请求
    /// </summary>
    private void ProcessPendingSpawns()
    {
        if (!isPlatformReady || platformRoot == null)
            return;

        List<string> pendingRoleIds = new List<string>();

        // 提取唯一 roleId，避免重复
        HashSet<string> processed = new HashSet<string>();
        while (pendingSpawnRequests.Count > 0)
        {
            string roleId = pendingSpawnRequests.Dequeue();
            if (!DataMgr.GetInstance()._spawnedPlayers.Contains(roleId) && !processed.Contains(roleId))
            {
                CharacterBase info = DataMgr.GetInstance().GetCharacterInfo(roleId);
                if (info != null)
                {
                    pendingRoleIds.Add(roleId);
                    processed.Add(roleId);
                }
            }
        }

        // 恢复未处理的请求（已生成或无效的）
        foreach (string id in processed)
        {
            if (!pendingRoleIds.Contains(id))
            {
                // 如果没被处理，说明被过滤了，不需要放回
            }
        }
        // 注意：这里我们不再放回，因为已经处理了去重

        if (pendingRoleIds.Count == 0) return;

        pendingRoleIds.Sort();

        PositionManager posManager = platformRoot.GetComponentInChildren<PositionManager>();
        if (posManager == null)
        {
            Debug.LogError("[CharacterSpawner] Missing PositionManager");
            return;
        }

        for (int i = 0; i < pendingRoleIds.Count; i++)
        {
            string roleId = pendingRoleIds[i];
            CharacterBase charInfo = DataMgr.GetInstance().GetCharacterInfo(roleId);

            //  关键：先标记为已生成，防止其他事件再次触发
            DataMgr.GetInstance().MarkPlayerAsSpawned(roleId);

            if (!TrySpawnSingleCharacterWithConsistentPosition(charInfo, posManager))
            {
                // 分配失败，移除标记，允许重试
                DataMgr.GetInstance().RemoveFromSpawnedPlayers(roleId);
            }
        }
    }
    private bool TrySpawnSingleCharacterWithConsistentPosition(CharacterBase charInfo, PositionManager posManager)
    {
        // 1. 获取所有未被占用的点位 ID（从小到大排序）
        List<int> availablePositions = new List<int>();
        for (int i = 0; i < posManager.positionCount; i++)
        {
            if (!posManager.IsPositionOccupied(i))
            {
                availablePositions.Add(i);
            }
        }

        if (availablePositions.Count == 0)
        {
            Debug.LogError("[CharacterSpawner] 无可用出生点！");
            return false;
        }

        // 2.  使用角色 ID + 全局种子 生成“确定性随机”索引
        // 这样每个角色在所有客户端上都会选中同一个点位
        int deterministicIndex = GetDeterministicIndex(charInfo.RoleId, availablePositions.Count);

        int selectedPosId = availablePositions[deterministicIndex];
        Vector3 spawnPos = posManager.GetPosition(selectedPosId);
        spawnPos.y += 30.0f; // 你原有的偏移

        // 3. 立即占用点位
        posManager.OccupyPosition(selectedPosId);
        Debug.Log($"[CharacterSpawner] 为角色 {charInfo.RoleName} 分配确定性点位 {selectedPosId}");

        // 4. 异步加载并生成
        ResMgr.GetInstance().LoadAsync<GameObject>(
            address: characterAddress,
            callback: (prefab) =>
            {
                if (prefab == null)
                {
                    Debug.LogError($"[CharacterSpawner] 预制体加载失败: {characterAddress}");
                    posManager.ReleasePosition(selectedPosId); // 失败则释放
                    return;
                }

                GameObject roleInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
                roleInstance.name = $"Role_{charInfo.RoleName}_{charInfo.RoleId}";

                CharacterInit charInit = roleInstance.GetComponent<CharacterInit>();
                if (charInit != null)
                {
                    charInit.ApplyData(charInfo);
                }

                spawnedCharacters[charInfo.RoleId] = roleInstance;
                characterToPositionId[charInfo.RoleId] = selectedPosId;

                Debug.Log($"[CharacterSpawner] 角色 {charInfo.RoleName} 成功生成于点位 {selectedPosId}");
            },
            autoRelease: true
        );

        return true;
    }
    /// <summary>
    /// 基于 roleId 和 全局种子，生成 [0, range) 范围内的确定性索引
    /// </summary>
    private int GetDeterministicIndex(string roleId, int range)
    {
        if (range <= 1) return 0;

        // 结合全局种子和角色 ID 生成哈希
        int hash = DataMgr.GetInstance().GlobalRandomSeed;
        foreach (char c in roleId)
        {
            hash ^= c;
            hash = hash * 31 + c; // 简单哈希
        }

        // 确保正数
        hash = Mathf.Abs(hash);
        return hash % range;
    }
    /// <summary>
    /// 尝试生成单个角色（使用 ResMgr 异步加载）
    /// </summary>
    //private bool TrySpawnSingleCharacter(CharacterBase charInfo)
    //{
    //    PositionManager posManager = platformRoot.GetComponentInChildren<PositionManager>();
    //    if (posManager == null)
    //    {
    //        Debug.LogError("[CharacterSpawner] 未找到 PositionManager！");
    //        return false;
    //    }

    //    //  1. 获取空闲点位
    //    int posId = GetRandomAvailablePosition(posManager);
    //    if (posId == -1)
    //    {
    //        Debug.LogError("[CharacterSpawner] 无可用出生点！");
    //        return false;
    //    }

    //    Vector3 spawnPos = posManager.GetPosition(posId);
    //    spawnPos.y += 30.0f;

    //    //  2.  立即占用点位！不要等到加载完成
    //    posManager.OccupyPosition(posId);
    //    Debug.Log($"[CharacterSpawner] 已预占用点位 {posId}，准备生成角色 {charInfo.RoleName}");

    //    //  3. 异步加载并实例化
    //    ResMgr.GetInstance().LoadAsync<GameObject>(
    //        address: characterAddress,
    //        callback: (prefab) =>
    //        {
    //            if (prefab == null)
    //            {
    //                Debug.LogError($"[CharacterSpawner] 预制体加载失败，地址: {characterAddress}");

    //                //  加载失败，要释放点位！
    //                posManager.ReleasePosition(posId);
    //                return;
    //            }

    //            GameObject roleInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
    //            roleInstance.name = $"Role_{charInfo.RoleName}_{charInfo.RoleId}";

    //            CharacterInit charInit = roleInstance.GetComponent<CharacterInit>();
    //            if (charInit != null)
    //            {
    //                charInit.ApplyData(charInfo);
    //            }
    //            else
    //            {
    //                Debug.LogWarning($"[CharacterSpawner] 角色缺少 CharacterInit 组件: {roleInstance.name}");
    //            }

    //            // 记录生成的角色和点位
    //            spawnedCharacters[charInfo.RoleId] = roleInstance;
    //            characterToPositionId[charInfo.RoleId] = posId;

    //            Debug.Log($"[CharacterSpawner] 角色 {charInfo.RoleName} 成功生成于点位 {posId}");
    //        },
    //        autoRelease: true
    //    );

    //    return true;
    //}

    #endregion

    #region 角色移除逻辑（玩家下线）

    /// <summary>
    /// 移除指定角色（玩家下线）
    /// </summary>
    private void RemoveCharacter(string roleId)
    {
        if (string.IsNullOrEmpty(roleId))
            return;

        // 1. 检查是否已生成
        if (!spawnedCharacters.TryGetValue(roleId, out GameObject roleInstance))
        {
            Debug.Log($"[CharacterSpawner] 角色 {roleId} 未找到或未生成，无需移除。");
            return;
        }

        // 2. 释放出生点
        if (characterToPositionId.TryGetValue(roleId, out int posId))
        {
            ReleaseCharacterPosition(posId);
            characterToPositionId.Remove(roleId);
        }

        // 3. 销毁 GameObject
        Destroy(roleInstance);
        spawnedCharacters.Remove(roleId);

        // 4. 从 DataMgr 中移除生成标记
        DataMgr.GetInstance()._spawnedPlayers.Remove(roleId);

        Debug.Log($"[CharacterSpawner] 角色 {roleId} 已移除并清理资源。");
    }

    #endregion

    #region 出生点管理

    /// <summary>
    /// 获取随机空闲出生点 ID（使用全局种子）
    /// </summary>
    private int GetRandomAvailablePosition(PositionManager posManager)
    {
        if (posManager == null)
        {
            Debug.LogError("[CharacterSpawner] PositionManager 为 null！");
            return -1;
        }

        List<int> available = new List<int>();
        for (int i = 0; i < posManager.positionCount; i++)
        {
            if (!posManager.IsPositionOccupied(i))
            {
                available.Add(i);
            }
        }

        if (available.Count == 0)
        {
            Debug.LogError("[CharacterSpawner] 所有点位已被占用！");
            return -1;
        }

        // 使用全局种子，确保多端一致
        Random.InitState(DataMgr.GetInstance().GlobalRandomSeed);
        int index = Random.Range(0, available.Count);
        return available[index];
    }

    /// <summary>
    /// 释放指定出生点（角色销毁时调用）
    /// </summary>
    public void ReleaseCharacterPosition(int positionId)
    {
        if (platformRoot == null) return;

        PositionManager posManager = platformRoot.GetComponentInChildren<PositionManager>();
        if (posManager != null && posManager.IsPositionOccupied(positionId))
        {
            posManager.ReleasePosition(positionId);
            Debug.Log($"[CharacterSpawner] 出生点 {positionId} 已释放");
        }
    }

    #endregion
}