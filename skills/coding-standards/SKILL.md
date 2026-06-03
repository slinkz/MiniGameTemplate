---
name: coding-standards
description: >
  MiniGameTemplate 项目的 C# 编码规范。此 Skill 应在以下场景 **编码前** 加载：
  (1) 编写新的 C# 脚本之前；(2) 修改现有 C# 代码之前；(3) 生成代码模板之前。
  与 code-review-checklist（编码后审查）互补——本规范关注"怎么写"，审查清单关注"写完检查什么"。
  触发关键词：编码规范、代码规范、coding standards、coding convention、编码前。
---

# C# 编码规范 — MiniGameTemplate

**编码前必须加载**此 Skill。每次写新代码或修改现有代码前，快速过一遍。

---

## §1 Unity 序列化陷阱（P0）

- **禁止对 UnityEngine.Object 派生类型使用 `??` / `?.` / `??=`**（fake-null 不触发）→ 一律用 `!= null` 显式判断
- 新增 `[SerializeField]` 后检查引用该类型的 `.unity` / `.prefab`
- **SO 禁止存储场景实例引用**（序列化后变 null）

## §2 命名空间安全

- 继承 `Editor` 基类一律用 `UnityEditor.Editor` 全限定名
- 程序集：`MiniGameFramework.Runtime` / `MiniGameFramework.Editor` / `Game.Runtime`，跨程序集引用须在 `.asmdef` 声明

## §3 ScriptableObject 驱动设计

- 跨系统共享数据必须存 SO，禁止静态变量/跨场景 MonoBehaviour
- 跨系统通信走 `GameEvent : ScriptableObject` 事件通道，禁止 Find/单例
- 自定义 SO 添加 `[CreateAssetMenu]`

## §4 单一职责

- MonoBehaviour < 150 行，超长则拆
- 预制件自包含，不依赖场景层级中其他对象

## §5 性能规范

- **热路径零 GC**：禁止 string 拼接、new 引用类型、LINQ、foreach（非数组）
- 事件驱动优先于 Update 轮询
- 频繁创建销毁对象使用对象池（BulletWorld / SpriteSheetVFXSystem）

## §5b Coroutine/Async 退场时序（P0）

**核心规则：清理子系统后、yield/await 之前，必须立即切状态。**

- Coroutine 中 `_onBattleEnd.Raise()` 清理了子系统（DespawnAll/StopAll），但 `Update()` 仍在运行
- 如果 `CurrentState` 未改变，中间帧的 `TickPlaying()` 会看到空 Entity 列表 + IsAllWavesCleared=true → **误判通关**
- **铁律**：`Raise()` 后第一行必须是 `CurrentState = BattleState.None`（或其他非 Playing 状态），然后才能 yield/await
- **OnDestroy 兜底**：用 `_battleCleanupRaised` 标记位防止双重 Raise；正常路径已 Raise 则 OnDestroy 跳过
- **Retry 特殊**：Retry 不卸载场景，Raise 后不置位标记（新一局需要重新允许清理）

> 也适用于 async void 方法中的 `await Task.Yield()` / `await ...Async()` 前

## §6 编辑器代码

- 修改 SO 时调用 `EditorUtility.SetDirty(target)`
- CustomEditor 用 `SerializedProperty` 遍历字段
- **⚠️ 自定义 Editor 同步安全（P0）**：
  - **铁律：修改任何有 `[CustomEditor]` 的类时，必须同步检查其 Editor 代码是否需要更新**
  - 触发场景：增删字段、改字段类型、重命名字段、修改枚举定义、调整序列化结构
  - 检查项：① 字段遍历逻辑 ② 硬编码的 `FindProperty("name")` ③ 自定义绘制逻辑 ④ 枚举映射
  - grep `[CustomEditor(typeof(你改的类))]` 找到所有关联 Editor
  - **隐蔽性**：Inspector 显示正确 ≠ 数据正确（读写用相同错误映射时自洽假象）
- **枚举序列化子规则**：
  - `enumValueIndex` ≠ `(int)enumValue`，跳号枚举必错位
  - 读用 `prop.intValue` + cast，写用 `prop.intValue = (int)value` 或查表映射

## §7 防御性编程

- 禁止魔法字符串（标签/层/动画器参数用 `const string`）
- `DontDestroyOnLoad` 限制使用，须在代码审查中说明理由

## §8 文件组织

- 一个文件一个公开类型（partial 除外）
- SO 资产放 `Assets/ScriptableObjects/` 按域分子目录
- 编辑器脚本放对应模块 `Editor/` 子目录

---

## §9 跨场景数据传递（P0）

**唯一正确路径**：`AppFlowNavigator + IFlowData`。禁止 SO 当全局变量/static/Resources/PlayerPrefs/DontDestroyOnLoad 传临时数据。

直跑场景 = 测试模式，控制器应有 fallback 默认值，禁止写入存档。

> 详细代码示例和禁止方式表格：`references/cross-scene-data.md`

## §10 Bug 修复 SOP（P0）

修 bug 前必须：①定位问题类型 ②确认框架是否已提供机制 ③修复方案过禁止事项 checklist ④真机可行性检查。

**红线**：框架不支持当前需求时，先提方案等天命人确认，不要 hack。

> 详细步骤：`references/bug-fix-sop.md`

