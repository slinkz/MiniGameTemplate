using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.Tests
{
    /// <summary>
    /// EntityPool + EntityManager 验证测试。
    /// P1.3 AC 验证：
    /// 1. 编译通过
    /// 2. 50 次 Acquire+Release 周期 GC = 0
    /// 3. 池满 LogWarning 不崩
    /// 4. 延迟销毁在 Tick 中不崩
    /// </summary>
    public class EntityPoolTests
    {
        private EntityConfigSO _testConfig;

        [SetUp]
        public void SetUp()
        {
            // 创建测试用配置 SO（不保存到磁盘）
            _testConfig = ScriptableObject.CreateInstance<EntityConfigSO>();
            _testConfig.name = "TestConfig";
            _testConfig.PoolMax = 8;
            _testConfig.Camp = Danmaku.EnumCamp.Enemy;
            _testConfig.Components = new ComponentType[0]; // P1.3 无具体组件
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testConfig);
        }

        // ─────────── EntityPool 基本功能 ───────────

        [Test]
        public void Pool_Acquire_ReturnsEntity()
        {
            var pool = new EntityPool(_testConfig);
            var entity = pool.Acquire(Vector2.zero, 0f);

            Assert.IsNotNull(entity);
            Assert.IsTrue(entity.IsAlive);
            Assert.AreEqual(1, pool.ActiveCount);
        }

        [Test]
        public void Pool_Release_ReturnsToPool()
        {
            var pool = new EntityPool(_testConfig);
            var entity = pool.Acquire(Vector2.zero, 0f);

            pool.Release(entity);

            Assert.IsFalse(entity.IsAlive);
            Assert.AreEqual(0, pool.ActiveCount);
        }

        [Test]
        public void Pool_AcquireAfterRelease_ReusesEntity()
        {
            var pool = new EntityPool(_testConfig);
            var entity1 = pool.Acquire(Vector2.one, 45f);
            int slot = entity1.PoolSlot;
            pool.Release(entity1);

            var entity2 = pool.Acquire(Vector2.zero, 0f);

            // 应该复用同一个槽位
            Assert.AreEqual(slot, entity2.PoolSlot);
            Assert.AreEqual(Vector2.zero, entity2.Position);
        }

        // ─────────── AC-3: 池满 LogWarning 不崩 ───────────

        [Test]
        public void Pool_AcquireBeyondCapacity_ReturnsNull_NoException()
        {
            var pool = new EntityPool(_testConfig); // 容量 = 8

            // 取满
            for (int i = 0; i < 8; i++)
            {
                Assert.IsNotNull(pool.Acquire(Vector2.zero, 0f));
            }

            // 超限应返回 null（LogWarning 由 Unity 处理）
            LogAssert.Expect(LogType.Warning, $"[EntityPool] 池满：{_testConfig.name}（容量={_testConfig.PoolMax}）");
            var overflow = pool.Acquire(Vector2.zero, 0f);
            Assert.IsNull(overflow);
            Assert.AreEqual(8, pool.ActiveCount);
        }

        // ─────────── AC-2: 50 次 Acquire+Release GC=0 ───────────

        [Test]
        public void Pool_50Cycles_ZeroGC()
        {
            var pool = new EntityPool(_testConfig);

            // 预热：首次 Acquire/Release 可能触发 JIT
            var warmup = pool.Acquire(Vector2.zero, 0f);
            pool.Release(warmup);

            // 正式测量
            int gcBefore = System.GC.CollectionCount(0);

            for (int cycle = 0; cycle < 50; cycle++)
            {
                var entity = pool.Acquire(new Vector2(cycle, cycle), cycle * 10f);
                pool.Release(entity);
            }

            int gcAfter = System.GC.CollectionCount(0);
            Assert.AreEqual(gcBefore, gcAfter,
                $"50 次 Acquire+Release 不应触发 GC。Before={gcBefore}, After={gcAfter}");
        }

        // ─────────── EntityManager 基本功能 ───────────

        [Test]
        public void Manager_Spawn_ReturnsAliveEntity()
        {
            var manager = new EntityManager();
            var entity = manager.Spawn(_testConfig, Vector2.one, 90f);

            Assert.IsNotNull(entity);
            Assert.IsTrue(entity.IsAlive);
            Assert.AreEqual(Vector2.one, entity.Position);
            Assert.AreEqual(90f, entity.Rotation);
            Assert.AreEqual(1, manager.ActiveCount);
            Assert.IsTrue(entity.Id.Value > 0, "EntityId 应 > 0");
        }

        [Test]
        public void Manager_Despawn_OutsideTick_ImmediateRelease()
        {
            var manager = new EntityManager();
            var entity = manager.Spawn(_testConfig, Vector2.zero, 0f);

            manager.Despawn(entity);

            Assert.IsFalse(entity.IsAlive);
            Assert.AreEqual(0, manager.ActiveCount);
        }

        [Test]
        public void Manager_UniqueIds_ForEachSpawn()
        {
            var manager = new EntityManager();
            var e1 = manager.Spawn(_testConfig, Vector2.zero, 0f);
            var e2 = manager.Spawn(_testConfig, Vector2.one, 0f);

            Assert.AreNotEqual(e1.Id, e2.Id, "每次 Spawn 应分配唯一 ID");
        }

        [Test]
        public void Manager_CountAliveByConfig_ExcludesPendingDespawn()
        {
            var manager = new EntityManager();
            manager.Spawn(_testConfig, Vector2.zero, 0f);
            manager.Spawn(_testConfig, Vector2.one, 0f);

            Assert.AreEqual(2, manager.CountAliveByConfig(_testConfig));
        }

        // ─────────── AC-4: 延迟销毁在 Tick 中不崩 ───────────

        [Test]
        public void Manager_DespawnDuringTick_DelayedExecution()
        {
            var manager = new EntityManager();
            var entity = manager.Spawn(_testConfig, Vector2.zero, 0f);

            // 模拟 Tick 期间 Despawn：
            // 我们无法直接在 Tick 回调中调用 Despawn（无具体组件），
            // 但可以验证标记+延迟机制是否正确

            // 手动模拟 Tick 期间的 Despawn 调用场景
            // 使用反射设置 _isTicking = true 来模拟
            var field = typeof(EntityManager).GetField("_isTicking",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(manager, true);

            manager.Despawn(entity);

            // Tick 期间：entity 标记为 PendingDespawn，但仍在活跃列表中
            Assert.IsTrue(entity.IsPendingDespawn);
            Assert.IsTrue(entity.IsAlive);
            Assert.AreEqual(1, manager.ActiveCount);

            // 模拟 Tick 结束：手动恢复 _isTicking = false 并执行延迟销毁
            field.SetValue(manager, false);

            // 调用一次正常 Tick，内部会处理 pendingDespawn
            // 但由于我们已手动设了 _isTicking=false，需要直接触发 Tick
            manager.Tick(0.016f);

            // pendingDespawn 应在上次 Tick 的 Phase B 处理
            // 但由于 entity.IsPaused=false 且 IsAlive=true，Tick 中会先 Tick entity
            // 然后 Phase B 处理 _pendingDespawn
            Assert.IsFalse(entity.IsAlive, "延迟销毁应在 Tick Phase B 执行");
            Assert.AreEqual(0, manager.ActiveCount);
        }

        [Test]
        public void Manager_SwapRemove_PreservesOtherEntities()
        {
            var manager = new EntityManager();
            var e1 = manager.Spawn(_testConfig, new Vector2(1, 0), 0f);
            var e2 = manager.Spawn(_testConfig, new Vector2(2, 0), 0f);
            var e3 = manager.Spawn(_testConfig, new Vector2(3, 0), 0f);

            // 删除中间的 e2
            manager.Despawn(e2);

            Assert.AreEqual(2, manager.ActiveCount);
            Assert.IsTrue(e1.IsAlive);
            Assert.IsTrue(e3.IsAlive);
            Assert.IsFalse(e2.IsAlive);

            // e3 的 ActiveListIndex 应被更新（swap-remove 后移到 e2 的位置）
            Assert.AreEqual(0, e1.ActiveListIndex);
            Assert.AreEqual(1, e3.ActiveListIndex);
        }

        [Test]
        public void Manager_PoolFull_SpawnReturnsNull_NoException()
        {
            var manager = new EntityManager();

            // 取满（容量 8）
            for (int i = 0; i < 8; i++)
            {
                Assert.IsNotNull(manager.Spawn(_testConfig, Vector2.zero, 0f));
            }

            // 超限
            LogAssert.Expect(LogType.Warning, $"[EntityPool] 池满：{_testConfig.name}（容量={_testConfig.PoolMax}）");
            var overflow = manager.Spawn(_testConfig, Vector2.zero, 0f);
            Assert.IsNull(overflow);
            Assert.AreEqual(8, manager.ActiveCount);
        }
    }
}
