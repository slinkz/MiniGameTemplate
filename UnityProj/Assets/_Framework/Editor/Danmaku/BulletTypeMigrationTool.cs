// ────────────────────────────────────────────────────────────
// BulletTypeMigrationTool.cs — 统一资源描述迁移器
//
// 功能：将 BulletTypeSO / VFXTypeSO 从旧字段格式迁移到 Phase 1.4/1.6
//       统一资源描述（SourceTexture + UVRect + SchemaVersion）。
//
// 工作流：dry-run → apply → report（遵循 REFACTOR_PLAN 迁移器契约）
//   dry-run：扫描所有 SO 资产 + prefab/scene 引用，输出待迁移清单
//   apply  ：只处理通过预检的资产，执行迁移并标脏
//   report ：输出 Markdown 归档报告到 Assets/../Docs/Agent/
//
// 变更日志：
//   2026-04-12  初版——Phase 1 统一资源描述迁移
// ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.VFX;

namespace MiniGameTemplate.Editor.Danmaku
{
    public class BulletTypeMigrationTool : EditorWindow
    {
        // ──── 常量 ────

        private const int CURRENT_BULLET_SCHEMA = 1;
        private const int CURRENT_VFX_SCHEMA = 1;
        private const string REPORT_DIR = "Docs/Agent/";
        private const string REPORT_FILENAME = "MIGRATION_REPORT_Phase1.md";

        // ──── 数据模型 ────

        private enum MigrationStatus { Pending, NeedsMigration, UpToDate, Error }

        [Serializable]
        private class AssetEntry
        {
            public string AssetPath;
            public string AssetName;
            public string TypeName; // "BulletTypeSO" or "VFXTypeSO"
            public int CurrentSchema;
            public int TargetSchema;
            public MigrationStatus Status;
            public List<string> Issues = new();
            public List<string> Warnings = new();
            public bool HasSourceTexture;
            public bool HasValidUVRect;
        }

        [Serializable]
        private class PrefabSceneRef
        {
            public string FilePath;
            public string FileType; // "Prefab" or "Scene"
            public List<string> ReferencedSOPaths = new();
        }

        // ──── 状态 ────

        private List<AssetEntry> _bulletEntries = new();
        private List<AssetEntry> _vfxEntries = new();
        private List<PrefabSceneRef> _prefabSceneRefs = new();
        private bool _dryRunComplete;
        private bool _applyComplete;
        private string _lastReportPath;
        private Vector2 _scrollPos;
        private int _migratedCount;
        private int _skippedCount;
        private int _errorCount;

        // ──── 窗口入口 ────

        [MenuItem("Tools/弹幕系统/资源描述迁移工具", priority = 100)]
        public static void ShowWindow()
        {
            var win = GetWindow<BulletTypeMigrationTool>("资源描述迁移器");
            win.minSize = new Vector2(600, 400);
        }

        // ──── GUI ────

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            GUILayout.Label("BulletTypeSO / VFXTypeSO 统一资源描述迁移工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "工作流：\n" +
                "1. Dry-Run — 扫描所有 SO 资产和 Prefab/Scene 引用\n" +
                "2. Apply — 对通过预检的资产执行迁移\n" +
                "3. Report — 生成 Markdown 归档报告",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // ── Step 1: Dry-Run ──
            using (new EditorGUI.DisabledGroupScope(_applyComplete))
            {
                if (GUILayout.Button("1. Dry-Run（预检扫描）", GUILayout.Height(30)))
                {
                    RunDryRun();
                }
            }

            if (_dryRunComplete)
            {
                DrawDryRunSummary();

                EditorGUILayout.Space(4);

                // ── Step 2: Apply ──
                using (new EditorGUI.DisabledGroupScope(_applyComplete || HasBlockingErrors()))
                {
                    var applyStyle = new GUIStyle(GUI.skin.button);
                    if (HasBlockingErrors())
                    {
                        EditorGUILayout.HelpBox("存在阻断错误，无法执行 Apply。请先修复上述问题。", MessageType.Error);
                    }

                    if (GUILayout.Button("2. Apply（执行迁移）", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog("确认迁移",
                            $"即将迁移 {CountNeedsMigration()} 个资产。\n\n此操作支持 Ctrl+Z 撤销。\n继续？",
                            "执行", "取消"))
                        {
                            RunApply();
                        }
                    }
                }

                if (_applyComplete)
                {
                    DrawApplySummary();

                    EditorGUILayout.Space(4);

                    // ── Step 3: Report ──
                    if (GUILayout.Button("3. Report（生成报告）", GUILayout.Height(30)))
                    {
                        GenerateReport();
                    }

                    if (!string.IsNullOrEmpty(_lastReportPath))
                    {
                        EditorGUILayout.HelpBox($"报告已生成：{_lastReportPath}", MessageType.Info);
                        if (GUILayout.Button("在资源管理器中显示"))
                        {
                            EditorUtility.RevealInFinder(_lastReportPath);
                        }
                    }
                }
            }

            EditorGUILayout.Space(8);

            // ── 详情列表 ──
            if (_dryRunComplete)
            {
                DrawDetailList();
            }
        }

