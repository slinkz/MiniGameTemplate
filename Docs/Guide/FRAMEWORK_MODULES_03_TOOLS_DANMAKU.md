# 框架模块使用手册 — Part 3：工具 & 弹幕系统

> FSM · WeChatBridge · DebugTools · Utils · Editor 工具 · DanmakuSystem

---

## 9. FSM — 有限状态机

**用途**：管理游戏全局状态流转（菜单 → 游戏中 → 暂停 → 结束）。

**位置**：`Assets/_Framework/FSM/`

### 创建状态和转换

1. 右键 → Create → MiniGameTemplate → FSM → State，创建状态 SO
2. 右键 → Create → MiniGameTemplate → FSM → State Transition，创建转换规则
3. 在转换 SO 中配置 FromState 和 ToState

### 设置状态机

1. 给 GameObject 添加 `StateMachine` 组件
2. 在 Inspector 中设置 Initial State
3. 拖入所有 Valid Transitions

### 代码中切换状态

```csharp
[SerializeField] private StateMachine _gameFSM;
[SerializeField] private State _playingState;
[SerializeField] private State _menuState;

void StartGame()
{
    // 有转换验证——只有配置了对应转换规则才能成功
    bool success = _gameFSM.TransitionTo(_playingState);
}

void ResetToMenu()
{
    // 强制切换，跳过验证（用于重置/重启）
    _gameFSM.ForceTransitionTo(_menuState);
}
```

### 监听状态变化

```csharp
_gameFSM.OnStateChanged += (previousState, newState) => {
    Debug.Log($"State changed: {previousState?.name} → {newState.name}");
};
```

### 状态事件

每个 State SO 可以配置 OnEnter 和 OnExit 事件（GameEvent）。进入/离开状态时自动 Raise，方便在 Inspector 中配置响应。

---

## 10. WeChatBridge — 微信 SDK 桥接

> 📐 完整接口清单、构建配置表、隐私授权流程图参见 [Agent/WECHAT_INTEGRATION.md](../Agent/WECHAT_INTEGRATION.md)。

**用途**：统一微信小游戏 SDK 接口层。模板默认提供：
- Editor / 非 WebGL：`WeChatBridgeStub`
- WebGL：`WeChatBridgeWebGL`（广告能力已落地）

**位置**：`Assets/_Framework/WeChatBridge/`

### 使用方式

```csharp
// 启动时注入广告位（推荐在 GameStartupFlow 调用）
WeChatBridgeFactory.SetAdUnitIds(rewardedId, bannerId, interstitialId);

// 获取桥接实例（工厂自动选择实现）
var wx = WeChatBridgeFactory.Create();

// 广告
wx.PreloadRewardedAd();
wx.ShowRewardedAd(success => {
    if (success) GiveReward();
});
wx.ShowBannerAd();
wx.HideBannerAd();
wx.ShowInterstitialAd();

// 分享
wx.Share("我的小游戏", imageUrl, "score=100");
```

### 桩实现行为

在 Editor 中运行时，`WeChatBridgeStub` 模拟所有 SDK 调用：
- 广告回调延迟 1.5 秒后返回 true
- 登录回调延迟 0.5 秒后返回模拟 code
- 隐私授权首次返回 `needAuthorize=true`，`RequirePrivacyAuthorize` 后标记为已授权

### WebGL 行为

- WebGL + 微信环境 + 已配置广告位：走 jslib 真实广告调用
- WebGL 但非微信环境 / 广告位为空：自动回退桩行为（保证可运行）

---

## 11. DebugTools — 调试工具

**用途**：运行时调试辅助。Release 构建中自动禁用。

**位置**：`Assets/_Framework/DebugTools/`

| 工具 | 用途 | 激活方式 |
|------|------|----------|
| `FPSDisplay` | 左上角帧率显示 | 挂到场景任意 GameObject |
| `RuntimeSOViewer` | 查看 SO 变量实时值 | 仅 Editor |
| `DebugConsole` | 简易运行时控制台 | 多指点击/摇一摇 |

所有调试代码包裹在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 中，Release 构建零开销。

---

## 12. Utils — 通用工具

**位置**：`Assets/_Framework/Utils/`

| 工具 | 用途 | 注意事项 |
|------|------|----------|
| `Singleton<T>` | MonoBehaviour 单例基类 | **仅限框架内部使用**，游戏代码禁用 |
| `GameLog` | 日志工具 | Release 构建自动剥离（`[Conditional]` 编译） |
| `CoroutineRunner` | 为非 MonoBehaviour 类提供协程能力 | 框架内部设施 |
| `MathUtils` | 数学工具方法 | 通用 |

### GameLog 使用

```csharp
// ✅ 日常调试——Release 中自动消失（包括字符串拼接的开销）
GameLog.Log("[MySystem] Something happened");
GameLog.LogWarning("[MySystem] Something suspicious");

// ✅ 致命错误——Release 中仍然可见
Debug.LogError("[MySystem] FATAL: Initialization failed");
```

---

## 13. Editor 工具

**位置**：`Assets/_Framework/Editor/`

通过 Unity 菜单 `Tools → MiniGame Template` 访问：

| 工具 | 菜单位置 | 用途 |
|------|----------|------|
| **架构验证** | Validate → Architecture Check | 检查代码是否违反架构规范 |
| **资源审计** | Validate → Asset Audit | 检查纹理尺寸、音频格式等 |
| **SO 创建向导** | Create → SO Creation Wizard | 可视化界面创建各种 SO 资产 |
| **一键构建** | Build → Build WebGL (Release) | 自动配置 PlayerSettings 并构建 |
| **打开构建目录** | Build → Open Build Folder | 定位构建输出 |
| **SO 运行时调试** | Debug → SO Runtime Viewer | 查看 SO 变量当前值 |

