---
system: architecture
scope: codegraph-mcp
last_verified: 2026-05-24
depends_on: [ARCHITECTURE, MCP_INTEGRATION]
related_code: .codegraph/*, ~/.workbuddy/mcp.json
---

# CodeGraph 集成（AI Agent 代码知识图谱）

## 概述

CodeGraph 是预索引的代码知识图谱工具，通过 tree-sitter 将源码解析为 AST，提取符号/调用/继承关系存入本地 SQLite，并通过 MCP 协议暴露给 AI Agent。

**核心价值**：将 Agent 代码检索从多次 grep + read_file 降低为单次图查询，减少 ~60% token 消耗和 ~50% 响应时间。

## 架构

```
AI Agent (WorkBuddy)
  │  MCP 协议（stdio）
  ▼
codegraph serve --mcp (CLI, 进程内 MCP Server)
  │  SQLite 读取
  ▼
.codegraph/codegraph.db (本地知识图谱)
  │  tree-sitter 解析
  ▼
项目源码 (*.cs, *.js, *.py 等)
```

## 安装

### 前置依赖

- Node.js 18+（当前 v22.22.2）
- Windows 10+

### 一键安装

```powershell
irm https://raw.githubusercontent.com/colbymchenry/codegraph/main/install.ps1 | iex
```

安装位置：`C:\Users\traimenxu\AppData\Local\codegraph\current\`

### 验证安装

```powershell
& "C:\Users\traimenxu\AppData\Local\codegraph\current\bin\codegraph.cmd" --version
# 预期输出: 0.9.3 或更高
```

> ⚠️ PowerShell 调用 `.cmd` 文件必须用 `&` 操作符。

## 初始化项目索引

```powershell
cd c:\workspace\mini-game-template
& "C:\Users\traimenxu\AppData\Local\codegraph\current\bin\codegraph.cmd" init
& "C:\Users\traimenxu\AppData\Local\codegraph\current\bin\codegraph.cmd" index
```

**当前索引统计**（2026-05-24）：
- 文件：1,479（C# 1,287 / JS 177 / Python 6 / C 4 / Lua 2）
- 节点：31,113 个符号
- 边：57,511 条关系（调用/继承/实现/包含）
- DB 大小：65.82 MB
- 索引耗时：~24s

产出目录：`.codegraph/`（已加入 `.gitignore`）

## MCP 配置

**已配置在 `~/.workbuddy/mcp.json`**：

```json
{
  "mcpServers": {
    "codegraph": {
      "command": "C:\\Users\\traimenxu\\AppData\\Local\\codegraph\\current\\bin\\codegraph.cmd",
      "args": ["serve", "--mcp"],
      "cwd": "c:\\workspace\\mini-game-template"
    }
  }
}
```

配置后需 **重启 WorkBuddy** 或重载 MCP 连接。

## [AGENT] 使用指南

### 工具优先级（铁律）

**代码检索首选 CodeGraph MCP 工具，次选 grep/read_file**。

| 场景 | 首选工具 | 回退方案 |
|------|---------|---------|
| 查找符号定义/位置 | `codegraph_search` | `search_content` |
| 理解功能/架构/调用链 | `codegraph_context`（PRIMARY） | `task(code-explorer)` |
| 查看某符号的调用者 | `codegraph_callers` | `search_content` 搜方法名 |
| 查看某符号调用了什么 | `codegraph_callees` | 读源码文件 |
| 评估修改影响范围 | `codegraph_impact` | 手动 grep 所有引用 |
| 获取符号详情/源码 | `codegraph_node` | `read_file` |
| 批量查看多个相关符号 | `codegraph_explore` | 多次 `read_file` |
| 了解项目文件结构 | `codegraph_files` | `list_dir` |
| 检查索引健康 | `codegraph_status` | CLI `codegraph status` |

### 关键工具说明

#### `codegraph_context`（最常用）

**PRIMARY TOOL** — 对任何"X 怎么工作"、架构、功能、bug 上下文问题，首先调用此工具。一次调用通常能返回足够的上下文（入口点、相关符号、关键代码）。

#### `codegraph_explore`

批量查看多个相关符号源码，远比逐个调用 `codegraph_node` 或多次 `read_file` 高效。**每个项目最多调用 2 次**。

#### `codegraph_search`

快速按名称搜索符号。只返回位置（不含代码）。用于确认存在性和定位。

### 注意事项

1. **不覆盖 Unity 序列化关系**：SO 引用、Inspector 拖拽赋值等不在代码调用链中，CodeGraph 无法捕获
2. **增量同步**：MCP Server 运行时自动监听文件变更（2s 去抖），无需手动 sync
3. **手动同步**：如需强制刷新索引：
   ```powershell
   & "C:\Users\traimenxu\AppData\Local\codegraph\current\bin\codegraph.cmd" sync
   ```
4. **query 语法**：使用具体的符号/文件/代码术语，不要用自然语言长句

## 日常维护

| 操作 | 命令 | 时机 |
|------|------|------|
| 查看状态 | `codegraph status` | 排查索引问题 |
| 手动增量同步 | `codegraph sync` | MCP 未运行时代码有变更 |
| 完全重建索引 | `codegraph index` | 索引损坏或升级 tree-sitter |
| 升级 CodeGraph | `irm .../install.ps1 \| iex` | 新版本发布时 |

## 局限性

| 局限 | 说明 | 替代方案 |
|------|------|---------|
| 不支持 Unity 序列化引用 | Inspector 拖拽的 SO 引用、Prefab 引用无法索引 | `search_content` 搜 GUID 或字段名 |
| tree-sitter C# 覆盖度 | 极少数 Unity 特有语法糖可能解析不全 | 发现时回退 grep |
| 仅索引代码 | 不索引 .asset / .prefab / .unity 文件 | 用 Unity MCP 工具查 |
| DB 体积 | ~66MB，不入 git | 每台机器本地生成 |

## 排障

| 症状 | 原因 | 解决 |
|------|------|------|
| MCP 工具不可见 | WorkBuddy 未重载 MCP | 重启 WorkBuddy |
| `codegraph_context` 返回空 | 索引过期或 DB 损坏 | `codegraph sync` 或重建 `codegraph index` |
| PowerShell 执行报错 | 未用 `&` 调用 .cmd | `& "...codegraph.cmd" <args>` |
| 索引不含新文件 | 文件在 .gitignore 排除的目录 | 检查 `.codegraph/config` 的 exclude 规则 |
