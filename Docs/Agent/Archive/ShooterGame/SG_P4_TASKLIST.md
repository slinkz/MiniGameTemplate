---
system: shootergame
scope: integration-acceptance
last_verified: 2026-05-06
depends_on: [SG_DEV_PLAN, SG_TDD_INDEX, SG_GAME_DESIGN]
related_code: Assets/_Game/Scripts/ShooterGame/**/*.cs, Assets/_Game/Configs/ShooterGame/**/*.asset
---

# SG-P4 集成验收任务清单

> **阶段定位**：不新增系统，只做"资产补齐 + 内容编排 + 全链路验收 + 发布前检查"  
> **前置条件**：SG-P0~P3 全部验收通过 ✅（2026-05-06）  
> **预计工时**：3~4h（含调试缓冲）  
> **完成标志**：5 关全链路 PlayMode 跑通 + 配置一致性 0 问题

---

## 当前资产盘点（2026-05-06 快照）

### 已存在

| 类型 | 已有资产 | 数量 |
|------|---------|------|
| EntityConfig | SG_Player / SG_Base / SG_Enemy_Normal | 3 |
| Config SO | SG_JoystickConfig / SG_ScreenShakeConfig | 2 |
| AI | SG_AI_MoveDown | 1 |
| Level | SG_Level_01 | 1 |
| Wave | SG_Wave_01（4 波，27 怪） | 1 |
| Variable SO | SG_BaseHP / SG_CurrentLevelIndex / SG_CurrentWaveIndex / SG_InputDirection / SG_KillCount / SG_TotalEnemyCount / SG_TotalWaveCount | 7 |
| FairyGUI bytes | SG_Battle / SG_LevelSelect / SG_Loading / SG_Popup + Common | 5 |

### 缺失

| 类型 | 缺失项 | 需新建数量 |
|------|--------|-----------|
| Level | SG_Level_02~05 | 4 |
| Wave | SG_Wave_02~05 | 4 |
| EntityConfig | SG_Enemy_Fast / SG_Enemy_Tank（可选，用于难度递进） | 0~2 |

---

## P4.1 配置资产收口

### 目标
补齐 5 关运行所需全部 SO 资产，确保引用完整、命名统一。

### 任务清单

| # | 任务 | 具体操作 | Done When |
|---|------|---------|-----------|
| 4.1.1 | 创建 SG_Level_02~05 | 右键 Create → ShooterGame/LevelConfig，设置 WaveConfig 引用、BaseHpRatio、UnlockRequirement | 4 个 .asset 存在 + Inspector 引用无空 |
| 4.1.2 | 创建 SG_Wave_02~05 | 复制 SG_Wave_01 → 修改波数/敌人数量/间隔/阵型 | 4 个 .asset 存在 + 每个至少 3 波 |
| 4.1.3 | BattleController 挂载 | Inspector `_levelConfigs` 数组填满 5 个 Level SO（索引 0~4） | 数组长度=5，无 null 槽位 |
| 4.1.4 | 可选：新增敌人类型 | 如需难度递进可新增 SG_Enemy_Fast（速度×1.5，HP×0.5）/ SG_Enemy_Tank（速度×0.5，HP×3） | 编译通过 + Inspector 合理 |
| 4.1.5 | SO 命名一致性扫描 | 所有 SG_ 前缀 + 路径在 `Configs/ShooterGame/` 子目录 | `find SG_*.asset` 无路径异常 |

### 目录结构（目标态）