### 资源导入规范自动化

模板包含 `AssetImportEnforcer`（AssetPostprocessor），自动执行以下规则：

| 资源类型 | 自动处理 |
|----------|----------|
| 纹理 | WebGL 平台最大 1024px |
| 音频 | WebGL 平台强制 Vorbis 压缩、50% 质量、短音效强制 Mono |

你不需要手动设置这些，导入资源时自动应用。

---

## 14. DanmakuSystem（弹幕系统）

> 位置：`Assets/_Framework/DanmakuSystem/`
>
> 📐 **详细文档**已拆分为专题页面，按需查阅：
> | 文档 | 内容 |
> |------|------|
> | [DANMAKU_SYSTEM.md](DANMAKU_SYSTEM.md) | 系统总览、架构图、设计决策汇总 |
> | [DANMAKU_DATA.md](DANMAKU_DATA.md) | 数据结构（BulletCore/Trail/Modifier、Laser/Spray/Obstacle） |
> | [DANMAKU_CONFIG.md](DANMAKU_CONFIG.md) | SO 配置体系（12 种 SO、发射模式、难度系统） |
> | [DANMAKU_RENDERING.md](DANMAKU_RENDERING.md) | 渲染管线（双 Mesh、拖尾、爆炸、飘字） |
> | [DANMAKU_COLLISION.md](DANMAKU_COLLISION.md) | 碰撞系统（7 阶段、障碍物、运行时入口） |

纯数据驱动弹幕系统，专为微信小游戏 WebGL 优化。支持弹丸/激光/喷雾三种武器类型 + 障碍物交互，零 GC 分配。

### 架构特点

- **SoA 三层分离**：BulletCore(36B) + BulletTrail(28B) + BulletModifier(16B)
- **预分配池**：所有容器启动时预分配，运行时零 new/GC
- **BatchRenderer 渲染**：相同图集合并 DrawCall，每帧单次 `SetVertexBufferData`
- **7 阶段碰撞**：弹丸/激光/喷雾 × 目标/障碍物/边缘
- **碰撞响应**：Die / ReduceHP / Pierce / BounceBack / Reflect / RecycleOnDistance
- **激光折射**：`LaserSegmentSolver` 射线 vs AABB，支持 Block/Pierce/Reflect
- **挂载跟踪**：激光/喷雾可挂载到 Transform，每帧自动同步

### 容量配置

| 类型 | 默认上限 | 容器 |
|------|----------|------|
| 弹丸 | 2048 | BulletWorld |
| 激光 | 16 | LaserPool |
| 喷雾 | 8 | SprayPool |
| 障碍物 | 64 | ObstaclePool |
| 挂载源 | 24 | AttachSourceRegistry |
| 调度任务 | 64 | PatternScheduler |
| 伤害飘字 | 128 | DamageNumberSystem |
| 拖尾曲线 | 64 | TrailPool |

### 快速接入

```csharp
// 设置玩家
DanmakuSystem.Instance.SetPlayer(playerTransform, 0.2f);

// 发射弹幕
DanmakuSystem.Instance.FireBullets(patternSO, spawnPosition, angleDeg);

// 发射弹幕组合
DanmakuSystem.Instance.FireGroup(groupSO, spawnPosition, angleDeg);

// 清场
DanmakuSystem.Instance.ClearAll();
```

### 激光 API

```csharp
// Detached 模式（固定位置）
int laserIdx = DanmakuSystem.Instance.FireLaser(
    typeIndex, origin, angle, length: 10f, lifetime: 0f);

// Attached 模式（跟随 Transform）
int laserIdx = DanmakuSystem.Instance.FireLaser(
    typeIndex, bossGunTransform, length: 10f, lifetime: 5f,
    localOffset: new Vector2(0, 0.5f), angleOffset: 0f);
```

### 喷雾 API

```csharp
// Detached 模式
int sprayIdx = DanmakuSystem.Instance.FireSpray(
    typeIndex, origin, direction,
    coneAngle: 30f, range: 5f, lifetime: 3f);

// Attached 模式
int sprayIdx = DanmakuSystem.Instance.FireSpray(
    typeIndex, bossTransform,
    coneAngle: 30f, range: 5f, lifetime: 3f,
    localOffset: default, angleOffset: 0f);
```

### SO 配置体系

> 📐 所有 SO 类型完整清单参见 [Agent/SO_WORKFLOWS_INDEX](../Agent/SO_WORKFLOWS_INDEX.md)。

| SO | 说明 |
|----|------|
| `BulletTypeSO` | 弹丸视觉类型 |
| `LaserTypeSO` | 激光类型 |
| `SprayTypeSO` | 喷雾类型 |
| `ObstacleTypeSO` | 障碍物类型 |
| `BulletPatternSO` | 弹幕发射模式 |
| `PatternGroupSO` | 弹幕组合编排 |
| `SpawnerProfileSO` | 发射器配置 |
| `DifficultyProfileSO` | 难度乘数 |
| `DanmakuWorldConfig` | 世界配置 |
| `DanmakuRenderConfig` | 渲染配置 |
| `DanmakuTypeRegistry` | 类型注册表 |
| `DanmakuTimeScaleSO` | 时间缩放 |

### 性能预算（60fps）

| 子系统 | 预算 |
|--------|------|
| BulletMover | ≤ 1.5ms |
| CollisionSolver（7 阶段） | ≤ 1.5ms |
| BulletRenderer | ≤ 1.5ms |
| 其他子系统 | ≤ 1.2ms |
| **总计** | **≤ 5.7ms** |
