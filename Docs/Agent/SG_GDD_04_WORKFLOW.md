---
system: shootergame-gdd
scope: design-workflow
last_verified: 2026-05-18
depends_on: [SG_GDD_INDEX]
related_code: Assets/_Game/Scripts/ShooterGame/**, Assets/Editor/**
---

# 九、策划工作流设计

### 9.1 当前工作流（V1）

```
策划调参流程（V1）：
1. 打开 Unity Editor
2. 找到 EntityConfigSO 资产
3. 在 Inspector 里改数值
4. 进入 Play Mode 验证
5. 不对就退出、改、再 Play
```

**痛点**：
- 技能效果配置散落在多个 SO 中（SkillConfigSO + BulletPatternSO + BuffConfigSO）
- 新增技能需要 3 步：创建 SO → 配置参数 → 挂到 EntityConfigSO
- 没有整体预览——改了一个参数不知道对全局平衡的影响

### 9.2 V2 策划工具需求

| # | 工具 | 优先级 | 用途 |
|---|------|--------|------|
| T1 | **技能预览窗口** | P0 必做 | 选中 SkillConfigSO → 在 Scene View 预览弹幕效果（含被动模拟开关） |
| T2 | **Buff 速览面板** | P0 必做 | 一览所有 BuffConfigSO 的属性修正值，快速对比 |
| T3 | **掉落概率计算器** | P1 应有 | 输入 DropTableSO → 输出各道具实际掉落概率 |
| T4 | **DPS 计算面板** | P1 应有 | 选中 EntityConfigSO → 输出"裸 DPS"+"含被动期望 DPS" |
| T5 | **ID 冲突检测** | P0 必做 | 构建前校验 BuffId / DotId 唯一性 + 范围合规 |
| T6 | **SO 批量创建向导** | P2 锦上添花 | 输入 SO 类型 + 命名模板 + 数量 → 一键生成到正确目录 |
| T7 | **BulletPattern 预览** | P0 必做 | V1 已提出，V2 必须做——在 Scene View 预览弹幕发射效果 |
| T8 | **SO 一致性构建验证器** | P0 必做 | 构建前校验：路径规则、命名前缀、引用完整性（v1.5 PK 新增） |
| T9 | **PickupConfigSO 自定义 Inspector** | P1 应有 | 根据 PickupType 动态显隐字段，防止误填（v1.5 PK 新增） |
| T10 | ~~**AI 行为编辑器**~~ | ~~P2~~ | ~~已移除（v1.6 敌机无 AI）~~ |
| T11 | **DropTable 概率模拟器** | P2 应有 | `[ContextMenu("Simulate 10000")]` 跑 10000 次 → Console 输出掉落分布（v2.1 工具 PK 新增） |
| T12 | **技能测试场景** | P3 应有 | `SkillTestScene` + EditorWindow 选择单个技能触发测试，不需从头打局（v2.1 工具 PK 新增） |
| T13 | **一键创建被动+关联 Buff** | P3 应有 | PassiveConfigSO Inspector 上 [Button] → 自动创建 `Buff_Passive_{名}` SO + 互相引用（v2.1 工具 PK 新增） |

### 9.3 技能预览窗口规格（T1）

```
┌───────────────────────────────────────────┐
│  技能预览器                   [▶预览] [■停止] │
├───────────────────────────────────────────┤
│                                           │
│  SkillConfigSO: [拖拽或选择]              │
│                                           │
│  ┌─ Scene View ──────────────────────┐    │
│  │                                    │    │
│  │    ● ← 虚拟施法者                  │    │
│  │    │                               │    │
│  │    ↑ ↗ ↑ ← 弹幕发射效果           │    │
│  │                                    │    │
│  │    ⊙ ⊙ ⊙ ← 虚拟敌人(可选)         │    │
│  │                                    │    │
│  └────────────────────────────────────┘    │
│                                           │
│  参数速览：                                │
│  · CD: 2.0s  前摇: 0.3s  后摇: 0.5s      │
│  · Effects: FireBulletsEffect x1           │
│  · 理论 DPS: 25/s                         │
│                                           │
│  [应用修改]  [重置默认值]                   │
└───────────────────────────────────────────┘
```

**交互规格**：
- 不进入 Play Mode，在 Editor 模式下模拟 DanmakuSystem 发射
- 虚拟施法者在 Scene View 中心，可拖拽位置
- 可选放置虚拟敌人观察追踪弹效果
- 实时修改参数后立刻刷新预览

**被动模拟面板**（v1.5 PK 新增）：
- 复选框勾选 0~3 个被动 → 预览器调整弹幕行为
  - ☑ 穿透 → 弹幕穿透不消失
  - ☑ 暴击 → 偶发暴击闪白 + 大数字
- Buff 模拟留作 V3 T4 扩展（V2 工时不支持）

### 9.4 策划配置 SOP（V2 新增技能流程）

```
新增一个敌机类型（v1.6 简化）：
  1. 创建 EntityConfigSO → 填基础属性（HP、移动速度）
  2. 如有射击能力 → 创建 BulletPatternSO → 配子弹参数
  3. 在 EntityConfigSO 上引用 BulletPattern（如有）
  4. 添加到 EntitySpawnWaveSO 的波次中
  5. Play Mode 验证
  ※ 不需要 AI 配置——所有敌机统一直线下落

新增一个玩家技能（v1.6 更新）：
  1. 创建 SkillConfigSO → 填 CD/前摇/后摇
  2. 在 Effects 列表中添加 ISkillEffect（如 FireBulletsEffect）
  3. 配 BulletPatternSO 给 FireBulletsEffect
  4. 技能预览器验证效果
  5. Play Mode 验证
  ※ 技能全自动释放，不需要配置触发方式

新增一个 Buff：
  1. 创建 BuffConfigSO → 填 BuffId（范围 1000~3999）+ 属性修正 + 持续时间
  2. Buff 速览面板检查数值合理性
  3. 创建 PickupConfigSO → Type=Buff → 引用此 BuffConfigSO
  4. 或者挂到玩家技能的 ApplyBuffEffect 上
  5. Play Mode 验证

新增一个 DOT（v1.5 更新——DOT 为独立 SO）：
  1. 创建 DotConfigSO → 填 DotId（范围 4000~4999）+ Damage/Interval/Duration
  2. 设置 DamageType + VfxPrefab + Tag
  3. 在需要施加 DOT 的地方引用此 DotConfigSO（如特殊子弹命中事件）
  4. Play Mode 验证
```

### 9.5 编辑态 SO 验证框架（v1.5 PK 新增）

> **设计目标**：策划修改 SO 参数时，错误在 Inspector 中**立即**标红反馈，而非 PlayMode 才崩溃。
> **节省时间指标**：每次验证反馈从 2~5 分钟（PlayMode 往返）→ 0 秒（Inspector 即时）。

**实现方案**：每类 SO 实现 `OnValidate()` + `IValidatable` 接口。

| SO 类型 | 验证规则 | 严重度 |
|---------|---------|--------|
| SkillConfigSO | CooldownTime > 0 / Effects[] 非空 / Effects[].BulletPattern 非 null | 🔴 错误 |
| BuffConfigSO | BuffId ∈ [1000, 2999] / Duration > 0 / StackMode=Stack → MaxStacks ≥ 1 | 🔴 错误 |
| DotConfigSO | DotId ∈ [4000, 4999] / Damage > 0 / Interval > 0 / Duration > 0 | 🔴 错误 |
| PickupConfigSO | PickupType=Buff → BuffConfig ≠ null / PickupType=Repair → RepairAmount > 0 | 🔴 错误 |
| DropTableSO | Entries[] 非空 / 所有 Weight > 0 / BaseDropRate ∈ (0, 1] | 🟡 警告 |
| BulletPatternSO | Speed > 0 / Lifetime > 0 / BulletType 引用非 null | 🔴 错误 |
| EntityConfigSO | MaxHp > 0 / 至少有 1 个 Component / SkillConfigs 内无 null | 🟡 警告 |

**Inspector 展示**：OnValidate 失败时使用 `[HelpBox]` 属性或 `MessageType.Error` 在 Inspector 顶部显示红色/黄色错误信息。

**构建卡口**：T8（SO 一致性构建验证器）在 `IPreprocessBuildWithReport` 中扫描全部 SO，阻断包含错误的构建。

> **T8 验证深度**（v2.0 架构师 PK 新增）：
>
> ```
> L1：SkillConfigSO → Effects[] 非空、非 null
> L2：FireBulletsEffect → BulletPatternSO 非 null
>     BulletPatternSO → BulletTypeSO 非 null
> L3+：BulletTypeSO → Sprite → 交给 Unity 自身的 Missing Reference 检测
> ```
>
> T8 做到 **二级引用检查**（L1 + L2），确保策划的配置链路不断。三级及以上引用断裂（如 Sprite 被删）属于美术资源管理问题，由 Unity Editor 自身 missing reference 警告覆盖。
>
> **边界限制理由**：若无限递归最终会检查到 Shader/材质球/纹理——超出 SO 配置验证范畴。

### 9.6 ID 分配约定（v1.5 PK 新增）

| ID 范围 | 类型 | 说明 |
|---------|------|------|
| 1000~2999 | Buff（增益） | BuffConfigSO.BuffId |
| 3000~3999 | Debuff（减益） | BuffConfigSO.BuffId |
| 4000~4999 | DOT | DotConfigSO.DotId |
| 5000~5999 | V3 预留 | Aura / 新机制 |

> **铁律**：BuffId 和 DotId **不共用**命名空间。"清除所有减益"按 Tag 扫描，不依赖 ID 范围——但 ID 范围约定仍需遵守以便工具校验。
> T5 校验内容更新：BuffId 唯一且 ∈ [1000, 3999] / DotId 唯一且 ∈ [4000, 4999]。

---

# 十、美术工作流设计

### 10.1 V2 新增美术资源清单

| 类别 | 资源 | 规格 | 数量 | 优先级 |
|------|------|------|------|--------|
| **新敌机** | 射手机 sprite | 48×48 px | 1 | P0 |
| | 散射机 sprite | 56×56 px | 1 | P0 |
| | 精英机 sprite | 64×64 px | 1 | P1 |
| **敌方子弹** | 直射弹 sprite | 12×12 px | 1 | P0 |
| | 散射弹 sprite | 10×10 px | 1 | P0 |
| | 追踪弹 sprite | 14×14 px | 1 | P1 |
| **玩家子弹变体** | 散射弹 sprite | 14×14 px | 1 | P0 |
| | 追踪导弹 sprite | 16×20 px | 1 | P1 |
| | 激光 sprite（拉伸） | 8×64 px tile | 1 | P1 |
| **Buff 特效** | 攻速加成光环 | 序列帧 4~6 帧 | 1 | P1 |
| | 护盾球体 | 序列帧 4~6 帧 | 1 | P1 |
| | 减速冰霜 | 序列帧 4 帧 | 1 | P2 |
| | 脆弱标记 | 静态 sprite | 1 | P2 |
| **DOT 特效** | 燃烧火焰 | 序列帧 4~6 帧 | 1 | P1 |
| | 中毒毒雾 | 序列帧 4 帧 | 1 | P2 |
| | 电弧闪电 | 序列帧 3 帧 | 1 | P2 |
| **道具** | 技能道具 sprite | 24×24 px | 1 | ~~P0~~ 已移除（v1.1） |
| | Buff 道具 sprite | 24×24 px | 1 | P0 |
| | 修复道具 sprite | 24×24 px | 1 | P0 |
| | 弹药道具 sprite | 24×24 px | 1 | P1 |
| | 金币道具 sprite | 24×24 px | 1 | P1（V3 商店预留） |
| **UI** | Buff 图标（7种） | 32×32 px | 7 | P1 |
| | 技能 CD 指示器（HUD 固定） | FairyGUI 组件 | 1 | P0 |
| | 被动技能栏 | FairyGUI 组件 | 1 | P1 |
| | Boss 血条（画面顶部固定） | FairyGUI 组件 | 1 | P1（v1.1 新增） |
| | 出战准备界面 | FairyGUI 面板（Sortie 包） | 1 | P0（v1.1 新增，v1.2 完善交互规格） |
| | 技能/被动解锁提示 | FairyGUI 弹窗 | 1 | P1（v1.1 新增，v1.2 完善弹窗设计） |
| | Buff 折叠 "+N" 标记 | FairyGUI 组件 | 1 | P1（v1.2 新增） |
| | 道具拾取通知条 | FairyGUI 组件 | 1 | P1（v1.2 新增，替代飘字） |
| **音效** | 敌机射击声 | ≤1s | 3 | P0 |
| | 技能释放声（6种） | ≤1s | 6 | P1 |
| | Buff 获取声 | ≤0.5s | 1 | P1 |
| | 道具拾取声 | ≤0.5s | 1 | P0 |
| | DOT 持续声（循环） | ≤2s loop | 2 | P2 |

### 10.2 美术工具需求

| # | 工具 | 优先级 | 用途 |
|---|------|--------|------|
| A1 | **Sprite Sheet VFX 工具** | ✅ 已有 | vfx-creator skill，序列帧特效制作 |
| A2 | **Runtime Atlas 系统** | ✅ 已有 | 动态图集，管理 V2 新增的大量 sprite |
| A3 | **BulletType 预览器** | P0 必做 | 选中 BulletTypeSO → 预览子弹外观 + 轨迹 |
| A4 | **Buff VFX 预览器** | P1 应有 | 附着在 Entity View 上预览 Buff 特效效果 |
| A5 | **道具外观预览** | P2 锦上添花 | 预览道具掉落 + 拾取的完整视觉流程 |
| A6 | **ShooterGame 资源导入校验器** | P3 远期 | `AssetPostprocessor` 自动检查 Sprites/ShooterGame/ 路径下的 sprite 尺寸/命名合规（v1.5 PK 新增） |

### 10.3 美术 SOP（V2 新增资源制作）

```
制作一种新敌方子弹：
  1. 在矢量工具中绘制子弹 sprite（遵循规格尺寸）
  2. 导出 PNG（透明底，命名：bullet_{name}_01.png）
  3. ⚠️ 不使用 _N 后缀（避免与法线贴图检测冲突）
  4. 放入 Assets/_Game/Sprites/ShooterGame/Bullets/
  5. 在 Unity 中创建 BulletTypeSO → 引用 sprite
  6. 设置碰撞半径、颜色、拖尾参数
  7. 用 BulletType 预览器检查外观

制作一个 Buff 特效：
  1. 用 vfx-creator skill 生成序列帧
  2. 导出到 Assets/_Game/Sprites/ShooterGame/VFX/
  3. 创建 PoolDefinition → 引用 VFX prefab
  4. 在 BuffConfigSO.VfxPrefab 中引用
  5. Play Mode 验证附着效果

制作道具 sprite：
  1. 绘制 24×24 道具图标（颜色编码：蓝=Buff，绿=修复，红=弹药，金=金币）
  2. 导出 PNG 到 Assets/_Game/Sprites/ShooterGame/Pickups/
  3. 创建 PickupConfigSO → 引用 sprite
  4. 在 DropTableSO 中配置掉落权重
```
