using System.Numerics;

public class HeavyBullet : Bullet, IBinarySerializable
{
    private const float BulletWidth = 0.2f;
    private const float BulletHeight = 0.2f;

    public override int Damage => 25;
    public override Vector2 Size => new Vector2(BulletWidth, BulletHeight);
    public override float GravityScale => 1.0f;

    public override Vector2 Velocity { get; set; }
    public override long TicksLeft { get; set; }

    public HeavyBullet(EntityId id, Vector2 position, EntityId firedBy, Vector2 velocity, long ticksLeft)
        : base(id, position, firedBy)
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
        writer.Write(Velocity);
        writer.Write(TicksLeft);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        return new HeavyBullet(reader.Read<EntityId>(), reader.Read<Vector2>(), reader.Read<EntityId>(), reader.Read<Vector2>(), reader.Read<long>());
    }
}
