using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 新的玩家生成器
/// 支持多玩家生成，可配置预制体，基于基准物体位置排列
/// </summary>
public class NewPlayerSpawner : MonoBehaviour
{
    [Header("玩家配置")]
    [SerializeField] private GameObject playerPrefab; // 玩家预制体，可选
    [SerializeField] private Transform playerBaseTransform; // 玩家基准物体
    [SerializeField] private float playerSpacing = 4f; // 玩家间距
    
    [Header("生成状态")]
    [SerializeField] private List<GameObject> spawnedPlayers = new List<GameObject>(); // 已生成的玩家列表
    
    private void Awake()
    {
        // 如果没有设置基准物体，使用当前物体作为基准
        if (playerBaseTransform == null)
        {
            playerBaseTransform = transform;
        }
    }
    
    /// <summary>
    /// 生成指定数量的玩家
    /// </summary>
    /// <param name="playerCount">玩家数量</param>
    public void SpawnPlayers(int playerCount)
    {
        // 清除所有已生成的玩家
        ClearAllPlayers();
        
        if (playerCount <= 0)
        {
            Debug.LogWarning("[NewPlayerSpawner] 玩家数量必须大于0");
            return;
        }
        
        if (playerBaseTransform == null)
        {
            Debug.LogError("[NewPlayerSpawner] 未设置玩家基准物体");
            return;
        }
        
        Debug.Log($"[NewPlayerSpawner] 开始生成 {playerCount} 个玩家");
        
        for (int i = 0; i < playerCount; i++)
        {
            Vector3 spawnPosition = CalculatePlayerPosition(i, playerCount);
            GameObject player = CreatePlayer(spawnPosition, i);
            
            if (player != null)
            {
                spawnedPlayers.Add(player);
                
                // 注册到玩家管理器
                int playerId = PlayerManager.Instance.RegisterPlayer(player, spawnPosition);
                Debug.Log($"[NewPlayerSpawner] 玩家 {playerId} 生成成功，位置: {spawnPosition}");
            }
        }
        
        Debug.Log($"[NewPlayerSpawner] 玩家生成完成，共生成 {spawnedPlayers.Count} 个玩家");
    }
    
    /// <summary>
    /// 计算玩家生成位置
    /// </summary>
    /// <param name="playerIndex">玩家索引</param>
    /// <param name="totalPlayers">总玩家数</param>
    /// <returns>生成位置</returns>
    private Vector3 CalculatePlayerPosition(int playerIndex, int totalPlayers)
    {
        Vector3 basePosition = playerBaseTransform.position;
        
        // 计算偏移量：以基准物体为中心，沿x轴排列
        float totalWidth = (totalPlayers - 1) * playerSpacing;
        float startX = -totalWidth / 2f;
        float offsetX = startX + playerIndex * playerSpacing;
        
        return new Vector3(basePosition.x + offsetX, basePosition.y, basePosition.z);
    }
    
    /// <summary>
    /// 创建单个玩家
    /// </summary>
    /// <param name="position">生成位置</param>
    /// <param name="playerIndex">玩家索引</param>
    /// <returns>生成的玩家GameObject</returns>
    private GameObject CreatePlayer(Vector3 position, int playerIndex)
    {
        GameObject player;
        
        if (playerPrefab != null)
        {
            // 使用预制体生成
            player = Instantiate(playerPrefab, position, Quaternion.identity);
            player.name = $"Player_{playerIndex + 1}_Prefab";
        }
        else
        {
            // 使用胶囊体代替
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.transform.position = position;
            player.transform.rotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;
            player.name = $"Player_{playerIndex + 1}_Capsule";
            
            // 添加一些基本组件
            if (!player.GetComponent<Rigidbody>())
            {
                Rigidbody rb = player.AddComponent<Rigidbody>();
                rb.freezeRotation = true; // 防止倾倒
            }
        }
        
        // 添加或获取PlayerIdentity组件
        PlayerIdentity playerIdentity = player.GetComponent<PlayerIdentity>();
        if (playerIdentity == null)
        {
            playerIdentity = player.AddComponent<PlayerIdentity>();
        }
        
        // 设置玩家ID和相关信息
        int playerId = playerIndex + 1;
        playerIdentity.SetPlayerId(playerId);
        playerIdentity.SetPlayerName($"Player_{playerId}");
        playerIdentity.SetMainPlayer(playerIndex == 0); // 第一个玩家设为主玩家
        
        // 设置父物体为当前生成器
        player.transform.SetParent(transform);
        
        return player;
    }
    
