using NUnit.Framework;
using UnityEngine;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;

namespace MiniGameFramework.Tests.Editor
{
    /// <summary>
    /// EntityCollisionSolver 单元测试（P2.2）。
    /// 测试覆盖：圆vs圆碰撞、阵营过滤、推力分离、接触伤害、冷却机制、碰撞层过滤。
    /// </summary>
    [TestFixture]
    public class EntityCollisionSolverTests
    {
        private EntityManager _manager;
        private EntityCollisionSolver _solver;

        // ──────────── 测试辅助 ────────────

        private EntityConfigSO CreateConfig(string name, EnumCamp camp, float radius = 0.5f,
            int contactDamage = 0, float contactInterval = 0.5f, int collisionLayer = 0)
        {
            var config = ScriptableObject.CreateInstance<EntityConfigSO>();
            config.name = name;
            config.Camp = camp;
            config.CollisionRadius = radius;
            config.MaxHp = 100;
            config.MoveSpeed = 3f;
            config.PoolMax = 4;
            config.EnableEntityCollision = true;
            config.CollisionLayer = collisionLayer;
            config.ContactDamage = contactDamage;
            config.ContactDamageInterval = contactInterval;

            // 最小组件集：Movement + Health
            config.Components = new[] { ComponentType.Movement, ComponentType.Health };
            return config;
        }

        [SetUp]
        public void SetUp()
        {
            _manager = new EntityManager();
            _solver = new EntityCollisionSolver();
        }

        [TearDown]
        public void TearDown()
        {
            _manager.DespawnAll();
            _solver.ClearCooldowns();
        }

        // ──────────── 基础碰撞检测 ────────────

