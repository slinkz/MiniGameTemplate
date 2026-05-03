# PK 评审记录 — SG_GAME_DESIGN.md

> **目标文档**：`Docs/Agent/SG_GAME_DESIGN.md`
> **文档类型**：游戏设计文档（可执行性评审）
> **攻方角色**：Unity 架构师（专精数据驱动架构、微信小游戏平台限制、Entity-Component 框架、性能预算）
> **守方角色**：游戏设计师（专精玩法节奏、玩家体验、关卡编排、休闲游戏设计）
> **开始时间**：2026-05-03 00:38
> **最大轮次**：6
> **PK 状态**：✅ 已完成

---

## PK Round 1 — 攻方提问（Unity 架构师）

### SG-001 | 严重度 🔴高 | 碰撞管线未定义：敌机 vs 飞机/基地走哪条路径
**涉及章节**：§3.1 碰撞结果表
**质疑**：文档定义三种碰撞对，但未指明技术实现路径：
1. "我方子弹→敌机"可走 DanmakuSystem TargetRegistry（已落地）
2. "敌机→基地"和"敌机→我方飞机"是 Entity vs Entity 碰撞——走 EntityCollisionSolver（圆 vs 圆）还是将基地注册为 Obstacle（圆 vs AABB）？
3. 基地碰撞体形状？（32px 高全屏宽横条 → 应是 AABB 而非 CircleHitbox）
4. ContactDamage / ContactDamageInterval 数值未给出
5. 敌机撞后"爆炸消失"——立即回收还是走 DeathDelay？
**潜在风险**：V1 核心玩法必经路径，碰撞管线不明确则程序员不知复用哪套系统
**建议方向**：明确基地=AABB（ObstaclePool）、飞机=圆形（EntityCollisionSolver）、ContactDamage 基准值
**状态**：🟡 待回应

---

### SG-002 | 严重度 🔴高 | 核心数值全部缺失（基地HP/子弹伤害/敌机HP/敌机速度/攻击间隔）
**涉及章节**：§3.1、§3.2、§4.2
**质疑**：程序员创建 EntityConfigSO 和 BulletPatternSO 时需要填初始值，文档只有"100%"的相对表述。缺失：基地 MaxHp、子弹 Damage、发射间隔、敌机 HP、敌机 MoveSpeed、ContactDamage、"90%基地血量"的绝对值。
**潜在风险**：阻塞原型搭建。随意填值导致一枪秒杀或打不死。
**建议方向**：提供"教学关基准数值表"（如基地HP=100, 子弹Damage=10, 间隔=0.3s, 普通敌机HP=20/速度=2/ContactDmg=15）
**状态**：🟡 待回应

---

### SG-003 | 严重度 🔴高 | 虚拟摇杆输入系统实现方案未定义
**涉及章节**：§2.2 操控手感
**质疑**：框架无内置虚拟摇杆。关键缺失：
1. 输入源：Unity Input.GetTouch 还是 wx.onTouchStart？
2. 摇杆模式：方向型（固定速度）还是模拟型（距离映射速度）？
3. 死区半径？
4. "松手即停"是否需要加减速为0？
5. 飞机坐标系？
**潜在风险**：输入是核心交互层，实现方案不清导致返工
**建议方向**：确认 Unity Touch API + 方向型（固定 MoveSpeed）+ 零加减速
**状态**：🟡 待回应

---

### SG-004 | 严重度 🟡中 | 时间线编辑器 vs 现有 EntitySpawnWaveSO 的关系未明确
**涉及章节**：§5.3 ~ §5.7
**质疑**：框架已有 EntitySpawnWaveSO + Editor，但文档描述了一个新的时间线编辑器（绝对时间 startTime），两者关系不清。§5.7 留了两条技术路径。V1 只有 5 关，现有工具可能已够用。
**潜在风险**：编辑器是独立较大开发任务，V1 是否需要投入？
**建议方向**：V1 使用现有 EntitySpawnWaveSO + Timer/AllCleared；时间线编辑器降为 V2
**状态**：🟡 待回应

---

### SG-005 | 严重度 🟡中 | 坐标系与屏幕映射未定义
**涉及章节**：全文
**质疑**：文档混用"spawnXRange[-0.6,0.6]"（归一化？）、"64×64px"、"全屏宽×32px"。未定义：正交相机 Size、PPU、设计分辨率、宽高比适配策略。
**潜在风险**：坐标系是所有位置/速度/碰撞半径的基础，不确定则所有数值无法正确设置
**建议方向**：定义设计分辨率 1080×1920、PPU=100、正交相机 Size=9.6、适配策略
**状态**：🟡 待回应

---

### SG-006 | 严重度 🟡中 | 我方子弹弹幕配置参数缺失
**涉及章节**：§3.2
**质疑**：文档只说"单发直射"，但创建 BulletPatternSO/BulletTypeSO 需要：子弹速度、大小、外观、Lifetime、发射偏移。且框架 AttackComponent 用 BulletTypeSO（WF-003），不是 BulletPatternSO——需明确走哪个 API。
**潜在风险**：子弹是高频对象，配置不当影响性能和手感
**建议方向**：明确 V1 走 AttackComponent + BulletTypeSO，给出 Speed=10、靠屏幕边缘回收
**状态**：🟡 待回应

---

### SG-007 | 严重度 🟡中 | 0.5秒无敌帧实现路径与框架双重无敌机制的关系
**涉及章节**：§3.1
**质疑**：框架有两套无敌帧：DanmakuSystem._invincibleTimer（弹丸→玩家）和 HealthComponent.IFrameCount（Entity 受击）。"敌机撞飞机→0.5秒无敌"是 Entity vs Entity 碰撞触发的，该用哪套？IFrameCount 是帧数非秒数。
**潜在风险**：两套并存可能逻辑冲突
**建议方向**：明确走 HealthComponent 层面，0.5s 换算为帧数（30fps=15帧），统一管控
**状态**：🟡 待回应

