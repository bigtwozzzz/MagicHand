using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 简化版手部控制器
/// 适用于快速原型开发和基本手部动作同步
/// 无需复杂的骨骼映射，直接控制手部Transform
/// </summary>
public class SimpleHandController : MonoBehaviour
{
    [Header("手势数据接收器")]
    public HandGestureUDPReceiver gestureReceiver;
    
    [Header("手部模型")]
    public Transform leftHandModel;   // 左手模型根节点
    public Transform rightHandModel;  // 右手模型根节点
    
    [Header("控制参数")]
    public bool enableLeftHand = true;
    public bool enableRightHand = true;
    public float positionSensitivity = 5.0f;  // 位置敏感度
    public float rotationSensitivity = 2.0f;  // 旋转敏感度
    public float smoothSpeed = 10.0f;         // 平滑速度
    
    [Header("手势动作映射")]
    public bool enableGestureAnimations = true;
    public float gestureAnimationDuration = 0.5f;
    
    [Header("调试")]
    public bool showDebugInfo = false;
    
    // 手部状态
    private bool leftHandActive = false;
    private bool rightHandActive = false;
    
    // 目标位置和旋转
    private Vector3 leftHandTargetPosition;
    private Vector3 rightHandTargetPosition;
    private Quaternion leftHandTargetRotation;
    private Quaternion rightHandTargetRotation;
    
    // 当前手势
    private int currentLeftGesture = 0;
    private int currentRightGesture = 0;
    
    // 动画协程引用
    private Coroutine leftHandAnimationCoroutine;
    private Coroutine rightHandAnimationCoroutine;
    
    void Start()
    {
        // 查找手势接收器
        if (gestureReceiver == null)
        {
            gestureReceiver = FindObjectOfType<HandGestureUDPReceiver>();
        }
        
        if (gestureReceiver != null)
        {
            // 订阅事件
            gestureReceiver.OnHandDataReceived += OnHandDataReceived;
            gestureReceiver.OnLeftHandGesture += OnLeftHandGesture;
            gestureReceiver.OnRightHandGesture += OnRightHandGesture;
        }
        else
        {
            Debug.LogError("SimpleHandController: 未找到HandGestureUDPReceiver组件！");
        }
        
        // 初始化手部模型状态
        if (leftHandModel != null)
            leftHandModel.gameObject.SetActive(false);
        if (rightHandModel != null)
            rightHandModel.gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        if (gestureReceiver != null)
        {
            gestureReceiver.OnHandDataReceived -= OnHandDataReceived;
            gestureReceiver.OnLeftHandGesture -= OnLeftHandGesture;
            gestureReceiver.OnRightHandGesture -= OnRightHandGesture;
        }
    }
    
    void Update()
    {
        // 平滑更新手部位置和旋转
        UpdateHandTransform(leftHandModel, leftHandTargetPosition, leftHandTargetRotation, leftHandActive);
        UpdateHandTransform(rightHandModel, rightHandTargetPosition, rightHandTargetRotation, rightHandActive);
    }
    
    /// <summary>
    /// 接收手部数据
    /// </summary>
    void OnHandDataReceived(List<HandGestureUDPReceiver.HandData> hands)
    {
        leftHandActive = false;
        rightHandActive = false;
        
        foreach (var hand in hands)
        {
            if (hand.hand_side == "Left" && enableLeftHand)
            {
                leftHandActive = true;
                UpdateHandData(hand, ref leftHandTargetPosition, ref leftHandTargetRotation, true);
            }
            else if (hand.hand_side == "Right" && enableRightHand)
            {
                rightHandActive = true;
                UpdateHandData(hand, ref rightHandTargetPosition, ref rightHandTargetRotation, false);
            }
        }
        
        // 控制手部模型显示
        if (leftHandModel != null)
            leftHandModel.gameObject.SetActive(leftHandActive);
        if (rightHandModel != null)
            rightHandModel.gameObject.SetActive(rightHandActive);
    }
    
