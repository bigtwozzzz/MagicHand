## 游戏客户端说明
### 事件中心: 消息传输
* 监听事件:
``` csharp
public void AddEventListener<T>(E_EventType name, UnityAction<T> action);
public void RemoveEventListener<T>(E_EventType name, UnityAction<T> action);
```
* 事件触发： 
``` csharp
public void EventTrigger<T>(E_EventType name, T info);
```
### 缓存池：用于隐藏暂时用不到的物体
* 隐藏物体：
``` csharp
public void GetObj(string name, UnityAction<GameObject> callBack);
public void PushObj(string name, GameObject obj);
```
### 公共mono类：用于处理非Mono类的协程调用，暂时没怎么用到

### 资源管理器：这个地方在考虑到底自己写一个资源池，还是用Addressables
* 同步加载资源：
``` csharp
public T Load<T>(string name) where T : Object
```
* 异步加载资源：
``` csharp
public void LoadAsync<T>(string name, UnityAction<T> callback) where T : Object
```
### Ui管理器：
* 会先全局创建一个Canvas和EventSystem,然后通过UIMgr去调用全局UI,除场景内等特殊UI以外其他面板等都由UIMgr去管理
* 加载UI：路径：Resources/UI/Prefabs/UIName.prefab
``` csharp
UIMgr.GetInstance().ShowPanel<LoginUI>("UIName", E_UI_Layer.(可选：bot/mid/top/system), (panel) =>
{
    // 可选：面板创建完成后做一些初始化
});
```
### 音效管理器：暂时没用到
### 场景管理器：有些时候只能用主线程加载，否则会出错
* 加载场景：
```csharp
public void LoadScene(string name, Action loadOverDo);
```
* 主线程加载场景：
```csharp
public void SafeLoadScene(string name, Action loadOverDo);
```
### 输入控制器：手势识别端定义
### 数据管理器：本地保存一些需要传输和多次调用的数据
* 用户获取：
```csharp
DataMgr.GetInstance().UserId;
```
......
### 物体状态管理器：暂时没用到
### 通信处理器：
* 服务端连接：
```csharp
### Gain.cs 获取客户端指令给Encoder处理
private void HandlePlayerCommand(PlayerCommandData commandData);
### Encoder.cs 编码打包发送至服务器
public byte[] Pack(uint msgId, byte[] msgBody);
public void Send(uint msgId, byte[] msgBody);
### Decoder.cs 接收服务器指令并解码到Assign处理
assign.DispatchNetworkEvent(msgId, msgBody);
### Assign.cs 处理收到的指令
public void DispatchNetworkEvent(uint msgId, byte[] msgBody)
```

* 手势识别端连接：
...