---

### SG-008 | 严重度 🟡中 | 战斗场景 Scene 架构未定义
**涉及章节**：§2.1~§2.4、§4.1
**质疑**：5 关是 5 个 Scene 还是 1 个 Scene + 5 个 WaveConfig SO？选关界面是独立 Scene 还是 UI overlay？关卡数据如何传递给战斗场景？
**潜在风险**：Scene 架构决策影响所有初始化/清理流程
**建议方向**：推荐"单战斗 Scene + 多 WaveConfig SO"+ 选关 UI overlay
**状态**：🟡 待回应

---

### SG-009 | 严重度 🟡中 | 性能预算未评估（同屏敌机数上限 vs TargetRegistry 64 槽）
**涉及章节**：§4.2
**质疑**：第 5 关"最高密度"同屏多少架敌机？TargetRegistry 硬上限 64。EntityViewBridge 256 上限。
**潜在风险**：V1 数量级小（预估 20-30 同屏峰值），但文档应明确上限供编排参考
**建议方向**：§4.2 补充"V1 硬限制：同屏敌机不超过 50 架"
**状态**：🟡 待回应

---

### SG-010 | 严重度 🟡中 | 存储方案与框架 ISaveSystem 的映射
**涉及章节**：§9
**质疑**：框架有 ISaveSystem（PlayerPrefs → 微信自动映射 wx.setStorageSync），但文档直接说"wx.setStorageSync"。存储 key 命名、数据结构未定义。
**潜在风险**：低风险，但明确结构避免后续迁移问题
**建议方向**：确认用 ISaveSystem，key="sg_progress"，value=JSON{clearedLevels:[],version:1}
**状态**：🟡 待回应

---

### SG-011 | 严重度 🟡中 | 战斗状态机（Intro→Playing→Victory/Defeat）未定义
**涉及章节**：§2.1
**质疑**：进场动画期间玩家能否操作？敌机是否计时？用什么控制状态？
**潜在风险**：不用 FSM 管理可能出现进场期间触发碰撞的 bug
**建议方向**：定义 `Intro(1.5s)→Playing→Victory/Defeat`，Intro 阶段不 Tick Spawner、不检测碰撞
**状态**：🟡 待回应

---

### SG-012 | 严重度 🟢低 | "敌机闪白+击退"与"匀速下落"的关系
**涉及章节**：§2.2 vs §3.3
**质疑**：击退是额外位移（框架已支持叠加），但文档未确认意图。KnockbackDistance=0.5 是否合适？
**潜在风险**：低风险，框架已支持
**建议方向**：§3.3 补充"击退不影响匀速下落"
**状态**：🟡 待回应

---

### SG-013 | 严重度 🟢低 | 屏幕震动参数缺失
**涉及章节**：§2.2
**质疑**：震动幅度/频率/衰减未给出
**潜在风险**：低风险，可编码迭代
**建议方向**：创建 ScreenShakeConfig SO，参数编码期间调试
**状态**：🟡 待回应

---

### SG-014 | 严重度 🟢低 | BGM"紧张层叠加"实现方案
**涉及章节**：§2.2
**质疑**：多轨叠加还是简单切换？框架 AudioSystem 是否支持多 BGM 同时播放？
**潜在风险**：低风险
**建议方向**：V1 简化为"血量<30%切换紧张版 BGM"
**状态**：🟡 待回应

---

### SG-015 | 严重度 🟢低 | 通关判定逻辑与现有 API 的衔接
**涉及章节**：§2.3、§4.3
**质疑**：通关="最后一架敌机被消灭"，需确认 API：IsAllWavesCleared + CountAliveBycamp(Enemy)==0？
**潜在风险**：低风险，实现直观
**建议方向**：确认或新增 CountAliveBycamp(EnumCamp) API
**状态**：🟡 待回应

---

## Round 1 统计

| 严重度 | 数量 |
|--------|------|
| 🔴 高 | 3 |
| 🟡 中 | 8 |
| 🟢 低 | 4 |
| **合计** | **15** |

---

## PK Round 2 — 守方回应（游戏设计师）

### SG-001 回应 | ⚠️ 部分接受

**回应**：

攻方提出的 5 个子问题逐个回应：

1. **"我方子弹→敌机"走 DanmakuSystem TargetRegistry** — 正确，无异议。
2. **"敌机→基地/飞机"走哪条路径** — 技术实现选型不属于设计文档职责范围。设计文档定义"碰撞对之间发生什么"已做到。但我补充设计意图和约束帮程序做选型。
3. **基地碰撞体形状** — 接受。基地是全屏宽横条，设计意图为 AABB 矩形区域。
4. **ContactDamage 数值** — 合并到 SG-002。
5. **爆炸消失——立即回收还是 DeathDelay？** — 设计意图：撞击后播放爆炸特效，特效播完回收。即走框架 `EntityConfigSO.DeathDelay` 流程。

**补充方案**（写入 §3.1）：

> **碰撞实现约束**（供程序参考）：
> - 基地碰撞体为 **AABB 矩形**（全屏宽 × 32px 高）
> - 我方飞机碰撞体为 **圆形**
> - 敌机碰撞后走 DeathDelay 流程（`EntityConfigSO.DeathDelay`），特效播完再回收
> - 具体碰撞管线技术选型由技术文档决定，设计文档只约束形状和行为结果

**状态**：✅ 收敛

---

### SG-002 回应 | ✅ 接受

**回应**：

攻方说得对。没有基准值，原型搭建无法开始。我提供 **V1 基准数值表**：

**补充方案**（新增 §3.4 "V1 基准数值表"）：

