# PK 评审记录 — SG_GAME_DESIGN.md（编辑器工具可执行性）

> **目标文档**：`Docs/Agent/SG_GAME_DESIGN.md` v3.0
> **文档类型**：游戏设计文档（编辑器工具与策划工作流可执行性评审）
> **攻方角色**：Unity 编辑器工具开发者（专精 EditorWindow、PropertyDrawer、AssetPostprocessor、构建管线、策划工具链 DX）
> **守方角色**：游戏设计师（专精玩法节奏、关卡编排、策划工作流需求定义）
> **开始时间**：2026-05-03 07:31
> **最大轮次**：6
> **PK 状态**：✅ 已完成（4 轮收敛，10/10 问题关闭）

---

## PK Round 1 — 攻方提问（Unity 编辑器工具开发者）

### SGT-001 | 严重度 🔴高 | §5.3 V1 关卡编辑工作流：EntitySpawnWaveSOEditor 现有能力严重不足

**涉及章节**：§5.2, §5.3, §5.4, §5.5

**质疑**：

文档说"V1 使用框架已有的 `EntitySpawnWaveSO` + Inspector 编辑"。但现有 `EntitySpawnWaveSOEditor.cs` **只有摘要面板 + DrawDefaultInspector()**——这意味着策划编辑 5 关波次数据的工作流是：

1. 在 Inspector 中展开嵌套数组 `Waves[]` → 展开每个 `SpawnWaveEntry` → 展开 `Groups[]` → 拖入 EntityConfig 引用
2. 没有时间预览、没有 X 坐标可视化、没有波间间歇的直觉反馈
3. 没有"复制波次"快捷操作——第 5 关可能有 8+ 波，每波需要手动 +1 → 展开 → 填参数

**具体问题**：
1. V1 策划能接受纯 Inspector 编辑 5 关（每关 4~8 波）的工作流效率吗？预估调试一关要多少分钟？
2. 是否需要在现有 `EntitySpawnWaveSOEditor` 基础上增加 **最小可用的体验改善**（如：一键复制波次、波间间歇直觉可视化、敌机数量统计汇总）？
3. `SpawnWaveEntry` 的 X 坐标范围字段（设计文档 §5.6 提到的 `spawnXRange`）在实际 SO 数据结构中如何表达？`SpawnGroup` 是否有 `SpawnXMin/SpawnXMax` 字段？

**潜在风险**：
- 策划效率极低 → 关卡调试循环>5 分钟/次 → 严重拖慢迭代速度
- 这是独立开发者项目，策划 = 开发者本人，但工具不好用一样影响效率

**建议方向**：
- V1 不做时间线编辑器（✅已确认），但在 `EntitySpawnWaveSOEditor` 中增加 3 个关键改善：① 一键复制最后一波 ② 波次折叠面板 ③ 总敌机/总时长统计

**状态**：🟡 待回应

---

### SGT-002 | 严重度 🔴高 | §3.4/§3.6 ScreenShakeConfigSO 不存在——设计文档引用了未实现的资产

**涉及章节**：§2.2, §3.1

**质疑**：

文档明确提到：
- "屏幕震动参数：创建 `ScreenShakeConfigSO`，参数在 Inspector 中编码期调试"
- "敌机撞飞机的玩家惩罚 = 屏幕震动反馈"

经代码搜索确认：**`ScreenShakeConfigSO` 在项目中完全不存在**——既无 SO 定义、无 Editor、也无运行时引用。

**具体问题**：
1. `ScreenShakeConfigSO` 的最小字段集是什么？（Duration、Intensity、DecayCurve？还是更简化？）
2. 屏幕震动实现方式：Camera Transform 抖动？还是 Post-Processing Volume？（微信小游戏对 Post-Processing 的支持有限制）
3. 这个 SO 是否需要 CustomEditor？还是 V1 用 DrawDefaultInspector 即可？
4. 需要在哪个时间点创建？现在还是"编码期再说"？

**潜在风险**：设计文档引用不存在的技术组件 = 实施时有 undefined 依赖

**建议方向**：
- 最小定义：`[Duration, Intensity, DecayCurve]` 三字段 SO
- 实现：Camera 位置偏移（最简单，零 GC）
- V1 不需要 CustomEditor（3 个字段用默认 Inspector 够了）
- 建议在 TDD 阶段明确创建

**状态**：🟡 待回应

---

### SGT-003 | 严重度 🟡中 | §3.6 基地 Entity 没有 ViewPrefab——底线检测的调试可视化如何实现？

**涉及章节**：§3.6 基地 Entity 建模

**质疑**：

基地 Entity 建模中 Components = `[Health]`，没有 View 相关配置。但策划/开发者需要在 **Scene View** 中看到：
1. BaseLineY 的位置（Gizmo 横线）
2. 基地当前 HP 状态（编码期调试用）
3. 底线检测生效区域

现有 `EntityGizmoDrawer.cs` 基于 `CollisionRadius` 画圆——但基地 CollisionRadius=0、不参与碰撞系统。

**具体问题**：
1. 基地的 ViewPrefab 填什么？纯色横条 Sprite？还是基地不需要 ViewPrefab（用 FairyGUI overlay 显示）？
2. BaseLineY 的 Gizmo 可视化由谁负责？（EntityGizmoDrawer 扩展？还是 BattleController 自带 OnDrawGizmos？）
3. 基地 HP 变化的编辑器调试（运行时）是否走 `EntityDebugWindow`？

**潜在风险**：底线检测是 V1 核心机制，但如果 Scene View 中看不到 BaseLineY 在哪，调参会非常痛苦

