using System;
using UnityEngine;
using MiniGameTemplate.Core;

namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// 定义一个导航节点（屏幕/页面）。
    /// 每个节点声明它需要什么场景 + 什么 UI 面板。
    /// 纯数据，零逻辑。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Navigation/Flow Node", order = 0)]
    public class FlowNodeSO : ScriptableObject
    {
        [Header("场景控制")]
        [Tooltip("进入此节点时需要加载的场景。null = 纯 UI 节点（不切场景）")]
        [SerializeField] private SceneDefinition _requiredScene;

        [Header("UI 面板")]
        [Tooltip("进入此节点时需要打开的面板注册表 key。空 = 不自动打开面板。")]
        [SerializeField] private string _panelTypeName;

        [Header("行为")]
        [Tooltip("离开时是否卸载场景（仅 _requiredScene != null 时生效）")]
        [SerializeField] private bool _unloadSceneOnExit = true;

        [Header("元数据")]
        [SerializeField] private string _displayName;

        [Header("序列化（Phase 4 栈恢复）")]
        [Tooltip("唯一标识 — 栈序列化/反序列化时用于定位此节点 SO。创建后不要修改。")]
        [SerializeField] private string _nodeId;

        // --- Public API ---
        public SceneDefinition RequiredScene => _requiredScene;
        public string PanelTypeName => _panelTypeName;
        public bool UnloadSceneOnExit => _unloadSceneOnExit;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string NodeId => _nodeId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 1. PanelTypeName 格式校验
            if (!string.IsNullOrEmpty(_panelTypeName) && _panelTypeName.Contains(' '))
                Debug.LogWarning($"[FlowNodeSO] '{name}': PanelTypeName 不应包含空格，请使用 PascalCase。");

            // 2. 无意义配置
            if (_requiredScene == null && _unloadSceneOnExit)
                Debug.LogWarning($"[FlowNodeSO] '{name}': UnloadSceneOnExit=true 但 RequiredScene 为空。");

            // 3. DisplayName 自动填充
            if (string.IsNullOrEmpty(_displayName))
                _displayName = name;

            // 4. NodeId 自动填充（首次创建时）
            if (string.IsNullOrEmpty(_nodeId))
                _nodeId = Guid.NewGuid().ToString("N");
        }

        /// <summary>Editor-only: 用于 FlowNodeSOEditor 读取 SerializedProperty 的名称。</summary>
        public const string PROP_NODE_ID = "_nodeId";
        public const string PROP_PANEL_TYPE_NAME = "_panelTypeName";
        public const string PROP_REQUIRED_SCENE = "_requiredScene";
        public const string PROP_UNLOAD_SCENE_ON_EXIT = "_unloadSceneOnExit";
        public const string PROP_DISPLAY_NAME = "_displayName";
#endif
    }
}