        // ════════════════════════════════════════════════════
        // DRY-RUN
        // ════════════════════════════════════════════════════

        private void RunDryRun()
        {
            _bulletEntries.Clear();
            _vfxEntries.Clear();
            _prefabSceneRefs.Clear();
            _dryRunComplete = false;
            _applyComplete = false;
            _lastReportPath = null;
            _migratedCount = 0;
            _skippedCount = 0;
            _errorCount = 0;

            try
            {
                // ── 扫描 BulletTypeSO ──
                var bulletGuids = AssetDatabase.FindAssets("t:BulletTypeSO");
                for (int i = 0; i < bulletGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(bulletGuids[i]);
                    EditorUtility.DisplayProgressBar("Dry-Run: BulletTypeSO",
                        path, (float)i / bulletGuids.Length);

                    var so = AssetDatabase.LoadAssetAtPath<BulletTypeSO>(path);
                    if (so == null) continue;

                    var entry = new AssetEntry
                    {
                        AssetPath = path,
                        AssetName = so.name,
                        TypeName = "BulletTypeSO",
                        CurrentSchema = so.SchemaVersion,
                        TargetSchema = CURRENT_BULLET_SCHEMA,
                        HasSourceTexture = so.SourceTexture != null,
                        HasValidUVRect = so.UVRect.width > 0 && so.UVRect.height > 0
                    };

                    ValidateBulletEntry(so, entry);
                    _bulletEntries.Add(entry);
                }

                // ── 扫描 VFXTypeSO ──
                var vfxGuids = AssetDatabase.FindAssets("t:VFXTypeSO");
                for (int i = 0; i < vfxGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(vfxGuids[i]);
                    EditorUtility.DisplayProgressBar("Dry-Run: VFXTypeSO",
                        path, (float)i / vfxGuids.Length);

                    var so = AssetDatabase.LoadAssetAtPath<VFXTypeSO>(path);
                    if (so == null) continue;

                    var entry = new AssetEntry
                    {
                        AssetPath = path,
                        AssetName = so.name,
                        TypeName = "VFXTypeSO",
                        CurrentSchema = so.SchemaVersion,
                        TargetSchema = CURRENT_VFX_SCHEMA,
                        HasSourceTexture = so.SourceTexture != null,
                        HasValidUVRect = so.UVRect.width > 0 && so.UVRect.height > 0
                    };

                    ValidateVFXEntry(so, entry);
                    _vfxEntries.Add(entry);
                }

                // ── 扫描 Prefab/Scene 引用 ──
                ScanPrefabSceneReferences();

                _dryRunComplete = true;
                Debug.Log($"[迁移器] Dry-Run 完成：{_bulletEntries.Count} BulletTypeSO + {_vfxEntries.Count} VFXTypeSO, " +
                          $"{_prefabSceneRefs.Count} Prefab/Scene 引用");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ValidateBulletEntry(BulletTypeSO so, AssetEntry entry)
        {
            // SchemaVersion 检查
            if (so.SchemaVersion >= CURRENT_BULLET_SCHEMA)
            {
                entry.Status = MigrationStatus.UpToDate;
                return;
            }

            entry.Status = MigrationStatus.NeedsMigration;

            // 资源完整性检查
            if (so.SourceTexture == null)
            {
                entry.Warnings.Add("SourceTexture 为空——迁移后需手动指定贴图");
            }

            // UVRect 合理性
            if (so.UVRect.width <= 0 || so.UVRect.height <= 0)
            {
                entry.Issues.Add("[阻断] UVRect 无效（宽/高 ≤ 0）");
                entry.Status = MigrationStatus.Error;
            }

            // SpriteSheet 配置合理性
            if (so.SamplingMode == BulletSamplingMode.SpriteSheet)
            {
                if (so.SheetColumns <= 0 || so.SheetRows <= 0)
                {
                    entry.Issues.Add("[阻断] SpriteSheet 列/行数 ≤ 0");
                    entry.Status = MigrationStatus.Error;
                }
                if (so.SheetTotalFrames <= 0)
                {
                    entry.Issues.Add("[阻断] SheetTotalFrames ≤ 0");
                    entry.Status = MigrationStatus.Error;
                }
                if (so.SheetTotalFrames > so.SheetColumns * so.SheetRows)
                {
                    entry.Warnings.Add($"SheetTotalFrames({so.SheetTotalFrames}) > Columns×Rows({so.SheetColumns * so.SheetRows})，将被自动钳位");
                }
            }

            // 非法组合检测
            if (so.SamplingMode == BulletSamplingMode.Static &&
                so.PlaybackMode != BulletPlaybackMode.StretchToLifetime)
            {
                entry.Warnings.Add($"Static + {so.PlaybackMode} 是非法组合，将重置 PlaybackMode 为 StretchToLifetime");
            }
        }

        private void ValidateVFXEntry(VFXTypeSO so, AssetEntry entry)
        {
            if (so.SchemaVersion >= CURRENT_VFX_SCHEMA)
            {
                entry.Status = MigrationStatus.UpToDate;
                return;
            }

            entry.Status = MigrationStatus.NeedsMigration;

            if (so.SourceTexture == null)
            {
                entry.Warnings.Add("SourceTexture 为空——迁移后需手动指定贴图");
            }

            if (so.UVRect.width <= 0 || so.UVRect.height <= 0)
            {
                entry.Issues.Add("[阻断] UVRect 无效（宽/高 ≤ 0）");
                entry.Status = MigrationStatus.Error;
            }

            if (so.Columns <= 0 || so.Rows <= 0)
            {
                entry.Issues.Add("[阻断] Columns/Rows ≤ 0");
                entry.Status = MigrationStatus.Error;
            }
        }

        private void ScanPrefabSceneReferences()
        {
            // 收集所有 SO 的 GUID
            var soGuids = new HashSet<string>();
            foreach (var e in _bulletEntries)
            {
                var guid = AssetDatabase.AssetPathToGUID(e.AssetPath);
                if (!string.IsNullOrEmpty(guid)) soGuids.Add(guid);
            }
            foreach (var e in _vfxEntries)
            {
                var guid = AssetDatabase.AssetPathToGUID(e.AssetPath);
                if (!string.IsNullOrEmpty(guid)) soGuids.Add(guid);
            }

            if (soGuids.Count == 0) return;

            // 扫描 Prefab
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                EditorUtility.DisplayProgressBar("扫描 Prefab 引用",
                    path, (float)i / prefabGuids.Length);

                var deps = AssetDatabase.GetDependencies(path, false);
                var referencedSOs = new List<string>();
                foreach (var dep in deps)
                {
                    var depGuid = AssetDatabase.AssetPathToGUID(dep);
                    if (soGuids.Contains(depGuid))
                        referencedSOs.Add(dep);
                }

                if (referencedSOs.Count > 0)
                {
                    _prefabSceneRefs.Add(new PrefabSceneRef
                    {
                        FilePath = path,
                        FileType = "Prefab",
                        ReferencedSOPaths = referencedSOs
                    });
                }
            }

            // 扫描 Scene（仅 BuildSettings 中的场景）
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                var path = scene.path;
                EditorUtility.DisplayProgressBar("扫描 Scene 引用", path, 0.5f);

                var deps = AssetDatabase.GetDependencies(path, false);
                var referencedSOs = new List<string>();
                foreach (var dep in deps)
                {
                    var depGuid = AssetDatabase.AssetPathToGUID(dep);
                    if (soGuids.Contains(depGuid))
                        referencedSOs.Add(dep);
                }

                if (referencedSOs.Count > 0)
                {
                    _prefabSceneRefs.Add(new PrefabSceneRef
                    {
                        FilePath = path,
                        FileType = "Scene",
                        ReferencedSOPaths = referencedSOs
                    });
                }
            }
        }

