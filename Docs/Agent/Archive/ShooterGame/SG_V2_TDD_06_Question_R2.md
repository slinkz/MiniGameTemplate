# PK 评审记录：SG_V2_TDD_06（第 2 轮 —— 编辑器工具视角）

> **目标文档**：`SHOOTER_GAME/V2_TDD/SG_V2_TDD_06_ATTACK_SKILL.md` (v0.2)  
> **攻方**：Unity 编辑器工具开发者（10 年经验，专精 Inspector/SO 序列化/Editor 工作流/配置安全）  
> **守方**：软件架构师（广智）  
> **最大轮次**：8  
> **ID 前缀**：ET（Editor Tooling）  
> **开始时间**：2026-05-25 15:24

---

## Round 1 — 攻方（Unity 编辑器工具开发者）提问

### ET-001 | 🔴高 | SkillConfigSOEditor 自定义 Inspector 不绘制新增字段

**涉及章节**：§2.2 / §三 P9
**质疑**：

当前 `SkillConfigSOEditor.cs`（L30~37）的 `OnEnable` 只缓存了 6 个 SerializedProperty：
```csharp
_displayName = serializedObject.FindProperty("DisplayName");
_triggerMode = serializedObject.FindProperty("TriggerMode");
_cooldownTime = serializedObject.FindProperty("CooldownTime");
_castTime = serializedObject.FindProperty("CastTime");
_recoveryTime = serializedObject.FindProperty("RecoveryTime");
_effects = serializedObject.FindProperty("Effects");
```

`OnInspectorGUI` 完全手动绘制这 6 个字段，**不调用 `DrawDefaultInspector()`**。

这意味着 TDD §2.2 新增的 `AimMode` 和 `IsNormalAttack` 字段在 Inspector 中**完全不可见**。策划在 Unity Editor 里创建/编辑 SkillConfigSO 时，看不到这两个字段，无法配置。

更严重的是：**已有的 `AttachedDotConfig` 和 `SourceTagId` 字段也被吞掉了**（它们在 SkillConfigSO.cs 里存在但不被 Editor 绘制）——这是一个**现存 bug**，TDD 实施时如果不修会雪上加霜。

**潜在风险**：P1 完成后策划无法在 Inspector 中配置 AimMode/IsNormalAttack，P5 配置 SO 资产时完全卡死。阻塞 P5/P6。

**建议方向**：
1. `SkillConfigSOEditor.OnEnable` 新增 `_aimMode`、`_isNormalAttack`、`_attachedDotConfig`、`_sourceTagId` 的 `FindProperty`
2. `OnInspectorGUI` 中在"基础"区新增 AimMode/IsNormalAttack 绘制
3. 或者改为 `DrawDefaultInspector()` + 只特殊处理 Effects 列表部分

---

### ET-002 | 🔴高 | BattleDebugLauncher 缺少 NormalAttackConfig 注入——调试直跑必崩

**涉及章节**：§2.8
**质疑**：

当前 `BattleDebugLauncher.BuildDebugLevelData()`（L101~130）构建 `BattleLevelData` 时：
- 设置了 `LevelIndex`
- 设置了 `EquippedSkills`（通过 `_debugSkills`）
- 设置了 `EquippedPassives`

**没有设置 `NormalAttackConfig`**。

TDD §2.8 的 `SetupPlayerSkills()` 中有 null 防御：
```csharp
if (_battleLevelData.NormalAttackConfig == null)
{
    Debug.LogError("[BattleController] NormalAttackConfig 未配置！普攻将不可用。");
    return;
}
```

这意味着使用调试直跑模式（非 Flow 启动）时，玩家**永远没有普攻**。直接阻塞开发调试工作流。

**潜在风险**：开发者日常用 BattleDebugLauncher 调试，P6 之后所有直跑测试将失败。

**建议方向**：
1. `BattleDebugLauncher` 新增 `[SerializeField] private SkillConfigSO _debugNormalAttack;` 字段
2. `BuildDebugLevelData` 中注入 `data.NormalAttackConfig = _debugNormalAttack ?? fallbackDefaultNormalAttack;`
3. fallback 从某个 Resources 路径或 AddressableDefault 加载

