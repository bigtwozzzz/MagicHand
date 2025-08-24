using System.Collections;
using UnityEngine;
using Leap;
using Leap.Unity.CameraHands;

namespace Leap.Unity.CameraHands
{
    /// <summary>
    /// 摄像头手部数据提供器，继承LeapProvider以兼容现有的LeapMotion架构
    /// 通过UDP接收MediaPipe处理的手部数据并转换为Leap格式
    /// </summary>
    public class CameraHandProvider : LeapProvider
    {
        [Header("Camera Hand Provider Settings")]
        [Tooltip("摄像头变换，用于坐标转换")]
        public Transform cameraTransform;
        
        [Tooltip("是否启用调试日志")]
        public bool enableDebugLog = false;
        
        [Tooltip("数据平滑强度 (0-1)")]
        [Range(0f, 1f)]
        public float smoothingFactor = 0.3f;
        
        [Tooltip("最大手部跟踪数量")]
        public int maxHands = 2;

        // UDP接收器组件
        private UDPHandDataReceiver udpReceiver;
        
        // 当前帧数据
        private Frame _currentFrame;
        private Frame _currentFixedFrame;
        
        // 数据平滑
        private Frame _previousFrame;
        private Coroutine _frameUpdateCoroutine;
        
        // 帧率控制
        private float _lastUpdateTime;
        private const float MIN_UPDATE_INTERVAL = 1f / 60f; // 最大60FPS

        public override Frame CurrentFrame => _currentFrame ?? new Frame();
        public override Frame CurrentFixedFrame => _currentFixedFrame ?? new Frame();

        protected virtual void Awake()
        {
            // 初始化帧数据
            _currentFrame = new Frame();
            _currentFixedFrame = new Frame();
            _previousFrame = new Frame();
            
            // 如果没有指定摄像头变换，使用当前对象的变换
            if (cameraTransform == null)
            {
                cameraTransform = transform;
            }
        }

        protected virtual void Start()
        {
            // 获取或添加UDP接收器组件
            udpReceiver = GetComponent<UDPHandDataReceiver>();
            if (udpReceiver == null)
            {
                udpReceiver = gameObject.AddComponent<UDPHandDataReceiver>();
            }
            
            // 订阅手部数据接收事件
            udpReceiver.OnHandDataReceived += OnHandDataReceived;
            udpReceiver.enableDebugLog = enableDebugLog;
            
            // 启动帧更新协程
            _frameUpdateCoroutine = StartCoroutine(FrameUpdateCoroutine());
            
            if (enableDebugLog)
            {
                Debug.Log("CameraHandProvider started successfully");
            }
        }

        protected virtual void OnDestroy()
        {
            // 取消订阅事件
            if (udpReceiver != null)
            {
                udpReceiver.OnHandDataReceived -= OnHandDataReceived;
            }
            
            // 停止协程
            if (_frameUpdateCoroutine != null)
            {
                StopCoroutine(_frameUpdateCoroutine);
            }
        }

        /// <summary>
        /// 处理接收到的手部数据
        /// </summary>
        private void OnHandDataReceived(HandData[] handsData)
        {
            if (handsData == null || handsData.Length == 0)
            {
                // 如果没有手部数据，创建空帧
                UpdateCurrentFrame(new Frame());
                return;
            }
            
            // 限制手部数量
            int handCount = Mathf.Min(handsData.Length, maxHands);
            HandData[] limitedHandsData = new HandData[handCount];
            System.Array.Copy(handsData, limitedHandsData, handCount);
            
            // 转换为Leap格式
            Frame newFrame = MediaPipeToLeapConverter.ConvertToFrame(limitedHandsData, cameraTransform);
            
            // 应用数据平滑
            if (smoothingFactor > 0f && _previousFrame != null && _previousFrame.Hands.Count > 0)
            {
                newFrame = ApplySmoothing(newFrame, _previousFrame, smoothingFactor);
            }
            
            // 更新当前帧
            UpdateCurrentFrame(newFrame);
            
            if (enableDebugLog)
            {
                Debug.Log($"Updated frame with {newFrame.Hands.Count} hands");
            }
        }

        /// <summary>
        /// 更新当前帧数据
        /// </summary>
        private void UpdateCurrentFrame(Frame newFrame)
        {
            // 保存前一帧用于平滑
            _previousFrame = _currentFrame;
            
            // 更新当前帧
            _currentFrame = newFrame;
            _currentFixedFrame = newFrame; // 简化处理，使用相同的帧
            
            _lastUpdateTime = Time.time;
        }

