using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.IO;

public class GestureDetectionLauncher : MonoBehaviour
{
    [Header("手势识别配置")]
    [SerializeField] private int autoMode = 0; // 自动模式间隔(毫秒)，0表示手动模式
    [SerializeField] private string modelPath = "work_dir/ResNet18/ResNet18_epoch_4_F1Score_0.94_loss_0.12.pth";
    [SerializeField] private int cameraId = 0;
    [SerializeField] private float confidenceThreshold = 0.5f;
    [SerializeField] private bool enableSignal = true; // 启用UDP信号发送到Unity
    [SerializeField] private bool enableWindow = true; // 启用实时检测窗口显示
    
    [Header("路径配置")]
    [SerializeField] private bool useRelativePath = true; // 使用相对路径
    [SerializeField] private bool useVirtualEnv = true; // 使用虚拟环境
    [SerializeField] private string pythonExecutable = "python"; // Python可执行文件路径（当不使用虚拟环境时）
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugOutput = true; // 启用调试信息输出
    [SerializeField] private bool suppressTensorFlowLogs = true; // 抑制TensorFlow日志
    
    private Process gestureProcess;
    private string scriptPath;
    private string pythonPath;
    
    void Start()
    {
        // 构建脚本路径和Python路径
        if (useRelativePath)
        {
            // Application.dataPath 指向 Assets 目录
            // 需要向上两级到达项目根目录: Assets -> Unity/MagicHand -> Unity -> 项目根目录
            string unityProjectRoot = Directory.GetParent(Application.dataPath).FullName; // Unity/MagicHand
            string unityRoot = Directory.GetParent(unityProjectRoot).FullName; // Unity
            string projectRoot = Directory.GetParent(unityRoot).FullName; // 项目根目录
            scriptPath = Path.Combine(projectRoot, "Gesture", "Hagrid", "realtime_gesture_detection.py");
            
            // 设置Python路径
            if (useVirtualEnv)
            {
                pythonPath = Path.Combine(projectRoot, "Gesture", "Hagrid", "gestures", "Scripts", "python.exe");
            }
            else
            {
                pythonPath = pythonExecutable;
            }
        }
        else
        {
            scriptPath = Path.Combine(Application.streamingAssetsPath, "realtime_gesture_detection.py");
            pythonPath = pythonExecutable;
        }
        
        StartGestureDetection();
    }
    
    public void StartGestureDetection()
    {
        if (gestureProcess != null && !gestureProcess.HasExited)
        {
            UnityEngine.Debug.LogWarning("手势识别进程已在运行中");
            return;
        }
        
        try
        {
            // 构建命令行参数
            List<string> args = new List<string>();
            args.Add($"\"{scriptPath}\"");
            args.Add($"--auto {autoMode}");
            args.Add($"--model_path \"{modelPath}\"");
            args.Add($"--camera_id {cameraId}");
            args.Add($"--confidence_threshold {confidenceThreshold}");
            
            if (enableSignal)
                args.Add("--signal");
            if (enableWindow)
                args.Add("--window");
            
            string arguments = string.Join(" ", args);
            
            // 创建进程
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = pythonPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            // 如果使用虚拟环境，设置工作目录
            if (useVirtualEnv && useRelativePath)
            {
                string unityProjectRoot = Directory.GetParent(Application.dataPath).FullName;
                string unityRoot = Directory.GetParent(unityProjectRoot).FullName;
                string projectRoot = Directory.GetParent(unityRoot).FullName;
                string hagridPath = Path.Combine(projectRoot, "Gesture", "Hagrid");
                startInfo.WorkingDirectory = hagridPath;
            }
            
            // 设置环境变量来抑制TensorFlow和其他库的日志
            if (suppressTensorFlowLogs)
            {
                startInfo.EnvironmentVariables["TF_CPP_MIN_LOG_LEVEL"] = "2"; // 只显示ERROR级别
                startInfo.EnvironmentVariables["GLOG_minloglevel"] = "2";     // 抑制Google日志
                startInfo.EnvironmentVariables["TF_ENABLE_ONEDNN_OPTS"] = "0"; // 禁用OneDNN优化信息
            }
            
            gestureProcess = new Process();
            gestureProcess.StartInfo = startInfo;
            gestureProcess.OutputDataReceived += OnOutputDataReceived;
            gestureProcess.ErrorDataReceived += OnErrorDataReceived;
            
            gestureProcess.Start();
            gestureProcess.BeginOutputReadLine();
            gestureProcess.BeginErrorReadLine();
            
            UnityEngine.Debug.Log($"手势识别进程已启动: {arguments}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"启动手势识别进程失败: {e.Message}");
        }
    }
    
    public void StopGestureDetection()
    {
        if (gestureProcess != null && !gestureProcess.HasExited)
        {
            try
            {
                gestureProcess.Kill();
                gestureProcess.WaitForExit(3000); // 等待3秒
                UnityEngine.Debug.Log("手势识别进程已停止");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"停止手势识别进程失败: {e.Message}");
            }
        }
    }
    
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data) && enableDebugOutput)
        {
            UnityEngine.Debug.Log($"[手势识别] {e.Data}");
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            // 错误信息始终显示，但可以过滤掉一些非关键警告
            if (enableDebugOutput || IsImportantError(e.Data))
            {
                UnityEngine.Debug.LogError($"[手势识别错误] {e.Data}");
            }
        }
    }
    
    private bool IsImportantError(string errorMessage)
    {
        // 过滤掉非关键的警告信息
        string lowerMessage = errorMessage.ToLower();
        
        // 这些是非关键的信息，可以忽略
        if (lowerMessage.Contains("info:") || 
            lowerMessage.Contains("created tensorflow lite") ||
            lowerMessage.Contains("all log messages before absl::initializelog") ||
            lowerMessage.Contains("feedback manager requires"))
        {
            return false;
        }
        
        // 其他错误信息认为是重要的
        return true;
    }
    
    void OnApplicationQuit()
    {
        StopGestureDetection();
    }
    
    void OnDestroy()
    {
        StopGestureDetection();
    }
    
    // 运行时调整参数的方法
    public void SetAutoMode(int value) { autoMode = value; }
    public void SetCameraId(int value) { cameraId = value; }
    public void SetConfidenceThreshold(float value) { confidenceThreshold = value; }
    public void SetEnableSignal(bool value) { enableSignal = value; }
    public void SetEnableWindow(bool value) { enableWindow = value; }
    
    // 调试控制方法
    public void SetEnableDebugOutput(bool value) 
    { 
        enableDebugOutput = value;
        UnityEngine.Debug.Log($"手势识别调试输出已{(value ? "启用" : "禁用")}");
    }
    
    public void SetSuppressTensorFlowLogs(bool value) 
    { 
        suppressTensorFlowLogs = value;
        UnityEngine.Debug.Log($"TensorFlow日志抑制已{(value ? "启用" : "禁用")}，重启进程后生效");
    }
    
    public void ToggleDebugOutput()
    {
        SetEnableDebugOutput(!enableDebugOutput);
    }
    
    public bool IsDebugOutputEnabled()
    {
        return enableDebugOutput;
    }
    
    // 重启手势识别（应用新参数）
    public void RestartGestureDetection()
    {
        StopGestureDetection();
        StartCoroutine(DelayedStart());
    }
    
    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);
        StartGestureDetection();
    }
}