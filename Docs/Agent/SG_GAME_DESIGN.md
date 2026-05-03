# 飞行弹幕射击 · 游戏设计文档

> **版本**：v3.2（SO 清单补全版）  
> **日期**：2026-05-03  
> **品类**：纵版飞行射击（弹幕）  
> **平台**：微信小游戏  
> **PK 评审**：✅ 通过（17/17 + 10/10 收敛）— 详见 `SG_GAME_DESIGN_PK.md` + `SG_GAME_DESIGN_PK_TOOLS.md`

---

## 一、游戏概述

一句话描述：**操控战机消灭一波又一波从天而降的敌机，保护你的基地不被摧毁。**

玩家需要做的只有两件事：
1. **移动飞机**躲避敌机
2. **看着子弹自动打出去**，感受越来越爽的火力倾泻

这是一个"操控感 + 视觉爽感"优先的休闲射击游戏。上手零门槛，但关卡编排提供递进的挑战感。

---

## 二、玩家体验流程

### 2.0 战斗状态机

```
Intro(1.5s) → Playing → Victory / Defeat
```

| 状态 | 行为 |
|------|------|
| **Intro** | 飞机进场动画，不 Tick Spawner、不检测碰撞、不响应输入 |
| **Playing** | 正常战斗 |
| **Victory** | 0.5s 静默 → 胜利界面 |
| **Defeat** | 基地爆炸 → 失败界面 |

实现方式：`BattleState` 枚举 + EntitySystemBootstrap 层控制。

### 2.1 首次进入

```
打开小游戏
  │
  ▼
【加载画面】简短品牌 LOGO（≤2秒）
  │
  ▼
【选关界面】
  · 只有第一关亮着，其余灰色上锁
  · 文案引导："点击开始你的第一场战斗"
  │
  ▼ 点击第一关
【战前过渡】
  · 简短进场动画：我方飞机从底部飞入画面
  · 基地血条出现，数值满格
  · 短暂 1 秒"预备"节奏（让玩家视觉定位）
  │
  ▼
【战斗开始】
```

### 2.2 战斗中体验

```
┌─────────────────────────────────────────┐
│            屏幕最上方                      │
│                                          │
│    敌机从这里刷出，向下移动                  │
│         ↓   ↓   ↓                       │
│                                          │
│                                          │
│         ✈️ ← 我方飞机（虚拟摇杆控制）      │
│         │                                │
│      子弹自动向上射出                       │
│                                          │
│  ═══════════════════════════════════════  │
│  ▉▉▉▉▉▉▉▉▉▉▉▉▉▉▉▉ 基地血条            │
│  【基 地 区 域】                           │
└─────────────────────────────────────────┘
```

**操控手感**：
- **虚拟摇杆**：手指按下屏幕任意位置即为摇杆中心，拖动方向控制飞机移动
- 手指不遮挡主角机——摇杆中心在手指按下处，飞机在画面中独立响应
- 移动范围：全屏幕（包括基地区域上方）
- **瞬时响应**，松手即停，休闲游戏优先手感直接
- **摇杆模式**：方向型（Direction-only），速度恒定 = `EntityConfigSO.MoveSpeed`
- **死区半径**：10px 物理像素
- **加减速**：零（无惯性），松手瞬间速度归零
- **移动约束**：飞机位置 clamp 在相机可视矩形内
- **输入 API**：Unity Touch API（平台适配由框架层处理）

**战斗节奏**：
- 敌机一波一波地出，每波之间有短暂间歇（让玩家喘口气 + 产生"下一波来了"的心理准备）
- 前几波很简单（3-5 架直线下落），让玩家建立信心
- 中段开始密集、出现快速敌机
- 后段压力最大，敌机数量和速度达到峰值
- **节奏核心**：紧-松-紧-松-高潮 → 结束

**打击反馈**：
- 子弹命中敌机 → 敌机闪白 + 轻微击退 + 碎屑粒子
- 敌机被击毁 → 爆炸特效 + 屏幕轻微震动 + 击杀积分飘字
- 敌机撞到飞机/基地 → 屏幕红色闪烁 + 血条扣减动画 + 警告音效
- 基地血量低于 30% → 血条变红 + 脉冲闪烁 + 切换紧张版 BGM（V1 不做多轨叠加）
- 屏幕震动参数：创建 `ScreenShakeConfigSO`，参数在 Inspector 中编码期调试

**V1 打击反馈实现优先级表**：