---

### ET-003 | 🟡中 | BattleLevelData.NormalAttackConfig 字段的来源未定义

**涉及章节**：§2.8 / §四
**质疑**：

TDD §2.8 假设 `_battleLevelData.NormalAttackConfig` 已有值，但**没有说明谁负责设置它**。

当前正式流程中 `BattleLevelData` 由 `SortieBottomSheet.Logic.cs → OnSortieClicked()` 构建：
```csharp
// OnSortieClicked 中 (L278)
var levelData = new BattleLevelData { LevelIndex = ... };
levelData.EquippedSkills = ...;
levelData.EquippedPassives = ...;
```

`NormalAttackConfig` 字段是什么类型？`[NonSerialized] public SkillConfigSO NormalAttackConfig`？还是序列化字段？谁在什么时机赋值？

考虑到 `EquippedSkills` 是 `[NonSerialized]`（因为 SO 引用无法 JSON 序列化），`NormalAttackConfig` 应该也是 `[NonSerialized]`——但它从哪里来？选项：
- A. 从 `EntityConfigSO` 中新增引用读取
- B. 从某个全局 SO（如 GameSettingsSO）读取
- C. BattleController 自己从 Resources 加载

**潜在风险**：实施时发现没有清晰的数据来源，导致临时方案 or 硬编码。

**建议方向**：明确数据流：`SortieBottomSheet` 从哪读 → 赋给 `BattleLevelData.NormalAttackConfig` → 传给 `BattleController`。

---

### ET-004 | 🟡中 | SkillConfigSO.OnValidate 在 IsNormalAttack=true 时缺少配置校验

**涉及章节**：§2.2 / §2.6
**质疑**：

当前 `SkillConfigSO.OnValidate()` 只检查 `Effects` 是否为空。TDD 新增的 `IsNormalAttack` 引入了新的配置约束：
- `IsNormalAttack = true` 时 `CooldownTime` **会被运行时覆盖**（§2.6）——策划可能误以为 CooldownTime 就是实际射速，浪费时间调参
- `IsNormalAttack = true` + `TriggerMode = Manual` 是矛盾的（全自动战斗设计下普攻必须是 Auto）
- `IsNormalAttack = true` 时 `AimMode` 应默认/强制为 `FixedForward`
- 多个 SO 同时标记 `IsNormalAttack = true` 可能导致运行时混乱

没有 Editor 校验 = 策划配置错误只能在运行时发现。

**潜在风险**：策划配置出矛盾状态，运行时表现异常但无明确报错，调试耗时。

**建议方向**：
1. `OnValidate` 新增：`IsNormalAttack + Manual → LogWarning`
2. Inspector 中 `IsNormalAttack=true` 时 CooldownTime 显示为 `ReadOnly` + HelpBox 说明"运行时由 AttackInterval 覆盖"
3. 可选：Editor 脚本扫描同项目所有 SkillConfigSO，检测 IsNormalAttack 重复

---

### ET-005 | 🟡中 | EntityConfigSO.AttackInterval 字段的 Editor 体验——语义已变但 Inspector 无提示

**涉及章节**：§2.9 / §五 R4
**质疑**：

TDD §五 R4 提到要在 `EntityConfigSO.AttackInterval` 字段加 `[Header("⚠ 此值运行时覆盖到 Slot[0] CD")]`。但单独一个 Header 不够强：

1. **字段名 `AttackInterval` 暗示它是组件配置**——策划不知道它已变成"被运行时系统读取的参数"
2. 没有 `[ReadOnly]` 或 `DisabledScope` 保护——策划可能在运行时修改它但看不到效果
3. `AttackBulletPattern` 和 `AttackFireOffset` 在 TDD 实施后实际上**不再被运行时使用**（普攻的 Pattern 来自 SK_NormalAttack.asset 的 FireBulletsEffect）——这两个字段变成了**僵尸字段**
4. 没有 Inspector 按钮或工具帮助策划验证"这个 AttackInterval 值最终控制的是 Slot[0] 的哪个行为"

