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

            // VFX 时间缩放联动
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

            // 6. VFX 逻辑更新（R4.0：编排层统一——从 SpriteSheetVFXSystem.Update 收编到此处）
            _vfxRuntime?.TickVFX(dt);

            // 7. 碰撞检测
            // 无敌帧递减
            if (_invincibleTimer > 0)
            {
                _invincibleTimer -= Time.unscaledDeltaTime;
            }

            var result = _collisionSolver.SolveAll(
                _bulletWorld, _laserPool, _sprayPool, _obstaclePool,
                _typeRegistry, _attachRegistry, _targetRegistry, _vfxRuntime, dt);

            // 8. 处理碰撞结果——仅在 Player 阵营目标被命中时触发事件 / 无敌帧 / 飘字
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

            // 9. 特效桥接——消费碰撞事件 Buffer
            _effectsBridge?.OnCollisionEventsReady(_collisionEventBuffer);

            // 10. 帧末 Reset Buffer
            _collisionEventBuffer.Reset();
        }

        private void RunLateUpdatePipeline()
        {
            float dt = Time.deltaTime;
            if (_timeScale != null)
                dt *= _timeScale.TimeScale;

            // 渲染统计帧开始
            RenderBatchManagerRuntimeStats.BeginFrame();

            // 渲染——renderQueue 决定 GPU 级层次（值越小越先渲染 = 在后面）
            // Trail(3090) < Bullet(3100) < Laser(3120) < VFX(3200) < DamageNumber(3300)
            _trailPool.Render();
            _bulletRenderer.Rebuild(_bulletWorld, _bulletWorld.Trails, _typeRegistry);
            _laserRenderer.Rebuild(_laserPool, _typeRegistry);
            _laserWarningRenderer.Rebuild(_laserPool, _typeRegistry);
            _vfxRuntime?.RenderVFX();   // R4.0：VFX 渲染从独立 LateUpdate 收编到统一管线
            _damageNumbers.Rebuild(dt);

            // 渲染统计帧结束
            RenderBatchManagerRuntimeStats.EndFrame();
        }
    }
}