**建议方向**：
- BaseLineY Gizmo：在 BattleController/Bootstrap 的 MonoBehaviour 上加 `OnDrawGizmos`，画一条可视化横线
- 基地 View：V1 可以不用 ViewPrefab（FairyGUI 血条够了），但需要在 EntityDebugWindow 中看到基地 HP 实时值

**状态**：🟡 待回应

---

### SGT-004 | 严重度 🟡中 | §3.4 基准数值表的 SO 配置方案未明确——需要多少个 EntityConfigSO 资产？

**涉及章节**：§3.4, §3.6

**质疑**：

文档定义了完整的数值表（基地/飞机/普通敌机/快速敌机），但没有明确说 **需要创建多少个 EntityConfigSO 资产、放在哪里、命名规范是什么**。

从数值表推导，V1 至少需要：

| SO 资产 | 用途 |
|---------|------|
| `SG_Player.asset` | 飞机：[Movement, Collision], MoveSpeed=8, ContactDamage=9999 |
| `SG_Base.asset` | 基地：[Health], MaxHp=100 |
| `SG_Enemy_Normal.asset` | 普通敌机：[Health, Movement, Collision, AI], MoveSpeed=2, HP=20 |
| `SG_Enemy_Fast.asset` | 快速敌机：[Health, Movement, Collision, AI], MoveSpeed=4, HP=20 |

**具体问题**：
1. 上述 4 个 SO 资产的列表正确吗？还是需要更多（如基地不同初始 HP 的变体）？
2. 存放路径是 `Assets/_Game/Configs/Entity/ShooterGame/` 还是 `Assets/_Game/Configs/ShooterGame/`？
3. 命名前缀 `SG_` 是否与项目既有模板命名规范 `Template_` 兼容？（`EntityTemplateSO_Creator` 创建的模板用 `Template_` 前缀）
4. 基地第 3/4/5 关初始 HP 不同——是同一个 SO 运行时修改 `MaxHp`？还是创建多个基地变体 SO？

**潜在风险**：配置资产管理不清晰 → 后续关卡扩展时 SO 膨胀

**建议方向**：
- 4 个 SO 资产足够 V1
- 路径 `Assets/_Game/Configs/ShooterGame/`
- 基地初始 HP：运行时通过 `HealthComponent.SetCurrentHp(maxHp * ratio)` 设置（单 SO，关卡参数化）
- 命名前缀 `SG_`（项目级前缀覆盖框架模板前缀）

**状态**：🟡 待回应

---

### SGT-005 | 严重度 🟡中 | §5.7 Luban vs ScriptableObject 的关卡数据管线未确定

**涉及章节**：§5.7

**质疑**：

文档说"哪种方案更好取决于实际需求"——但这是 V1 必须回答的问题，因为它直接决定：
1. 策划的实际操作方式（SO Inspector vs Excel/JSON → Luban 导表）
2. 构建管线是否需要包含 Luban 导表步骤
3. 关卡数据 Hot Reload 能力（SO 改了 Play Mode 下立即生效 vs 需要重新导表 + 重启）

框架已有 Luban 集成（`Packages/com.code-philosophy.luban`），但 `EntitySpawnWaveSO` 是纯 SO 路径。这两者混用会让关卡数据管线变得模糊。

**具体问题**：
1. V1 关卡数据走纯 SO 路径（5 个 `EntitySpawnWaveSO` 资产）还是走 Luban Excel → 代码生成？
2. 如果走 SO 路径，是否需要在 `BuildPipeline.cs` 中添加 `EntitySpawnWaveSO` 的预构建校验（检查每个关卡 SO 的 Waves 非空、Group.EntityConfig 非空）？

**潜在风险**：不确定 = 两种都半做 = 两种都不好用

**建议方向**：
- V1 纯 SO 路径：最简、Hot Reload 友好、独立开发者不需要 Excel 流程
- 在 `EntityConfigValidator.cs` 中已有 EntitySpawnWaveSO 校验（Waves 非空 + Group 完整性），可复用
- Luban 留给未来 V2 数据量大时迁移

**状态**：🟡 待回应

---

### SGT-006 | 严重度 🟡中 | §4.4 关卡切换的 FloatVariable SO 传递方案缺 Editor 验证

**涉及章节**：§4.4

**质疑**：

文档定义：
> 选关 → 设置 `CurrentLevelIndex` FloatVariable SO → 战斗场景 Bootstrap 读取对应 WaveConfig

这个数据流涉及：
1. FairyGUI UI 代码写入 `FloatVariable.Value`
2. Bootstrap 读取 `FloatVariable.Value` 并映射到 `EntitySpawnWaveSO[]` 数组索引

**具体问题**：
1. 如果 `CurrentLevelIndex` = 3 但只有 5 个 WaveConfig SO——是否需要编辑器时校验索引越界？
2. WaveConfig SO 数组是放在哪里？Bootstrap MonoBehaviour 的 `[SerializeField] EntitySpawnWaveSO[] _levelConfigs;`？还是一个专门的 `LevelConfigSO`？
3. `FloatVariable` 用来传 int 索引——为什么不用 `IntVariable`？（框架已有 `IntVariable` + `IntVariableDrawer`）

**潜在风险**：
- 运行时 IndexOutOfRange 没有编辑器预防
- FloatVariable 存 int 值有精度风险（虽然 5 以内不会出问题，但概念不清晰）

**建议方向**：
- 改用 `IntVariable`（语义正确）
- WaveConfig 数组放在 Bootstrap 的 `[SerializeField]` 上
- `EntityConfigValidator.cs` 扩展：校验场景 Bootstrap 引用的 WaveConfig SO 数量与解锁关卡数匹配

**状态**：🟡 待回应

---

