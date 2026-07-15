---
system: shootergame-gdd
scope: skill-system-overview
last_verified: 2026-05-18-c
depends_on: [SG_GAME_DESIGN]
related_code: Assets/_Game/Scripts/ShooterGame/**, Assets/_Framework/EntitySystem/Components/Skill*, Buff*
---

# ShooterGame 技能系统 GDD · 索引

> **版本**：v2.4（PK-R4 TDD↔GDD 一致性同步——CD 尺寸/布局/被动尺寸/首波动效/DotSlot 扩容）  
> **日期**：2026-05-19  
> **前置**：`SHOOTER_GAME/SG_GAME_DESIGN.md` v3.2  
> **定位**：从"撞击式玩法"升级为"射击 + 技能 + 攻防兼备"的完整战斗体验

---

## 前置知识清单

> 本 GDD 假设读者已了解以下前置上下文。若不熟悉，请先阅读对应文档/代码。

| 概念 | 来源 | 一句话说明 |
|------|------|-----------|
| Entity-Component 框架 | `Assets/_Framework/EntitySystem/` + `SHOOTER_GAME/SG_GAME_DESIGN.md` §3 | 纯 C# 对象 + ComponentType O(1) 查找 + EntityPool 零 GC |
| 弹幕系统（DanmakuSystem） | `Assets/_Framework/DanmakuSystem/` + `SHOOTER_GAME/SG_GAME_DESIGN.md` §4 | BulletWorld NativeArray，Camp 区分敌我，支持 1024 同屏弹丸 |
| 伤害管线（IDamageModifier） | `Assets/_Framework/EntitySystem/Components/DamageDealer*` | BaseDmg → Crit → Modifier 链 → FinalDmg |
| 屏幕坐标系 | `SHOOTER_GAME/SG_GAME_DESIGN.md` §2 | 750×1334 逻辑像素，世界空间 Y 轴向上，原点在屏幕中下 |
| EntityConfigSO | `Assets/_Framework/EntitySystem/Config/` | Entity 的静态配置数据载体（SO） |
| BulletPatternSO | `Assets/_Framework/DanmakuSystem/Config/` | 子弹发射模式配置（数量/角度/速度/生命周期） |

---

## 子文件列表

| 文件 | 主题 | 一句话摘要 |
|------|------|-----------|
| [SG_GDD_01_ACTIVE_SKILLS](SHOOTER_GAME/GDD/SG_GDD_01_ACTIVE_SKILLS.md) | 主动技能设计 | 6 种主动技能（散射→激光→导弹→僚机→护盾→轨道炮）+ 数值框架 |
| [SG_GDD_02_PASSIVE_BUFFS](SHOOTER_GAME/GDD/SG_GDD_02_PASSIVE_BUFFS.md) | 被动+Buff+DOT | 4 被动 + 7 Buff + 3 DOT 的完整状态系统 |
| [SG_GDD_03_ITEMS_CONFIG](SHOOTER_GAME/GDD/SG_GDD_03_ITEMS_CONFIG.md) | 道具掉落 + 配置表 | 4 类道具 + Luban 配置表结构 |
| [SG_GDD_04_WORKFLOW](SHOOTER_GAME/GDD/SG_GDD_04_WORKFLOW.md) | 策划 + 美术工作流 | SO 配置流程 + VFX 资源规范 + 关卡设计流程 |
| [SG_GDD_05_SUPPLEMENT](SHOOTER_GAME/GDD/SG_GDD_05_SUPPLEMENT.md) | 补充设计 + 已确认决策 | 难度曲线、经济系统、AI 行为、音频、无障碍等 |
| [SG_GDD_06_ROADMAP](SHOOTER_GAME/GDD/SG_GDD_06_ROADMAP.md) | 路线图 + 风险 + 附录 | 5 Sprint 实现计划 + 风险矩阵 + 数值模板 |

---

## 设计支柱（Design Pillars）

| # | 支柱 | 玩家感受 | 违反案例 |
|---|------|---------|---------|
| P1 | **零门槛上手** | 不看教程也能打——**全自动战斗** + 玩家只操控飞机移动 | 需要手动释放技能或瞄准 |
| P2 | **火力渐强的爽感** | 越打越强、弹幕越来越密、屏幕越来越满 | 全程同一种子弹，视觉不变 |
| P3 | **走位即策略** | 站位选择=核心决策——有的地方安全但 DPS 低，有的地方危险但 DPS 高 | 站哪都一样 |
| P4 | **可控的紧张感** | 压力来自"来不来得及消灭"，而非"看不懂怎么死的" | 敌机弹幕淹没屏幕、玩家没有安全区 |
| P5 | **轻量配置、快速迭代** | 策划改一个 SO 立刻看效果，不需要写代码 | 新增技能必须写 C# |

---

## 系统总览

### 当前痛点

| 问题 | 影响 |
|------|------|
| 玩家飞机只能撞敌机，没有射击 | 核心动词缺失——"射击"是品类最基本的体验 |
| 敌机只会直线下落，不会攻击 | 威胁感单一、走位缺乏深度 |
| 战斗体验无成长感 | 第 1 关和第 5 关手感一样，没有"变强"的满足 |
| 无法做关卡难度分层 | 只能调数量和速度，缺少敌机行为维度 |

### 目标演进

```
V1（当前）：撞击 + 自动直射
    ↓
V2（本次）：我方多技能 + 敌方射击 + Buff/DOT + 被动技能 + 道具掉落
    ↓
V3（未来）：Boss 战 + 技能升级树 + 无尽模式
```

### V2 核心循环

```
操控飞机移动（唯一操作）→ 躲避敌弹/敌机
→ 全部技能自动释放 → 拾取道具 → 火力变强
→ 应对更多更肉的敌机编队
→ 火力不够？→ 解锁/升级技能（付费循环）
```

---

## 四层架构总览

```
┌──────────────────────────────────────────────────┐
│              技能系统（Skill System）              │
├──────────┬──────────┬───────────┬────────────────┤
│ 主动技能  │ 被动技能  │  Buff 系统 │  DOT 系统     │
│ Active   │ Passive  │  Buff     │  Damage Over   │
│ Skill    │ Skill    │  System   │  Time          │
├──────────┴──────────┴───────────┴────────────────┤
│              伤害管线（Damage Pipeline）           │
│   BaseDmg → Crit → IDamageModifier → FinalDmg   │
├──────────────────────────────────────────────────┤
│              弹幕系统（Danmaku System）            │
│   BulletPattern → Spawn → Move → Collide → Die  │
└──────────────────────────────────────────────────┘
```

| 层 | 定义 | 核心职责 | 举例 |
|----|------|---------|------|
| **主动技能** | CD 管理的攻击行为，**全自动触发**，有前摇/后摇时间轴 | 发射特殊弹幕、AOE 爆炸、召唤僚机 | 散射弹幕、导弹齐射、激光扫射 |
| **被动技能** | 有 CD 的周期性自动增强（v1.7 修正） | 周期性修改现有机制的行为 | 子弹穿透窗口、暴击窗口、磁吸范围 |
| **Buff** | 有持续时间的状态修正 | 加/减属性倍率 | 攻速+50%、减速 30%、护盾 |
| **DOT** | 持续伤害区域/状态 | 每 N 秒造成一次伤害 | 毒雾、燃烧地面、电弧链 |
