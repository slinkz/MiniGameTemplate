using System.Collections.Generic;
using UnityEngine;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// Editor 模式弹幕模拟器（TDD_05 S5.1）。
    /// 纯数据模拟，不创建 GameObject。供 SkillPreviewWindow 在 Scene View 中可视化。
    /// </summary>
    public class EditorBulletSimulator
    {
        public const int MAX_SIM_BULLETS = 500; // PK-R2 ET-014 性能护栏

        public struct SimBullet
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Lifetime;
            public float Age;
            public Color Color;
            public bool IsHoming;
            public bool IsPiercing;
            public float HomingTurnRate;
        }

        public List<SimBullet> Bullets = new();
        public bool ReachedCapacity { get; private set; }

        /// <summary>虚拟敌人位置（追踪弹目标）</summary>
        public Vector2 EnemyPosition = new(0, 5);

        public void Clear()
        {
            Bullets.Clear();
            ReachedCapacity = false;
        }

        /// <summary>
        /// 根据 BulletPatternSO 生成模拟弹丸。
        /// </summary>
        public void Spawn(MiniGameTemplate.Danmaku.BulletPatternSO pattern, Vector2 origin, float aimAngleDeg,
                          bool isPiercing = false, bool isHoming = false, float homingTurnRate = 90f)
        {
            if (pattern == null) return;

            int count = pattern.Count;
            float spreadAngle = pattern.SpreadAngle;
            float startAngle = pattern.StartAngle + aimAngleDeg;
            float speed = pattern.Speed;
            float lifetime = pattern.Lifetime;

            // 计算每发角度间隔
            float angleStep = count > 1 ? spreadAngle / count : 0f;
            float halfSpread = spreadAngle * 0.5f;
            float firstAngle = count > 1 ? startAngle - halfSpread + angleStep * 0.5f : startAngle;

            Color bulletColor = isHoming ? new Color(1f, 0.5f, 0f) : Color.yellow;
            if (isPiercing) bulletColor = Color.cyan;

            for (int i = 0; i < count; i++)
            {
                if (Bullets.Count >= MAX_SIM_BULLETS)
                {
                    ReachedCapacity = true;
                    return;
                }

                float angle = firstAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));

                Bullets.Add(new SimBullet
                {
                    Position = origin,
                    Velocity = dir * speed,
                    Lifetime = lifetime,
                    Age = 0f,
                    Color = bulletColor,
                    IsHoming = isHoming,
                    IsPiercing = isPiercing,
                    HomingTurnRate = homingTurnRate,
                });
            }
        }

        /// <summary>
        /// 推进一个时间步。
        /// </summary>
        public void Tick(float dt)
        {
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                var b = Bullets[i];
                b.Age += dt;

                if (b.Age >= b.Lifetime)
                {
                    Bullets.RemoveAt(i);
                    continue;
                }

                // 追踪弹：朝虚拟敌人偏转
                if (b.IsHoming)
                {
                    Vector2 toTarget = (EnemyPosition - b.Position).normalized;
                    Vector2 currentDir = b.Velocity.normalized;
                    float speed = b.Velocity.magnitude;
                    float maxTurn = b.HomingTurnRate * dt * Mathf.Deg2Rad;

                    float currentAngle = Mathf.Atan2(currentDir.y, currentDir.x);
                    float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x);
                    float angleDiff = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, targetAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    float clampedTurn = Mathf.Clamp(angleDiff, -maxTurn, maxTurn);
                    float newAngle = currentAngle + clampedTurn;

                    b.Velocity = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * speed;
                }

                b.Position += b.Velocity * dt;
                Bullets[i] = b;
            }
        }
    }
}