        // ════════════════════════════════════════════════════
        // APPLY
        // ════════════════════════════════════════════════════

        private void RunApply()
        {
            _migratedCount = 0;
            _skippedCount = 0;
            _errorCount = 0;

            var allEntries = _bulletEntries.Concat(_vfxEntries).ToList();
            int total = allEntries.Count;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    var entry = allEntries[i];
                    EditorUtility.DisplayProgressBar("迁移中...",
                        $"{entry.TypeName}: {entry.AssetName}", (float)i / total);

                    if (entry.Status == MigrationStatus.UpToDate)
                    {
                        _skippedCount++;
                        continue;
                    }

                    if (entry.Status == MigrationStatus.Error)
                    {
                        _errorCount++;
                        continue;
                    }

                    if (entry.Status != MigrationStatus.NeedsMigration)
                    {
                        _skippedCount++;
                        continue;
                    }

                    // 执行迁移
                    bool success = entry.TypeName == "BulletTypeSO"
                        ? MigrateBulletAsset(entry)
                        : MigrateVFXAsset(entry);

                    if (success)
                        _migratedCount++;
                    else
                        _errorCount++;
                }

                AssetDatabase.SaveAssets();
                _applyComplete = true;

                Debug.Log($"[迁移器] Apply 完成：{_migratedCount} 迁移, {_skippedCount} 跳过, {_errorCount} 错误");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private bool MigrateBulletAsset(AssetEntry entry)
        {
            var so = AssetDatabase.LoadAssetAtPath<BulletTypeSO>(entry.AssetPath);
            if (so == null)
            {
                entry.Issues.Add("[运行时] 无法加载资产");
                entry.Status = MigrationStatus.Error;
                return false;
            }

            Undo.RecordObject(so, "Migrate BulletTypeSO to Schema v1");

            // 修复非法组合
            if (so.SamplingMode == BulletSamplingMode.Static &&
                so.PlaybackMode != BulletPlaybackMode.StretchToLifetime)
            {
                so.PlaybackMode = BulletPlaybackMode.StretchToLifetime;
                entry.Warnings.Add("已修复：Static 模式下 PlaybackMode 重置为 StretchToLifetime");
            }

            // 钳位 SheetTotalFrames
            if (so.SamplingMode == BulletSamplingMode.SpriteSheet &&
                so.SheetTotalFrames > so.SheetColumns * so.SheetRows)
            {
                so.SheetTotalFrames = so.SheetColumns * so.SheetRows;
                entry.Warnings.Add($"已修复：SheetTotalFrames 钳位为 {so.SheetTotalFrames}");
            }

            // 升版
            so.SchemaVersion = CURRENT_BULLET_SCHEMA;

            EditorUtility.SetDirty(so);
            entry.Status = MigrationStatus.UpToDate;
            return true;
        }