**潜在风险**：
- 策划修改 `AttackBulletPattern` 以为在改普攻弹幕 → 实际无效 → 浪费调参时间
- `AttackFireOffset` 同理——真正的 FireOffset 在 SK_NormalAttack 的 FireBulletsEffect 中

**建议方向**：
1. `AttackBulletPattern` / `AttackFireOffset` 加 `[Obsolete]` + `[HideInInspector]`（或 Editor 中灰色 + HelpBox）
2. `AttackInterval` 重命名为 `NormalAttackCooldown`（语义更准确）—— 或保持名字但 Inspector 加 HelpBox
3. EntityConfigSO Editor 中增加"跳转到 SK_NormalAttack"快捷按钮

---

### ET-006 | 🟡中 | SetSlotCooldownTimer 公开 API 暴露了内部状态机——Editor 调试工具风险

**涉及章节**：§2.8 / §四
**质疑**：

TDD 新增 `SkillComponent.SetSlotCooldownTimer(int, float)` 为 public method。它直接修改 `_slots[i].CooldownTimer` 和 `_slots[i].State`。

问题：
1. 这是一个**仅在初始化时使用一次**的 API，但 public 意味着任何代码/Editor 工具都可以随时调用
2. 如果 Editor 开发者写了调试工具（如"重置所有技能 CD"按钮），调用此 API 时如果传入 timer=0 + State=Cooldown，下一帧立即转 Idle——行为正确。但如果传入 State=Cooldown 而 Config=null（Slot 未初始化），会 NullRef
3. 没有文档或 `[System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]` 提示这是内部辅助 API

**潜在风险**：未来 Editor 工具误用此 API 导致状态机混乱，或者热重载时被意外调用。

**建议方向**：
1. 改为 `internal` 而非 `public`（BattleController 在同一 Assembly 内）
2. 或保持 public 但加 `[System.ComponentModel.EditorBrowsable(EditorBrowsableState.Advanced)]` + XML doc 明确"仅用于初始化首发延迟"
3. 加 null check：`if (_slots[slotIndex].Config == null) return;`

---

### ET-007 | 🟢低 | P9 的 Editor Custom Inspector 更新范围描述不足

**涉及章节**：§三 P9
**质疑**：

TDD §三 中 P9 描述为"SkillConfigSOEditor 更新：显示 AimMode + IsNormalAttack 字段"，预估 1h。

但根据 ET-001 的分析，实际需要更新的不仅是 2 个字段——还有 `AttachedDotConfig` 和 `SourceTagId`（已存在但被吞掉的字段）。同时如果要加 ET-004 的校验逻辑（HelpBox/ReadOnly/OnValidate），工作量可能超过 1h。

建议 P9 描述扩展为：
1. 新增 AimMode / IsNormalAttack / AttachedDotConfig / SourceTagId 的绘制
2. IsNormalAttack=true 时的条件 UI（CooldownTime 只读 + HelpBox）
3. OnValidate 增强

**潜在风险**：P9 预估偏乐观，可能挤压 P10 回归测试时间。

**建议方向**：将 P9 预估调整为 2h，拆分为 P9a（字段绘制）和 P9b（校验逻辑）。

---

### ET-008 | 🟢低 | 缺少 SO 资产迁移验证工具

**涉及章节**：§2.9 / §三 P7
**质疑**：

P7 要求"EntityConfigSO 的 Components 数组中移除 ComponentType.Attack（Editor 脚本批量处理，或手动逐个 Inspector 操作）"。

但没有定义：
1. 批量处理 Editor 脚本的具体实现（MenuItem 路径？ScriptableWizard？）
2. 处理后如何验证（是否有"扫描确认所有 EntityConfigSO 已无 Attack"的检查脚本）
3. 如果遗漏了某个 EntityConfigSO（未移除 Attack），运行时 CreateComponent 返回 null + LogWarning 是兜底——但策划看到 Warning 不知道如何修复

**潜在风险**：手动操作遗漏 + 无自动化验证 = P10 回归时发现问题。