```
Assets/_Game/Configs/ShooterGame/
├── SG_Player.asset
├── SG_Base.asset
├── SG_Enemy_Normal.asset
├── SG_Enemy_Fast.asset          (可选)
├── SG_Enemy_Tank.asset          (可选)
├── SG_JoystickConfig.asset
├── SG_ScreenShakeConfig.asset
├── AI/
│   └── SG_AI_MoveDown.asset
├── Levels/
│   ├── SG_Level_01.asset        ← WaveConfig → SG_Wave_01
│   ├── SG_Level_02.asset        ← WaveConfig → SG_Wave_02
│   ├── SG_Level_03.asset        ← WaveConfig → SG_Wave_03
│   ├── SG_Level_04.asset        ← WaveConfig → SG_Wave_04
│   └── SG_Level_05.asset        ← WaveConfig → SG_Wave_05
├── Waves/
│   ├── SG_Wave_01.asset         (已有：4 波 / 27 怪)
│   ├── SG_Wave_02.asset
│   ├── SG_Wave_03.asset
│   ├── SG_Wave_04.asset
│   └── SG_Wave_05.asset
└── Variables/
    ├── SG_BaseHP.asset
    ├── SG_CurrentLevelIndex.asset
    ├── SG_CurrentWaveIndex.asset
    ├── SG_InputDirection.asset
    ├── SG_KillCount.asset
    ├── SG_TotalEnemyCount.asset
    └── SG_TotalWaveCount.asset
```

---

## P4.2 五关波次编排

### 目标
5 关有节奏、有递进、能通关，不出现卡死/空关/无限刷怪。

### 设计原则

| 原则 | 说明 |
|------|------|
| 穿越时间锚点 | CameraSize=8 → 可视 9×16 世界单位。敌机从顶部到底线约 4~8 秒（取决于速度） |
| 通关判定 | `EntitySpawner.IsAllWavesCleared`（所有波次发完 + 场上活敌=0） |
| V1 无 Timer 波 | 只用 `AllCleared` 触发下一波（PM-008 铁律） |
| 难度递进 | 递增维度：数量 / 速度 / 密度 / 混编。**不加新 AI / 不加新弹幕** |

### 建议配置方案

| 关卡 | 波数 | 总敌人数 | 难度定位 | 编排要点 |
|------|------|---------|---------|---------|
| Level 1 | 4 | 27 | 🟢 教学 | 已有：Line 5 → Circle 6 → Grid 16 → 混合 12。节奏舒缓 |
| Level 2 | 4 | 35 | 🟡 适应 | 数量略加，间隔缩短。引入 2 组混编波 |
| Level 3 | 5 | 50 | 🟠 挑战 | 加入密集 Grid 波（20+）。最后一波双组同时出 |
| Level 4 | 5 | 65 | 🔴 压力 | 高密度 + 短间隔。可选引入 Fast 敌人 |
| Level 5 | 6 | 80 | 💀 收口 | 最大波 30+ 同屏。最后一波作为 Boss Wave（大量 + Tank） |

### BaseHpRatio 建议

| 关卡 | BaseHpRatio | 说明 |
|------|-------------|------|
| 1 | 1.0 | 满血，容错高 |
| 2 | 1.0 | 保持不变 |
| 3 | 0.8 | 略微降低容错 |
| 4 | 0.7 | 明显施压 |
| 5 | 0.6 | 终局挑战 |

### 验收标准

- [ ] 5 关都能从头打到通关（不卡死）
- [ ] 5 关都能打到失败（基地被摧毁）
- [ ] 难度递进肉眼可感知
- [ ] 无空波 / 无无限刷怪 / 无残留 Entity 导致假未通关
- [ ] HUD 波次文字（Wave X/Y）正确显示

---

## P4.3 全链路集成验收

### 目标
验证"从启动到结束"完整闭环，不只是单点功能。

### 验收路径

#### 路径 A：首次启动 → 通关全 5 关

| 步骤 | 验证内容 | Pass 条件 |
|------|---------|-----------|
| A1 | Boot → Loading | 进度条正常走完 → Loading 淡出 |
| A2 | LevelSelect 初始态 | 仅 Level 1 解锁（白色）；2~5 锁定（灰色） |
| A3 | 点击 Level 1 → Battle | Intro → Playing 正常转换；摇杆可拖动 |
| A4 | 通关 Level 1 | Victory 面板显示 → 正确关卡信息 |
| A5 | 返回 LevelSelect | Level 1 已完成 + Level 2 解锁 |
| A6 | 逐关推进到 Level 5 | 重复 A3~A5 |
| A7 | 全通关后 LevelSelect | 所有 Level 标记完成 |

#### 路径 B：失败 + Retry 回归

| 步骤 | 验证内容 | Pass 条件 |
|------|---------|-----------|
| B1 | 基地被摧毁 → Defeat | Defeat 面板正常弹出 |
| B2 | Retry | 运行时状态干净重置（无翻倍刷怪、无秒败） |
| B3 | Retry 后正常游玩 | 摇杆/射击/波次推进/通关全部正常 |

