# Frontmatter 模板速查

> 创建或拆分文档时，从此处复制对应系统的 Frontmatter 模板。

## 通用模板

```markdown
---
system: <system-id>
scope: <1-3词范围描述>
last_verified: <YYYY-MM-DD>
depends_on: [<前置文件1>, <前置文件2>]
related_code: <Assets/路径/通配符>
---
```

## 各系统模板

### Entity-Component

```markdown
---
system: entity-component
scope: <overview|entity-pool|components-core|components-combat|systems|view|appendix>
last_verified: 2026-05-02
depends_on: [EC_TDD_01]
related_code: Assets/_Framework/EntitySystem/**/*.cs
---
```

### Phase 3A 技能/Buff

```markdown
---
system: phase3a-skill-buff
scope: <overview|damage-dealer|skill|buff|appendix>
last_verified: 2026-05-02
depends_on: [EC_TDD_INDEX, PHASE3A_TDD_INDEX]
related_code: Assets/_Framework/EntitySystem/Components/Skill*, Buff*
---
```

### ADR 决策记录

```markdown
---
system: architecture
scope: <foundation|danmaku|entity|recent>
last_verified: 2026-05-02
depends_on: []
related_code: <涉及的核心代码路径>
---
```

### Runtime Atlas

```markdown
---
system: runtime-atlas
scope: <design|implementation|acceptance>
last_verified: 2026-05-02
depends_on: [ATLAS_TDD_INDEX]
related_code: Assets/_Framework/RuntimeAtlas/**/*.cs
---
```

### OBB 碰撞

```markdown
---
system: obb-collision
scope: <design|implementation>
last_verified: 2026-05-02
depends_on: [OBB_TDD_INDEX]
related_code: Assets/_Framework/OBB/**/*.cs
---
```

### 编码约定

```markdown
---
system: conventions
scope: <naming|coding|workflow>
last_verified: 2026-05-02
depends_on: []
related_code: <全局>
---
```

### 编辑器工具

```markdown
---
system: editor-tools
scope: <build|validate|entity|inspectors>
last_verified: 2026-05-02
depends_on: []
related_code: Assets/_Framework/Editor/**/*.cs
---
```

### SO 配置

```markdown
---
system: so-config
scope: <core|entity|danmaku|vfx-render|infra>
last_verified: 2026-05-02
depends_on: [SO_CATALOG]
related_code: Assets/_Framework/**/*SO.cs, Assets/_Game/Configs/**/*.asset
---
```

## 有效 system 值清单

| system 值 | 对应系统 |
|-----------|---------|
| `entity-component` | Entity-Component 框架 |
| `phase3a-skill-buff` | 技能/Buff 子系统 |
| `danmaku` | 弹幕系统 |
| `runtime-atlas` | Runtime Atlas 渲染 |
| `obb-collision` | OBB 碰撞检测 |
| `architecture` | 架构决策（ADR） |
| `conventions` | 编码/命名/工作流约定 |
| `editor-tools` | 编辑器工具 |
| `so-config` | SO 配置流程 |
| `wechat` | 微信平台集成 |
| `general` | 通用/不属于特定系统 |
