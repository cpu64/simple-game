using System.Numerics;

public class HeavyBullet : Bullet, IGravityAffected, IMoving, IBinarySerializable
{
    private const float BulletGravity = 10.0f;
    private const float BulletWidth = 0.2f;
    private const float BulletHeight = 0.2f;

    public override int Damage => 25;
    public override Vector2 Size => new Vector2(BulletWidth, BulletHeight);

    public override Vector2 Velocity { get; set; }
    public override long TicksLeft { get; set; }

    public HeavyBullet(EntityId id, Vector2 position, EntityId firedBy, Vector2 velocity, long ticksLeft)
        : base(id, position, firedBy)
    {
        Velocity = velocity;
        TicksLeft = ticksLeft;
    }

    public override ImpactResult Tick(World world, float deltaTime)
    {
        Velocity += new Vector2(0, BulletGravity * deltaTime);

        if (!PhysicsSystem.TryMove(Position, BulletWidth, BulletHeight, Velocity * deltaTime, world.Blocks, out Vector2 newPosition))
            return ImpactResult.Block();

        Position = newPosition;
        TicksLeft--;
        return ImpactResult.None;
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
