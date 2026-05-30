# SG_TOOLS_TDD_02: 调试工具与 Gizmo

> 父文档：[SG_TOOLS_TDD_INDEX.md](SG_TOOLS_TDD_INDEX.md)

---

## 1. Debug 字段与 ProfilerMarker（P0）

### 1.1 需求

在关键 Controller 中添加 `#if UNITY_EDITOR` 保护的调试字段，零运行时开销。

### 1.2 涉及类

| 类 | Debug 字段 | 用途 |
|----|-----------|------|
| BattleHUDController | `Debug_DisplayHP`, `Debug_TargetHP`, `Debug_ActiveFloatingTextCount` | 血条预损状态可视化 |
| BattleController | `Debug_CurrentState`, `Debug_StateTimer`, `Debug_AliveEnemyCount` | 战斗状态一目了然 |
| BaseLineDetector | — | 纯 C#，无 Inspector（通过 BattleStateWindow 监视） |

### 1.3 ProfilerMarker 插桩位

```csharp
#if UNITY_EDITOR
using Unity.Profiling;
#endif

public class BattleController : MonoBehaviour
{
#if UNITY_EDITOR
    private static readonly ProfilerMarker s_TickPlayingMarker = 
        new ProfilerMarker("SG.BattleController.TickPlaying");
    private static readonly ProfilerMarker s_BaseLineDetectMarker = 
        new ProfilerMarker("SG.BaseLineDetector.Tick");
#endif

    private void TickPlaying(float dt)
    {
#if UNITY_EDITOR
        using (s_TickPlayingMarker.Auto())
#endif
        {
            // ... 战斗逻辑
        }
    }
}
```

> **注意**：ProfilerMarker 在非 Editor 构建中自动被 strip，零运行时开销。但为清晰起见仍用 `#if` 包裹。

---

## 2. Debug MenuItem（P0）

### 2.1 5 个快捷命令

