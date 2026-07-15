# SG_TDD_03: 关卡与存储系统

> 父文档：[SHOOTER_GAME/TDD/SG_TDD_INDEX.md](SHOOTER_GAME/TDD/SG_TDD_INDEX.md)  
> **版本**：v1.1 | 微信真机 PK 修正（WX-001~008）

---

## 1. SG_LevelConfigSO（关卡元数据）

### 1.0 索引语义约定

> **全局铁律**：
> - **内部索引 = 0-based**：`SG_CurrentLevelIndex` SO 变量、`_levelConfigs[]` 数组、BattleController 内部逻辑
> - **外部显示 = 1-based**：`SG_ProgressManager` 接口参数、UI 显示文字（"第 1 关"）
> - **转换公式**：`displayLevel = internalIndex + 1`，`internalIndex = displayLevel - 1`
> - 每处转换必须加注释 `// 0-based → 1-based` 或反之

### 1.1 类设计

```csharp
namespace Game.ShooterGame
{
    /// <summary>
    /// 游戏级关卡配置——不污染框架 SO。
    /// 每关一个资产，由 BattleController._levelConfigs[] 索引。
    /// </summary>
    [CreateAssetMenu(menuName = "ShooterGame/LevelConfig")]
    public class SG_LevelConfigSO : ScriptableObject
    {
        [Tooltip("本关波次配置")]
        public EntitySpawnWaveSO WaveConfig;
        
        [Tooltip("基地初始 HP 比例（0~1），1.0 = 满血")]
        [Range(0.1f, 1.0f)]
        public float BaseHpRatio = 1.0f;
        
        [Tooltip("前一关需要几星解锁（V1 = 0，通关即解锁）")]
        public int UnlockRequirement = 0;
        
        [Tooltip("基地底线 Y 坐标（覆盖全局默认值，-1 = 使用全局）")]
        public float BaseLineYOverride = -1f;
    }
}
```

### 1.2 五关配置参考值

| 资产 | WaveConfig | BaseHpRatio | BaseLineY | 设计定位 |
|------|-----------|-------------|-----------|---------|
| SG_Level_01 | SG_Wave_01 | 1.0 | 全局默认 | 教学关 |
| SG_Level_02 | SG_Wave_02 | 1.0 | 全局默认 | 热身 |
| SG_Level_03 | SG_Wave_03 | 0.9 | 全局默认 | 上强度 |
| SG_Level_04 | SG_Wave_04 | 0.8 | 全局默认 | 高压 |
| SG_Level_05 | SG_Wave_05 | 0.8 | 全局默认 | 终局 |

---

## 2. 存储系统集成（SG_ProgressManager）

### 2.0 生命周期与创建者

> **创建者**：Boot 场景的 `GameStartupFlow.Awake()` 创建 `SG_ProgressManager` 实例。
> **ISaveSystem 来源**：框架层 `SaveSystemFactory.Create()` → 微信小游戏返回 `WxSaveSystem`，Editor 返回 `PlayerPrefsSaveSystem`。
> **跨场景共享**：`SG_ProgressManager` 实例存储在 `GameStartupFlow` 的静态字段 `public static SG_ProgressManager Progress { get; private set; }`。
> Battle 场景通过 `GameStartupFlow.Progress` 访问，无需 DontDestroyOnLoad。
> **生命周期**：Boot 场景加载时创建 → 整个游戏会话期间不销毁（静态引用持有）。

#### 2.0.1 微信小游戏热启动处理（WX-005）

> **问题**：微信小游戏用户切后台再回来时，Unity WebGL 实例可能保留旧内存状态（热启动），导致静态字段中的 `_data` 与实际 storage 不一致。
> **方案**：`SG_Boot.InitProgress()` 在每次 Boot 场景加载时调用 `Progress.Reload()`，强制从 storage 重新加载。
> ```csharp
> public static void InitProgress()
> {
>     if (Progress == null)
>         Progress = new SG_ProgressManager(SaveSystemFactory.Create());
>     else
>         Progress.Reload();  // WX-005: 热启动时重载
> }
> ```

#### 2.0.2 V1 已知限制与 V2 升级路径（WX-002）