**建议方向**：
1. 提供 `[MenuItem("Tools/Migration/Remove Attack Components")]` 一键脚本
2. 提供 `[MenuItem("Tools/Migration/Verify No Attack Components")]` 验证脚本
3. 两者放在 P7 scope 内

---

**攻方 Round 1 小结**：
- 🔴高：2 个（ET-001 Inspector 不显示新字段 / ET-002 调试直跑缺失普攻）
- 🟡中：4 个（ET-003 数据来源 / ET-004 OnValidate / ET-005 僵尸字段 / ET-006 API 暴露）
- 🟢低：2 个（ET-007 P9 范围 / ET-008 迁移工具）
- 共 8 个问题

---

## Round 1 — 守方（软件架构师）回应

### 对 ET-001（🔴 Inspector 不显示新字段）— ✅ 接受，制定修复方案

**承认**：`SkillConfigSOEditor` 是完全自定义 Inspector，确实会吞掉未显式绘制的字段。这不仅影响 TDD 新增字段，还暴露了 `AttachedDotConfig` 和 `SourceTagId` 的现存遗漏。

**修正方案**：采用"保留自定义 Effects + 其余字段 DrawPropertiesExcluding"混合模式：

```csharp
// SkillConfigSOEditor.cs 改造
public override void OnInspectorGUI()
{
    serializedObject.Update();

    // ── 基础 ──
    EditorGUILayout.LabelField("基础", EditorStyles.boldLabel);
    EditorGUILayout.PropertyField(_displayName);
    EditorGUILayout.PropertyField(_triggerMode);

    EditorGUILayout.Space(4);

    // ── 瞄准（新增）──
    EditorGUILayout.LabelField("瞄准", EditorStyles.boldLabel);
    EditorGUILayout.PropertyField(_aimMode);
    EditorGUILayout.PropertyField(_isNormalAttack);

    // 【ET-004】IsNormalAttack=true 时的提示
    if (_isNormalAttack.boolValue)
    {
        EditorGUILayout.HelpBox(
            "⚠ 普攻标记已启用。CooldownTime 运行时由 EntityConfigSO.AttackInterval 覆盖。",
            MessageType.Info);
    }

    EditorGUILayout.Space(4);

    // ── 时间轴 ──
    EditorGUILayout.LabelField("时间轴", EditorStyles.boldLabel);
    using (new EditorGUI.DisabledScope(_isNormalAttack.boolValue)) // 普攻时 CD 只读
        EditorGUILayout.PropertyField(_cooldownTime);
    EditorGUILayout.PropertyField(_castTime);
    EditorGUILayout.PropertyField(_recoveryTime);

    EditorGUILayout.Space(4);

    // ── V2 扩展字段（修复现存遗漏）──
    EditorGUILayout.LabelField("V2 扩展", EditorStyles.boldLabel);
    EditorGUILayout.PropertyField(_attachedDotConfig);
    EditorGUILayout.PropertyField(_sourceTagId);

    EditorGUILayout.Space(8);

    // ── 效果列表（保留自定义绘制）──
    EditorGUILayout.LabelField("效果列表", EditorStyles.boldLabel);
    DrawEffectsList();

    serializedObject.ApplyModifiedProperties();
}
```

`OnEnable` 新增 `FindProperty`：
```csharp
_aimMode = serializedObject.FindProperty("AimMode");
_isNormalAttack = serializedObject.FindProperty("IsNormalAttack");
_attachedDotConfig = serializedObject.FindProperty("AttachedDotConfig");
_sourceTagId = serializedObject.FindProperty("SourceTagId");
```

**→ 回写文档 §三 P9 + §2.2 补充说明**

---

### 对 ET-002（🔴 调试直跑缺失普攻）— ✅ 接受，补充 BattleDebugLauncher

**承认**：`BattleDebugLauncher` 确实缺失 `NormalAttackConfig` 注入，直跑模式必然无普攻。

**修正方案**：