    /// <summary>
    /// 清除所有已生成的玩家
    /// </summary>
    public void ClearAllPlayers()
    {
        Debug.Log($"[NewPlayerSpawner] 清除 {spawnedPlayers.Count} 个已生成的玩家");
        
        foreach (GameObject player in spawnedPlayers)
        {
            if (player != null)
            {
                DestroyImmediate(player);
            }
        }
        
        spawnedPlayers.Clear();
        
        // 清除玩家管理器中的数据
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ClearAllPlayers();
        }
    }
    
    /// <summary>
    /// 获取已生成的玩家数量
    /// </summary>
    /// <returns>玩家数量</returns>
    public int GetSpawnedPlayerCount()
    {
        // 移除空引用
        spawnedPlayers.RemoveAll(player => player == null);
        return spawnedPlayers.Count;
    }
    
    /// <summary>
    /// 获取已生成的玩家列表
    /// </summary>
    /// <returns>玩家列表</returns>
    public List<GameObject> GetSpawnedPlayers()
    {
        // 移除空引用
        spawnedPlayers.RemoveAll(player => player == null);
        return new List<GameObject>(spawnedPlayers);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (playerBaseTransform == null) return;
        
        // 绘制基准点
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerBaseTransform.position, 0.5f);
        
        // 绘制预览位置（假设生成2个玩家）
        Gizmos.color = Color.green;
        for (int i = 0; i < 2; i++)
        {
            Vector3 previewPos = CalculatePlayerPosition(i, 2);
            Gizmos.DrawWireCube(previewPos, Vector3.one);
        }
        
        // 绘制间距线
        Gizmos.color = Color.yellow;
        Vector3 pos1 = CalculatePlayerPosition(0, 2);
        Vector3 pos2 = CalculatePlayerPosition(1, 2);
        Gizmos.DrawLine(pos1, pos2);
    }
}

#if UNITY_EDITOR
/// <summary>
/// 自定义编辑器，添加右键菜单功能
/// </summary>
[CustomEditor(typeof(NewPlayerSpawner))]
public class NewPlayerSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        NewPlayerSpawner spawner = (NewPlayerSpawner)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("测试功能", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("生成1个玩家"))
        {
            spawner.SpawnPlayers(1);
        }
        
        if (GUILayout.Button("生成2个玩家"))
        {
            spawner.SpawnPlayers(2);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("生成3个玩家"))
        {
            spawner.SpawnPlayers(3);
        }
        
        if (GUILayout.Button("生成4个玩家"))
        {
            spawner.SpawnPlayers(4);
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("清除所有玩家", GUILayout.Height(30)))
        {
            spawner.ClearAllPlayers();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"当前已生成玩家数量: {spawner.GetSpawnedPlayerCount()}");
    }
}

/// <summary>
/// 右键菜单功能
/// </summary>
public static class NewPlayerSpawnerContextMenu
{
    [MenuItem("CONTEXT/NewPlayerSpawner/生成1个玩家")]
    static void SpawnOnePlayer(MenuCommand command)
    {
        NewPlayerSpawner spawner = (NewPlayerSpawner)command.context;
        spawner.SpawnPlayers(1);
    }
    
    [MenuItem("CONTEXT/NewPlayerSpawner/生成2个玩家")]
    static void SpawnTwoPlayers(MenuCommand command)
    {
        NewPlayerSpawner spawner = (NewPlayerSpawner)command.context;
        spawner.SpawnPlayers(2);
    }
    
    [MenuItem("CONTEXT/NewPlayerSpawner/生成3个玩家")]
    static void SpawnThreePlayers(MenuCommand command)
    {
        NewPlayerSpawner spawner = (NewPlayerSpawner)command.context;
        spawner.SpawnPlayers(3);
    }
    
    [MenuItem("CONTEXT/NewPlayerSpawner/生成4个玩家")]
    static void SpawnFourPlayers(MenuCommand command)
    {
        NewPlayerSpawner spawner = (NewPlayerSpawner)command.context;
        spawner.SpawnPlayers(4);
    }
    
    [MenuItem("CONTEXT/NewPlayerSpawner/清除所有玩家")]
    static void ClearAllPlayers(MenuCommand command)
    {
        NewPlayerSpawner spawner = (NewPlayerSpawner)command.context;
        spawner.ClearAllPlayers();
    }
}
#endif