> | 参数 | 值 | 说明 |
> |------|-----|------|
> | 基地 MaxHp | 100 | 统一基准 |
> | 子弹 Damage | 10 | 单发伤害 |
> | 子弹发射间隔 | 0.3s | AttackInterval |
> | 子弹速度 | 12 单位/秒 | 偏快，确保打击密度感 |
> | 普通敌机 MaxHp | 20 | 2 发击杀 → 即时满足感 |
> | 普通敌机 MoveSpeed | 2 单位/秒 | 慢速，给玩家反应时间 |
> | 快速敌机 MoveSpeed | 4 单位/秒 | 第 3 关起出现 |
> | 敌机 ContactDamage | 15 | 约 6~7 次撞击 Game Over |
> | 无敌帧时长 | 0.5s（15 帧@30fps） | 飞机被撞后免疫 |
> | 基地第 3 关初始 HP | 90 | MaxHp×90% |
> | 基地第 4/5 关初始 HP | 80 | MaxHp×80% |
>
> **数值逻辑**：普通敌机 2 发击杀 → 玩家有效率感；容错率约 50%，对新手友好。

**状态**：✅ 收敛

---

### SG-003 回应 | ⚠️ 部分接受

**回应**：

1. **输入源** — 使用 Unity Touch API（平台适配由框架层处理）
2. **摇杆模式** — 确认：**方向型（固定速度）**。原因：休闲游戏不需要精确变速
3. **死区半径** — **10px 物理像素**
4. **加减速** — **零加减速**，松手瞬间速度归零
5. **飞机坐标系** — 世界坐标系，移动范围 clamp 在相机可视区域矩形内（四边各留 0.5 个碰撞半径）

**补充方案**（更新 §2.2 + §十）：

> **操控手感补充**：
> - 摇杆模式：**方向型**（Direction-only），速度恒定 = `EntityConfigSO.MoveSpeed`
> - 死区半径：**10px 物理像素**
> - 加减速：**零**（无惯性）
> - 移动约束：飞机位置 clamp 在相机可视矩形内
> - 输入 API：Unity Touch API（平台适配由框架层处理）

**状态**：✅ 收敛

---

### SG-004 回应 | ❌ 反驳（但结论一致）

**回应**：

攻方建议"V1 用现有 EntitySpawnWaveSO，时间线编辑器降 V2"——我**完全同意结论**，但这不是"质疑"。

§5 章节的定位是**描述策划理想工作流愿景**，§5.7 已明确留了两条路径。设计文档描述编辑器愿景是为了让程序理解策划最终要什么，以便 V1 数据结构设计预留兼容性。

**补充方案**（§5.3 开头加说明）：

> ⚠️ **V1 实现方案**：V1 使用框架已有的 `EntitySpawnWaveSO` + Inspector 编辑，不开发独立时间线编辑器。以下为 **V2 愿景**。

**状态**：✅ 收敛

---

### SG-005 回应 | ⚠️ 部分接受

**回应**：

攻方说得有道理——坐标系是所有数值基础。但要区分：

- "spawnXRange[-0.6, 0.6]" 是 §5.6 中的伪数据示例（说明设计意图），不是坐标系定义
- "64×64px" 是美术资源规格，与游戏内坐标系无关
- PPU / 正交相机 Size 是**技术文档范畴**

**我接受的部分**：设计文档应给出可操作的空间约束。

**补充方案**（新增 §3.5 "空间约束"）：

> **§3.5 空间约束（设计意图层）**
> - 设计分辨率：**1080×1920**（竖版 9:16）
> - 飞机活动区域：屏幕可视区全范围
> - 敌机出生线：屏幕顶部外 1 个身位
> - 基地区域：屏幕底部 32px 高横条
> - 敌机出生 X 范围：屏幕宽度的中间 60%~80%
> - 具体 PPU / CameraSize 由技术文档定义

**状态**：✅ 收敛

---

### SG-006 回应 | ✅ 接受

**回应**：

接受。V1 走 `AttackComponent` + `BulletTypeSO`（框架 WF-003 确认）。补充子弹参数：

**补充方案**（§3.2 补充）：

> **V1 子弹参数**：
> - 走 `AttackComponent` + `BulletTypeSO`（框架 WF-003）
> - Speed = 12 单位/秒
> - Size = 0.16 世界单位（对应 16px@100PPU）
> - Lifetime = 2s（超出屏幕前已足够到达顶部）
> - 发射偏移 = (0, 0.4)（飞机头部位置）
> - 碰撞后回收（击中敌机 → Die 响应）

**状态**：✅ 收敛

---

### SG-007 回应 | ✅ 接受

**回应**：

接受攻方建议。明确统一走 `HealthComponent` 层面：

**补充方案**（§3.1 补充）：

> **无敌帧实现**：统一走 `HealthComponent.IFrameCount`。0.5s = 15帧@30fps。
> DanmakuSystem._invincibleTimer 仅用于弹丸→玩家的弹幕碰撞（不影响 Entity vs Entity）。

**状态**：✅ 收敛

---

### SG-008 回应 | ✅ 接受

**回应**：

接受。推荐方案清晰实用：

**补充方案**（新增 §4.4 "场景架构"）：

> **§4.4 场景架构**
> - **单战斗 Scene** + **多 WaveConfig SO**（5 关 = 5 个 EntitySpawnWaveSO）
> - 选关界面 = FairyGUI UI overlay（不切 Scene）
> - 关卡数据传递：选关 → 设置 `CurrentLevelIndex` FloatVariable SO → 战斗场景 Bootstrap 读取对应 WaveConfig
> - 进入战斗 = 加载 Battle Scene；退出 = 回到 MainMenu Scene（Boot.unity 走 SceneLoader）

**状态**：✅ 收敛

---

### SG-009 回应 | ✅ 接受

**回应**：

接受。V1 设计上不会超过 30 同屏，但明确写出约束避免编排时踩坑。

**补充方案**（§4.2 补充）：

