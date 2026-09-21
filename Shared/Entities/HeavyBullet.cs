using System.Numerics;

public class HeavyBullet : Bullet, IBinarySerializable
{
    private const float BulletWidth = 0.35f;
    private const float BulletHeight = 0.35f;

    public override int Damage => 25;
    public override Vector2 Size => new Vector2(BulletWidth, BulletHeight);
    public override float GravityScale => 1.0f;
    public override float KnockbackForce => 6.0f;

    public override Vector2 Velocity { get; set; }
    public override long TicksLeft { get; set; }

    public HeavyBullet(EntityId id, Vector2 position, EntityId firedBy, Vector2 velocity, long ticksLeft, bool firedByPlayer = true)
        : base(id, position, firedBy, firedByPlayer)
    {
        Velocity = velocity;
        TicksLeft = ticksLeft;
    }

    public override HeavyBullet Copy()
    {
        return (HeavyBullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, Velocity={Velocity}, TicksLeft={TicksLeft}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(FiredBy);
        writer.Write(FiredByPlayer);
        writer.Write(Velocity);
        writer.Write(TicksLeft);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        EntityId id = reader.Read<EntityId>();
        Vector2 position = reader.Read<Vector2>();
        EntityId firedBy = reader.Read<EntityId>();
        bool firedByPlayer = reader.Read<bool>();
        Vector2 velocity = reader.Read<Vector2>();
        long ticksLeft = reader.Read<long>();

        return new HeavyBullet(id, position, firedBy, velocity, ticksLeft, firedByPlayer);
    }
}
