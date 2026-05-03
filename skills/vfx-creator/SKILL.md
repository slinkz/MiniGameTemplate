---
name: vfx-creator
description: >
  MiniGameTemplate 项目的 Sprite Sheet VFX 工作流 Skill。
  当用户要新增特效、创建爆炸/烟雾/buff 等装饰型特效、整理 VFX SOP、
  或把特效接入 VFXDemo / DanmakuDemo / 业务命中链路时，应加载此 Skill。
  它固化了需求分型、验证样本设计、Unity 接入、运行验证、收尾沉淀的完整流程。
---

# VFX Creator — MiniGameTemplate Sprite Sheet VFX 工作流

## 适用范围

优先处理以下任务：

1. **装饰型 Sprite Sheet 特效**
   - 爆炸
   - 烟雾
   - 火花
   - buff 光环
   - 治疗闪光
   - 命中特效

2. **VFX 工作流固化与复用**
   - 编写/更新 VFX SOP
   - 给新会话提供标准执行路径
   - 沉淀 prompt 模板、接线清单、验证清单

3. **VFX 系统接入**
   - 接入 `SpriteSheetVFXSystem`
   - 接入 `VFXTypeRegistrySO`
   - 接入 `VFXDemo` / `DanmakuDemo`
   - 接入业务命中/触发链路

## 不适用范围

以下情况不要硬走本 Skill 主路径：

1. **弹幕本体渲染**
   - 子弹主体
   - 激光主体
   - 喷雾主体
   - 大量持续存在的弹幕表现

   这些优先走 `DanmakuSystem` 原生渲染链，不要硬塞进 `SpriteSheetVFXSystem`。

2. **UI 白模 / FairyGUI 界面生成**
   - 这类任务交给 `fairygui-tools`

3. **高保真粒子系统调优**
   - 当前模板主推 Sprite Sheet VFX，不主推粒子系统宇宙大一统

---

## 核心原则

### 1. 先分型，再动手
先判断任务属于哪类：

- 装饰型特效 → `SpriteSheetVFXSystem`
- 弹幕本体表现 → `DanmakuSystem`
- UI 特效 → 轻量 UI 方案 / 少量粒子

### 2. 先做可验证样本，再做正式资产
如果目标是验证“有没有切到另一种特效”，样本必须先满足肉眼强可区分：

- 颜色差异大
- 尺寸差异大
- 轮廓差异大
- Blend / Layer 差异明显
- 帧节奏明显不同

**禁止**直接拿视觉差异很弱的样本做逻辑验证。

### 3. 先打通最小闭环，再谈抽象
优先让真实业务链路先播出来，再决定要不要抽象成事件通道或更复杂架构。

### 4. 不在 Boot 场景做 VFX 验证
VFX 验证必须放在：

- `Assets/_Example/VFXDemo/`
- `Assets/_Example/DanmakuDemo/`
- 或新的专用 Example / Demo 场景

### 5. 收尾必须包含硬验收
代码改动完成后，必须：

1. 按项目 code review 清单自检
2. 跑 Unity batchmode 冷启动编译

---

## 标准工作流

### 阶段 A：任务分型
先回答 4 个问题：

1. 这是装饰型特效，还是弹幕本体表现？
2. 目标是做新特效，还是接入现有业务链路？
3. 当前有没有现成美术资源？
4. 当前验收场景是 `VFXDemo`、`DanmakuDemo`，还是新的 Example 场景？

如果是装饰型特效，继续本 Skill 主路径。

### 阶段 B：定义验证样本
如果没有正式资源，先准备验证样本：

- AI 生成 Sprite Sheet
- 程序生成占位图集
- 或使用已有测试图集

验证样本必须写清：

- 颜色主调
- 帧数
- 是否循环
- 预期尺寸
- 是否需要高对比验证版

### 阶段 C：落地到 Unity
按以下顺序操作：

1. 准备图集 / 材质 / `VFXTypeSO`
2. 注册到 `VFXTypeRegistrySO`
3. 接到 `SpriteSheetVFXSystem`
4. 决定放在 `VFXDemo` 还是业务 Demo
5. 如果是业务接入，只做最小可用链路

### 阶段 D：验证
验证顺序固定：

1. **先看接线**
   - `SpriteSheetVFXSystem` 是否已接
   - `VFXTypeSO` 是否已接
   - `TypeRegistry` 是否包含该类型

2. **再看选择链路**
   - 当前选中的类型是不是目标类型
   - 输入/触发逻辑是不是走到了目标分支

3. **再看运行时映射**
   - `RuntimeIndex`
   - Registry 映射
   - 池内数据是否正确

4. **最后看视觉表现**
   - 材质
   - Layer
   - Blend
   - Tint
   - 图集本身是否导致“其实切了但看不出来”

### 阶段 E：收尾
至少同步以下几项中的相关内容：

- `CHANGELOG.md`
- 对应 Example README
- `.tasks/active/*.md`
- 必要时 `Docs/Agent/*`
- 必要时 `known-pitfalls.md`

---

## Demo 场景规则

### VFXDemo
适合验证：

- 新建 VFX 类型是否能正常播放
- 多类型切换是否生效
- 播放参数是否合理

### DanmakuDemo
适合验证：

- 命中点触发特效
- 玩家受击特效
- 与弹幕系统的最小闭环集成

### 当前已验证的命中特效链路
当前项目已验证通过：

- `DanmakuSystem` 检查器字段：
  - `_hitVfxSystem`
  - `_hitVfxType`
  - `_hitVfxScale`
- `boss -> player` 命中时可播放命中特效

后续新增命中特效时，优先复用这条链路，而不是重新造一套。

---

## 输出要求

执行本 Skill 时，优先交付这些可落地结果：

1. 新增/修改的 VFX 资产清单
2. 场景接线清单
3. 验证步骤
4. 文档更新项（至少检查对应 Demo README 是否需要同步）
5. 若本轮改动涉及 `[SerializeField]` 默认值变更或新增字段，附带场景/预制体实例序列化同步结果（不要假设 Unity 会自动回写）
6. 若有代码改动，附带编译验收结果


不要只停留在概念建议，必须给出能直接落地的文件或操作结果。

---

## 参考资料

- `references/workflow.md` — 完整工作流说明
- `references/validation-checklist.md` — 短版验证清单
- `references/prompt-templates.md` — 常用 Sprite Sheet prompt 模板
- `assets/task-template.md` — 新 VFX 任务记录模板