### SGT-007 | 严重度 🟡中 | §3.2 BulletPatternSO 无 CustomEditor——策划配置子弹模式无引导

**涉及章节**：§3.2

**质疑**：

文档说"走 `AttackComponent` + `BulletPatternSO`"。但搜索确认 **BulletPatternSO 没有 CustomEditor**（只在 SOCreationWizard 中被列出可创建）。

`BulletPatternSO` 是策划配置飞机射击模式的核心入口，需要设置：
- BulletType 引用
- 发射数量/角度/间隔
- 多弹幕组合

没有 CustomEditor 意味着策划看到的是一个平铺的字段列表，没有条件显示、没有配置完整性校验。

**具体问题**：
1. V1 只有"单发直射"一种模式——是否意味着只需要 1 个 BulletPatternSO 资产？
2. 如果只有 1 个且参数固定，是否需要 CustomEditor？还是 V1 跳过、V2 武器升级系统时再加？
3. 现有 `BulletTypeSOEditor` 已有子图选择按钮——BulletPatternSO 是否至少需要一个 HelpBox 提示"V1 仅支持单发直射"？

**潜在风险**：中等——V1 只有 1 种子弹模式时影响小，但 V2 扩展时必须补

**建议方向**：V1 跳过（1 个 SO、参数固定），V2 列为工具需求

**状态**：🟡 待回应

---

### SGT-008 | 严重度 🟡中 | §2.2 打击反馈涉及多个未定义的 VFX/SFX 配置入口

**涉及章节**：§2.2

**质疑**：

打击反馈列了一长串效果：
- 敌机闪白 → 走 EntityConfigSO.HitFlashDuration/HitFlashColor ✅ 已有
- 击退 → MovementComponent.ApplyKnockback ✅ 已有
- 碎屑粒子 → 走什么？ParticleSystem Prefab 引用在哪配？
- 爆炸特效 → EntityConfigSO.DeathEffect ✅ 已有
- 屏幕震动 → ScreenShakeConfigSO ❌ 不存在
- 击杀积分飘字 → 走什么 UI 通道？FairyGUI 动态创建？
- 血条扣减动画 → FairyGUI UI Tween？还是 DOTween？
- 红色闪烁 → Camera overlay？Post-Processing？

**具体问题**：
1. 这些反馈效果中，哪些是 V1 必须有的，哪些是 V2 再加？
2. "碎屑粒子"引用放在 EntityConfigSO.HitEffect 还是单独字段？
3. "击杀积分飘字"是否走现有框架的 `ShowDamageNumber` 系统（EntityConfigSO 已有该字段）？
4. 请给出一个 **V1 打击反馈实现优先级表**

**潜在风险**：反馈效果分散在多个系统（Entity VFX、FairyGUI、Camera、Audio），实施时容易遗漏

**建议方向**：
- V1 P0（必须有）：闪白 + 击退 + 爆炸特效 + 屏幕震动（最小 shake）
- V1 P1（应该有）：碎屑粒子 = EntityConfigSO.HitEffect、击杀飘字 = ShowDamageNumber
- V2：积分飘字、血条动画、红色闪烁、BGM 切换

**状态**：🟡 待回应

---

### SGT-009 | 严重度 🟢低 | §6.2 美术资源命名规范与 AssetImportEnforcer 规则的兼容性

**涉及章节**：§6.2

**质疑**：

文档定义命名规范：`{类型}_{名字}_{序号}.png`（如 `enemy_normal_01.png`）

现有 `TextureImportEnforcer` 通过后缀 `_N` 检测法线贴图。如果美术命名中出现 `..._N.png`（如 `enemy_normal_N.png` 缩写），会被误判为法线贴图并自动修改 TextureType。

**具体问题**：
1. 美术命名中是否会出现以 `_N` 结尾的文件名？如果有，需要在 TextureImportEnforcer 中加排除规则。
2. 纹理最大尺寸 1024px 对 ShooterGame 的 Sprite 够用吗？（文档建议 64×64px 的小 sprite，完全够）
3. 是否需要为 ShooterGame 的 Sprite Atlas 在 AssetImportEnforcer 中加特殊规则？

**潜在风险**：低——命名冲突概率不高，但值得确认

**建议方向**：
- 明确 ShooterGame 美术资源不使用 `_N` 后缀
- 1024px 限制对 64px sprite 完全足够
- V1 不需要特殊规则

**状态**：🟡 待回应

---

### SGT-010 | 严重度 🟢低 | §4.4/§9.3 构建前验证是否需要扩展 ShooterGame 专项检查

**涉及章节**：§4.4, §9.3

**质疑**：

现有 `BuildPipeline.cs` 在构建前运行 `ArchitectureValidator.RunValidation()`，但这只检查代码规范（禁止 GameObject.Find 等）。

ShooterGame 引入了新的构建前需要验证的项：
1. 所有 5 个 WaveConfig SO 非空且配置完整
2. Player/Base/Enemy EntityConfigSO 存在且必填字段完整
3. 场景 Build Settings 中包含 MainMenu + Battle scene
4. FloatVariable/IntVariable SO 存在

**具体问题**：
1. 是否需要在 BuildPipeline 中加 ShooterGame 专项构建前检查？
2. 还是复用 `EntityConfigValidator.ValidateAll()` 就足够了？
3. 如果加，是 Warning 级别还是 Error 级别（阻断构建）？

**潜在风险**：低——构建验证是防御性工具，V1 可以延后

**建议方向**：
- V1 复用 `EntityConfigValidator.ValidateAll()` 即可（已覆盖 SO 完整性）
- 场景校验在 BuildPipeline 中已有 `GetEnabledScenes()` 检查空场景列表
- V2 再考虑构建时 Error 阻断

