# Entity-Component Phase 3A · TDD v0.4 — 索引

> **版本**：v0.4 (PK Round 2~4 全部收敛)  
> **日期**：2026-05-02  
> **状态**：✅ **PK 收敛 — 可实施**  
> **前置文档**：ENTITY_COMPONENT_TDD.md（v2.6）、PHASE3_DESIGN.md（游戏设计师评审版）  
> **决策记录**：待 ADR 编号  
> **适用范围**：MiniGameTemplate Entity-Component 框架 Phase 3A — 战斗能力扩展
>
> **天命人决策**（2026-05-01）：
> - Phase 3B（击杀计分/道具掉落/命数/难度/会话管理器）整体延后
> - 仅"玩家移动边界"（原 P3B.4）归入 Phase 3A 作为前置基建
> - Phase 3A 范围：P3.0 玩家移动边界 + P3.1 空间查询&AutoAim + P3.2 DamageDealer + P3.3 SkillComponent + P3.4 BuffComponent

---

## 📋 文档拆分说明

为方便阅读和维护，TDD v0.4 拆分为以下子文件：

| # | 文件名 | 内容 | 行数 |
|---|--------|------|------|
| 1 | [PHASE3A_TDD_01_OVERVIEW.md](PHASE3A_TDD_01_OVERVIEW.md) | 设计目标 + 行为契约扩展（§一~§二） | ~100 |
| 2 | [PHASE3A_TDD_02_P30_P31.md](PHASE3A_TDD_02_P30_P31.md) | §3.0 玩家移动边界 + §3.1 空间查询 & AutoAim | ~350 |
| 3 | [PHASE3A_TDD_03_P32_P33.md](PHASE3A_TDD_03_P32_P33.md) | §3.2 DamageDealer + §3.3 SkillComponent | ~400 |
| 4 | [PHASE3A_TDD_04_P34.md](PHASE3A_TDD_04_P34.md) | §3.4 BuffComponent | ~300 |
| 5 | [PHASE3A_TDD_05_APPENDIX.md](PHASE3A_TDD_05_APPENDIX.md) | §四~§十一（槽位/时序/步骤/验收/架构/杠杆/风险/未决/变更清单） | ~350 |

---

## 📝 PK 评审记录

| Round | 角色 | 文件 | 状态 |
|-------|------|------|------|
| Round 2 | 工具开发者 vs 架构师 | [PHASE3A_PK_ROUND2.md](PHASE3A_PK_ROUND2.md) | ✅ 收敛（12 项修正） |
| Round 3 | 游戏设计师 vs 架构师 | [PHASE3A_PK_ROUND3.md](PHASE3A_PK_ROUND3.md) | ✅ 收敛（17 项修正） |
| Round 4 | 软件架构师 vs 架构师 | [PHASE3A_PK_ROUND4.md](PHASE3A_PK_ROUND4.md) | ✅ 收敛（15 项修正） |

**PK 累计**：44 项修正全部回写到 v0.4 子文件中。

---

## 变更日志

### v0.4 (2026-05-02) — PK Round 2~4 全量回写

**Round 2（工具开发者）修正**：ATK-001~ATK-018（12 项）  
**Round 3（游戏设计师）修正**：GD-001~GD-017（17 项）  
**Round 4（软件架构师）修正**：SA-001~SA-017（15 项）  

详细变更日志见各子文件末尾。

### v0.3 (2026-05-02) — PK Round 1 收敛

UA-009~UA-015（7 项修正）

### v0.2 (2026-05-01) — PK Round 1 修正

UA-001~UA-008（8 项修正）

### v0.1 (2026-05-01) — 初稿

Phase 3A TDD 完整初稿。
