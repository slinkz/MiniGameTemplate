# Bug 修复 SOP（修改代码前必须执行）

> 归档自 coding-standards SKILL.md §10，按需加载。

## Step 0: 定位问题类型

先判断 bug 属于哪种类型：

| 类型 | 表现 | 诊断方向 |
|------|------|----------|
| 数据流断裂 | 数据从 A 到 B 传不过去 | 画完整链路图，找断点 |
| 时序错误 | 有时好使有时不好使 | 列出生命周期调用顺序（Awake→OnEnable→Start→OnFlowEnter） |
| 逻辑错误 | 稳定复现但结果不对 | 写出期望 vs 实际的对比 |
| 配置遗漏 | 功能存在但未生效 | 检查 Inspector 绑定、SO 引用、场景设置 |

## Step 1: 确认框架是否已提供解决机制

**在动手写任何代码之前，先回答这个问题：**

> "这个项目已有的框架/机制中，是否已经提供了解决这个问题的正确路径？"

- 如果有 → 用框架提供的路径，找出为什么它没生效
- 如果不确定 → 读 TDD / ARCHITECTURE 文档，或**问天命人**
- **绝不允许**在不确定的情况下自己发明一条路

## Step 2: 禁止事项 Checklist

修复方案写好后，逐项验证：

- [ ] 没有用 SO 当全局变量
- [ ] 没有用 static 传参
- [ ] 没有用 Resources.Load 当通信手段
- [ ] 没有引入 Editor-only API（AssetDatabase、EditorUtility 等）
- [ ] 修复方案在真机（WebGL/微信小游戏）环境下同样有效
- [ ] 没有绕过现有框架另起炉灶
- [ ] 修改不会在场景常驻/热重载时产生状态残留

## Step 3: 真机可行性检查

微信小游戏环境**不支持**：
- `System.IO.File.*`（用 WX SDK 的 FileSystemManager）
- `AssetDatabase.*`（Editor-only）
- `Thread`（WebGL 单线程）
- 同步阻塞 IO

## 红线

> 如果觉得框架不支持当前需求，**先提出框架改进方案，等天命人确认**。
> 不要自己 hack。这是铁律。