**状态**：🟡 待回应

---

## Round 1 统计

| 问题编号 | 严重度 | 维度 | 核心关切 |
|----------|--------|------|----------|
| SGT-001 | 🔴高 | 策划工作流 | EntitySpawnWaveSOEditor 能力不足以支撑 5 关编辑 |
| SGT-002 | 🔴高 | 缺失依赖 | ScreenShakeConfigSO 不存在 |
| SGT-003 | 🟡中 | 调试可视化 | 基地底线检测无 Gizmo/调试支持 |
| SGT-004 | 🟡中 | 资产管理 | SO 资产清单/路径/命名未明确 |
| SGT-005 | 🟡中 | 数据管线 | Luban vs SO 路径未确定 |
| SGT-006 | 🟡中 | 数据传递 | FloatVariable 传 int 索引 + 缺越界校验 |
| SGT-007 | 🟡中 | 编辑器 DX | BulletPatternSO 无 CustomEditor |
| SGT-008 | 🟡中 | 实施完整性 | 打击反馈效果散落多系统、优先级不清 |
| SGT-009 | 🟢低 | 导入规则 | 美术命名与 _N 后缀冲突 |
| SGT-010 | 🟢低 | 构建验证 | ShooterGame 专项构建前检查 |

**攻方总结**：10 个问题，聚焦"策划工具链 DX"和"设计文档引用了不存在的技术组件"两大主题。核心诉求：设计文档应明确 V1 实施所需的 **编辑器工具最小集**，而不是留下"编码期再说"的模糊地带。

---

## PK Round 2 — 守方回应（游戏设计师）

> 逐条回应。立场：承认合理质疑，拒绝过度工程化，所有回答以"独立开发者单人迭代"的实际场景为基准。

### SGT-001 回应 | ✅ 部分接受

**核心观点**：攻方说得对——纯 Inspector 嵌套展开确实痛苦。但要区分"痛苦程度"和"V1 值得的投入"。

**事实澄清**：
- V1 一共 5 关，每关 4~6 波，每波 1~3 种敌机 × 1~5 只。**总数据量约 25~30 个 SpawnWaveEntry**，不是一千行 Excel。
- 独立开发者 = 策划本人 = 不需要"对策划友好"的高 DX 工具——需要的是"不出错"和"能快速复制调整"。
- 预估：从零配一关约 10 分钟，调试迭代约 3 分钟/次。对 5 关总量来说可控。

**接受的改善（3 条够了）**：
1. ✅ **一键复制最后一波** — 1 小时工作量，直接省重复填写
2. ✅ **总敌机/总时长统计面板** — 已有摘要面板基础，扩展统计字段即可
3. ⚠️ **波次折叠面板** — 有用但优先级不如上两条，V1 如果顺手加就加

**拒绝的改善**：
- ❌ X 坐标可视化 / 时间轴预览 → V2 时间线编辑器的活，V1 不做
- ❌ 独立 EditorWindow → Inspector 改善已足够 V1 数据量

**对第 3 个子问题的回答**：
- `SpawnGroup` 中已有 `SpawnXRange: Vector2`（minX, maxX），这在框架 SpawnWaveSO 里是现成字段。文档 §5.6 的描述与代码一致。

**结论**：SGT-001 接受"一键复制 + 统计面板"两条改善写入 TDD 工具需求。

---

### SGT-002 回应 | ✅ 完全接受

**核心观点**：攻方指出得好。ScreenShakeConfigSO 确实只在设计文档中被引用，实体未创建——这是 GDD 层面的"意图声明"而非"实现承诺"。

**明确回答四个子问题**：

1. **最小字段集**：`Duration(float)` + `Intensity(float)` + `DecayCurve(AnimationCurve)` — 三字段够了
2. **实现方式**：Camera Transform 位置偏移（Perlin Noise 采样）。❌ 不用 Post-Processing（微信小游戏不友好 + 过重）
3. **CustomEditor**：V1 不需要。3 个字段 + 一个 AnimationCurve（Unity 默认 CurveField 已经足够直觉）
4. **创建时间点**：在 TDD 中明确，实施阶段第一天就创建 SO 定义。GDD 回写补充"TDD 阶段创建"。

**结论**：SGT-002 完全接受。GDD 回写增加一句 "ScreenShakeConfigSO 在 TDD 阶段定义，最小字段集 [Duration, Intensity, DecayCurve]，实现为 Camera Transform 偏移。"

---

### SGT-003 回应 | ✅ 部分接受

**核心观点**：基地底线可视化确实需要，但实现代价极低，不需要专门的 ViewPrefab。

**明确回答**：

1. **基地 ViewPrefab**：**不需要**。基地不在世界空间渲染——它是逻辑概念（底线 Y），视觉由 FairyGUI HP 血条 Overlay 表达。给它一个 ViewPrefab 反而浪费 DrawCall。
2. **BaseLineY Gizmo**：同意攻方建议——BattleController/Bootstrap MonoBehaviour 的 `OnDrawGizmos()` 画横线。代码 3 行：`Gizmos.color = Color.red; Gizmos.DrawLine(new Vector3(-5, baseLineY, 0), new Vector3(5, baseLineY, 0));` 完事。
3. **基地 HP 调试**：走 EntityDebugWindow 即可——基地也是 Entity，自动出现在 Active Entity 列表里，HP 字段实时可见。

**结论**：接受"BaseLineY Gizmo"需求。在 TDD 中列为 BattleController 的 `OnDrawGizmos` 实现项。不做 ViewPrefab。

---

### SGT-004 回应 | ✅ 完全接受

