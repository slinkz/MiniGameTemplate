# VFX Creator Workflow

## 1. 任务分型

先判断任务属于哪类：

### A. 装饰型 Sprite Sheet 特效
适用：爆炸、烟雾、火花、buff 光环、治疗闪光、命中特效。

处理路径：
- 使用 `SpriteSheetVFXSystem`
- 使用 `VFXTypeSO`
- 使用 `VFXTypeRegistrySO`

### B. 弹幕本体表现
适用：子弹主体、激光主体、喷雾主体、大量持续存在的战斗表现。

处理路径：
- 优先使用 `DanmakuSystem` 原生渲染链
- 不要为了统一而强行塞进 Sprite Sheet VFX

### C. UI 特效
适用：按钮闪光、面板打开装饰、轻量前景特效。

处理路径：
- 优先轻量 UI 方案
- 粒子系统只在确实更合适时使用

## 2. 验证样本设计

### 原则
如果这次任务需要验证“是否切到了另一种特效”，验证样本必须先做到肉眼强可区分。

### 最低要求
至少满足以下一项，最好同时满足两项：
- 颜色明显不同
- 尺寸明显不同
- 轮廓明显不同
- Blend / Layer 明显不同
- 帧节奏明显不同

### 反例
不要用两个只是轻微色偏、轻微亮度差的爆炸图去验证类型切换。那是在给自己挖坑。

## 3. Unity 接入顺序

1. 导入图集
2. 创建材质
3. 创建 `VFXTypeSO`
4. 注册到 `VFXTypeRegistrySO`
5. 接到 `SpriteSheetVFXSystem`
6. 决定验证场景
7. 接入业务触发链路

## 4. 场景选择规则

### 禁止
- 在 `Boot` 场景直接做 VFX 验证

### 推荐
- `Assets/_Example/VFXDemo/`
- `Assets/_Example/DanmakuDemo/`
- 新建专用 Example 场景

## 5. 业务接入策略

### 推荐做法
先做最小闭环：
- 让真实业务链路能播出来
- 再决定要不要抽象成 SO 事件通道

### 当前项目已验证的例子
- `DanmakuSystem.Update()` 中调用 `PlayHitVFX()`
- `SpriteSheetVFXSystem.PlayOneShot(...)`
- `boss -> player` 命中时播放受击特效

## 6. 验证顺序

固定按下面顺序排查：

1. 接线是否完整
2. 类型选择是否正确
3. 运行时映射是否正确
4. 视觉表现是否足够可区分

## 7. 收尾要求

至少完成以下动作中的相关项：
- 更新 `CHANGELOG.md`
- 更新 Example README
- 更新 `.tasks/active/*.md`
- 必要时更新 Agent 文档
- 必要时追加 `known-pitfalls.md`
- 运行 Unity batchmode 冷启动编译