> **V1 性能约束**：同屏敌机不超过 **40 架**（TargetRegistry 64 槽，留余量给玩家子弹碰撞目标）。第 5 关峰值设计上限 30~35 架。

**状态**：✅ 收敛

---

### SG-010 回应 | ✅ 接受

**回应**：

接受。用框架 ISaveSystem 而非直接调用 wx 接口。

**补充方案**（§9.3 更新）：

> **存储方案**：
> - 统一走框架 `ISaveSystem`（PlayerPrefs → 微信自动映射 wx.setStorageSync）
> - Key = `"sg_progress"`
> - Value = JSON: `{"clearedLevels":[1,2],"version":1}`
> - 版本字段 `version` 预留后续数据迁移

**状态**：✅ 收敛

---

### SG-011 回应 | ✅ 接受

**回应**：

接受。战斗状态机是必要的，防止进场期间触发碰撞。

**补充方案**（新增 §2.1 战斗状态机）：

> **战斗状态机**：`Intro(1.5s) → Playing → Victory / Defeat`
> - **Intro**：飞机进场动画，不 Tick Spawner、不检测碰撞、不响应输入
> - **Playing**：正常战斗
> - **Victory**：0.5s 静默 → 胜利界面
> - **Defeat**：基地爆炸 → 失败界面
> - 实现方式：`BattleState` 枚举 + EntitySystemBootstrap 层控制

**状态**：✅ 收敛

---

### SG-012 回应 | ✅ 接受

**回应**：低风险，接受补充。

**补充方案**：§3.3 补充"击退是额外位移，不影响匀速下落方向的运动"

**状态**：✅ 收敛

---

### SG-013 回应 | ✅ 接受

**回应**：低风险，接受。屏幕震动参数编码期间调试即可。

**补充方案**：创建 `ScreenShakeConfigSO`，参数在 Inspector 中调试。

**状态**：✅ 收敛

---

### SG-014 回应 | ✅ 接受

**回应**：接受简化方案。

**补充方案**：V1 实现为"基地 HP < 30% → 切换紧张版 BGM"，不做多轨叠加。

**状态**：✅ 收敛

---

### SG-015 回应 | ✅ 接受

**回应**：接受。框架已有 `EntitySpawner.IsAllWavesCleared` + 可用 `EntityManager.CountAliveByConfig()` 确认全灭。

**补充方案**：通关判定 = `IsAllWavesCleared && CountAliveBycamp(Enemy) == 0`

**状态**：✅ 收敛

---

## Round 2 统计

| 回应类型 | 数量 |
|----------|------|
| ✅ 接受 | 10 |
| ⚠️ 部分接受 | 4 |
| ❌ 反驳 | 1 |
| **收敛** | **15/15** |

---

## PK Round 3 — 攻方深挖（Unity 架构师）

### SG-001 | ✅ 确认收敛
守方明确了基地=AABB、飞机=圆形、DeathDelay 流程，且合理地将碰撞管线技术选型边界划归技术文档。补充的碰撞实现约束信息对程序员已足够开始选型。

---

### SG-002 | 🔄 追问
**追问**：基准数值表已给出且逻辑自洽，整体接受。但有一个遗漏：**基地没有独立的 EntityConfigSO 配置指引**。基地作为己方 Entity，程序员需要知道：
1. 基地是否作为 Entity 实例存在（走 EntityManager.Spawn）？还是纯 UI 血条 + 逻辑变量？
2. 如果是 Entity，它的 `Components[]` 列表是什么？（只需 Health？不需 Movement/Collision？）
3. 基地被敌机撞击——是敌机主动对基地 Entity 造成 ContactDamage，还是敌机到达底部屏幕线时由 Spawner 逻辑直接扣血？

这三个问题决定了基地的技术形态，直接影响碰撞管线的整合方式。

**为什么还没收敛**：数值表补全了"多少血"，但"基地是什么"这个实体建模问题在 SG-001 和 SG-002 中都未被明确回答。EntityCollisionSolver 目前只支持圆 vs 圆（代码已确认），而基地是 AABB——意味着敌机 vs 基地**不能直接走 EntityCollisionSolver**，需要明确替代路径。

---

### SG-003 | ✅ 确认收敛
五个子问题全部给出明确回答：Unity Touch API、方向型、10px 死区、零加减速、世界坐标 clamp。程序员可直接实现。

---

### SG-004 | ✅ 确认收敛
V1 明确使用 EntitySpawnWaveSO + Inspector，时间线编辑器标注为 V2 愿景。结论清晰，路径无歧义。

---

### SG-005 | 🔄 追问
**追问**：设计分辨率 1080x1920 已确认，空间约束也补充了。但守方将 PPU/CameraSize 完全推给技术文档，这里有一个**设计侧必须回答**的问题：

§3.4 基准数值表中"子弹速度 12 单位/秒"、"敌机 MoveSpeed 2 单位/秒"——这里的"单位"是什么？如果 PPU=100 且 CameraSize=9.6，则屏幕可视高度=19.2 世界单位，敌机从顶到底需要 ~9.6 秒。如果 CameraSize=4.8（另一种常见设置），敌机穿越时间减半为 ~4.8 秒。

**请确认**：这些数值是基于哪个 CameraSize 假设的？或者给出"敌机从屏幕顶部到底部的期望穿越时间"（秒），让程序员反推 CameraSize。

**为什么还没收敛**：速度数值和坐标系尺度是耦合的。不定义其中一个，另一个就是悬空的。设计文档至少应给出"穿越时间"这个体验层参数。

---

### SG-006 | ✅ 确认收敛
V1 走 AttackComponent + BulletTypeSO，参数完备。

⚠️ **技术事实修正**：守方回应写"走 BulletTypeSO（框架 WF-003）"，而实际代码中 `EntityConfigSO.AttackBulletPattern` 字段类型是 `BulletPatternSO`（非 BulletTypeSO）。此为框架侧文档/代码不一致，不阻塞设计文档收敛，但程序员需注意。