| 优先级 | 效果 | 系统归属 | 配置入口 |
|--------|------|----------|----------|
| P0 必须 | 敌机闪白 | EntityHitReactionHandler | EntityConfigSO.HitFlashDuration/Color |
| P0 必须 | 击退 | MovementComponent | EntityConfigSO.KnockbackForce |
| P0 必须 | 爆炸特效 | Entity View → DeathEffect Prefab | EntityConfigSO.DeathEffect |
| P0 必须 | 屏幕震动 | Camera Shake（新增） | ScreenShakeConfigSO（2 字段组） |
| P1 应有 | 碎屑粒子 | Entity View → HitEffect | EntityConfigSO.HitEffect |
| P1 应有 | 击杀飘字 | ShowDamageNumber 系统 | EntityConfigSO.ShowDamageNumber=true |
| V2 | 积分飘字 | FairyGUI 动态 | 专用 ScorePopup 组件 |
| V2 | 基地受伤红闪 | Camera Overlay | 独立 RedFlash 组件 |
| V2 | 血条动画 | FairyGUI Tween | UI 层实现 |

**ScreenShake 触发规则**：

| 事件 | 触发 | 强度 | 理由 |
|------|------|------|------|
| 玩家子弹击杀敌机 | ❌ | — | 频率太高，持续震动 = 没震动 |
| 飞机撞击杀敌机 | ✅ | 中 (0.15s, 0.3) | 撞击是高风险操作，需反馈强化 |
| 敌机突破底线（基地扣血） | ✅ | 强 (0.3s, 0.6) | 惩罚信号，必须明确 |

### 2.3 战斗结束

**通关**：
```
EntitySpawner.IsAllWavesCleared == true
（内部语义：所有波次生成完毕 + 所有 SpawnGroup 对应 EntityConfig 在场存活数为 0）
  │
  ▼
短暂 0.5 秒静默（让玩家意识到"打完了"）
  │
  ▼
【胜利界面】
  · "VICTORY" 大字 + 星级评价（预留，V1 不做评分）
  · 显示本关数据：击杀数、剩余血量百分比
  · [确定] 按钮 → 返回选关界面
  · 下一关解锁动画（锁头打开的微动效）
```

**失败**：
```
基地血量归零
  │
  ▼
基地爆炸特效 + 屏幕渐暗
  │
  ▼
【失败界面】
  · "DEFEAT" + 简短鼓励文案（"再来一次！"）
  · 显示本关进度：消灭了 X / 总共 Y 架敌机
  · [再试一次] → 重新开始本关
  · [返回] → 回到选关界面
```

> **设计意图**：失败界面的"再试一次"按钮比"返回"更突出——降低流失，鼓励重试。

### 2.4 关卡间流程

```
选关界面
  │
  ├── 关卡 1 ★（已通关，显示星标）
  ├── 关卡 2 ▶（可进入，高亮脉冲）
  ├── 关卡 3 🔒（未解锁，灰色）
  ├── 关卡 4 🔒
  └── 关卡 5 🔒
```

- 玩家通关后自动回到选关界面，下一关解锁
- 已通关的关卡可以**重玩**（用于刷分或纯粹享受）
- 选关界面整体布局：关卡图标可以做成一条航线/路径的形式（视觉引导进度感）

---

## 三、核心规则

### 3.1 阵营与碰撞

| 元素 | 阵营 | 说明 |
|------|------|------|
| 我方飞机 | 己方 | 玩家操控，**不可被摧毁**（不挂 HealthComponent） |
| 基地 | 己方 | 固定在底部，有血量（Entity + HealthComponent） |
| 我方子弹 | 己方 | 自动发射，向上飞行 |
| 敌方飞机 | 敌方 | 自动向下移动，有血量 |

**碰撞结果表**：

| 碰撞对 | 对飞机 | 对敌机 | 对基地 | 实现路径 |
|--------|--------|--------|--------|----------|
| 我方子弹 → 敌机 | — | 受伤，子弹消失 | — | DanmakuSystem TargetRegistry |
| 敌机 → 飞机 | 屏幕震动反馈 | 被一击致死（飞机 ContactDamage=9999）→ DeathDelay 播爆炸 | **无影响** | EntityCollisionSolver（圆 vs 圆） |
| 敌机 → 基地 | — | Despawn（DeathDelay 播爆炸） | 扣 ContactDamage 点 HP | 底线检测（Position.y ≤ BaseLineY） |

**碰撞实现约束**：
- 飞机碰撞体：**圆形**（CollisionRadius=0.3），参与 EntityCollisionSolver
- 基地碰撞体：**不参与 EntityCollisionSolver**（底线检测替代 AABB 碰撞）
- 敌机碰撞后走 DeathDelay 流程（`EntityConfigSO.DeathDelay`），特效播完再回收
- "撞后爆炸消失"实现：飞机 ContactDamage=9999 → 敌机 HP 归零 → OnDeath → DeathDelay → 回收

**无敌帧实现**：
- 飞机不挂 HealthComponent → `TryApplyDamage` 中 `GetComponent(Health)` 为 null → 自然免疫
- ~~飞机被撞进入 0.5s 无敌帧~~（已移除：飞机无 HP，不需要无敌帧）
- 敌机碰撞冷却：`ContactDamageInterval` 控制（飞机设 0.01s，近乎无冷却）