**核心观点**：SO 资产清单应该在 GDD 数值表旁边明确列出。攻方推导的 4 个 SO 资产完全正确。

**明确回答**：

1. **4 个 SO 正确**：Player / Base / Enemy_Normal / Enemy_Fast。V1 不需要更多。
2. **存放路径**：`Assets/_Game/Configs/ShooterGame/` — 独立游戏目录隔离
3. **命名前缀**：`SG_` — 这是游戏级资产（区别于框架模板 `Template_`）。不冲突——`Template_` 是框架示例资产前缀，`SG_` 是 ShooterGame 特有前缀。
4. **基地 HP 参数化**：**单 SO + 运行时参数化**。每关 WaveConfig 中附带 `BaseInitialHpRatio: float`，Bootstrap 加载时 `healthComp.SetMaxHp(configHP * ratio)`。不搞 SO 变体。

**结论**：接受全部建议。GDD 回写补充 SO 资产清单表（4 资产 + 路径 + 命名规范）。

---

### SGT-005 回应 | ✅ 完全接受

**核心观点**：V1 纯 SO 路径，没有任何犹豫。

**决策理由**：
- 独立开发者不需要 Excel 流程——没有"策划团队"需要离开 Unity 编辑数据
- `EntitySpawnWaveSO` 在 Play Mode 下修改后可以 `Apply` → **准 Hot Reload**
- Luban 集成仅用于未来大规模数据迁移（数百条数值表时），5 关数据用 SO 恰到好处
- `EntityConfigValidator` 已有 SpawnWaveSO 校验（Waves 非空、Group.EntityConfig 引用非空）——直接复用

**结论**：明确写入 GDD——"V1 关卡数据管线 = 纯 SO 路径（5 个 EntitySpawnWaveSO 资产），Luban 路径保留为 V2 扩展选项。"

---

### SGT-006 回应 | ✅ 完全接受

**核心观点**：攻方说得完全对——`FloatVariable` 传 int 索引概念不清晰。

**修正方案**：
1. ✅ 改用 `IntVariable`（框架已有 `IntVariable` + `IntVariableDrawer`）
2. ✅ WaveConfig 数组放 Bootstrap `[SerializeField] EntitySpawnWaveSO[] _levelConfigs;`
3. ✅ 越界防护：Bootstrap 加载时 `Mathf.Clamp(index, 0, _levelConfigs.Length - 1)` + `Debug.LogWarning`

**关于编辑器校验**：
- 不需要专门扩展 `EntityConfigValidator`——运行时 Clamp + LogWarning 已足够 V1
- 5 关 = 5 个 SO 槽位，场景中拖齐就完事。如果真漏了一个，Play 一下立刻 NullRef 暴露

**结论**：GDD 回写：`FloatVariable` → `IntVariable`，WaveConfig 数组在 Bootstrap SerializeField。

---

### SGT-007 回应 | ✅ 接受 V1 跳过

**核心观点**：攻方自己给出了最优答案——V1 只有 1 种子弹模式（单发直射），1 个 BulletPatternSO 资产，参数固定。

**补充说明**：
- V1 玩家射击 = 直线子弹，发射间隔 0.15s，单发。这一个 SO 配好后**基本不会再改**。
- CustomEditor 对"配好后不再改"的 SO 没有价值——投入产出不成立
- V2 武器升级系统引入多弹幕模式时，必须补 CustomEditor（条件显示 + 弹幕预览）

**关于 HelpBox**：不需要——SO 本身叫 `SG_PlayerBullet_Straight.asset`，命名已说明一切。

**结论**：GDD 不改动。V1 跳过 BulletPatternSO Editor，V2 工具需求列表中列入。

---

### SGT-008 回应 | ✅ 部分接受

**核心观点**：攻方要的"V1 打击反馈优先级表"确实应该写在 GDD 里。

**V1 打击反馈实现优先级表**：

| 优先级 | 效果 | 系统归属 | 配置入口 |
|--------|------|----------|----------|
| P0 必须 | 敌机闪白 | EntityHitReactionHandler | EntityConfigSO.HitFlashDuration/Color |
| P0 必须 | 击退 | MovementComponent | EntityConfigSO.KnockbackForce |
| P0 必须 | 爆炸特效 | Entity View → Prefab Particle | EntityConfigSO.DeathEffect |
| P0 必须 | 屏幕震动 | Camera Shake（新增） | ScreenShakeConfigSO |
| P1 应有 | 碎屑粒子 | Entity View → HitEffect | EntityConfigSO.HitEffect |
| P1 应有 | 击杀飘字 | ShowDamageNumber 系统 | EntityConfigSO.ShowDamageNumber=true |
| V2 | 积分飘字 | FairyGUI 动态 | 专用 ScorePopup 组件 |
| V2 | 基地受伤红闪 | Camera Overlay | 独立 RedFlash 组件 |
| V2 | 血条动画 | FairyGUI Tween | UI 层实现 |

**结论**：GDD 回写补充此优先级表（放 §2.2 打击感小节末尾）。

---

### SGT-009 回应 | ✅ 接受，确认无冲突

**核心观点**：好问题，但实际不会冲突。

**原因**：
- ShooterGame 美术资源命名规范：`{类型}_{名字}_{序号}.png`
- 举例：`enemy_normal_01.png`, `enemy_fast_01.png`, `player_ship_01.png`
- **不会**出现以 `_N` 结尾的文件名——因为序号总是数字结尾
- 即使未来加法线贴图（V1 是 2D Sprite 不需要法线），也会用完整后缀 `_Normal` 而非缩写 `_N`

**额外确认**：1024px 限制对 64px sprite 完全足够，不需要特殊规则。