```csharp
namespace Game.ShooterGame.Editor
{
    using UnityEditor;
    using UnityEngine;
    using MiniGameTemplate.Data;
    using EnumCamp = Danmaku.EnumCamp;  // AT-009: 集中别名，命名空间迁移只改一处
    
    // AT-004: 公共编辑器工具方法（FindSOByName 等复用）
    // PT-003 假设边界：项目 SO 规模 < 100 时性能无忧。
    // 注意 FindAssets 是子串匹配——"SG_BaseHP" 也会匹配 "SG_BaseHP_v2"。
    // V1 靠命名唯一性保证（21 个 SO 无重名前缀），V2 如需精确匹配改用 GUID 查找。
    public static class SG_EditorUtility
    {
        public static T FindSOByName<T>(string name) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name} {name}");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
    
    public static class SG_DebugMenuItems
    {
        private const string MENU_ROOT = "Tools/SG/Debug/";
        
        // AT-007: 菜单路径常量化，执行方法和 Validate 使用同一 const
        private const string MENU_RETRY = MENU_ROOT + "重试当前关卡 %&R";
        private const string MENU_VICTORY = MENU_ROOT + "直接胜利";
        private const string MENU_DEFEAT = MENU_ROOT + "直接失败";
        private const string MENU_SKIP_WAVE = MENU_ROOT + "跳到下一波";
        private const string MENU_SET_HP = MENU_ROOT + "设置基地HP为50%";
        
        /// <summary>强制重试当前关卡</summary>
        [MenuItem(MENU_RETRY)]
        private static void ForceRetry()
        {
            var bc = Object.FindObjectOfType<BattleController>();
            if (bc == null) { Debug.LogWarning("[SG Debug] 未找到 BattleController"); return; }
            bc.RetryBattle();
        }
        
        /// <summary>直接判定胜利</summary>
        [MenuItem(MENU_VICTORY)]
        private static void ForceVictory()
        {
            var bc = Object.FindObjectOfType<BattleController>();
            if (bc == null) { Debug.LogWarning("[SG Debug] 未找到 BattleController"); return; }
            bc.DebugForceVictory();
        }
        
        /// <summary>直接判定失败</summary>
        [MenuItem(MENU_DEFEAT)]
        private static void ForceDefeat()
        {
            var bc = Object.FindObjectOfType<BattleController>();
            if (bc == null) { Debug.LogWarning("[SG Debug] 未找到 BattleController"); return; }
            bc.DebugForceDefeat();
        }
        
        /// <summary>跳到下一波（秒杀全部敌机 → AllCleared 自动推进）</summary>
        [MenuItem(MENU_ROOT + "跳到下一波")]
        private static void SkipToNextWave()
        {
            // 方案：秒杀场上全部敌方 Entity，利用框架 AllCleared 机制自动推进下一波
            // AT-001: 走 DamageDealer 正式管线（重入保护 + PendingDespawn 安全检查）
            var mgr = EntityManagerAccessor.Instance;
            if (mgr == null) { Debug.LogWarning("[SG Debug] EntityManager 未初始化"); return; }
            
            var entities = mgr.ActiveEntities;
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                var entity = entities[i];
                if (entity.Camp == EnumCamp.Enemy && !entity.IsPendingDespawn)
                {
                    // 通过 DamageDealer 走正式伤害管线（不绕过安全检查）
                    DamageDealer.ApplyDamage(entity, 99999, default);
                }
            }
            Debug.Log("[SG Debug] 已秒杀全部敌机，等待下一波推进");
        }
        
        /// <summary>设置基地 HP</summary>
        [MenuItem(MENU_SET_HP)]
        private static void SetBaseHP50()
        {
            var baseHP = SG_EditorUtility.FindSOByName<FloatVariable>("SG_BaseHP");
            if (baseHP != null) baseHP.SetValue(0.5f);
        }
        
        // ── Validate（仅 Play Mode 可用）── AT-007: 使用 const 路径
        
        [MenuItem(MENU_RETRY, true)]
        [MenuItem(MENU_VICTORY, true)]
        [MenuItem(MENU_DEFEAT, true)]
        [MenuItem(MENU_SKIP_WAVE, true)]
        [MenuItem(MENU_SET_HP, true)]
        private static bool ValidatePlayMode()
        {
            return Application.isPlaying;
        }
    }
}
```

### 2.2 BattleController 需暴露的调试接口

```csharp
// 在 BattleController 中添加（#if UNITY_EDITOR 包裹）
#if UNITY_EDITOR
    public void DebugForceVictory() => EnterState(BattleState.Victory);
    public void DebugForceDefeat() => EnterState(BattleState.Defeat);
#endif
```

### 2.3 EntitySpawner 调试说明

> **PK ST-002 决策**：不在框架层 EntitySpawner 添加 DebugSkipToNextWave()。
> 原因：ActiveSpawnState 是 private struct，外部无法直接操作。
> 替代方案：Game 层秒杀全部敌机（TakeDamage(99999)），利用框架 AllCleared 机制自动推进。
> 这样零框架改动，且行为更接近真实玩家"快速通关"的效果。

---

## 3. BaseLineY Gizmo（P0）

### 3.1 实现位置

在 `BattleController` 的 `OnDrawGizmos()` 中绘制（不需要独立 GizmoDrawer）。

```csharp
// BattleController.cs 中（追加到 §1.2 已有字段的类定义中）
// AT-003 注意：_baseLineY 字段已在核心 TDD_02 §1.2 声明，此处不重复声明
// 以下仅为 OnDrawGizmos 方法的追加定义
#if UNITY_EDITOR
    private EntitySystemBootstrap _cachedBootstrap;  // AT-008: 缓存引用
    
    private void OnDrawGizmos()
    {
        // 底线红色横线（动态取 KillBounds X 范围）
        // AT-008: 缓存 bootstrap 引用，null 时重新查找
        if (_cachedBootstrap == null)
            _cachedBootstrap = FindObjectOfType<EntitySystemBootstrap>();
        
        float xMin = -6f, xMax = 6f;
        if (_cachedBootstrap != null)
        {
            xMin = _cachedBootstrap.KillBounds.xMin;
            xMax = _cachedBootstrap.KillBounds.xMax;
        }
        
        Gizmos.color = Color.red;
        Vector3 left = new Vector3(xMin, _baseLineY, 0f);
        Vector3 right = new Vector3(xMax, _baseLineY, 0f);
        Gizmos.DrawLine(left, right);
        
        // 标签
        UnityEditor.Handles.Label(
            new Vector3(xMin, _baseLineY + 0.3f, 0f), 
            $"BaseLine Y={_baseLineY:F1}", 
            new GUIStyle { normal = { textColor = Color.red }, fontSize = 10 });
    }
#endif
```

