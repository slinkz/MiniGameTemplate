# 跨场景数据传递规范

> 归档自 coding-standards SKILL.md §9，按需加载。

## 唯一正确路径：AppFlowNavigator + IFlowData

```csharp
// ✅ 正确：通过导航框架传递数据
var data = new BattleLevelData { LevelIndex = levelIndex };
await AppFlowNavigator.Instance.PushAsync(battleNode, data);

// 接收端：IFlowHandler.OnFlowEnter(IFlowData data)
public void OnFlowEnter(IFlowData data)
{
    if (data is BattleLevelData battleData)
        _controller.SetLaunchContext(battleData.LevelIndex);
}
```

## 禁止的跨场景传参方式

| 禁止方式 | 为什么不行 |
|----------|-----------|
| ❌ SO 写运行时值当全局变量 | SO 是项目级资产，Play Mode 下修改不持久化；多入口时状态残留 |
| ❌ static 字段传参 | 无生命周期管理，热重载丢失，测试困难 |
| ❌ Resources.Load 在运行时读 SO 当通信 | 本质是文件 IO 伪装通信，与 Addressables/YooAsset 冲突 |
| ❌ PlayerPrefs 传临时数据 | 持久化到磁盘，性能差，无类型安全 |
| ❌ DontDestroyOnLoad 当消息总线 | 生命周期不可控，场景重载不重置 |
| ❌ AssetDatabase.LoadAssetAtPath | Editor-only，真机直接崩 |

## 直跑场景 = 测试模式

直接运行一个场景（不经导航框架）时，`IFlowData` 为空。此时：
- 控制器应有**安全的 fallback 行为**（如使用 Inspector 配置的默认值）
- **禁止写入存档/进度**（`_launchLevelIndex == null` → 不写 ProgressManager）
- 这是调试通道，不是正式游戏流程
