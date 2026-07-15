---
system: knowledge-engineering
scope: module-card-danmaku-system
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/Danmaku_Rendering.md
---

# Module Card: DanmakuSystem

## 1. 模块职责

DanmakuSystem 负责高频弹幕逻辑：弹丸、激光、喷雾、Trail、弹幕调度、运动更新、碰撞检测、碰撞事件缓冲，以及与 VFX/飘字/渲染管线的桥接。

## 2. 不负责什么

- 不负责 Entity 的生命周期和 AI 决策。
- 不负责最终战斗胜负状态机。
- 不负责 FairyGUI UI 展示。
- 不直接承担 RuntimeAtlas 的全部实现细节，但会持有并驱动共享 Atlas/Renderer 管线。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `DanmakuSystem` | Facade、Update/LateUpdate 管线入口 |
| `BulletWorld` | 弹丸 SoA 数据容器 |
| `LaserPool`, `SprayPool`, `TrailPool` | 激光、喷雾、拖尾池 |
| `PatternScheduler` | 弹幕 Pattern 调度 |
| `BulletMover`, `LaserUpdater`, `SprayUpdater` | 运动更新 |
| `CollisionSolver` | 弹幕碰撞阶段处理 |
| `CollisionEventBuffer` | 碰撞事件缓存 |
| `BulletRenderer`, `LaserRenderer` | 渲染提交 |

## 4. 数据流

```text
BulletPatternSO / Fire API
  -> Spawner/Skill 调用 DanmakuSystem
  -> BulletWorld / LaserPool / SprayPool 分配槽位
  -> Update 管线更新运动、碰撞、事件
  -> EffectsBridge / VFX / DamageNumber 响应事件
  -> LateUpdate 管线重建 Mesh / RBM 上传绘制
  -> 超时、碰撞或越界后回收槽位
```

## 5. 生命周期

```text
WarmUp/Initialize -> Fire -> Active Update -> Collision -> Event Dispatch -> Render -> Recycle/Cleanup
```

退场或重试时必须清理所有弹幕池、调度器、事件缓冲、VFX 桥接和渲染状态。

## 6. 依赖关系

DanmakuSystem 依赖基础 Utils/Event/ObjectPool/Audio/Rendering/VFX 桥接。EntitySystem 和 ShooterGame 可调用 DanmakuSystem 发射弹幕，但 DanmakuSystem 不应直接依赖具体业务规则。

## 7. 关键 SO / 配置路径

```text
Assets/_Game/Configs/ShooterGame/BulletPattern/
Assets/_Game/Configs/ShooterGame/BulletType/
Assets/_Game/Configs/ShooterGame/LaserTypes/
Assets/_Game/Configs/_Template/BulletPattern/
Assets/_Game/Configs/_Template/BulletType/
```

常见 SO：`BulletTypeSO`、`BulletPatternSO`、`PatternGroupSO`、`SpawnerProfileSO`、`DifficultyProfileSO`、VFX 资源描述。

## 8. 关键 ADR

- ADR-006：DanmakuSystem 保留 Facade。
- ADR-012：阵营模型通用关系。
- ADR-016：Danmaku 到 VFX 桥接解耦。
- ADR-020：CollisionEventBuffer 溢出不影响主逻辑。
- ADR-028/031：RuntimeAtlas 统一管线深化。

## 9. 热路径 / 性能约束

- 弹幕更新、碰撞和渲染重建是高频热路径，必须零 GC。
- 使用池和 SoA 容器，不在帧内动态扩容。
- 碰撞事件缓冲溢出不能影响主逻辑安全。
- WebGL 图形 API 兼容性优先。

## 10. 常见错误

- 调整容量后忘记同步配置、统计和溢出策略。
- 修改碰撞响应后忘记 Entity 侧 Camp/TargetRegistry 语义。
- 只验证 DrawCall，不验证纹理像素和可见性。
- 清理战斗时漏掉 PatternScheduler 或 CollisionEventBuffer。
- 新弹型只创建代码，未创建 BulletType/Pattern SO 和 Atlas 资源。

## 11. 修改前必读

- `CONTEXT_PACKS/Danmaku_Rendering.md`
- `ARCHITECTURE.md` 中 DanmakuSystem 架构与统一渲染管线
- `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_03_DANMAKU.md`
- `DEBUG_PLAYBOOK.md`
- `SYSTEMS/ATLAS_TDD/ATLAS_TDD_INDEX.md`

## 12. 修改后必验

- 弹丸/激光/喷雾生成、移动、碰撞、回收正常。
- 退场和重试后无活跃弹幕残留。
- DrawCall、active count、bucket、Mesh、纹理链路可验证。
- 热路径 GC 为零或无新增分配。
- 新弹型在 Unity 和微信 WebGL 路径可见。
