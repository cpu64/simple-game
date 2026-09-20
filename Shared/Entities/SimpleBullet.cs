using System.Numerics;

public class SimpleBullet : Bullet, ICollidable, ILifespan, IMoving, IBinarySerializable
{
    public Vector2 Velocity { get; set; }
    public long TicksLeft { get; set; }

    public SimpleBullet(EntityId id, Vector2 position, EntityId firedBy, Vector2 velocity, long ticksLeft)
        : base(id, position, firedBy)
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
        writer.Write(Velocity);
        writer.Write(TicksLeft);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        return new SimpleBullet(reader.Read<EntityId>(), reader.Read<Vector2>(), reader.Read<EntityId>(), reader.Read<Vector2>(), reader.Read<long>());
    }
}
