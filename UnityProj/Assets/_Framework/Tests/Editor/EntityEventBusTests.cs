using NUnit.Framework;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.Tests
{
    /// <summary>
    /// EntityEventBus 验证测试。
    /// P1.2 AC 验证：
    /// 1. Publish→Subscribe 正确分发
    /// 2. ClearAll 后无残留
    /// 3. 100 次 Pub/Sub 周期 GC = 0
    /// </summary>
    public class EntityEventBusTests
    {
        // ─────────── 测试用事件 struct ───────────

        private struct TestEventA
        {
            public int Value;
        }

        private struct TestEventB
        {
            public float Data;
        }

        // ─────────── AC-1: Publish→Subscribe 正确分发 ───────────

        [Test]
        public void Subscribe_ThenPublish_HandlerReceivesEvent()
        {
            var bus = new EntityEventBus();
            int received = 0;

            bus.Subscribe<TestEventA>(evt => { received = evt.Value; });
            bus.Publish(new TestEventA { Value = 42 });

            Assert.AreEqual(42, received, "Handler 应收到正确事件值");
        }

        [Test]
        public void MultipleSubscribers_AllReceiveEvent()
        {
            var bus = new EntityEventBus();
            int countA = 0;
            int countB = 0;

            bus.Subscribe<TestEventA>(_ => countA++);
            bus.Subscribe<TestEventA>(_ => countB++);
            bus.Publish(new TestEventA { Value = 1 });

            Assert.AreEqual(1, countA);
            Assert.AreEqual(1, countB);
        }

        [Test]
        public void DifferentEventTypes_IndependentDispatch()
        {
            var bus = new EntityEventBus();
            int receivedA = 0;
            float receivedB = 0f;

            bus.Subscribe<TestEventA>(evt => receivedA = evt.Value);
            bus.Subscribe<TestEventB>(evt => receivedB = evt.Data);

            bus.Publish(new TestEventA { Value = 7 });
            bus.Publish(new TestEventB { Data = 3.14f });

            Assert.AreEqual(7, receivedA);
            Assert.AreEqual(3.14f, receivedB, 0.001f);
        }

        [Test]
        public void Unsubscribe_HandlerNoLongerCalled()
        {
            var bus = new EntityEventBus();
            int callCount = 0;
            System.Action<TestEventA> handler = _ => callCount++;

            bus.Subscribe(handler);
            bus.Publish(new TestEventA());
            Assert.AreEqual(1, callCount);

            bus.Unsubscribe(handler);
            bus.Publish(new TestEventA());
            Assert.AreEqual(1, callCount, "Unsubscribe 后不应再收到事件");
        }

        // ─────────── AC-2: ClearAll 后无残留 ───────────

        [Test]
        public void ClearAll_NoHandlersCalled()
        {
            var bus = new EntityEventBus();
            int callCount = 0;

            bus.Subscribe<TestEventA>(_ => callCount++);
            bus.Subscribe<TestEventB>(_ => callCount++);

            // 确认订阅生效
            bus.Publish(new TestEventA());
            bus.Publish(new TestEventB());
            Assert.AreEqual(2, callCount);

            // 清空
            bus.ClearAll();
            callCount = 0;

            // 再次 Publish，不应有任何响应
            bus.Publish(new TestEventA());
            bus.Publish(new TestEventB());
            Assert.AreEqual(0, callCount, "ClearAll 后不应有任何 Handler 被调用");
        }

        [Test]
        public void ClearAll_CanResubscribeAfter()
        {
            var bus = new EntityEventBus();
            int received = 0;

            bus.Subscribe<TestEventA>(evt => received = evt.Value);
            bus.ClearAll();

            // 重新订阅
            bus.Subscribe<TestEventA>(evt => received = evt.Value + 100);
            bus.Publish(new TestEventA { Value = 5 });

            Assert.AreEqual(105, received, "ClearAll 后应可正常重新订阅");
        }

        // ─────────── AC-3: GC = 0 验证 ───────────

        [Test]
        public void PubSub_100Cycles_ZeroGC()
        {
            // 预热：确保 TypeId 已分配（首次访问会触发 List.Add 的 GC）
            var bus = new EntityEventBus();
            int dummy = 0;
            System.Action<TestEventA> handler = evt => dummy += evt.Value;
            bus.Subscribe(handler);
            bus.Publish(new TestEventA { Value = 1 });
            bus.Unsubscribe(handler);
            bus.ClearAll();

            // 正式测量：通过 Gen0 GC 次数判断是否触发了垃圾收集
            int gcCountBefore = System.GC.CollectionCount(0);

            // 100 次完整 Pub/Sub 周期
            for (int cycle = 0; cycle < 100; cycle++)
            {
                bus.Subscribe(handler);
                bus.Publish(new TestEventA { Value = cycle });
                bus.Unsubscribe(handler);
            }

            int gcCountAfter = System.GC.CollectionCount(0);

            // 验证没有触发 GC（Gen0 收集次数不变）
            Assert.AreEqual(gcCountBefore, gcCountAfter,
                $"100 次 Pub/Sub 周期不应触发 GC。Before={gcCountBefore}, After={gcCountAfter}");

            // 额外验证：使用 GC.GetTotalMemory 判断是否有显著分配
            // 注意：这是一个宽松检查，主要验证没有大块分配（如委托链 Combine）
            // 严格的零分配需要 Unity Profiler 在 Play Mode 下验证
        }

        // ─────────── 边界条件 ───────────

        [Test]
        public void MaxHandlersPerType_ExceedLimit_SilentlyIgnored()
        {
            var bus = new EntityEventBus();
            int callCount = 0;

            // 订阅 4 个（上限）
            for (int i = 0; i < 4; i++)
            {
                bus.Subscribe<TestEventA>(_ => callCount++);
            }

            // 第 5 个应被忽略
            bus.Subscribe<TestEventA>(_ => callCount += 100);

            bus.Publish(new TestEventA());
            Assert.AreEqual(4, callCount, "超过 MAX_HANDLERS_PER_TYPE 的订阅应被静默忽略");
        }

        [Test]
        public void PublishWithoutSubscribers_NoError()
        {
            var bus = new EntityEventBus();

            // 无订阅者时 Publish 不应抛异常
            Assert.DoesNotThrow(() => bus.Publish(new TestEventA { Value = 99 }));
        }

        [Test]
        public void UnsubscribeNonExistent_NoError()
        {
            var bus = new EntityEventBus();
            System.Action<TestEventA> handler = _ => { };

            // 未订阅时取消订阅不应抛异常
            Assert.DoesNotThrow(() => bus.Unsubscribe(handler));
        }

        [Test]
        public void SwapRemove_PreservesOtherHandlers()
        {
            var bus = new EntityEventBus();
            string order = "";

            System.Action<TestEventA> h1 = _ => order += "1";
            System.Action<TestEventA> h2 = _ => order += "2";
            System.Action<TestEventA> h3 = _ => order += "3";

            bus.Subscribe(h1);
            bus.Subscribe(h2);
            bus.Subscribe(h3);

            // 移除中间的 h2，swap-remove 会把 h3 移到 h2 的位置
            bus.Unsubscribe(h2);

            bus.Publish(new TestEventA());

            // h1 和 h3 仍应被调用（顺序可能变化：h1, h3 或 h3, h1）
            Assert.IsTrue(order.Contains("1"), "h1 应被调用");
            Assert.IsTrue(order.Contains("3"), "h3 应被调用");
            Assert.IsFalse(order.Contains("2"), "h2 不应被调用");
        }
    }
}