---

### SG-007 | ✅ 确认收敛
统一走 HealthComponent.IFrameCount，0.5s=15帧@30fps，两套机制的职责边界清晰。代码已确认 `HealthComponent` 有 `_iFrameMax` 和 `_iFrameRemaining` 字段。

---

### SG-008 | ✅ 确认收敛
单战斗 Scene + 多 WaveConfig SO + FairyGUI overlay 选关 + FloatVariable SO 传递关卡索引。架构清晰。

---

### SG-009 | ✅ 确认收敛
40 架硬限制、峰值 30~35 的设计上限合理，与 TargetRegistry 64 槽留有余量。

---

### SG-010 | ✅ 确认收敛
ISaveSystem + key/value JSON 结构 + version 字段，清晰可落地。

---

### SG-011 | ✅ 确认收敛
`Intro→Playing→Victory/Defeat` 四态清晰，Intro 阶段行为约束已定义。`BattleState` 枚举 + Bootstrap 层控制合理。

---

### SG-012 | ✅ 确认收敛
低风险，"击退不影响匀速下落"明确。框架 `MovementComponent.ApplyKnockback` 已验证为额外位移叠加。

---

### SG-013 | ✅ 确认收敛
低风险，ScreenShakeConfigSO 编码期间调试即可。

---

### SG-014 | ✅ 确认收敛
V1 简化为 HP < 30% 切换紧张版 BGM，实现路径清晰。

---

### SG-015 | 🔄 追问
**追问**：守方回应确认通关判定为 `IsAllWavesCleared && CountAliveBycamp(Enemy) == 0`。但经查验框架代码：
- `EntityManager.CountAliveByConfig(EntityConfigSO)` **存在**（按配置类型查询）
- `CountAliveByCamp(EnumCamp)` **不存在**

而 `EntitySpawner` 内部的 AllCleared 判定已经使用了 `CountAliveByConfig`——即检查当前波次所有 `SpawnGroup.EntityConfig` 对应的存活数为 0。

实际通关判定应为：`EntitySpawner.IsAllWavesCleared == true`（这个属性内部已经包含了"最后一波的所有敌机被消灭"的语义）。

**请确认**：通关判定是否直接用 `EntitySpawner.IsAllWavesCleared`（无需额外 API），还是确实需要新增 `CountAliveByCamp` 方法？

**为什么还没收敛**：守方引用了一个不存在的 API。程序员照着文档写代码会发现编译不过，需要明确实际路径。

---

### SG-NEW-001 | 🆕 新问题 | 严重度 🟡中 | 敌机"撞后爆炸消失"的碰撞响应行为未定义
**质疑**：设计文档 §3.1 说"敌机→基地：基地扣血，敌机爆炸消失"、"敌机→我方飞机：基地扣血，敌机爆炸消失"。但查看 `EntityCollisionSolver` 源码，碰撞响应只有两个行为：**推力分离** + **接触伤害**。没有"碰撞后销毁 Entity"的逻辑。

也就是说，按照现有框架行为：
- 敌机撞到飞机 → 双方被推开 + 互相造成接触伤害 → 敌机**不会消失**，会继续下落
- 如果要实现"撞后消失"，需要：要么在游戏层监听碰撞事件后手动 Despawn，要么将敌机 HP 设为等于 ContactDamage 使其被"撞死"

**请明确**：
1. 敌机撞飞机/基地后是**立即销毁**（类似子弹命中即消失），还是**受到足够伤害才死**？
2. 如果是立即销毁，需要游戏层在碰撞响应中新增"碰撞后销毁敌机"的逻辑——这不是框架自带行为。

---

### SG-NEW-002 | 🆕 新问题 | 严重度 🟡中 | 飞机 Entity 建模缺失——"不可被摧毁"的实现路径
**质疑**：§3.1 明确"我方飞机：不可被摧毁"，同时"敌机撞飞机→基地扣血"。这意味着：
1. 飞机被撞时，伤害不扣飞机自己的 HP，而是**转移到基地 HP**
2. 但框架的 `EntityCollisionSolver.ApplyContactDamage` 是对**碰撞双方各自的 HealthComponent** 造成伤害

程序员需要明确的伤害转移机制：
- 方案 A：飞机不挂 HealthComponent（免疫所有伤害），碰撞事件由游戏层监听后手动扣基地血
- 方案 B：飞机挂 HealthComponent 但使用 IDamageModifier 拦截所有伤害（return false 中断链），同时在 Modifier 中转发伤害给基地
- 方案 C：飞机和基地共享同一个 HealthComponent 实例（技术上可能但不优雅）

**请指定设计意图中的推荐方案**，以便程序员选择实现路径。

---

## Round 3 统计

| 类型 | 数量 | 问题编号 |
|------|------|----------|
| ✅ 确认收敛 | 12 | SG-001, 003, 004, 006, 007, 008, 009, 010, 011, 012, 013, 014 |
| 🔄 追问 | 3 | SG-002, SG-005, SG-015 |
| 🆕 新问题 | 2 | SG-NEW-001 (🟡中), SG-NEW-002 (🟡中) |

**收敛进展**：12/15 确认收敛（80%），3 追问 + 2 新问题待 Round 4 收敛。

---

## PK Round 4 — 守方回应（游戏设计师）

### SG-002 追问回应 | ✅ 接受

**回应**：

攻方说得对，"基地是什么"这个实体建模问题确实一直没被正面回答。现在明确回答三个子问题：

**1. 基地是 Entity 实例还是纯逻辑变量？**

基地是 **Entity 实例**，走 `EntityManager.Spawn()`。理由：
- 基地有 HP、需要受伤反馈（闪白、屏幕震动），这些都是 Entity 生态圈已有的能力
- 未来可能给基地加护盾 Buff（走 IDamageModifier），用 Entity 可零成本扩展
- 基地死亡触发 `OnDeath` 事件，直接驱动 BattleState → Defeat，事件链清晰