    /// <summary>
    /// 左手手势事件
    /// </summary>
    void OnLeftHandGesture(HandGestureUDPReceiver.HandData leftHand)
    {
        if (currentLeftGesture != leftHand.gesture_id)
        {
            currentLeftGesture = leftHand.gesture_id;
            
            if (showDebugInfo)
                Debug.Log($"左手手势变化: {leftHand.gesture_name} (ID: {leftHand.gesture_id})");
            
            if (enableGestureAnimations)
                TriggerGestureAnimation(leftHandModel, leftHand.gesture_id, true);
        }
    }
    
    /// <summary>
    /// 右手手势事件
    /// </summary>
    void OnRightHandGesture(HandGestureUDPReceiver.HandData rightHand)
    {
        if (currentRightGesture != rightHand.gesture_id)
        {
            currentRightGesture = rightHand.gesture_id;
            
            if (showDebugInfo)
                Debug.Log($"右手手势变化: {rightHand.gesture_name} (ID: {rightHand.gesture_id})");
            
            if (enableGestureAnimations)
                TriggerGestureAnimation(rightHandModel, rightHand.gesture_id, false);
        }
    }
    
    /// <summary>
    /// 更新手部数据
    /// </summary>
    void UpdateHandData(HandGestureUDPReceiver.HandData hand, ref Vector3 targetPosition, ref Quaternion targetRotation, bool isLeftHand)
    {
        if (hand.unityLandmarks == null || hand.unityLandmarks.Length < 21)
            return;
        
        // 使用手腕位置作为手部中心
        Vector3 wristPos = hand.unityLandmarks[0];
        Vector3 middleFingerBase = hand.unityLandmarks[9];
        Vector3 indexFingerTip = hand.unityLandmarks[8];
        
        // 计算手部位置（基于手腕）
        targetPosition = new Vector3(
            (wristPos.x - 0.5f) * positionSensitivity,
            (wristPos.y - 0.5f) * positionSensitivity,
            wristPos.z * positionSensitivity
        );
        
        // 计算手部旋转（基于手腕到中指的方向）
        Vector3 handDirection = (middleFingerBase - wristPos).normalized;
        Vector3 upDirection = (indexFingerTip - wristPos).normalized;
        
        if (handDirection != Vector3.zero)
        {
            // 为左右手调整旋转
            if (isLeftHand)
            {
                targetRotation = Quaternion.LookRotation(handDirection, upDirection) * Quaternion.Euler(0, 180, 0);
            }
            else
            {
                targetRotation = Quaternion.LookRotation(handDirection, upDirection);
            }
            
            targetRotation *= Quaternion.Euler(0, 0, rotationSensitivity);
        }
    }
    
    /// <summary>
    /// 更新手部Transform
    /// </summary>
    void UpdateHandTransform(Transform handTransform, Vector3 targetPos, Quaternion targetRot, bool isActive)
    {
        if (handTransform == null || !isActive) return;
        
        // 平滑移动到目标位置和旋转
        handTransform.localPosition = Vector3.Lerp(
            handTransform.localPosition, 
            targetPos, 
            Time.deltaTime * smoothSpeed
        );
        
        handTransform.localRotation = Quaternion.Lerp(
            handTransform.localRotation, 
            targetRot, 
            Time.deltaTime * smoothSpeed
        );
    }
    
    /// <summary>
    /// 触发手势动画
    /// </summary>
    void TriggerGestureAnimation(Transform handModel, int gestureId, bool isLeftHand)
    {
        if (handModel == null) return;
        
        // 停止之前的动画
        if (isLeftHand && leftHandAnimationCoroutine != null)
        {
            StopCoroutine(leftHandAnimationCoroutine);
        }
        else if (!isLeftHand && rightHandAnimationCoroutine != null)
        {
            StopCoroutine(rightHandAnimationCoroutine);
        }
        
        // 根据手势ID执行不同的动画
        switch (gestureId)
        {
            case 1: // Wave
                if (isLeftHand)
                    leftHandAnimationCoroutine = StartCoroutine(WaveAnimation(handModel));
                else
                    rightHandAnimationCoroutine = StartCoroutine(WaveAnimation(handModel));
                break;
                
            case 5: // Like (Thumbs up)
                if (isLeftHand)
                    leftHandAnimationCoroutine = StartCoroutine(ThumbsUpAnimation(handModel));
                else
                    rightHandAnimationCoroutine = StartCoroutine(ThumbsUpAnimation(handModel));
                break;
                
            case 10: // Fist
                if (isLeftHand)
                    leftHandAnimationCoroutine = StartCoroutine(FistAnimation(handModel));
                else
                    rightHandAnimationCoroutine = StartCoroutine(FistAnimation(handModel));
                break;
                
            case 11: // Peace
                if (isLeftHand)
                    leftHandAnimationCoroutine = StartCoroutine(PeaceAnimation(handModel));
                else
                    rightHandAnimationCoroutine = StartCoroutine(PeaceAnimation(handModel));
                break;
        }
    }
    
