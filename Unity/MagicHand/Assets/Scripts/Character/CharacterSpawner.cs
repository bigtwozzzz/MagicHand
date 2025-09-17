using Broadcast;
using Character;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("����")]
    public string characterAddress = "Character/Character001"; // ResMgr ʹ�õ� Addressables ��ַ
    public Transform platformRoot; // ƽ̨���ڵ�

    private bool isPlatformReady = false;
    private Queue<string> pendingSpawnRequests = new(); // ���� roleId

    // �������洢�����ɵĽ�ɫʵ������������
    private Dictionary<string, GameObject> spawnedCharacters = new();
    private Dictionary<string, int> characterToPositionId = new();

    private void OnEnable()
    {
        EventCenter.GetInstance().AddEventListener<string>(
            E_EventType.Event_Character_Spawn_Ready,
            OnCharacterSpawnReady);

        EventCenter.GetInstance().AddEventListener<GameObject>(
            E_EventType.Event_Platform_Loaded,
            OnPlatformLoaded);

        // ������������¼�
        EventCenter.GetInstance().AddEventListener<PlayerOfflineNotify>(
            E_EventType.Event_Player_Offline,
            OnPlayerOffline);

        // ������ɫ��Ϣ�����¼���������¼ʱ�յ��ľ������Ϣ��
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

        // �Ƴ���ɫ��Ϣ���¼���
        EventCenter.GetInstance().RemoveEventListener<CharacterBase>(
            E_EventType.Event_Character_Info_Update,
            OnCharacterInfoUpdate);

        pendingSpawnRequests.Clear();
        spawnedCharacters.Clear();
        characterToPositionId.Clear();
    }

    /// <summary>
    /// ������ɫ��Ϣ���£�������������߹㲥����¼ʱ�յ��ľ������Ϣ��
    /// </summary>
    private void OnCharacterInfoUpdate(CharacterBase charInfo)
    {
        if (charInfo == null || string.IsNullOrEmpty(charInfo.RoleId))
        {
            Debug.LogError("[CharacterSpawner] �յ���Ч�Ľ�ɫ��Ϣ���£�");
            return;
        }

        Debug.Log($"[CharacterSpawner] �յ���ɫ��Ϣ���£�{charInfo.RoleName} (ID: {charInfo.RoleId})");

        // 1. �����ɫ��Ϣ�� DataMgr
        DataMgr.GetInstance().UpdateCharacterInfo(charInfo);

        // 2. ������������ʹ�� roleId��
        // ע�⣺���ﲻ��ֱ�����ɣ����Ƿ�����У��ȴ�ƽ̨�������
        if (!DataMgr.GetInstance()._spawnedPlayers.Contains(charInfo.RoleId))
        {
            pendingSpawnRequests.Enqueue(charInfo.RoleId);
            ProcessPendingSpawns(); // ������������
        }
        else
        {
            Debug.Log($"[CharacterSpawner] ��ɫ {charInfo.RoleName} �����ɣ�������");
        }
    }

    #region �¼��ص�

    private void OnPlatformLoaded(GameObject platformObj)
    {
        if (platformObj == null)
        {
            Debug.LogError("[CharacterSpawner] ���յ���ƽ̨����");
            return;
        }

        platformRoot = platformObj.transform;
        isPlatformReady = true;
        Debug.Log($"[CharacterSpawner] ƽ̨�Ѽ��أ�{platformObj.name}");

        // ���Դ�����ѹ����������
        ProcessPendingSpawns();
    }

    private void OnCharacterSpawnReady(string roleId)
    {
        Debug.Log($"[CharacterSpawner] �յ���ɫ�������󣬽�ɫID: {roleId}");

        // ���������Ժ���
        pendingSpawnRequests.Enqueue(roleId);

        // ������������
        ProcessPendingSpawns();
    }

    /// <summary>
    /// ������������¼�
    /// </summary>
    private void OnPlayerOffline(PlayerOfflineNotify offlineNotify)
    {
        if (offlineNotify == null || string.IsNullOrEmpty(offlineNotify.PlayerId))
        {
            Debug.LogError("[CharacterSpawner] �յ���Ч������֪ͨ��");
            return;
        }

        // ͨ�� player_id �� role_id
        string roleId = DataMgr.GetInstance().GetRoleIdByPlayerId(offlineNotify.PlayerId);
        Debug.Log($"[CharacterSpawner] �յ��������֪ͨ��{roleId}");

        RemoveCharacter(roleId);
    }

    #endregion

    #region ������������

    /// <summary>
    /// �������еȴ��еĽ�ɫ��������
    /// </summary>
    private void ProcessPendingSpawns()
    {
        if (!isPlatformReady || platformRoot == null)
            return;

        List<string> pendingRoleIds = new();

        // ��ȡΨһ roleId�������ظ�
        HashSet<string> processed = new();
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

        // �ָ�δ���������������ɻ���Ч�ģ�
        foreach (string id in processed)
        {
            if (!pendingRoleIds.Contains(id))
            {
                // ���û��������˵���������ˣ�����Ҫ�Ż�
            }
        }
        // ע�⣺�������ǲ��ٷŻأ���Ϊ�Ѿ�������ȥ��

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

            //  �ؼ����ȱ��Ϊ�����ɣ���ֹ�����¼��ٴδ���
            DataMgr.GetInstance().MarkPlayerAsSpawned(roleId);

            if (!TrySpawnSingleCharacterWithConsistentPosition(charInfo, posManager))
            {
                // ����ʧ�ܣ��Ƴ���ǣ���������
                DataMgr.GetInstance().RemoveFromSpawnedPlayers(roleId);
            }
        }
    }
    private bool TrySpawnSingleCharacterWithConsistentPosition(CharacterBase charInfo, PositionManager posManager)
    {
        // 1. ��ȡ����δ��ռ�õĵ�λ ID����С��������
        List<int> availablePositions = new();
        for (int i = 0; i < posManager.positionCount; i++)
        {
            if (!posManager.IsPositionOccupied(i))
            {
                availablePositions.Add(i);
            }
        }

        if (availablePositions.Count == 0)
        {
            Debug.LogError("[CharacterSpawner] �޿��ó����㣡");
            return false;
        }

        // 2.  ʹ�ý�ɫ ID + ȫ������ ���ɡ�ȷ�������������
        // ����ÿ����ɫ�����пͻ����϶���ѡ��ͬһ����λ
        int deterministicIndex = GetDeterministicIndex(charInfo.RoleId, availablePositions.Count);

        int selectedPosId = availablePositions[deterministicIndex];
        Vector3 spawnPos = posManager.GetPosition(selectedPosId);
        spawnPos.y += 30.0f; // ��ԭ�е�ƫ��

        // 3. ����ռ�õ�λ
        posManager.OccupyPosition(selectedPosId);
        Debug.Log($"[CharacterSpawner] Ϊ��ɫ {charInfo.RoleName} ����ȷ���Ե�λ {selectedPosId}");

        // 4. �첽���ز�����
        ResMgr.GetInstance().LoadAsync<GameObject>(
            address: characterAddress,
            callback: (prefab) =>
            {
                if (prefab == null)
                {
                    Debug.LogError($"[CharacterSpawner] Ԥ�������ʧ��: {characterAddress}");
                    posManager.ReleasePosition(selectedPosId); // ʧ�����ͷ�
                    return;
                }

                GameObject roleInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
                roleInstance.name = $"Role_{charInfo.RoleName}_{charInfo.RoleId}";

                if (roleInstance.TryGetComponent<CharacterInit>(out var charInit))
                {
                    charInit.ApplyData(charInfo);
                }

                spawnedCharacters[charInfo.RoleId] = roleInstance;
                characterToPositionId[charInfo.RoleId] = selectedPosId;

                Debug.Log($"[CharacterSpawner] ��ɫ {charInfo.RoleName} �ɹ������ڵ�λ {selectedPosId}");
            },
            autoRelease: true
        );

        return true;
    }
    /// <summary>
    /// ���� roleId �� ȫ�����ӣ����� [0, range) ��Χ�ڵ�ȷ��������
    /// </summary>
    private int GetDeterministicIndex(string roleId, int range)
    {
        if (range <= 1) return 0;

        // ���ȫ�����Ӻͽ�ɫ ID ���ɹ�ϣ
        int hash = DataMgr.GetInstance().GlobalRandomSeed;
        foreach (char c in roleId)
        {
            hash ^= c;
            hash = hash * 31 + c; // �򵥹�ϣ
        }

        // ȷ������
        hash = Mathf.Abs(hash);
        return hash % range;
    }
    /// <summary>
    /// �������ɵ�����ɫ��ʹ�� ResMgr �첽���أ�
    /// </summary>
    //private bool TrySpawnSingleCharacter(CharacterBase charInfo)
    //{
    //    PositionManager posManager = platformRoot.GetComponentInChildren<PositionManager>();
    //    if (posManager == null)
    //    {
    //        Debug.LogError("[CharacterSpawner] δ�ҵ� PositionManager��");
    //        return false;
    //    }

    //    //  1. ��ȡ���е�λ
    //    int posId = GetRandomAvailablePosition(posManager);
    //    if (posId == -1)
    //    {
    //        Debug.LogError("[CharacterSpawner] �޿��ó����㣡");
    //        return false;
    //    }

    //    Vector3 spawnPos = posManager.GetPosition(posId);
    //    spawnPos.y += 30.0f;

    //    //  2.  ����ռ�õ�λ����Ҫ�ȵ��������
    //    posManager.OccupyPosition(posId);
    //    Debug.Log($"[CharacterSpawner] ��Ԥռ�õ�λ {posId}��׼�����ɽ�ɫ {charInfo.RoleName}");

    //    //  3. �첽���ز�ʵ����
    //    ResMgr.GetInstance().LoadAsync<GameObject>(
    //        address: characterAddress,
    //        callback: (prefab) =>
    //        {
    //            if (prefab == null)
    //            {
    //                Debug.LogError($"[CharacterSpawner] Ԥ�������ʧ�ܣ���ַ: {characterAddress}");

    //                //  ����ʧ�ܣ�Ҫ�ͷŵ�λ��
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
    //                Debug.LogWarning($"[CharacterSpawner] ��ɫȱ�� CharacterInit ���: {roleInstance.name}");
    //            }

    //            // ��¼���ɵĽ�ɫ�͵�λ
    //            spawnedCharacters[charInfo.RoleId] = roleInstance;
    //            characterToPositionId[charInfo.RoleId] = posId;

    //            Debug.Log($"[CharacterSpawner] ��ɫ {charInfo.RoleName} �ɹ������ڵ�λ {posId}");
    //        },
    //        autoRelease: true
    //    );

    //    return true;
    //}

    #endregion

    #region ��ɫ�Ƴ��߼���������ߣ�

    /// <summary>
    /// �Ƴ�ָ����ɫ��������ߣ�
    /// </summary>
    private void RemoveCharacter(string roleId)
    {
        if (string.IsNullOrEmpty(roleId))
            return;

        // 1. ����Ƿ�������
        if (!spawnedCharacters.TryGetValue(roleId, out GameObject roleInstance))
        {
            Debug.Log($"[CharacterSpawner] ��ɫ {roleId} δ�ҵ���δ���ɣ������Ƴ���");
            return;
        }

        // 2. �ͷų�����
        if (characterToPositionId.TryGetValue(roleId, out int posId))
        {
            ReleaseCharacterPosition(posId);
            characterToPositionId.Remove(roleId);
        }

        // 3. ���� GameObject
        Destroy(roleInstance);
        spawnedCharacters.Remove(roleId);

        // 4. �� DataMgr ���Ƴ����ɱ��
        DataMgr.GetInstance()._spawnedPlayers.Remove(roleId);

        Debug.Log($"[CharacterSpawner] ��ɫ {roleId} ���Ƴ���������Դ��");
    }

    #endregion

    #region ���������

    /// <summary>
    /// ��ȡ������г����� ID��ʹ��ȫ�����ӣ�
    /// </summary>
    private int GetRandomAvailablePosition(PositionManager posManager)
    {
        if (posManager == null)
        {
            Debug.LogError("[CharacterSpawner] PositionManager Ϊ null��");
            return -1;
        }

        List<int> available = new();
        for (int i = 0; i < posManager.positionCount; i++)
        {
            if (!posManager.IsPositionOccupied(i))
            {
                available.Add(i);
            }
        }

        if (available.Count == 0)
        {
            Debug.LogError("[CharacterSpawner] ���е�λ�ѱ�ռ�ã�");
            return -1;
        }

        // ʹ��ȫ�����ӣ�ȷ�����һ��
        Random.InitState(DataMgr.GetInstance().GlobalRandomSeed);
        int index = Random.Range(0, available.Count);
        return available[index];
    }

    /// <summary>
    /// �ͷ�ָ�������㣨��ɫ����ʱ���ã�
    /// </summary>
    public void ReleaseCharacterPosition(int positionId)
    {
        if (platformRoot == null) return;

        PositionManager posManager = platformRoot.GetComponentInChildren<PositionManager>();
        if (posManager != null && posManager.IsPositionOccupied(positionId))
        {
            posManager.ReleasePosition(positionId);
            Debug.Log($"[CharacterSpawner] ������ {positionId} ���ͷ�");
        }
    }

    #endregion
}