---
name: code-review-checklist
description: >
  MiniGameTemplate 项目的代码审查检查清单 Skill。此 Skill 应在以下场景加载：
  (1) 任何代码改动后进行代码审查；(2) 大段代码一次性生成后的完整性验证；
  (3) 涉及 FairyGUI、Unity API、跨文件重构等高风险操作时。
  此 Skill 基于项目历史踩坑经验，提供分类检查清单，逐项验证以降低编译错误和运行时 bug。
---

# Code Review Checklist — MiniGameTemplate

## 审查 SOP

1. **读取踩坑经验库**：必读 `references/known-pitfalls.md`（活跃层），按需读 `references/known-pitfalls-archive.md`
2. **常规四维审查**：正确性 → 安全性 → 可维护性 → 性能
3. **CL-1~CL-10 逐项过**（见下方）
4. **MCP 编译验证**：先 `AssetDatabase.Refresh()` 再 `unity_get_compilation_errors`（不 Refresh = 旧缓存假阳性）
5. **输出审查报告**（模板见 `references/maintenance-guide.md`）

---

## 检查清单 CL-1 ~ CL-10

### CL-1: 跨文件引用完整性
A 文件引用 B 的成员 → grep 确认 B 中确实存在。新增/重命名字段后全局搜索旧名。

### CL-2: 命名空间安全
自定义命名空间不得与 Unity/System 内置类重名。跨命名空间引用考虑 `global::` 前缀。

### CL-3: Unity API 版本兼容
版本敏感 API 确认目标版本（2022 LTS）可用。添加 `#if` 条件编译后全局搜索同一 API 确保无遗漏。

### CL-4: 字符串级引用验证
场景名、FairyGUI 包名/组件名、资产路径、PanelPackageName → 与磁盘实际文件逐一比对。

### CL-5: 生命周期与时序
- `OnOpen` 绑事件 → `OnClose` 必须解绑
- `OnRefresh` **禁止调 `OnOpen`**（事件双绑定 P0）
- ClosePanel 后闭包引用检查

### CL-6: 渲染管线与 Mesh API
- 顶点结构字段顺序 = `VertexAttributeDescriptor[]` 声明顺序（Position→Color→UV）
- 材质纹理绑定、Shader 属性名匹配
- 对象池 `FreeAll()` 与 `Free(i)` 数据清零一致性
- Registry 白名单未命中 → Error（非 Warning）
- 配置开关必须被执行层真实消费（禁止假开关）

### CL-7: FairyGUI 改动完整性
**操作顺序**：①改 FairyGUI XML → ②导出 C# → ③改 Logic.cs。禁止反向操作。
- 移动包时同步 `codePath`、C# 文件、namespace、asmdef 引用
- Logic.cs 属性名/类型必须与 Extension 类同步

### CL-8: 第三方库命名空间验证
grep 第三方源码实际 `namespace` 声明，不要想当然。wrapper/fork 库可能修改过原命名空间。

### CL-9: 架构一致性（🔴 阻塞级）
- 跨场景传参走 `AppFlowNavigator + IFlowData`，禁止 SO/static/Resources/PlayerPrefs hack
- 修复在框架机制内，不另起炉灶；框架不支持→先提方案等天命人确认
- 持久化数据增删字段 → 云函数 `saveProgress`/`getProgress` 必须同步（JS 解构静默丢弃）

### CL-10: 方法重载安全（🔴 阻塞级）
新增重载后全局搜索所有调用点。重点检查 `null`/`default`/`0` 是否匹配多个重载（CS0121 歧义）。

---

## 踩坑追加协议

新 bug 修复后 → 追加 PIT 记录到 `references/known-pitfalls.md` → 评估是否需要新增/补充 CL 条目。

经验库维护规范（蒸馏规则、容量阈值）和审查报告模板见 `references/maintenance-guide.md`。

---

## 归档索引

| 文件 | 内容 |
|------|------|
| `references/known-pitfalls.md` | 踩坑经验活跃层（必读） |
| `references/known-pitfalls-archive.md` | 踩坑经验归档层（按需） |
| `references/maintenance-guide.md` | 经验库蒸馏规则 + 审查报告模板 + 追加协议 |
