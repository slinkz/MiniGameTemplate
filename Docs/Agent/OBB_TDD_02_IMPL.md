---
system: obb-collision
scope: implementation-acceptance
last_verified: 2026-05-02
depends_on: [OBB_TDD_01_DESIGN]
related_code: Assets/_Framework/OBB/*.cs
---

## 4. 向后兼容性

| API | 变更类型 | 影响 |
|-----|----------|------|
| `ObstacleData` 字段 | 结构体重构 | 仅框架内部（CollisionSolver, LaserSegmentSolver, ObstaclePool） |
| `ObstaclePool.AddRect()` | 末尾加默认参数 | **无破坏**——现有 `AddRect(c, s, hp, fac)` 调用编译通过，`rotationRad` 默认 0f |
| `ObstaclePool.UpdatePosition()` | 保留 | **无破坏** |
| `ObstaclePool.UpdateTransform()` | 新增方法 | **无破坏** |
| `ObstacleRegistrar` | 新增 `RequireComponent(BoxCollider2D)` | 现有预制体需手动加 BoxCollider2D |
| `DanmakuEnums.CollisionTarget.Obstacle` | 注释更新 | 无运行时影响 |

**预制体迁移**：3 个现有预制体需要添加 BoxCollider2D（isTrigger=true），设置 size 匹配 SpriteRenderer。

---

## 5. 性能分析

### 5.1 运行时开销

| 操作 | AABB | OBB | 增量 |
|------|------|-----|------|
| 圆 vs 障碍物 | clamp(2) + distSq(3) | +逆旋转(4 mul + 2 add) | ~6 FLOPs |
| 射线 vs 障碍物 | slab(6) | +逆旋转 origin + dir(8 mul + 4 add) | ~12 FLOPs |
| 法线计算 | 2 sub + 2 abs + 1 cmp | +逆旋转 + 正旋转(8 mul + 4 add) | ~12 FLOPs |

最坏情况（2000 弹 × 64 障碍物 = 128K 次测试）：每次 +6 FLOPs ≈ +768K FLOPs/帧。
现代移动 CPU（Cortex-A76 级别）约 20 GFLOPS，增量 < 0.04ms。**可忽略。**

### 5.2 内存

+768 bytes total。**可忽略。**

### 5.3 预留优化空间（不在本轮）

- `UpdatePosition` 不重算 Sin/Cos（已内建：保持原旋转）
- 障碍物数量 >64 时，可引入粗糙 AABB 预筛（OBB 的外接 AABB 做快速剔除），但当前 64 上限不需要

---

## 6. 实施计划

| Phase | 内容 | 预估 | 可验证节点 |
|-------|------|------|-----------|
| P1 | 数据层：`ObstacleData` + `ObstaclePool` | 20 min | 编译 0 errors |
| P2 | 碰撞数学：新建 `ObstacleCollisionMath`、改 `CollisionSolver` + `LaserSegmentSolver`、删重复代码。**备注**：Framework asmdef 中添加 `[InternalsVisibleTo("MiniGameFramework.Tests.Editor")]` | 40 min | 编译 0 errors；旋转 0° 回归 |
| P3 | 注册层：`ObstacleRegistrar`（BoxCollider2D）+ `ObstacleSpawner`（Rotation 字段 + Gizmo） | 20 min | 编译 0 errors |
| P4 | 资产迁移 + 验证：预制体加 BoxCollider2D、Play Mode 测试（0°回归 + 45°旋转 + 激光折射） | 30 min | AC 全部通过 |

**预估总工时：~2 小时**

---

## 7. 验收标准 (AC)

| ID | 类别 | 验收条件 | 契约 | 状态 |
|----|------|----------|------|------|
| AC-01 | 回归 | 旋转 0° 的障碍物与升级前行为完全一致（弹丸反射方向、激光截断位置、喷雾遮挡） | BC-01 | ⬜ |
| AC-02 | 核心 | 旋转 45° 的障碍物正确阻挡弹丸，碰撞法线方向合理（弹丸反射方向正确） | BC-02 | ⬜ |
| AC-03 | 核心 | 旋转障碍物正确截断/折射激光 | BC-03 | ⬜ |
| AC-04 | 核心 | 旋转障碍物正确遮挡喷雾 | BC-04 | ⬜ |
| AC-05 | 编辑器 | Scene View 中 BoxCollider2D 绿色线框准确反映碰撞区域 | — | ⬜ |
| AC-06 | 编辑器 | 运行时修改 Transform.Rotation.Z，碰撞区域即时同步 | — | ⬜ |
| AC-07 | 兼容 | `UpdatePosition(index, center)` 只更新位置，旋转不变 | BC-06 | ⬜ |
| AC-08 | 兼容 | `AddCircle()` 正常工作（旋转 = 0） | BC-05 | ⬜ |
| AC-09 | 编译 | 0 errors / 0 warnings | — | ✅ 2026-04-23 MCP 验证通过 |
| AC-10 | 视觉 | 旋转障碍物被摧毁后视觉反馈正常（变灰半透明） | BC-07 | ⬜ |

---

## 8. 风险与缓解

| # | 风险 | 概率 | 影响 | 缓解 |
|---|------|------|------|------|
| R1 | 浮点精度导致旋转后碰撞边缘抖动 | 低 | 中 | 沿用 `1e-4f` 容差，与现有 Slab 代码一致 |
| R2 | BoxCollider2D 与 Unity Physics2D 冲突（意外触发物理碰撞） | 中 | 中 | 强制 `isTrigger = true` + 不添加 Rigidbody2D；`Reset()` 中自动设置 |
| R3 | `Collider.offset` 在有旋转时的世界坐标计算错误 | 中 | 高 | `offset` 需要随 Transform 旋转一起旋转到世界坐标（代码中使用 `RotateVector`） |
| R4 | 预制体迁移遗漏（未加 BoxCollider2D） | 低 | 低 | `RequireComponent` 编译期强制 |
| R5 | 扇形 vs OBB 角度检查用中心点近似，窄长 OBB 旋转时可能漏判 | 低 | 低 | 这是现有行为（AABB 也用中心点），不引入新的近似误差 |
| R6 | `lossyScale` 含负值（翻转 Sprite）导致 HalfExtents 为负 | 中 | 高 | 注册时取 `Mathf.Abs(lossyScale.x/.y)` |
| R7 | BoxCollider2D 的 Physics2D.autoSyncTransforms 隐性开销 | 低 | 低 | 64 个 Collider2D 开销可忽略；如项目未使用 Unity 内置物理，可设 `Physics2D.simulationMode = Script` (v1.2 新增) |

---

## 9. 文件变更清单

| 文件 | 操作 | 说明 |
|------|------|------|
| `_Framework/.../Data/ObstacleData.cs` | **修改** | 结构体字段：Min/Max → Center/HalfExtents/Rotation/Sin/Cos |
| `_Framework/.../Data/ObstaclePool.cs` | **修改** | AddRect 加 rotationRad 参数 + 新增 UpdateTransform |
| `_Framework/.../Core/ObstacleCollisionMath.cs` | **新建** | 共享 OBB 碰撞数学（CircleVsOBB, RayVsOBB, GetOBBNormal） |
| `_Framework/.../Core/CollisionSolver.cs` | **修改** | Phase 2/6 改调共享工具类 + 删除 ClampToAABB/GetAABBNormal |
| `_Framework/.../Core/LaserSegmentSolver.cs` | **修改** | 改调共享工具类 + 删除 RayVsAABB/GetAABBNormal |
| `_Framework/.../Data/DanmakuEnums.cs` | **修改** | CollisionTarget.Obstacle 注释 AABB→OBB |
| `_Example/.../ObstacleRegistrar.cs` | **修改** | +BoxCollider2D + 旋转传入 + Reset() |
| `_Example/.../ObstacleSpawner.cs` | **修改** | ObstacleDefinition +Rotation + Gizmo 旋转 |
| `_Example/.../Prefabs/Obstacle_*.prefab` (×3) | **修改** | 添加 BoxCollider2D(isTrigger=true) |

---

## 附录 A：代码重复消除

当前 `GetAABBNormal` 在以下两处存在 **100% 相同的副本**：
1. `CollisionSolver.cs` L691-701
2. `LaserSegmentSolver.cs` L424-434

`ClampToAABB` 仅在 `CollisionSolver.cs` L684-689 有一处。

本次升级将三者统一归入 `ObstacleCollisionMath`，消除重复。

## 附录 B：上下游调用链（无变更部分）

```
DanmakuSystem.Runtime.cs  → new ObstaclePool()           // 无变更
DanmakuSystem.API.cs      → ObstaclePool 属性暴露         // 无变更
DanmakuSystem.UpdatePipeline.cs → 传递 obstaclePool      // 无变更
LaserUpdater.cs           → 传递 obstaclePool             // 无变更
```

上游创建和传递 `ObstaclePool` 的代码不受影响，变更封装在 Pool 内部和碰撞算法层。

---

## 变更记录

| 版本 | 日期 | 变更 |
|------|------|------|
| v1.0 | 2026-04-23 | 初稿 |
| v1.1 | 2026-04-23 | 补充文件变更清单、附录 B |
| v1.2 | 2026-04-23 | PK Round 1 回应：CircleVsOBB 法线内联(OBB-001)、补充 AddRect/UpdateTransform/UpdatePosition 实现(OBB-002/005)、RotateVector 定义(OBB-003)、方法访问修饰符 internal(OBB-004)、内存对齐说明(OBB-006)、Phase 6 伪代码(OBB-007)、lossyScale Abs(OBB-008)、BC-03 前提假设(OBB-009)、Update 变化检测(OBB-010)、InternalsVisibleTo 备注(OBB-011)、注释澄清(OBB-012)、R7 Physics2D(OBB-014) |
| v1.3 | 2026-04-23 | PK Round 2 回应：新增 DistanceSqToOBB 封装方法(OBB-016)、Update worldCenter 完整计算(OBB-017)、ConeAngle 半角注释(OBB-018) |

## 遗留项

| 项 | 优先级 | 说明 |
|----|--------|------|
| 单元测试 | 中 | OBB 碰撞数学的关键角度自动化测试（0°/45°/90°/180°/270°），待项目测试基础设施建立后补充 (OBB-011) |