        [Test]
        public void Overlapping_DifferentCamp_Entities_Detected()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f);
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f);

            // 两个 Entity 放在相同位置（完全重叠）
            _manager.Spawn(cfgP, Vector2.zero, 0f);
            _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(1, _solver.PairCount, "应检测到 1 个碰撞对");
        }

        [Test]
        public void NonOverlapping_Entities_Not_Detected()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f);
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f);

            // 两个 Entity 间距 > 两半径之和
            _manager.Spawn(cfgP, Vector2.zero, 0f);
            _manager.Spawn(cfgE, new Vector2(2f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(0, _solver.PairCount, "不应检测到碰撞");
        }

        [Test]
        public void SameCamp_Entities_NotCollide()
        {
            var cfgE1 = CreateConfig("Enemy1", EnumCamp.Enemy, 0.5f);
            var cfgE2 = CreateConfig("Enemy2", EnumCamp.Enemy, 0.5f);

            _manager.Spawn(cfgE1, Vector2.zero, 0f);
            _manager.Spawn(cfgE2, new Vector2(0.3f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(0, _solver.PairCount, "同阵营不应碰撞");
        }

        // ──────────── 推力分离 ────────────

        [Test]
        public void Separation_PushesEntities_Apart()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f);
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f);

            var player = _manager.Spawn(cfgP, Vector2.zero, 0f);
            var enemy = _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f); // overlap = 0.5

            float distBefore = Vector2.Distance(player.Position, enemy.Position);
            _solver.Solve(_manager, 0.016f);
            float distAfter = Vector2.Distance(player.Position, enemy.Position);

            Assert.Greater(distAfter, distBefore, "分离后距离应增大");
        }

        [Test]
        public void Separation_EqualPush_BothDirections()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f);
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f);

            _manager.Spawn(cfgP, new Vector2(-0.25f, 0f), 0f);
            _manager.Spawn(cfgE, new Vector2(0.25f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            var entities = _manager.ActiveEntities;
            // Player 应向左推，Enemy 应向右推（等量分离）
            Assert.Less(entities[0].Position.x, -0.25f, "Player 应向左推");
            Assert.Greater(entities[1].Position.x, 0.25f, "Enemy 应向右推");
        }

        // ──────────── 接触伤害 ────────────

        [Test]
        public void ContactDamage_AppliedOnCollision()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f, 10);
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f, 5);

            var player = _manager.Spawn(cfgP, Vector2.zero, 0f);
            var enemy = _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            var playerHp = player.GetComponent(ComponentType.Health) as HealthComponent;
            var enemyHp = enemy.GetComponent(ComponentType.Health) as HealthComponent;

            Assert.AreEqual(95, playerHp.CurrentHp, "Player 应受到 Enemy 的 5 接触伤害");
            Assert.AreEqual(90, enemyHp.CurrentHp, "Enemy 应受到 Player 的 10 接触伤害");
        }

        [Test]
        public void ContactDamage_Cooldown_PreventsRepeatDamage()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f, 10, 1f); // 1s 冷却
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f, 0); // 不造成伤害

            var player = _manager.Spawn(cfgP, Vector2.zero, 0f);
            var enemy = _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            // 第一帧：造成伤害
            _solver.Solve(_manager, 0.016f);
            var enemyHp = enemy.GetComponent(ComponentType.Health) as HealthComponent;
            int hpAfterFirst = enemyHp.CurrentHp;

            // 分离后把它们推回去
            enemy.Position = new Vector2(0.5f, 0f);
            player.Position = Vector2.zero;

            // 第二帧（冷却中）：不应造成伤害
            _solver.Solve(_manager, 0.016f);
            Assert.AreEqual(hpAfterFirst, enemyHp.CurrentHp, "冷却期内不应再次造成伤害");
        }

        [Test]
        public void ContactDamage_Zero_DoesNothing()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f, 0); // 无接触伤害
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f, 0);

            var player = _manager.Spawn(cfgP, Vector2.zero, 0f);
            var enemy = _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            var playerHp = player.GetComponent(ComponentType.Health) as HealthComponent;
            var enemyHp = enemy.GetComponent(ComponentType.Health) as HealthComponent;

            Assert.AreEqual(100, playerHp.CurrentHp, "无接触伤害配置时 HP 不变");
            Assert.AreEqual(100, enemyHp.CurrentHp, "无接触伤害配置时 HP 不变");
        }

        // ──────────── 碰撞层 ────────────

        [Test]
        public void CollisionLayer_SameNonZero_Collides()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f, 0, 0.5f, 1);
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f, 0, 0.5f, 1);

            _manager.Spawn(cfgP, Vector2.zero, 0f);
            _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(1, _solver.PairCount, "相同碰撞层应碰撞");
        }

        [Test]
        public void CollisionLayer_DifferentNonZero_NoCollision()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f, 0, 0.5f, 1);
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f, 0, 0.5f, 2);

            _manager.Spawn(cfgP, Vector2.zero, 0f);
            _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(0, _solver.PairCount, "不同碰撞层不应碰撞");
        }

        [Test]
        public void CollisionLayer_ZeroDefault_CollideWithAll()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f, 0, 0.5f, 0); // 默认层
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f, 0, 0.5f, 5);

            _manager.Spawn(cfgP, Vector2.zero, 0f);
            _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(1, _solver.PairCount, "默认层（0）应与任何层碰撞");
        }

        // ──────────── 禁用碰撞 ────────────

        [Test]
        public void DisabledEntityCollision_SkipsEntity()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f);
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f);
            cfgE.EnableEntityCollision = false; // 关闭

            _manager.Spawn(cfgP, Vector2.zero, 0f);
            _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(0, _solver.PairCount, "禁用碰撞的 Entity 不参与碰撞");
        }

        // ──────────── 边界场景 ────────────

        [Test]
        public void SingleEntity_NoPairs()
        {
            var cfg = CreateConfig("Solo", EnumCamp.Player, 0.5f);
            _manager.Spawn(cfg, Vector2.zero, 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(0, _solver.PairCount, "单个 Entity 不产生碰撞对");
        }

        [Test]
        public void ClearCooldowns_ResetsState()
        {
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f, 10, 10f); // 10s 长冷却
            var cfgE = CreateConfig("Enemy", EnumCamp.Enemy, 0.5f, 0);

            var player = _manager.Spawn(cfgP, Vector2.zero, 0f);
            var enemy = _manager.Spawn(cfgE, new Vector2(0.5f, 0f), 0f);

            // 第一次碰撞
            _solver.Solve(_manager, 0.016f);
            var enemyHp = enemy.GetComponent(ComponentType.Health) as HealthComponent;
            Assert.AreEqual(90, enemyHp.CurrentHp);

            // 清除冷却
            _solver.ClearCooldowns();

            // 把它们推回去
            player.Position = Vector2.zero;
            enemy.Position = new Vector2(0.5f, 0f);

            // 应该再次造成伤害
            _solver.Solve(_manager, 0.016f);
            Assert.AreEqual(80, enemyHp.CurrentHp, "清除冷却后应再次造成伤害");
        }

        [Test]
        public void Neutral_Camp_CollidesWithAll()
        {
            var cfgN = CreateConfig("Neutral", EnumCamp.Neutral, 0.5f);
            var cfgP = CreateConfig("Player", EnumCamp.Player, 0.5f);

            _manager.Spawn(cfgN, Vector2.zero, 0f);
            _manager.Spawn(cfgP, new Vector2(0.5f, 0f), 0f);

            _solver.Solve(_manager, 0.016f);

            Assert.AreEqual(1, _solver.PairCount, "Neutral 应与任何阵营碰撞");
        }
    }
}