        private bool MigrateVFXAsset(AssetEntry entry)
        {
            var so = AssetDatabase.LoadAssetAtPath<VFXTypeSO>(entry.AssetPath);
            if (so == null)
            {
                entry.Issues.Add("[运行时] 无法加载资产");
                entry.Status = MigrationStatus.Error;
                return false;
            }

            Undo.RecordObject(so, "Migrate VFXTypeSO to Schema v1");

            // 升版
            so.SchemaVersion = CURRENT_VFX_SCHEMA;

            EditorUtility.SetDirty(so);
            entry.Status = MigrationStatus.UpToDate;
            return true;
        }

        // ════════════════════════════════════════════════════
        // REPORT
        // ════════════════════════════════════════════════════

        private void GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 统一资源描述迁移报告（Phase 1）");
            sb.AppendLine();
            sb.AppendLine($"**生成时间**：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**目标 SchemaVersion**：BulletTypeSO={CURRENT_BULLET_SCHEMA}, VFXTypeSO={CURRENT_VFX_SCHEMA}");
            sb.AppendLine();

            // 汇总
            sb.AppendLine("## 汇总");
            sb.AppendLine();
            sb.AppendLine($"| 类型 | 总数 | 已迁移 | 跳过（已是最新） | 错误 |");
            sb.AppendLine($"|------|------|--------|-----------------|------|");
            sb.AppendLine($"| BulletTypeSO | {_bulletEntries.Count} | {_bulletEntries.Count(e => e.Status == MigrationStatus.UpToDate && e.CurrentSchema < CURRENT_BULLET_SCHEMA)} | {_bulletEntries.Count(e => e.CurrentSchema >= CURRENT_BULLET_SCHEMA)} | {_bulletEntries.Count(e => e.Status == MigrationStatus.Error)} |");
            sb.AppendLine($"| VFXTypeSO | {_vfxEntries.Count} | {_vfxEntries.Count(e => e.Status == MigrationStatus.UpToDate && e.CurrentSchema < CURRENT_VFX_SCHEMA)} | {_vfxEntries.Count(e => e.CurrentSchema >= CURRENT_VFX_SCHEMA)} | {_vfxEntries.Count(e => e.Status == MigrationStatus.Error)} |");
            sb.AppendLine($"| **合计** | {_bulletEntries.Count + _vfxEntries.Count} | {_migratedCount} | {_skippedCount} | {_errorCount} |");
            sb.AppendLine();