**2. 基地 Entity 的 Components[] 列表：**

```
Components = [ Health ]
```

- **Health** — 必需，HP=100，承载所有受伤/死亡逻辑
- **不挂 Movement** — 基地不移动
- **不挂 Collision** — 基地不参与 EntityCollisionSolver（圆 vs 圆不适用 AABB）
- **不挂 Attack** — 基地不攻击

**3. 敌机 vs 基地的碰撞路径：**

**设计决定：敌机 vs 基地不走 EntityCollisionSolver，改用"底线检测"方案。**

- 基地不参与 `EntityCollisionSolver`（不挂 Collision，CollisionRadius=0）
- Game 层（BattleController / Bootstrap）每帧检测所有 Enemy 阵营 Entity 的 `Position.y`
- 当 `Position.y <= BaseLineY` 时，判定该敌机"撞到基地"
- 触发效果：① 对基地 Entity 的 HealthComponent 调用 `TakeDamage(ContactDamage)`；② 对该敌机调用 Despawn（走 DeathDelay 播放爆炸特效后回收）
- `BaseLineY` = 基地区域上沿的 Y 坐标（由 CameraSize 决定具体值）

**设计意图**：底线检测方案更符合射击游戏经典做法——敌人穿过防线就扣基地血。比 AABB 碰撞更简洁、更高效。

**补充方案**（新增 §3.6 "基地 Entity 建模"）：

> **§3.6 基地 Entity 建模**
>
> | 属性 | 值 | 说明 |
> |------|-----|------|
> | 实体类型 | Entity（走 EntityManager.Spawn） | 非纯 UI 逻辑 |
> | Camp | Ally | 己方阵营 |
> | Components[] | `[ Health ]` | 仅血量，无移动/碰撞/攻击 |
> | MaxHp | 100 | V1 基准 |
> | EnableEntityCollision | false | 不参与 EntityCollisionSolver |
> | CollisionRadius | 0 | 无圆形碰撞体 |
>
> **敌机 vs 基地碰撞方案**：底线检测——Game 层每帧检查敌机 `Position.y <= BaseLineY`，命中则：
> 1. 对基地 HealthComponent 造成 `敌机.ContactDamage` 伤害
> 2. 敌机 Despawn（走 DeathDelay 播放爆炸特效后回收）

**状态**：✅ 收敛

---

### SG-005 追问回应 | ✅ 接受

**回应**：

攻方说得对。速度数值和坐标系尺度是耦合的。设计文档应给出体验层锚点参数，让程序员据此反推技术参数。

**体验层锚点参数（设计意图）**：

| 参数 | 值 | 设计意图 |
|------|-----|----------|
| 普通敌机从顶到底穿越时间 | **~8 秒** | 充分反应时间，休闲节奏 |
| 快速敌机穿越时间 | **~4 秒** | 压力感，但仍可拦截 |
| 子弹从底到顶穿越时间 | **~1.3 秒** | 快速到达，打击密度高 |
| 飞机从最左到最右穿越时间 | **~1.1 秒** | 操控灵敏但不失控 |

**推导**（CameraSize=8，可视区域=9×16 世界单位）：
- 普通敌机 MoveSpeed=2 → 16/2=8s ✓
- 快速敌机 MoveSpeed=4 → 16/4=4s ✓
- 子弹速度=12 → 16/12≈1.33s ✓
- 飞机 MoveSpeed=8（新增） → 可视宽度9/8≈1.12s ✓

**补充方案**（更新 §3.5 空间约束）：

> **§3.5 空间约束（更新）**
>
> | 参数 | 值 | 说明 |
> |------|-----|------|
> | 设计分辨率 | 1080×1920（9:16） | |
> | **推荐 CameraSize** | **8**（正交半高） | 可视区域 = 9×16 世界单位 |
> | 推荐 PPU | 100 | 1 世界单位 = 100px |
>
> **体验锚点（设计侧承诺）**：
> | 行为 | 期望时间 | 对应速度 |
> |------|---------|----------|
> | 普通敌机顶→底 | ~8s | 2 单位/秒 |
> | 快速敌机顶→底 | ~4s | 4 单位/秒 |
> | 子弹底→顶 | ~1.3s | 12 单位/秒 |
> | 飞机左→右 | ~1.1s | 8 单位/秒 |
>
> 如果技术侧调整 CameraSize，应**等比缩放所有速度值**，保持穿越时间不变。穿越时间是设计锚点，绝对速度是派生值。

§3.4 基准数值表补充：飞机 MoveSpeed = 8 单位/秒。

**状态**：✅ 收敛

---

### SG-015 追问回应 | ✅ 接受

**回应**：

攻方完全正确。我在 Round 2 引用了 `CountAliveByCamp(EnumCamp)` 这个不存在的 API，是我的失误。

经查代码确认：
- `EntityManager.CountAliveByConfig(EntityConfigSO)` — **存在**
- `EntitySpawner.IsAllWavesCleared` — **存在**，内部已用 `CountAliveByConfig` 逐 Group 检查存活数为 0

`IsAllWavesCleared` 精确等价于"最后一架敌机被消灭"的设计意图。

**更正后的通关判定**：

```
通关条件 = EntitySpawner.IsAllWavesCleared == true
```

不需要额外 API，不需要新增 `CountAliveByCamp`。

**补充方案**（更新 §2.3 / §4.3 通关判定）：

> **通关判定**：`EntitySpawner.IsAllWavesCleared == true`
>
> 该属性内部语义 = 所有波次生成完毕 + 所有 SpawnGroup 对应 EntityConfig 的在场存活数为 0。
> ~~`CountAliveByCamp(EnumCamp)`~~（已删除，框架中不存在此 API）

