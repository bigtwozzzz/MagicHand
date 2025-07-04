# MediaPipe手势识别系统

基于MediaPipe的实时手势识别系统，支持双手检测，通过UDP协议向Unity发送手部关键点坐标、手势编号和左右手标识。

## 系统概述

本系统包含以下组件：

1. **Python手势识别端** (`mediapipe_gesture_recognition.py`)
   - 使用MediaPipe进行实时手部检测和关键点提取
   - 基于手指角度和位置关系进行手势识别
   - 通过UDP协议发送数据到Unity

2. **Unity接收端** (`Unity_UDP_Receiver.cs`)
   - 接收UDP数据并解析JSON格式的手势信息
   - 提供事件系统用于响应手势变化
   - 支持坐标系转换和数据处理

3. **Unity使用示例** (`Unity_Gesture_Example.cs`)
   - 展示如何使用UDP接收器
   - 提供手势响应的示例代码
   - 包含手部关键点可视化功能

4. **手势参考表** (`gesture_reference_table.md`)
   - 详细的手势编号映射表
   - 数据格式说明
   - 使用指南

## 支持的手势

系统支持16种不同的手势，每种手势都有对应的编号：

| 编号 | 手势名称 | 描述 |
|------|----------|------|
| 0 | unknown | 未知手势 |
| 1 | Wave | 挥手 |
| 2 | Stop | 停止手势 |
| 3 | Handshake | 握手 |
| 4 | Heart_single | 单手比心 |
| 5 | like | 点赞 |
| 6 | dislike | 点踩 |
| 7 | ILY | 我爱你 |
| 8 | Insult | 侮辱手势 |
| 9 | one | 数字一 |
| 10 | fist | 拳头 |
| 11 | peace | 胜利手势 |
| 12 | call | 打电话 |
| 13 | ok | OK手势 |
| 14 | four | 数字四 |
| 15 | three | 数字三 |
| 16 | three2 | 数字三(变体) |

## 安装和配置

### Python端要求

```bash
pip install opencv-python
pip install mediapipe
pip install numpy
```

### Unity端要求

1. Unity 2019.4 或更高版本
2. Newtonsoft.Json包（通过Package Manager安装）

## 使用方法

### 1. 启动Python手势识别

```bash
cd /path/to/project
python mediapipe_gesture_recognition.py
```

程序启动后会显示：
- 摄像头画面
- 手部关键点标注
- 实时手势识别结果
- FPS信息
- UDP发送状态

按 'q' 键退出程序。

### 2. Unity端配置

#### 步骤1：导入脚本
将以下脚本导入Unity项目：
- `Unity_UDP_Receiver.cs`
- `Unity_Gesture_Example.cs`

#### 步骤2：创建接收器
1. 在场景中创建一个空的GameObject
2. 将 `HandGestureUDPReceiver` 脚本挂载到该对象上
3. 配置UDP端口（默认12345）

#### 步骤3：创建示例控制器（可选）
1. 创建另一个GameObject
2. 将 `GestureExample` 脚本挂载到该对象上
3. 在Inspector中关联 `gestureReceiver` 引用

#### 步骤4：创建主线程调度器
系统会自动创建 `UnityMainThreadDispatcher`，无需手动配置。

### 3. 网络配置

默认配置：
- IP地址：127.0.0.1（本地回环）
- 端口：12345

如需修改，请同时更新Python和Unity端的配置。

## 数据格式

### UDP发送的JSON数据结构

```json
{
  "hands": [
    {
      "hand_side": "Left",
      "gesture_name": "peace",
      "gesture_id": 11,
      "landmarks": [
        [0.5, 0.5, 0.0],  // 关键点0：手腕
        [0.52, 0.48, -0.01], // 关键点1：拇指根部
        // ... 共21个关键点
      ]
    }
  ],
  "timestamp": 1234567890.123
}
```

### 关键点索引

MediaPipe手部模型提供21个关键点：

