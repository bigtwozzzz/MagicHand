using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 手部骨骼控制器
/// 根据MediaPipe手部关键点数据控制骨骼绑定的手部模型
/// 实现虚拟手部与现实手部的同步运动
/// </summary>
public class HandSkeletonController : MonoBehaviour
{
    [Header("手势数据接收器")]
    public HandGestureUDPReceiver gestureReceiver;
    
    [Header("手部模型设置")]
    public Transform leftHandRoot;   // 左手根骨骼
    public Transform rightHandRoot;  // 右手根骨骼
    
    [Header("骨骼映射 - 左手")]
    [SerializeField] private HandBones leftHandBones;
    
    [Header("骨骼映射 - 右手")]
    [SerializeField] private HandBones rightHandBones;
    
    [Header("控制参数")]
    public bool enableLeftHand = true;
    public bool enableRightHand = true;
    public float smoothingFactor = 0.1f;  // 平滑因子，减少抖动
    public float positionScale = 1.0f;    // 位置缩放
    public bool useIK = true;             // 是否使用反向运动学
    
    [Header("调试设置")]
    public bool showDebugInfo = false;
    public bool showGizmos = false;
    
    // 简化的手部骨骼结构定义
    [System.Serializable]
    public class HandBones
    {
        [Header("手掌")]
        public Transform palm;            // 手掌 (关键点0)
        
        [Header("拇指链")]
        public Transform thumbProximal;   // 拇指近端 (关键点2)
        public Transform thumbDistal;     // 拇指远端 (关键点3)
        public Transform thumbTip;        // 拇指指尖 (关键点4)
        
        [Header("食指链")]
        public Transform indexProximal;   // 食指近端 (关键点6)
        public Transform indexDistal;     // 食指远端 (关键点7)
        public Transform indexTip;        // 食指指尖 (关键点8)
        
        [Header("中指链")]
        public Transform middleProximal;  // 中指近端 (关键点10)
        public Transform middleDistal;    // 中指远端 (关键点11)
        public Transform middleTip;       // 中指指尖 (关键点12)
        
        [Header("无名指链")]
        public Transform ringProximal;    // 无名指近端 (关键点14)
        public Transform ringDistal;      // 无名指远端 (关键点15)
        public Transform ringTip;         // 无名指指尖 (关键点16)
        
        [Header("小指链")]
        public Transform pinkyProximal;   // 小指近端 (关键点18)
        public Transform pinkyDistal;     // 小指远端 (关键点19)
        public Transform pinkyTip;        // 小指指尖 (关键点20)
        
        // 获取所有骨骼Transform的数组（按简化结构排列）
        public Transform[] GetAllBones()
        {
            return new Transform[]
            {
                palm,
                thumbProximal, thumbDistal, thumbTip,
                indexProximal, indexDistal, indexTip,
                middleProximal, middleDistal, middleTip,
                ringProximal, ringDistal, ringTip,
                pinkyProximal, pinkyDistal, pinkyTip
            };
        }
        
        // 获取手指链的映射关系
        public FingerChain[] GetFingerChains()
        {
            return new FingerChain[]
            {
                new FingerChain("拇指", new Transform[] { palm, thumbProximal, thumbDistal, thumbTip }, new int[] { 0, 2, 3, 4 }),
                new FingerChain("食指", new Transform[] { palm, indexProximal, indexDistal, indexTip }, new int[] { 0, 6, 7, 8 }),
                new FingerChain("中指", new Transform[] { palm, middleProximal, middleDistal, middleTip }, new int[] { 0, 10, 11, 12 }),
                new FingerChain("无名指", new Transform[] { palm, ringProximal, ringDistal, ringTip }, new int[] { 0, 14, 15, 16 }),
                new FingerChain("小指", new Transform[] { palm, pinkyProximal, pinkyDistal, pinkyTip }, new int[] { 0, 18, 19, 20 })
            };
        }
    }
    
    // 手指链结构
    [System.Serializable]
    public class FingerChain
    {
        public string name;
        public Transform[] bones;
        public int[] landmarkIndices;
        
        public FingerChain(string name, Transform[] bones, int[] landmarkIndices)
        {
            this.name = name;
            this.bones = bones;
            this.landmarkIndices = landmarkIndices;
        }
    }
    
    // 平滑处理用的数据
    private Vector3[] leftHandTargetPositions = new Vector3[21];
    private Vector3[] rightHandTargetPositions = new Vector3[21];
    private Vector3[] leftHandCurrentPositions = new Vector3[21];
    private Vector3[] rightHandCurrentPositions = new Vector3[21];
    
    // 手部检测状态
    private bool leftHandDetected = false;
    private bool rightHandDetected = false;
    