**状态**：✅ 收敛

---

### SG-NEW-001 回应 | ✅ 接受

**回应**：

攻方分析完全正确。`EntityCollisionSolver` 碰撞响应只有推力分离 + 接触伤害，没有"碰撞后销毁"逻辑。

**设计决定：敌机"撞后消失" = 利用框架已有能力，通过 ContactDamage 数值关系让敌机被"撞死"。**

| 碰撞对 | 具体行为 | 数值设计 |
|--------|---------|----------|
| 敌机 vs 飞机 | 飞机对敌机造成 ContactDamage=9999 → 敌机 HP 归零 → OnDeath → DeathDelay 播爆炸 → 回收 | 飞机 ContactDamage=9999 |
| 敌机 vs 基地 | 底线检测触发（见 SG-002）→ 手动 TakeDamage + Despawn | 不走 EntityCollisionSolver |

**为什么不用"碰撞后手动 Despawn"**：
- 利用框架已有的"接触伤害→HP归零→死亡→回收"链路，零额外代码
- 设计意图和技术实现完美对齐

**补充方案**（§3.1 + §3.4 补充）：

> **§3.1 碰撞响应补充**：
> 敌机"撞后爆炸消失"实现：飞机 ContactDamage=9999，敌机碰撞后被一击致死，走 OnDeath → DeathDelay 流程。
>
> **§3.4 基准数值表补充**：
> | 参数 | 值 | 说明 |
> |------|-----|------|
> | 飞机 ContactDamage | 9999 | 确保一撞即杀敌机 |
> | 飞机 ContactDamageInterval | 0 | 无冷却（默认会被设为 0.5s，需显式设 0 让框架走最小间隔） |

**状态**：✅ 收敛

---

### SG-NEW-002 回应 | ✅ 接受

**回应**：

**选定方案 A：飞机不挂 HealthComponent。**

理由：
1. **最简单直观** — 飞机没有 HealthComponent，`TryApplyDamage` 中 `victim.GetComponent(Health)` 返回 null，自然跳过伤害流程。零额外代码。
2. **设计意图完美匹配** — "飞机不可被摧毁"在技术层面 = "飞机没有 HP 这个概念"，语义清晰。

**设计决策修正**：

原 §3.1 说"敌机→飞机：基地扣血"。经深思后修正——玩家操控飞机被撞的惩罚应该是**敌机被消灭 + 屏幕震动反馈**，而非基地扣血。基地扣血唯一来源 = 敌机突破底线。这样玩家因果感更清晰：

| 碰撞对 | 对飞机 | 对敌机 | 对基地 |
|--------|--------|--------|--------|
| 敌机 → 飞机 | 屏幕震动反馈 | 被一击致死（ContactDamage=9999） | **无影响** |
| 敌机 → 基地（底线检测） | — | Despawn | 扣 ContactDamage 点 HP |

**飞机 Entity 建模**：

> | 属性 | 值 | 说明 |
> |------|-----|------|
> | 实体类型 | Entity（走 EntityManager.Spawn） | |
> | Camp | Ally | 己方阵营 |
> | Components[] | `[ Movement, Collision ]` | **不挂 Health** |
> | EnableEntityCollision | true | 参与圆 vs 圆碰撞 |
> | CollisionRadius | 0.3 | |
> | ContactDamage | 9999 | 一撞即杀敌机 |
> | MoveSpeed | 8 | 玩家操控速度 |
>
> **"不可被摧毁"实现**：不挂 HealthComponent → TryApplyDamage 中 GetComponent(Health) 为 null → 自然免疫所有伤害。方案 A，零额外代码。
>
> **设计修正**：~~敌机→飞机：基地扣血~~（已删除）。基地 HP 唯一扣减来源 = 敌机突破底线。

**状态**：✅ 收敛

---

## Round 4 统计

| 问题编号 | 类型 | 回应 | 状态 |
|----------|------|------|------|
| SG-002 追问 | 基地 Entity 建模 | ✅ 接受：Entity + [Health] + 底线检测 | ✅ 收敛 |
| SG-005 追问 | 速度坐标系锚点 | ✅ 接受：穿越时间锚点 + CameraSize=8 | ✅ 收敛 |
| SG-015 追问 | 通关判定 API | ✅ 接受：直接用 IsAllWavesCleared | ✅ 收敛 |
| SG-NEW-001 | 敌机撞后消失 | ✅ 接受：飞机 ContactDamage=9999 撞死 | ✅ 收敛 |
| SG-NEW-002 | 飞机不可被摧毁 | ✅ 接受：方案 A，不挂 HealthComponent | ✅ 收敛 |

**总收敛进展**：17/17（100%）。所有问题全部收敛。

---

## PK Round 5 — 攻方终轮确认（Unity 架构师）

Round 4 守方对所有 5 个未决问题给出了具体、可落地、有代码级别可执行性的回应。逐一确认：

### SG-002 | ✅ 终轮确认收敛
基地 = Entity + [Health] + 底线检测方案。三个子问题全部得到明确回答。底线检测方案比 AABB 碰撞扩展更简洁，框架零改动。**完全接受**。

### SG-005 | ✅ 终轮确认收敛
穿越时间锚点 + CameraSize=8 推导链完整。"穿越时间是锚点，速度是派生值"这个原则非常好——即使后续调整 CameraSize 也不会打破体验预期。飞机 MoveSpeed=8 补充到位。**完全接受**。

### SG-015 | ✅ 终轮确认收敛
纠正了不存在的 API，直接用 `EntitySpawner.IsAllWavesCleared`。经代码验证（`EntitySpawner.cs:300-311`），内部已逐 Group 检查 `CountAliveByConfig`。**完全接受**。

