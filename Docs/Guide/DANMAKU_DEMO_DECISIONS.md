# 弹幕试炼场 Demo 决策记录

> 决策日期：2026-04-09
> 状态：**已确认，待实施**

---

## 未决项决策

| ID | 优先级 | 问题 | 决策 |
|----|--------|------|------|
| U1 | P0 | Player→Boss 碰撞：框架无通用 Target 机制 | **选 B：框架新增 RegisterTarget API**，CollisionSolver 支持多目标碰撞 |
| U2 | P0 | SpawnerProfileSO 无驱动逻辑 | **框架补 SpawnerDriver 系统**，自动驱动 Sequential/Random 切换 |
| U3 | P1 | 无敌帧配置 | **加上**，DanmakuWorldConfig 已有 `InvincibleDuration` 字段，确保 Demo 中启用 |
| U4 | P1 | 弹丸计数来源 | **展示**，使用 `BulletWorld.ActiveCount` 直接读取 |
| U5 | P2 | 性能 HUD Draw Call 显示 | **展示**，改为固定值显示或自行计数（不依赖 UnityStats API） |
| U6 | P2 | 精灵/贴图资源制作 | **资源制作由人类负责**，AI 提供制作清单和详细步骤 |
| U7 | P2 | FPS 帧率统计 | **改为固定值显示或自行计数**，不依赖 `1/Time.deltaTime`，Demo 自行维护帧计数器 |
| U8 | P3 | 调度器观测 API（调试用） | **赞成**，PatternScheduler 补充只读属性 |
| U9 | P3 | 性能 HUD 的 UI 实现方案 | **使用 FairyGUI**，与模板 UI 框架保持一致 |
| U10 | P3 | 渐进难度用 SO 热切换 | **赞成**，Demo 中演示运行时切换 DifficultyProfileSO |

---

## 实施顺序

### 第一阶段：框架短板补齐（本次开发）

1. **CollisionSolver RegisterTarget API（U1）**
   - 新增 `ICollisionTarget` 接口 + `TargetRegistry` 注册表
   - CollisionSolver.SolveBulletVsTarget 遍历注册的全部目标（含现有 Player）
   - 阵营过滤泛化：Enemy 弹丸 → Player 目标，Player 弹丸 → Enemy 目标，Neutral → 全部

2. **SpawnerDriver 系统（U2）**
   - 新增 `SpawnerDriver` 类（纯逻辑，非 MonoBehaviour）
   - 由 DanmakuSystem 每帧 Tick
   - 驱动 SpawnerProfileSO 的 Sequential / Random 切换 + 冷却计时
   - External 模式下不自动切换，由外部 FSM 调用 `SetGroupIndex`

3. **调度器观测 API（U8）**
   - PatternScheduler 补充 `ActiveTasks` 只读属性（已有）
   - 考虑补充 `PeakTasks`、`TotalScheduled` 统计

4. **无敌帧确认（U3）**
   - DanmakuWorldConfig 已有 `InvincibleDuration`，DanmakuSystem.Update 已有无敌帧逻辑
   - 确保事件通知在无敌帧期间被正确抑制

### 第二阶段：Demo 实现（后续开发）

- 5 脚本 + ~15 SO 资产 + FairyGUI 性能 HUD
- 资源制作清单另行提供

---

## 框架改动影响评估

| 改动 | 影响文件 | 风险 |
|------|----------|------|
| ICollisionTarget + TargetRegistry | 新增 2 文件 + 改 CollisionSolver + 改 DanmakuSystem | 中：碰撞热路径，需验证性能 |
| SpawnerDriver | 新增 1 文件 + 改 DanmakuSystem | 低：纯增量逻辑 |
| 调度器观测 API | 改 PatternScheduler | 极低：只加只读属性 |

---

## 资源制作清单（U6，后续提供详细步骤）

*待 Demo 实现阶段提供完整的精灵/贴图制作清单，包括：*
- 弹丸精灵图集（各种弹型 + 颜色变体）
- 激光/喷雾贴图
- Boss 精灵
- Player 精灵
- 障碍物精灵
- UI 素材
