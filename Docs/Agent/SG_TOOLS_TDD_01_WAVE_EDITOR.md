# SG_TOOLS_TDD_01: EntitySpawnWaveSO 编辑器增强

> 父文档：[SG_TOOLS_TDD_INDEX.md](SG_TOOLS_TDD_INDEX.md)

---

## 1. 需求来源

| GDD 章节 | 需求 |
|----------|------|
| §11.1 #1 | 一键复制最后一波（深拷贝 + delay 自动递增） |
| §11.1 #2 | 总敌机/总时长统计面板 |
| §5.3 | V1 用 Inspector 编辑替代时间线编辑器 |

---

## 2. SG_SpawnWaveSOEditor

### 2.1 设计决策

- **继承框架已有的 CustomEditor**：框架层已有 `EntitySpawnWaveSOEditor`（含波次摘要面板），Game 层通过继承扩展，调用 `base.OnInspectorGUI()` 保留框架功能
- 文件放 `Assets/_Game/Editor/ShooterGame/`（Game 层编辑器目录）
- **关键**：Unity 对同一类型只生效一个 CustomEditor，继承方案确保框架摘要不被覆盖

### 2.2 类设计

```csharp
namespace Game.ShooterGame.Editor
{
    using UnityEditor;
    using UnityEngine;
    using MiniGameTemplate.Entity;
    using MiniGameTemplate.EditorTools;  // 框架层 EntitySpawnWaveSOEditor
    
    [CustomEditor(typeof(EntitySpawnWaveSO))]
    public class SG_SpawnWaveSOEditor : EntitySpawnWaveSOEditor  // ★ 继承框架 Editor
    {
        private EntitySpawnWaveSO _target;
        
        private void OnEnable()
        {
            base.OnEnable();  // AT-005: 调用基类初始化（缓存 SerializedProperty 等）
            _target = (EntitySpawnWaveSO)target;
        }
        
        public override void OnInspectorGUI()
        {
            // 1. ShooterGame 统计面板（置顶）
            DrawStatisticsPanel();
            
            EditorGUILayout.Space(8);
            
            // 2. 调用 base → 框架层摘要面板 + DrawDefaultInspector
            base.OnInspectorGUI();
            
            EditorGUILayout.Space(4);
            
            // 3. 一键复制按钮（Waves 列表底部）
            DrawCopyLastWaveButton();
        }
    }
}
```

> **PK ST-001 修正**：继承 `EntitySpawnWaveSOEditor` 而非 `UnityEditor.Editor`，
> 调用 `base.OnInspectorGUI()` 保留框架的波次摘要面板。

---

## 3. 统计面板

### 3.1 行为规格

| 显示项 | 计算方式 | 格式 |
|--------|---------|------|
| 总波次数 | `Waves.Length` | "共 5 波" |
| 总敌机数 | ΣΣ Group.Count | "共 42 架敌机" |
| 预估总时长 | 仅统计 Timer 模式波次：Σ(TriggerDelay + max group duration) | "预估 ≥ 45.2 秒（仅 Timer 波）" |

> **PK ST-005 修正**：AllCleared/OnCallback 模式的时长无法预估（取决于玩家行为/外部回调），
> 面板只统计 Timer 波次的确定性时长，并在显示中标注"仅 Timer 波"。
> 如全部波次为 AllCleared，显示"不可预估（全部为 AllCleared 模式）"。

### 3.2 实现

```csharp
private void DrawStatisticsPanel()
{
    if (_target.Waves == null || _target.Waves.Length == 0)
    {
        EditorGUILayout.HelpBox("暂无波次数据", MessageType.Info);
        return;
    }
    
    // 计算统计数据
    int totalWaves = _target.Waves.Length;
    int totalEnemies = 0;
    float totalDuration = 0f;
    int timerWaveCount = 0;
    
    for (int w = 0; w < _target.Waves.Length; w++)
    {
        var wave = _target.Waves[w];
        
        // 只统计 Timer 模式波次的时长（AllCleared/OnCallback 无法预估）
        if (wave.TriggerMode == WaveTriggerMode.Timer)
        {
            totalDuration += wave.TriggerDelay;
            timerWaveCount++;
        }
        
        if (wave.Groups == null) continue;
        
        float maxGroupDuration = 0f;
        for (int g = 0; g < wave.Groups.Length; g++)
        {
            var group = wave.Groups[g];
            totalEnemies += group.Count;
            
            float groupDuration = group.Count > 1 
                ? (group.Count - 1) * group.SpawnInterval 
                : 0f;
            maxGroupDuration = Mathf.Max(maxGroupDuration, groupDuration);
        }
        if (wave.TriggerMode == WaveTriggerMode.Timer)
            totalDuration += maxGroupDuration;
    }
    
    // 绘制面板
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("📊 波次统计", EditorStyles.boldLabel);
    
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField($"共 {totalWaves} 波", GUILayout.Width(80));
    EditorGUILayout.LabelField($"共 {totalEnemies} 架敌机", GUILayout.Width(120));
    
    // ST-005: 标注仅 Timer 波
    if (timerWaveCount == 0)
        EditorGUILayout.LabelField("时长不可预估（全部为 AllCleared/OnCallback）");
    else if (timerWaveCount < totalWaves)
        EditorGUILayout.LabelField($"预估 ≥ {totalDuration:F1} 秒（仅 {timerWaveCount} 波 Timer）");
    else
        EditorGUILayout.LabelField($"预估 {totalDuration:F1} 秒");
    
    EditorGUILayout.EndHorizontal();
    
    EditorGUILayout.EndVertical();
}
```