### SG-NEW-001 | ✅ 终轮确认收敛
飞机 ContactDamage=9999 利用框架已有链路一击致死敌机，零额外代码。方案巧妙。需注意 `ContactDamageInterval` 默认 0.5s（`EntityCollisionSolver.cs:237`），飞机配置需显式设为小值（如 0.01s）确保立即生效。**小 tip，不阻塞收敛。完全接受**。

### SG-NEW-002 | ✅ 终轮确认收敛
方案 A：飞机不挂 HealthComponent → 自然免疫。设计修正"撞飞机不扣基地血"让因果链更清晰。`TryApplyDamage` 代码已确认（`EntityCollisionSolver.cs:223`）：`victim.GetComponent(Health)` 为 null 时跳过。**完全接受**。

### Round 5 攻方补充技术提醒（不阻塞收敛）

以下为程序员实施时需注意的技术细节，不需要回写设计文档：

1. **ContactDamageInterval 默认值兜底**：`EntityCollisionSolver.TryApplyDamage` 中 `if (interval <= 0f) interval = 0.5f`——飞机配置 ContactDamageInterval 如果设 0 会被框架兜底为 0.5s。建议飞机设 0.01f 确保近乎无冷却。
2. **底线检测性能**：每帧遍历 ActiveEntities 检查 Position.y 是 O(n)，但 n≤40 完全可接受。
3. **BulletPatternSO vs BulletTypeSO**：Round 3 SG-006 技术事实修正中提到的框架命名不一致需要在技术文档中标注。
4. **飞机碰撞推力分离**：飞机不挂 Movement 的话 `ApplySeparation` 对飞机无效（因为 `moveA/moveB` 为 null 跳过）——但飞机挂了 Movement（操控需要），所以碰撞时飞机会被推开一小段。体验上可能需要调整 SeparationStrength 或飞机配置单独处理。这是调参问题，不阻塞。

---

## PK Round 6 — 最终收敛判定

### 收敛统计

| 问题编号 | 严重度 | 收敛轮次 | 最终状态 |
|----------|--------|---------|----------|
| SG-001 | 🔴高 | R2→R3✅ | ✅ 收敛 |
| SG-002 | 🔴高 | R2→R3🔄→R4✅→R5✅ | ✅ 收敛 |
| SG-003 | 🔴高 | R2→R3✅ | ✅ 收敛 |
| SG-004 | 🟡中 | R2→R3✅ | ✅ 收敛 |
| SG-005 | 🟡中 | R2→R3🔄→R4✅→R5✅ | ✅ 收敛 |
| SG-006 | 🟡中 | R2→R3✅ | ✅ 收敛 |
| SG-007 | 🟡中 | R2→R3✅ | ✅ 收敛 |
| SG-008 | 🟡中 | R2→R3✅ | ✅ 收敛 |
| SG-009 | 🟡中 | R2→R3✅ | ✅ 收敛 |
| SG-010 | 🟡中 | R2→R3✅ | ✅ 收敛 |
| SG-011 | 🟡中 | R2→R3✅ | ✅ 收敛 |
| SG-012 | 🟢低 | R2→R3✅ | ✅ 收敛 |
| SG-013 | 🟢低 | R2→R3✅ | ✅ 收敛 |
| SG-014 | 🟢低 | R2→R3✅ | ✅ 收敛 |
| SG-015 | 🟢低 | R2→R3🔄→R4✅→R5✅ | ✅ 收敛 |
| SG-NEW-001 | 🟡中 | R3🆕→R4✅→R5✅ | ✅ 收敛 |
| SG-NEW-002 | 🟡中 | R3🆕→R4✅→R5✅ | ✅ 收敛 |

### 最终判定

**✅ PK 评审通过 — 17/17 问题全部收敛**

- 原始问题 15 个 + 深挖新增 2 个 = 合计 17 个
- Round 3 确认 12 个一次收敛 + Round 4~5 收敛剩余 5 个
- 零遗留未决项
- 所有回应具备代码级可执行性

### 需要回写到设计文档的变更清单

| 优先级 | 变更内容 | 影响章节 |
|--------|---------|----------|
| P0 | 新增 §3.6 基地 Entity 建模（Entity + [Health] + 底线检测） | §3.1, §3.6 |
| P0 | 新增飞机 Entity 建模（[Movement, Collision] + 不挂 Health + ContactDamage=9999） | §3.6 |
| P0 | 更新碰撞结果表（修正"撞飞机扣基地血"→"撞飞机无基地影响"） | §3.1 |
| P0 | 新增 §3.4 V1 基准数值表（含基地/子弹/敌机/飞机全部参数） | §3.4 |
| P0 | 更新 §3.5 空间约束（CameraSize=8 + 穿越时间锚点表） | §3.5 |
| P1 | 新增 §2.1 战斗状态机（Intro→Playing→Victory/Defeat） | §2.1 |
| P1 | 新增 §4.4 场景架构（单 Scene + 多 WaveConfig SO） | §4.4 |
| P1 | 更新通关判定 API（EntitySpawner.IsAllWavesCleared） | §2.3, §4.3 |
| P1 | 操控手感补充（方向型/10px死区/零加减速/Unity Touch API） | §2.2 |
| P1 | V1 子弹参数（Speed=12, Lifetime=2s, 发射偏移, BulletPatternSO） | §3.2 |
| P2 | 无敌帧统一走 HealthComponent.IFrameCount | §3.1 |
| P2 | V1 性能约束（同屏敌机≤40, 峰值30~35） | §4.2 |
| P2 | 存储方案（ISaveSystem + sg_progress key） | §9.3 |
| P2 | V1 时间线编辑器降级为 V2 愿景 | §5.3 |
| P2 | 击退不影响匀速下落 | §3.3 |
| P2 | ScreenShakeConfigSO 编码期调试 | §2.2 |
| P2 | BGM 简化为 HP<30% 切换紧张版 | §2.2 |

> **PK 状态**：✅ 已完成
> **结束时间**：2026-05-03

