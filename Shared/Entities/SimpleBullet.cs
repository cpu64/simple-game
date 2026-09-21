using System.Numerics;

public class SimpleBullet : Bullet, IBinarySerializable
{
    public override int Damage => 10;
    public override Vector2 Velocity { get; set; }
    public override long TicksLeft { get; set; }
    public override float GravityScale => 0.0f;

    public SimpleBullet(EntityId id, Vector2 position, EntityId firedBy, Vector2 velocity, long ticksLeft, bool firedByPlayer = true)
        : base(id, position, firedBy, firedByPlayer)
    {
        Velocity = velocity;
        TicksLeft = ticksLeft;
    }

    public override SimpleBullet Copy()
    {
        return (SimpleBullet)MemberwiseClone();
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

        return new SimpleBullet(id, position, firedBy, velocity, ticksLeft, firedByPlayer);
    }
}