> **核心设计决策（v3.0 修正）**：
> - 飞机**不可被摧毁**且**不挂 HealthComponent**——被撞时自然免疫所有伤害
> - 基地 HP 唯一扣减来源 = **敌机突破底线**（底线检测方案）
> - ~~撞飞机扣基地血~~（v3.0 删除）——让因果链更清晰：走位好 → 敌机被拦截在上方 → 基地安全
> - 敌机撞飞机的玩家惩罚 = 屏幕震动反馈 + 被推开的短暂位移（DPS 输出中断）

**ScreenShakeConfigSO 定义**（TDD 阶段创建）：

```
ScreenShakeConfigSO : ScriptableObject
├── CollisionShake（飞机撞击敌机时触发）
│   ├── Duration: 0.15f
│   ├── Intensity: 0.3f
│   └── DecayCurve: AnimationCurve
├── BaseHitShake（敌机突破底线时触发）
│   ├── Duration: 0.3f
│   ├── Intensity: 0.6f
│   └── DecayCurve: AnimationCurve
```

- **实现方式**：Camera Transform 位置偏移（Perlin Noise 采样），零 GC
- **不使用** Post-Processing Volume（微信小游戏不友好）
- V1 用 2 字段组（6 个字段），不做通用预设数组
- V1 不需要 CustomEditor（字段量小，默认 Inspector + AnimationCurve 编辑已足够）

### 3.2 技能系统（V1 简化版）

- 飞机拥有一个**技能列表**
- 每个技能按固定间隔**自动发射**，无需玩家操作
- V1 默认只有一个技能：**单发直射**（向正前方发一颗子弹）
- 所有子弹走**弹幕系统**处理（高频生成/销毁、轨迹计算、碰撞检测）

**V1 子弹参数**：
- 走 `AttackComponent` + `BulletPatternSO`
- Speed = 12 单位/秒
- Size = 0.16 世界单位（对应 16px@100PPU）
- Lifetime = 2s（超出屏幕前已足够到达顶部）
- 发射偏移 = (0, 0.4)（飞机头部位置）
- 碰撞后回收（击中敌机 → 弹幕系统 Die 响应）

**后续可扩展**：
- 散射（一次 3/5 发扇形）
- 追踪弹（自动锁定最近敌机）
- 激光（持续伤害、穿透）
- 护盾（暂时挡一次碰撞伤害）

### 3.3 敌机行为（V1）

V1 所有敌机行为极简：
- **直线下落**：匀速从出生点向屏幕底部移动
- 不开火、不变速、不转向
- **击退是额外位移**，不影响匀速下落方向的运动（框架 `MovementComponent.ApplyKnockback` 叠加实现）

> 这已经够用了——节奏感来自"什么时候出、出多少、出多快"的编排，不是来自 AI 复杂度。

---

### 3.4 V1 基准数值表

| 参数 | 值 | 说明 |
|------|-----|------|
| 基地 MaxHp | 100 | 统一基准 |
| 子弹 Damage | 10 | 单发伤害 |
| 子弹发射间隔 | 0.3s | AttackInterval |
| 子弹速度 | 12 单位/秒 | 偏快，确保打击密度感 |
| 普通敌机 MaxHp | 20 | 2 发击杀 → 即时满足感 |
| 普通敌机 MoveSpeed | 2 单位/秒 | 慢速，给玩家反应时间 |
| 快速敌机 MoveSpeed | 4 单位/秒 | 第 3 关起出现 |
| 敌机 ContactDamage | 15 | 约 6~7 次突破底线 Game Over |
| 飞机 MoveSpeed | 8 单位/秒 | 操控灵敏但不失控 |
| 飞机 ContactDamage | 9999 | 一撞即杀敌机 |
| 飞机 ContactDamageInterval | 0.01s | 近乎无冷却 |
| 基地第 3 关初始 HP | 90 | MaxHp×90% |
| 基地第 4/5 关初始 HP | 80 | MaxHp×80% |

**数值逻辑**：普通敌机 2 发击杀 → 玩家有效率感；基地容错约 6~7 次突破，对新手友好。

**V1 SO 资产清单**（21 个）：

