using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// FlowNode 注册表 SO。持有所有 FlowNodeSO 引用，通过 nodeId 反查 SO 实例。
    /// 用于栈反序列化时从 nodeId 恢复到具体 SO。（Phase 4）
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Navigation/Flow Node Registry", order = 1)]
    public class FlowNodeRegistry : ScriptableObject
    {
        [SerializeField] private List<FlowNodeSO> _nodes = new();

        // Runtime 缓存（首次访问时构建）
        private Dictionary<string, FlowNodeSO> _lookup;

        /// <summary>
        /// 通过 nodeId 查找对应的 FlowNodeSO。找不到返回 null。
        /// </summary>
        public FlowNodeSO GetByNodeId(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;

            if (_lookup == null)
                RebuildLookup();

            _lookup.TryGetValue(nodeId, out var node);
            return node;
        }

        /// <summary>所有注册节点。</summary>
        public IReadOnlyList<FlowNodeSO> Nodes => _nodes;

        private void RebuildLookup()
        {
            _lookup = new Dictionary<string, FlowNodeSO>(_nodes.Count);
            foreach (var node in _nodes)
            {
                if (node == null) continue;
                if (string.IsNullOrEmpty(node.NodeId))
                {
                    Debug.LogWarning($"[FlowNodeRegistry] Node '{node.name}' has empty NodeId — skipping.");
                    continue;
                }
                if (_lookup.ContainsKey(node.NodeId))
                {
                    Debug.LogWarning($"[FlowNodeRegistry] Duplicate NodeId '{node.NodeId}' on '{node.name}' — skipping duplicate.");
                    continue;
                }
                _lookup[node.NodeId] = node;
            }
        }

        private void OnEnable()
        {
            _lookup = null; // 强制重建缓存
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _lookup = null;
        }

        /// <summary>Editor-only: 自动收集项目中所有 FlowNodeSO 资产。</summary>
        [ContextMenu("Auto-Collect All FlowNodeSO Assets")]
        private void AutoCollect()
        {
            _nodes.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:FlowNodeSO");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var node = UnityEditor.AssetDatabase.LoadAssetAtPath<FlowNodeSO>(path);
                if (node != null)
                    _nodes.Add(node);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[FlowNodeRegistry] Auto-collected {_nodes.Count} FlowNodeSO assets.");
        }
#endif
    }
}