**结论**：GDD 回写补一句"美术资源不使用 `_N` 后缀，避免与 TextureImportEnforcer 规则冲突。"

---

### SGT-010 回应 | ✅ 接受现状足够

**核心观点**：V1 不需要专项构建检查。

**理由**：
- `EntityConfigValidator.ValidateAll()` 已覆盖 SO 完整性（包括 SpawnWaveSO 引用非空检查）
- 场景 Build Settings 由 `BuildPipeline.cs` 的 `GetEnabledScenes()` 自动检查
- 5 关数据量小到"Play 一下就知道少了什么"——不值得为此加构建阻断
- 独立开发者：编译报错 + Play 即测 = 已经是最短反馈循环

**V2 升级条件**：当关卡数 >10 或 SO 资产 >20 时，增加 Warning 级别构建前日志

**结论**：GDD 不改动。V1 复用现有校验，V2 评估是否需要 Error 级阻断。

---

## Round 2 守方统计

| 问题编号 | 守方裁定 | 具体动作 |
|----------|----------|----------|
| SGT-001 | 部分接受 | 接受"一键复制波次 + 统计面板"，拒绝 X 可视化/独立窗口 |
| SGT-002 | 完全接受 | GDD 回写 ScreenShakeConfigSO 最小定义 |
| SGT-003 | 部分接受 | 接受 BaseLineY Gizmo，拒绝 ViewPrefab |
| SGT-004 | 完全接受 | GDD 回写 SO 资产清单表 |
| SGT-005 | 完全接受 | GDD 明确 V1 = 纯 SO 路径 |
| SGT-006 | 完全接受 | FloatVariable → IntVariable |
| SGT-007 | 接受跳过 | V1 不做，V2 列入工具需求 |
| SGT-008 | 部分接受 | GDD 回写打击反馈优先级表 |
| SGT-009 | 确认无冲突 | GDD 补一句命名排除说明 |
| SGT-010 | 接受现状 | V1 复用现有校验 |

**守方总结**：10 个问题中完全接受 5 个、部分接受 3 个、确认无问题 2 个。守方没有否决任何一条——因为攻方质疑确实合理。关键分歧仅在于"做多少"：独立开发者场景下，V1 工具投入的 ROI 必须精算，不能"预防一切风险"变成"工具开发完游戏还没做"。

---

## PK Round 3 — 攻方反驳/追问（Unity 编辑器工具开发者）

> 守方回应质量很高，大部分接受后结论清晰。攻方聚焦 3 个仍有模糊/遗漏的点做精确追问，不再纠缠已收敛条目。

### SGT-001 追问 | 关于"一键复制最后一波"的实现规格

**守方说**：接受一键复制 + 统计面板。1 小时工作量。

**追问**：

"一键复制最后一波"的具体行为需要定义，否则实施时会踩坑：

1. **复制的是整个 Wave（含所有 SpawnGroup）？还是只复制最后一个 SpawnGroup？**
   - 如果复制 Wave：新波次的 `spawnDelay` 应该自动递增还是和源波次相同？
   - 如果相同 delay → 策划还要手动改 → 体验没好多少

2. **复制后光标定位**：新复制的波次应该自动展开显示吗？Inspector 里嵌套数组展开到第 6 层很容易迷路。

3. **建议规格**：
   - 行为 = 深拷贝 `Waves[last]` → 追加到 `Waves` 末尾
   - 新波次的 `spawnDelay` = 源波次 delay + 默认间隔（如 3 秒）
   - 复制后自动折叠旧波次、展开新波次
   - 按钮位置：放在 Waves 列表底部（Inspector 的 "+" 按钮旁边）

**期望守方回答**：是否同意上述规格？还是有更简单的方案？

**状态**：🟡 待回应

---

### SGT-004 追问 | 基地 HP 参数化字段放在哪个 SO 中

**守方说**：单 SO + 运行时参数化，WaveConfig 附带 `BaseInitialHpRatio`。

**追问**：

这引发一个数据归属问题：

1. **`BaseInitialHpRatio` 放在 `EntitySpawnWaveSO` 里？** 这意味着每个 WaveConfig SO 额外多一个字段。但从语义上看，`EntitySpawnWaveSO` 的职责是"定义敌机波次"，混入"基地 HP"属于关注点交叉。

2. **替代方案**：创建一个极简的 `LevelConfigSO`（每关一个）：
   ```
   [CreateAssetMenu] LevelConfigSO:
     - EntitySpawnWaveSO WaveConfig;
     - float BaseHpRatio = 1.0;
     - int UnlockStarRequirement = 0;
   ```
   这样每关的"元数据"有统一入口，Bootstrap 只读一个 `LevelConfigSO[]`。

3. **问题核心**：如果直接在 `EntitySpawnWaveSO` 上加 `BaseInitialHpRatio`——这是框架级的 SO 类，ShooterGame 的字段会"污染"框架代码。是否应该在游戏层做一个 wrapper？

**期望守方回答**：`BaseInitialHpRatio` 的字段归属——放 WaveSO（框架级）？还是新建 LevelConfigSO（游戏级）？

**状态**：🟡 待回应

---

### SGT-008 追问 | ScreenShake 触发时机的精确定义

**守方说**：ScreenShakeConfigSO 完全接受，Camera Transform 偏移实现。

**追问**：

设计文档 §2.2 说"敌机撞飞机 = 屏幕震动"。但 v3.0 同时说"飞机 ContactDamage=9999 → 一撞即杀敌机"。这意味着：
- 飞机撞敌机 → 敌机死 → 有爆炸特效（DeathEffect）→ **此时要不要也震屏？**
- 基地被突破 → 扣 HP → **此时要不要震屏？**

