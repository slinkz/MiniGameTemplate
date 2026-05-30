#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// EntityConfigSO 自定义 Inspector。
    /// ET-001/002: Checkbox Grid + 条件显示 + HelpBox 校验。
    /// WF-002: Play Mode 黄色 HelpBox。
    /// WF-006: 空 Components 红色 HelpBox。
    /// WF-011: 条件显示段落前加分段标题。
    /// </summary>
    [CustomEditor(typeof(EntityConfigSO))]
    public class EntityConfigSOEditor : UnityEditor.Editor
    {
        private SerializedProperty _configId;
        private SerializedProperty _displayName;
        private SerializedProperty _camp;
        private SerializedProperty _components;
        private SerializedProperty _poolMax;
        private SerializedProperty _maxHp;
        private SerializedProperty _moveSpeed;
        private SerializedProperty _turnSpeed;
        private SerializedProperty _collisionRadius;
        private SerializedProperty _knockbackDistance;
        private SerializedProperty _knockbackDuration;
        // P2.4 受击参数
        private SerializedProperty _iFrameCount;
        private SerializedProperty _hitStopFrames;
        private SerializedProperty _knockbackCurve;
        // P2.2 Entity 碰撞
        private SerializedProperty _enableEntityCollision;
        private SerializedProperty _collisionLayer;
        private SerializedProperty _contactDamage;
        private SerializedProperty _contactDamageInterval;
        // 自动瞄准（P3.1）
        private SerializedProperty _autoAimRadius;
        private SerializedProperty _autoAimSearchInterval;
        // 攻击
        private SerializedProperty _attackPower;
        private SerializedProperty _critRate;
        private SerializedProperty _critDamageMultiplier;
        private SerializedProperty _attackInterval;
        private SerializedProperty _firstAttackDelay;
        private SerializedProperty _attackBulletPattern;
        private SerializedProperty _attackFireOffset;
        // 普攻技能（TDD-06）
        private SerializedProperty _normalAttackSkill;
        // 技能（P3.3）
        private SerializedProperty _skillConfig;
        private SerializedProperty _aiBehavior;
        // View 桥接（Phase 1.9）
        private SerializedProperty _viewPrefab;
        private SerializedProperty _viewPoolDef;
        private SerializedProperty _spriteAnimData;
        private SerializedProperty _debugColor;
        // 受击反馈 + 视觉特效（P1.11）
        private SerializedProperty _hitFlashDuration;
        private SerializedProperty _hitFlashColor;
        private SerializedProperty _showDamageNumber;
        private SerializedProperty _spawnEffect;
        private SerializedProperty _hitEffect;
        private SerializedProperty _deathEffect;
        private SerializedProperty _deathDelay;

        private static readonly ComponentType[] AllTypes = (ComponentType[])System.Enum.GetValues(typeof(ComponentType));

        private void OnEnable()
        {
            _configId = serializedObject.FindProperty("ConfigId");
            _displayName = serializedObject.FindProperty("DisplayName");
            _camp = serializedObject.FindProperty("Camp");
            _components = serializedObject.FindProperty("Components");
            _poolMax = serializedObject.FindProperty("PoolMax");
            _maxHp = serializedObject.FindProperty("MaxHp");
            _moveSpeed = serializedObject.FindProperty("MoveSpeed");
            _turnSpeed = serializedObject.FindProperty("TurnSpeed");
            _collisionRadius = serializedObject.FindProperty("CollisionRadius");
            _knockbackDistance = serializedObject.FindProperty("KnockbackDistance");
            _knockbackDuration = serializedObject.FindProperty("KnockbackDuration");
            // P2.4 受击参数
            _iFrameCount = serializedObject.FindProperty("IFrameCount");
            _hitStopFrames = serializedObject.FindProperty("HitStopFrames");
            _knockbackCurve = serializedObject.FindProperty("KnockbackCurve");
            // P2.2 Entity 碰撞
            _enableEntityCollision = serializedObject.FindProperty("EnableEntityCollision");
            _collisionLayer = serializedObject.FindProperty("CollisionLayer");
            _contactDamage = serializedObject.FindProperty("ContactDamage");
            _contactDamageInterval = serializedObject.FindProperty("ContactDamageInterval");
            // 自动瞄准（P3.1）
            _autoAimRadius = serializedObject.FindProperty("AutoAimRadius");
            _autoAimSearchInterval = serializedObject.FindProperty("AutoAimSearchInterval");
            // 攻击
            _attackPower = serializedObject.FindProperty("AttackPower");
            _critRate = serializedObject.FindProperty("CritRate");
            _critDamageMultiplier = serializedObject.FindProperty("CritDamageMultiplier");
            _attackInterval = serializedObject.FindProperty("AttackInterval");
            _firstAttackDelay = serializedObject.FindProperty("FirstAttackDelay");
            _attackBulletPattern = serializedObject.FindProperty("AttackBulletPattern");
            _attackFireOffset = serializedObject.FindProperty("AttackFireOffset");
            // 普攻技能（TDD-06）
            _normalAttackSkill = serializedObject.FindProperty("NormalAttackSkill");
            // 技能（P3.3）
            _skillConfig = serializedObject.FindProperty("SkillConfig");
            _aiBehavior = serializedObject.FindProperty("AIBehavior");
            // View 桥接（Phase 1.9）
            _viewPrefab = serializedObject.FindProperty("ViewPrefab");
            _viewPoolDef = serializedObject.FindProperty("ViewPoolDef");
            _spriteAnimData = serializedObject.FindProperty("SpriteAnimData");
            _debugColor = serializedObject.FindProperty("DebugColor");
            // 受击反馈 + 视觉特效（P1.11）
            _hitFlashDuration = serializedObject.FindProperty("HitFlashDuration");
            _hitFlashColor = serializedObject.FindProperty("HitFlashColor");
            _showDamageNumber = serializedObject.FindProperty("ShowDamageNumber");
            _spawnEffect = serializedObject.FindProperty("SpawnEffect");
            _hitEffect = serializedObject.FindProperty("HitEffect");
            _deathEffect = serializedObject.FindProperty("DeathEffect");
            _deathDelay = serializedObject.FindProperty("DeathDelay");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ──── HelpBox 警告层 ────
            DrawWarnings();

            // ──── 基础信息 ────
            EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_configId);
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_camp);

            EditorGUILayout.Space(8);

            // ──── Components Checkbox Grid ────
            EditorGUILayout.LabelField("组件列表", EditorStyles.boldLabel);
            DrawComponentCheckboxGrid();

            EditorGUILayout.Space(8);

            // ──── 对象池 ────
            EditorGUILayout.LabelField("对象池", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_poolMax);

            EditorGUILayout.Space(8);

            // ──── 属性 ────
            EditorGUILayout.LabelField("属性", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_maxHp);
            EditorGUILayout.PropertyField(_moveSpeed);
            EditorGUILayout.PropertyField(_turnSpeed);

            // ──── 战斗属性（独立显示）────
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("战斗属性", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_attackPower);
            EditorGUILayout.PropertyField(_critRate);
            EditorGUILayout.PropertyField(_critDamageMultiplier);
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_attackInterval);
            EditorGUILayout.PropertyField(_firstAttackDelay);
            EditorGUILayout.PropertyField(_attackBulletPattern);
            EditorGUILayout.PropertyField(_attackFireOffset);

            // ──── 普攻技能配置（TDD-06）────
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_normalAttackSkill);
            if (_normalAttackSkill.objectReferenceValue != null && _attackInterval.floatValue > 0)
            {
                EditorGUILayout.HelpBox(
                    $"AttackInterval={_attackInterval.floatValue:F2}s 将在运行时覆盖 SkillConfigSO.CooldownTime",
                    MessageType.Info);
            }

            // 碰撞组件配置（条件显示）
            if (HasComponent(ComponentType.Collision))
            {
                EditorGUILayout.Space(4);
                DrawSectionTitle("碰撞组件配置（因勾选了 Collision 而显示）");
                EditorGUILayout.PropertyField(_collisionRadius);
            }

            // 击退
            EditorGUILayout.PropertyField(_knockbackDistance);
            EditorGUILayout.PropertyField(_knockbackDuration);
            EditorGUILayout.PropertyField(_knockbackCurve);

            // ──── 受击参数（P2.4）────
            if (HasComponent(ComponentType.Health))
            {
                EditorGUILayout.Space(4);
                DrawSectionTitle("受击参数（因勾选了 Health 而显示）");
                EditorGUILayout.PropertyField(_iFrameCount);
                EditorGUILayout.PropertyField(_hitStopFrames);
            }

            // ──── Entity vs Entity 碰撞（P2.2）────
            if (HasComponent(ComponentType.Collision))
            {
                EditorGUILayout.Space(4);
                DrawSectionTitle("Entity 碰撞（P2.2）");
                EditorGUILayout.PropertyField(_enableEntityCollision);
                if (_enableEntityCollision.boolValue)
                {
                    EditorGUILayout.PropertyField(_collisionLayer);
                    EditorGUILayout.PropertyField(_contactDamage);
                    if (_contactDamage.intValue > 0)
                    {
                        EditorGUILayout.PropertyField(_contactDamageInterval);
                    }
                }
            }

            // ──── 自动瞄准 ────
            EditorGUILayout.Space(4);
            DrawSectionTitle("自动瞄准（AutoAim）");
            EditorGUILayout.PropertyField(_autoAimRadius);
            if (_autoAimRadius.floatValue > 0)
            {
                EditorGUILayout.PropertyField(_autoAimSearchInterval);
            }

            // ──── 技能组件配置（条件显示）────
            if (HasComponent(ComponentType.Skill))
            {
                EditorGUILayout.Space(8);
                DrawSectionTitle("技能组件配置（因勾选了 Skill 而显示）");
                EditorGUILayout.PropertyField(_skillConfig);
            }

            // ──── AI 组件配置（条件显示）────
            if (HasComponent(ComponentType.AI))
            {
                EditorGUILayout.Space(8);
                DrawSectionTitle("AI 组件配置（因勾选了 AI 而显示）");
                EditorGUILayout.PropertyField(_aiBehavior);
            }

            // ──── 视觉 View（Phase 1.9）────
            EditorGUILayout.Space(8);
            DrawSectionTitle("视觉 View（Phase 1: DebugColor 生效 / Phase 2: ViewPrefab 生效）");
            EditorGUILayout.PropertyField(_viewPrefab);
            if (_viewPrefab.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(_viewPoolDef);
                if (_viewPoolDef.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(
                        "已填 ViewPrefab 但 ViewPoolDef 为空——ViewBridge 将 fallback 到 Debug View。",
                        MessageType.Warning);
                }
                EditorGUILayout.PropertyField(_spriteAnimData);
            }
            EditorGUILayout.PropertyField(_debugColor);

            // ──── 受击反馈 + 视觉特效（P1.11）────
            EditorGUILayout.Space(8);
            DrawSectionTitle("受击反馈 + 视觉特效");
            EditorGUILayout.PropertyField(_hitFlashDuration);
            EditorGUILayout.PropertyField(_hitFlashColor);
            EditorGUILayout.PropertyField(_showDamageNumber);
            EditorGUILayout.PropertyField(_spawnEffect);
            EditorGUILayout.PropertyField(_hitEffect);
            EditorGUILayout.PropertyField(_deathEffect);
            EditorGUILayout.PropertyField(_deathDelay);

            serializedObject.ApplyModifiedProperties();
        }

        // ──────────── 私有方法 ────────────

        private void DrawWarnings()
        {
            // WF-002: Play Mode 提示
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Play Mode：修改此配置仅对新生成的 Entity 生效，已存在的 Entity 不受影响。\n" +
                    "如需验证所有 Entity，请使用 Entity Debug Overview 窗口的 Restart All Waves 按钮，或退出并重新进入 Play Mode。",
                    MessageType.Warning);
                EditorGUILayout.Space(4);
            }

            // WF-006: 空 Components
            if (_components.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ 组件列表为空！Entity 将没有任何能力。请至少勾选 State 组件。",
                    MessageType.Error);
                EditorGUILayout.Space(4);
            }

            // 有 AI 但无 AIBehavior
            if (HasComponent(ComponentType.AI) && _aiBehavior.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Components 含 AI 但 AIBehavior 未填——运行时将 fallback Idle。",
                    MessageType.Warning);
            }

            // Control/AI 互斥
            if (HasComponent(ComponentType.Control) && HasComponent(ComponentType.AI))
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Control 和 AI 不应同时勾选！运行时 DecisionMaker 会冲突。",
                    MessageType.Error);
            }

            // 有攻击间隔但无弹幕配置也无普攻技能——不会发射
            if (_attackInterval.floatValue > 0 &&
                _attackBulletPattern.objectReferenceValue == null &&
                _normalAttackSkill.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "AttackInterval > 0 但 BulletPattern 和 NormalAttackSkill 均未填——Entity 不会发射弹幕。",
                    MessageType.Warning);
            }

            // 依赖建议
            if (HasComponent(ComponentType.AI) && !HasComponent(ComponentType.Movement))
            {
                EditorGUILayout.HelpBox(
                    "建议：AI 组件通常搭配 Movement 使用（否则 AI 无法驱动移动）。",
                    MessageType.Info);
            }

            // 有 Collision 但无 Health（伤害无法结算）
            if (HasComponent(ComponentType.Collision) && !HasComponent(ComponentType.Health))
            {
                EditorGUILayout.HelpBox(
                    "建议：Collision 组件通常搭配 Health 使用（否则碰撞不会造成伤害）。",
                    MessageType.Info);
            }
        }

        private void DrawComponentCheckboxGrid()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 读取当前 Components 数组为 HashSet
            var active = new System.Collections.Generic.HashSet<ComponentType>();
            for (int i = 0; i < _components.arraySize; i++)
            {
                active.Add((ComponentType)_components.GetArrayElementAtIndex(i).enumValueIndex);
            }

            // 绘制每个 ComponentType 为 checkbox
            bool changed = false;
            foreach (var type in AllTypes)
            {
                // 跳过内部枚举
                if (type == ComponentType.MAX)
                    continue;

                bool wasOn = active.Contains(type);

                // Control/AI 互斥灰化
                bool disabled = false;
                if (type == ComponentType.Control && active.Contains(ComponentType.AI))
                    disabled = true;
                if (type == ComponentType.AI && active.Contains(ComponentType.Control))
                    disabled = true;

                EditorGUI.BeginDisabledGroup(disabled);
                bool isOn = EditorGUILayout.ToggleLeft(GetComponentLabel(type), wasOn);
                EditorGUI.EndDisabledGroup();

                if (isOn != wasOn)
                {
                    changed = true;
                    if (isOn)
                        active.Add(type);
                    else
                        active.Remove(type);
                }
            }

            if (changed)
            {
                // 写回数组
                _components.ClearArray();
                int idx = 0;
                foreach (var type in AllTypes)
                {
                    if (active.Contains(type))
                    {
                        _components.InsertArrayElementAtIndex(idx);
                        _components.GetArrayElementAtIndex(idx).enumValueIndex = (int)type;
                        idx++;
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private static string GetComponentLabel(ComponentType type)
        {
            return type switch
            {
                ComponentType.State => "State（状态机）",
                ComponentType.Health => "Health（生命值）",
                ComponentType.Movement => "Movement（移动）",
                ComponentType.Animation => "Animation（动画逻辑）",
                ComponentType.Collision => "Collision（碰撞）",
                ComponentType.Skill => "Skill（技能，Phase 3）",
                ComponentType.Control => "Control（玩家输入）",
                ComponentType.AI => "AI（自动决策）",
                _ => type.ToString()
            };
        }

        private bool HasComponent(ComponentType type)
        {
            for (int i = 0; i < _components.arraySize; i++)
            {
                if ((ComponentType)_components.GetArrayElementAtIndex(i).enumValueIndex == type)
                    return true;
            }
            return false;
        }

        private static void DrawSectionTitle(string title)
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 9, rect.width, 1), new Color(0.5f, 0.5f, 0.5f, 0.5f));
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            EditorGUI.LabelField(rect, title, style);
        }
    }
}
#endif
