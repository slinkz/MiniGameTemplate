# 弹幕系统架构设计

> **预计阅读**：10 分钟 &nbsp;|&nbsp; **目标**：理解弹幕系统的整体架构、设计动机和各子文档定位

本文档是弹幕系统的总览入口。详细实现拆分为四个子文档，按需查阅。

---

## 为什么需要专门的弹幕系统？

微信小游戏运行在手机 WebGL 上，有三个致命约束：

| 约束 | 影响 |
|------|------|
| **Draw Call 预算极低**（50-80） | 几百颗弹幕不能每颗一个 DC |
| **单线程**（无 Job System / Burst） | 所有逻辑跑一个线程 |
| **Physics2D 极慢** | 大量 Collider 会卡死 |

解法：**弹幕不是 GameObject，是 struct 数组里的一行数据**。

---

## 系统总览

```
┌─────────────────────────────────────────────────────────────────────┐
│                          游戏逻辑层                                  │
│  BossAI / StageController / PlayerController                        │
└──────────┬──────────────────────┬────────────────────────────────────┘
           │ 引用 SO               │ 调用 API
┌──────────┴──────────────────────┴────────────────────────────────────┐
│                   ScriptableObject 资产层                             │
│  BulletTypeSO / LaserTypeSO / SprayTypeSO / ObstacleTypeSO             │
│  BulletPatternSO / PatternGroupSO / SpawnerProfileSO                 │
│  DanmakuWorldConfig / DanmakuRenderConfig / DanmakuTypeRegistry      │
│  DanmakuTimeScaleSO / DifficultyProfileSO                            │
└──────────┬───────────────────────────────────────────────────────────┘
           │ 驱动
┌──────────┴───────────────────────────────────────────────────────────┐
│                    DanmakuSystem（唯一 MonoBehaviour）                │
│  弹丸子系统: BulletWorld → BulletMover → CollisionSolver → Renderer  │
│  激光子系统: LaserPool + LaserUpdater                                │
│  喷雾子系统: SprayPool + SprayUpdater                                │
│  障碍物:     ObstaclePool                                            │
│  组合引擎:   PatternScheduler                                        │
│  特效:       TrailPool / DamageNumberSystem                         │
│  VFX桥接:   IDanmakuVFXRuntime → SpriteSheetVFXSystem（R4.0）        │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 关键设计约束

| 约束 | 说明 |
|------|------|
| **弹幕 = struct** | 预分配数组中的值类型数据，不是 GameObject |
| **零 GC** | 运行时无 new / List 扩容 / 装箱 |
| **7-11 Draw Call** | 弹幕+发光+飘字+拖尾+特效，远低于 50 DC 预算 |
| **自写碰撞** | 圆vs圆 / 线段vs圆 / 扇形vs圆 / 圆vsAABB，不用 Physics2D |
| **SO 驱动** | 弹幕类型、发射模式、碰撞响应、难度曲线全部 Inspector 可配 |
| **单一 Update** | 整个模块只有 `DanmakuSystem` 一个 MonoBehaviour |
| **阵营过滤** | Enemy/Player/Neutral 三阵营，碰撞最外层 early-out |
| **碰撞响应分离** | 对象/障碍物/屏幕边缘的行为分别配置 |

---

## 三种武器类型

| 类型 | 数据结构 | 池容量 | 碰撞 | 伤害模型 | 渲染 |
|------|---------|--------|------|---------|------|
| **弹丸** | BulletCore[] + Trail[] + Modifier[] | 2048 | 圆vs圆（网格分区） | 按碰撞响应 | Mesh 合批 |
| **激光** | LaserData[] | 16 | 线段vs圆 | 固定间隔 DPS | 拉伸四边形 |
| **喷雾** | SprayData[] | 8 | 扇形vs圆 | 固定间隔 DPS | ParticleSystem |

---

## 命名空间约定

统一 `MiniGameTemplate.Danmaku`。引用的框架类型：

| 类型 | 命名空间 |
|------|---------|
| `PoolDefinition` | `MiniGameTemplate.Pool` |
| `GameEvent` / `IntGameEvent` | `MiniGameTemplate.Events` |
| `AudioClipSO` | `MiniGameTemplate.Audio` |

---

## 推荐目录结构

```
Assets/_Framework/DanmakuSystem/
├── MODULE_README.md
├── Scripts/
│   ├── Data/           # BulletCore/Trail/Modifier, 枚举, Pool 容器
│   ├── Config/         # 全部 SO 定义
│   ├── Core/           # Mover, Spawner, Scheduler, Collision, Renderer
│   └── DanmakuSystem.cs
├── Shaders/            # DanmakuBullet（Alpha Blend）/ DanmakuLaser
└── Editor/             # 预览器 / 测试器 / 校验器 / 图集工具
```

---

## Shader 与 WebGL 兼容性

> **P1-4 决策**：目标 WebGL 2.0（GLES 3.0），可用 `discard`/`highp`，不用 Compute Shader。

| Shader | 功能 |
|--------|------|
| `DanmakuBullet` | 弹丸 / 伤害飘字 / VFX（Alpha Blend + 顶点色 + UV 动画） |
| `DanmakuLaser` | 激光 + 激光预警线（UV 滚动 + 边缘发光） |

> ADR-029 v2：Additive Blend 已移除，所有弹丸统一走 Alpha Blend。renderQueue 控制层序。

---

## 性能预算

| 系统 | 预算 |
|------|------|
| BulletMover（2048 颗） | ≤ 0.8ms |
| CollisionSolver（含障碍物 AABB） | ≤ 0.75ms |
| BulletRenderer | ≤ 1.5ms |
| GPU（7-11 DC） | ≤ 2.5ms |
| **总计** | **≤ 5.7ms（60fps 下 34%）** |

GC 预算：全部子系统每帧 0 bytes。

---

## 与框架集成

| SO 事件 | 发布者 | 监听者 |
|---------|--------|--------|
| `OnPlayerHit` | DanmakuSystem | PlayerHealth / VFXController / AudioManager |
| `OnDamageDealt` | DanmakuSystem | DamageNumberSystem |
| `OnBossPhaseChanged` | BossAI | ClearAllBullets + 切换 SpawnerProfile |

| 框架模块 | 使用方式 |
|---------|---------|
| EventSystem | 跨系统通信 |
| AudioSystem | 发射/命中音效 |
| TimerService | Boss 弹幕间隔 |
| FSM | Boss 阶段状态机 |
| ObjectPool | 重特效/TrailPool |

---

## 美术工具

| 工具 | 优先级 | 形态 |
|------|:---:|------|
| 弹丸预览器 | P0 | BulletTypeSO CustomEditor + Scene View |
| 弹幕模式测试器 | P1 | EditorWindow |
| 喷雾判定校验器 | P1 | SprayTypeSO CustomEditor |
| 图集打包工具 | P1 | EditorWindow / 菜单项 |

---

## 扩展预留

| 功能 | 扩展方式 | 难度 |
|------|---------|:---:|
| 擦弹判定 | BulletTypeSO 加 `GrazeRadius` | 低 |
| 清弹特效 | `ClearAllBulletsWithEffect()` + IDanmakuEffectsBridge | ✅ 已实现 |
| 弹幕录像回放 | 序列化发射指令重新模拟 | 中 |
| 多目标碰撞 | TargetRegistry + ICollisionTarget | ✅ 已实现 |
| 弹丸vs弹丸 | 双层网格分区 | 中 |
| Swept Circle | Speed > 12 启用射线检测 | 中 |
| 手机振动 | `wx.vibrateShort` via IWeChatBridge | 低 |
| GPU 粒子弹幕 | 等 WebGL + Compute Shader | 高 |

---

## 已确认的全部设计决策

| ID | 决策摘要 |
|----|---------|
| P0-1 | BulletModifier 冷数据分离 |
| P0-2 | 三字段延迟变速优先，SpeedOverLifetime 互斥 |
| P0-3 | 空洞遍历 + FLAG_ACTIVE 跳过 |
| P0-4 | ScheduleTask 索引查表，不持有 SO 引用 |
| P0-5 | SetPlayer 缓存 Transform+radius |
| P1-1 | 环引用仅 Awake DFS 检测 |
| P1-2 | BulletMover 顺便写 Trail |
| P1-3 | DamageNumber 环形缓冲区 128 |
| P1-4 | WebGL 2.0 (GLES 3.0) |
| P1-5 | PatternScheduler 64 槽 |
| P1-6 | AimAtPlayer 快照 vs FLAG_HOMING 实时追踪 |
| P1-7 | SpawnerProfileSO 和 PatternGroupSO 分层 |
| P1-8 | DifficultyProfileSO 混合模式 |
| P1-9 | ActiveCount 精确计数 |
| P1-10 | 复用框架 PoolDefinition |
| P2-5 | 子弹幕基准角 |

详细说明见各子文档中的对应章节。

---

## 子文档导航

| 文档 | 内容 |
|------|------|
| **[数据结构](DANMAKU_DATA.md)** | BulletCore/Trail/Modifier、枚举、CircleHitbox、激光/喷雾数据、数据容器、更新器 |
| **[SO 配置体系](DANMAKU_CONFIG.md)** | BulletTypeSO、LaserTypeSO、SprayTypeSO、PatternSO、PatternGroupSO、Scheduler、Spawner、配置拆分、TimeScale、难度 |
| **[渲染架构](DANMAKU_RENDERING.md)** | Mesh 上传优化、分层合批、旋转/排序、图集、拖尾、爆炸特效、伤害飘字 |
| **[碰撞与运行时](DANMAKU_COLLISION.md)** | 障碍物子系统、7 阶段碰撞、碰撞响应、Pierce 冷却、伤害模型、无敌帧、延迟变速、DanmakuSystem 入口 |
