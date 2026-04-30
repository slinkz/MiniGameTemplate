using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// ConfigId ↔ EntityConfigSO 运行时注册表。
    /// P2.3: 提供 Luban configId 到 SO 的 O(1) 查找。
    /// 
    /// 注册方式：
    /// - 自动：EntitySystemBootstrap.Awake 时扫描所有 EntityConfigSO 资产
    /// - 手动：Register(EntityConfigSO) / Unregister(int configId)
    /// 
    /// 设计决策：
    /// - 不使用 ScriptableObject 存储注册表（SO 无法安全引用自身列表）
    /// - 使用 Dictionary 而非数组（ConfigId 不保证连续）
    /// - ConfigId=0 视为"未配置"，不参与注册
    /// </summary>
    public static class EntityConfigRegistry
    {
        private static readonly Dictionary<int, EntityConfigSO> _map = new(32);

        /// <summary>当前已注册的配置数量</summary>
        public static int Count => _map.Count;

        /// <summary>
        /// 注册一个 EntityConfigSO。ConfigId=0 的 SO 将被跳过（未配置 Luban ID）。
        /// 重复注册同一 ConfigId 会覆盖并警告。
        /// </summary>
        public static void Register(EntityConfigSO config)
        {
            if (config == null) return;
            if (config.ConfigId == 0) return; // 未配置 Luban ID，跳过

            if (_map.TryGetValue(config.ConfigId, out var existing))
            {
                if (existing != config)
                {
                    Debug.LogWarning(
                        $"[EntityConfigRegistry] ConfigId={config.ConfigId} 重复注册！" +
                        $" 旧={existing.name}，新={config.name}。将覆盖。");
                }
            }
            _map[config.ConfigId] = config;
        }

        /// <summary>
        /// 批量注册。通常在 Bootstrap 时一次性注册所有 SO。
        /// </summary>
        public static void RegisterAll(IEnumerable<EntityConfigSO> configs)
        {
            foreach (var config in configs)
                Register(config);
        }

        /// <summary>
        /// 通过 ConfigId 查找 EntityConfigSO。O(1)。
        /// </summary>
        /// <returns>找到的 SO，或 null</returns>
        public static EntityConfigSO Get(int configId)
        {
            _map.TryGetValue(configId, out var config);
            return config;
        }

        /// <summary>
        /// 通过 ConfigId 查找，找不到则报错。
        /// </summary>
        public static EntityConfigSO GetOrThrow(int configId)
        {
            if (_map.TryGetValue(configId, out var config))
                return config;

            Debug.LogError($"[EntityConfigRegistry] ConfigId={configId} 未注册！请检查 EntityConfigSO 是否已赋值 ConfigId。");
            return null;
        }

        /// <summary>注销指定 ConfigId</summary>
        public static void Unregister(int configId)
        {
            _map.Remove(configId);
        }

        /// <summary>清空注册表（场景卸载时调用）</summary>
        public static void Clear()
        {
            _map.Clear();
        }

        /// <summary>只读访问（Editor 调试用）</summary>
        public static IReadOnlyDictionary<int, EntityConfigSO> All => _map;
    }
}