    #region 手势动画协程
    
    /// <summary>
    /// 挥手动画
    /// </summary>
    System.Collections.IEnumerator WaveAnimation(Transform hand)
    {
        Vector3 originalScale = hand.localScale;
        float timer = 0;
        
        while (timer < gestureAnimationDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / gestureAnimationDuration;
            
            // 简单的缩放动画
            float scale = 1.0f + Mathf.Sin(progress * Mathf.PI * 4) * 0.1f;
            hand.localScale = originalScale * scale;
            
            yield return null;
        }
        
        hand.localScale = originalScale;
    }
    
    /// <summary>
    /// 点赞动画
    /// </summary>
    System.Collections.IEnumerator ThumbsUpAnimation(Transform hand)
    {
        Vector3 originalPosition = hand.localPosition;
        float timer = 0;
        
        while (timer < gestureAnimationDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / gestureAnimationDuration;
            
            // 向上弹跳动画
            float bounce = Mathf.Sin(progress * Mathf.PI) * 0.2f;
            hand.localPosition = originalPosition + Vector3.up * bounce;
            
            yield return null;
        }
        
        hand.localPosition = originalPosition;
    }
    
    /// <summary>
    /// 拳头动画
    /// </summary>
    System.Collections.IEnumerator FistAnimation(Transform hand)
    {
        Vector3 originalScale = hand.localScale;
        float timer = 0;
        
        while (timer < gestureAnimationDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / gestureAnimationDuration;
            
            // 收缩动画
            float scale = 1.0f - Mathf.Sin(progress * Mathf.PI) * 0.15f;
            hand.localScale = originalScale * scale;
            
            yield return null;
        }
        
        hand.localScale = originalScale;
    }
    
    /// <summary>
    /// 胜利手势动画
    /// </summary>
    System.Collections.IEnumerator PeaceAnimation(Transform hand)
    {
        Quaternion originalRotation = hand.localRotation;
        float timer = 0;
        
        while (timer < gestureAnimationDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / gestureAnimationDuration;
            
            // 旋转动画
            float rotation = Mathf.Sin(progress * Mathf.PI * 2) * 15f;
            hand.localRotation = originalRotation * Quaternion.Euler(0, 0, rotation);
            
            yield return null;
        }
        
        hand.localRotation = originalRotation;
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置手部模型
    /// </summary>
    public void SetHandModels(Transform leftHand, Transform rightHand)
    {
        leftHandModel = leftHand;
        rightHandModel = rightHand;
    }
    
    /// <summary>
    /// 获取当前手势状态
    /// </summary>
    public (int leftGesture, int rightGesture) GetCurrentGestures()
    {
        return (currentLeftGesture, currentRightGesture);
    }
    
    /// <summary>
    /// 重置手部状态
    /// </summary>
    public void ResetHandStates()
    {
        leftHandActive = false;
        rightHandActive = false;
        currentLeftGesture = 0;
        currentRightGesture = 0;
        
        if (leftHandModel != null)
            leftHandModel.gameObject.SetActive(false);
        if (rightHandModel != null)
            rightHandModel.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 启用/禁用手部控制
    /// </summary>
    public void SetHandEnabled(bool leftEnabled, bool rightEnabled)
    {
        enableLeftHand = leftEnabled;
        enableRightHand = rightEnabled;
    }
    
    #endregion
}