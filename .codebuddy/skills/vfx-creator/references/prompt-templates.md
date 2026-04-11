# Sprite Sheet Prompt 模板

以下模板用于生成验证样本或占位用 Sprite Sheet。重点不是追求终稿美术，而是先做出可验证、可区分的样本。

---

## 1. 爆炸类（高对比验证版）

### 用途
- 命中爆炸
- 多类型切换验证
- Danmaku 命中特效

### 模板
```text
Create a 2D sprite sheet for a stylized explosion effect, top-down game VFX, 8 frames arranged in a horizontal strip, transparent background, strong high-contrast electric blue core with bright cyan outer flame, exaggerated radial burst silhouette, each frame clearly readable, no UI, no text, no background scene, optimized for Unity sprite sheet playback.
```

### 变体建议
- 蓝色验证版：electric blue + cyan
- 红色验证版：hot orange + red
- 紫色验证版：violet + magenta

---

## 2. 烟雾类（持续扩散版）

### 用途
- 爆炸后残留
- 环境装饰
- 技能释放尾烟

### 模板
```text
Create a 2D sprite sheet for a stylized smoke puff effect, 10 frames arranged in a horizontal strip, transparent background, soft gray smoke with slightly blue shadow tones, expanding outward over time, readable silhouette in every frame, no text, no background, suitable for Unity sprite sheet animation.
```

### 变体建议
- 冷色烟雾：gray + blue shadow
- 热色烟雾：dark gray + ember orange edge

---

## 3. Buff / 治疗闪光类

### 用途
- 治疗触发
- buff 获得
- 状态刷新

### 模板
```text
Create a 2D sprite sheet for a magical healing flash effect, 8 frames arranged in a horizontal strip, transparent background, bright green and gold light burst with circular sparkles, clean readable silhouette, top-down game style, no text, no background, suitable for Unity sprite sheet playback.
```

### 变体建议
- 治疗：green + gold
- 护盾：cyan + white
- buff 强化：yellow + orange

---

## 使用规则

1. 先拿高对比版本做验证，不要一上来追求细腻终稿
2. 如果要验证“类型切换”，至少准备两套肉眼一眼能分开的颜色方案
3. 生成后要记录：帧数、排列方向、是否透明背景、建议缩放值