```csharp
// BattleDebugLauncher.cs 新增
[Header("V2: 普攻配置")]
[SerializeField] private SkillConfigSO _debugNormalAttack;

public BattleLevelData BuildDebugLevelData()
{
    // ... 现有逻辑 ...

    // 普攻（V2 TDD-06）
    if (_debugNormalAttack != null)
        data.NormalAttackConfig = _debugNormalAttack;
    else
        Debug.LogWarning("[BattleDebugLauncher] _debugNormalAttack 未配置，直跑模式无普攻");

    return data;
}
```

**同时**：在 `BattleController` 的 fallback 路径中，如果非 Flow 启动（直跑）且 `NormalAttackConfig == null`，尝试从 `Resources.Load<SkillConfigSO>("ShooterGame/SK_NormalAttack")` 兜底。

**→ 回写文档 §2.8 补充直跑兜底 + 新增 §2.12 BattleDebugLauncher 改造**

---

### 对 ET-003（🟡 NormalAttackConfig 数据来源）— ✅ 接受，明确数据流

**修正方案**：定义完整数据流：

**正式流程**：
```
EntityConfigSO.NormalAttackSkill (新增 SO 引用字段)
    ↓ BattleController.SetupPlayer 时读取
    ↓ 如果 BattleLevelData.NormalAttackConfig != null 则使用（优先）
    ↓ 否则从 EntityConfigSO.NormalAttackSkill 读取（兜底）
```

**关键决策**：`NormalAttackConfig` 字段加在 **EntityConfigSO**（每种飞机自带默认普攻引用），而非 BattleLevelData。

理由：
- 普攻是飞机固有能力，不是玩家装备选择
- BattleLevelData 负责"玩家选择"（技能/被动），不负责"飞机固有属性"
- SortieBottomSheet **不需要改动**——不用让玩家选普攻

`BattleLevelData.NormalAttackConfig` 字段**改为可选覆盖**（调试/特殊关卡用），正常为 null。

```csharp
// EntityConfigSO.cs 新增
[Header("V2: 普攻技能")]
[Tooltip("此实体的普攻配置。运行时注入 SkillComponent Slot[0]。")]
public SkillConfigSO NormalAttackSkill;

// BattleController.SetupPlayerSkills 中
var normalAttack = _battleLevelData?.NormalAttackConfig  // 优先：BattleLevelData 覆盖
                ?? _playerEntityConfig.NormalAttackSkill; // 兜底：EntityConfigSO 自带
if (normalAttack == null)
{
    Debug.LogError("[BattleController] 无普攻配置！检查 EntityConfigSO.NormalAttackSkill");
    return;
}
```

**→ 回写文档 §2.8 / §四（接口变更新增 EntityConfigSO.NormalAttackSkill）**

---

### 对 ET-004（🟡 OnValidate 校验）— ✅ 接受

**修正方案**：

```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    if (Effects == null || Effects.Length == 0)
        Debug.LogWarning($"[SkillConfigSO] '{name}' Effects 为空——技能无实际效果", this);

    // 【ET-004】IsNormalAttack 校验
    if (IsNormalAttack)
    {
        if (TriggerMode != SkillTriggerMode.Auto)
            Debug.LogWarning($"[SkillConfigSO] '{name}' IsNormalAttack=true 但 TriggerMode!=Auto，全自动战斗下普攻必须为 Auto", this);
        if (AimMode != AimMode.FixedForward)
            Debug.LogWarning($"[SkillConfigSO] '{name}' IsNormalAttack=true 建议 AimMode=FixedForward（纵版射击向上）", this);
    }
}
#endif
```

多 SO 重复标记的检查放在 Editor 批处理脚本中（P9 scope），不放 OnValidate（OnValidate 无法跨 SO 扫描）。

**→ 回写文档 §2.2 补充 OnValidate**

---

### 对 ET-005（🟡 僵尸字段）— ✅ 接受，分阶段处理

**修正方案**：

