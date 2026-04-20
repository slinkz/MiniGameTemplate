using MiniGameTemplate.Danmaku;
using NUnit.Framework;
using UnityEngine;

namespace MiniGameTemplate.Tests.Editor
{
    public class CollisionEventBufferTests
    {
        [Test]
        public void TryWrite_WhenCapacityExceeded_IncrementsOverflowCount()
        {
            var buffer = new CollisionEventBuffer(2);
            var evt = CreateEvent();

            Assert.That(buffer.TryWrite(ref evt), Is.True);
            Assert.That(buffer.TryWrite(ref evt), Is.True);
            Assert.That(buffer.TryWrite(ref evt), Is.False);
            Assert.That(buffer.TryWrite(ref evt), Is.False);

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer.OverflowCount, Is.EqualTo(2));
        }

        [Test]
        public void Reset_ClearsCountAndOverflowCount()
        {
            var buffer = new CollisionEventBuffer(1);
            var evt = CreateEvent();

            buffer.TryWrite(ref evt);
            buffer.TryWrite(ref evt);
            buffer.Reset();

            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.OverflowCount, Is.EqualTo(0));
            Assert.That(buffer.AsReadOnlySpan().Length, Is.EqualTo(0));
        }

        [Test]
        public void TryWrite_WithZeroCapacity_OnlyAccumulatesOverflow()
        {
            var buffer = new CollisionEventBuffer(0);
            var evt = CreateEvent();

            Assert.That(buffer.TryWrite(ref evt), Is.False);
            Assert.That(buffer.TryWrite(ref evt), Is.False);
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.OverflowCount, Is.EqualTo(2));
        }

        [Test]
        public void AsReadOnlySpan_ReturnsWrittenEventsInOrder()
        {
            var buffer = new CollisionEventBuffer(2);
            var first = CreateEvent();
            var second = CreateEvent();
            second.BulletIndex = 99;
            second.Damage = 42;

            buffer.TryWrite(ref first);
            buffer.TryWrite(ref second);

            var span = buffer.AsReadOnlySpan();
            Assert.That(span.Length, Is.EqualTo(2));
            Assert.That(span[0].BulletIndex, Is.EqualTo(1));
            Assert.That(span[1].BulletIndex, Is.EqualTo(99));
            Assert.That(span[1].Damage, Is.EqualTo(42));
        }

        [Test]
        public void Reset_AfterReuse_WritesNormally()
        {
            var buffer = new CollisionEventBuffer(1);
            var first = CreateEvent();
            var second = CreateEvent();
            second.BulletIndex = 7;

            buffer.TryWrite(ref first);
            buffer.TryWrite(ref first);
            buffer.Reset();

            Assert.That(buffer.TryWrite(ref second), Is.True);
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.OverflowCount, Is.EqualTo(0));
            Assert.That(buffer.AsReadOnlySpan()[0].BulletIndex, Is.EqualTo(7));
        }

        private static CollisionEvent CreateEvent()

        {
            return new CollisionEvent
            {
                BulletIndex = 1,
                TargetSlot = 2,
                Position = new Vector2(3f, 4f),
                Damage = 5,
                SourceFaction = BulletFaction.Player,
                TargetFaction = BulletFaction.Enemy,
                EventType = CollisionEventType.BulletHit,
            };
        }
    }
}