> **PK ST-007 修正**：X 范围从 EntitySystemBootstrap.KillBounds 动态获取，
> 不硬编码 ±6。CameraSize 或 KillBounds 修改后 Gizmo 自动适配。

---

## 4. 战斗状态监视 EditorWindow（P1）

### 4.1 需求

集中显示所有 `SG_*` SO 变量的实时值，无需逐个点击 SO 资产查看。

### 4.2 类设计

```csharp
namespace Game.ShooterGame.Editor
{
    using UnityEditor;
    using UnityEngine;
    using MiniGameTemplate.Data;
    using EnumCamp = Danmaku.EnumCamp;  // AT-009: 别名
    
    public class SG_BattleStateWindow : EditorWindow
    {
        [MenuItem("Tools/SG/战斗状态面板")]
        public static void ShowWindow()
        {
            GetWindow<SG_BattleStateWindow>("SG 战斗状态");
        }
        
        // SO 引用缓存
        private FloatVariable _baseHP;
        private IntVariable _currentWaveIndex;
        private IntVariable _totalWaveCount;
        private IntVariable _killCount;
        private IntVariable _totalEnemyCount;
        private IntVariable _currentLevelIndex;
        private BattleController _cachedBattleController;  // ★ 缓存引用
        private double _nextRepaintTime;  // AT-002: 定时刷新
        private const double REPAINT_INTERVAL = 0.1;  // 100ms
        
        private void OnEnable()
        {
            CacheSOs();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnEditorUpdate;  // AT-002: 定时刷新
        }
        
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
        }
        
        // AT-002: 定时刷新替代每帧 Repaint，O(n) 遍历只在 Repaint 帧执行
        private void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup >= _nextRepaintTime)
            {
                _nextRepaintTime = EditorApplication.timeSinceStartup + REPAINT_INTERVAL;
                Repaint();
            }
        }
        
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            // 进入/退出 Play Mode 时刷新缓存
            _cachedBattleController = null;
            if (state == PlayModeStateChange.EnteredPlayMode)
                CacheSOs();
        }
        
        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅 Play Mode 可用", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("🎮 战斗状态", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            
            // 战斗状态（缓存引用，避免每帧 FindObjectOfType）
            if (_cachedBattleController == null)
                _cachedBattleController = FindObjectOfType<BattleController>();
            if (_cachedBattleController != null)
            {
                EditorGUILayout.LabelField("战斗状态", _cachedBattleController.CurrentState.ToString());
            }
            
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("📊 SO 变量实时值", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            
            DrawSOField("关卡索引", _currentLevelIndex);
            DrawSOField("基地 HP", _baseHP, "F2");
            DrawSOField("当前波次", _currentWaveIndex);
            DrawSOField("总波次", _totalWaveCount);
            DrawSOField("击杀数", _killCount);
            DrawSOField("总敌机数", _totalEnemyCount);
            
            EditorGUILayout.Space(8);
            
            // Entity 统计
            var mgr = EntityManagerAccessor.Instance;
            if (mgr != null)
            {
                EditorGUILayout.LabelField("📦 Entity 统计", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("活跃 Entity", mgr.ActiveEntities.Count.ToString());
                
                // AT-010: 拆分为非子弹 Entity 和子弹
                // PT-008 V1 假设：子弹 = 无 HealthComponent 的非中立 Entity。
                // V2 如有 HP 子弹（可被打掉的弹幕）需改用 EntityTag 判断。
                int enemyCount = 0, allyCount = 0, bulletCount = 0;
                for (int i = 0; i < mgr.ActiveEntities.Count; i++)
                {
                    var e = mgr.ActiveEntities[i];
                    // 子弹 Entity 通常没有 HealthComponent 或有特殊标记
                    bool isBullet = e.GetComponent(ComponentType.Health) == null 
                                    && e.Camp != EnumCamp.Neutral;
                    if (isBullet)
                    {
                        bulletCount++;
                    }
                    else if (e.Camp == EnumCamp.Enemy) enemyCount++;
                    else if (e.Camp == EnumCamp.Player || e.Camp == EnumCamp.Ally) allyCount++;
                }
                EditorGUILayout.LabelField("  敌方单位", enemyCount.ToString());
                EditorGUILayout.LabelField("  友方单位", allyCount.ToString());
                EditorGUILayout.LabelField("  子弹", bulletCount.ToString());
            }
            
            // AT-002: 不在此处调用 Repaint()——由 OnEditorUpdate 以 0.1s 间隔定时刷新
        }
        
        private void DrawSOField(string label, FloatVariable so, string format = "F1")
        {
            float val = so != null ? so.Value : 0f;
            EditorGUILayout.LabelField(label, val.ToString(format));
        }
        
        private void DrawSOField(string label, IntVariable so)
        {
            int val = so != null ? so.Value : 0;
            EditorGUILayout.LabelField(label, val.ToString());
        }
        
        private void CacheSOs()
        {
            // AT-004: 使用公共工具方法
            _baseHP = SG_EditorUtility.FindSOByName<FloatVariable>("SG_BaseHP");
            _currentWaveIndex = SG_EditorUtility.FindSOByName<IntVariable>("SG_CurrentWaveIndex");
            _totalWaveCount = SG_EditorUtility.FindSOByName<IntVariable>("SG_TotalWaveCount");
            _killCount = SG_EditorUtility.FindSOByName<IntVariable>("SG_KillCount");
            _totalEnemyCount = SG_EditorUtility.FindSOByName<IntVariable>("SG_TotalEnemyCount");
            _currentLevelIndex = SG_EditorUtility.FindSOByName<IntVariable>("SG_CurrentLevelIndex");
        }
    }
}
```

