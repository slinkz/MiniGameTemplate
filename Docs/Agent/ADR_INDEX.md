# 架构决策记录（ADR）索引

> **起始日期**：2026-04-11  
> **共计**：36 条 ADR（ADR-001 ~ ADR-036）  
> **状态标记**：✅ Accepted / 🔄 Superseded / ❌ Rejected

---

## 可执行 ADR 使用规则

> P3 知识工程新增：ADR 不只作为历史记录，还作为 Agent 编码前的架构约束入口。详见 [ADR_SCHEMA.md](ADR_SCHEMA.md)。

### Agent 使用流程

1. 根据任务读取对应 Context Pack 和 Module Card。
2. 查看模块卡引用的 ADR。
3. 在本文确认 ADR 状态是否 Accepted / Superseded / Rejected。
4. 对优先 ADR，读取 `ADR_SCHEMA.md` 中的可执行摘要，检查 `AppliesTo`、`Constraints`、`Verification`。
5. 若本次设计违反现有 ADR，必须先提出 ADR 更新或新增 ADR，不要直接改代码。

### 优先可执行 ADR

| ADR | 主题 | 可执行重点 | 入口 |
|-----|------|------------|------|
| 028 | RuntimeAtlasSystem 统一管线 | RuntimeAtlas 是运行时统一渲染核心；旧独立贴图运行时约束被替代 | `ADR_SCHEMA.md` §ADR-028 |
| 031 | RuntimeAtlas 深化 | 懒建页、Laser 条件入 Atlas、Trail 纹理化 | `ADR_SCHEMA.md` §ADR-031 |
| 032 | new Material shaderKeywords | 运行时克隆材质必须复制 shaderKeywords | `ADR_SCHEMA.md` §ADR-032 |
| 033 | Entity-Component 框架 | Entity 纯 C#、热路径零 GC、组件/Tick 契约 | `ADR_SCHEMA.md` §ADR-033 |
| 034 | AppFlow 栈式导航 | Push/Pop/Replace、面板 Suspend/Resume、冷启动清栈 | `ADR_SCHEMA.md` §ADR-034 |
| 035 | 战斗退场生命周期 | BattleLifecycleEvent + IBattleCleanup；已代码级确认 | `ADR_SCHEMA.md` §ADR-035 |
| 036 | 飘字统一 RBM | 禁止新增并行飘字路径，统一 FloatingTextSystem | `ADR_SCHEMA.md` §ADR-036 |

---

## 子文件目录

| # | 文件 | ADR 范围 | 主题域 | 行数 |
|---|------|---------|--------|------|
| 1 | [ADR_01_FOUNDATION.md](ADR_01_FOUNDATION.md) | 001~010 | 基础架构：渲染层/Batch/碰撞/容量/资源策略 | ~299 |
| 2 | [ADR_02_DANMAKU.md](ADR_02_DANMAKU.md) | 011~020 | 弹幕&VFX：迁移/阵营/附着/排序/桥接 | ~280 |
| 3 | [ADR_03_RENDERING.md](ADR_03_RENDERING.md) | 021~028 | 渲染进阶：句柄/容量表/热重载/版本化迁移/RuntimeAtlas | ~510 |
| 4 | [ADR_04_ATLAS.md](ADR_04_ATLAS.md) | 029~030 | Atlas 精简：Additive移除 + TypeRegistry内化 | ~311 |
| 5 | [ADR_05_RECENT.md](ADR_05_RECENT.md) | 031~034 | 最新：Atlas深化 + Material踩坑 + Entity框架 + AppFlow导航 | ~320 |
| 6 | [ADR_06_LIFECYCLE.md](ADR_06_LIFECYCLE.md) | 035~036 | 战斗退场生命周期 + 飘字系统统一 | ~220 |

---

## 决策速查表

| ADR | 主题 | 状态 | 文件 |
|-----|------|------|------|
| 001 | RenderLayer 归属统一 | ✅ | 01 |
| 002 | BatchManager 共享实现不共享实例 | ✅ | 01 |
| 003 | CollisionEventBuffer 单主消费者 | ✅ | 01 |
| 004 | MotionRegistry 受控注册表 | ✅ | 01 |
| 005 | 容量配置分层收拢 | ✅ | 01 |
| 006 | DanmakuSystem 保留 Facade | ✅ | 01 |
| 007 | Bullet 资源独立贴图 | 🔄 by 028 | 01 |
| 008 | VFX 资源独立贴图 | 🔄 by 028 | 01 |
| 009 | DamageNumber 共用数字图集 | ✅ | 01 |
| 010 | Atlas 编辑器可选工具 | 🔄 by 028 | 01 |
| 011 | 旧 SO 迁移自动化 | ✅ | 02 |
| 012 | 阵营模型通用关系 | ✅ | 02 |
| 013 | VFX 附着模式显式建模 | ✅ | 02 |
| 014 | sortingOrder 独立配置 | ✅ | 02 |
| 015 | VFX Registry 重建时机 | ✅ (修正 by 030) | 02 |
| 016 | Danmaku→VFX 桥接解耦 | ✅ | 02 |
| 017 | RBM 桶预热 | 🔄 by 030 | 02 |
| 018 | Bullet/VFX 资源描述统一 | ✅ | 02 |
| 019 | Atlas 可逆派生产物 | ✅ | 02 |
| 020 | CollisionEventBuffer 溢出不影响主逻辑 | ✅ | 02 |
| 021 | VFX FollowTarget 抽象句柄 | ✅ | 03 |
| 022 | 容量配置化范围表 | ✅ | 03 |
| 023 | OnValidate 与热重载边界 | ✅ | 03 |
| 024 | 统一资源描述版本化迁移 | ✅ | 03 |
| 025 | 编辑器刷新工作流 | ✅ (修正 by 030) | 03 |
| 026 | 子弹序列帧动画 | ✅ | 03 |
| 027 | 最终执行契约收口 | ✅ | 03 |
| 028 | RuntimeAtlasSystem 统一管线 | ✅ | 03 |
| 029 | 移除 Additive Blend | ✅ | 04 |
| 030 | TypeRegistry 内化+懒注册 | ✅ | 04 |
| 031 | RuntimeAtlas 深化 | ✅ | 05 |
| 032 | new Material() shaderKeywords | ✅ | 05 |
| 033 | Entity-Component 框架 | ✅ | 05 |
| 034 | AppFlow 栈式导航系统 | ✅ | 05 |
| 035 | 战斗退场生命周期统一事件通道 | ✅ 已实施（代码级确认 2026-07-14） | 06 |
| 036 | 飘字系统统一到 RBM 渲染管线 | ✅ 已实施（2026-06-03） | 06 |
