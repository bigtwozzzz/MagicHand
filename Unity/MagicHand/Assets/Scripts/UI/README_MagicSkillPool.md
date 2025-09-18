# 魔法技能池系统

## 概述

魔法技能池系统是对现有魔法UI系统的扩展，实现了魔法的解锁机制。系统将所有魔法分为两类：
- **已解锁魔法**：可以设置到UI栏位并触发的魔法
- **魔法池**：尚未解锁，无法使用的魔法

## 核心功能

### 1. 魔法解锁系统
- 魔法必须先解锁才能设置到UI栏位
- 默认解锁魔法24（光束魔法）和魔法23（治疗魔法）
- 提供`UnlockMagic(int magicId)`方法解锁指定魔法

### 2. 魔法触发检查
- 在`MagicManager.TryTriggerMagic()`中添加解锁状态检查
- 未解锁的魔法无法通过手势或其他方式触发
- 确保只有已解锁的魔法才能生效

### 3. UI栏位管理
- 新增`SetMagicToSlotWithUnlockCheck()`方法，带解锁检查的栏位设置
- 支持强制设置模式（`forceSet`参数），用于系统初始化
- 保持原有`SetMagicToSlot()`方法的兼容性

## API 接口

### 魔法解锁相关
```csharp
// 解锁指定魔法
public bool UnlockMagic(int magicId)

// 检查魔法是否已解锁
public bool IsMagicUnlocked(int magicId)

// 获取已解锁魔法列表
public List<int> GetUnlockedMagics()

// 获取魔法池中的魔法列表
public List<int> GetMagicPool()
```

### 栏位设置相关
```csharp
// 带解锁检查的栏位设置
public bool SetMagicToSlotWithUnlockCheck(int slotIndex, int magicId, bool forceSet = false)

// 原有的栏位设置方法（保持兼容）
public void SetMagicToSlot(int slotIndex, int magicId)
```

## 使用示例

### 解锁魔法并设置到栏位
```csharp
// 获取MagicUIController实例
MagicUIController magicUI = FindObjectOfType<MagicUIController>();

// 解锁魔法32（流星）
if (magicUI.UnlockMagic(32))
{
    // 解锁成功，设置到栏位1
    magicUI.SetMagicToSlotWithUnlockCheck(1, 32);
}

// 检查魔法是否已解锁
if (magicUI.IsMagicUnlocked(32))
{
    Debug.Log("魔法32已解锁");
}
```

### 获取魔法状态信息
```csharp
// 获取已解锁的魔法列表
List<int> unlockedMagics = magicUI.GetUnlockedMagics();
Debug.Log($"已解锁魔法: {string.Join(", ", unlockedMagics)}");

// 获取魔法池中的魔法
List<int> magicPool = magicUI.GetMagicPool();
Debug.Log($"魔法池: {string.Join(", ", magicPool)}");
```

## 测试功能

系统提供了两个测试方法，可在Inspector中通过右键菜单调用：

1. **测试魔法栏位**：演示解锁魔法并设置到栏位的流程
2. **测试解锁魔法**：演示魔法解锁功能和状态查询

## 系统集成

### 初始化流程
1. `MagicUIController.Awake()`中调用`InitializeMagicPool()`
2. 从`MagicConfigLoader`获取所有可用魔法
3. 将启用的魔法添加到魔法池
4. 默认解锁魔法24和23

### 魔法触发流程
1. 手势识别触发`MagicManager.TryTriggerMagic()`
2. 检查魔法是否已解锁（新增检查）
3. 执行原有的其他检查（暂停、死亡、配置、启用、冷却）
4. 触发魔法效果

## 注意事项

1. **向后兼容**：保持了原有API的兼容性，现有代码无需修改
2. **性能考虑**：使用`FindObjectOfType<MagicUIController>()`获取实例，建议在实际项目中使用单例模式优化
3. **数据持久化**：当前解锁状态仅在运行时保存，重启游戏后会重置
4. **扩展性**：系统设计支持未来添加更复杂的解锁条件和奖励机制

## 未来扩展

- 添加魔法解锁条件系统（等级、任务、道具等）
- 实现解锁状态的持久化存储
- 添加解锁动画和UI反馈
- 支持魔法升级和进阶系统