---

## 5. FairyGUI 包校验 MenuItem（P2 — Backlog）

> **PK ST-006 降级**：FairyGUI 包尚未制作，package.xml 路径结构未确认。
> 等 FairyGUI 包实际制作后再编写校验器，避免白费功夫。
> V1 不实施，保留设计供 V2 参考。
>
> **PT-005 注意**：以下代码为**参考实现草案**，V2 时需根据实际 FairyGUI XML 结构重新适配。
> V1 开发者请**跳过此段**，不要花时间实现。

### 5.1 需求

自动校验 FairyGUI 发布输出的 `package.xml`，检查设计走查清单中标记 ✅ 的项目。

### 5.2 V1 校验项

| # | 走查项 | 校验逻辑 |
|---|--------|---------|
| 1 | 所有按钮有三态资源 | 解析 `package.xml`，找 Button 类型组件，检查 `pages` 数量 ≥ 3 |
| 3 | 摇杆组件存在 | 检查 Battle 包中名为 "Joystick" 的组件存在 |
| 4 | 关卡节点有三态 Controller | 检查 LevelSelect 包中 "LevelNode" 的 Controller page 数量 ≥ 3 |

### 5.3 类设计

```csharp
namespace Game.ShooterGame.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Xml;
    
    public static class SG_FairyGUIValidator
    {
        [MenuItem("Tools/SG/校验 FairyGUI 包")]
        private static void ValidatePackages()
        {
            string[] packagePaths = {
                "Assets/_Game/FairyGUI/Common",
                "Assets/_Game/FairyGUI/Loading",
                "Assets/_Game/FairyGUI/LevelSelect",
                "Assets/_Game/FairyGUI/Battle",
                "Assets/_Game/FairyGUI/Popup",
            };
            
            int errors = 0;
            int warnings = 0;
            
            foreach (string path in packagePaths)
            {
                string xmlPath = $"{path}/package.xml";
                if (!System.IO.File.Exists(xmlPath))
                {
                    Debug.LogWarning($"[SG Validate] 包路径不存在: {xmlPath}");
                    warnings++;
                    continue;
                }
                
                var doc = new XmlDocument();
                doc.Load(xmlPath);
                
                // 校验按钮三态
                ValidateButtonStates(doc, path, ref errors);
            }
            
            // 校验特定组件存在
            ValidateComponentExists("Battle", "Joystick", ref errors);
            ValidateComponentExists("LevelSelect", "LevelNode", ref errors);
            
            if (errors == 0)
                Debug.Log($"[SG Validate] ✅ 全部通过（{warnings} 个警告）");
            else
                Debug.LogError($"[SG Validate] ❌ {errors} 个错误, {warnings} 个警告");
        }
        
        private static void ValidateButtonStates(XmlDocument doc, string pkgPath, ref int errors)
        {
            // 遍历所有 Button 类型组件，检查 pages 数量
            var nodes = doc.SelectNodes("//component[@extention='Button']");
            if (nodes == null) return;
            
            foreach (XmlNode node in nodes)
            {
                string name = node.Attributes?["name"]?.Value ?? "unknown";
                var pages = node.SelectNodes(".//controller/pages");
                // 简化检查：按钮应有 down/over/disabled 至少 3 页
                // 实际 XML 结构需要根据 FairyGUI 版本调整
                Debug.Log($"[SG Validate] 检查按钮 {name} @ {pkgPath}");
            }
        }
        
        private static void ValidateComponentExists(string pkg, string comp, ref int errors)
        {
            // 检查 FairyGUI 发布目录中对应组件文件是否存在
            string path = $"Assets/_Game/FairyGUI/{pkg}/{comp}.xml";
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError($"[SG Validate] ❌ 缺少组件: {pkg}/{comp}");
                errors++;
            }
        }
    }
}
```