> **V1 已知限制**：
> - 进度存储纯本地（`wx.setStorageSync`），**用户换设备或清除小游戏数据后进度丢失**
> - 无用户登录体系，无跨设备同步
>
> **V2 升级路径**（用户量 >1000 DAU 时优先实施）：
> 1. `wx.login()` 静默登录获取 `code` → 服务端换取 `openid`
> 2. 服务端最简 CRUD：`GET /progress/{openid}` + `PUT /progress/{openid}`
> 3. 本地 storage 作为读缓存 + 离线写缓冲
> 4. `SG_ProgressManager` 新增 `SyncToCloud()` 接口（通关时调用）
> 5. 冲突策略：服务端进度 ∪ 本地进度（取并集，不丢失任何通关记录）
>
> **V2 不阻塞 V1 编码**：`ISaveSystem` 接口不变，仅 `WxSaveSystem` 内部升级为"写本地+异步上云"。

### 2.1 存储数据格式

```json
{
    "version": 1,
    "clearedLevels": [1, 2]
}
```

- **Key**：`"sg_progress"`
- **版本字段**：预留后续数据迁移

### 2.2 类设计

```csharp
namespace Game.ShooterGame
{
    /// <summary>
    /// ShooterGame 进度管理——封装 ISaveSystem 读写。
    /// 纯 C# 类（无 MonoBehaviour）。
    /// </summary>
    public class SG_ProgressManager
    {
        private const string SAVE_KEY = "sg_progress";
        private const int CURRENT_VERSION = 1;
        
        private readonly ISaveSystem _saveSystem;
        private readonly int _totalLevels;  // WX-010: 外部注入，避免硬编码
        private ProgressData _data;
        
        [System.Serializable]
        private class ProgressData
        {
            public int version = CURRENT_VERSION;
            public List<int> clearedLevels = new List<int>();
        }
        
        /// <param name="totalLevels">总关卡数（从 _levelConfigs.Length 获取）</param>
        public SG_ProgressManager(ISaveSystem saveSystem, int totalLevels = 5)
        {
            _saveSystem = saveSystem;
            _totalLevels = totalLevels;
            Load();
        }
        
        // ── 查询 ──
        
        /// <summary>指定关卡是否已通关（1-based）</summary>
        public bool IsLevelCleared(int levelIndex)
        {
            return _data.clearedLevels.Contains(levelIndex);
        }
        
        /// <summary>指定关卡是否可进入</summary>
        public bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex <= 1) return true;  // 第一关始终解锁
            return IsLevelCleared(levelIndex - 1);  // 前一关通关即解锁
        }
        
        /// <summary>最大已解锁关卡（1-based）</summary>
        /// <param name="totalLevels">总关卡数（由调用方从 _levelConfigs.Length 获取）</param>
        public int MaxUnlockedLevel(int totalLevels)
        {
            int max = 1;
            for (int i = 1; i <= totalLevels; i++)
            {
                if (IsLevelUnlocked(i)) max = i;
            }
            return max;
        }
        
        // ── 写入 ──
        
        /// <summary>标记关卡通关并持久化。返回 false 表示存储失败。</summary>
        public bool MarkLevelCleared(int levelIndex)
        {
            if (!_data.clearedLevels.Contains(levelIndex))
            {
                _data.clearedLevels.Add(levelIndex);
                return Save();
            }
            return true;
        }
        
        // ── 内部方法 ──
        
        private void Load()
        {
            string json = _saveSystem.LoadString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                _data = new ProgressData();
                return;
            }
            
            try
            {
                _data = JsonUtility.FromJson<ProgressData>(json);
                // 版本迁移预留
                if (_data.version < CURRENT_VERSION)
                    MigrateData(_data);
                // WX-004: 加载后校验数据合法性
                ValidateData();
            }
            catch
            {
                Debug.LogWarning("[SG_ProgressManager] 存档数据损坏，重置");
                _data = new ProgressData();
            }
        }
        
        /// <summary>WX-001: 存储失败安全处理</summary>
        private bool Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_data);
                _saveSystem.SaveString(SAVE_KEY, json);
                _saveSystem.FlushIfDirty();
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SG_ProgressManager] 存储失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>WX-004: 过滤非法数据（防篡改/损坏）</summary>
        private void ValidateData()
        {
            _data.clearedLevels.RemoveAll(lv => lv < 1 || lv > _totalLevels);
            if (_data.version < 1) _data.version = CURRENT_VERSION;
        }
        
        private void MigrateData(ProgressData data)
        {
            // V1→V2 迁移逻辑预留
            // WX-007 迁移规范：
            // - 字段只追加不删除
            // - 迁移失败 → 保留旧数据但 version 不升级（下次重试）
            // - version < 1 视为损坏 → ValidateData() 强制修正为 CURRENT_VERSION
            data.version = CURRENT_VERSION;
        }
        
        /// <summary>
        /// WX-005: 热启动时强制重新从 storage 加载（应对微信后台恢复场景）。
        /// ⚠️ 仅在非战斗状态下调用（Boot 场景）。战斗中调用会覆盖内存中的临时状态。
        /// </summary>
        public void Reload()
        {
            Load();
        }
        
        /// <summary>清除所有进度（调试用）</summary>
        public void ResetAll()
        {
            _data = new ProgressData();
            Save();
        }
    }
}
```

