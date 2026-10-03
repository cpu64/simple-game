using System.Numerics;

public class Eagle : Mob, IBinarySerializable, ICollidable
{
    public Vector2 CollisionSize { get; set; }

    public Eagle(
        EntityId id,
        Vector2 position,
        Vector2 collisionSize = new Vector2(),
        float rotation = 0,
        int maxHealth = 100,
        int health = 100,
        Vector2 hitBox = new Vector2(),
        long invincibleUntil = 0,
        Vector2 velocity = new Vector2()
    )
        : base(id, position, rotation, maxHealth, health, hitBox, invincibleUntil, velocity)
    {
        CollisionSize = collisionSize;
    }

    public override Eagle Copy()
    {
        return (Eagle)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, CollisionSize={CollisionSize}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(CollisionSize);
        writer.Write(Rotation);
        writer.Write(MaxHealth);
        writer.Write(Health);
        writer.Write(HitBox);
        writer.Write(InvincibleUntil);
        writer.Write(Velocity);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        var id = reader.Read<EntityId>();
        var position = reader.Read<Vector2>();
        var collisionSize = reader.Read<Vector2>();
        var rotation = reader.Read<float>();
        var maxHealth = reader.Read<int>();
        var health = reader.Read<int>();
        var hitBox = reader.Read<Vector2>();
        var invincibleUntil = reader.Read<long>();
        var velocity = reader.Read<Vector2>();

        return new Eagle(id, position, collisionSize, rotation, maxHealth, health, hitBox, invincibleUntil, velocity);
    }
}