            // 详细清单
            sb.AppendLine("## BulletTypeSO 详细");
            sb.AppendLine();
            AppendEntryTable(sb, _bulletEntries);

            sb.AppendLine("## VFXTypeSO 详细");
            sb.AppendLine();
            AppendEntryTable(sb, _vfxEntries);

            // Prefab/Scene 引用
            sb.AppendLine("## Prefab / Scene 引用扫描");
            sb.AppendLine();
            if (_prefabSceneRefs.Count == 0)
            {
                sb.AppendLine("未发现引用了目标 SO 的 Prefab/Scene。");
            }
            else
            {
                sb.AppendLine($"| 文件 | 类型 | 引用的 SO |");
                sb.AppendLine($"|------|------|----------|");
                foreach (var r in _prefabSceneRefs)
                {
                    sb.AppendLine($"| `{r.FilePath}` | {r.FileType} | {string.Join(", ", r.ReferencedSOPaths.Select(p => $"`{Path.GetFileNameWithoutExtension(p)}`"))} |");
                }
            }
            sb.AppendLine();

            // 人工处理清单
            var manualItems = _bulletEntries.Concat(_vfxEntries)
                .Where(e => e.Warnings.Count > 0 || e.Issues.Count > 0)
                .ToList();

            if (manualItems.Count > 0)
            {
                sb.AppendLine("## 需人工关注项");
                sb.AppendLine();
                foreach (var item in manualItems)
                {
                    sb.AppendLine($"### {item.TypeName}: {item.AssetName}");
                    sb.AppendLine($"- 路径：`{item.AssetPath}`");
                    foreach (var issue in item.Issues)
                        sb.AppendLine($"- ❌ {issue}");
                    foreach (var warn in item.Warnings)
                        sb.AppendLine($"- ⚠️ {warn}");
                    sb.AppendLine();
                }
            }

            // 写入文件
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../"));
            var reportDir = Path.Combine(projectRoot, REPORT_DIR);
            if (!Directory.Exists(reportDir))
                Directory.CreateDirectory(reportDir);

            var reportPath = Path.Combine(reportDir, REPORT_FILENAME);
            File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
            _lastReportPath = reportPath;

            Debug.Log($"[迁移器] 报告已生成：{reportPath}");
            AssetDatabase.Refresh();
        }

