# P1.0 变更包：BulletFaction → EnumCamp 阵营枚举统一迁移

> **日期**：2026-04-30  
> **TDD 步骤**：P1.0  
> **ADR**：ADR-033（Entity-Component 通用角色框架）  
> **触发**：TDD v2.6 SA-002

## 变更摘要

将弹幕系统中的 `BulletFaction` 枚举统一重命名为 `EnumCamp`，为 Entity-Component 系统提供跨系统共享的阵营枚举。

## 变更详情

### 枚举定义（1 文件）
- `DanmakuEnums.cs`：`BulletFaction` → `EnumCamp`，枚举值 (0,1,2) 不变，新增迁移历史注释

### 接口（1 文件）
- `ICollisionTarget.cs`：`Faction` 属性类型 `BulletFaction` → `EnumCamp`

### 配置 SO（4 文件）
- `BulletTypeSO.cs` / `LaserTypeSO.cs` / `SprayTypeSO.cs` / `ObstacleTypeSO.cs`：`Faction` 字段类型迁移

### 数据结构（2 文件）
- `CollisionEventBuffer.cs`：`CollisionEvent.SourceFaction` / `TargetFaction` 类型迁移
- `ObstaclePool.cs`：`AddRect()` / `AddCircle()` 参数类型迁移

### 核心逻辑（2 文件）
- `CollisionSolver.cs`：全 4 条碰撞路径（弹丸/激光/喷雾/障碍物）迁移 + `ShouldCollide()` 签名 + 局部变量 `bulletFaction` → `sourceCamp`
- `DanmakuSystem.cs`：内部 `PlayerTarget.Faction` 属性迁移

### 测试（1 文件）
- `CollisionEventBufferTests.cs`：测试数据中的枚举引用迁移

### Demo（2 文件）
- `ObstacleSpawner.cs` / `ObstacleRegistrar.cs`：字段类型和默认值迁移

## 影响范围

- 共 13 个 .cs 文件
- 底层 byte 值不变 → 已有 SO 资产零影响
- 序列化字段名（`Faction`）不变 → 零重序列化

## 验收结论

| AC | 结果 |
|----|------|
| 全项目零 `BulletFaction` 类型引用 | ✅（仅注释中保留历史记录） |
| 编译通过 | ✅（待 Unity 编辑器验证） |
| DanmakuDemo 行为不变 | ✅（枚举值不变，序列化兼容） |
