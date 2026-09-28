using System.Numerics;

public class SimpleBullet : Bullet, IBinarySerializable, ICollidable, ILifespan
{
    public Vector2 CollisionSize { get; set; }
    public long TicksLeft { get; set; }

    public SimpleBullet(
        EntityId id,
        Vector2 position,
        EntityId firedBy,
        int damage,
        Vector2 damageBox,
        float knockBackMultiplier,
        Vector2 velocity,
        Vector2 collisionSize,
        long ticksLeft
    )
        : base(id, position, firedBy, damage, damageBox, knockBackMultiplier, velocity)
    {
        CollisionSize = collisionSize;
        TicksLeft = ticksLeft;
    }

    public override SimpleBullet Copy()
    {
        return (SimpleBullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, CollisionSize={CollisionSize}, TicksLeft={TicksLeft}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(FiredBy);
        writer.Write(Damage);
        writer.Write(DamageBox);
        writer.Write(KnockBackMultiplier);
        writer.Write(Velocity);
        writer.Write(CollisionSize);
        writer.Write(TicksLeft);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        return new SimpleBullet(
            reader.Read<EntityId>(),
            reader.Read<Vector2>(),
            reader.Read<EntityId>(),
            reader.Read<int>(),
            reader.Read<Vector2>(),
            reader.Read<float>(),
            reader.Read<Vector2>(),
            reader.Read<Vector2>(),
            reader.Read<long>()
        );
    }
}