> **注意**：FairyGUI package.xml 的实际 XML 结构需要在制作 FairyGUI 包后确认，上述代码为框架性实现，具体 XPath 需适配。

---

## 6. 飘字坐标 Debug.DrawLine（P1）

### 6.1 实现位置

`BattleHUDController` 中。

```csharp
#if UNITY_EDITOR
    public void ShowFloatingText(Vector3 worldPos, string text, Color color)
    {
        // ... 正常飘字逻辑 ...
        
        // Debug: 画世界坐标→屏幕坐标映射线
        Debug.DrawLine(worldPos, worldPos + Vector3.up * 0.5f, Color.yellow, 0.8f);
    }
#endif
```

---

## 7. 摇杆 Gizmo 叠加（P1）

### 7.1 需求

在 Game View 中可视化摇杆死区和最大半径。

### 7.2 实现方案

由于 FairyGUI 摇杆在 UI 层，无法用 Unity Gizmo（世界坐标）直接绘制。

**替代方案**：在 `JoystickController` 中用 `OnGUI()` 绘制 Debug 圈。

```csharp
#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!_isActive) return;
        
        // V1 只绘制方向线段（圆需要 GL.Draw，复杂度高，降为 P2）
        Vector2 dir = _inputDirection.Value;
        if (dir.sqrMagnitude > 0.001f)
        {
            // 方向线段（绿色）：从触摸原点到当前方向 * 最大半径
            Vector2 endPoint = _touchOrigin + dir * _config.MaxRadius;
            
            // 用 GUI.DrawTexture 画线（Texture2D 1x1 pixel 拉伸）
            var lineColor = Color.green;
            DrawGUILine(_touchOrigin, endPoint, lineColor, 2f);
        }
        
        // 死区和最大半径圆（P2 实现，需要 GL.PushMatrix + GL.LINES）
        // TODO P2: DrawDebugCircle(_touchOrigin, _config.DeadZone, Color.blue);
        // TODO P2: DrawDebugCircle(_touchOrigin, _config.MaxRadius, Color.white);
    }
    
    private static void DrawGUILine(Vector2 from, Vector2 to, Color color, float width)
    {
        var savedColor = GUI.color;
        GUI.color = color;
        
        Vector2 delta = to - from;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float length = delta.magnitude;
        
        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, length, width), Texture2D.whiteTexture);
        GUIUtility.RotateAroundPivot(-angle, from);
        
        GUI.color = savedColor;
    }
#endif
```

