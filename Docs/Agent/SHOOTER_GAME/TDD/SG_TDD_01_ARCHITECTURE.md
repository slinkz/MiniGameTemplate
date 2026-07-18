---
system: shootergame
scope: architecture
last_verified: 2026-05-06
depends_on: [SG_TDD_INDEX, APPFLOW_TDD]
related_code: Assets/_Game/Scripts/ShooterGame/Core/*.cs, Assets/_Game/Scenes/Main.unity, Assets/_Game/Scenes/Battle.unity, Assets/_Game/Scenes/Transition.unity
---

# SG_TDD_01: 总体架构

> 父文档：[SHOOTER_GAME/TDD/SG_TDD_INDEX.md](SHOOTER_GAME/TDD/SG_TDD_INDEX.md)

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
Boot Scene (一次性启动)
  └── GameBootstrapper (DontDestroyOnLoad)
      ├── 初始化所有 Singleton（AppFlowNavigator / SceneLoader / UIManager / DanmakuSystem）
      └── 驱动 IStartupFlow.RunAsync
  └── GameStartupFlow (DontDestroyOnLoad，在 GameBootstrapper GO 上)
      └── 加载 FairyGUI 包 → Push(Node_MainMenu) → 加载 Main 场景（Single，替换 Boot）

Main Scene (非战斗宿主)
  └── 正交相机 (Size=8)
  └── FairyGUI 面板运行在 GRoot（DontDestroyOnLoad）上：MainMenu / LevelSelect

Transition Scene (空过渡)
  └── 无业务对象；SceneLoader 在卸载最后一个业务场景前临时切入

Battle Scene (战斗)
  └── BattleController (MonoBehaviour)
      ├── Awake(): 读取 SG_CurrentLevelIndex → 加载对应 SG_LevelConfigSO
      ├── Start(): 创建子系统 → 进入 Intro 状态
      ├── Update(): 按 BattleState 驱动逻辑
      └── OnDestroy(): 取消事件订阅 + DanmakuSystem.ClearAll()
  └── EntitySystemBootstrap (框架)
      ├── Awake(): 创建 EntityManager/Spawner/CollisionSolver/HitReactionHandler
      ├── Update(): 驱动 Entity 系统 Tick
      └── OnDestroy(): 注销 EntityManagerAccessor + 清理所有活跃 Entity
  └── UI Controllers (MonoBehaviour)
      ├── 各 Controller.Start(): CreateObject 挂 GRoot
      └── 各 Controller.OnDestroy(): Dispose() 清理 FairyGUI 对象（防止 GRoot 残留）
```

---

## 4. 场景与界面关系

```
┌─ DontDestroyOnLoad (常驻) ────────────────────┐
│  GameBootstrapper (Singleton 宿主)             │
│  ├── AppFlowNavigator                        │
│  ├── SceneLoader                             │
│  ├── UIManager                               │
│  └── GameStartupFlow                         │
│  DanmakuSystem (Singleton)                    │
│  FairyGUI GRoot + Stage (UI 根)               │
└───────────────────────────────────────────────┘

┌─ Boot.unity (仅启动时短暂存在) ──────────────┐
│  GameBootstrapper GO → 初始化后进入常驻层       │
│  LoadingScreenController → Loading 完成后 Dispose │
└───────────────────────────────────────────────┘
        │ Push(Node_MainMenu) → LoadScene(SD_Main, Single) → Boot 被替换
        ▼
┌─ Main.unity (非战斗宿主) ────────────────────┐
│  Main Camera (Orthographic, Size=8)           │
│  UI 面板运行在 GRoot 上：                      │
│  ├── MainMenuPanel（通过 UIManager）          │
│  └── LevelSelectScreen（通过 UIManager）      │
└───────────────────────────────────────────────┘
        │ Push(Node_Battle) → LoadScene(SD_Battle, Single) → Main 被替换
        ▼
┌─ Battle.unity (战斗) ────────────────────────┐
│  BattleController (MonoBehaviour)             │
│  BattleSceneBootstrapper (MonoBehaviour)      │
│  EntitySystemBootstrap (MonoBehaviour)        │
│  CameraShaker (MonoBehaviour)                │
│  SG_PlayerInputBridge (MonoBehaviour)         │
│  Main Camera (Orthographic, Size=8)           │
│  UI Controllers (MonoBehaviour, 面板挂 GRoot)：│
│  ├── BattleHUDController                     │
│  ├── JoystickController                      │
│  ├── PausePanelController                    │
│  ├── VictoryPanelController                  │
│  └── DefeatPanelController                   │
└───────────────────────────────────────────────┘
        │ Pop() → LoadScene(Transition, Single) → 释放 Battle handle → LoadScene(SD_Main, Single)
        ▼
        回到 Main.unity
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
| 业务对象不用 DontDestroyOnLoad | 场景卸载即清理（框架 Singleton 除外） | 架构铁律 |
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
├── GameBootstrapper (DontDestroyOnLoad)
│   ├── GameStartupFlow (IStartupFlow)
│   ├── AppFlowNavigator (Singleton)
│   ├── SceneLoader (Singleton)
│   └── UIManager (Singleton)
├── FairyGUI GRoot (UI 根，自动创建，DontDestroyOnLoad)
└── 启动完成后 → Push(Node_MainMenu) → 加载 Main.unity (Single) → Boot 被替换

Main.unity
├── Main Camera (Orthographic, Size=8)
└── （纯场景壳，UI 面板运行在 GRoot 上由 UIManager 管理）

Battle.unity
├── BattleController / EntitySystemBootstrap / CameraShaker 等
└── UI Controllers 在 Start() 中将面板挂到 GRoot

Transition.unity
└── 空过渡场景，避免卸载最后一个已加载业务场景时触发 Unity warning
```