#### 路径 C：暂停 + 恢复 + 退出

| 步骤 | 验证内容 | Pass 条件 |
|------|---------|-----------|
| C1 | 战斗中点暂停 | Pause 面板弹出 + 游戏冻结 |
| C2 | Resume | 游戏恢复 + 摇杆继续可用 |
| C3 | Quit → LevelSelect | 返回选关 + 无残留逻辑 |
| C4 | 再次进入 Battle | 正常开局（非上一局残留态） |

#### 路径 D：配置一致性检查

| 项目 | 检查内容 | Pass 条件 |
|------|---------|-----------|
| D1 | SO 命名 | 全部 `SG_` 前缀 + 合理子目录 |
| D2 | Inspector 引用 | BattleController `_levelConfigs[5]` 全非 null |
| D3 | FairyGUI 包名 | bytes 文件名 = 包名 = 代码引用名 |
| D4 | Build Settings | Boot + Main + Battle 三场景在列 |
| D5 | ProgressManager 初始化 | totalLevels = _levelConfigs.Length（动态取值不硬编码） |
| D6 | Console | PlayMode 全程无 Error / 无 Warning（预期外的） |

### 阻塞判定

- 任何路径 A/B/C 有 **功能不通** → 阻塞，必须修复后重跑
- 路径 D 发现问题 → 仅修复后标记通过即可，不重跑全路径

---

## P4.4 发布前检查（可选，进入真机前执行）

| # | 检查项 | 操作 | Pass 条件 |
|---|--------|------|-----------|
| 4.4.1 | 无残留 Debug.Log | grep `Debug.Log.*ShooterGame` | 0 条非刻意保留的 |
| 4.4.2 | 无 Editor-Only 引用 | `#if UNITY_EDITOR` 包裹完整 | Game asmdef 无 UnityEditor 引用 |
| 4.4.3 | 资源路径无中文 | `find Assets/_Game -name "*[\u4e00-\u9fff]*"` | 0 条 |
| 4.4.4 | Scene dirty 清理 | 保存所有 Scene | 无 unsaved 标记 |
| 4.4.5 | FairyGUI bytes 最新 | 对比 UIProject 修改时间 vs bytes | bytes 晚于源 XML |

---

## 执行建议

### 推荐顺序

```
P4.1（补资产） → P4.2（编排波次） → P4.3 路径 D（配置检查）
→ P4.3 路径 A（首次通关） → P4.3 路径 B（失败回归）
→ P4.3 路径 C（暂停退出） → P4.4（发布前）
```

### 谁做什么

| 角色 | 负责 |
|------|------|
| Agent | P4.1 资产创建 + P4.2 波次编排 + P4.3-D 配置检查 + P4.4 |
| 天命人 | P4.3 路径 A/B/C 人工验收（需要操作手感判断） |

### 时间估计

| 子阶段 | 估计 | 说明 |
|--------|------|------|
| P4.1 | 30min | Agent 可直接通过 MCP 创建 SO |
| P4.2 | 45min | 主要是设计节奏 + 配数值 |
| P4.3-D | 15min | 自动化检查为主 |
| P4.3 A/B/C | 60min | 天命人手动 PlayMode 验收 |
| P4.4 | 15min | grep + 自动化 |
| **合计** | **~2.5h** | |

---

## 下一步

完成 SG-P4 全部验收后，转入：
- `EC-P3A-DEVICE`（微信小游戏真机验证）
- 或 SG-P5（如有）：美术精修 / 音效接入 / 微信发布

---

## 关联文档

| 文档 | 用途 |
|------|------|
| SG_DEV_PLAN.md | P4 原始定义 + 全局路线图 |
| SG_TDD_INDEX → 01~05 | 技术设计参考 |
| SG_GAME_DESIGN.md | 游戏设计锚点（难度/节奏） |
| SO_WORKFLOWS_INDEX | SO 创建流程参考 |
| SG_P3_ACCEPTANCE_PLAN.md | P3 验收参考（FairyGUI 部分） |
