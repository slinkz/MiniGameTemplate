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
        private SerializedProperty _attackInterval;
        private SerializedProperty _attackBulletPattern;
        private SerializedProperty _attackFireOffset;
        private SerializedProperty _aiBehavior;

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
            _attackInterval = serializedObject.FindProperty("AttackInterval");
            _attackBulletPattern = serializedObject.FindProperty("AttackBulletPattern");
            _attackFireOffset = serializedObject.FindProperty("AttackFireOffset");
            _aiBehavior = serializedObject.FindProperty("AIBehavior");
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

            // ──── 攻击组件配置（条件显示）────
            if (HasComponent(ComponentType.Attack))
            {
                EditorGUILayout.Space(8);
                DrawSectionTitle("攻击组件配置（因勾选了 Attack 而显示）");
                EditorGUILayout.PropertyField(_attackInterval);
                EditorGUILayout.PropertyField(_attackBulletPattern);
                EditorGUILayout.PropertyField(_attackFireOffset);
            }

            // ──── AI 组件配置（条件显示）────
            if (HasComponent(ComponentType.AI))
            {
                EditorGUILayout.Space(8);
                DrawSectionTitle("AI 组件配置（因勾选了 AI 而显示）");
                EditorGUILayout.PropertyField(_aiBehavior);
            }

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

            // 有 Attack 但无 BulletPattern
            if (HasComponent(ComponentType.Attack) &&
                _attackBulletPattern.objectReferenceValue == null &&
                _attackInterval.floatValue > 0)
            {
                EditorGUILayout.HelpBox(
                    "Components 含 Attack 但 AttackBulletPattern 未填且 AttackInterval > 0——Entity 不会发射弹幕。",
                    MessageType.Warning);
            }

            // Control/AI 互斥
            if (HasComponent(ComponentType.Control) && HasComponent(ComponentType.AI))
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Control 和 AI 不应同时勾选！运行时 AttackComponent 将优先使用 Control 的决策。",
                    MessageType.Error);
            }

            // 依赖建议
            if (HasComponent(ComponentType.AI) && !HasComponent(ComponentType.Movement))
            {
                EditorGUILayout.HelpBox(
                    "建议：AI 组件通常搭配 Movement 使用（否则 AI 无法驱动移动）。",
                    MessageType.Info);
            }

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
                ComponentType.Attack => "Attack（定时攻击）",
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
