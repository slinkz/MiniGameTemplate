# OBB 障碍物升级变更包

> **日期**: 2026-04-23  
> **关联 TDD**: `docs/Agent/OBB_OBSTACLE_TDD.md` v1.3  
> **状态**: 编码完成，待 Play Mode 验收

## 变更概述

将弹幕系统的障碍物碰撞从 AABB（轴对齐包围盒）升级为 OBB（有向包围盒），支持任意 2D Z 轴旋转。

## 核心变更

### 新增文件
| 文件 | 说明 |
|------|------|
| `_Framework/.../Core/ObstacleCollisionMath.cs` | OBB 碰撞数学共享工具类（CircleVsOBB, RayVsOBB, GetOBBNormal, DistanceSqToOBB） |

### 修改文件
| 文件 | 变更说明 |
|------|----------|
| `_Framework/.../Data/ObstacleData.cs` | 结构体重构：Min/Max → Center/HalfExtents/RotationRad/Sin/Cos (24B→36B) |
| `_Framework/.../Data/ObstaclePool.cs` | AddRect 末尾加 rotationRad 参数 + 新增 UpdateTransform + 保留 UpdatePosition |
| `_Framework/.../Core/CollisionSolver.cs` | Phase 2 圆vsOBB + Phase 6 扇形vsOBB + 删除 ClampToAABB/GetAABBNormal |
| `_Framework/.../Core/LaserSegmentSolver.cs` | 射线vsOBB + 删除 RayVsAABB/GetAABBNormal |
| `_Framework/.../Data/DanmakuEnums.cs` | CollisionTarget.Obstacle 注释 AABB→OBB |
| `_Example/.../ObstacleRegistrar.cs` | +BoxCollider2D + OBB 旋转 + 变化检测 + Reset() + Gizmo |
| `_Example/.../ObstacleSpawner.cs` | ObstacleDefinition +Rotation + Gizmo 旋转 |
| `_Framework/DanmakuSystem/MODULE_README.md` | 新增 ObstacleCollisionMath + 碰撞描述更新 |
| `docs/Agent/OBB_OBSTACLE_TDD.md` | AC-09 编译通过 + 状态更新 |

## 向后兼容性

- `AddRect` 不传 rotationRad 默认 0°，现有调用零修改
- `UpdatePosition` 保留，只更新位置不动旋转
- `AddCircle` 内部 rotationRad 固定 0°

## 待验收

- AC-01~08, AC-10：需要 Play Mode 手动验收
- 预制体迁移：3 个 Obstacle 预制体需手动添加 BoxCollider2D

## 验证结果

- Unity 编译：0 errors / 0 warnings（MCP 验证）
- TDD 符合性：30+ 检查点全部通过
- 代码评审：无质量问题
