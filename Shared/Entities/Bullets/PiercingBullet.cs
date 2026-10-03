using System.Numerics;

public class PiercingBullet : Bullet, IBinarySerializable, ILifespan
{
    public long TicksLeft { get; set; }

    public PiercingBullet(
        EntityId id,
        Vector2 position,
        EntityId firedBy,
        int damage,
        Vector2 damageBox,
        float knockBackMultiplier,
        Vector2 velocity,
        long ticksLeft
    )
        : base(id, position, firedBy, damage, damageBox, knockBackMultiplier, velocity)
    {
        TicksLeft = ticksLeft;
    }

    public override PiercingBullet Copy()
    {
        return (PiercingBullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, TicksLeft={TicksLeft}";
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
        writer.Write(TicksLeft);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        return new PiercingBullet(
            reader.Read<EntityId>(),
            reader.Read<Vector2>(),
            reader.Read<EntityId>(),
            reader.Read<int>(),
            reader.Read<Vector2>(),
            reader.Read<float>(),
            reader.Read<Vector2>(),
            reader.Read<long>()
        );
    }
}
