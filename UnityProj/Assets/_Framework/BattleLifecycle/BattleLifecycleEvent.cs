using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Battle
{
    /// <summary>
    /// SO 事件通道——战斗退场生命周期事件。
    /// 所有需要在退场时清理的系统，通过 Register/Unregister 自行注册。
    /// Raise() 时按 CleanupOrder 顺序逐一通知。
    /// TDD-07 §3.1
    /// </summary>
    [CreateAssetMenu(menuName = "ShooterGame/Events/Battle Lifecycle Event")]
    public class BattleLifecycleEvent : ScriptableObject
    {
        private readonly List<IBattleCleanup> _listeners = new List<IBattleCleanup>(16);
        private bool _isBroadcasting;
        private readonly List<IBattleCleanup> _pendingRemoval = new List<IBattleCleanup>(4);

        /// <summary>当前注册的监听者数量（调试/验证用）。</summary>
        public int ListenerCount => _listeners.Count;

        // ── UT-004: Domain Reload 关闭时清空残留监听者 ──
#if UNITY_EDITOR
        private static readonly List<BattleLifecycleEvent> s_allInstances = new List<BattleLifecycleEvent>();

        private void OnEnable() => s_allInstances.Add(this);
        private void OnDisable() => s_allInstances.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAllListeners()
        {
            foreach (var inst in s_allInstances)
                inst._listeners.Clear();
        }
#endif

        // WX-002: 静态委托缓存——确保 IL2CPP 下无重复分配
        private static readonly System.Comparison<IBattleCleanup> s_orderComparer =
            (a, b) => a.CleanupOrder.CompareTo(b.CleanupOrder);

        /// <summary>
        /// 注册清理监听者。重复注册会被忽略。
        /// </summary>
        public void Register(IBattleCleanup listener)
        {
            // WX-002: 用 ReferenceEquals 绕过 Unity 重载 ==，避免 native interop 开销
            for (int i = 0; i < _listeners.Count; i++)
            {
                if (ReferenceEquals(_listeners[i], listener))
                    return;
            }
            _listeners.Add(listener);
            // UT-007: n ≤ 10，Sort 开销可忽略。如 listener 数显著增长（>50）改为 lazy sort
            _listeners.Sort(s_orderComparer);
        }

        /// <summary>
        /// 注销清理监听者。广播期间延迟移除。
        /// </summary>
        public void Unregister(IBattleCleanup listener)
        {
            // UT-010: 广播期间延迟移除，避免遍历时修改列表
            if (_isBroadcasting)
                _pendingRemoval.Add(listener);
            else
                _listeners.Remove(listener);
        }

        /// <summary>
        /// 广播退场事件。所有注册者按 CleanupOrder 顺序执行清理。
        /// 单个系统异常不阻塞后续清理。
        /// </summary>
        public void Raise()
        {
            _isBroadcasting = true;
            for (int i = 0; i < _listeners.Count; i++)
            {
                try
                {
                    _listeners[i].OnBattleCleanup();
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    // 继续执行后续清理，不因一个系统异常阻塞全链
                }
            }
            _isBroadcasting = false;

            // UT-010: 处理广播期间的延迟移除
            if (_pendingRemoval.Count > 0)
            {
                for (int i = 0; i < _pendingRemoval.Count; i++)
                    _listeners.Remove(_pendingRemoval[i]);
                _pendingRemoval.Clear();
            }
        }

#if UNITY_EDITOR
        /// <summary>编辑器用：获取所有监听者名称（调试）。</summary>
        public string[] GetListenerNames()
        {
            var names = new string[_listeners.Count];
            for (int i = 0; i < _listeners.Count; i++)
                names[i] = $"[{_listeners[i].CleanupOrder}] {_listeners[i].GetType().Name}";
            return names;
        }
#endif
    }
}