| 资产名 | SO 类型 | 存放路径 |
|--------|---------|----------|
| SG_Player | EntityConfigSO | `Assets/_Game/Configs/ShooterGame/` |
| SG_Base | EntityConfigSO | `Assets/_Game/Configs/ShooterGame/` |
| SG_Enemy_Normal | EntityConfigSO | `Assets/_Game/Configs/ShooterGame/` |
| SG_Enemy_Fast | EntityConfigSO | `Assets/_Game/Configs/ShooterGame/` |
| SG_Level_01 ~ 05 | SG_LevelConfigSO | `Assets/_Game/Configs/ShooterGame/Levels/` |
| SG_Wave_01 ~ 05 | EntitySpawnWaveSO | `Assets/_Game/Configs/ShooterGame/Waves/` |
| SG_PlayerBullet_Straight | BulletPatternSO | `Assets/_Game/Configs/ShooterGame/` |
| SG_ScreenShake_Default | ScreenShakeConfigSO | `Assets/_Game/Configs/ShooterGame/` |
| SG_CurrentLevelIndex | IntVariable | `Assets/_Game/Configs/ShooterGame/Variables/` |
| SG_BaseHP | FloatVariable | `Assets/_Game/Configs/ShooterGame/Variables/` |
| SG_CurrentWaveIndex | IntVariable | `Assets/_Game/Configs/ShooterGame/Variables/` |
| SG_TotalWaveCount | IntVariable | `Assets/_Game/Configs/ShooterGame/Variables/` |
| SG_KillCount | IntVariable | `Assets/_Game/Configs/ShooterGame/Variables/` |
| SG_TotalEnemyCount | IntVariable | `Assets/_Game/Configs/ShooterGame/Variables/` |
| SG_InputDirection | Vector2Variable | `Assets/_Game/Configs/ShooterGame/Variables/` |

> **SG_BaseHP**：存储归一化比例（0~1），`HealthComponent` 在 HP 变化时写入 `currentHP / maxHP`。UI 直接读取显示百分比，无需知道 maxHP。

> **命名规范**：`SG_` 前缀为 ShooterGame 游戏级资产（区别于框架模板前缀 `Template_`）。

### 3.5 空间约束

| 参数 | 值 | 说明 |
|------|-----|------|
| 设计分辨率 | 1080×1920（9:16） | 竖版 |
| **推荐 CameraSize** | **8**（正交半高） | 可视区域 = 9×16 世界单位 |
| 推荐 PPU | 100 | 1 世界单位 = 100px |
| 飞机活动区域 | 屏幕可视区全范围 | |
| 敌机出生线 | 屏幕顶部外 1 个身位 | |
| 基地区域 | 屏幕底部（BaseLineY） | |
| 敌机出生 X 范围 | 屏幕宽度的中间 60%~80% | |

**体验锚点（设计侧承诺）**：

| 行为 | 期望时间 | 对应速度 |
|------|---------|----------|
| 普通敌机顶→底 | ~8s | 2 单位/秒 |
| 快速敌机顶→底 | ~4s | 4 单位/秒 |
| 子弹底→顶 | ~1.3s | 12 单位/秒 |
| 飞机左→右 | ~1.1s | 8 单位/秒 |

> 如果技术侧调整 CameraSize，应**等比缩放所有速度值**，保持穿越时间不变。穿越时间是设计锚点，绝对速度是派生值。

### 3.6 Entity 建模

#### 基地 Entity

| 属性 | 值 | 说明 |
|------|-----|------|
| 实体类型 | Entity（走 EntityManager.Spawn） | 非纯 UI 逻辑 |
| Camp | Ally | 己方阵营 |
| Components[] | `[ Health ]` | 仅血量，无移动/碰撞/攻击 |
| MaxHp | 100 | V1 基准 |
| EnableEntityCollision | false | 不参与 EntityCollisionSolver |
| CollisionRadius | 0 | 无圆形碰撞体 |

**敌机 vs 基地碰撞方案**：底线检测——Game 层每帧检查敌机 `Position.y <= BaseLineY`，命中则：
1. 对基地 HealthComponent 造成 `敌机.ContactDamage` 伤害
2. 敌机 Despawn（走 DeathDelay 播放爆炸特效后回收）

**基地渲染与调试**：
- **不需要 ViewPrefab**——基地是逻辑概念（底线 Y），视觉由 FairyGUI HP 血条 Overlay 表达。设置 ViewPrefab 反而浪费 DrawCall。
- **BaseLineY Gizmo**：BattleController/Bootstrap MonoBehaviour 的 `OnDrawGizmos()` 画一条红色横线标记底线位置，辅助调参。
- **基地 HP 调试**：走 EntityDebugWindow——基地也是 Entity，自动出现在 Active Entity 列表中，HP 实时可见。

#### 飞机 Entity

| 属性 | 值 | 说明 |
|------|-----|------|
| 实体类型 | Entity（走 EntityManager.Spawn） | |
| Camp | Ally | 己方阵营 |
| Components[] | `[ Movement, Collision ]` | **不挂 Health**——不可被摧毁 |
| EnableEntityCollision | true | 参与圆 vs 圆碰撞 |
| CollisionRadius | 0.3 | |
| ContactDamage | 9999 | 一撞即杀敌机 |
| ContactDamageInterval | 0.01 | 近乎无冷却 |
| MoveSpeed | 8 | 玩家操控速度 |

