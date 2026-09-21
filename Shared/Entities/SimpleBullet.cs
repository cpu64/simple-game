using System.Numerics;

public class SimpleBullet : Bullet, IMoving, IBinarySerializable
{
    public override int Damage => 10;
    public Vector2 Velocity { get; set; }
    public override long TicksLeft { get; set; }

    public SimpleBullet(EntityId id, Vector2 position, EntityId firedBy, Vector2 velocity, long ticksLeft)
        : base(id, position, firedBy)
    {
        Velocity = velocity;
        TicksLeft = ticksLeft;
    }

    public override ImpactResult Tick(World world, float deltaTime)
    {
        if (!PhysicsSystem.TryMove(Position, Size.X, Size.Y, Velocity * deltaTime, world.Blocks, out Vector2 newPosition))
            return ImpactResult.Block();

        Position = newPosition;
        TicksLeft--;
        return ImpactResult.None;
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