    void Start()
    {
        // 自动查找手势接收器
        if (gestureReceiver == null)
        {
            gestureReceiver = FindObjectOfType<HandGestureUDPReceiver>();
        }
        
        if (gestureReceiver != null)
        {
            // 订阅手势数据事件
            gestureReceiver.OnHandDataReceived += OnHandDataReceived;
            gestureReceiver.OnLeftHandGesture += OnLeftHandDetected;
            gestureReceiver.OnRightHandGesture += OnRightHandDetected;
        }
        else
        {
            Debug.LogError("HandSkeletonController: 未找到HandGestureUDPReceiver组件！");
        }
        
        // 初始化位置数组
        InitializePositionArrays();
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (gestureReceiver != null)
        {
            gestureReceiver.OnHandDataReceived -= OnHandDataReceived;
            gestureReceiver.OnLeftHandGesture -= OnLeftHandDetected;
            gestureReceiver.OnRightHandGesture -= OnRightHandDetected;
        }
    }
    
    void Update()
    {
        // 平滑更新手部位置
        if (enableLeftHand && leftHandDetected)
        {
            UpdateHandSkeleton(leftHandBones, leftHandTargetPositions, leftHandCurrentPositions);
        }
        
        if (enableRightHand && rightHandDetected)
        {
            UpdateHandSkeleton(rightHandBones, rightHandTargetPositions, rightHandCurrentPositions);
        }
    }
    
    /// <summary>
    /// 初始化位置数组
    /// </summary>
    void InitializePositionArrays()
    {
        for (int i = 0; i < 21; i++)
        {
            leftHandTargetPositions[i] = Vector3.zero;
            rightHandTargetPositions[i] = Vector3.zero;
            leftHandCurrentPositions[i] = Vector3.zero;
            rightHandCurrentPositions[i] = Vector3.zero;
        }
    }
    
    /// <summary>
    /// 接收到手部数据时的回调
    /// </summary>
    void OnHandDataReceived(List<HandGestureUDPReceiver.HandData> hands)
    {
        leftHandDetected = false;
        rightHandDetected = false;
        
        foreach (var hand in hands)
        {
            if (hand.hand_side == "Left" && enableLeftHand)
            {
                leftHandDetected = true;
                UpdateHandTargetPositions(hand, leftHandTargetPositions);
                
                if (showDebugInfo)
                    Debug.Log($"左手检测到: {hand.gesture_name}");
            }
            else if (hand.hand_side == "Right" && enableRightHand)
            {
                rightHandDetected = true;
                UpdateHandTargetPositions(hand, rightHandTargetPositions);
                
                if (showDebugInfo)
                    Debug.Log($"右手检测到: {hand.gesture_name}");
            }
        }
        
        // 控制手部模型的显示状态
        if (leftHandRoot != null)
            leftHandRoot.gameObject.SetActive(leftHandDetected);
        if (rightHandRoot != null)
            rightHandRoot.gameObject.SetActive(rightHandDetected);
    }
    
    /// <summary>
    /// 左手检测回调
    /// </summary>
    void OnLeftHandDetected(HandGestureUDPReceiver.HandData leftHand)
    {
        if (showDebugInfo)
            Debug.Log($"左手手势: {leftHand.gesture_name} (ID: {leftHand.gesture_id})");
    }
    
    /// <summary>
    /// 右手检测回调
    /// </summary>
    void OnRightHandDetected(HandGestureUDPReceiver.HandData rightHand)
    {
        if (showDebugInfo)
            Debug.Log($"右手手势: {rightHand.gesture_name} (ID: {rightHand.gesture_id})");
    }
    
    /// <summary>
    /// 更新手部目标位置
    /// </summary>
    void UpdateHandTargetPositions(HandGestureUDPReceiver.HandData hand, Vector3[] targetPositions)
    {
        if (hand.unityLandmarks == null || hand.unityLandmarks.Length != 21)
            return;
        
        for (int i = 0; i < 21; i++)
        {
            // 转换坐标系统并应用缩放
            Vector3 landmark = hand.unityLandmarks[i];
            
            // 将归一化坐标转换为世界坐标
            targetPositions[i] = new Vector3(
                (landmark.x - 0.5f) * positionScale,
                (landmark.y - 0.5f) * positionScale,
                landmark.z * positionScale
            );
        }
    }
    
