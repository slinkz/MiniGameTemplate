# ShooterGame V2 技能系统 TDD · 索引

> **版本**：v1.5  
> **日期**：2026-05-25  
> **GDD 版本**：v2.4（SG_GDD_INDEX）  
> **定位**：V2 技能系统逐 Sprint 实施方案 + 验收方案 + 架构升级

---

## 前置依赖

| 文档 | 用途 |
|------|------|
| SG_GDD_INDEX (v2.4) | 游戏设计全文——本 TDD 的设计源 |
| SG_TDD_INDEX (V1) | V1 基础框架 TDD——战斗骨架/关卡/UI/输入/云存储 |
| EC_TDD_INDEX | Entity-Component 框架 TDD |
| ATLAS_TDD_INDEX | RuntimeAtlas 动态图集 TDD |
| SG_TOOLS_TDD_INDEX | 编辑器工具 TDD |

---

## 总体实施策略

### 原则

1. **Sprint 顺序严格串行**——前一个 Sprint 验收通过后才推进下一个
2. **每 Sprint = 编码 → PlayMode 验收 → 真机验证 → 合格后推进**
3. **SO 配置驱动**——新增功能优先通过 SO 扩展实现，策划可热调
4. **零 GC 热路径**——所有战斗循环新增代码遵循零分配原则
5. **最小增量**——每次只新增一个可验证的功能切片

### 架构全景

```
┌──────────────────────────────────────────────────────────┐
│                   BattleController（胶水层）              │
├──────────┬──────────┬───────────┬──────────┬────────────┤
│ Sprint 1 │ Sprint 2 │ Sprint 3  │ Sprint 4 │ Sprint 5   │
│ 敌方射击 │ 技能装备 │ Buff/DOT  │ 关卡平衡 │ 工具+UI    │
│ + 碰撞   │ + 道具   │ + 被动    │ + 数值   │ + 打磨     │
├──────────┴──────────┴───────────┴──────────┴────────────┤
│              Entity-Component 框架 (V1 已有)              │
├──────────────────────────────────────────────────────────┤
│              弹幕系统 + RuntimeAtlas (V1 已有)            │
└──────────────────────────────────────────────────────────┘
```

### Sprint 依赖链

```
Sprint 1（敌方射击+碰撞）
  ↓ 敌弹存在 → 才有"挡弹策略"
Sprint 2（技能解锁+装备+道具）
  ↓ SkillComponent + PickupSystem 存在
Sprint 3（Buff/DOT/被动）
  ↓ BuffComponent 扩展 + PassiveComponent
Sprint 4（关卡编排+数值平衡）
  ↓ 全系统就位 → Playtest 调参
Sprint 5（策划工具+UI+打磨）
  → 工具辅助后续迭代 + UI 完善 + 发布前打磨
```

---

## 子文件列表

| 文件 | Sprint | 主题 | 预估工时 | 状态 |
|------|--------|------|---------|------|
| [SG_V2_TDD_01](SG_V2_TDD_01_ENEMY_SHOOTING.md) | Sprint 1 | 敌方射击 + 碰撞规则 + 伤害转发 | ~10h | ✅ 编码+逻辑验收通过 |
| [SG_V2_TDD_02](SG_V2_TDD_02_SKILL_EQUIP_ITEM.md) | Sprint 2 | 技能解锁 + 战前装备 + 道具系统 | ~14h | ✅ 编码+逻辑验收通过 |
| [SG_V2_TDD_03](SG_V2_TDD_03_BUFF_DOT_PASSIVE.md) | Sprint 3 | Buff 扩展 + DOT + 被动技能 | ~15h | ✅ 编码+逻辑验收通过 |
| [SG_V2_TDD_04](SG_V2_TDD_04_LEVEL_BALANCE.md) | Sprint 4 | 关卡编排 + 数值平衡 | ~8h | ✅ 编码+逻辑验收通过 |
| [SG_V2_TDD_05](SG_V2_TDD_05_TOOLS_UI_POLISH.md) | Sprint 5 | 策划工具 + UI 完善 + 打磨 | ~18.5h | ✅ 全部验收通过 |
| [SG_V2_TDD_06](SG_V2_TDD_06_ATTACK_SKILL.md) | 架构升级 | 普攻升格为技能系统（AimMode 数据驱动） | ~20.5h | 🔨 编码中（P1~P10✅ 待验收） |

**总计**：~86h（Sprint 1~5: 65.5h + 架构升级: 20.5h）

---

## 公共约定

### 命名空间

所有 V2 新增代码归属 `MiniGameTemplate.Entity` 或 `Game.ShooterGame`，不新建命名空间。

### SO 资产路径

| 类型 | 路径 |
|------|------|
| SkillConfigSO | `Assets/_Game/Configs/ShooterGame/Skills/` |
| BuffConfigSO | `Assets/_Game/Configs/ShooterGame/Buffs/` |
| DotConfigSO | `Assets/_Game/Configs/ShooterGame/Dots/` |
| PassiveAbilitySO | `Assets/_Game/Configs/ShooterGame/Passives/` |
| PickupConfigSO | `Assets/_Game/Configs/ShooterGame/Pickups/` |
| DropTableSO | `Assets/_Game/Configs/ShooterGame/` |
| 解锁表 SO | `Assets/_Game/Resources/ShooterGame/` |

### ID 分配

| 范围 | 类型 |
|------|------|
| 1000~2999 | Buff（增益） |
| 3000~3999 | Debuff（减益） |
| 4000~4999 | DOT |
| 5000~5999 | Passive（被动技能） |

### 验收通用标准

每个 Sprint 验收必须满足：
1. ✅ 编译零错误零警告
2. ✅ PlayMode 功能点全部 PASS
3. ✅ 零 GC 热路径验证（Profiler Deep Profile 确认）
4. ✅ 微信真机验证（无 WebGL 不兼容问题）
5. ✅ SO 配置完整（无 Missing Reference）

---

_创建于 2026-05-18 | V2 技能系统实施 TDD_
