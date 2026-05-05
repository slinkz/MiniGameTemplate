using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// 栈序列化工具类。将 AppFlowNavigator 栈序列化为 JSON 字符串，
    /// 或从 JSON 反序列化恢复栈。用于微信热启动恢复。（Phase 4）
    /// 
    /// 格式：{ "version": 1, "entries": [{ "nodeId": "xxx", "dataType": "...", "dataJson": "{...}" }] }
    /// </summary>
    public static class FlowStackSerializer
    {
        private const int CURRENT_VERSION = 1;

        [Serializable]
        private struct SerializedStack
        {
            public int version;
            public SerializedEntry[] entries;
        }

        [Serializable]
        private struct SerializedEntry
        {
            public string nodeId;
            public string dataType;
            public string dataJson;
        }

        /// <summary>
        /// 将当前导航栈序列化为 JSON 字符串。
        /// </summary>
        public static string SerializeStack(IReadOnlyList<AppFlowNavigator.StackEntry> stack)
        {
            var entries = new SerializedEntry[stack.Count];
            for (int i = 0; i < stack.Count; i++)
            {
                var entry = stack[i];
                entries[i] = new SerializedEntry
                {
                    nodeId = entry.Node != null ? entry.Node.NodeId : "",
                    dataType = entry.Data != null ? entry.Data.GetType().AssemblyQualifiedName : "",
                    dataJson = entry.Data != null ? JsonUtility.ToJson(entry.Data) : ""
                };
            }

            var serialized = new SerializedStack
            {
                version = CURRENT_VERSION,
                entries = entries
            };

            return JsonUtility.ToJson(serialized);
        }

        /// <summary>
        /// 从 JSON 反序列化栈。失败时返回 null。
        /// </summary>
        /// <param name="json">序列化的 JSON 字符串</param>
        /// <param name="registry">FlowNodeRegistry 用于 nodeId → SO 映射</param>
        /// <returns>恢复的栈条目列表，或 null（版本不匹配/解析失败）</returns>
        public static List<AppFlowNavigator.StackEntry> DeserializeStack(string json, FlowNodeRegistry registry)
        {
            if (string.IsNullOrEmpty(json)) return null;
            if (registry == null) return null;

            SerializedStack serialized;
            try
            {
                serialized = JsonUtility.FromJson<SerializedStack>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FlowStackSerializer] JSON parse failed: {ex.Message}");
                return null;
            }

            // 版本兼容检查
            if (serialized.version != CURRENT_VERSION)
            {
                Debug.LogWarning($"[FlowStackSerializer] Version mismatch: expected {CURRENT_VERSION}, got {serialized.version}. Discarding.");
                return null;
            }

            if (serialized.entries == null || serialized.entries.Length == 0)
                return null;

            var result = new List<AppFlowNavigator.StackEntry>(serialized.entries.Length);

            for (int i = 0; i < serialized.entries.Length; i++)
            {
                var se = serialized.entries[i];

                // nodeId → SO
                var node = registry.GetByNodeId(se.nodeId);
                if (node == null)
                {
                    Debug.LogWarning($"[FlowStackSerializer] NodeId '{se.nodeId}' not found in registry. " +
                        "Discarding this entry and all above.");
                    break; // 丢弃该层级以上所有栈帧
                }

                // Data 反序列化
                IFlowData data = null;
                if (!string.IsNullOrEmpty(se.dataType) && !string.IsNullOrEmpty(se.dataJson))
                {
                    try
                    {
                        var type = Type.GetType(se.dataType);
                        if (type != null)
                        {
                            var obj = JsonUtility.FromJson(se.dataJson, type);
                            data = obj as IFlowData;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[FlowStackSerializer] Data deserialization failed for node '{se.nodeId}': {ex.Message}. Using null data.");
                    }
                }

                result.Add(new AppFlowNavigator.StackEntry { Node = node, Data = data });
            }

            return result.Count > 0 ? result : null;
        }
    }
}
