using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 碰撞事件结构体——旁路表现通道的单条事件记录。
    /// 用于 VFX、飘字、统计等非主逻辑消费。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CollisionEvent
    {
        /// <summary>弹丸/激光/喷雾在各自池中的索引</summary>
        public int BulletIndex;

        /// <summary>被命中目标在 TargetRegistry 中的槽位索引</summary>
        public int TargetSlot;

        /// <summary>碰撞发生位置（世界坐标）</summary>
        public Vector2 Position;

        /// <summary>造成的伤害值</summary>
        public int Damage;

        /// <summary>弹丸/激光/喷雾的阵营</summary>
        public BulletFaction SourceFaction;

        /// <summary>目标的阵营</summary>
        public BulletFaction TargetFaction;

        /// <summary>碰撞事件类型</summary>
        public CollisionEventType EventType;
    }

    /// <summary>
    /// 零 GC 碰撞事件缓冲区。
    /// 预分配数组，旁路表现通道——溢出只记计数不报错，不影响主逻辑（伤害/击退/死亡）。
    /// 每帧帧末 Reset，由 VFX/飘字/统计系统消费。
    /// </summary>
    public class CollisionEventBuffer
    {
        private readonly CollisionEvent[] _events;
        private int _count;
        private int _overflowCount;

        /// <summary>当前帧已写入的事件数量</summary>
        public int Count => _count;

        /// <summary>缓冲区容量</summary>
        public int Capacity => _events.Length;

        /// <summary>当前帧溢出（被丢弃）的事件数量</summary>
        public int OverflowCount => _overflowCount;

        public CollisionEventBuffer(int capacity = 256)
        {
            _events = new CollisionEvent[capacity];
            _count = 0;
            _overflowCount = 0;
        }

        /// <summary>
        /// 尝试写入一条碰撞事件。
        /// Buffer 满时不报错，仅递增溢出计数。
        /// </summary>
        /// <returns>true 写入成功，false 缓冲区已满</returns>
        public bool TryWrite(ref CollisionEvent evt)
        {
            if (_count >= _events.Length)
            {
                _overflowCount++;
                return false;
            }
            _events[_count++] = evt;
            return true;
        }

        /// <summary>
        /// 获取当前帧所有事件的只读 Span。
        /// </summary>
        public ReadOnlySpan<CollisionEvent> AsReadOnlySpan()
        {
            return new ReadOnlySpan<CollisionEvent>(_events, 0, _count);
        }

        /// <summary>
        /// 帧末重置——清零计数，不清数组内容（下一帧覆盖写入）。
        /// </summary>
        public void Reset()
        {
            _count = 0;
            _overflowCount = 0;
        }
    }
}
