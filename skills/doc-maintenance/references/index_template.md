# INDEX.md 路由表模板

> 此文件是 `Docs/Agent/INDEX.md` 的结构模板。创建或更新 INDEX.md 时参考此模板。

## 文件头

```markdown
# 文档总索引

> Agent 每次会话首先读取此文件，通过路由表精确定位目标文档。
> 最后更新：YYYY-MM-DD

---
```

## 路由表 A：任务路由

```markdown
## 🎯 任务路由

| 我要做什么 | 读什么文件 | 备注 |
|-----------|-----------|------|
| 新建一种敌人 | SO_WORKFLOWS_02 + EC_TDD_04 | SO 创建 + 组件配置 |
| 新建一个技能 | SO_WORKFLOWS_02 §SkillConfigSO + PHASE3A_TDD_03 | 技能 SO + Effect 链路 |
| 新建一个 Buff | SO_WORKFLOWS_02 §BuffConfigSO + PHASE3A_TDD_04 | Buff SO + Duration/叠加 |
| 新增子弹花样 | SO_WORKFLOWS_03 + ATLAS_TDD_01 §API | 弹幕 SO + Atlas 纹理 |
| 修改碰撞逻辑 | EC_TDD_03 §Collision + OBB_TDD_01 | 碰撞组件 + OBB 数学 |
| 新增 ADR 决策 | ADR_INDEX + ADR_04_RECENT | 追加到最新 ADR 子文件 |
| 新增编辑器工具 | EDITOR_TOOLS_MANUAL_INDEX | 模板 + 注册流程 |
| 配置微信广告 | WECHAT_INTEGRATION §Ads | 广告 ID + 回调 |
| 调试性能问题 | DEBUG_PLAYBOOK §Performance | Profiler + DC 排查 |
| 从零开始新项目 | NEWGAME_GUIDE | 全流程 |
```

**维护规则**：新增功能/能力时，追加对应行。使用 `§` 标记文档内锚点。

## 路由表 B：代码→文档映射

```markdown
## 🔗 代码→文档映射

| 代码路径/模式 | 对应文档 | 说明 |
|--------------|---------|------|
| `EntitySystem/*.cs` | EC_TDD_INDEX 相关子文件 | 组件/系统变更 |
| `EntitySystem/Components/Skill*` | PHASE3A_TDD_03 | 技能子系统 |
| `EntitySystem/Components/Buff*` | PHASE3A_TDD_04 | Buff 子系统 |
| `Danmaku/**/*.cs` | SO_WORKFLOWS_03 + ATLAS_TDD_INDEX | 弹幕+渲染 |
| `Editor/**/*.cs` | EDITOR_TOOLS_MANUAL_INDEX | 工具注册 |
| `*ConfigSO.cs` / `*SO.cs` | SO_CATALOG + SO_WORKFLOWS_INDEX | SO 目录+流程 |
| `EntitySystemBootstrap.cs` | EC_TDD_05 §Bootstrap | 胶水层 |
```

**维护规则**：新增代码目录/模块时，追加映射行。路径支持 glob 通配符。

## 路由表 C：概念速查

```markdown
## 📖 概念速查

| 概念/术语 | 定义位置 | 一句话 |
|-----------|---------|--------|
| PendingDespawn | EC_TDD_02 §EntityPool | Entity 标记待回收但本帧不立即销毁 |
| DamageContext | PHASE3A_TDD_03 §DamageDealer | 伤害传递结构体（替代裸 int） |
| ComponentType 枚举 | EC_TDD_01 §枚举定义 | O(1) 组件访问的位标志 |
| TypeRegistry | ADR_INDEX §ADR-030 | 弹幕类型注册（内化到框架） |
| RuntimeAtlas | ATLAS_TDD_01 §架构 | 运行时动态纹理合批 |
| CampUtility | PHASE3A_TDD_03 §阵营 | 阵营判定工具类 |
| 变更包 | CONV_03 §变更包工作流 | 每次修改的归档记录 |
| Template_ 前缀 | CONV_01 §SO命名 | 模板 SO 资产命名约定 |
| TickOrder | EC_TDD_05 §更新顺序 | 系统 Tick 执行优先级 |
| EntityEventBus | EC_TDD_02 §事件 | 零 GC 预分配事件总线 |
```

**维护规则**：新增概念/术语/缩写时，追加速查行。定义位置格式为 `文件名 §章节标记`。

## 文件尾

```markdown
---

## 归档

- `Archive/` — 过程文档（PK评审/验收报告/旧设计），仅供追溯，不作开发依据
- `changes/` — 变更包归档

---
_本索引随项目演进更新。每次文档操作后检查是否需要同步路由表。_
```