Phase 实施阶段（P7）：
```csharp
// EntityConfigSO.cs — 标记废弃字段
[Header("⚠ Legacy —— 以下字段已被 SkillComponent Slot[0] 取代")]
[Obsolete("Use NormalAttackSkill.FireBulletsEffect.Pattern instead")]
public BulletPatternSO AttackBulletPattern;

[Obsolete("Use NormalAttackSkill.FireBulletsEffect.FireOffset instead")]
public Vector2 AttackFireOffset;

[Header("普攻射速（运行时覆盖 Slot[0] CD）")]
[Tooltip("此值被 BattleController 读取，覆盖 NormalAttackSkill.CooldownTime")]
public float AttackInterval = 0.25f;
```

**不加 `[HideInInspector]`**——保留可见但标灰，策划看到 Obsolete 就知道别改。下一轮清理统一删除。

**→ 回写文档 §2.9 补充字段标记策略**

---

### 对 ET-006（🟡 SetSlotCooldownTimer 暴露）— ✅ 接受，改为 internal

**修正方案**：

```csharp
// SkillComponent.cs
/// <summary>
/// 设置槽位 CD 计时器和状态。仅用于初始化首发延迟。
/// internal：限制在 Game.Runtime Assembly 内使用。
/// </summary>
internal void SetSlotCooldownTimer(int slotIndex, float timer)
{
    if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return;
    if (_slots[slotIndex].Config == null) return; // 【ET-006 补充】null 防御
    _slots[slotIndex].CooldownTimer = timer;
    _slots[slotIndex].State = SkillState.Cooldown;
}
```

**但**：SkillComponent 在 `MiniGameFramework.Runtime` Assembly，BattleController 在 `Game.Runtime` Assembly。`internal` 需要 `[InternalsVisibleTo("Game.Runtime")]`。

验证可行性——当前 `MiniGameFramework.Runtime.asmdef` 已有对 Game.Runtime 的 InternalsVisibleTo？如果没有，改为 `public` + `[System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]` + null check。

**→ 回写文档 §2.8 修改 API 访问级别说明**

---

### 对 ET-007（🟢 P9 范围）— ✅ 接受

**修正方案**：P9 拆分为：
- **P9a**（1.5h）：SkillConfigSOEditor 字段绘制（AimMode/IsNormalAttack/AttachedDotConfig/SourceTagId + 条件 UI）
- **P9b**（1h）：OnValidate 增强 + IsNormalAttack 互斥校验

总预估：2.5h（原 1h → 2.5h，+1.5h）

**→ 回写文档 §三**

---

### 对 ET-008（🟢 迁移工具）— ✅ 接受

**修正方案**：P7 内包含两个 MenuItem 脚本：

```csharp
// Assets/_Framework/EntitySystem/Editor/Migration/RemoveAttackComponentMigration.cs
[MenuItem("Tools/Migration/Remove Attack From All EntityConfigs")]
static void RemoveAttack() { /* 扫描所有 EntityConfigSO，移除 ComponentType.Attack */ }

[MenuItem("Tools/Migration/Verify No Attack Components")]
static void VerifyNoAttack() { /* 扫描验证 + EditorUtility.DisplayDialog 报告结果 */ }
```

**→ 回写文档 §2.9 补充迁移工具 + §三 P7 预估调整**

---

**守方 Round 1 小结**：
- 全部 8 个问题均接受
- 0 个驳回
- 关键新增：EntityConfigSO.NormalAttackSkill 数据源方案（ET-003）、Editor 混合绘制模式（ET-001）

---

## Round 2 — 攻方（Unity 编辑器工具开发者）追问

### Round 1 回应评估

| ID | 判定 | 理由 |
|----|------|------|
| ET-001 | 🟢 满意 | 混合绘制模式方案清晰，同时修复了现存遗漏 |
| ET-002 | 🟢 满意 | BattleDebugLauncher 注入 + Resources 兜底方案完整 |
| ET-003 | 🟢 满意 | EntityConfigSO.NormalAttackSkill 方案优于 BattleLevelData，数据归属正确 |
| ET-004 | 🟢 满意 | OnValidate 校验 + 多 SO 重复检查放 Editor 脚本合理 |
| ET-005 | 🟢 满意 | Obsolete 标记 + 保留可见策略得当 |
| ET-006 | 🟡 部分解决 | internal 方案需要 InternalsVisibleTo，但未确认可行性 |
| ET-007 | 🟢 满意 | P9 拆分合理 |
| ET-008 | 🟢 满意 | MenuItem 迁移工具方案足够 |

