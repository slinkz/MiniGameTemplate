# 编码规范索引（CONVENTIONS）

> **适用范围**：MiniGameTemplate 全项目  
> **目标平台**：微信小游戏（WebGL）

---

## 子文件目录

| # | 文件 | 内容摘要 | 行数 |
|---|------|---------|------|
| 1 | [SYSTEMS/CONV/CONV_01_NAMING.md](SYSTEMS/CONV/CONV_01_NAMING.md) | 命名规范 · 代码风格 · FairyGUI面板 · 禁止事项 · 目录/路径/Git规范 | ~254 |
| 2 | [SYSTEMS/CONV/CONV_02_CODING.md](SYSTEMS/CONV/CONV_02_CODING.md) | 日志 · 错误处理 · 异步 · GC优化 · 框架系统使用 | ~277 |
| 3 | [SYSTEMS/CONV/CONV_03_PLATFORM.md](SYSTEMS/CONV/CONV_03_PLATFORM.md) | WebGL约束 · 安全编码 · 模块依赖 · SO设计 · 集合迭代 · 弹幕/Mesh/渲染规范 · 提交清单 | ~336 |
| 4 | [SYSTEMS/CONV/CONV_04_WORKFLOW.md](SYSTEMS/CONV/CONV_04_WORKFLOW.md) | 技术文档管理 · 变更包模板 · SDD借鉴 | ~146 |

---

## 核心原则速查

1. **零 GC 热路径** — Update/Tick 中禁止 new、LINQ、string 拼接
2. **WebGL 兼容** — 无线程、无 System.IO、无阻塞调用
3. **SO 不引用场景对象** — ScriptableObject 是项目级资产
4. **命名即文档** — 类名/变量名/SO名 遵循统一前缀约定
5. **每次提交过提交清单** — 见 CONV_03 末尾