```
0: 手腕 (WRIST)
1-4: 拇指 (THUMB_CMC, THUMB_MCP, THUMB_IP, THUMB_TIP)
5-8: 食指 (INDEX_FINGER_MCP, INDEX_FINGER_PIP, INDEX_FINGER_DIP, INDEX_FINGER_TIP)
9-12: 中指 (MIDDLE_FINGER_MCP, MIDDLE_FINGER_PIP, MIDDLE_FINGER_DIP, MIDDLE_FINGER_TIP)
13-16: 无名指 (RING_FINGER_MCP, RING_FINGER_PIP, RING_FINGER_DIP, RING_FINGER_TIP)
17-20: 小拇指 (PINKY_MCP, PINKY_PIP, PINKY_DIP, PINKY_TIP)
```

## Unity中的使用示例

### 基本手势检测

```csharp
public class MyGestureHandler : MonoBehaviour
{
    public HandGestureUDPReceiver gestureReceiver;
    
    void Start()
    {
        gestureReceiver.OnLeftHandGesture += OnLeftHandGesture;
        gestureReceiver.OnRightHandGesture += OnRightHandGesture;
    }
    
    void OnLeftHandGesture(HandGestureUDPReceiver.HandData leftHand)
    {
        if (leftHand.gesture_id == 11) // Peace手势
        {
            Debug.Log("左手做出胜利手势！");
        }
    }
    
    void OnRightHandGesture(HandGestureUDPReceiver.HandData rightHand)
    {
        if (rightHand.gesture_name == "fist")
        {
            Debug.Log("右手握拳！");
        }
    }
}
```

### 获取手部关键点

```csharp
void Update()
{
    var leftHand = gestureReceiver.GetLeftHand();
    if (leftHand != null)
    {
        // 获取左手食指尖端位置（关键点8）
        Vector3 indexTip = leftHand.unityLandmarks[8];
        
        // 将归一化坐标转换为世界坐标
        Vector3 worldPos = new Vector3(
            (indexTip.x - 0.5f) * 10f,
            (indexTip.y - 0.5f) * 10f,
            indexTip.z * 10f
        );
        
        // 移动某个对象到食指位置
        someObject.transform.position = worldPos;
    }
}
```

## 性能优化建议

1. **摄像头分辨率**：根据需要调整分辨率，较低分辨率可提高性能
2. **检测置信度**：适当提高置信度阈值可减少误检测
3. **UDP频率控制**：如果不需要极高频率，可以添加发送间隔
4. **Unity中的数据处理**：避免在每帧都进行复杂计算

## 故障排除

### 常见问题

1. **摄像头无法打开**
   - 检查摄像头是否被其他程序占用
   - 确认摄像头权限设置
   - 尝试更改摄像头索引（cv2.VideoCapture(1)）

2. **Unity接收不到数据**
   - 检查防火墙设置
   - 确认IP地址和端口配置
   - 查看Unity Console中的错误信息

3. **手势识别不准确**
   - 确保光线充足
   - 保持手部在摄像头视野内
   - 避免背景干扰
   - 调整检测置信度参数

4. **性能问题**
   - 降低摄像头分辨率
   - 减少最大检测手数
   - 优化Unity中的数据处理逻辑

### 调试信息

Python端会在控制台输出：
- 手势识别结果
- UDP发送状态
- 错误信息

Unity端可以启用调试模式查看：
- 接收到的数据
- 解析状态
- 手势变化事件

## 扩展开发

### 添加新手势

1. 在Python端的 `h_gesture()` 函数中添加新的判断逻辑
2. 在 `GESTURE_MAPPING` 字典中添加新的编号映射
3. 更新手势参考表
4. 在Unity端添加对应的处理逻辑

### 自定义数据格式

可以修改 `send_hand_data_to_unity()` 函数来发送额外的数据，如：
- 手部旋转角度
- 手势置信度
- 自定义特征值

### 多设备支持

通过修改UDP配置，可以支持：
- 多台设备同时发送数据
- 网络环境下的远程通信
- 多个Unity客户端接收数据

## 许可证

本项目基于MediaPipe开源框架开发，请遵循相应的开源许可证。

## 联系信息

如有问题或建议，请通过以下方式联系：
- 项目仓库：[GitHub链接]
- 邮箱：[联系邮箱]

---

**注意**：本系统仅供学习和研究使用，商业使用请确保符合相关许可证要求。