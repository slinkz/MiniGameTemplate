using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Entity;
using EntityClass = MiniGameTemplate.Entity.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 底线检测——每帧扫描敌方 Entity，穿过底线则扣基地 HP。
    /// 纯 C# 类（无 MonoBehaviour），由 BattleController 驱动 Tick。
    /// 伤害走 OnCollisionHit 事件，统一伤害入口（闪白/击退/被动均可触发）。
    /// HP 同步由 HealthComponent.OnHpChanged → BattleController.OnBaseHpChanged 自动推送。
    /// TDD_02 §2.2
    /// </summary>
    public class BaseLineDetector
    {
        private float _baseLineY;
        private EntityClass _baseEntity;
        private HealthComponent _baseHealth;

        /// <summary>本帧是否有敌机突破底线</summary>
        public bool HasBreachThisFrame { get; private set; }

        /// <summary>本帧突破底线的敌机数量（BattleController 据此触发反馈）</summary>
        public int BreachCountThisFrame { get; private set; }

        public void Init(float baseLineY, EntityClass baseEntity)
        {
            _baseLineY = baseLineY;
            _baseEntity = baseEntity;
            _baseHealth = baseEntity.GetComponent(ComponentType.Health) as HealthComponent;
        }

        /// <summary>
        /// 每帧由 BattleController 调用（在 EntityManager.Tick 之后）。
        /// 返回基地是否死亡。
        /// 注意：先收集待 Despawn 列表，循环结束后统一 Despawn，
        /// 避免遍历 ActiveEntities 时 swap-remove 导致跳过元素。
        /// </summary>
        private readonly List<EntityClass> _breachedEnemies = new List<EntityClass>(8);

        public bool Tick(EntityManager mgr)
        {
            HasBreachThisFrame = false;
            BreachCountThisFrame = 0;
            _breachedEnemies.Clear();
            var entities = mgr.ActiveEntities;

            // Phase 1: 收集越线敌机
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.IsPendingDespawn) continue;
                if (entity.Camp != EnumCamp.Enemy) continue;

                if (entity.Position.y <= _baseLineY)
                {
                    HasBreachThisFrame = true;
                    BreachCountThisFrame++;
                    _breachedEnemies.Add(entity);

                    // 对基地造成伤害——走 OnCollisionHit 事件统一管线
                    int damage = 15;
                    if (entity.ConfigSO != null)
                    {
                        damage = entity.ConfigSO.BaseLineBreachDamage > 0
                            ? entity.ConfigSO.BaseLineBreachDamage
                            : entity.ConfigSO.ContactDamage;
                    }
                    var ctx = new DamageContext
                    {
                        BaseDamage = damage,
                        AttackerId = entity.Id,
                        HitType = MiniGameTemplate.Entity.CollisionEventType.ContactHit,
                        SourcePosition = entity.Position,
                        HasSourcePosition = true
                    };
                    _baseEntity.EventBus.Publish(new OnCollisionHit { Context = ctx });
                }
            }

            // Phase 2: 统一 Despawn（避免遍历中修改列表）
            for (int i = 0; i < _breachedEnemies.Count; i++)
            {
                mgr.Despawn(_breachedEnemies[i]);
            }

            return _baseHealth.IsDead;
        }
    }
}