        /// <summary>
        /// 帧更新协程，定期触发帧事件
        /// </summary>
        private IEnumerator FrameUpdateCoroutine()
        {
            while (true)
            {
                // 触发更新帧事件
                DispatchUpdateFrameEvent(_currentFrame);
                
                yield return new WaitForSeconds(MIN_UPDATE_INTERVAL);
            }
        }

        /// <summary>
        /// 在FixedUpdate中触发固定帧事件
        /// </summary>
        protected virtual void FixedUpdate()
        {
            DispatchFixedFrameEvent(_currentFixedFrame);
        }

        /// <summary>
        /// 应用数据平滑
        /// </summary>
        private Frame ApplySmoothing(Frame currentFrame, Frame previousFrame, float smoothing)
        {
            if (currentFrame.Hands.Count == 0 || previousFrame.Hands.Count == 0)
            {
                return currentFrame;
            }
            
            // 简化的平滑处理：对手掌位置进行平滑
            for (int i = 0; i < currentFrame.Hands.Count && i < previousFrame.Hands.Count; i++)
            {
                var currentHand = currentFrame.Hands[i];
                var previousHand = previousFrame.Hands[i];
                
                // 平滑手掌位置
                currentHand.PalmPosition = Vector3.Lerp(currentHand.PalmPosition, previousHand.PalmPosition, smoothing);
                
                // 平滑手指位置
                if (currentHand.Fingers != null && previousHand.Fingers != null)
                {
                    for (int j = 0; j < currentHand.Fingers.Count && j < previousHand.Fingers.Count; j++)
                    {
                        currentHand.Fingers[j].TipPosition = Vector3.Lerp(
                            currentHand.Fingers[j].TipPosition,
                            previousHand.Fingers[j].TipPosition,
                            smoothing
                        );
                    }
                }
            }
            
            return currentFrame;
        }

        /// <summary>
        /// 获取手势信息（扩展功能）
        /// </summary>
        public string GetCurrentGesture(bool isLeft = true)
        {
            if (_currentFrame?.Hands == null || _currentFrame.Hands.Count == 0)
            {
                return "unknown";
            }
            
            // 查找对应的手
            foreach (var hand in _currentFrame.Hands)
            {
                if (hand.IsLeft == isLeft)
                {
                    // 这里可以根据需要返回手势信息
                    // 目前返回简单的手指伸展状态
                    int extendedFingers = 0;
                    if (hand.Fingers != null)
                    {
                        foreach (var finger in hand.Fingers)
                        {
                            if (finger.IsExtended)
                            {
                                extendedFingers++;
                            }
                        }
                    }
                    return $"{extendedFingers}_fingers_extended";
                }
            }
            
            return "unknown";
        }

        /// <summary>
        /// 检查是否有手部数据
        /// </summary>
        public bool HasHandData()
        {
            return _currentFrame?.Hands != null && _currentFrame.Hands.Count > 0;
        }

        /// <summary>
        /// 获取手部数量
        /// </summary>
        public int GetHandCount()
        {
            return _currentFrame?.Hands?.Count ?? 0;
        }

        /// <summary>
        /// 重置提供器状态
        /// </summary>
        public void ResetProvider()
        {
            _currentFrame = new Frame();
            _currentFixedFrame = new Frame();
            _previousFrame = new Frame();
            
            if (enableDebugLog)
            {
                Debug.Log("CameraHandProvider reset");
            }
        }

        // 编辑器中的调试信息
        #if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!enableDebugLog || _currentFrame?.Hands == null)
                return;
                
            // 绘制手部调试信息
            Gizmos.color = Color.green;
            foreach (var hand in _currentFrame.Hands)
            {
                // 绘制手掌位置
                Gizmos.DrawWireSphere(hand.PalmPosition, 0.02f);
                
                // 绘制手指位置
                if (hand.Fingers != null)
                {
                    Gizmos.color = hand.IsLeft ? Color.blue : Color.red;
                    foreach (var finger in hand.Fingers)
                    {
                        Gizmos.DrawWireSphere(finger.TipPosition, 0.01f);
                    }
                }
            }
        }
        #endif
    }
}