    /// <summary>
    /// 更新手部骨骼（简化版本）
    /// </summary>
    void UpdateHandSkeleton(HandBones handBones, Vector3[] targetPositions, Vector3[] currentPositions)
    {
        if (handBones == null) return;
        
        // 平滑更新所有关键点位置
        for (int i = 0; i < 21; i++)
        {
            currentPositions[i] = Vector3.Lerp(
                currentPositions[i], 
                targetPositions[i], 
                smoothingFactor
            );
        }
        
        // 更新手掌位置
        if (handBones.palm != null)
        {
            handBones.palm.localPosition = currentPositions[0];
        }
        
        // 更新每个手指链
        FingerChain[] fingerChains = handBones.GetFingerChains();
        foreach (var chain in fingerChains)
        {
            UpdateFingerChain(chain, currentPositions);
        }
    }
    

    
    /// <summary>
    /// 更新手指链（简化版本）
    /// </summary>
    void UpdateFingerChain(FingerChain chain, Vector3[] positions)
    {
        if (chain == null || chain.bones == null || chain.landmarkIndices == null) return;
        
        // 确保数组长度匹配
        int length = Mathf.Min(chain.bones.Length, chain.landmarkIndices.Length);
        
        for (int i = 0; i < length; i++)
        {
            if (chain.bones[i] != null)
            {
                // 设置骨骼位置
                int landmarkIndex = chain.landmarkIndices[i];
                if (landmarkIndex < positions.Length)
                {
                    if (useIK && i > 0)
                    {
                        // 使用IK方式：让当前骨骼指向下一个关键点
                        if (i < length - 1)
                        {
                            int nextLandmarkIndex = chain.landmarkIndices[i + 1];
                            if (nextLandmarkIndex < positions.Length)
                            {
                                Vector3 direction = (positions[nextLandmarkIndex] - positions[landmarkIndex]).normalized;
                                if (direction != Vector3.zero)
                                {
                                    chain.bones[i].rotation = Quaternion.LookRotation(direction, Vector3.up);
                                }
                            }
                        }
                        chain.bones[i].position = transform.TransformPoint(positions[landmarkIndex]);
                    }
                    else
                    {
                        // 直接设置位置
                        chain.bones[i].localPosition = positions[landmarkIndex];
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 获取当前手势信息
    /// </summary>
    public string GetCurrentGestureInfo()
    {
        if (gestureReceiver == null) return "无手势数据";
        
        string info = "当前手势:\n";
        
        var leftHand = gestureReceiver.GetLeftHand();
        if (leftHand != null)
        {
            info += $"左手: {leftHand.gesture_name} (ID: {leftHand.gesture_id})\n";
        }
        
        var rightHand = gestureReceiver.GetRightHand();
        if (rightHand != null)
        {
            info += $"右手: {rightHand.gesture_name} (ID: {rightHand.gesture_id})\n";
        }
        
        return info;
    }
    
    /// <summary>
    /// 重置手部位置
    /// </summary>
    public void ResetHandPositions()
    {
        InitializePositionArrays();
        
        if (leftHandRoot != null)
            leftHandRoot.gameObject.SetActive(false);
        if (rightHandRoot != null)
            rightHandRoot.gameObject.SetActive(false);
    }
    
    // Gizmos绘制用于调试
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // 绘制左手关键点
        if (leftHandDetected && enableLeftHand)
        {
            DrawHandGizmos(leftHandCurrentPositions, Color.blue);
        }
        
        // 绘制右手关键点
        if (rightHandDetected && enableRightHand)
        {
            DrawHandGizmos(rightHandCurrentPositions, Color.red);
        }
    }
    
    void DrawHandGizmos(Vector3[] positions, Color color)
    {
        Gizmos.color = color;
        
        for (int i = 0; i < positions.Length; i++)
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(positions[i]), 0.01f);
        }
        
        // 绘制手指连线
        DrawFingerConnections(positions);
    }
    
    void DrawFingerConnections(Vector3[] positions)
    {
        // 简化的手指连线 - 只连接手掌到近端、近端到远端、远端到指尖
        // 拇指连线
        DrawFingerLine(positions, new int[] { 0, 2, 3, 4 });
        // 食指连线
        DrawFingerLine(positions, new int[] { 0, 6, 7, 8 });
        // 中指连线
        DrawFingerLine(positions, new int[] { 0, 10, 11, 12 });
        // 无名指连线
        DrawFingerLine(positions, new int[] { 0, 14, 15, 16 });
        // 小指连线
        DrawFingerLine(positions, new int[] { 0, 18, 19, 20 });
    }
    
    void DrawFingerLine(Vector3[] positions, int[] indices)
    {
        for (int i = 0; i < indices.Length - 1; i++)
        {
            Vector3 start = transform.TransformPoint(positions[indices[i]]);
            Vector3 end = transform.TransformPoint(positions[indices[i + 1]]);
            Gizmos.DrawLine(start, end);
        }
    }
}