### 2.3 存储时机

| 事件 | 操作 |
|------|------|
| 通关时 | `MarkLevelCleared(levelIndex)` → 自动持久化（同步写入） |
| 失败时 | 不写入 |
| 退出关卡（暂停→返回） | 不写入 |
| 应用暂停/退出 | `ISaveSystem.FlushIfDirty()`（⚠️ 见下方微信注意事项） |

#### 2.3.1 微信小游戏存储注意事项（WX-003/WX-006/WX-008）

> **WX-003 同步 vs 异步**：
> - V1 `WxSaveSystem` 使用 `wx.setStorageSync`（同步版本）
> - V1 数据量极小（<100 字节），同步写入耗时 <1ms，可接受
> - V2 如果 `ProgressData` 扩展到 >1KB，考虑切换为 `wx.setStorage`（异步）+ 回调确认
>
> **WX-006 场景切换时序安全**：
> - `Save()` 内部 `FlushIfDirty()` = 立即持久化（`wx.setStorageSync` 同步保证）
> - `HandleVictoryConfirm` 中 `MarkLevelCleared` → `Save()` → 立即可靠写入 → 随后 `LoadScene` 安全
> - **铁律**：`Save()` 是同步操作，返回后数据已落盘，不存在"还没写完就切场景"的风险
>
> **WX-008 OnApplicationPause/Quit 不可靠**：
> - ⚠️ 微信小游戏环境中 `OnApplicationPause(true)` 在 iOS 上不保证触发
> - ⚠️ `OnApplicationQuit()` 在微信小游戏中几乎永远不触发
> - **结论**：V1 所有关键数据在操作时立即持久化（`Save()` 在 `MarkLevelCleared` 中调用），不依赖暂停/退出时 flush
> - 存储时机表中的"应用暂停/退出"仅为**额外安全网**，非唯一保证
>
> **WX-001 存储失败处理**：
> - `Save()` 返回 `bool`，失败时 `MarkLevelCleared` 返回 `false`
> - 调用方（BattleController）在 `HandleVictoryConfirm` 中检查返回值：
>   - 成功：正常流程
>   - 失败：Toast "进度保存失败，请检查存储空间" + 仍然返回选关界面（内存中进度保留，本次会话有效）

---

## 3. 关卡解锁数据流

```
[选关界面]
  │
  ├── LevelSelectController.OnShow()
  │     for i = 1..5:
  │       if ProgressManager.IsLevelCleared(i)  → 节点设为"已通关 ★"
  │       elif ProgressManager.IsLevelUnlocked(i) → 节点设为"可进入 ▶"
  │       else → 节点设为"锁定 🔒"
  │
  ├── 点击可进入的关卡
  │     → SG_CurrentLevelIndex.SetValue(clickedIndex)
  │     → SceneManager.LoadScene("Battle")
  │
  └── 战斗胜利 → VictoryPanelController
        → ProgressManager.MarkLevelCleared(currentLevel)
        → 返回选关界面
        → 新解锁关卡播放"锁打开"动效
```

---

## 4. 基地 Entity 初始化

### 4.1 SpawnBase 流程