        private void AppendEntryTable(StringBuilder sb, List<AssetEntry> entries)
        {
            if (entries.Count == 0)
            {
                sb.AppendLine("无资产。");
                sb.AppendLine();
                return;
            }

            sb.AppendLine($"| 资产名 | 路径 | Schema | SourceTexture | 状态 | 问题/警告 |");
            sb.AppendLine($"|--------|------|--------|---------------|------|----------|");
            foreach (var e in entries)
            {
                var statusStr = e.Status switch
                {
                    MigrationStatus.UpToDate => "✅ 最新",
                    MigrationStatus.NeedsMigration => "🔄 待迁移",
                    MigrationStatus.Error => "❌ 错误",
                    _ => "⏳ 未知"
                };
                var texStr = e.HasSourceTexture ? "✅" : "⚠️ 空";
                var issueStr = string.Join("; ", e.Issues.Concat(e.Warnings));
                if (string.IsNullOrEmpty(issueStr)) issueStr = "—";

                sb.AppendLine($"| {e.AssetName} | `{e.AssetPath}` | v{e.CurrentSchema}→v{e.TargetSchema} | {texStr} | {statusStr} | {issueStr} |");
            }
            sb.AppendLine();
        }

        // ════════════════════════════════════════════════════
        // GUI 辅助
        // ════════════════════════════════════════════════════

        private void DrawDryRunSummary()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Dry-Run 结果", EditorStyles.boldLabel);

            int bulletNeedsMigration = _bulletEntries.Count(e => e.Status == MigrationStatus.NeedsMigration);
            int bulletUpToDate = _bulletEntries.Count(e => e.Status == MigrationStatus.UpToDate);
            int bulletErrors = _bulletEntries.Count(e => e.Status == MigrationStatus.Error);
            int vfxNeedsMigration = _vfxEntries.Count(e => e.Status == MigrationStatus.NeedsMigration);
            int vfxUpToDate = _vfxEntries.Count(e => e.Status == MigrationStatus.UpToDate);
            int vfxErrors = _vfxEntries.Count(e => e.Status == MigrationStatus.Error);

            EditorGUILayout.LabelField($"BulletTypeSO: {_bulletEntries.Count} 个 " +
                $"（待迁移 {bulletNeedsMigration}, 已最新 {bulletUpToDate}, 错误 {bulletErrors}）");
            EditorGUILayout.LabelField($"VFXTypeSO: {_vfxEntries.Count} 个 " +
                $"（待迁移 {vfxNeedsMigration}, 已最新 {vfxUpToDate}, 错误 {vfxErrors}）");
            EditorGUILayout.LabelField($"Prefab/Scene 引用: {_prefabSceneRefs.Count} 个文件");
        }

        private void DrawApplySummary()
        {
            EditorGUILayout.Space(4);
            var msg = $"迁移完成：{_migratedCount} 成功, {_skippedCount} 跳过, {_errorCount} 错误";
            var type = _errorCount > 0 ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox(msg, type);
        }

        private void DrawDetailList()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("详细列表", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            var allEntries = _bulletEntries.Concat(_vfxEntries).ToList();
            foreach (var entry in allEntries)
            {
                var icon = entry.Status switch
                {
                    MigrationStatus.UpToDate => "✅",
                    MigrationStatus.NeedsMigration => "🔄",
                    MigrationStatus.Error => "❌",
                    _ => "⏳"
                };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{icon} [{entry.TypeName}] {entry.AssetName}",
                    EditorStyles.miniLabel, GUILayout.Width(350));
                EditorGUILayout.LabelField($"v{entry.CurrentSchema}→v{entry.TargetSchema}",
                    GUILayout.Width(70));

                if (!entry.HasSourceTexture)
                    EditorGUILayout.LabelField("⚠️ 无贴图", EditorStyles.miniLabel, GUILayout.Width(60));

                if (GUILayout.Button("选择", GUILayout.Width(45)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.AssetPath);
                    if (obj != null) Selection.activeObject = obj;
                }
                EditorGUILayout.EndHorizontal();

                // 显示问题
                foreach (var issue in entry.Issues)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"  ❌ {issue}", EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
                foreach (var warn in entry.Warnings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"  ⚠️ {warn}", EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool HasBlockingErrors()
        {
            return _bulletEntries.Any(e => e.Status == MigrationStatus.Error) ||
                   _vfxEntries.Any(e => e.Status == MigrationStatus.Error);
        }

        private int CountNeedsMigration()
        {
            return _bulletEntries.Count(e => e.Status == MigrationStatus.NeedsMigration) +
                   _vfxEntries.Count(e => e.Status == MigrationStatus.NeedsMigration);
        }
    }
}
#endif
