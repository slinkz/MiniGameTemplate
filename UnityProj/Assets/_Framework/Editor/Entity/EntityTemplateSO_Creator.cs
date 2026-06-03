using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Editor.Entity
{
    /// <summary>
    /// P1.11 模板 SO 创建器——一键在 Assets/_Game/Configs/_Template/ 下创建 Demo 用模板。
    /// WF-009：Demo SO 资产保留为模板（文件名 Template_ 前缀）。
    /// </summary>
    public static class EntityTemplateSO_Creator
    {
        private const string TEMPLATE_PATH = "Assets/_Game/Configs/_Template";

        [MenuItem("MiniGameTemplate/Entity/Create P1.11 Template SOs", false, 200)]
        public static void CreateAllTemplates()
        {
            EnsureDirectoryExists(TEMPLATE_PATH);
            EnsureDirectoryExists(TEMPLATE_PATH + "/Entity");
            EnsureDirectoryExists(TEMPLATE_PATH + "/AI");
            EnsureDirectoryExists(TEMPLATE_PATH + "/SpawnWave");
            EnsureDirectoryExists(TEMPLATE_PATH + "/Pool");

            CreatePlayerTemplate();
            CreateSlimeTemplate();
            CreateSlimeAIBehaviorTemplate();
            CreateEnemyWaveTemplate();
            CreateDebugViewPoolTemplate();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[EntityTemplateSO_Creator] P1.11 模板 SO 全部创建完成！");
        }

        private static void CreatePlayerTemplate()
        {
            string path = $"{TEMPLATE_PATH}/Entity/Template_Player.asset";
            if (AssetDatabase.LoadAssetAtPath<EntityConfigSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<EntityConfigSO>();
            so.DisplayName = "Player";
            so.Camp = EnumCamp.Player;
            so.Components = new[]
            {
                ComponentType.State,
                ComponentType.Health,
                ComponentType.Movement,
                ComponentType.Collision,
                ComponentType.Control,
                ComponentType.Skill,
            };
            so.PoolMax = 1;
            so.MaxHp = 500;
            so.MoveSpeed = 5f;
            so.CollisionRadius = 0.3f;
            so.AttackInterval = 0.15f;
            so.AttackFireOffset = new Vector2(0f, 0.5f);
            // 尝试加载 Demo 弹幕配置（如不存在则留空，用户手动配置）
            so.AttackBulletPattern = AssetDatabase.LoadAssetAtPath<BulletPatternSO>(
                "Assets/_Example/DanmakuDemo/BulletPattern/Pattern_Aimed5.asset");
            so.KnockbackDistance = 0f; // 玩家不被击退（或很小）
            so.KnockbackDuration = 0f;
            so.DebugColor = new Color(0.2f, 0.8f, 1f, 1f); // 浅蓝
            so.HitFlashDuration = 0.08f;
            so.ShowDamageNumber = false; // 玩家不显示伤害数字
            so.DeathDelay = 0f;

            AssetDatabase.CreateAsset(so, path);
        }

        private static void CreateSlimeTemplate()
        {
            string path = $"{TEMPLATE_PATH}/Entity/Template_Slime.asset";
            if (AssetDatabase.LoadAssetAtPath<EntityConfigSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<EntityConfigSO>();
            so.DisplayName = "Slime";
            so.Camp = EnumCamp.Enemy;
            so.Components = new[]
            {
                ComponentType.State,
                ComponentType.Health,
                ComponentType.Movement,
                ComponentType.Collision,
                ComponentType.AI,
            };
            so.PoolMax = 16;
            so.MaxHp = 30;
            so.MoveSpeed = 2f;
            so.CollisionRadius = 0.4f;
            so.AttackInterval = 1.5f;
            so.KnockbackDistance = 0.5f;
            so.KnockbackDuration = 0.15f;
            so.DebugColor = new Color(0.8f, 0.2f, 0.2f, 1f); // 红色
            so.HitFlashDuration = 0.12f;
            so.HitFlashColor = Color.white;
            so.ShowDamageNumber = true;
            so.DeathDelay = 0.3f;

            AssetDatabase.CreateAsset(so, path);
        }

        private static void CreateSlimeAIBehaviorTemplate()
        {
            string path = $"{TEMPLATE_PATH}/AI/Template_SlimeAI.asset";
            if (AssetDatabase.LoadAssetAtPath<AIBehaviorSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<AIBehaviorSO>();
            so.Entries = new AIBehaviorEntry[]
            {
                // 优先级 1：目标在攻击范围内 → 攻击
                new AIBehaviorEntry
                {
                    Condition = AIConditionType.TargetInRange,
                    ConditionParam = 3f, // 攻击距离
                    Action = AIActionType.Attack,
                    ActionParam = 0f,
                },
                // 优先级 2：有目标但不在范围内 → 追击
                new AIBehaviorEntry
                {
                    Condition = AIConditionType.Always,
                    ConditionParam = 0f,
                    Action = AIActionType.MoveToTarget,
                    ActionParam = 0f,
                },
            };

            AssetDatabase.CreateAsset(so, path);
        }

        private static void CreateEnemyWaveTemplate()
        {
            string path = $"{TEMPLATE_PATH}/SpawnWave/Template_EnemyWave.asset";
            if (AssetDatabase.LoadAssetAtPath<EntitySpawnWaveSO>(path) != null) return;

            // 加载 Slime 模板
            var slimeConfig = AssetDatabase.LoadAssetAtPath<EntityConfigSO>(
                $"{TEMPLATE_PATH}/Entity/Template_Slime.asset");

            var so = ScriptableObject.CreateInstance<EntitySpawnWaveSO>();
            so.Waves = new SpawnWaveEntry[]
            {
                // 第 1 波：3 个 Slime
                new SpawnWaveEntry
                {
                    Groups = new SpawnGroup[]
                    {
                        new SpawnGroup
                        {
                            EntityConfig = slimeConfig,
                            Camp = EnumCamp.Enemy,
                            Count = 3,
                            SpawnInterval = 0.5f,
                            Formation = SpawnFormation.Random,
                        }
                    },
                    TriggerMode = WaveTriggerMode.AllCleared,
                    TriggerDelay = 0f,
                },
                // 第 2 波：5 个 Slime
                new SpawnWaveEntry
                {
                    Groups = new SpawnGroup[]
                    {
                        new SpawnGroup
                        {
                            EntityConfig = slimeConfig,
                            Camp = EnumCamp.Enemy,
                            Count = 5,
                            SpawnInterval = 0.3f,
                            Formation = SpawnFormation.Random,
                        }
                    },
                    TriggerMode = WaveTriggerMode.Timer,
                    TriggerDelay = 2f,
                },
            };
            so.Loop = true;
            so.LoopStartWave = 0;

            AssetDatabase.CreateAsset(so, path);
        }

        private static void CreateDebugViewPoolTemplate()
        {
            string path = $"{TEMPLATE_PATH}/Pool/Template_DebugViewPool.asset";
            if (AssetDatabase.LoadAssetAtPath<MiniGameTemplate.Pool.PoolDefinition>(path) != null) return;

            // PoolDefinition 需要引用 Prefab——但我们没有 Prefab。
            // 创建一个空的 PoolDefinition 占位，用户需手动指定 Prefab。
            // 实际 Demo 中使用 EntityDebugView_Creator 脚本生成 Prefab 后填入。
            var so = ScriptableObject.CreateInstance<MiniGameTemplate.Pool.PoolDefinition>();
            AssetDatabase.CreateAsset(so, path);
            Debug.Log("[Template] Template_DebugViewPool 已创建。请手动指定 Prefab（或运行 CreateDebugViewPrefab）。");
        }

        // ──────────── 工具 ────────────

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = System.IO.Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDirectoryExists(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