### 新问题

### ET-009 | 🟡中 | SetSlotCooldownTimer 的 internal 方案需要跨 Assembly 配置

**涉及章节**：守方对 ET-006 的回应
**质疑**：

经验证：
- `SkillComponent` 在 `MiniGameFramework.Runtime` Assembly
- `BattleController` 在 `Game.Runtime` Assembly
- 当前 `MiniGameFramework.Runtime.asmdef` **没有** `InternalsVisibleTo` 配置

守方提到"如果没有 InternalsVisibleTo，改为 public + EditorBrowsable"。但这需要明确最终方案选择：

**方案 A**：在 `MiniGameFramework.Runtime.asmdef` 或通过 `AssemblyInfo.cs` 添加 `[InternalsVisibleTo("Game.Runtime")]` → 以后 Framework internal 都暴露给 Game，可能不符合框架封装原则。

**方案 B**：保持 `public` + `[EditorBrowsable(Never)]` + null check + XML doc 警告 → 更简单但不够严格。

**方案 C**：取消 `SetSlotCooldownTimer`，把首发延迟逻辑移到 `InitWithEquipment` 内部（新增参数 `float firstSlotInitialCD = 0`）→ 不需要暴露新 API。

**建议方向**：倾向方案 C——减少 API 表面积。首发延迟是初始化细节，不应外泄。

---

### ET-010 | 🟢低 | EntityConfigSO.NormalAttackSkill 新增字段需同步到 EntityConfigSOEditor（如有）

**涉及章节**：守方对 ET-003 的回应
**质疑**：EntityConfigSO 是否也有自定义 Editor？如果有，同样需要手动绘制新字段。

**建议方向**：确认 EntityConfigSO 的 Editor 情况，如果是 DrawDefaultInspector 则无需额外处理。

---

**攻方 Round 2 小结**：
- 7/8 问题满意，1 个部分解决（ET-006）
- 新增 1 个 🟡中（ET-009 API 暴露方案选择）
- 新增 1 个 🟢低（ET-010 EntityConfigSOEditor）
- 无新 🔴高——**趋势明显收敛**

---

## Round 2 — 守方（软件架构师）回应

### 对 ET-009（SetSlotCooldownTimer 方案选择）— ✅ 接受方案 C

**分析三个方案**：

| 方案 | 优点 | 缺点 |
|------|------|------|
| A. InternalsVisibleTo | 语义最严格 | 开了口子，框架 internal 全暴露给 Game |
| B. public + EditorBrowsable | 最简单 | API 表面积增加 |
| C. 合并到 InitWithEquipment | 零新 API | InitWithEquipment 参数多了 1 个 |

**决策：方案 C**。

```csharp
/// <summary>
/// 初始化技能装备。
/// </summary>
/// <param name="equippedSkills">技能 SO 数组（Slot[0]=普攻）</param>
/// <param name="staggerOffsetPerSlot">错开 CD 间隔</param>
/// <param name="firstSlotInitialCD">Slot[0] 首发延迟（普攻初始 CD 周期）。
///   0 = 立即可用，>0 = 等待指定秒数后首发。</param>
public void InitWithEquipment(
    SkillConfigSO[] equippedSkills,
    float staggerOffsetPerSlot = 0.5f,
    float firstSlotInitialCD = 0f)
{
    System.Array.Clear(_runtimeCooldownOverrides, 0, MAX_SLOTS);
    for (int i = 0; i < MAX_SLOTS; i++)
        _slots[i] = default;

    // ... 现有装备逻辑 ...

    // Slot[0] 首发延迟
    if (firstSlotInitialCD > 0f && _slots[0].Config != null)
    {
        _slots[0].CooldownTimer = firstSlotInitialCD;
        _slots[0].State = SkillState.Cooldown;
    }
}
```

