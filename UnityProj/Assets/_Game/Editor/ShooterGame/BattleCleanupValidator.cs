#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MiniGameTemplate.Battle;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// TDD-07 Phase D：BattleCleanup 验证器。
    /// 1. 扫描所有实现 IBattleCleanup 的 MonoBehaviour 类型，验证它们声明了 [SerializeField] BattleLifecycleEvent 字段。
    /// 2. WX-007 实例级检查：扫描当前场景中所有含 BattleLifecycleEvent 引用的 MB 实例，检查引用是否为 null。
    /// </summary>
    public static class BattleCleanupValidator
    {
        [MenuItem("ShooterGame/Validate Battle Cleanup")]
        public static void ValidateAll()
        {
            int issueCount = 0;

            Debug.Log("═══ [BattleCleanupValidator] 开始验证 ═══");

            // ── Step 1: 类型级验证 ──
            issueCount += ValidateTypes();

            // ── Step 2: 实例级验证（WX-007）──
            issueCount += ValidateSceneInstances();

            if (issueCount == 0)
                Debug.Log("✅ [BattleCleanupValidator] 验证通过——0 个遗漏。");
            else
                Debug.LogWarning($"⚠️ [BattleCleanupValidator] 发现 {issueCount} 个问题，请检查上方日志。");

            Debug.Log("═══ [BattleCleanupValidator] 验证结束 ═══");
        }

        /// <summary>扫描所有实现 IBattleCleanup 的 MB 类型，检查是否有 BattleLifecycleEvent 字段。</summary>
        private static int ValidateTypes()
        {
            int issues = 0;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(MonoBehaviour).IsAssignableFrom(type)) continue;
                    if (!typeof(IBattleCleanup).IsAssignableFrom(type)) continue;

                    // 检查是否有 [SerializeField] BattleLifecycleEvent 字段
                    bool hasField = false;
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(BattleLifecycleEvent))
                        {
                            hasField = true;
                            break;
                        }
                    }

                    if (!hasField)
                    {
                        Debug.LogWarning(
                            $"[BattleCleanupValidator] 类型 {type.FullName} 实现了 IBattleCleanup 但缺少 BattleLifecycleEvent 字段——" +
                            "退场时不会被自动清理。如果是通过代理注册（如 Awake 手动 Register），请忽略此警告。");
                        issues++;
                    }
                    else
                    {
                        Debug.Log($"  ✓ {type.Name} — CleanupOrder 声明正确，BattleLifecycleEvent 字段存在");
                    }
                }
            }

            return issues;
        }

        /// <summary>WX-007: 扫描当前场景实例，检查 BattleLifecycleEvent 引用是否为 null。</summary>
        private static int ValidateSceneInstances()
        {
            int issues = 0;
            var allMBs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);

            foreach (var mb in allMBs)
            {
                var type = mb.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in fields)
                {
                    if (field.FieldType != typeof(BattleLifecycleEvent)) continue;

                    var value = field.GetValue(mb);
                    if (value == null || (value is UnityEngine.Object uObj && uObj == null))
                    {
                        Debug.LogError(
                            $"[BattleCleanupValidator] {type.Name} 实例 \"{mb.gameObject.name}\" 的 " +
                            $"BattleLifecycleEvent 字段 \"{field.Name}\" 为 null！退场时该系统不会被清理。",
                            mb.gameObject);
                        issues++;
                    }
                }
            }

            return issues;
        }
    }
}
#endif