**需要明确的触发点**：

| 事件 | 是否触发 ScreenShake | 强度 |
|------|---------------------|------|
| 玩家子弹击杀敌机 | ❓ | 弱 |
| 飞机撞击杀敌机 | ❓ | 中 |
| 敌机突破底线（基地扣血） | ❓ | 强 |

**期望守方回答**：ScreenShake 只在"基地受伤"时触发？还是多事件触发、不同强度？如果多事件，是同一个 SO 的单一配置还是多个 SO 资产？

**状态**：🟡 待回应

---

### Round 3 已收敛条目（无需追问）

| 编号 | 状态 |
|------|------|
| SGT-002 | ✅ 完全收敛（ScreenShakeConfigSO 定义已明确） |
| SGT-003 | ✅ 完全收敛（BaseLineY Gizmo = OnDrawGizmos 3 行） |
| SGT-005 | ✅ 完全收敛（V1 纯 SO 路径） |
| SGT-006 | ✅ 完全收敛（IntVariable + Clamp） |
| SGT-007 | ✅ 完全收敛（V1 跳过） |
| SGT-009 | ✅ 完全收敛（确认无冲突） |
| SGT-010 | ✅ 完全收敛（复用现有校验） |

**攻方 Round 3 总结**：7/10 已收敛。剩余 3 个追问均为"具体实施规格确认"——不再质疑方向，只需对齐细节。

---

## PK Round 4 — 守方二次回应（游戏设计师）

> 3 个追问都是好问题。快速对齐。

### SGT-001 二次回应 | ✅ 同意攻方规格（微调）

攻方建议的规格合理，逐条确认：

1. **复制粒度**：深拷贝整个 Wave（含所有 SpawnGroup）。✅ 同意。
2. **spawnDelay 自动递增**：✅ 同意。`newWave.spawnDelay = lastWave.spawnDelay + lastWave.estimatedDuration + 3f`（3 秒默认间歇）。但不需要太精确——策划（我自己）复制后必然要手调 delay，自动递增只是"默认值比 0 好"。
3. **自动展开**：✅ 同意。新波次展开、旧波次折叠。
4. **按钮位置**：放 Waves 列表底部。✅ 同意。

**微调**：按钮文案建议 `"+ 复制最后一波"` 而非 `"Copy Last Wave"`——中文比英文直觉，独立开发者无国际化需求。

**SGT-001 最终结论**：✅ 完全收敛。

---

### SGT-004 二次回应 | ✅ 接受 LevelConfigSO 方案

攻方切中要害——`BaseInitialHpRatio` 不应该放框架级 `EntitySpawnWaveSO`。

**决策**：新建游戏级 `SG_LevelConfigSO`（放 `Assets/_Game/Scripts/ShooterGame/Config/` 下）

```
[CreateAssetMenu(menuName = "ShooterGame/LevelConfig")]
public class SG_LevelConfigSO : ScriptableObject
{
    public EntitySpawnWaveSO WaveConfig;
    public float BaseHpRatio = 1.0f;
    public int UnlockRequirement = 0; // 前一关需要几星解锁
}
```

**理由**：
- 不污染框架 SO
- 每关一个 `SG_LevelConfigSO`（5 个资产），Bootstrap 读 `SG_LevelConfigSO[]`
- 未来扩展方便：可以加 `BackgroundType`、`BossWaveConfig`、`DifficultyModifier` 等关卡元数据
- `IntVariable` 的 `CurrentLevelIndex` → 索引到 `SG_LevelConfigSO[]`

**SO 资产清单更新**（完整 V1 列表）：

| SO 资产 | 类型 | 路径 |
|---------|------|------|
| SG_Player | EntityConfigSO | `Configs/ShooterGame/` |
| SG_Base | EntityConfigSO | `Configs/ShooterGame/` |
| SG_Enemy_Normal | EntityConfigSO | `Configs/ShooterGame/` |
| SG_Enemy_Fast | EntityConfigSO | `Configs/ShooterGame/` |
| SG_Level_01~05 | SG_LevelConfigSO | `Configs/ShooterGame/Levels/` |
| SG_Wave_01~05 | EntitySpawnWaveSO | `Configs/ShooterGame/Waves/` |
| SG_PlayerBullet_Straight | BulletPatternSO | `Configs/ShooterGame/` |
| SG_ScreenShake_Default | ScreenShakeConfigSO | `Configs/ShooterGame/` |
| SG_CurrentLevelIndex | IntVariable | `Configs/ShooterGame/Variables/` |
| **总计** | **14 个 SO 资产** | |

**SGT-004 最终结论**：✅ 完全收敛。

---

### SGT-008 二次回应 | ✅ 明确触发点

好问题。明确 ScreenShake 触发规则：

| 事件 | 触发 ScreenShake | 强度 | 理由 |
|------|-----------------|------|------|
| 玩家子弹击杀敌机 | ❌ 不触发 | — | 频率太高（每秒 6-7 发），持续震动 = 没震动 |
| 飞机撞击杀敌机 | ✅ 触发 | 中等 (0.15s, 0.3 intensity) | 撞击是高风险操作，需要反馈强化 |
| 敌机突破底线 | ✅ 触发 | 强 (0.3s, 0.6 intensity) | 基地受伤 = 惩罚信号，必须明确 |

**SO 配置方案**：
- **单个 `ScreenShakeConfigSO`**，但提供 **预设数组**：
  ```
  ScreenShakeConfigSO:
    Presets: ScreenShakePreset[]
      - Name: "CollisionShake"   Duration=0.15 Intensity=0.3
      - Name: "BaseHitShake"     Duration=0.3  Intensity=0.6
  ```