> **PK ST-009 修正**：V1 只实现方向线段（DrawGUILine），死区/最大半径圆降为 P2（需 GL.Draw）。

---

## 7.5 EntityGizmoDrawer 矩形碰撞体可视化（v2.8 新增，2026-05-30）

### 7.5.1 需求

Entity 碰撞体新增 `HitboxShape.Rect` 类型后，Scene View 中的 Gizmo 需支持矩形 AABB 线框绘制，而非仅圆形。

### 7.5.2 实现位置

`Assets/_Framework/Editor/Entity/EntityGizmoDrawer.cs`（`[InitializeOnLoad]` 静态类，编辑器 Gizmo 回调）。

**关键实现**：
- 根据 `EntityConfigSO.HitboxType` 分支绘制：`Circle` → `Handles.DrawWireDisc()`；`Rect` → `Handles.DrawPolyLine(corners[])`
- 矩形顶点数组 `_rectCorners` 缓存为 `static readonly Vector3[5]`，避免每帧每实体 GC 分配
- `labelOffsetY` 根据碰撞体形状动态计算（圆=Radius+0.2f，矩形=HalfHeight+0.2f）

### 7.5.3 性能考量

| 项 | 优化前 | 优化后 |
|---|--------|--------|
| 矩形顶点分配 | 每帧每实体 `new Vector3[5]`（120B GC） | 静态数组填充，0 GC |
| 圆形绘制 | 无变化 | 无变化 |

---

## 8. 验收标准汇总

| # | 工具 | 验收项 |
|---|------|--------|
| 1 | Debug 字段 | Play Mode 中 Inspector 显示 _displayHP 等调试值 |
| 2 | Debug 字段 | Build 后无残留（#if UNITY_EDITOR） |
| 3 | MenuItem 重试 | Play Mode 中 Ctrl+Alt+R 触发重试 |
| 4 | MenuItem 胜利 | 点击后立即进入 Victory 状态 |
| 5 | MenuItem 失败 | 点击后立即进入 Defeat 状态 |
| 6 | MenuItem 跳波 | 点击后推进到下一波次——验证方式：打开战斗状态面板，确认 SG_CurrentWaveIndex +1（PT-004：注意可能有 1 帧延迟） |
| 7 | MenuItem 设HP | 点击后基地 HP 变为 50% |
| 8 | MenuItem 灰显 | **PT-002 细化步骤**：退出 Play Mode → 打开 Tools/SG/Debug 菜单 → 确认 5 项（重试/胜利/失败/跳波/设HP）全部灰显不可点击 |
| 9 | BaseLineY Gizmo | Scene View 中显示红色横线 + Y 坐标标注 |
| 10 | 战斗状态面板 | Play Mode 中实时显示所有 SO 值 + Entity 统计（敌方/友方/子弹分开） |
| 11 | FairyGUI 校验 | P2 backlog——V1 不验收 |
| 12 | Rect Gizmo | Scene View 中 HitboxType=Rect 的 Entity 显示矩形 AABB 线框（非圆圈），静态缓存 corners 零 GC |

---

## 9. 已知限制

| 限制 | 原因 | 解决时机 |
|------|------|---------|
| 摇杆 Gizmo 用 OnGUI 而非 Gizmo | FairyGUI UI 坐标系无法直接用 Gizmo | V2 考虑 SceneView overlay |
| FairyGUI 校验依赖 XML 结构 | 需 FairyGUI 包制作后适配 XPath | 制作 FairyGUI 包时 |
| ~~战斗状态面板每帧 Repaint~~ | ~~可能影响编辑器性能~~ | ~~AT-002 已修复：0.1s 定时刷新~~ |
