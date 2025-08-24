using UnityEngine;
using Leap.Unity;
using Leap.Unity.CameraHands;

namespace Leap.Unity.CameraHands
{
    /// <summary>
    /// 摄像头手部动捕系统设置助手
    /// 用于快速配置和管理摄像头手部跟踪系统
    /// </summary>
    public class CameraHandSetup : MonoBehaviour
    {
        [Header("System Components")]
        [Tooltip("摄像头手部提供器")]
        public CameraHandProvider cameraHandProvider;
        
        [Tooltip("手部模型管理器")]
        public HandModelManager handModelManager;
        
        [Tooltip("主摄像头（用于坐标转换）")]
        public Camera mainCamera;

        [Header("Auto Setup")]
        [Tooltip("启动时自动设置系统")]
        public bool autoSetupOnStart = true;
        
        [Tooltip("自动查找HandModelManager")]
        public bool autoFindHandModelManager = true;
        
        [Tooltip("自动查找主摄像头")]
        public bool autoFindMainCamera = true;

        [Header("Debug")]
        [Tooltip("显示调试信息")]
        public bool showDebugInfo = true;
        
        [Tooltip("在Scene视图中显示手部数据")]
        public bool showHandGizmos = true;

        // 系统状态
        private bool isSystemReady = false;
        private float lastDataTime = 0f;
        private const float DATA_TIMEOUT = 2f; // 2秒无数据则认为连接断开

        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupSystem();
            }
        }

        /// <summary>
        /// 设置整个摄像头手部跟踪系统
        /// </summary>
        [ContextMenu("Setup Camera Hand System")]
        public void SetupSystem()
        {
            Debug.Log("Setting up Camera Hand Tracking System...");
            
            // 1. 设置或查找CameraHandProvider
            SetupCameraHandProvider();
            
            // 2. 设置或查找HandModelManager
            SetupHandModelManager();
            
            // 3. 设置摄像头引用
            SetupCameraReference();
            
            // 4. 连接组件
            ConnectComponents();
            
            // 5. 验证设置
            ValidateSetup();
            
            Debug.Log("Camera Hand Tracking System setup completed!");
        }

        private void SetupCameraHandProvider()
        {
            if (cameraHandProvider == null)
            {
                cameraHandProvider = FindObjectOfType<CameraHandProvider>();
                
                if (cameraHandProvider == null)
                {
                    // 创建新的CameraHandProvider
                    GameObject providerGO = new GameObject("CameraHandProvider");
                    providerGO.transform.SetParent(transform);
                    cameraHandProvider = providerGO.AddComponent<CameraHandProvider>();
                    
                    Debug.Log("Created new CameraHandProvider");
                }
                else
                {
                    Debug.Log("Found existing CameraHandProvider");
                }
            }
            
            // 配置CameraHandProvider
            cameraHandProvider.enableDebugLog = showDebugInfo;
        }

        private void SetupHandModelManager()
        {
            if (handModelManager == null && autoFindHandModelManager)
            {
                handModelManager = FindObjectOfType<HandModelManager>();
                
                if (handModelManager != null)
                {
                    Debug.Log("Found existing HandModelManager");
                }
                else
                {
                    Debug.LogWarning("No HandModelManager found in scene. Please add one manually.");
                }
            }
        }

        private void SetupCameraReference()
        {
            if (mainCamera == null && autoFindMainCamera)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
                
                if (mainCamera != null)
                {
                    Debug.Log($"Found camera: {mainCamera.name}");
                }
            }
            
            // 设置摄像头变换到CameraHandProvider
            if (cameraHandProvider != null && mainCamera != null)
            {
                cameraHandProvider.cameraTransform = mainCamera.transform;
            }
        }

        private void ConnectComponents()
        {
            if (cameraHandProvider != null && handModelManager != null)
            {
                // 将CameraHandProvider设置为HandModelManager的LeapProvider
                handModelManager.leapProvider = cameraHandProvider;
                Debug.Log("Connected CameraHandProvider to HandModelManager");
            }
        }

        private void ValidateSetup()
        {
            isSystemReady = true;
            
            if (cameraHandProvider == null)
            {
                Debug.LogError("CameraHandProvider is missing!");
                isSystemReady = false;
            }
            
            if (handModelManager == null)
            {
                Debug.LogWarning("HandModelManager is missing. Hand models will not be displayed.");
            }
            
            if (mainCamera == null)
            {
                Debug.LogWarning("Main camera is missing. Using default coordinate system.");
            }
            
            if (isSystemReady)
            {
                Debug.Log("✓ Camera Hand Tracking System is ready!");
            }
        }

        void Update()
        {
            if (!isSystemReady || cameraHandProvider == null)
                return;
                
            // 检查数据连接状态
            if (cameraHandProvider.HasHandData())
            {
                lastDataTime = Time.time;
            }
            
            // 显示调试信息
            if (showDebugInfo)
            {
                UpdateDebugInfo();
            }
        }

        private void UpdateDebugInfo()
        {
            bool hasConnection = (Time.time - lastDataTime) < DATA_TIMEOUT;
            
            if (!hasConnection && Time.time > DATA_TIMEOUT)
            {
                Debug.LogWarning("No hand data received. Check Python script and UDP connection.");
            }
        }

        /// <summary>
        /// 重置系统
        /// </summary>
        [ContextMenu("Reset System")]
        public void ResetSystem()
        {
            if (cameraHandProvider != null)
            {
                cameraHandProvider.ResetProvider();
            }
            
            lastDataTime = 0f;
            Debug.Log("Camera Hand Tracking System reset");
        }

        /// <summary>
        /// 获取系统状态信息
        /// </summary>
        public string GetSystemStatus()
        {
            if (!isSystemReady)
                return "System not ready";
                
            bool hasConnection = (Time.time - lastDataTime) < DATA_TIMEOUT;
            int handCount = cameraHandProvider?.GetHandCount() ?? 0;
            
            return $"Connection: {(hasConnection ? "Active" : "Inactive")}, Hands: {handCount}";
        }

        /// <summary>
        /// 获取当前手势信息
        /// </summary>
        public void LogCurrentGestures()
        {
            if (cameraHandProvider == null || !cameraHandProvider.HasHandData())
            {
                Debug.Log("No hand data available");
                return;
            }
            
            string leftGesture = cameraHandProvider.GetCurrentGesture(true);
            string rightGesture = cameraHandProvider.GetCurrentGesture(false);
            
            Debug.Log($"Left hand: {leftGesture}, Right hand: {rightGesture}");
        }

        // GUI显示
        void OnGUI()
        {
            if (!showDebugInfo || !isSystemReady)
                return;
                
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Camera Hand Tracking", EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
            GUILayout.Label($"Status: {GetSystemStatus()}");
            
            if (cameraHandProvider != null)
            {
                GUILayout.Label($"Hands detected: {cameraHandProvider.GetHandCount()}");
            }
            
            if (GUILayout.Button("Log Gestures"))
            {
                LogCurrentGestures();
            }
            
            if (GUILayout.Button("Reset System"))
            {
                ResetSystem();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        // Scene视图中的调试绘制
        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!showHandGizmos || cameraHandProvider == null)
                return;
                
            var frame = cameraHandProvider.CurrentFrame;
            if (frame?.Hands == null)
                return;
                
            foreach (var hand in frame.Hands)
            {
                // 设置颜色
                Gizmos.color = hand.IsLeft ? Color.blue : Color.red;
                
                // 绘制手掌
                Gizmos.DrawWireSphere(hand.PalmPosition, hand.PalmWidth * 0.5f);
                
                // 绘制手指
                if (hand.Fingers != null)
                {
                    foreach (var finger in hand.Fingers)
                    {
                        Gizmos.color = finger.IsExtended ? Color.green : Color.gray;
                        Gizmos.DrawWireSphere(finger.TipPosition, 0.01f);
                        
                        // 绘制手指骨骼
                        if (finger.bones != null)
                        {
                            foreach (var bone in finger.bones)
                            {
                                Gizmos.DrawLine(bone.PrevJoint, bone.NextJoint);
                            }
                        }
                    }
                }
                
                // 绘制手臂
                if (hand.Arm != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(hand.Arm.ElbowPosition, hand.Arm.WristPosition);
                }
            }
        }
        #endif
    }
}