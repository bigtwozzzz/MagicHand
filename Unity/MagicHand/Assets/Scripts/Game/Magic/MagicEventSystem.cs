using System;
using UnityEngine;

/// <summary>
/// 魔法事件系统
/// 定义所有魔法相关事件，提供统一的事件触发接口
/// </summary>
public static class MagicEventSystem
{
    // 魔法触发事件
    public static event Action<int, MagicData, int> OnMagicTriggered;
    
    // 魔法冷却开始事件
    public static event Action<int, float> OnMagicCooldownStart;
    
    // 魔法冷却结束事件
    public static event Action<int> OnMagicCooldownEnd;
    
    // 手势检测事件
    public static event Action<int> OnGestureDetected;
    
    // 魔法特效播放事件
    public static event Action<int, Vector3, Quaternion> OnMagicEffectPlay;
    
    // 魔法特效回收事件
    public static event Action<GameObject> OnMagicEffectRecycle;
    
    /// <summary>
    /// 触发魔法事件
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <param name="magicData">魔法数据</param>
    /// <param name="playerId">触发魔法的玩家ID</param>
    public static void TriggerMagic(int magicId, MagicData magicData, int playerId = 1)
    {
        OnMagicTriggered?.Invoke(magicId, magicData, playerId);
        Debug.Log($"[MagicEventSystem] 玩家{playerId}触发魔法事件: {magicData.magicName} (ID: {magicId})");
    }
    
    /// <summary>
    /// 触发魔法冷却开始事件
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <param name="cooldownTime">冷却时间</param>
    public static void StartMagicCooldown(int magicId, float cooldownTime)
    {
        OnMagicCooldownStart?.Invoke(magicId, cooldownTime);
        Debug.Log($"[MagicEventSystem] 魔法 {magicId} 开始冷却，时长: {cooldownTime}s");
    }
    
    /// <summary>
    /// 触发魔法冷却结束事件
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    public static void EndMagicCooldown(int magicId)
    {
        OnMagicCooldownEnd?.Invoke(magicId);
        Debug.Log($"[MagicEventSystem] 魔法 {magicId} 冷却结束");
    }
    
    /// <summary>
    /// 触发手势检测事件
    /// </summary>
    /// <param name="gestureId">手势ID</param>
    public static void DetectGesture(int gestureId)
    {
        OnGestureDetected?.Invoke(gestureId);
        Debug.Log($"[MagicEventSystem] 检测到手势: {gestureId}");
    }
    
    /// <summary>
    /// 触发魔法特效播放事件
    /// </summary>
    /// <param name="magicId">魔法ID</param>
    /// <param name="position">播放位置</param>
    /// <param name="rotation">播放旋转</param>
    public static void PlayMagicEffect(int magicId, Vector3 position, Quaternion rotation)
    {
        OnMagicEffectPlay?.Invoke(magicId, position, rotation);
        Debug.Log($"[MagicEventSystem] 播放魔法特效: {magicId} at {position}");
    }
    
    /// <summary>
    /// 触发魔法特效回收事件
    /// </summary>
    /// <param name="effectObject">特效对象</param>
    public static void RecycleMagicEffect(GameObject effectObject)
    {
        OnMagicEffectRecycle?.Invoke(effectObject);
        Debug.Log($"[MagicEventSystem] 回收魔法特效: {effectObject.name}");
    }
}