BattleController 调用简化为：
```csharp
skillComp.InitWithEquipment(allSkills, 0.5f, firstSlotInitialCD: attackInterval);
skillComp.OverrideSlotCooldown(0, attackInterval);
```

**删除 `SetSlotCooldownTimer` API**——从 §四 接口变更表中移除。

**→ 回写文档 §2.7 / §2.8 / §四**

---

### 对 ET-010（EntityConfigSOEditor）— ✅ 确认安全

验证：搜索 `[CustomEditor(typeof(EntityConfigSO))]`——CodeGraph 和项目中**没有** EntityConfigSO 的自定义 Editor。EntityConfigSO 使用默认 Inspector（`DrawDefaultInspector`），新增的 `NormalAttackSkill` 字段会**自动显示**。无需额外处理。

---

**守方 Round 2 小结**：
- 2 个问题全部接受
- 0 个驳回
- SetSlotCooldownTimer 改为方案 C（合并到 InitWithEquipment），减少 API 表面积

---

## 收敛判定

| 维度 | 状态 |
|------|------|
| 🔴高 问题全部解决 | ✅（ET-001 Editor 修复，ET-002 调试兜底） |
| 🟡中 问题全部解决 | ✅（ET-003~006 + ET-009 均有具体方案） |
| 🟢低 问题无分歧 | ✅（ET-007/008/010 全部接受） |
| 攻方无新 🔴高 追问 | ✅（Round 2 仅 🟡中和🟢低） |
| 连续两轮无 🔴高 | ✅（Round 1 有 2 个 🔴，Round 2 无 🔴） |
| 严重度呈下降趋势 | ✅（R1: 2🔴4🟡2🟢 → R2: 0🔴1🟡1🟢） |

**✅ 收敛达成 — PK 结束（2 轮）**

---

## PK 最终统计

| 指标 | 数值 |
|------|------|
| 总轮次 | 2 |
| 攻方问题总数 | 10（ET-001 ~ ET-010） |
| 🔴高 | 2 → 全部解决 |
| 🟡中 | 5 → 全部解决 |
| 🟢低 | 3 → 全部接受 |
| 守方驳回 | 0 |
| 文档需回写修改点 | 8 处 |

## Top 3 最有价值变更

1. **ET-001**：修复 `SkillConfigSOEditor` 不显示新字段的阻塞 bug + 顺带修复了 AttachedDotConfig/SourceTagId 的现存遗漏
2. **ET-003→回应**：确立 `EntityConfigSO.NormalAttackSkill` 作为普攻数据源（而非 BattleLevelData），数据归属更合理
3. **ET-009→方案 C**：取消 `SetSlotCooldownTimer` 公开 API，首发延迟合并到 `InitWithEquipment` 参数中，减少 API 表面积

## 需回写到 TDD 文档的修改清单

| # | 章节 | 修改内容 | 来源 |
|---|------|---------|------|
| 1 | §2.2 | 补充 SkillConfigSOEditor 需同步更新 + OnValidate 增强代码 | ET-001/004 |
| 2 | §2.7 | InitWithEquipment 新增 firstSlotInitialCD 参数 | ET-009 |
| 3 | §2.8 | 删除 SetSlotCooldownTimer，改用 InitWithEquipment 参数；补充直跑兜底 | ET-002/009 |
| 4 | §2.9 | 补充 AttackBulletPattern/AttackFireOffset 的 Obsolete 标记 + 迁移 MenuItem 工具 | ET-005/008 |
| 5 | §2.12（新） | BattleDebugLauncher 改造 + Resources 兜底 | ET-002 |
| 6 | §三 | P9 拆分为 P9a+P9b（2.5h）；P7 补充迁移工具 | ET-007/008 |
| 7 | §四 | 接口变更新增 EntityConfigSO.NormalAttackSkill；删除 SetSlotCooldownTimer；新增 InitWithEquipment.firstSlotInitialCD | ET-003/009 |
| 8 | §五 | 新增 R7: NormalAttackConfig 数据来源说明 | ET-003 |

> **PK 状态**：✅ 已收敛
> **结束时间**：2026-05-25 15:40




</content>
</invoke>