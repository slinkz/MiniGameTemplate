using MiniGameTemplate.Rendering;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    // DanmakuSystem.UpdatePipeline.cs — Update/LateUpdate 内的逐步驱动逻辑
    public partial class DanmakuSystem
    {
        private void RunUpdatePipeline()
        {
            float dt = Time.deltaTime;
            if (_timeScale != null)
                dt *= _timeScale.TimeScale;

            // VFX 时间缩放联动（Phase 3 — 3.8）
            _vfxRuntime?.SetTimeScale(_timeScale != null ? _timeScale.TimeScale : 1f);


            // 1. 发射器驱动 Tick（SpawnerDriver 自动发射）
            _spawnerDriver.Tick(dt, this);

            // 2. 调度器 Tick（触发到期的发射任务）
            _scheduler.Tick(dt, _bulletWorld, _typeRegistry, _difficulty, _trailPool);

            // 3. 弹丸运动
            Vector2 playerPos = _builtinPlayerTarget != null
                ? _builtinPlayerTarget.Hitbox.Center
                : Vector2.zero;
            BulletMover.UpdateAll(_bulletWorld, _typeRegistry, playerPos, dt, this, _trailPool);

            // 4. 激光更新（含挂载同步 + 折射段解算）
            LaserUpdater.UpdateAll(_laserPool, _typeRegistry, _obstaclePool,
                _attachRegistry, _worldConfig.WorldBounds, dt);

            // 5. 喷雾更新（含挂载同步 + VFX 附着）
            SprayUpdater.UpdateAll(_sprayPool, _attachRegistry, _typeRegistry, _vfxRuntime, dt);


            // 6. 碰撞检测
            // 无敌帧递减
            if (_invincibleTimer > 0)
            {
                _invincibleTimer -= Time.unscaledDeltaTime;
            }

            var result = _collisionSolver.SolveAll(
                _bulletWorld, _laserPool, _sprayPool, _obstaclePool,
                _typeRegistry, _attachRegistry, _targetRegistry, _vfxRuntime, dt);

            // 7. 处理碰撞结果——仅在 Player 阵营目标被命中时触发事件 / 无敌帧 / 飘字
            if (result.PlayerHit && _invincibleTimer <= 0)
            {
                if (_onPlayerHit != null)
                    _onPlayerHit.Raise();
                if (result.PlayerDamage > 0 && _onDamageDealt != null)
                    _onDamageDealt.Raise(result.PlayerDamage);

                _damageNumbers.Spawn(result.PlayerHitPosition, result.PlayerDamage, result.PlayerDamage >= 10);

                if (_worldConfig.InvincibleDuration > 0)
                    _invincibleTimer = _worldConfig.InvincibleDuration;
            }

            // 8. 特效桥接——消费碰撞事件 Buffer
            _effectsBridge?.OnCollisionEventsReady(_collisionEventBuffer);

            // 9. 帧末 Reset Buffer
            _collisionEventBuffer.Reset();
        }

        private void RunLateUpdatePipeline()
        {
            float dt = Time.deltaTime;
            if (_timeScale != null)
                dt *= _timeScale.TimeScale;

            // 渲染统计帧开始
            RenderBatchManagerRuntimeStats.BeginFrame();

            // 渲染
            _bulletRenderer.Rebuild(_bulletWorld, _bulletWorld.Trails, _typeRegistry);
            _laserRenderer.Rebuild(_laserPool, _typeRegistry);
            _laserWarningRenderer.Rebuild(_laserPool, _typeRegistry);
            _damageNumbers.Rebuild(dt);
            _trailPool.Render();

            // 渲染统计帧结束
            RenderBatchManagerRuntimeStats.EndFrame();
        }
    }
}
