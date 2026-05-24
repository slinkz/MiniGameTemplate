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

## §6 编辑器代码

- 修改 SO 时调用 `EditorUtility.SetDirty(target)`
- CustomEditor 用 `SerializedProperty` 遍历字段

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

---

## 归档索引

| 文件 | 内容 |
|------|------|
| `references/cross-scene-data.md` | §9 详细代码示例 + 禁止方式表格 |
| `references/bug-fix-sop.md` | §10 Bug 修复 SOP 完整步骤 |
| `references/cloud-sync-pitfall.md` | §12 云函数同步 checklist + 原理 |
