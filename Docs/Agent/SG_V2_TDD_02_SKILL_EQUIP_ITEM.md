---
system: shootergame-v2-tdd
scope: sprint2-skill-equip-item
last_verified: 2026-05-30
depends_on: [SG_V2_TDD_INDEX, SG_V2_TDD_01_ENEMY_SHOOTING, SG_GDD_01_ACTIVE_SKILLS, SG_GDD_03_ITEMS_CONFIG]
version: v1.6
related_code: Assets/_Framework/EntitySystem/Components/Skill*, Assets/_Game/Scripts/ShooterGame/**
---

# Sprint 2：技能解锁 + 战前装备 + 道具系统（~14h）

> **目标**：玩家飞机拥有 6 技能槽全自动释放 + 战前装备 + 道具掉落拾取。
> **前置**：Sprint 1 验收通过（敌弹存在才有"挡弹 vs 拾取道具"的策略决策）。

---

## 1. 实施任务分解

### S2.1 SkillUnlockTableSO / PassiveUnlockTableSO（2h）

#### 实施方案

**新增 `SkillUnlockTableSO`**：

```
SkillUnlockTableSO : ScriptableObject
├── [CreateAssetMenu] Configs/ShooterGame
├── Entry[] Entries:
│   ├── SkillConfigSO Skill
│   ├── UnlockConditionType ConditionType  // enum: Default, ClearLevel, Achievement
│   ├── int ConditionParam                 // ClearLevel=关卡编号, Achievement=成就ID
│   └── string Description                // "通关第 2 关解锁"
```

**V2 解锁表内容**（GDD §技能解锁条件）：

| 技能 | ConditionType | ConditionParam | 描述 |
|------|-------------|----------------|------|
| 散射弹幕 | Default | — | 默认解锁 |
| 追踪导弹 | ClearLevel | 2 | 通关第 2 关 |
| 激光射线 | ClearLevel | 4 | 通关第 4 关 |
| 火力全开 | ClearLevel | 6 | 通关第 6 关 |
| 护盾 | Achievement | 1 | 累计死亡 5 次 |
| 冲击波 | Achievement | 2 | 单关击杀 50 敌机 |

**新增 `PassiveUnlockTableSO`**：同构结构。

| 被动 | ConditionType | ConditionParam |
|------|-------------|----------------|
| 子弹穿透 | Default | — |
| 暴击强化 | ClearLevel | 3 |
| 磁吸范围 | ClearLevel | 5 |
| 尾翼反击 | Achievement | 3（累计被命中 30 次） |

**新增 `SkillUnlockManager`**：

```
SkillUnlockManager（纯 C# 服务，不继承 MonoBehaviour）
├── SkillUnlockTableSO _skillTable
├── PassiveUnlockTableSO _passiveTable
├── SG_ProgressManager _progress    // 读取存档
├── List<SkillConfigSO> GetUnlockedSkills():
│   → 遍历 _skillTable.Entries
│   → 根据 ConditionType 检查 _progress（ClearLevel / Achievement）
│   → 返回已解锁列表
├── List<PassiveAbilitySO> GetUnlockedPassives(): 同理
├── bool CheckNewUnlocks(out List<UnlockEntry> newlyUnlocked):
│   → 对比上次检查时的解锁列表，返回新增
```

**OnValidate 规格**（PK-R2 ET-003）：

```
SkillUnlockTableSO.OnValidate():
├── foreach Entry: Skill != null → LogError("Entry[{i}] 技能引用为空")
├── HashSet 去重：无重复 SkillConfigSO → LogError("重复技能: {name}")
├── ClearLevel 类 ConditionParam ∈ [1, 30] → LogWarning("关卡编号越界")

PassiveUnlockTableSO.OnValidate(): 同理
```

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| A1 | 默认解锁 | 新存档启动 | 散射弹幕+子弹穿透已解锁 | 列表长度正确 |
| A2 | 通关解锁 | 模拟通关第 2 关 | 追踪导弹变为已解锁 | CheckNewUnlocks 返回 |
| A3 | 成就解锁 | 模拟死亡 5 次 | 护盾解锁 | 同上 |
| A4 | 存档持久化 | 解锁后重启 | 解锁状态保留 | SG_ProgressManager 存取正确 |

---

### S2.2 解锁进度存档对接（2h）

#### 实施方案

**扩展 `SharedProgressData`**（V2 云存档 DTO）：

```
SharedProgressData（已有，扩展）:
├── [已有] int Version
├── [已有] List<int> ClearedLevels
├── [新增] List<string> UnlockedSkillIds    // SkillConfigSO 的资产名列表
├── [新增] List<string> UnlockedPassiveIds  // PassiveAbilitySO 的资产名列表
├── [新增] int TotalDeaths                  // 累计死亡次数（Achievement 用）
├── [新增] int MaxKillsInOneLevel           // 单关最高击杀（Achievement 用）
├── [新增] int TotalHitsTaken               // 累计被命中次数（PA-04 解锁用）
├── [新增] Dictionary<int, int> LevelStars  // 关卡→最高星级
```

**SG_ProgressManager 扩展**：
- 新增读写解锁列表的 API
- 新增成就计数器更新 API（OnDeath / OnKill / OnHit）
- 与 CloudSaveSystem V3 存储兼容（拉取覆盖模式）

**序列化兼容**：老存档无新字段时，反序列化默认为 null/0，SkillUnlockManager 自动补齐默认解锁项。

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| B1 | 新存档兼容 | 删除存档重启 | 默认解锁生效，新字段初始化 | 无报错 |
| B2 | 存档升级 | 加载 V1 老存档 | 新字段默认值，已有数据保留 | 向后兼容 |
| B3 | 解锁持久化 | 解锁→退出→重进 | 解锁状态存在 | 云存档同步 |
| B4 | 成就计数 | 累计死亡/击杀/被命中 | 计数器正确递增 | 精确匹配 |

---

### S2.3 出战准备 Bottom Sheet（3h）

> **⏳ 延后说明（2026-05-22 决策）**：S2.3 的 UI 实现与验收（C1~C8）整体延后至 **Sprint 5** 与选关界面 V2 升级（S5.7）一并开发和验收。
> - **原因**：数据管线（`BattleLevelData.EquippedSkills/Passives` → `SkillComponent` 多槽位 → `BattleController.Init`）已在 S2.6 打通，当前 `EquippedSkills=null` 走 `EntityConfigSO.SkillConfig` 单技能兜底不影响 S3/S4 功能开发。出战准备 UI 属表现层，不阻塞后续 Sprint。
> - **临时方案**（✅ 已实现）：`BattleDebugLauncher` 组件（`#if UNITY_EDITOR`），直跑 Battle 场景时在 Inspector 配置技能/被动组合，自动注入 `BattleLevelData`。
> - **S5 验收时**：C1~C8 全部验收 + S5.7 选关界面联调 + S5.8 最终整合测试。

#### 实施方案

**FairyGUI 包**：`Sortie`（独立包）

**面板类**：`SortieBottomSheet : IUIPanel`

```
SortieBottomSheet
├── 生命周期：
│   ├── OnInit(): 绑定 UI 元素
│   ├── OnShow(levelIndex):
│   │   → 从 SkillUnlockManager 获取已解锁技能/被动列表
│   │   → 填充技能横排图标（已解锁=正常，未解锁=灰色/???）
│   │   → 填充被动横排图标
│   │   → 敌机预告（读取关卡波次配置汇总敌机类型+数量）
│   │   → 默认自动全选已解锁技能（V2 技能 ≤6）
│   ├── OnHide(): 清理
├── 交互：
│   ├── 技能卡片点击 → Toggle 装备/取消（最多 6 个）
│   ├── 被动卡片点击 → Toggle 装备/取消（最多 3 个）
│   ├── 技能卡片长按 0.5s → Tooltip
│   ├── 已满 6/3 时点击未选卡 → 卡片摇晃 + 弱提示
│   ├── "出击"按钮 → 写入选中的技能/被动到 BattleLevelData → 进入战斗
│   ├── 下滑/点蒙版 → 关闭 Bottom Sheet
├── 数据传递：
│   → BattleLevelData.EquippedSkills = SkillConfigSO[]
│   → BattleLevelData.EquippedPassives = PassiveAbilitySO[]
│   → AppFlowNavigator.PushAsync(battleNode, data)
```

**视觉规格**（GDD §出战准备界面设计 v1.7 + PK-R3 UID-001 安全区适配）：
- 半屏 Bottom Sheet，从选关界面底部滑出
- **面板高度**：屏幕高度 × 65%（≈867pt @1334pt），底部 padding ≥ safeAreaInsets.bottom（iPhone X+ = 34pt）
- **布局**（从上到下）：
  ```
  ┌─ 拖拽条（12pt 高）──────────────────────┐
  ├─ 关卡标题 + 敌机预告（80pt 高）：────────┤
  │   左侧：关卡名 + 难度星                  │
  │   右侧：敌机类型图标 × N + 数量标签       │
  ├─ 主动技能区（GList，高度 ≈280pt）：──────┤
  │   200×120pt 卡片 × 3 列，V2 最多 2 行    │
  │   V3 扩展后可滚动                        │
  ├─ 被动技能区（GList，高度 ≈130pt）：──────┤
  │   160×90pt 卡片 × 4 列，V2 1 行          │
  ├─ 出击按钮（64pt 高 + 底部 safe padding）─┤
  └──────────────────────────────────────────┘
  ```
- 选中状态：品牌色边框 #4FC3F7 + 右上角 ✓
- **安全区适配**：`FairyGUI GRoot.inst.SetContentScaleFactor` 已处理宽度适配；底部 safe area 通过 `Screen.safeArea` 动态计算底部 padding

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| C1 | Bottom Sheet 滑出 | 选关界面点击关卡 | 底部滑出半屏面板 | 动画流畅 |
| C2 | 技能展示 | 新存档 | 散射已解锁(亮)，其余锁定(灰/???) | 状态正确 |
| C3 | 选中交互 | 点击已解锁卡片 | 边框高亮 + ✓ 标记 | Toggle 正常 |
| C4 | 装备上限 | 选满 6 技能后点第 7 个 | 卡片摇晃 + 弱提示 | 不超限 |
| C5 | 长按 Tooltip | 长按卡片 0.5s | 弹出 CD+描述 | 内容正确 |
| C6 | 出击数据传递 | 点击出击 | 进入战斗，选中的技能/被动生效 | 无丢失 |
| C7 | 关闭交互 | 下滑或点蒙版 | Bottom Sheet 关闭 | 回到选关 |
| C8 | 敌机预告 | 查看面板 | 显示关卡敌机类型+数量 | 与波次配置一致 |

---

### S2.4 PickupConfigSO / DropTableSO 实现（2h）

#### 实施方案

**新增 `PickupConfigSO`**：

```
[CreateAssetMenu] PickupConfigSO : ScriptableObject
├── string DisplayName
├── PickupType Type  // enum { Buff, Repair, Ammo, Coin }
├── int DropWeight
├── [Type=Buff时] BuffConfigSO BuffConfig
├── [Type=Repair时] int RepairAmount
├── [Type=Ammo时] BuffConfigSO AmmoBuffConfig
├── [Type=Coin时] int CoinAmount
├── GameObject ViewPrefab
├── PoolDefinition PickupVfx
├── AudioClipSO PickupSfx
├── OnValidate(): 按 Type 校验必填字段
```

**新增 `DropTableSO`**：

```
[CreateAssetMenu] DropTableSO : ScriptableObject
├── DropEntry[] Entries
│   ├── PickupConfigSO Pickup
│   └── int Weight
├── float BaseDropRate  // 0~1
├── bool GuaranteeDrop  // V2 不用
├── OnValidate(): Entries 非空 / Weight > 0 / BaseDropRate ∈ (0,1]
├── PickupConfigSO Roll():  // 加权随机抽取
│   → int totalWeight = sum(Entries.Weight)
│   → int roll = Random.Range(0, totalWeight)
│   → 遍历 Entries 累加找到命中区间
│   → 返回 PickupConfigSO
```

**创建 SO 资产**：

| 资产 | 类型 | PickupType | 关键配置 |
|------|------|-----------|---------|
| SG_Pickup_Buff_SpeedUp | PickupConfigSO | Buff | BuffConfig=SG_Buff_SpeedUp |
| SG_Pickup_Buff_Zephyr | PickupConfigSO | Buff | BuffConfig=SG_Buff_SpeedUp（疾风 BuffId=1001，效果=AttackInterval×0.5 攻速加倍）（PK-R4 DE-006: AttackUp→Zephyr 避免命名误导） |
| SG_Pickup_Repair | PickupConfigSO | Repair | RepairAmount=10 |
| SG_Pickup_Ammo | PickupConfigSO | Ammo | AmmoBuffConfig=SG_Buff_Berserk |
| SG_Pickup_Coin | PickupConfigSO | Coin | CoinAmount=10 |
| SG_DropTable_Normal | DropTableSO | — | BaseDropRate=0.3, 5 Entries |
| SG_DropTable_Elite | DropTableSO | — | BaseDropRate=0.5, 5 Entries |

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| D1 | SO 创建 | Project 窗口 | 所有 SO 存在，Inspector 无警告 | 无 Missing Ref |
| D2 | OnValidate | Buff 类型留空 BuffConfig | Inspector 红色错误提示 | 即时反馈 |
| D3 | Roll 概率 | 编辑器运行 10000 次 Roll | 概率分布与权重比例一致(±5%) | 加权随机正确 |

---

### S2.5 道具 Entity + 拾取逻辑（3h）

#### 实施方案

**道具 Entity 配置**（轻量 Entity）：
- Components: `[Movement]`（MoveSpeed=0.8 向下）+ `[State]`
- 无 Health、无 Collision
- 每个道具 Entity 持有 `PickupConfigSO` 引用

**新增 `PickupSystem`**：

```
PickupSystem（纯 C# 服务，struct 数组零 GC）
├── PickupInstance[MAX_PICKUPS] _pickups  // 固定数组
├── int _activeCount                      // 活跃道具数
├── Entity _playerEntity                  // 玩家飞机引用
├── float _basePickupRadius               // 基础拾取半径
├── const ATTRACT_BASE_SPEED = 3f         // 磁吸初始速度
├── const ATTRACT_ACCEL = 12f             // 磁吸加速度
├── const ATTRACT_MAX_SPEED = 20f         // 磁吸最大速度
├── const COLLECT_DIST_SQR = 0.04f        // 拾取判定距离²
├── Tick(float dt):
│   → effectiveRadius = _basePickupRadius × BuffComponent.PickupRadiusModifier
│   → hasMagnet = (radiusMod > 1.01f)
│   → for each active pickup (reverse iterate):
│       ┌─ 磁吸飞行状态（IsAttracting=true）：
│       │   → distSqr < COLLECT_DIST_SQR → CollectPickup + Remove
│       │   → 加速飞向玩家：AttractSpeed += ACCEL×dt（cap MAX）
│       │   → 归一化方向 × 步长更新 Position
│       └─ 正常漂浮状态：
│           → Position.y -= FloatSpeed × dt（向下漂浮）
│           → RemainingTime 倒计时 → 超时/越底线 → Remove
│           → distSqr < baseRadiusSqr → CollectPickup（普通拾取）
│           → hasMagnet && distSqr < effectiveRadiusSqr → 进入磁吸状态
├── CollectPickup(ref PickupInstance):
│   → switch Type: Buff/Repair/Ammo/Coin → 对应效果
│   → 播放 VFX + SFX + 通知条
├── SpawnPickup(Vector2 position, PickupConfigSO config):
│   → 填充 _pickups[_activeCount++]
```

**新增 `ItemDropSystem`**：

```
ItemDropSystem（敌机死亡时触发掉落）
├── DropTableSO _normalDropTable
├── DropTableSO _eliteDropTable
├── int _wavesSinceLastRepair = 0    // 保底计数器
├── OnEnemyKilled(Entity enemy, Vector2 position):
│   → 选择 DropTable（普通/精英）
│   → float roll = Random.value
│   → if (roll < dropTable.BaseDropRate)
│       var pickupConfig = dropTable.Roll()
│       PickupSystem.SpawnPickup(position, pickupConfig)
│   → 保底检查：连续 5 波不出修复→强制出一个
```

**道具空间参数**（GDD §7.3 + PK-R3 UID-008 闪烁规格）：
- 漂浮速度：0.8 单位/s
- 存在时限：8s（最后 2s 闪烁）
- **闪烁规格**：alpha 在 [0.3, 1.0] sin 波动；前 1s = 2Hz，后 1s = 4Hz（加速暗示即将消失）
- **消失动效**：缩小 scale 1.0→0 + 淡出 alpha→0，持续 0.15s
- 到达底线：直接消失（无缩小），不扣基地血

**磁吸飞行行为**（v1.6 实装，2026-05-29）：
- 触发条件：`BuffComponent.PickupRadiusModifier > 1.01f`（被动磁吸 Buff 激活时）
- 进入磁吸：道具进入放大后的拾取半径 → `IsAttracting = true`
- 飞行物理：初速 3 → 加速 12/s → 上限 20（加速度 EaseIn 效果，手感好）
- 拾取判定：`distSqr < 0.04`（0.2 单位）→ 立即拾取
- 设计意图：磁吸被动让道具"飞来"而非"扩大判定圈"，视觉反馈更强烈

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| E1 | 道具掉落 | Play Mode 击杀敌机 | 概率掉落道具 | ~30% 掉率 |
| E2 | 自动拾取 | 飞机靠近道具 | 距离<1.0 自动拾取 | 无需点击 |
| E3 | Buff 道具 | 拾取 Buff 道具 | 飞机获得对应 Buff | 属性修正生效 |
| E4 | 修复道具 | 拾取修复道具 | 基地 HP +10 | 不超最大值 |
| E5 | 道具超时 | 8s 不拾取 | 最后 2s 闪烁后消失 | 视觉正确 |
| E6 | 底线消失 | 道具到底线 | 直接消失，不扣血 | 与 GDD 一致 |
| E7 | 保底机制 | 连续 5 波不出修复 | 第 6 波强制出修复 | 保底触发 |
| E8 | 同屏道具 | 多道具同时存在 | 各自独立，同时拾取 Buff 走刷新/叠加 | 无冲突 |

---

### S2.6 SkillComponent 实现 + 6 技能全自动释放（额外任务，含在 S2.3 出击数据传递中）

#### 实施方案

**新增 `SkillComponent`**（GDD §3.1 框架集成指导 v1.8）：

```
SkillComponent : IEntityComponent, ITickable
├── TickOrder = 200（在 EnemyShoot=150 之后）
├── SkillSlot[6] _slots
├── struct SkillSlot:
│   ├── SkillConfigSO Config    // null = 空槽
│   ├── float CooldownTimer
│   ├── float CastTimer         // 前摇/后摇
│   ├── SkillState State        // Idle / Casting / Recovery / Cooldown
│   └── bool IsEmpty => Config == null
├── Init(Entity entity, SkillConfigSO[] equipped):
│   → for (int i = 0; i < 6; i++)
│       if (i < equipped.Length) _slots[i].Config = equipped[i]
│       else _slots[i].Config = null
│       _slots[i].State = Cooldown; _slots[i].CooldownTimer = 0.5f  // 初始短CD避免开场齐射
├── Tick(float dt):
│   → for (int i = 0; i < 6; i++)
│       if (_slots[i].IsEmpty) continue
│       ref var slot = ref _slots[i]
│       switch (slot.State):
│           Cooldown:
│               slot.CooldownTimer -= dt
│               if (timer <= 0) → State = Idle
│           Idle:
│               → 自动触发 → State = Casting; CastTimer = Config.CastTime
│           Casting:
│               slot.CastTimer -= dt
│               if (timer <= 0) → ExecuteEffects(slot) → State = Recovery; CastTimer = Config.RecoveryTime
│           Recovery:
│               slot.CastTimer -= dt
│               if (timer <= 0) → State = Cooldown; CooldownTimer = Config.CooldownTime
├── ExecuteEffects(slot):
│   → foreach (ISkillEffect effect in slot.Config.Effects)
│       effect.Execute(entity, slot.Config)
```

**ISkillEffect 实现类**（V2 三种）：

| 类 | 技能 | 职责 |
|----|------|------|
| `FireBulletsEffect` | SK-P01/P02/P03 | 发射弹幕 |
| `ApplyBuffToSelfEffect` | SK-P04/P05 | 自身施 Buff |
| `DealAreaDamageEffect` | SK-P06 | AOE + 击退 |

#### 验收方案

| # | 验收项 | 操作 | 预期 | PASS |
|---|--------|------|------|------|
| F1 | 散射技能 | 装备散射出击 | CD=0.3s 自动释放 5 发扇形弹 | 角度~30° |
| F2 | 追踪技能 | 装备追踪出击 | CD=2.0s，前摇 0.3s 后发射 2 发追踪弹 | 追踪行为正确 |
| F3 | 激光技能 | 装备激光出击 | CD=3.0s，持续 DPS | 穿透不消失 |
| F4 | 火力全开 | 装备火力全开出击 | CD=8.0s，自身攻速×2+子弹×2 持续 3s | Buff 正确施加 |
| F5 | 护盾 | 装备护盾出击 | CD=10.0s，免疫 1 次碰撞伤害 | Buff 生效 |
| F6 | 冲击波 | 装备冲击波出击 | CD=5.0s，AOE 伤害+击退 | 范围和伤害正确 |
| F7 | 6 技能同时 | 全部装备出击 | 6 技能各自独立 CD 全自动释放 | 互不干扰 |
| F8 | 空槽位 | 只装备 2 个技能 | 其余 4 个槽位空，不报错 | IsEmpty 跳过 |

---

## 2. Sprint 2 验收总表

### 功能验收

| # | 场景 | 预期 | 状态 |
|---|------|------|------|
| G1 | 新存档初始解锁 | 散射+穿透已解锁 | ⬜ |
| G2 | 通关解锁 | 通关对应关卡后新技能解锁 | ⬜ |
| G3 | Bottom Sheet | 选关后弹出，展示正确 | ⬜ |
| G4 | 技能装备 | 选中/取消正常，上限检查 | ⬜ |
| G5 | 出击→战斗 | 技能/被动数据传入战斗场景 | ⬜ |
| G6 | 6 技能全自动 | 所有已装备技能 CD 好了自动释放 | ⬜ |
| G7 | 道具掉落 | 击杀敌机概率掉落 | ⬜ |
| G8 | 道具拾取 | 自动拾取，效果正确 | ⬜ |
| G9 | 道具生命周期 | 8s 超时消失，底线消失 | ⬜ |
| G10 | 保底机制 | 连续 5 波无修复→保底出 | ⬜ |
| G11 | 存档兼容 | V1 老存档正常加载 | ⬜ |

### 性能验收

| # | 指标 | 目标 | 工具 |
|---|------|------|------|
| P1 | SkillComponent.Tick | < 0.15ms | Profiler |
| P2 | PickupSystem.Tick | < 0.05ms（≤5 道具） | Profiler |
| P3 | 热路径零 GC | 0 bytes/frame | Deep Profile |

---

_创建于 2026-05-18 | Sprint 2 TDD v1.4_

**变更历史**：
- v1.0（2026-05-18）：初始版本
- v1.1（2026-05-18）：PK-R1 Unity 架构师回写
- v1.2（2026-05-18）：PK-R2 Unity 编辑器工具开发者回写（ET-003 解锁表 OnValidate）
- v1.3（2026-05-18）：PK-R3 UI 设计师回写（UID-001 安全区适配+布局，UID-008 道具闪烁规格）
- v1.4（2026-05-19）：PK-R4 技术文档工程师回写（DE-006 Buff 道具命名 AttackUp→Zephyr）
- v1.5（2026-05-22）：S2.3 延后说明
- v1.6（2026-05-30）：PickupSystem 实装磁吸飞行行为（IsAttracting 两阶段状态机 + 加速飞向玩家）
