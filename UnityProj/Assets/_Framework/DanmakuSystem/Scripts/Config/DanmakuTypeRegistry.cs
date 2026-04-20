using System;
using System.Collections.Generic;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕类型注册表（ADR-030）——internal 运行时类，懒注册。
    /// 不再继承 ScriptableObject，不再持久化为 .asset 文件。
    /// 所有类型在首次使用时自动注册，index 在单次运行内 append-only 保持稳定。
    /// </summary>
    internal sealed class DanmakuTypeRegistry
    {
        private readonly List<BulletTypeSO> _bulletTypes = new();
        private readonly Dictionary<BulletTypeSO, ushort> _bulletIndex = new();

        private readonly List<LaserTypeSO> _laserTypes = new();
        private readonly Dictionary<LaserTypeSO, byte> _laserIndex = new();

        private readonly List<SprayTypeSO> _sprayTypes = new();
        private readonly Dictionary<SprayTypeSO, byte> _sprayIndex = new();

        public ushort GetOrRegisterBullet(BulletTypeSO type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (_bulletIndex.TryGetValue(type, out ushort index))
                return index;

            if (_bulletTypes.Count >= ushort.MaxValue + 1)
                throw new InvalidOperationException("[DanmakuTypeRegistry] BulletType 数量超出 ushort 上限。");

            index = (ushort)_bulletTypes.Count;
            _bulletTypes.Add(type);
            _bulletIndex.Add(type, index);
            return index;
        }

        public BulletTypeSO GetBulletType(ushort index)
        {
            if (index >= _bulletTypes.Count)
                throw new IndexOutOfRangeException($"[DanmakuTypeRegistry] BulletType index 越界: {index}/{_bulletTypes.Count}");
            return _bulletTypes[index];
        }

        public int BulletTypeCount => _bulletTypes.Count;

        public byte GetOrRegisterLaser(LaserTypeSO type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (_laserIndex.TryGetValue(type, out byte index))
                return index;

            if (_laserTypes.Count >= byte.MaxValue + 1)
                throw new InvalidOperationException("[DanmakuTypeRegistry] LaserType 数量超出 byte 上限。");

            index = (byte)_laserTypes.Count;
            _laserTypes.Add(type);
            _laserIndex.Add(type, index);
            return index;
        }

        public LaserTypeSO GetLaserType(byte index)
        {
            if (index >= _laserTypes.Count)
                throw new IndexOutOfRangeException($"[DanmakuTypeRegistry] LaserType index 越界: {index}/{_laserTypes.Count}");
            return _laserTypes[index];
        }

        public int LaserTypeCount => _laserTypes.Count;

        public byte GetOrRegisterSpray(SprayTypeSO type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (_sprayIndex.TryGetValue(type, out byte index))
                return index;

            if (_sprayTypes.Count >= byte.MaxValue + 1)
                throw new InvalidOperationException("[DanmakuTypeRegistry] SprayType 数量超出 byte 上限。");

            index = (byte)_sprayTypes.Count;
            _sprayTypes.Add(type);
            _sprayIndex.Add(type, index);
            return index;
        }

        public SprayTypeSO GetSprayType(byte index)
        {
            if (index >= _sprayTypes.Count)
                throw new IndexOutOfRangeException($"[DanmakuTypeRegistry] SprayType index 越界: {index}/{_sprayTypes.Count}");
            return _sprayTypes[index];
        }

        public int SprayTypeCount => _sprayTypes.Count;

        /// <summary>
        /// 批量预注册——编辑器预热时使用，提前填充 registry 以避免首帧 spike。
        /// 运行时不需要调用此方法。
        /// </summary>
        public void WarmUp(IEnumerable<BulletTypeSO> bullets, IEnumerable<LaserTypeSO> lasers, IEnumerable<SprayTypeSO> sprays)
        {
            if (bullets != null)
            {
                foreach (var bullet in bullets)
                {
                    if (bullet != null)
                        GetOrRegisterBullet(bullet);
                }
            }

            if (lasers != null)
            {
                foreach (var laser in lasers)
                {
                    if (laser != null)
                        GetOrRegisterLaser(laser);
                }
            }

            if (sprays != null)
            {
                foreach (var spray in sprays)
                {
                    if (spray != null)
                        GetOrRegisterSpray(spray);
                }
            }
        }
    }
}
