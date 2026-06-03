---
system: shootergame-v2-tdd
scope: sprint5-tools-ui-polish
last_verified: 2026-06-03
depends_on: [SG_V2_TDD_INDEX, SG_V2_TDD_04_LEVEL_BALANCE, SG_GDD_04_WORKFLOW, SG_GDD_05_SUPPLEMENT]
related_code: Assets/Editor/ShooterGame/**, Assets/_Game/Scripts/ShooterGame/UI/**, Assets/_Framework/EntitySystem/**
---

# Sprint 5：策划工具 + UI 完善 + 打磨（~19.5h）

> **目标**：T1/T7 弹幕预览 + T2 Buff 速览 + T5/T8 构建验证 + 战斗 HUD + 结算面板 + 波次动效 + 最终打磨。
> **前置**：Sprint 4 验收通过（全系统+全关卡就位 → UI 和工具才有完整上下文）。

---

## 0. Editor 工具约定（PK-R2 新增）

### 菜单路径约定

```
ShooterGame/
├── Tools/          → 策划/开发工具（预览、速览、模拟器）
│   ├── Skill Preview
│   ├── Buff Overview
│   ├── Battle State Inspector（现有 SG_BattleStateWindow）
├── Validate/       → 验证/检查（数据正确性）
│   ├── Check ID Conflicts
│   ├── Validate All SOs
│   ├── Validate Selected
├── Create/         → 快速创建（V3 预留）
│   ├── Create Passive + Buff Pair（T13）
```

### asmdef 依赖说明

```
Game.Editor.asmdef（Assets/_Game/Editor/）
├── 引用 Game.Runtime（ShooterGame 业务类型）
├── 引用 MiniGameTemplate.EntitySystem（EntityConfigSO、BuffConfigSO 等）
├── 引用 MiniGameTemplate.DanmakuSystem（BulletPatternSO、BulletTypeSO——仅读配置）
├── Define Constraints: [UNITY_EDITOR]

EditorBulletSimulator：
├── 不直接依赖 DanmakuSystem.BulletWorld（Runtime NativeArray 不可 Editor 用）
├── 仅读取 BulletPatternSO 配置数据（speed/angle/count）
├── 自行模拟弹道（纯 Vector2 数学）
```

### 验证职责划分

```
OnValidate（即时反馈）：
├── 仅做"本对象字段级"校验（范围/null/格式）
├── 不做跨 SO 引用链校验
├── 调用 SOValidationRules.Validate{Type}Fields() 共享规则
├── 输出：Debug.LogError + Inspector HelpBox

T8 构建验证（阻断级全量）：
├── 调用 SOValidationRules + 额外跨 SO 引用链逻辑
├── 包含全局唯一性校验（ID 冲突）
├── 包含解锁表验证、关卡数据完整性验证
├── 输出：Debug.LogError($"[SOValidator] {type} \"{name}\": {msg}", soAsset)
│   → soAsset 作为 context → Console 点击直接 Ping 到资产
├── 总结行：Debug.LogError($"== SO Validation FAILED: {N} errors in {M} assets ==")
```

---

## 1. 实施任务分解

### S5.1 T1 技能预览窗口 + T7 BulletPattern 预览（4h）

#### 实施方案

**菜单路径**：`ShooterGame/Tools/Skill Preview`

**EditorWindow 规格**（GDD §9.3）：

```
SkillPreviewWindow : EditorWindow
├── 输入区域：
│   ├── SkillConfigSO 拖入槽
│   ├── [Toggle] 被动模拟面板（4 个复选框）：
│   │   ☐ 穿透 → 弹幕穿透不消失
│   │   ☐ 暴击 → 偶发暴击闪白+大数字
│   │   ☐ 磁吸 → 仅拾取相关，预览无可视效果
│   │   ☐ 尾翼 → 模拟碰撞触发反击弹幕
│   ├── [Button] ▶ 预览 / ■ 停止
│   ├── [Button] 应用修改 / 重置默认值
├── Scene View 集成：
│   ├── 在 Editor 模式模拟 BulletWorld 发射（不进 Play Mode）
│   ├── 虚拟施法者（蓝色圆圈）在 Scene View 中心
│   ├── 可选虚拟敌人（红色方块）用于追踪弹观测
│   ├── 弹丸使用 Handles.DrawSolidDisc 渲染（非实际 GameObject）
│   ├── SceneView.duringSceneGui 注册绘制回调
├── 参数速览区域：
│   ├── CD / 前摇 / 后摇 / Effects 列表
│   ├── 理论 DPS（调用 DPSCalculator 公式）
├── T7 集成：
│   ├── SkillConfigSO 拖入后自动读取 BulletPatternSO
│   ├── 也可直接拖入 BulletPatternSO 独立预览弹幕发射形态
```

**"应用修改"按钮 Undo 策略**（PK-R2 ET-004）：

```
"应用修改" 按钮逻辑：
├── Undo.RecordObject(targetSkillConfig, "Skill Preview: Apply Changes")
├── 写入修改
├── EditorUtility.SetDirty(targetSkillConfig)
→ Ctrl+Z 可撤销
```

**Scene View 集成坐标系**（PK-R2 ET-009）：

```
坐标系：World Space
├── 虚拟施法者：固定 World (0, 0, 0)
├── 虚拟敌人：World (0, 5, 0)（可拖动，Handles.PositionHandle）
├── 多 SceneView 行为：自然正确（World Space 各视图各自变换）
├── HandleUtility.AddDefaultControl(0) 防止 SceneView 拦截点击
```

**Editor 模式弹幕模拟**：

```
EditorBulletSimulator（Editor-only 类）
├── const int MAX_SIM_BULLETS = 500       // PK-R2 ET-014 性能护栏
├── List<SimBullet> _bullets
├── struct SimBullet:
│   ├── Vector2 Position, Velocity
│   ├── float Lifetime, Age
│   ├── Color Color
│   ├── bool IsHoming
├── Spawn(BulletPatternSO pattern, Vector2 origin, Vector2 dir):
│   → if (_bullets.Count >= MAX_SIM_BULLETS) → 黄色 Warning "已达预览上限"
│   → 按 pattern 参数创建 SimBullet 列表
├── Tick(float dt):
│   → 更新每个 SimBullet 的 Position
│   → 追踪弹：朝虚拟敌人方向偏转
│   → Age > Lifetime → 移除
├── Draw():
│   → foreach SimBullet → Handles.DrawSolidDisc
```

**驱动机制**（PK-R2 ET-001）：

```
├── 驱动源：EditorApplication.update（~100Hz 稳定）
├── dt = EditorApplication.timeSinceStartup - _lastTime
│   └── Clamp(0, 0.1f) 防止超大步长
├── SceneView.duringSceneGui 仅负责 Draw()
├── SkillPreviewWindow.OnEnable:
│   → EditorApplication.update += Tick
│   → SceneView.duringSceneGui += OnSceneGUI
├── SkillPreviewWindow.OnDisable:
│   → 取消注册，清理 _bullets
│   → SceneView.RepaintAll() 清除残留
├── 每帧末尾 SceneView.RepaintAll() 强制重绘
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| A1 | 打开窗口 | 菜单打开 | 正常显示 | 无报错 |
| A2 | 散射预览 | 拖入散射 SkillConfigSO | Scene View 显示 5 发扇形弹 | 角度正确 |
| A3 | 追踪预览 | 拖入追踪 SkillConfigSO + 放置虚拟敌人 | 追踪弹偏转 | 追踪行为 |
| A4 | 激光预览 | 拖入激光 SkillConfigSO | 直线贯穿 | 宽度正确 |
| A5 | 被动穿透 | 勾选穿透 | 弹丸不消失（穿透可视化） | 效果正确 |
| A6 | 被动暴击 | 勾选暴击 | 偶发红色闪烁弹 | 视觉标记 |
| A7 | BulletPattern 独立预览 | 直接拖入 BulletPatternSO | 仅显示弹幕形态 | 不依赖技能 |
| A8 | 参数速览 | 查看面板 | CD/DPS 显示正确 | 与 T4 一致 |
| A9 | 不进 Play Mode | Editor 模式 | 预览全程不触发 Play Mode | Editor-only |

---

### S5.2 T2 Buff 速览面板（1h）

#### 实施方案

**菜单路径**：`ShooterGame/Tools/Buff Overview`

```
BuffOverviewWindow : EditorWindow
├── 自动扫描 Assets/ 下所有 BuffConfigSO
├── 列表展示（可排序）：
│   ├── BuffId | 名称 | Tag | Duration | 属性修正列 | StackMode
│   ├── 点击行 → Inspector ping 定位
├── 筛选条件：
│   ├── [Dropdown] Tag 过滤（All / Positive / Negative / Status）
│   ├── [Search] 名称搜索
├── 新增 DOT 标签页：
│   ├── DotId | 名称 | Damage | Interval | Duration | DPS
├── 刷新按钮：重新扫描
├── 排序偏好持久化（PK-R2 ET-008）：
│   ├── EditorPrefs "SG_BuffOverview_SortColumn" (int)
│   ├── EditorPrefs "SG_BuffOverview_SortAscending" (bool)
│   ├── OnEnable 读取，OnDisable 写入
│   ├── 默认：按 BuffId 升序
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| B1 | 打开面板 | 菜单打开 | 列出所有 BuffConfigSO | 数量正确 |
| B2 | Tag 过滤 | 选 Negative | 仅显示 3 个 Debuff | 过滤正确 |
| B3 | Ping 定位 | 点击某行 | Inspector 高亮对应 SO 资产 | 联动正确 |
| B4 | DOT 标签 | 切换 DOT | 列出 3 个 DotConfigSO | DPS 计算正确 |

---

### S5.3 T5 ID 冲突检测 + T8 SO 一致性验证器（2h）

#### 实施方案

**T5 ID 冲突检测**（Sprint 3 预定义，此处实现）：

```
[MenuItem("ShooterGame/Validate/Check ID Conflicts")]
static bool CheckIdConflicts()
├── 扫描所有 BuffConfigSO：
│   → BuffId 唯一性检查
│   → BuffId ∈ [1000, 3999] 范围检查
├── 扫描所有 DotConfigSO：
│   → DotId 唯一性检查
│   → DotId ∈ [4000, 4999] 范围检查
├── 输出到 Console（Error + Warning）
├── return bool（供 T8 调用）
```

**开发期快速验证**（PK-R2 ET-002）：

```
[MenuItem("ShooterGame/Validate/Validate Selected")]
static void ValidateSelected()
├── 获取 Selection.objects 中所有 ScriptableObject
├── 逐个调用 SOValidationRules 对应方法
├── 输出结构化报告到 Console
├── 无选中时 LogWarning "请先选中要验证的 SO 资产"
```

**T8 SO 一致性构建验证器**（GDD §9.5 + §T8 验证深度 v2.0）：

```
SOConsistencyValidator : IPreprocessBuildWithReport
├── OnPreprocessBuild(BuildReport report):
│   → bool pass = true
│   → pass &= CheckIdConflicts()           // T5
│   → pass &= ValidateAllSkillConfigs()     // L1+L2
│   → pass &= ValidateAllBuffConfigs()
│   → pass &= ValidateAllDotConfigs()
│   → pass &= ValidateAllPickupConfigs()
│   → pass &= ValidateAllDropTables()
│   → pass &= ValidateAllEntityConfigs()
│   → pass &= ValidateAllBulletPatterns()
│   → pass &= ValidateUnlockTables()        // PK-R2 ET-003
│   → pass &= ValidateAllLevelConfigs()     // PK-R2 ET-010
│   → if (!pass) throw BuildFailedException("SO 验证失败")
│
├── ValidateAllSkillConfigs():
│   → L1: Effects[] 非空非 null
│   → L2: FireBulletsEffect → BulletPatternSO 非 null
│   →      BulletPatternSO → BulletTypeSO 非 null
│   → L2: 弹幕/追踪/激光 → BulletPattern 必填
│   → L2: 护盾/火力全开 → LinkedBuff 必填
│
├── ValidateAllEntityConfigs():
│   → MaxHp > 0
│   → SkillConfigs 元素非 null（空数组合法）
│
├── ValidateUnlockTables()（PK-R2 ET-003）：
│   → SkillUnlockTableSO.Entries[i].Skill != null
│   → 无重复 SkillConfigSO 引用
│   → ClearLevel 类 ConditionParam ∈ [1, MAX_LEVEL]
│   → PassiveUnlockTableSO 同理
│
├── ValidateAllLevelConfigs()（PK-R2 ET-010）：
│   → LevelConfigSO.Waves != null && Waves.Length > 0
│   → foreach wave: wave != null
│   → foreach wave.Entries[j]: EnemyConfig != null
│   → 总波次数 ≤ 30（防误配 → Warning）
```

**各 SO OnValidate 实现**：按 GDD §9.5 约束表逐类型实现。每类 SO 的 OnValidate 方法调用 `SOValidationRules` 共享规则做字段级校验，失败时 `Debug.LogError` + Inspector 可见的 HelpBox 标红。

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| C1 | T5 无冲突 | 运行菜单命令 | Console 无 Error | 全通过 |
| C2 | T5 制造冲突 | 两个 Buff 同 ID | 检出冲突 Error | 正确报告 |
| C3 | T5 范围越界 | BuffId=5000 | 检出越界 Error | 正确报告 |
| C4 | T8 构建卡口 | 制造 SkillConfigSO.Effects=空 | 构建失败 | 阻断 |
| C5 | T8 二级检查 | SkillConfigSO→BulletPatternSO=null | 构建失败 | L2 检查 |
| C6 | T8 正常构建 | 所有 SO 合法 | 构建通过 | 不误报 |
| C7 | OnValidate | BuffConfigSO.Duration=-1 | Inspector 红色 HelpBox | 即时反馈 |
| C8 | Validate Selected | 选中 3 个 SO 运行菜单 | 仅验证选中项 | PK-R2 ET-002 |
| C9 | 解锁表验证 | SkillUnlockTable 含 null Entry | 构建失败 | PK-R2 ET-003 |
| C10 | 关卡验证 | LevelConfigSO.Waves=空 | 构建失败 | PK-R2 ET-010 |
| C11 | EditMode Test | 运行 SOValidationRulesTests | 全部 PASS | PK-R2 ET-012 |

---

### S5.4 战斗 HUD 完善（4h）

#### 实施方案

**战斗开始过渡动效**（PK-R3 UID-010 新增）：

```
BattleStartSequence（BattleController 管理）：
├── 场景加载完成 → Time.timeScale = 0
├── T+0.0s：暗幕淡入（alpha 0→0.5，0.2s）
├── T+0.2s："Wave 1" 文字居中放大缩小（scale 2.0→1.0→1.2→1.0，0.5s，EaseOutBack）
├── T+0.7s：暗幕淡出（alpha→0，0.3s）
├── T+1.0s：Time.timeScale = 1，正式开始
│   → 所有 Entity 的 FirstAttackDelay 从此刻开始计算
│   → EntitySpawner 从此刻开始第一波刷新
├── 总时长 1.0s
├── 首波 WaveIndicator 也走弹跳动效（取消"首波无动效"限制）

技术实现细节（PK-R3 UID-012）：
├── UI 动效驱动：FairyGUI GTween 使用 ignoreTimeScale=true 模式
│   → 暗幕/文字动画不受 Time.timeScale=0 影响
├── EntitySpawner 启动机制：BattleController 显式调用 EntitySpawner.StartSpawning()
│   → 不依赖 timeScale 判断，由 BattleState 枚举控制
│   → BattleState.Starting（timeScale=0 期间）→ BattleState.Playing（timeScale=1 后）
├── 暂停菜单区分：BattleState.Paused vs BattleState.Starting
│   → 两者都 timeScale=0，但 BattleState 不同
│   → 暂停菜单仅在 BattleState.Playing 时可触发
```

**FairyGUI 包**：`Battle`

**HUD 组件规格表**（GDD §11.1 v2.2）：

| 组件 | 位置 | 尺寸 | 状态机/行为 |
|------|------|------|------------|
| **SkillCDIndicator ×6** | 下方固定（基地血条上方）2行×3列居中 | 48×48pt 圆形，间距 4pt，总宽 152pt，总高 100pt | Cooldown(扇形遮罩) → Ready(全亮) → Casting(金色边框) → Release(闪白0.2s+通用SFX `sfx_skill_release`)；未装备=灰色空圈(α0.3)。**PK-R3 UID-002**：36→48pt 提升低端机可读性；1行→2行×3列 减少横向拥挤；新增释放通用音效 |
| **PassiveIndicator ×3** | 左上角 | 40×40pt **方形圆角**(r=8) | Cooldown(暗色+充能边框顺时针) → Ready(亮色+呼吸缩放1.0~1.05,2Hz) → Active(**绿色环形进度条消耗**+发光脉冲，不放倒计时文字)。**PK-R3 UID-003**：方形圆角与技能圆形形成差异化；Active 态环形进度消耗代替数字倒计时；尺寸 32→40pt |
| **WaveIndicator** | 右上角 | — | 常规波次: scale弹跳1.0→1.3→1.0(0.3s)+色高亮；FINAL WAVE: 红色+1.0→1.5→1.0(0.4s)；**首波也执行弹跳**（PK-R3 UID-010 取消首波无动效）；触发=新波次首敌刷出 |
| **PickupNotification** | 中下方固定（基地血条上偏高） | — | "获得：{名}！"弹入0.2s→显示1.5s→淡出0.3s；队列最大2(超出快速替换)；P0红闪期间延迟0.3s |
| **PauseButton** | 左上角（被动栏左侧） | — | 点击→暂停菜单(sortOrder=800)，Time.timeScale=0 |

**数据绑定**：SkillComponent.GetSlotState(i) / PassiveComponent.GetSlotState(i) / PickupSystem.OnCollected 事件。

**战斗 HUD Z-Order 层级图**（PK-R3 UID-005）：

```
Layer 9000: LoadingMask（全屏遮罩，仅加载期间）
Layer  800: PauseMenuPanel（暂停菜单）
Layer  700: ConfirmDialog（确认弹窗/重试弹框）
Layer  200: UnlockPopup
Layer  100: VictoryPanel / DefeatPanel
Layer   50: DamageRedFlash（屏幕边缘红闪，CanvasGroup alpha 渐变，不覆盖中央 HUD）
Layer   10: BattleHUD 常驻层
             ├── PassiveIndicator（左上）
             ├── PauseButton（左上，被动栏左侧）
             ├── WaveIndicator（右上）
             ├── PickupNotification（中下偏上，Z=15 确保在红闪之上）
             ├── SkillCDIndicator（下方）
             ├── BaseHPBar（最下方）
Layer    0: 游戏场景（弹幕/敌机/飞机/基地）
```

红闪（Layer 50）使用 `CanvasGroup.alpha` 从 0.6 渐变到 0，确保不覆盖 HUD 文字。PickupNotification（Z=15 但叠加在红闪之上因为红闪只覆盖屏幕边缘 100pt 区域，不覆盖中下方通知条位置）。

#### 验收方案

| # | 验收项 | 预期 | PASS |
|---|--------|------|------|
| D1 | 技能CD显示 | 6个CD指示器居中，四态流畅切换 | 位置+动画正确 |
| D2 | 空槽位 | 只带3技能→其余灰色空圈 | 有意设计 |
| D3 | 被动三态 | 冷却充能→就绪呼吸→激活发光 | 视觉区隔 |
| D4 | 波次弹跳+FINAL | 新波次弹跳，最终波红色大弹跳 | 仪式感 |
| D5 | 通知条+队列 | 拾取弹出1.5s淡出，连续3个→替换最旧 | 队列限2 |
| D6 | 暂停 | 点击→菜单弹出，timeScale=0 | 正确 |

---

### S5.5 暂停菜单 + 结算面板（3.5h）

#### 实施方案

**暂停菜单**（GDD §暂停菜单规格 v1.9）：

```
PauseMenuPanel : IUIPanel
├── FairyGUI 包：Battle（同 HUD）
├── sortOrder：800
├── 内容区域：
│   ├── "当前 Build"：显示已装备主动(6 格)+被动(3 格)图标
│   ├── "当前 Buff"：实时显示活跃 Buff 列表（名称+剩余时间）
│   │   → 从 BuffComponent 读取活跃 Buff
│   │   → 按获得顺序排列（v1.9 确认）
│   │   → 增益=蓝色边框(#4FC3F7)，减益=红色边框(#EF5350)
│   │   → 无活跃 Buff 显示"无"
│   │   → **PK-R3 UID-006**：使用 GList 虚拟列表，固定高度 240pt，超出可滚动
│   │   → Buff 列表项高度：40pt（图标 32pt + 名称 + 倒计时），最多显示 6 条免滚动
│   ├── "本局统计"：击杀数 / 当前波次 / 用时
│   ├── 继续按钮（最大最醒目，品牌色填充）→ 恢复 timeScale
│   ├── 重试按钮 → 直接重开（无二次确认——#35/#80）
│   ├── 退出按钮 → 二次确认弹窗 → 返回选关
├── 蒙版：alpha=0.7，点击蒙版=继续
├── Buff 倒计时冻结（timeScale=0 时不走）
```

**胜利结算面板**（GDD §11.5）：

```
VictoryPanel : IUIPanel
├── FairyGUI 包：Battle
├── sortOrder：100
├── 动效：底部滑入 0.3s + 星星依次弹出（间隔 0.3s）
├── 内容：
│   ├── 关卡名 + 星级（⭐×N）
│   ├── 击杀数 + 用时 + 获得金币
│   ├── 技能贡献：前 4 名按百分比**横向条形图**，≤5% 合并为"其他"
│   │   → 可视化规格（PK-R3 UID-004）：
│   │     ├── 每行：技能图标(32pt)+名称+百分比文字+彩色横条(高24pt)
│   │     ├── 颜色映射：基础攻击=#78909C,SK-P01=#4FC3F7,SK-P02=#FF7043,SK-P03=#AB47BC,SK-P04=#FFA726,SK-P06=#66BB6A,PA-04=#EF5350,其他=#9E9E9E
│   │     ├── 最大 5 行（4 名+1"其他"），高度 ≈ 160pt
│   │     ├── 只有 1~2 项时：条形占满宽度即可，无需 fallback 布局
│   │     ├── **sourceTag→显示名映射**（PK-R4 DE-008 新增）：
│   │     │   `SkillStatsMapping` 静态工具类，包含
│   │     │   `Dictionary<int, (string name, string iconKey, Color barColor)>` 硬编码映射表
│   │     │   V2 技能数量固定（基础攻击=0, SK-P01~P06, PA-04, DOT 4001~4003），无需动态化
│   │     │   V3 技能扩展时可改为 SO 驱动
│   │     ├── **动效规格**（PK-R3 UID-013）：条形依次从 0% 生长到实际百分比，每条 0.3s（EaseOutCubic），间隔 0.1s；百分比数字从 0 滚动到最终值；最高贡献条额外金色描边(2pt #FFD54F)
│   ├── 下一关 按钮（CTA 优先）
│   ├── 返回选关 按钮
│   ├── 广告位（V3：看广告 ×2 金币）
├── 关闭回调：
│   → 面板淡出 0.3s → Dispose
│   → 等待 0.1s → 检查解锁
│   → 若有新解锁 → 弹出解锁弹窗(sortOrder=200)
```

**失败结算面板**（GDD §11.5）：

```
DefeatPanel : IUIPanel
├── FairyGUI 包：Battle
├── sortOrder：100
├── 动效：屏幕暗红 → 面板淡入 0.4s
├── 内容：
│   ├── "基地沦陷" + 存活至 Wave N/M + 击杀 X/Y
│   ├── 火力提示（动态）：
│   │   → 检查 SkillUnlockManager.GetNextUnlockable()
│   │   → 若有 → 显示技能图标+名称+"通关第 N 关可解锁"提示文字
│   │   → 若全解锁 → 不显示此区域
│   │   → **PK-R3 UID-007**：V2 不实现广告解锁，仅展示文字提示引导玩家继续游玩
│   │   →   [看广告解锁] + [直接解锁] → V3 预留（V2 不实现）
│   ├── 重新挑战 按钮（CTA 优先）
│   ├── 返回选关 按钮
├── 不触发解锁弹窗
```

**解锁弹窗**：

```
UnlockPopup : IUIPanel
├── sortOrder：200
├── 触发：胜利面板关闭后
├── 内容：技能图标(大) + 名称 + 类型标签 + 描述
├── "立即装备" → 跳转出战准备(下一关)，技能自动选中
├── "稍后再说" → 返回选关界面
├── 多解锁处理（**PK-R3 UID-009 — 天命人已决策：方案 A**）：
│   → **采用方案 A**：逐个弹出（间隔 0.3s 或玩家点击关闭后弹下一个）
│   → 理由：实现简单通用，后续有需要再做合并弹窗优化
│   → 每个弹窗复用同一个 UnlockPopup 组件，只切换内容数据
├── 动效：中心缩放弹出 + 图标旋转闪光
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| E1 | 暂停菜单 | 点击暂停 | 显示 Build+Buff+统计 | 内容正确 |
| E2 | Buff 列表 | 有 Buff 时暂停 | 显示名称+倒计时+颜色 | 实时冻结 |
| E3 | 继续 | 点击继续 | 恢复游戏 | timeScale=1 |
| E4 | 重试 | 点击重试 | 直接重开（无确认） | 与 #35 一致 |
| E5 | 退出 | 点击退出 | 弹出确认 → 返回选关 | 二次确认 |
| E6 | 胜利面板 | 通关 | 星级+击杀+技能贡献 | 数据正确 |
| E7 | 星星动效 | 三星通关 | 星星依次弹出+最后一颗金色粒子 | 动效正确 |
| E8 | 技能贡献 | 查看结算 | 前 4 名+其他百分比正确 | 与 damageStats 一致 |
| E9 | 失败面板 | 基地 HP=0 | 暗红+火力提示 | 推荐下一个未解锁技能 |
| E10 | 解锁弹窗 | 通关第 2 关 | 胜利面板关闭后弹出"追踪导弹" | 时序正确 |
| E11 | "立即装备" | 点击立即装备 | 跳转出战准备，技能已选中 | 数据传递 |
| E12 | 层级正确 | 所有面板 | sortOrder 无冲突 | 100/200/700/800/9000 |

---

### S5.6 视觉反馈打磨（2h）

#### 实施方案

**视觉反馈规格表**：

| 触发 | 效果 | 参数 |
|------|------|------|
| 基地受伤（敌弹） | 屏幕边缘红闪 + Camera shake | 红闪0.3s淡出 / shake振幅0.05,持续0.15s / 血条红色高亮 |
| 敌机碰飞机 | 加强版红闪+震动+飞机闪烁 | shake振幅0.1,持续0.25s / 飞机半透明0.5s(无敌帧) |
| PA-04 反击 | 红闪→图标闪白→环形弹幕 | T+0.00s红闪 → T+0.15s图标闪白+"反击！" → T+0.20s 8发弹幕 |
| 技能释放 | CD指示器边框闪白(Release态) | 0.2s；前摇>0 先 Casting(金色)→Release |
| 道具拾取 | Sprite飞入+光芒爆发+通知条 | 0.2s缩放+移动→光芒→通知弹出 |

#### 验收方案

| # | 验收项 | 预期 | PASS |
|---|--------|------|------|
| F1 | 受伤红闪+震动 | 被命中→红光+shake | P0反馈 |
| F2 | 碰撞增强版 | 敌机碰飞机→强震+闪烁 | 加强版 |
| F3 | PA-04时序链 | 红闪→闪白→弹幕(0.2s内) | 时序正确 |
| F4 | 技能闪白 | 释放→CD指示器闪白 | 0.2s |
| F5 | 道具吸入 | Sprite飞入+光芒 | 流畅 |

---

### S5.7 选关界面 V2 升级 + 出战准备 Bottom Sheet（PK-R3 UID-011 新增 + S2.3 延后合入）✅ 编码完成

> **📌 S2.3 合入说明（2026-05-22）**：原 TDD_02 §S2.3 出战准备 Bottom Sheet（3h）延后至本节一并开发与验收。数据管线已在 S2 打通（`BattleLevelData.EquippedSkills/Passives`），本节负责 UI 实现 + 端到端联调。验收时需同时完成 TDD_02 §S2.3 的 C1~C8 验收项。

#### 实施方案

**V2 新增变更点**（相对 V1 选关界面）：

```
LevelSelectScreen V2 升级：
├── 关卡按钮规格（每个 120×140pt）：
│   ├── 关卡图标/缩略图（80×80pt，居上）
│   ├── 关卡编号 "第 N 关"（16pt，图标下方）
│   ├── 星级显示（3 颗星横排，每颗 20×20pt，间距 4pt）：
│   │   ├── 已获得星：金色填充 ★（#FFD54F）
│   │   ├── 未获得星：灰色描边 ☆（#616161）
│   │   ├── 未通关：3 颗全灰 ☆
│   ├── 锁定态：整体灰色(desaturate) + 锁图标叠加居中(32×32pt) + 不可点击
│   ├── 解锁条件文字（锁定时显示）："通关第 N-1 关"（12pt 灰色）
├── 布局：横向 5 列（V2 刚好 5 关一排），居中
│   ├── V3 超过 5 关 → 改为 GList 横滑 或 分页
├── 点击已解锁关卡 → 弹出出战准备 Bottom Sheet
├── 数据源：SG_ProgressManager.LevelStars + ClearedLevels
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| I1 | 新存档 | 启动 | 关卡 1 解锁(0 星)，2~5 锁定 | 状态正确 |
| I2 | 星级显示 | 通关后查看 | 获得的星金色，未获得灰色 | 视觉区分 |
| I3 | 解锁推进 | 通关第 1 关 | 第 2 关解锁 | 锁定→解锁 |
| I4 | 最高星级 | 降星重打 | 显示最高星级不降 | 只升不降 |

---

### S5.8 最终整合测试 + 打磨（2h）✅ 全部验收通过

#### 实施方案

**完整流程**：冷启动 → 主界面 → 选关 → 出战准备 → 战斗 → 结算 → 解锁 → 下一关 → ... → 第 5 关通关

**Checklist**（全部 PASS 才算验收通过）：
- 5 关全部可从头通关 + 技能解锁正确触发 + 被动 CD 正常
- Buff/DOT 效果与配置一致 + 道具掉落概率与 DropTable 一致
- 伤害统计守恒 + 星级评价正确 + 存档读写正确（云同步）
- HUD 所有组件正确 + 暂停菜单正确 + 解锁弹窗时序正确
- 视觉反馈优先级正确（P0 不被抑制）+ 60fps（中端）+ 零 GC 热路径

**待 playtest 微调项**：HP 倍率 / 星级阈值(50%/80%) / 波间间歇 / DPS 数值

---

### S5.8 BuffConfigSO CustomEditor（1h）（PK-R2 ET-005 新增）✅ 编码完成

#### 实施方案

```
[CustomEditor(typeof(BuffConfigSO))]
BuffConfigEditor : Editor
├── 根据 Tag 显隐字段：
│   ├── Positive/Negative 共有：Duration, StackMode, Id
│   ├── 仅 Positive：StatModifiers（攻速/伤害/护盾...）
│   ├── 仅 Negative：Slow/Freeze 专有字段
│   ├── Status：触发条件字段
├── HelpBox 规则：
│   ├── Duration < 0 → 红色 HelpBox "Duration 必须 ≥ 0"
│   ├── Id ∉ [1000, 3999] → 红色 HelpBox "BuffId 超出范围"
│   ├── Id 冲突（同 Project 其他 BuffConfigSO）→ 黄色 HelpBox
├── 折叠式数值预览区：
│   ├── 当前 StatModifiers 效果一览（只读）
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| H1 | Tag 显隐 | 选 Positive | 显示 StatModifiers，隐藏 Freeze | 正确显隐 |
| H2 | HelpBox | Duration=-1 | 红色 HelpBox 出现 | 即时 |
| H3 | Id 冲突 | 设置已存在的 Id | 黄色 HelpBox | 提示 |
| H4 | 预览区 | 展开 | 显示修正数值 | 只读正确 |

---

## 2. 新增代码文件清单

| 文件路径 | 类型 | 说明 |
|---------|------|------|
| `Editor/ShooterGame/SkillPreviewWindow.cs` | 新增 | T1+T7 技能/弹幕预览 |
| `Editor/ShooterGame/EditorBulletSimulator.cs` | 新增 | Editor 模式弹幕模拟 |
| `Editor/ShooterGame/BuffOverviewWindow.cs` | 新增 | T2 Buff 速览 |
| `Editor/ShooterGame/SOConsistencyValidator.cs` | 新增 | T5+T8 构建验证器 |
| `Editor/ShooterGame/IdConflictChecker.cs` | 新增 | T5 ID 冲突检测 |
| `Editor/ShooterGame/SOValidationRules.cs` | 新增 | 共享验证规则（PK-R2 ET-006） |
| `Editor/ShooterGame/BuffConfigEditor.cs` | 新增 | BuffConfigSO CustomEditor（PK-R2 ET-005） |
| `Editor/ShooterGame/Tests/SOValidationRulesTests.cs` | 新增 | EditMode Test（PK-R2 ET-012） |
| `ShooterGame/UI/BattleHUDPanel.cs` | 新增/重写 | 战斗 HUD 面板 |
| `ShooterGame/UI/SkillCDIndicator.cs` | 新增 | 技能 CD 指示器组件 |
| `ShooterGame/UI/PassiveIndicator.cs` | 新增 | 被动技能栏组件 |
| `ShooterGame/UI/WaveIndicator.cs` | 新增 | 波次指示器组件 |
| `ShooterGame/UI/PickupNotification.cs` | 新增 | 道具拾取通知条 |
| `ShooterGame/UI/PauseMenuPanel.cs` | 新增 | 暂停菜单 |
| `ShooterGame/UI/VictoryPanel.cs` | 新增 | 胜利结算面板 |
| `ShooterGame/UI/DefeatPanel.cs` | 新增 | 失败结算面板 |
| `ShooterGame/UI/UnlockPopup.cs` | 新增 | 解锁弹窗 |
| `ShooterGame/UI/DamageVisualFeedback.cs` | 新增 | 受伤红闪+震动 |

---

## 3. Sprint 5 验收总表

### 功能验收

| # | 场景 | 预期 | 状态 |
|---|------|------|------|
| G1 | T1 技能预览 | Editor 模式预览弹幕形态 | ✅ 编译通过+脚本就位 |
| G2 | T2 Buff 速览 | 一览所有 Buff 属性 | ✅ 编译通过+脚本就位 |
| G3 | T5 ID 检测 | 无冲突通过/有冲突报错 | ✅ MCP 验证 0 冲突 |
| G4 | T8 构建卡口 | 非法 SO 阻断构建 | ✅ RunFullValidation=True |
| G5 | 技能 CD HUD | 6 个 CD 指示器四态正确 | ✅ 编码完成（待真机验收） |
| G6 | 被动栏 HUD | 3 个被动三态正确 | ✅ 编码完成（待真机验收） |
| G7 | 波次动效 | 弹跳+FINAL WAVE | ✅ 编码完成（待真机验收） |
| G8 | 通知条 | 拾取弹出+队列限 2 | ✅ 编码完成（待真机验收） |
| G9 | 暂停菜单 | Build+Buff+统计正确 | ✅ 编码完成（待真机验收） |
| G10 | 胜利面板 | 星级+技能贡献+解锁触发 | ✅ 编码完成（待真机验收） |
| G11 | 失败面板 | 火力提示+付费入口 | ✅ 编码完成（待真机验收） |
| G12 | 解锁弹窗 | 正确时序+立即装备 | ✅ 编码完成（待真机验收） |
| G13 | 受伤视觉反馈 | 红闪+震动+反击时序 | ✅ 编码完成（待真机验收） |
| G14 | 完整流程 | 5 关通关无崩溃 | ✅ PlayTest 通过 |
| G15 | BuffConfigEditor | Tag 显隐+HelpBox+预览 | ✅ MCP 验证（12个SO+模板排除） |
| G16 | EditMode Test | SOValidationRulesTests 全绿 | ✅ PlayTest 通过 |

### 性能 + 真机验收

| # | 指标 | 目标 |
|---|------|------|
| P1 | HUD 更新 | < 0.5ms/frame（Profiler） |
| P2 | 通知条 GC | 0 bytes（对象池化） |
| P3 | Editor 工具 | 打开不卡编辑器 |
| W1 | 微信 FairyGUI | 所有 HUD 组件正常显示+触摸响应 |
| W2 | 微信帧率 | 战斗+HUD ≥ 30fps（低端机 ≥ 24fps） |

---

_创建于 2026-05-18 | Sprint 5 TDD v1.5_

**变更历史**：
- v1.0（2026-05-18）：初始版本
- v1.1（2026-05-18）：PK-R1 Unity 架构师回写（UA-001~011）
- v1.2（2026-05-18）：PK-R2 Unity 编辑器工具开发者回写（ET-001~015，+§0 工具约定，+S5.8 BuffConfigEditor，+SOValidationRules，+EditMode Test，工时 18.5h→19.5h）
- v1.3（2026-05-18）：PK-R3 R1 UI 设计师回写（UID-001~010，+战斗开始过渡，+Z-Order 层级图，+技能贡献条形图规格，+SkillCD 48pt 2×3布局，+被动方形圆角40pt，+暂停Buff滚动，+失败面板V3预留广告，+多解锁待决策）
- v1.4（2026-05-18）：PK-R3 R2 UI 设计师回写（UID-011~013，+选关界面V2升级，+BattleStartSequence技术细节，+条形图动效）
- v1.5（2026-05-19）：PK-R4 技术文档工程师回写（DE-008 VictoryPanel 补充 sourceTag→显示名 SkillStatsMapping 映射方案）
