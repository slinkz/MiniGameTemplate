---
system: knowledge-engineering
scope: module-card-datasystem-so-luban
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/SO_Config_Workflow.md, Docs/Agent/CONTEXT_PACKS/WeChat_Build_Cloud.md
---

# Module Card: DataSystem_SO_Luban

## 1. 模块职责

DataSystem/SO/Luban 负责项目数据来源和持久化：`ISaveSystem`、`CloudSaveSystem`、PlayerPrefs fallback、SO 配置资产、Luban 表定义/生成/加载，以及 ShooterGame 进度、关卡、波次、Entity 配置的数据闭环。

## 2. 不负责什么

- 不决定战斗规则本身，战斗规则由 ShooterGame / EntitySystem 消费数据。
- 不直接调用微信 JS，云能力通过 WeChatBridge。
- 不替 UI 展示进度或错误，只提供数据状态和回调。
- 不把场景对象写进 ScriptableObject。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `ISaveSystem` | 存档接口 |
| `CloudSaveSystem` | 微信环境云端权威 + 纯内存进度存储 |
| `PlayerPrefsSaveSystem` | Editor/非微信 fallback |
| `ConfigManager` | Luban 配置加载入口 |
| `TablesExtension` | Luban 表名维护 |
| `EntityConfigSO` / `SkillConfigSO` / `BuffConfigSO` | Entity/ShooterGame SO 配置 |
| `SG_ProgressManager` | ShooterGame 进度消费方 |

## 4. 数据流

```text
SO asset / Luban xlsx
  -> Validator / gen_config
  -> Runtime config load
  -> Entity / ShooterGame / UI consume

SG_ProgressManager
  -> ISaveSystem
  -> PlayerPrefsSaveSystem 或 CloudSaveSystem
  -> WeChatBridge / CloudFunctions
```

## 5. 生命周期

```text
Bootstrap 初始化配置与存档服务
  -> 读取配置表 / SO
  -> 云环境启动 Pull
  -> 游戏运行读写内存状态
  -> 通关或关键事件 Save/Upload
  -> Reload / Retry / Return 验证状态一致
```

## 6. 依赖关系

DataSystem 是框架基础层。ShooterGame、EntitySystem、UI 消费数据；DataSystem 不反向依赖具体游戏业务。CloudSaveSystem 可依赖 WeChatBridge，但微信平台细节不应泄漏到普通业务逻辑。

## 7. 关键配置 / 资产路径

```text
UnityProj/Assets/_Framework/DataSystem/
UnityProj/Assets/_Framework/DataSystem/Scripts/Config/
UnityProj/Assets/_Framework/DataSystem/Scripts/Persistence/
UnityProj/DataTables/
UnityProj/Tools/gen_config.bat
UnityProj/Assets/_Game/ConfigData/
UnityProj/Assets/_Game/Configs/
skills/luban-config/
```

## 8. 关键 ADR / 约束

- ADR-033：Entity 配置可由 SO 驱动，保留 Luban 迁移/桥接空间。
- ADR-034：进度、关卡与 UI/场景流转不能绕过 AppFlow 语义。
- WebGL/微信环境云存储遵循平台约束，不能使用阻塞 IO 或未验证 API。
- SO 是项目级资产，不引用场景对象；Luban 生成代码和 bytes 不手改。

## 9. 热路径 / 平台约束

- Tick/碰撞/渲染热路径不查表、不做字符串拼接、不反复解析配置。
- 配置加载在启动或初始化阶段完成，运行时使用缓存引用。
- 微信环境 CloudSaveSystem 当前语义是云端权威 + 纯内存；Editor fallback 与微信路径隔离。
- Luban 生成必须同步 C#、bytes、JSON 预览和 TablesExtension。

## 10. 常见错误

- 新增 SO 类型但未补 Validator、模板资产或创建流程。
- 修改 SO 字段后忘记自定义 Inspector 和现有资产迁移。
- 新增 Luban 表后忘记更新 `TablesExtension` 或运行 `gen_config`。
- 只改关卡/波次配置，忘记进度解锁、保存、Reload、云同步验证。
- 云端权威与本地缓存混用，导致跨设备进度不一致。

## 11. 修改前必读

- `CONTEXT_PACKS/SO_Config_Workflow.md`
- `CONTEXT_PACKS/WeChat_Build_Cloud.md`
- `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX.md`
- `SHOOTER_GAME/TDD/SG_TDD_06_CLOUD_SAVE.md`
- `skills/luban-config/SKILL.md`
- `MODULE_CARDS/ShooterGame.md`
- `MODULE_CARDS/EntitySystem.md`

## 12. 修改后必验

- SO Validator / Missing Reference 通过。
- 新增或修改关卡、波次、敌人、技能、Buff/DOT 后能在运行时读到配置。
- 涉及 Luban 时运行生成脚本，确认 Generated C#、bytes、JSON 预览和 `TablesExtension`。
- 进度相关改动验证解锁、保存、Reload、离线/重试、云同步。
- 微信路径验证登录、Pull、Upload、冲突/离线；Editor fallback 不受影响。
