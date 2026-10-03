using System.Numerics;

public class HeavyBullet : Bullet, IBinarySerializable, ICollidable, IGravityAffected, ILifespan
{
    public Vector2 CollisionSize { get; set; }
    public float GravitationalAcceleration { get; set; }
    public long TicksLeft { get; set; }

    public HeavyBullet(
        EntityId id,
        Vector2 position,
        EntityId firedBy,
        int damage,
        Vector2 damageBox,
        float knockBackMultiplier,
        Vector2 velocity,
        Vector2 collisionSize,
        float gravitationalAcceleration,
        long ticksLeft
    )
        : base(id, position, firedBy, damage, damageBox, knockBackMultiplier, velocity)
    {
        CollisionSize = collisionSize;
        GravitationalAcceleration = gravitationalAcceleration;
        TicksLeft = ticksLeft;
    }

    public override HeavyBullet Copy()
    {
        return (HeavyBullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, CollisionSize={CollisionSize}, GravitationalAcceleration={GravitationalAcceleration}, TicksLeft={TicksLeft}";
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
        writer.Write(GravitationalAcceleration);
        writer.Write(TicksLeft);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        return new HeavyBullet(
            reader.Read<EntityId>(),
            reader.Read<Vector2>(),
            reader.Read<EntityId>(),
            reader.Read<int>(),
            reader.Read<Vector2>(),
            reader.Read<float>(),
            reader.Read<Vector2>(),
            reader.Read<Vector2>(),
            reader.Read<float>(),
            reader.Read<long>()
        );
    }
}
