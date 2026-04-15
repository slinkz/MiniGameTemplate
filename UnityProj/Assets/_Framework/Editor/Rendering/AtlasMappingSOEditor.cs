using System.Collections.Generic;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Rendering;
using MiniGameTemplate.VFX;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Editor.Rendering
{
    /// <summary>
    /// AtlasMappingSO 自定义 Inspector——在默认 Inspector 之上添加"回写到关联 TypeSO"按钮。
    /// 回写工作流：dry-run（列出变更）→ apply（Undo 支持）→ report。
    /// </summary>
    [CustomEditor(typeof(AtlasMappingSO))]
    public class AtlasMappingSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mapping = (AtlasMappingSO)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("工具", EditorStyles.boldLabel);

            if (mapping.Entries == null || mapping.Entries.Length == 0)
            {
                EditorGUILayout.HelpBox("Atlas 映射为空，请先使用 Atlas 打包工具打包。", MessageType.Info);
                return;
            }

            if (GUILayout.Button("📝 回写 UVRect 到关联 TypeSO", GUILayout.Height(28)))
            {
                RunWriteback(mapping);
            }
        }

        private void RunWriteback(AtlasMappingSO mapping)
        {
            // === Phase 1: Dry-Run ===
            var changes = new List<WritebackEntry>();

            // 扫描所有 BulletTypeSO
            string[] bulletGuids = AssetDatabase.FindAssets("t:BulletTypeSO");
            foreach (string guid in bulletGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var bulletType = AssetDatabase.LoadAssetAtPath<BulletTypeSO>(path);
                if (bulletType == null) continue;

                // 匹配条件：AtlasBinding == 当前 mapping
                if (bulletType.AtlasBinding != mapping) continue;

                if (mapping.TryFindEntry(bulletType.SourceTexture, out var entry))
                {
                    if (bulletType.UVRect != entry.UVRect)
                    {
                        changes.Add(new WritebackEntry
                        {
                            Target = bulletType,
                            AssetPath = path,
                            OldUV = bulletType.UVRect,
                            NewUV = entry.UVRect,
                            TypeName = "BulletTypeSO",
                        });
                    }
                }
            }

            // 扫描所有 VFXTypeSO
            string[] vfxGuids = AssetDatabase.FindAssets("t:VFXTypeSO");
            foreach (string guid in vfxGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var vfxType = AssetDatabase.LoadAssetAtPath<VFXTypeSO>(path);
                if (vfxType == null) continue;

                if (vfxType.AtlasBinding != mapping) continue;

                if (mapping.TryFindEntry(vfxType.SourceTexture, out var entry))
                {
                    if (vfxType.UVRect != entry.UVRect)
                    {
                        changes.Add(new WritebackEntry
                        {
                            Target = vfxType,
                            AssetPath = path,
                            OldUV = vfxType.UVRect,
                            NewUV = entry.UVRect,
                            TypeName = "VFXTypeSO",
                        });
                    }
                }
            }

            if (changes.Count == 0)
            {
                EditorUtility.DisplayDialog("回写结果", "所有关联 TypeSO 的 UVRect 已经是最新的，无需更新。", "确定");
                return;
            }

            // === Phase 2: 用户确认 ===
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"将修改 {changes.Count} 个 TypeSO 的 UVRect：\n");
            foreach (var change in changes)
            {
                sb.AppendLine($"  [{change.TypeName}] {change.Target.name}");
                sb.AppendLine($"    旧: {change.OldUV}");
                sb.AppendLine($"    新: {change.NewUV}");
                sb.AppendLine();
            }
            sb.AppendLine("是否应用？");

            if (!EditorUtility.DisplayDialog("Atlas UVRect 回写（Dry-Run）", sb.ToString(), "应用", "取消"))
                return;

            // === Phase 3: Apply ===
            int appliedCount = 0;
            foreach (var change in changes)
            {
                Undo.RecordObject(change.Target, "Atlas UVRect Writeback");

                if (change.Target is BulletTypeSO bulletSO)
                {
                    bulletSO.UVRect = change.NewUV;
                }
                else if (change.Target is VFXTypeSO vfxSO)
                {
                    vfxSO.UVRect = change.NewUV;
                }

                EditorUtility.SetDirty(change.Target);
                appliedCount++;
            }

            AssetDatabase.SaveAssets();

            // 触发编辑器刷新链路（本文件已在 Editor asmdef 下，无需 #if UNITY_EDITOR）
            MiniGameTemplate.Danmaku.Editor.DanmakuEditorRefreshCoordinator.RunControlledRefreshMenu();

            // === Phase 4: Report ===
            string report = $"[AtlasWriteback] 回写完成：{appliedCount}/{changes.Count} 个 TypeSO 已更新 UVRect。";
            Debug.Log(report);
            EditorUtility.DisplayDialog("回写完成", report, "确定");
        }

        private struct WritebackEntry
        {
            public ScriptableObject Target;
            public string AssetPath;
            public Rect OldUV;
            public Rect NewUV;
            public string TypeName;
        }
    }
}
