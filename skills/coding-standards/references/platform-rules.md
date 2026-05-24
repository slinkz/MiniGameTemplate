# 平台与框架编码铁律

> 归档自 MEMORY.md 项目级铁律（2026-05-24 迁移）。编码时按需加载。

---

## Material / Shader（P0）

| ID | 规则 | 原因 |
|----|------|------|
| RULE-001 | `new Material(source)` 后必须显式赋值 `shaderKeywords` | ADR-032：关键字不复制导致变体不匹配 |
| RULE-028 | 自定义 Shader + 运行时 `new Material()` 必须加 `AlwaysIncludedShaders` | `Graphics.DrawMesh` 动态材质不在引用追踪链上，真机 WebGL 打包剥离 shader |
| RULE-028b | 不要声明 `multi_compile_instancing`（如果实际不用 GPU Instancing） | 引入无法匹配的变体 |

## ScriptableObject（P0）

| ID | 规则 | 原因 |
|----|------|------|
| RULE-002 | SO 不能引用场景对象 | 序列化后变 null（已在 §1 提及，此处强调场景引用） |

## 跨场景传参（P0）

> 已有 §9 + `references/cross-scene-data.md` 详细说明，此处仅索引。

| ID | 规则 |
|----|------|
| RULE-004 | 跨场景传参**唯一正确路径** = `AppFlowNavigator + IFlowData` |

## jslib / Emscripten（P0 — 微信小游戏 WebGL）

| ID | 规则 | 原因 |
|----|------|------|
| RULE-006 | jslib 辅助函数一律挂 `window.__wxBridgeHelpers` | 不放 mergeInto 块外裸函数，否则 Emscripten 链接失败 |
| RULE-007 | jslib 禁止 `err` 作为参数名 | Emscripten 内部保留 `err` 变量，冲突导致静默覆盖 |
| RULE-016 | `cloud.init` 必须先于任何 `CallCloudFunction` | jslib `mergeInto` 内不能调外部函数 |

## 异步与状态管理（P0）

| ID | 规则 | 原因 |
|----|------|------|
| RULE-009 | 启动异步操作必须在 `return` 前同步标记状态 | 防止重入/竞态 |
| RULE-010 | `TryRestoreNavigationStackAsync` 一律清空 + `return false` | 冷启动清栈，防止恢复到非法页面 |

## CDN 配置（P0 — 微信小游戏）

| ID | 规则 | 原因 |
|----|------|------|
| RULE-011 | CDN **单一数据源**：只在微信转换面板配置 | 运行时 `WXDataCDNHelper.GetDataCDN()` 读取，不要硬编码 |
| RULE-012 | CDN 域名必须在微信后台加白名单 | `request合法域名` + `downloadFile合法域名` 都要加。开发者工具 `urlCheck:false` **不绕过真机**。详见 `WECHAT_INTEGRATION §CDN缓存策略 ¶6` |

## UI 层级（P1）

| ID | 规则 | 原因 |
|----|------|------|
| RULE-008 | LoadingMask(9000) > ConfirmDialog(700) | NetworkRetryService 自动协调互斥，新增弹框时注意层级 |

## 云存档（P0）

| ID | 规则 | 原因 |
|----|------|------|
| RULE-017 | **云存档 V4 = 云端权威 + 纯内存** | 微信环境永不信本地。启动 Pull 云端→内存，写入 内存→Upload 云端。网络断则内存重试，进程被杀丢失（可接受）。编辑器走 PlayerPrefsSaveSystem |
| RULE-018 | 云函数必须同步客户端数据结构 | `SharedProgressData` 增删字段 → 云函数必须同步。详见 `references/cloud-sync-pitfall.md` |

## 微信小游戏诊断（P1）

| ID | 规则 | 原因 |
|----|------|------|
| RULE-029 | 真机诊断必须用 `Debug.LogWarning` | 微信开发者工具 Console 默认不显示 `console.log`（Unity `Debug.Log` 映射），只显示 `warn`/`error` |
