# SG_TDD_01: 总体架构

> 父文档：[SG_TDD_INDEX.md](SG_TDD_INDEX.md)

---

## 1. 目录结构

```
Assets/_Game/Scripts/ShooterGame/
├── Core/
│   ├── BattleController.cs          ← 战斗编排指挥（唯一入口）
│   ├── BattleState.cs               ← 状态枚举
│   ├── BaseLineDetector.cs          ← 底线检测
│   └── CameraShaker.cs             ← 屏幕震动
├── Config/
│   ├── SG_LevelConfigSO.cs         ← 关卡元数据
│   ├── ScreenShakeConfigSO.cs      ← 震动参数
│   └── JoystickConfigSO.cs         ← 摇杆参数
├── Progress/
│   └── SG_ProgressManager.cs       ← 存档管理
├── Input/
│   ├── SG_PlayerInputBridge.cs     ← 摇杆→Entity 桥接
│   └── JoystickController.cs       ← 摇杆逻辑（FairyGUI）
└── UI/
    ├── LoadingScreenController.cs
    ├── LevelSelectController.cs
    ├── BattleHUDController.cs
    ├── PausePanelController.cs
    ├── VictoryPanelController.cs
    └── DefeatPanelController.cs

Assets/_Game/Configs/ShooterGame/
├── SG_Player.asset
├── SG_Base.asset
├── SG_Enemy_Normal.asset
├── SG_Enemy_Fast.asset
├── SG_PlayerBullet_Straight.asset
├── SG_ScreenShake_Default.asset
├── Levels/
│   └── SG_Level_01 ~ 05.asset
├── Waves/
│   └── SG_Wave_01 ~ 05.asset
└── Variables/
    ├── SG_BaseHP.asset
    ├── SG_CurrentLevelIndex.asset
    ├── SG_CurrentWaveIndex.asset
    ├── SG_TotalWaveCount.asset
    ├── SG_KillCount.asset
    ├── SG_TotalEnemyCount.asset
    └── SG_InputDirection.asset

Assets/_Framework/DataSystem/Scripts/Variables/
└── Vector2Variable.cs              ← 框架层新增
```

---

## 2. 命名空间与程序集

| 程序集 (asmdef) | 命名空间 | 引用 |
|----------------|---------|------|
| Game.ShooterGame | `Game.ShooterGame` | MiniGameTemplate.Entity, MiniGameTemplate.Data, MiniGameTemplate.Pool |
| Game.ShooterGame.UI | `Game.ShooterGame.UI` | Game.ShooterGame, FairyGUI |
| Game.ShooterGame.Editor | `Game.ShooterGame.Editor` | Game.ShooterGame, UnityEditor |

> **asmdef 边界原则**：Game 层单向依赖框架层，框架层不知道 Game 层存在。

---

## 3. 核心生命周期（场景级）

```
Boot Scene (MainMenu)
  └── GameStartupFlow → 加载 FairyGUI 包 → 显示 LoadingScreen → 完成后显示 LevelSelect

Battle Scene
  └── BattleController (MonoBehaviour)
      ├── Awake(): 读取 SG_CurrentLevelIndex → 加载对应 SG_LevelConfigSO
      ├── Start(): 创建子系统 → 进入 Intro 状态
      ├── Update(): 按 BattleState 驱动逻辑
      └── OnDestroy(): 清理

  └── EntitySystemBootstrap (框架)
      ├── Awake(): 创建 EntityManager/Spawner/CollisionSolver/HitReactionHandler
      └── Update(): 驱动 Entity 系统 Tick
```

---

## 4. 场景与界面关系

```
┌─ Boot.unity ─────────────────────────────────┐
│  GameStartupFlow (MonoBehaviour)              │
│  FairyGUI GRoot (UI 根)                       │
│  ├── LoadingScreenController                  │
│  └── LevelSelectController                   │
└───────────────────────────────────────────────┘
        │ 选关 → SceneManager.LoadScene("Battle")
        ▼
┌─ Battle.unity ───────────────────────────────┐
│  BattleController (MonoBehaviour)             │
│  EntitySystemBootstrap (MonoBehaviour)        │
│  CameraShaker (MonoBehaviour)                │
│  SG_PlayerInputBridge (MonoBehaviour)         │
│  Main Camera (Orthographic, Size=8)           │
│  FairyGUI GRoot (UI 根)                       │
│  ├── BattleHUDController                     │
│  ├── JoystickController                      │
│  ├── PausePanelController                    │
│  ├── VictoryPanelController                  │
│  └── DefeatPanelController                   │
└───────────────────────────────────────────────┘
```

---

## 5. 数据流图

