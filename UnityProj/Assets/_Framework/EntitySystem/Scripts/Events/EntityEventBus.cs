using System;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 零 GC 实体本地事件总线。
    /// 每个 Entity 独立实例，事件不跨 Entity 传播（BC-03.1）。
    /// 
    /// 实现要点：
    /// - TypeId&lt;T&gt; 通过泛型静态字段 + 懒初始化递增分配，O(1) 类型分发
    /// - Handler 存储用预分配二维数组替代 Delegate.Combine，避免委托链 GC
    /// - Reset 时自动清空所有订阅（防池化后事件泄漏，BC-03.4）
    /// </summary>
    public sealed class EntityEventBus
    {
        private const int MAX_EVENT_TYPES = 16;
        private const int MAX_HANDLERS_PER_TYPE = 4;

        // 二维预分配数组：[eventTypeId][handlerSlot]
        private readonly Delegate[,] _handlers = new Delegate[MAX_EVENT_TYPES, MAX_HANDLERS_PER_TYPE];
        private readonly int[] _handlerCounts = new int[MAX_EVENT_TYPES];

        /// <summary>
        /// 发布事件。所有订阅该类型的 Handler 同步收到回调。
        /// </summary>
        public void Publish<T>(T evt) where T : struct
        {
            int typeId = TypeId<T>.Get();
            if (typeId >= MAX_EVENT_TYPES) return;
            int count = _handlerCounts[typeId];
            for (int i = 0; i < count; i++)
            {
                ((Action<T>)_handlers[typeId, i])?.Invoke(evt);
            }
        }

        /// <summary>
        /// 订阅事件。每种事件类型最多 4 个订阅者，超限静默丢弃（开发期可 LogWarning）。
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            int typeId = TypeId<T>.Get();
            if (typeId >= MAX_EVENT_TYPES) return;
            int count = _handlerCounts[typeId];
            if (count >= MAX_HANDLERS_PER_TYPE)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning(
                    $"[EntityEventBus] 事件 {typeof(T).Name} 订阅者已满（{MAX_HANDLERS_PER_TYPE}），新订阅被忽略。");
#endif
                return;
            }
            _handlers[typeId, count] = handler;
            _handlerCounts[typeId] = count + 1;
        }

        /// <summary>
        /// 取消订阅事件。使用 swap-remove 保持数组紧凑。
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            int typeId = TypeId<T>.Get();
            if (typeId >= MAX_EVENT_TYPES) return;
            int count = _handlerCounts[typeId];
            for (int i = 0; i < count; i++)
            {
                if (_handlers[typeId, i] == (Delegate)handler)
                {
                    // swap-remove
                    _handlers[typeId, i] = _handlers[typeId, count - 1];
                    _handlers[typeId, count - 1] = null;
                    _handlerCounts[typeId] = count - 1;
                    return;
                }
            }
        }

        /// <summary>
        /// 清空所有订阅（Reset 时调用，防池化后事件泄漏）。
        /// </summary>
        public void ClearAll()
        {
            Array.Clear(_handlers, 0, _handlers.Length);
            Array.Clear(_handlerCounts, 0, _handlerCounts.Length);
        }
    }

    /// <summary>
    /// 泛型事件类型 ID 分配器。利用泛型静态字段实现自动递增。
    /// IL2CPP/AOT 安全——每个 T 的静态字段在首次访问时初始化。
    /// 
    /// v2.3（SA-004）：从 static readonly 改为 static int + 懒初始化，
    /// 解决 Domain Reload 后 TypeId 乱序导致 EventBus 事件分发错误。
    /// </summary>
    internal static class TypeId<T> where T : struct
    {
        private static int _value = -1; // -1 = 未分配

        public static int Get()
        {
            if (_value < 0)
            {
                _value = TypeIdCounter.Next();
                TypeIdCounter.RegisterResetCallback(() => _value = -1);
            }
            return _value;
        }
    }

    /// <summary>
    /// TypeId 全局递增计数器。
    /// Domain Reload 时重置所有已分配的 TypeId。
    /// </summary>
    internal static class TypeIdCounter
    {
        private static int _next;
        private static readonly System.Collections.Generic.List<Action> _resetCallbacks = new();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _next = 0;
            for (int i = 0; i < _resetCallbacks.Count; i++)
                _resetCallbacks[i]?.Invoke();
            _resetCallbacks.Clear();
        }

        public static int Next()
        {
            return System.Threading.Interlocked.Increment(ref _next) - 1;
        }

        public static void RegisterResetCallback(Action callback)
        {
            _resetCallbacks.Add(callback);
        }
    }
}