## §11 最低可视化原则（P0）

**没有显示 = 功能未完成**。每个运行时实体/效果必须有最低限度的可视组件：
- 新增实体 → 至少占位 Sprite
- 新增 Buff → 至少有可观测表现（图标/颜色/粒子）
- 新增碰撞 → 碰撞体有对应可视边界
- **判定标准**：天命人在 Game 视图中看不到变化 = 未完成

## §12 云函数必须同步客户端数据结构（P0）

`SharedProgressData` 增删字段时，`saveProgress` / `getProgress` 云函数**必须同步更新**。JS 解构不报错→新字段静默丢弃→数据丢失。

> 详细 checklist 和原理：`references/cloud-sync-pitfall.md`

## §13 系统退役必须物理删除（P0）

**当系统 B 完整替代系统 A 时，A 的代码 + 配置字段必须在同一个改动中删除。**

- `[Obsolete]` 标记 ≠ 删除。只要代码还存在，就有人（包括 AI）会读它。编译报错是最好的自检
- 执行清单：① 对每个公开字段 `Find All References` ② 确认引用已迁移/删除 ③ 删除旧类+字段+Editor ④ 编译通过=完成
- ❌ 禁止"先标 Obsolete 以后再删" / "留着兼容" / 只看类引用不看字段引用

## §14 伤害飘字必须走 FloatingTextSystem（P0）

**战斗伤害数字必须走 `FloatingTextSystem`（RBM 渲染，`MiniGameTemplate.Rendering` 命名空间），禁止 FairyGUI/TextMesh 对象池。颜色语义由调用方通过 `FloatingTextColors` 常量传入，系统内部不硬编码颜色。**

## §15 Sprint 验收防御机制（P0 · ADR-037）

**编译通过 ≠ 功能交付。Agent 自验不具备最终裁判权。**

### 15.1 TDD 交付物清单

每个 TDD 子任务（G-item）**必须**在任务定义时列出"交付物文件路径清单"：

```markdown
| ID | 功能 | 交付物文件 |
|----|------|-----------|
| G5 | 技能CD HUD | BattleSkillCDHUD.cs, BattleSkillCDHUDExtension.cs |
```

### 15.2 Gate-0 文件存在性扫描

Sprint 验收的**第一步**（Gate-0）：逐一检查交付物清单中的文件是否存在于磁盘。

- **扫描工具**：`codegraph_search` 或 `search_file` 按路径/类名搜索
- **缺文件 = 直接中止**：不进入功能验收，标记 "⬜ 未实现（文件不存在）"
- **禁止**：以"编译通过"或"无报错"作为文件存在的替代证据

### 15.3 验收三态标记

废弃旧的 ✅/空白 二态。所有验收结果只允许三种状态：

| 标记 | 含义 | 使用条件 |
|------|------|---------|
| ✅ PASS | 功能正确 | 文件存在 + 运行时行为符合验收标准 |
| ⬜ 未实现 | 代码不存在 | Gate-0 扫描未找到交付物文件 |
| ❌ FAIL | 存在但不正确 | 文件存在但行为不符合验收标准 |

### 15.4 传播隔离（天命人确认节点）

- Agent 的 Sprint 自验结果 → 写入 TDD 文档的验收总表
- **不自动传播**到 `DEVICE_ACCEPTANCE.md`（统一验收手册）
- 天命人在 H 区（真机/UI/性能统一验收）手动确认后才更新统一验收手册
- **铁律**：任何 "全部通过" 级别的结论必须经天命人确认，Agent 无权单方面宣布

---

## 附录：踩坑快速索引

| ID | 陷阱 | 级别 | 章节 |
|----|------|------|------|
| PIT-034 | `??`/`?.` 对 Unity Object 无效 | P0 | §1 |
| PIT-035 | `Editor` 命名空间与基类冲突 | P1 | §2 |
| PIT-036 | 跨场景传参绕过 Navigation 框架 | P0 | §9 |
| PIT-037 | 直跑场景测试模式写入真实存档 | P1 | §9 |
| PIT-038 | 纯逻辑实现无显示对象，无法验收 | P0 | §11 |
| PIT-041 | 云函数未同步数据结构，静默丢弃 | P0 | §12 |
| PIT-050 | 退场 Raise 后 yield 前未切状态→误判通关 | P0 | §5b |
| PIT-051 | 被替代系统未物理删除，旧字段隐性覆盖新系统 | P0 | §13 |
| PIT-052 | 改数据模型未同步自定义 Editor（枚举序列化错位为典型表现） | P0 | §6 |
| PIT-053 | FairyGUI/TextMesh 飘字性能差且不跟相机缩放，必须走 FloatingTextSystem（RBM） | P0 | §14 |
| PIT-054 | 编译通过≠功能交付，Agent 自验批量标通过但代码文件不存在 | P0 | §15 |

---

## 归档索引

| 文件 | 内容 |
|------|------|
| `references/cross-scene-data.md` | §9 详细代码示例 + 禁止方式表格 |
| `references/bug-fix-sop.md` | §10 Bug 修复 SOP 完整步骤 |
| `references/cloud-sync-pitfall.md` | §12 云函数同步 checklist + 原理 |
| `references/platform-rules.md` | 平台与框架编码铁律（Material/jslib/CDN/云存档/微信诊断等） |
