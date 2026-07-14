---
system: knowledge-engineering
scope: impact-analysis-template
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Impact Analysis Template

> 用途：中大型任务编码前复制本模板，先完成影响面分析，再进入实现。小改动可以口头简化，但不能跳过 ADR/热路径/验证判断。

## 1. 任务摘要

- 需求：
- 目标行为：
- 非目标 / 不做范围：

## 2. 触碰模块

| 模块 | Module Card | 是否核心影响 | 说明 |
|------|-------------|--------------|------|
| | | 是/否 | |

## 3. 代码路径影响

| 路径/符号 | 改动类型 | 来源映射 | 风险 |
|-----------|----------|----------|------|
| | 新增/修改/删除 | `CODE_KNOWLEDGE_MAP.md` | 低/中/高 |

## 4. 文档与设计依据

| 类型 | 文档 | 本次使用点 |
|------|------|------------|
| Context Pack | | |
| Module Card | | |
| TDD/GDD | | |
| ADR | | |
| SO Workflow | | |
| Debug/Playbook | | |

## 5. ADR 检查

| ADR | 状态 | AppliesTo 是否命中 | Constraints 是否满足 | 备注 |
|-----|------|--------------------|----------------------|------|
| | | 是/否 | 是/否 | |

如果任一 ADR 约束不满足，先停下：需要提出 ADR 更新或新增 ADR，不直接编码。

## 6. 数据 / SO / 资产影响

| 资产类型 | 路径 | 是否需要新增/修改 | 验证方式 |
|----------|------|-------------------|----------|
| SO | | | |
| FairyGUI | | | |
| Scene | | | |
| Luban | | | |
| Prefab/Texture/Audio | | | |

## 7. 热路径与平台约束

- 是否触碰 Update/Tick/渲染/碰撞热路径：是/否
- 是否可能新增 GC：是/否
- 是否触碰 WebGL/微信限制：是/否
- 是否触碰 IL2CPP stripping/link.xml：是/否
- 是否触碰异步/云存储/网络：是/否

说明：

```text

```

## 8. 验证计划

| 验证项 | 工具/入口 | 通过标准 |
|--------|-----------|----------|
| 编译 | Unity/MCP | 0 error |
| 架构检查 | Architecture Check | 0 blocker |
| SO 校验 | Validator | 无 Missing/非法配置 |
| PlayMode | | 核心流程通过 |
| Profiler/GC | | 热路径无新增分配 |
| 微信开发者工具 | | 无平台异常 |
| 真机 | | 关键路径通过 |

## 9. 回归风险

| 风险 | 触发条件 | 缓解 |
|------|----------|------|
| | | |

## 10. 文档更新计划

| 文档 | 是否更新 | 原因 |
|------|----------|------|
| `CODE_KNOWLEDGE_MAP.md` | 是/否 | |
| Module Card | 是/否 | |
| Context Pack | 是/否 | |
| ADR | 是/否 | |
| SO Workflow | 是/否 | |
| Debug Playbook | 是/否 | |

## 11. 结论

- 可以直接编码：是/否
- 需要先补设计/ADR：是/否
- 需要用户确认的问题：