**"不可被摧毁"实现**：不挂 HealthComponent → TryApplyDamage 中 GetComponent(Health) 为 null → 自然免疫所有伤害。方案 A，零额外代码。

---

## 四、关卡系统

### 4.1 关卡结构

- 共 5 关
- 线性解锁（通关第 N 关解锁第 N+1 关）
- 每关一套独立的**出怪时间线**
- 每关可以有不同的基地初始血量（体现难度梯度）

### 4.2 难度曲线设计指导

| 关卡 | 定位 | 节奏特点 | 参考基地血量 |
|------|------|----------|-------------|
| 第 1 关 | 教学关 | 敌机少、慢、间隔长；让玩家熟悉操控 | 100% |
| 第 2 关 | 热身 | 敌机数量增加，出现两列同时下落 | 100% |
| 第 3 关 | 上强度 | 出现快速敌机，波次间隔缩短 | 90% |
| 第 4 关 | 高压 | 密集出怪 + 多种速度混合 | 80% |
| 第 5 关 | 终局 | 最高密度，节奏最快，最后一波高潮 | 80% |

> 数值仅为指导，具体靠策划在编辑器中调试打磨。

**V1 性能约束**：同屏敌机不超过 **40 架**（TargetRegistry 64 槽，留余量给玩家子弹碰撞目标）。第 5 关峰值设计上限 30~35 架。

### 4.3 单关内部结构

每一关由若干**波次（Wave）** 组成，每波之间有**间歇**。

```
关卡开始
  │
  ▼
[Wave 1] ─── 间歇 ─── [Wave 2] ─── 间歇 ─── [Wave 3] ─── ... ─── [Final Wave]
  │                                                                      │
  ▼                                                                      ▼
  简单热身                                                           最终高潮
```

**一个 Wave 的定义**：
- 一组敌机同时或依次出场
- 出场位置（X 坐标）
- 出场间隔（同一波内逐架出场的时间间隔）
- 敌机类型

**波与波之间的间歇**：
- 让玩家喘口气
- 间歇时间可以随关卡推进而缩短（加紧张感）
- 最后一波前可以有一个稍长的间歇（"暴风雨前的宁静"）

### 4.4 场景架构

- **单战斗 Scene** + **多 LevelConfig SO**（5 关 = 5 个 `SG_LevelConfigSO`）
- 选关界面 = FairyGUI UI overlay（不切 Scene）
- 关卡数据传递：选关 → 设置 `CurrentLevelIndex` **IntVariable** SO → 战斗场景 Bootstrap 读取对应 LevelConfig
- 进入战斗 = 加载 Battle Scene；退出 = 回到 MainMenu Scene（Boot.unity 走 SceneLoader）
- 越界防护：Bootstrap 加载时 `Mathf.Clamp(index, 0, _levelConfigs.Length - 1)` + `Debug.LogWarning`

**SG_LevelConfigSO**（游戏级关卡元数据，不污染框架 SO）：

```csharp
[CreateAssetMenu(menuName = "ShooterGame/LevelConfig")]
public class SG_LevelConfigSO : ScriptableObject
{
    public EntitySpawnWaveSO WaveConfig;  // 本关波次数据
    public float BaseHpRatio = 1.0f;      // 基地初始 HP 比例
    public int UnlockRequirement = 0;     // 前一关需要几星解锁（V1 = 0，通关即解锁）
}
```

- Bootstrap 读取 `SG_LevelConfigSO[]`，按 `CurrentLevelIndex` 索引
- 基地 HP 参数化：`healthComp.SetMaxHp(configMaxHp * levelConfig.BaseHpRatio)`

### 4.5 通关判定

```
通关条件 = EntitySpawner.IsAllWavesCleared == true
```

内部语义：所有波次生成完毕 + 所有 SpawnGroup 对应 EntityConfig 的在场存活数为 0。等价于"最后一架敌机被消灭"。

---

## 五、策划关卡编排工作流（重点）

### 5.1 问题定义

传统做法：策划填一张 Excel 表（时间、坐标、类型...），然后导出到游戏里看效果。

**痛点**：
- 数字不直观——"delay=3.5, spawnX=0.3" 是什么感觉？必须启动游戏才知道
- 调试循环慢——改数 → 导表 → 重启 → 玩到那一波 → 发现不对 → 再改
- 节奏感难把握——纯看数字无法感受"紧凑"还是"松弛"

### 5.2 目标工作流

策划需要的是：**所见即所得、快速迭代、节奏可视化**。

### 5.3 方案：时间线编辑器（Unity Editor 内）

> ⚠️ **V1 实现方案**：V1 使用框架已有的 `EntitySpawnWaveSO` + Inspector 编辑，不开发独立时间线编辑器。以下为 **V2 愿景**。