```csharp
private void SpawnBase()
{
    // 使用 EntityConfigSO "SG_Base" 配置 Spawn
    var mgr = EntityManagerAccessor.Instance;
    _baseEntity = mgr.Spawn(
        _baseEntityConfig,           // EntityConfigSO 引用
        new Vector2(0, _baseLineY),  // 底线位置
        0f                           // rotation（基地无朝向）
    );
    _baseEntity.Camp = EnumCamp.Ally;  // Camp 通过字段单独设置
    
    // 设置初始 HP（按关卡 BaseHpRatio 缩放）
    var health = _baseEntity.GetComponent(ComponentType.Health) as HealthComponent;
    if (health != null)
    {
        int maxHp = _baseEntityConfig.MaxHp;
        int initialHp = Mathf.RoundToInt(maxHp * _currentLevel.BaseHpRatio);
        health.SetHp(initialHp);
    }
}
```

### 4.2 SpawnPlayer 流程

```csharp
private void SpawnPlayer()
{
    var mgr = EntityManagerAccessor.Instance;
    _playerEntity = mgr.Spawn(
        _playerEntityConfig,            // EntityConfigSO 引用
        new Vector2(0, -5f),            // 屏幕下方偏上
        270f                            // 朝上（竖版飞机默认朝上）
    );
    _playerEntity.Camp = EnumCamp.Player;  // Camp 通过字段单独设置
    
    // 玩家飞机不需要额外设置——MovementComponent 由 SG_PlayerInputBridge 驱动
}
```

---

## 5. 波次事件桥接

### 5.1 波次索引更新

```csharp
// BattleController 自维护波次计数
// 原因：EntitySpawner.CurrentWaveIndex 是内部 struct 的私有字段，不对外公开
// 且 Spawner 管理多个刷怪点，不存在全局唯一波次索引
// 方案：利用 OnDespawned 事件统计敌机全灭次数 → 推进波次计数
private int _displayWaveIndex = 0;  // UI 显示用（1-based）

/// <summary>
/// 波次推进检测：当所有当前波次的敌机全灭时，视为进入下一波。
/// 在 TickPlaying 中调用。简化方案适用于 ShooterGame 单刷怪点场景。
/// </summary>
private void UpdateWaveIndex()
{
    // 利用 Spawner.IsAllWavesCleared 判定最终波完成
    // 中间波次推进：统计 ActiveEntities 中敌方数量变化
    int aliveEnemies = CountAliveEnemies();
    
    // 当存活敌机归零且 Spawner 未完成所有波次 → 波次推进了
    if (aliveEnemies == 0 && !EntityManagerAccessor.Spawner.IsAllWavesCleared
        && _displayWaveIndex < _totalWaveCount.Value)
    {
        _displayWaveIndex++;
        _currentWaveIndex.SetValue(_displayWaveIndex);
    }
}

private int CountAliveEnemies()
{
    int count = 0;
    var entities = EntityManagerAccessor.Instance.ActiveEntities;
    for (int i = 0; i < entities.Count; i++)
    {
        if (entities[i].Camp == EnumCamp.Enemy && !entities[i].IsPendingDespawn)
            count++;
    }
    return count;
}
```

> **~~待确认~~** → **PM-008 已确认，V1 铁律**：
> **V1 五关全部使用 AllCleared 推进模式，不使用 Timer 波。**
> 此方案在 AllCleared 模式下准确无误。
> V2 如需 Timer 波，需先给 EntitySpawner 补公开 CurrentWaveIndex 接口。

---

## 6. 行为契约

| ID | 契约 | 验证方式 |
|----|------|---------|
| SG-BC-01 | 基地 HP 唯一扣减来源 = BaseLineDetector | 编码审查 |
| SG-BC-02 | 存储仅在通关时写入 | 代码路径分析 |
| SG-BC-03 | 关卡索引始终 Clamp 到合法范围 | Awake 中 Clamp |
| SG-BC-04 | 重试不重载场景 | RetryBattle 不调用 LoadScene |
| SG-BC-05 | ProgressData.version 始终 ≥ 1 | 构造函数 + ValidateData() |
| SG-BC-06 | Save() 失败不导致崩溃 | try-catch + 返回 bool（WX-001） |
| SG-BC-07 | 加载后非法数据被过滤 | ValidateData() 清理越界值（WX-004） |
| SG-BC-08 | 热启动后进度与 storage 一致 | Boot 时 Reload()（WX-005） |
| SG-BC-09 | V1 不依赖 OnApplicationPause/Quit 做关键持久化 | 代码路径分析（WX-008） |
