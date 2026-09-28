using System.Numerics;

public class SimpleBullet : Bullet, IBinarySerializable, ICollidable, IDamaging, ILifespan, IMoving
{
    public Vector2 CollisionSize { get; set; }
    public int Damage { get; set; }
    public Vector2 DamageBox { get; set; }
    public float KnockBackMultiplier { get; set; }
    public Vector2 Velocity { get; set; }
    public long TicksLeft { get; set; }

    public SimpleBullet(
        EntityId id,
        Vector2 position,
        EntityId firedBy,
        Vector2 collisionSize,
        int damage,
        Vector2 damageBox,
        float knockBackMultiplier,
        Vector2 velocity,
        long ticksLeft
    )
        : base(id, position, firedBy)
    {
        CollisionSize = collisionSize;
        Damage = damage;
        DamageBox = damageBox;
        KnockBackMultiplier = knockBackMultiplier;
        Velocity = velocity;
        TicksLeft = ticksLeft;
    }

    public override SimpleBullet Copy()
    {
        return (SimpleBullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, CollisionSize={CollisionSize}, Damage={Damage}, "
            + $"DamageBox={DamageBox}, KnockBackMultiplier={KnockBackMultiplier}, Velocity={Velocity}, TicksLeft={TicksLeft}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(FiredBy);
        writer.Write(CollisionSize);
        writer.Write(Damage);
        writer.Write(DamageBox);
        writer.Write(KnockBackMultiplier);
        writer.Write(Velocity);
        writer.Write(TicksLeft);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        return new SimpleBullet(
            reader.Read<EntityId>(),
            reader.Read<Vector2>(),
            reader.Read<EntityId>(),
            reader.Read<Vector2>(),
            reader.Read<int>(),
            reader.Read<Vector2>(),
            reader.Read<float>(),
            reader.Read<Vector2>(),
            reader.Read<long>()
        );
    }
}