**V1 EntitySpawnWaveSOEditor 改善需求**（TDD 阶段实施）：

| # | 改善项 | 行为规格 |
|---|--------|----------|
| 1 | **一键复制最后一波** | 深拷贝 `Waves[last]` 追加到末尾；新波次 `spawnDelay` = 源 delay + estimatedDuration + 3s；复制后自动折叠旧波次、展开新波次 |
| 2 | **总敌机/总时长统计面板** | 在摘要面板中显示：总波次数、总敌机数、预估总时长（秒） |

- 按钮文案：`"+ 复制最后一波"`（中文，独立开发者无国际化需求）
- 按钮位置：Waves 列表底部

设计一个类似"音序器"的**关卡时间线编辑器**：

```
┌─────────────────────────────────────────────────────────┐
│  关卡时间线编辑器                            [▶播放] [■停止] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  时间轴 ──────────────────────────────────────────────── │
│  0s    2s    4s    6s    8s    10s   12s   14s   16s    │
│  │     │     │     │     │     │     │     │     │     │
│  ┃━━━Wave1━━━┃  ┃━━━Wave2━━━━━━┃  ┃━━━━Wave3━━━━━━━┃   │
│  │           │  │              │  │                │   │
│  │  ●●●      │  │  ●●●●●      │  │  ●●●●●●●●     │   │
│  │  3架慢速  │  │  5架中速    │  │  8架快速      │   │
│  │           │  │              │  │                │   │
├─────────────────────────────────────────────────────────┤
│  属性面板（选中 Wave2）：                                 │
│  · 敌机类型：中型机                                       │
│  · 数量：5                                               │
│  · 出场间隔：0.4s                                        │
│  · 出场 X 范围：[-0.6, 0.6]（随机散布）                   │
│  · 移动速度倍率：1.0x                                    │
└─────────────────────────────────────────────────────────┘
```

### 5.4 编辑器核心功能

| 功能 | 说明 |
|------|------|
| **时间轴拖拽** | 每个 Wave 是时间轴上的一个色块，可拖动调整出场时间 |
| **Wave 属性面板** | 选中一个 Wave，右侧显示详细参数可直接修改 |
| **预览播放** | 点击播放按钮，在 Scene View 中预览出怪效果（不进入 Play Mode） |
| **快进/慢放** | 调整预览速度，快速定位到某一波查看效果 |
| **跳转到指定波** | 直接从某一波开始预览，不用从头看 |
| **实时修改** | 预览过程中可以暂停、修改参数、继续——即时看到变化 |
| **出场位置可视化** | 在 Scene View 中显示每个 Wave 的出场区域（用 Gizmo 画出出生点分布） |

### 5.5 策划实际操作流程

```
第一步：选择要编辑的关卡
  │
  ▼
第二步：在时间轴上添加 Wave
  · 右键时间轴空白处 → "添加波次"
  · 拖动色块调整出场时间
  │
  ▼
第三步：配置每个 Wave 的参数
  · 选中 Wave → 属性面板中设置敌机类型、数量、间隔、位置
  │
  ▼
第四步：点击"预览"看效果
  · Scene View 中实时展示敌机出场
  · 可暂停、快进、跳到任意波
  │
  ▼
第五步：反复微调
  · 觉得这波太密？拉开间隔
  · 觉得节奏太松？把下一波往前拖
  · 直到手感满意为止
  │
  ▼
第六步：保存
  · 编辑器自动序列化为配置数据
  · 也可以导出为可读的 JSON/表格（方便版本管理）
```

### 5.6 底层数据结构（策划不需要直接操作）

编辑器最终序列化出的数据等价于：

```
Level:
  id: 1
  baseHp: 100
  waves:
    - startTime: 0.0
      enemyType: "normal"
      count: 3
      spawnInterval: 0.8
      spawnXRange: [-0.4, 0.4]
      speedMultiplier: 1.0
    - startTime: 4.0
      enemyType: "normal"
      count: 5
      spawnInterval: 0.5
      spawnXRange: [-0.6, 0.6]
      speedMultiplier: 1.2
    ...
```

> 策划不需要手写这些——全部通过可视化编辑器操作生成。表格只是存储格式，不是编辑界面。

### 5.7 与 Luban 配置的关系

**V1 决策：纯 SO 路径**。

- V1 关卡数据管线 = 5 个 `EntitySpawnWaveSO` 资产 + 5 个 `SG_LevelConfigSO` 资产，直接在 Inspector 编辑
- **理由**：独立开发者不需要 Excel 流程；SO 在 Play Mode 下可准 Hot Reload；5 关数据量小
- `EntityConfigValidator` 已有 SpawnWaveSO 校验（Waves 非空、Group.EntityConfig 引用非空），直接复用
- **Luban 路径保留为 V2 扩展选项**：当关卡数 >10 或数值表规模需要团队协作时迁移