---

## 4. 一键复制最后一波

### 4.1 行为规格

| 步骤 | 行为 |
|------|------|
| 1 | 深拷贝 `Waves[last]` 所有字段（Groups 数组也要深拷贝） |
| 2 | 新波次 `TriggerDelay` = 源波次 TriggerDelay + 源波次预估时长 + 3s |
| 3 | 追加到 `Waves[]` 末尾 |
| 4 | 标记 SO 为 Dirty（`EditorUtility.SetDirty`） |
| 5 | 注册 Undo（`Undo.RecordObject`） |

### 4.2 实现

```csharp
private void DrawCopyLastWaveButton()
{
    if (_target.Waves == null || _target.Waves.Length == 0) return;
    
    EditorGUILayout.Space(4);
    
    if (GUILayout.Button("+ 复制最后一波", GUILayout.Height(28)))
    {
        CopyLastWave();
    }
}

private void CopyLastWave()
{
    Undo.RecordObject(_target, "复制最后一波");
    
    var lastWave = _target.Waves[_target.Waves.Length - 1];
    
    // 深拷贝
    var newWave = new SpawnWaveEntry
    {
        TriggerMode = lastWave.TriggerMode,
        TriggerDelay = CalculateNewDelay(lastWave),
        Groups = DeepCopyGroups(lastWave.Groups),
    };
    
    // 追加
    var newWaves = new SpawnWaveEntry[_target.Waves.Length + 1];
    System.Array.Copy(_target.Waves, newWaves, _target.Waves.Length);
    newWaves[newWaves.Length - 1] = newWave;
    _target.Waves = newWaves;
    
    EditorUtility.SetDirty(_target);
    Debug.Log($"[SG] 已复制波次 → 共 {newWaves.Length} 波");
}

private float CalculateNewDelay(SpawnWaveEntry sourceWave)
{
    // ST-008: AllCleared/OnCallback 模式下 TriggerDelay 不生效，保持源值不自动递增
    if (sourceWave.TriggerMode != WaveTriggerMode.Timer)
        return sourceWave.TriggerDelay;
    
    // Timer 模式：源 Delay + 源预估时长 + 3s
    float waveDuration = 0f;
    if (sourceWave.Groups != null)
    {
        for (int g = 0; g < sourceWave.Groups.Length; g++)
        {
            var grp = sourceWave.Groups[g];
            float d = grp.Count > 1 ? (grp.Count - 1) * grp.SpawnInterval : 0f;
            waveDuration = Mathf.Max(waveDuration, d);
        }
    }
    return sourceWave.TriggerDelay + waveDuration + 3f;
}

private SpawnGroup[] DeepCopyGroups(SpawnGroup[] source)
{
    if (source == null) return null;
    
    // AT-006: 当框架 SpawnGroup 新增字段时，此处必须同步更新！
    // 替代方案：用 JsonUtility.ToJson/FromJson 自动深拷贝所有 Serializable 字段：
    //   string json = JsonUtility.ToJson(new SpawnGroupWrapper { groups = source });
    //   return JsonUtility.FromJson<SpawnGroupWrapper>(json).groups;
    // V1 使用显式拷贝（性能更好，字段少时可控）
    
    var copy = new SpawnGroup[source.Length];
    for (int i = 0; i < source.Length; i++)
    {
        copy[i] = new SpawnGroup
        {
            EntityConfig = source[i].EntityConfig,  // SO 引用浅拷贝（正确）
            Camp = source[i].Camp,
            Count = source[i].Count,
            SpawnInterval = source[i].SpawnInterval,
            Formation = source[i].Formation,
        };
    }
    return copy;
}
```

---

## 5. 验收标准

| # | 验收项 | 预期结果 |
|---|--------|---------|
| 1 | 选中 EntitySpawnWaveSO 资产 | Inspector 顶部显示统计面板 |
| 2 | 统计面板数据 | 波次数/敌机总数/预估时长 与手动计算一致 |
| 3 | 点击"+ 复制最后一波" | Waves 数组长度 +1 |
| 4 | 新波次 Groups | 与源波次内容相同（深拷贝验证：修改新波次第一个 Group.Count+1 → 确认源波次 Count 未变 → Ctrl+Z 撤销） |
| 5 | 新波次 TriggerDelay | PT-006 修正：AllCleared 模式 = 源 TriggerDelay（不递增）；Timer 模式 = 源 Delay + 源时长 + 3s |
| 6 | Ctrl+Z 撤销 | 复制操作完整撤销 |
| 7 | 空 Waves 时 | 统计面板显示提示，复制按钮不显示 |

---

## 6. 已知限制（V1 接受）

- 复制后不自动折叠旧波次（SerializedProperty 控制折叠 API 复杂，V1 不做）
- 统计面板不实时刷新（修改字段后需点击其他地方触发 Repaint）
- 不做波次可视化预览（V2 时间线编辑器负责）
