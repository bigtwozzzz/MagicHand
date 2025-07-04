using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unity端UDP接收器，用于接收Python手势识别数据
/// 使用方法：将此脚本挂载到GameObject上即可自动开始接收数据
/// </summary>
public class HandGestureUDPReceiver : MonoBehaviour
{
    [Header("UDP配置")]
    public int udpPort = 12345;
    public bool showDebugInfo = true;
    
    [Header("手势数据")]
    public List<HandData> currentHands = new List<HandData>();
    
    private UdpClient udpClient;
    private Thread udpThread;
    private bool isReceiving = false;
    
    // 手势数据结构
    [System.Serializable]
    public class HandData
    {
        public string hand_side;        // "Left" 或 "Right"
        public string gesture_name;     // 手势名称
        public int gesture_id;          // 手势编号
        public List<List<float>> landmarks; // 21个关键点坐标
        
        // Unity中使用的转换后的关键点
        [System.NonSerialized]
        public Vector3[] unityLandmarks = new Vector3[21];
    }
    
    [System.Serializable]
    public class ReceivedData
    {
        public List<HandData> hands;
        public double timestamp;
    }
    
    // 手势编号到名称的映射
    private readonly Dictionary<int, string> gestureNames = new Dictionary<int, string>()
    {
        {0, "unknown"}, {1, "Wave"}, {2, "Stop"}, {3, "Handshake"}, {4, "Heart_single"},
        {5, "like"}, {6, "dislike"}, {7, "ILY"}, {8, "Insult"}, {9, "one"},
        {10, "fist"}, {11, "peace"}, {12, "call"}, {13, "ok"}, {14, "four"},
        {15, "three"}, {16, "three2"}
    };
    
    // 事件：当接收到新的手势数据时触发
    public System.Action<List<HandData>> OnHandDataReceived;
    public System.Action<HandData> OnLeftHandGesture;
    public System.Action<HandData> OnRightHandGesture;
    
    void Start()
    {
        // 确保UnityMainThreadDispatcher在主线程中初始化
        UnityMainThreadDispatcher.Instance();
        
        StartUDPReceiver();
    }
    
    void StartUDPReceiver()
    {
        try
        {
            udpClient = new UdpClient(udpPort);
            isReceiving = true;
            
            udpThread = new Thread(new ThreadStart(ReceiveUDPData))
            {
                IsBackground = true
            };
            udpThread.Start();
            
            Debug.Log($"UDP接收器已启动，监听端口: {udpPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"启动UDP接收器失败: {e.Message}");
        }
    }
    
    void ReceiveUDPData()
    {
        while (isReceiving)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string jsonString = Encoding.UTF8.GetString(data);
                
                // 在主线程中处理数据
                UnityMainThreadDispatcher.Instance().Enqueue(() => ProcessReceivedData(jsonString));
            }
            catch (Exception e)
            {
                if (isReceiving)
                {
                    Debug.LogError($"UDP接收错误: {e.Message}");
                }
            }
        }
    }
    
    void ProcessReceivedData(string jsonString)
    {
        try
        {
            ReceivedData receivedData = JsonUtility.FromJson<ReceivedData>(jsonString);
            
            if (receivedData?.hands != null)
            {
                // 更新当前手势数据
                currentHands.Clear();
                currentHands.AddRange(receivedData.hands);
                
                // 转换坐标系统并触发事件
                foreach (var hand in currentHands)
                {
                    ConvertLandmarksToUnity(hand);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"{hand.hand_side}手检测到手势: {hand.gesture_name} (ID: {hand.gesture_id})");
                    }
                    
                    // 触发对应的事件
                    if (hand.hand_side == "Left")
                        OnLeftHandGesture?.Invoke(hand);
                    else if (hand.hand_side == "Right")
                        OnRightHandGesture?.Invoke(hand);
                }
                
                // 触发总体事件
                OnHandDataReceived?.Invoke(currentHands);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析手势数据失败: {e.Message}\nJSON: {jsonString}");
        }
    }
    
    void ConvertLandmarksToUnity(HandData hand)
    {
        if (hand.landmarks != null && hand.landmarks.Count == 21)
        {
            for (int i = 0; i < 21; i++)
            {
                if (hand.landmarks[i].Count >= 3)
                {
                    // 将归一化坐标转换为Unity坐标系统
                    // 注意：可能需要根据实际需求调整坐标转换
                    float x = hand.landmarks[i][0]; // 0-1范围
                    float y = 1.0f - hand.landmarks[i][1]; // 翻转Y轴
                    float z = hand.landmarks[i][2]; // 深度信息
                    
                    hand.unityLandmarks[i] = new Vector3(x, y, z);
                }
            }
        }
    }
    
    // 获取特定手势的手部数据
    public HandData GetHandByGesture(int gestureId)
    {
        return currentHands.Find(hand => hand.gesture_id == gestureId);
    }
    
    public HandData GetHandByGesture(string gestureName)
    {
        return currentHands.Find(hand => hand.gesture_name == gestureName);
    }
    
    // 获取左手或右手数据
    public HandData GetLeftHand()
    {
        return currentHands.Find(hand => hand.hand_side == "Left");
    }
    
    public HandData GetRightHand()
    {
        return currentHands.Find(hand => hand.hand_side == "Right");
    }
    
    // 检查是否检测到特定手势
    public bool IsGestureDetected(int gestureId)
    {
        return currentHands.Exists(hand => hand.gesture_id == gestureId);
    }
    
    public bool IsGestureDetected(string gestureName)
    {
        return currentHands.Exists(hand => hand.gesture_name == gestureName);
    }
    
    void OnDestroy()
    {
        StopUDPReceiver();
    }
    
    void OnApplicationQuit()
    {
        StopUDPReceiver();
    }
    
    void StopUDPReceiver()
    {
        isReceiving = false;
        
        if (udpThread != null && udpThread.IsAlive)
        {
            udpThread.Abort();
        }
        
        if (udpClient != null)
        {
            udpClient.Close();
        }
        
        Debug.Log("UDP接收器已停止");
    }
    
    // GUI显示当前手势信息（仅在编辑器中显示）
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"UDP端口: {udpPort}", new GUIStyle(GUI.skin.label) { fontSize = 14 });
        GUILayout.Label($"检测到手部数量: {currentHands.Count}", new GUIStyle(GUI.skin.label) { fontSize = 14 });
        
        foreach (var hand in currentHands)
        {
            GUILayout.Label($"{hand.hand_side}手: {hand.gesture_name} (ID:{hand.gesture_id})", 
                           new GUIStyle(GUI.skin.label) { fontSize = 12 });
        }
        GUILayout.EndArea();
    }
}

/// <summary>
/// 主线程调度器，用于在主线程中执行UDP接收到的数据处理
/// 需要在场景中创建一个GameObject并挂载此脚本
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<System.Action> _executionQueue = new Queue<System.Action>();
    private static bool _initialized = false;
    
    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null && !_initialized)
        {
            // 标记为已初始化，避免重复创建
            _initialized = true;
            
            // 在主线程中创建实例
            if (UnityEngine.Application.isPlaying)
            {
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }
        return _instance;
    }
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public void Enqueue(System.Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
    
    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}