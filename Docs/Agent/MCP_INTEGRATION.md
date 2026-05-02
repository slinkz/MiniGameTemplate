---
system: architecture
scope: mcp-integration
last_verified: 2026-05-02
depends_on: [ARCHITECTURE]
related_code: Packages/com.anklebreaker.unity-mcp/**
---

# Unity MCP 集成（AI Agent ↔ Unity Editor）

## 概述

本项目集成了 [AnkleBreaker Unity MCP](https://github.com/AnkleBreaker-Studio/unity-mcp-plugin)，允许 AI Agent 通过 **Model Context Protocol (MCP)** 直接操作运行中的 Unity Editor。Agent 可以在不离开对话的情况下：

- 检查编译错误和警告
- 读取 Console 日志
- 查看/创建/删除 GameObject 和场景
- 读取和修改组件属性
- 创建和更新 C# 脚本
- 触发构建
- 截取 Scene View / Game View 截图做视觉验证
- 执行任意 C# 代码片段
- 以及 288 个工具覆盖 30+ 类别

## 架构

```
AI Agent (WorkBuddy)
  │  MCP 协议（stdio）
  ▼
anklebreaker-unity-mcp (Node.js MCP Server)
  │  HTTP (localhost:<自动端口>)
  ▼
com.anklebreaker.unity-mcp (Unity Editor Plugin)
  │  编辑器 API 调用
  ▼
Unity Editor (2021.3.17f1)
```

## 配置

**Unity 侧**（已就绪，无需操作）：
- 插件包：`Packages/com.anklebreaker.unity-mcp/`（本地冻结版本，含 2021.3 兼容补丁）
- 端口：**自动端口模式**（插件默认在 `7890-7899` 范围内自动选可用端口）
- 自动启动：Unity Editor 打开即运行

**WorkBuddy 侧**（已配置在 `~/.workbuddy/mcp.json`）：
```json
{
  "mcpServers": {
    "unity": {
      "command": "C:\\Program Files\\nodejs\\npx.cmd",
      "args": ["-y", "anklebreaker-unity-mcp@latest"],
      "env": {
        "UNITY_BRIDGE_PORT": "7891"
      }
    }
  }
}
```

> ⚠️ **铁律**：`UNITY_BRIDGE_PORT` 只是 Node MCP Server 的默认探测入口，**不代表 Unity 实际端口固定为 7891**。每次连接前必须先 `unity_list_instances` 扫描 `7890-7899`，拿到真实端口后再 `unity_select_instance` 并在后续工具里显式透传该端口。

**前置依赖**：
- Node.js 18+（当前：v22.22.2，路径：`C:\Program Files\nodejs\`）
- Unity Editor 需要处于打开状态且 MCP Bridge 正在运行

## [AGENT] 使用指南

### 编译验证（最常用）

代码修改后，**优先使用 MCP 工具验证编译**，不要让用户手动检查：

```
工具名: unity_get_compilation_errors
参数:   { "severity": "all", "port": <当前实例端口> }
```

- 返回 `count: 0` 表示编译通过
- 返回错误时包含文件路径、行号、错误消息，可直接定位修复
- 此工具基于 `CompilationPipeline`，独立于 Console 日志缓冲区

### 连接健康检查

```
工具名: unity_editor_ping
参数:   { "port": <当前实例端口> }
```

返回 Unity 版本、项目名、项目路径、平台信息。

### 常用工具速查

| 场景 | MCP 工具 | 说明 |
|------|---------|------|
| 编译检查 | `unity_get_compilation_errors` | 获取编译错误/警告 |
| Console 日志 | `unity_console_log` | 获取运行时日志 |
| 场景信息 | `unity_scene_info` | 当前场景名、路径、脏状态 |
| 场景层级 | `unity_scene_hierarchy` | 完整 GameObject 树 |
| 读 C# 脚本 | `unity_script_read` | 从 Unity 项目读取脚本内容 |
| 写 C# 脚本 | `unity_script_create` / `unity_script_update` | 创建或更新脚本 |
| 执行 C# 代码 | `unity_execute_code` | 在编辑器上下文执行任意代码 |
| 截图验证 | `unity_graphics_scene_capture` / `unity_graphics_game_capture` | 截取场景/游戏视图 |
| 项目信息 | `unity_project_info` | 包列表、渲染管线、构建设置 |
| Play Mode | `unity_play_mode` | 进入/暂停/停止播放 |

> **⚠️ 重要**：所有 MCP 工具调用时请携带当前 Unity 实例的实际端口参数；先用 `unity_list_instances` 发现端口，再把该端口透传给后续工具。

### 备用方案

如果 MCP Server 不可用（如 Node.js 未安装、进程未启动），可回退到 HTTP 直连：

```powershell
curl.exe -s --max-time 10 http://127.0.0.1:<端口>/api/compilation/errors
curl.exe -s --max-time 5  http://127.0.0.1:<端口>/api/ping
```

## 注意事项

1. **本地冻结**：Unity 插件是本地冻结版本（非 git URL 引用），含 Unity 2021.3 兼容补丁。详见 `Packages/com.anklebreaker.unity-mcp/README_MINIGAME_PATCH.md`
2. **仅本地访问**：Bridge 绑定 `127.0.0.1`，不暴露到网络
3. **Undo 支持**：所有编辑操作支持 Unity Undo 系统
4. **多 Agent 安全**：多个 Agent 同时连接时，请求会排队执行