```
[SO 变量层]
  SG_InputDirection ←── JoystickController (每帧写入)
  SG_BaseHP         ←── BaseLineDetector (基地受伤时写入 ratio)
  SG_CurrentWaveIndex ←── EntitySpawner (波次推进时写入)
  SG_TotalWaveCount ←── BattleController.Init (启动时写入)
  SG_KillCount      ←── BattleController (敌机死亡时递增)
  SG_TotalEnemyCount ←── BattleController.Init (计算总敌机数)
  SG_CurrentLevelIndex ←── LevelSelectController (选关时写入)

[消费者]
  SG_InputDirection ──→ SG_PlayerInputBridge → MovementComponent.SetMoveDirection
  SG_BaseHP         ──→ BattleHUDController (血条更新)
  SG_CurrentWaveIndex ──→ BattleHUDController ("Wave 2/5")
  SG_KillCount      ──→ VictoryPanelController / DefeatPanelController
  SG_CurrentLevelIndex ──→ BattleController (加载哪一关)
```

---

## 6. 关键约束

| 约束 | 值 | 来源 |
|------|-----|------|
| 单帧 GC Alloc | 0 (战斗稳态) | 框架铁律 |
| 同屏 Entity 上限 | 40 | GDD §4.2 |
| SO 变量不可存场景引用 | 强制 | 项目铁律 |
| TimeScale=0 暂停 | FairyGUI Tween 自然冻结 | UI §8.1 |
| 不使用 DontDestroyOnLoad | 场景卸载即清理 | 架构铁律 |
| MonoBehaviour ≤ 150 行 | SRP 铁律 | 架构规范 |

---

## 7. 错误处理策略

| 场景 | 处理 |
|------|------|
| SG_CurrentLevelIndex 越界 | `Clamp(0, configs.Length-1)` + `Debug.LogWarning` |
| EntityConfigSO 引用丢失 | 框架 EntityConfigValidator 在编辑器阶段拦截 |
| FairyGUI 包加载失败 | LoadingScreen 停留 + Error Log |
| SaveSystem 写入失败 | `try/catch` + 游戏继续（不存档不阻断游戏） |

---

## 8. 与 Entity 系统集成点

| 集成点 | Game 层做什么 | 框架层提供什么 |
|--------|-------------|---------------|
| 战斗启动 | BattleController 配置 SpawnPoint + 设置 BaseHP | EntitySystemBootstrap 自动 Tick |
| 底线检测 | BaseLineDetector 每帧扫描敌方 Entity.Position.y | EntityManager.ActiveEntities |
| 通关判定 | 检查 EntitySpawner.IsAllWavesCleared | EntitySpawner 状态 |
| 击杀计数 | 订阅 Entity.OnDeath 事件 | EntityEventBus |
| 玩家移动 | SG_PlayerInputBridge 读 SO 写 MovementComponent | MovementComponent.SetMoveDirection |
| 屏幕震动 | BattleController 触发 CameraShaker | EntityCollisionSolver 碰撞回调 |

---

## 9. GameStartupFlow（Boot 场景入口）

### 9.1 职责

Boot 场景唯一入口 MonoBehaviour——负责初始化全局系统并驱动 Loading→LevelSelect 流程。

### 9.2 类设计

```csharp
namespace Game.ShooterGame
{
    /// <summary>
    /// Boot 场景入口——初始化全局系统，驱动 Loading→LevelSelect 流程。
    /// PM-004: 补充骨架级设计，确保开发者有明确着手点。
    /// </summary>
    public class GameStartupFlow : MonoBehaviour
    {
        [Header("UI Controllers（Inspector 拖拽）")]
        [SerializeField] private LoadingScreenController _loadingScreen;
        [SerializeField] private LevelSelectController _levelSelect;
        
        /// <summary>跨场景访问进度管理器（静态引用，不用 DontDestroyOnLoad）</summary>
        public static SG_ProgressManager Progress { get; private set; }
        
        private void Awake()
        {
            // 1. 创建存储系统
            var saveSystem = SaveSystemFactory.Create();  // 微信→WxSaveSystem / Editor→PlayerPrefs
            
            // 2. 创建进度管理器
            Progress = new SG_ProgressManager(saveSystem);
        }
        
        private IEnumerator Start()
        {
            // 3. 显示 Loading 界面
            _loadingScreen.Show();
            
            // 4. 加载 FairyGUI 包（按需异步）
            yield return LoadFairyGUIPackages();
            
            // 5. Loading 完成→切换到选关
            _loadingScreen.Hide();
            _levelSelect.Init(Progress);
            _levelSelect.Show();
        }
        
        private IEnumerator LoadFairyGUIPackages()
        {
            // 按序加载 4 个 FairyGUI 包：Loading → LevelSelect → Battle → Popup
            string[] packages = { "Loading", "LevelSelect", "Battle", "Popup" };
            for (int i = 0; i < packages.Length; i++)
            {
                UIPackage.AddPackage($"FairyGUI/{packages[i]}");
                _loadingScreen.SetProgress((float)(i + 1) / packages.Length);
                yield return null;  // 分帧避免卡顿
            }
        }
    }
}
```

### 9.3 场景布局

```
Boot.unity
├── GameStartupFlow (MonoBehaviour)
│   ├── _loadingScreen → LoadingScreenController (同 GO 或子 GO)
│   └── _levelSelect → LevelSelectController (同 GO 或子 GO)
├── FairyGUI GRoot (UI 根，自动创建)
└── EventSystem (如需)
```