---

## 六、美术资源需求

### 6.1 资源清单

| 类别 | 资源 | 规格建议 | 备注 |
|------|------|----------|------|
| **我方飞机** | 静态 sprite | 64×64 px | 需 2-3 帧倾斜动画（左移/右移时机身微倾） |
| **敌方飞机** | 静态 sprite × 2-3 种 | 48×48 px ~ 64×64 px | 普通机、快速机、大型机（V1 可先做 1 种） |
| **子弹** | 小 sprite / 粒子 | 16×16 px | 要有明显的拖尾/发光感 |
| **基地** | 底部横条区域 sprite | 全屏宽 × 32px 高 | 可以是一条科幻风格的防线 |
| **背景** | 滚动星空 / 大气层 | 全屏 | 循环滚动营造飞行感 |
| **爆炸特效** | 序列帧 / 粒子 | 3-5 帧 | 敌机被击毁时播放 |
| **命中特效** | 小闪光 | 2-3 帧 | 子弹命中瞬间 |
| **UI** | 血条、按钮、关卡图标 | - | FairyGUI 组件 |
| **音效** | 射击声、爆炸声、被撞警告、胜利/失败 | - | 短促清脆 |
| **BGM** | 战斗 BGM × 1、选关界面 BGM × 1 | - | 循环播放 |

### 6.2 美术工作流

```
第一步：确认风格
  · 已确定：扁平矢量风格（现代感、适配多分辨率、清晰辨识）
  │
  ▼
第二步：出关键帧概念图
  · 一张战斗画面截图级 mockup（标注每个元素位置和比例）
  │
  ▼
第三步：按优先级交付
  · P0：我方飞机 + 敌机 1 种 + 子弹 + 基地 + 背景 → 可跑通一关
  · P1：爆炸特效 + 命中特效 + UI → 完整体验
  · P2：更多敌机种类 + 音效 + BGM → 丰富度
  │
  ▼
第四步：规格约束
  · 矢量源文件用 SVG / AI 格式，导出为 PNG
  · 所有 sprite 尺寸对齐 2 的幂次（或 atlas 打包时自动处理）
  · 单张 atlas 不超过 2048×2048（微信小游戏内存限制）
  · 导出格式：PNG（透明底）
  · 命名规范：{类型}_{名字}_{序号}.png（如 enemy_normal_01.png）
  · ⚠️ **不使用 `_N` 后缀**——避免与框架 TextureImportEnforcer 法线贴图检测规则冲突
```

---

## 七、完整玩家动线（Player Journey Map）

从玩家视角，体验一个完整的游戏会话：

```
【打开小游戏】
    │
    ▼
【加载画面】── 品牌 LOGO，≤2秒
    │
    ▼
【选关界面】── 看到进度，选择要挑战的关卡
    │
    ▼
【进入关卡】── 飞机进场动画，血条出现
    │
    ▼
【战斗阶段】
    │
    ├─ 虚拟摇杆操控飞机走位
    ├─ 看子弹自动打出（爽感）
    ├─ 看敌机被打爆（成就感）
    ├─ 被敌机撞了！血条掉了！（紧张感）
    ├─ 越来越多越来越快（压力递增）
    │
    ├──→ 全部消灭！ → 胜利界面 → 解锁下一关 → 选关界面
    └──→ 血量清零 → 失败界面 → "再试一次" / "返回"
    
【一个会话中可能玩 2-5 关】
    │
    ▼
【退出 / 全通关】── 全通关后可重玩任意关卡
```

**情绪曲线**：
```
兴奋度
  ▲
  │         /\        /\     /\  ← 每关高潮
  │        /  \      /  \   /  \ 
  │   /\  /    \    /    \ /    \
  │  /  \/      \  /      X     ← 通关时的最高峰
  │ /            \/             
  │/                           
  └───────────────────────────────→ 时间
     关1    关2    关3    关4   关5
```

---

## 八、未覆盖 / 后续版本的设计空间

这些不在 V1 范围内，但作为设计师我需要标记出后续可以挖掘的方向：

| 方向 | 体验价值 | 优先级 |
|------|----------|--------|
| **技能升级/解锁** | 给玩家"变强"的成长感 | 高（V2 首选） |
| **敌机开火** | 增加走位深度，从"拦截"变为"攻防兼备" | 中 |
| **Boss 关** | 单对单对决，仪式感强，记忆点 | 中 |
| **道具掉落** | 随机性、惊喜感、短期爽感爆发 | 中 |
| **无尽模式** | 给通关玩家留下来的理由 + 排行榜社交 | 高 |
| **每日挑战** | 留存手段，每天给一个新鲜理由 | 高（V2 或 V3） |
| **IAA 广告位** | 复活广告、翻倍奖励广告 | 高（变现必须） |
| **皮肤/外观** | IAP 付费点或观看广告解锁 | 低（V3） |

