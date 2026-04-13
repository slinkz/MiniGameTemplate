# Phase 2 用户验收清单

> 日期：2026-04-12 | 验收范围：事件与扩展性
> 前置条件：Phase 0 ✅ + Phase 1 ✅ 已通过验收

---

## 一、编译验证（阻塞项）

| # | 检查项 | 验收方式 | 状态 |
|---|--------|----------|------|
| C-01 | Unity batchmode 编译 0 errors, 0 warnings | Agent 已验证 | ✅ 通过 |
| C-02 | 编辑器打开工程无编译错误 | 用户打开 Unity 确认 Console | ✅ 通过 |
| C-03 | 现有 Demo 场景可正常进入 | Play → 场景加载无报错 | ✅ 通过 |

---

## 二、新增文件完整性

| # | 文件 | 行数 | 验收方式 | 状态 |
|---|------|------|----------|------|
| F-01 | `DanmakuSystem.Runtime.cs` | 94 | 确认文件存在 + partial class 编译通过 | ✅ |
| F-02 | `DanmakuSystem.API.cs` | 261 | 确认文件存在 + API 方法可调用 | ✅ |
| F-03 | `DanmakuSystem.UpdatePipeline.cs` | 78 | 确认文件存在 + Update 管线正常运行 | ✅ |
| F-04 | `CollisionEventBuffer.cs` | 97 | 确认 `CollisionEvent` struct + `CollisionEventBuffer` class | ✅ |
| F-05 | `MotionRegistry.cs` | 62 | 确认 `MotionStrategy` 委托 + 静态注册表 | ✅ |
| F-06 | `DefaultMotionStrategy.cs` | 84 | 确认默认运动策略 | ✅ |
| F-07 | `SineWaveMotionStrategy.cs` | 76 | 确认正弦波运动策略 | ✅ |
| F-08 | `SpiralMotionStrategy.cs` | 73 | 确认螺旋运动策略 | ✅ |
| F-09 | `IDanmakuEffectsBridge.cs` | 27 | 确认接口定义 | ✅ |
| F-10 | `DefaultDanmakuEffectsBridge.cs` | 59 | 确认默认桥接实现 | ✅ |

---

## 三、架构约束验证

| # | 约束（来自 REFACTOR_PLAN 执行约束） | 验证结果 | 状态 |
|---|-------------------------------------|----------|------|
| A-01 | `CollisionEventBuffer` 仅用于旁路消费（不承载伤害/击退/死亡主逻辑） | ✅ 主逻辑仍通过 `ICollisionTarget.OnBulletHit/OnLaserHit/OnSprayHit` 回调 | ✅ |
| A-02 | Buffer 溢出只影响 VFX/飘字（不影响 CollisionResult 统计） | ✅ TryWrite 返回 false 时仅递增 `_overflowCount` | ✅ |
| A-03 | `MotionRegistry` 为受控注册表，不做运行时开放注册 | ✅ `Initialize()` 仅注册 3 种内置策略，无 public Register 方法 | ✅ |
| A-04 | 新增运动类型无需修改 `BulletMover` 核心热路径 | ✅ `BulletMover` 只调用 `MotionRegistry.Get(type.MotionType)` | ✅ |
| A-05 | DanmakuSystem 保留单一 MonoBehaviour 入口 | ✅ `partial class` 拆分，Unity 只看到一个 Component | ✅ |
| A-06 | DanmakuSystem 不直接引用 VFX 命名空间（仅通过桥接） | ⚠️ Facade 仍持有 `_hitVfxSystem` / `_hitVfxType` 序列化字段（DEV-002，Phase 3 待迁移） | ⚠️ DEV-002 |
| A-07 | `DefaultDanmakuEffectsBridge` 是唯一引用 `MiniGameTemplate.VFX` 的运行时类 | ✅ 已确认 | ✅ |
| A-08 | `CollisionEventBuffer` 默认容量 256 固化在 `DanmakuWorldConfig.CollisionEventBufferCapacity` | ✅ | ✅ |
| A-09 | 2.6（Facade 拆分）与 2.7（VFX 桥接）同步执行，无中间态硬耦合 | ✅ 同一团队同步完成 | ✅ |

---

## 四、功能验证（需用户在 Unity 编辑器中操作）

| # | 功能 | 验收步骤 | 预期结果 | 状态 |
|---|------|----------|----------|------|
| V-01 | 弹丸碰撞触发命中 VFX | Play Demo → 弹丸命中目标 → 观察命中位置 | 出现 VFX 特效（通过 EffectsBridge） | ✅ 通过 |
| V-02 | 清屏 API | 运行时通过代码调用 `ClearAllBulletsWithEffect()` | 所有弹丸消失 + 每颗弹丸位置播放 VFX | ⏭️ 延后验证 |
| V-03 | 正弦波弹丸 | 创建 BulletTypeSO，MotionType=SineWave → 发射 | 弹丸沿正弦波轨迹飞行 | ⏭️ 延后验证 |
| V-04 | 螺旋弹丸 | 创建 BulletTypeSO，MotionType=Spiral → 发射 | 弹丸持续转向形成螺旋轨迹 | ⏭️ 延后验证 |
| V-05 | 默认弹丸（回归） | 现有 Default MotionType 弹丸行为不变 | 运动/碰撞/死亡与 Phase 1 一致 | ✅ 通过 |
| V-06 | 激光碰撞事件 | Play Demo → 激光命中目标 | Console 无报错 + VFX 触发 | ✅ 通过 |
| V-07 | 喷雾碰撞事件 | Play Demo → 喷雾命中目标 | Console 无报错 + VFX 触发 | ✅ 通过 |
| V-08 | ClearAll 回归 | 调用 `ClearAll()` | 所有弹丸/激光/喷雾/障碍物/挂载源/调度任务全部清除 | ✅ 通过 |

---

## 五、已知偏差与待办

| ID | 类型 | 描述 | 影响 | 处理 |
|----|------|------|------|------|
| DEV-001 | 已修复 | 旧 `CollisionSolver.Initialize(config)` 单参数重载未删除 | 编译歧义 | ✅ 已删除 |
| DEV-002 | Phase 3 待办 | Facade 仍持有 `_hitVfxSystem` / `_hitVfxType` 序列化字段 | 命名空间耦合（不影响运行） | Phase 3 迁移到桥接组件 |
| DEV-003 | Backlog | `CollisionEventBuffer.Reset()` 清零 overflow count（无累计统计） | 调试信息丢失 | 建议添加累计 counter |
| DEV-004 | Backlog | `CalculateModifierSpeed` 在 3 个策略类中重复 | 代码重复 ~15 行/处 | 建议提取共享 helper |

---

## 六、验收结论

- [x] **编译通过**：用户确认 Unity 编辑器无编译错误
- [x] **Demo 回归**：现有 Demo 场景行为不变
- [x] **新功能验证**：V-01 + V-05 通过，Demo 整体无异常
- [x] **Phase 2 正式关闭**

> 签字：踹门 日期：2026-04-12