- 调用方通过 preset name 触发：`ScreenShakeManager.Shake("CollisionShake")`
- 好处：单个 SO 管理所有震动配置，不做 SO 膨胀

**替代方案（更简单）**：
- 不用 preset 数组，直接 2 个字段组 `CollisionShake` + `BaseHitShake`（各自 Duration + Intensity + Curve）
- V1 只有 2 种触发事件，2 个字段组 = 6 个字段，不需要数组抽象

**守方倾向**：替代方案（更简单的 2 字段组）。V1 不需要通用预设系统。

**SGT-008 最终结论**：✅ 完全收敛。

---

## Round 4 守方统计

| 追问编号 | 结论 |
|----------|------|
| SGT-001 追问 | ✅ 完全收敛 — 同意深拷贝规格 + 中文按钮 |
| SGT-004 追问 | ✅ 完全收敛 — 新建 SG_LevelConfigSO + 14 SO 资产清单 |
| SGT-008 追问 | ✅ 完全收敛 — 2 字段组（CollisionShake + BaseHitShake） |

**守方 Round 4 总结**：10/10 全部收敛。攻守双方在"做什么"和"怎么做"上已无分歧。

---

## PK Round 5 — 攻方终轮确认（Unity 编辑器工具开发者）

> 全部 10 个问题已收敛。攻方确认无新质疑。

**确认结论**：守方回应务实、决策清晰、拒绝合理。作为编辑器工具开发者，认可以下结果：

1. **工具投入适度**：V1 只做 2 项 Editor 改善（复制波次 + 统计面板），ROI 合理
2. **缺失组件已明确**：ScreenShakeConfigSO 定义已完整，2 字段组方案简单有效
3. **数据架构清晰**：SG_LevelConfigSO 隔离游戏级关卡元数据，不污染框架
4. **SO 清单完整**：14 个资产、路径、命名规范全部对齐
5. **调试可视化**：BaseLineY Gizmo + EntityDebugWindow 覆盖调试需求
6. **管线明确**：纯 SO 路径 + IntVariable + Clamp 防护
7. **打击反馈有序**：P0/P1/V2 优先级表确保不遗漏、不过做

**攻方唯一补充建议**（非质疑）：

建议 GDD 新增一个小节 §X "V1 编辑器工具与配置清单"，汇总所有 PK 收敛的结论：
- Editor 改善清单（2 项）
- SO 资产清单（14 个）
- 新增 SO 类型定义（SG_LevelConfigSO, ScreenShakeConfigSO）
- Gizmo 需求（BaseLineY）
- 打击反馈优先级表

这样 TDD 阶段直接从这个清单展开，不需要翻 PK 记录。

**攻方最终裁定**：✅ 全部通过，无遗留问题。

---

## PK Round 6 — 最终收敛判定

### 收敛状态

| 问题编号 | 最终状态 | 收敛轮次 |
|----------|----------|----------|
| SGT-001 | ✅ 收敛 | R4 |
| SGT-002 | ✅ 收敛 | R2 |
| SGT-003 | ✅ 收敛 | R2 |
| SGT-004 | ✅ 收敛 | R4 |
| SGT-005 | ✅ 收敛 | R2 |
| SGT-006 | ✅ 收敛 | R2 |
| SGT-007 | ✅ 收敛 | R2 |
| SGT-008 | ✅ 收敛 | R4 |
| SGT-009 | ✅ 收敛 | R2 |
| SGT-010 | ✅ 收敛 | R2 |

### PK 评审结论

- **总问题数**：10
- **收敛率**：100%（10/10）
- **实际使用轮次**：4 轮（Round 5-6 为确认+判定，无新争议）
- **GDD 需回写条目数**：8 处修改

### GDD 回写清单（✅ 已完成）

| # | 修改位置 | 修改内容 | 状态 |
|---|----------|----------|------|
| 1 | §2.2 末尾 | 新增"V1 打击反馈优先级表" + ScreenShake 触发规则 | ✅ |
| 2 | §3.1 | 补充 ScreenShakeConfigSO 定义（2 字段组 + 实现方式）| ✅ |
| 3 | §3.4 附近 | 新增"V1 SO 资产清单表"（14 个） | ✅ |
| 4 | §3.6 基地 | 补充"无 ViewPrefab + BaseLineY Gizmo + EntityDebugWindow" | ✅ |
| 5 | §4.4 关卡切换 | `FloatVariable` → `IntVariable`；新增 `SG_LevelConfigSO` 定义 | ✅ |
| 6 | §5.3 + §5.7 | V1 纯 SO 路径 + Editor 改善需求（一键复制 + 统计面板） | ✅ |
| 7 | §6.2 命名 | 补充"不使用 _N 后缀" | ✅ |
| 8 | 新增 §十一 | 编辑器工具与配置清单汇总（Editor + SO 类型 + Gizmo + 校验） | ✅ |

> GDD 版本 v3.0 → v3.1，回写完成时间：2026-05-03 15:43

### PK 最终评价

**攻方视角**：设计文档 v3.0 在玩法层面成熟度高，但**编辑器工具/策划工作流维度几乎空白**——这正是 PK 评审的价值所在。经过 4 轮对齐，文档将补充完整的实施级工具需求，TDD 阶段可直接消费。

**守方视角**：攻方质疑合理且克制——没有要求建设"完整的时间线编辑器"或"可视化配置工具"，所有建议的投入产出比都适合独立开发者场景。唯一需要警惕的是：别让工具需求 creep 进来把开发周期拉长。

> **PK 状态**：✅ 已完成
> **结束时间**：2026-05-03 08:17
