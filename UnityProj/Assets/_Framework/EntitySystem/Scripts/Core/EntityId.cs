namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 唯一标识（uint32 值类型）。
    /// 用于跨系统安全引用 Entity，避免直接对象引用导致的 GC 和悬空风险。
    /// </summary>
    public readonly struct EntityId : System.IEquatable<EntityId>
    {
        public readonly uint Value;

        public EntityId(uint value) => Value = value;

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => (int)Value;
        public override string ToString() => $"Entity#{Value}";

        public static bool operator ==(EntityId left, EntityId right) => left.Value == right.Value;
        public static bool operator !=(EntityId left, EntityId right) => left.Value != right.Value;

        /// <summary>无效 ID（0），表示空引用或未初始化。</summary>
        public static readonly EntityId Invalid = new(0);
    }
}
