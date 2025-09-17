using UnityEngine;

/// <summary>
/// 相机跟随玩家脚本
/// 挂载在相机上，让相机跟随本地玩家位置
/// </summary>
public class CameraFollowPlayer : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, -1.5f);  // 相机相对玩家的偏移
    [SerializeField] private float followSpeed = 5f;                        // 跟随速度
    [SerializeField] private bool smoothFollow = true;                      // 是否平滑跟随
    
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;                     // 显示调试信息
    
    private Transform targetPlayer;                                          // 目标玩家Transform
    private int currentPlayerId = -1;                                       // 当前跟随的玩家ID
    
    private void Start()
    {
        // 订阅玩家身份变更事件
        PlayerIdentity.OnPlayerIdentityChanged += OnPlayerIdentityChanged;
        
        // 初始化时查找本地玩家
        FindLocalPlayer();
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        PlayerIdentity.OnPlayerIdentityChanged -= OnPlayerIdentityChanged;
    }
    
    private void LateUpdate()
    {
        // 如果没有目标玩家，尝试查找
        if (targetPlayer == null)
        {
            FindLocalPlayer();
            return;
        }
        
        // 计算目标位置
        Vector3 targetPosition = targetPlayer.position + offset;
        
        // 更新相机位置
        if (smoothFollow)
        {
            // 平滑跟随
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            // 直接跟随
            transform.position = targetPosition;
        }
    }
    
    /// <summary>
    /// 查找本地玩家
    /// </summary>
    private void FindLocalPlayer()
    {
        if (PlayerManager.Instance == null) return;
        
        var players = PlayerManager.Instance.GetActivePlayers();
        foreach (var player in players)
        {
            PlayerIdentity identity = player.playerObject.GetComponent<PlayerIdentity>();
            if (identity != null && identity.IsMainPlayer)
            {
                SetTargetPlayer(identity.PlayerId, player.playerObject.transform);
                return;
            }
        }
        
        // 如果没有找到主玩家，默认跟随1号玩家
        var player1Data = PlayerManager.Instance.GetPlayerData(1);
        if (player1Data != null && player1Data.playerObject != null)
        {
            SetTargetPlayer(1, player1Data.playerObject.transform);
        }
    }
    
    /// <summary>
    /// 设置目标玩家
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="playerTransform">玩家Transform</param>
    private void SetTargetPlayer(int playerId, Transform playerTransform)
    {
        if (currentPlayerId == playerId && targetPlayer == playerTransform) return;
        
        currentPlayerId = playerId;
        targetPlayer = playerTransform;
        
        if (showDebugInfo)
        {
            Debug.Log($"[CameraFollowPlayer] 相机开始跟随玩家{playerId}: {playerTransform.name}");
        }
    }
    
    /// <summary>
    /// 玩家身份变更事件处理
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="playerName">玩家名称</param>
    private void OnPlayerIdentityChanged(int playerId, string playerName)
    {
        // 重新查找本地玩家
        FindLocalPlayer();
    }
    
    /// <summary>
    /// 设置相机偏移
    /// </summary>
    /// <param name="newOffset">新的偏移值</param>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        if (showDebugInfo)
        {
            Debug.Log($"[CameraFollowPlayer] 相机偏移已更新: {offset}");
        }
    }
    
    /// <summary>
    /// 设置跟随速度
    /// </summary>
    /// <param name="speed">跟随速度</param>
    public void SetFollowSpeed(float speed)
    {
        followSpeed = Mathf.Max(0.1f, speed);
        if (showDebugInfo)
        {
            Debug.Log($"[CameraFollowPlayer] 跟随速度已更新: {followSpeed}");
        }
    }
    
    /// <summary>
    /// 切换平滑跟随模式
    /// </summary>
    /// <param name="smooth">是否平滑跟随</param>
    public void SetSmoothFollow(bool smooth)
    {
        smoothFollow = smooth;
        if (showDebugInfo)
        {
            Debug.Log($"[CameraFollowPlayer] 平滑跟随模式: {(smooth ? "开启" : "关闭")}");
        }
    }
    
    /// <summary>
    /// 立即移动到目标位置（不使用平滑跟随）
    /// </summary>
    public void TeleportToTarget()
    {
        if (targetPlayer != null)
        {
            transform.position = targetPlayer.position + offset;
            if (showDebugInfo)
            {
                Debug.Log($"[CameraFollowPlayer] 相机已瞬移到玩家{currentPlayerId}位置");
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // 绘制目标位置
        if (targetPlayer != null)
        {
            Vector3 targetPos = targetPlayer.position + offset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPos, 0.5f);
            Gizmos.DrawLine(targetPlayer.position, targetPos);
        }
        
        // 绘制当前相机位置
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}