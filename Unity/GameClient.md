# 游戏客户端说明文档

---

## 1. 事件中心（Event Center）
用于全局消息传输，所有信息（服务器消息、客户端事件、玩家操作等）均可通过该系统进行转发。

### 监听事件
```csharp
public void AddEventListener<T>(E_EventType name, UnityAction<T> action);
public void RemoveEventListener<T>(E_EventType name, UnityAction<T> action);
```

### 触发事件
```csharp
public void EventTrigger<T>(E_EventType name, T info);
```

---

## 2. 缓存池（Object Pool）
用于管理临时隐藏的 GameObject，实现对象复用以提升性能。

### 接口
```csharp
// 获取对象（异步加载）
public void GetObj(string name, UnityAction<GameObject> callBack);

// 回收对象（放入池中）
public void PushObj(string name, GameObject obj);
```

---

## 3. 公共 Mono 类（GlobalMono）
用于在非 MonoBehaviour 类中执行协程或 Update 操作（目前使用较少）。

---

## 4. 资源管理器（ResMgr）
封装了 Addressables 系统，统一管理资源加载。  
**资源路径规范**：`AddressableResources/文件名/`，命名格式为 `文件名/文件`（不包含后缀如 `.prefab`）。

### 接口
```csharp
// 同步加载资源
public T Load<T>(string name) where T : Object;

// 异步加载资源
public void LoadAsync<T>(string name, UnityAction<T> callback) where T : Object;
```

---

## 5. UI 管理器（UIMgr）
- 自动创建全局 `Canvas` 和 `EventSystem`。
- 统一管理除场景内特殊 UI 外的所有面板（不包含 3D UI）。
- UI 面板路径：`Resources/UI/Prefabs/UIName.prefab`

### 接口
```csharp
// 显示面板
UIMgr.GetInstance().ShowPanel<T>("UIName", E_UI_Layer.layer, (panel) =>
{
    // 可选：初始化逻辑
});

// 隐藏面板
UIMgr.GetInstance().HidePanel("UIName");
```

> **说明**：
> - `T` 为对应的 UI 脚本类（如 `LoginUI`）。
> - `E_UI_Layer` 可选层级：`bot`（底层）、`mid`（中层）、`top`（顶层）、`system`（系统层）。

---

## 6. 音效管理器（MusicMgr）
管理背景音乐与音效播放。  
**资源路径**：
- 背景音乐：`Resources/Music/BK/`
- 音效：`Resources/Music/Sound/`

### 接口
```csharp
/// <summary>
/// 播放背景音乐
/// </summary>
/// <param name="name">音乐文件名（不含路径和扩展名）</param>
public void PlayBkMusic(string name);

/// <summary>
/// 暂停背景音乐
/// </summary>
public void PauseBKMusic();

/// <summary>
/// 停止背景音乐
/// </summary>
public void StopBKMusic();

/// <summary>
/// 调整背景音乐音量
/// </summary>
/// <param name="v">音量值（0.0f ~ 1.0f）</param>
public void ChangeBKValue(float v);

/// <summary>
/// 播放音效
/// </summary>
/// <param name="name">音效文件名（不含路径和扩展名）</param>
/// <param name="isLoop">是否循环播放</param>
/// <param name="callBack">播放开始后的回调，传入 AudioSource</param>
public void PlaySound(string name, bool isLoop, UnityAction<AudioSource> callBack = null);

/// <summary>
/// 调整所有音效的音量
/// </summary>
/// <param name="value">音量值（0.0f ~ 1.0f）</param>
public void ChangeSoundValue(float value);

/// <summary>
/// 停止指定音效
/// </summary>
/// <param name="source">要停止的 AudioSource 对象</param>
public void StopSound(AudioSource source);
```

---

## 7. 场景管理器（SceneMgr）
确保场景加载在线程安全环境下进行，避免异常。

### 接口
```csharp
// 一般场景加载（可能异步）
public void LoadScene(string name, Action loadOverDo);

// 安全加载（强制主线程加载，避免跨线程错误）
public void SafeLoadScene(string name, Action loadOverDo);
```

---

## 8. 输入控制器（Input Controller）
负责接收手势识别设备输入，识别后将数据转发至 **事件中心** 进行分发。

---

## 9. 数据管理器（DataMgr）
用于本地存储和访问需要持久化或频繁使用的数据。

### 示例
```csharp
// 获取用户 ID
DataMgr.GetInstance().UserId;
```

> 可扩展用于保存配置、角色数据、游戏进度等。

---

## 10. 物体状态管理器（Object State Manager）
当前项目中暂未启用。

---

## 11. 通信处理器（Network Handler）
处理与服务端的网络通信流程，分为以下模块：

### 11.1 Gain.cs
接收客户端指令，并交由 Encoder 处理。
```csharp
private void HandlePlayerCommand(PlayerCommandData commandData);
```

### 11.2 Encoder.cs
负责将消息编码并发送至服务器。
```csharp
// 打包消息
public byte[] Pack(uint msgId, byte[] msgBody);

// 发送消息
public void Send(uint msgId, byte[] msgBody);
```

### 11.3 Decoder.cs
接收服务器数据，解码后分发给 Assign 处理。
```csharp
// 接收并解码网络消息
assign.DispatchNetworkEvent(msgId, msgBody);
```

### 11.4 Assign.cs
处理解码后的网络事件，进行具体逻辑分发。
```csharp
public void DispatchNetworkEvent(uint msgId, byte[] msgBody);
```

---

## 12. 手势识别端连接
（待补充）  
当前仅说明已接入，具体接口和协议尚未列出。

--- 

> ✅ **文档说明**：  
> 本说明旨在为开发人员提供清晰的架构与接口参考，建议结合代码注释与实际调用示例使用。