---

## 九、存储与进度

### 9.1 需要存什么

```
玩家进度：
  - 每一关是否通关（bool）
  - （预留）每关最佳表现数据
```

### 9.2 什么时候存

- **通关时**：标记当前关已通关 → 解锁下一关 → 持久化
- **失败时**：不改变进度
- **退出时**：当前关进度不保存（退出等于放弃本次战斗）

### 9.3 存储方案

- 统一走框架 `ISaveSystem`（PlayerPrefs → 微信自动映射 wx.setStorageSync）
- Key = `"sg_progress"`
- Value = JSON: `{"clearedLevels":[1,2],"version":1}`
- 版本字段 `version` 预留后续数据迁移
- 上线期可选升级：微信云开发（跨设备同步）

---

## 十、已确认设计决策

| # | 问题 | 决策 |
|---|------|------|
| 1 | 操控方式 | **虚拟摇杆**——方向型（Direction-only），速度恒定，10px 死区，零加减速 |
| 2 | 手指遮挡问题 | 摇杆中心在按下处，飞机在画面中独立移动，**手指不遮挡主角机** |
| 3 | 飞机被撞惩罚 | 敌机被一击致死（ContactDamage=9999）+ 屏幕震动。~~基地扣血~~（v3.0 删除） |
| 4 | 基地扣血来源 | **唯一来源**：敌机突破底线（Position.y ≤ BaseLineY） |
| 5 | 飞机不可被摧毁 | **方案 A**：不挂 HealthComponent，自然免疫 |
| 6 | 基地建模 | Entity + [Health]，底线检测替代 AABB 碰撞 |
| 7 | 坐标系 | CameraSize=8，可视区域 9×16，PPU=100。穿越时间是锚点，速度是派生值 |
| 8 | 通关判定 | `EntitySpawner.IsAllWavesCleared == true` |
| 9 | 场景架构 | 单战斗 Scene + 多 WaveConfig SO + FairyGUI overlay 选关 |
| 10 | 重玩奖励 | **V1 无奖励**，纯重玩；后续版本再加留存设计 |
| 11 | 美术风格 | **扁平矢量**（现代感、多分辨率适配好） |
| 12 | V1 编辑器 | 用现有 EntitySpawnWaveSO + Inspector，时间线编辑器降 V2 |
| 13 | 存储 | ISaveSystem + key="sg_progress" + JSON + version 字段 |
| 14 | V1 数据管线 | **纯 SO 路径**（EntitySpawnWaveSO + SG_LevelConfigSO），Luban 留 V2 |
| 15 | 关卡索引 | **IntVariable**（非 FloatVariable），运行时 Clamp 防越界 |

---

## 十一、V1 编辑器工具与配置清单

> 来源：编辑器工具可执行性 PK 评审收敛结论（`SG_GAME_DESIGN_PK_TOOLS.md`）

### 11.1 Editor 改善清单

| # | 改善项 | 目标 SO | 预估工时 |
|---|--------|---------|----------|
| 1 | 一键复制最后一波（深拷贝 + delay 自动递增） | EntitySpawnWaveSO | 1h |
| 2 | 总敌机/总时长统计面板 | EntitySpawnWaveSO | 0.5h |

### 11.2 新增 SO 类型

| SO 类 | 职责 | 字段 |
|--------|------|------|
| `SG_LevelConfigSO` | 关卡元数据（游戏级，不污染框架） | WaveConfig, BaseHpRatio, UnlockRequirement |
| `ScreenShakeConfigSO` | 屏幕震动配置 | CollisionShake(Duration/Intensity/Curve) + BaseHitShake(Duration/Intensity/Curve) |

### 11.3 Gizmo 需求

| Gizmo | 负责方 | 用途 |
|-------|--------|------|
| BaseLineY 红色横线 | BattleController.OnDrawGizmos | 底线位置可视化，辅助调参 |

### 11.4 构建与校验

- 复用现有 `EntityConfigValidator.ValidateAll()`（覆盖 SO 完整性）
- 复用现有 `BuildPipeline.cs` 场景检查
- V1 不新增构建前 Error 阻断

### 11.5 V2 工具待办（不在 V1 范围）

- BulletPatternSO CustomEditor（条件显示 + 弹幕预览）
- 时间线编辑器 EditorWindow
- 构建前 Error 级阻断（关卡数 >10 时）

---

> **文档状态**：v3.2 — SO 清单补全（14→21 个，含 UI 相关 SO）  
> **PK 评审**：✅ 通过（17/17 + 10/10 收敛）— 详见 `SG_GAME_DESIGN_PK.md` + `SG_GAME_DESIGN_PK_TOOLS.md`  
> **下一步**：输出 ShooterGame TDD 技术设计文档 → 实施。
