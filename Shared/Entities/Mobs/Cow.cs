using System.Numerics;

public class Cow : Mob, IBinarySerializable, ICollidable, IGravityAffected
{
    public Vector2 CollisionSize { get; set; }

    public float GravitationalAcceleration { get; set; }

    public Cow(
        EntityId id,
        Vector2 position,
        Vector2 collisionSize = new Vector2(),
        float rotation = 0,
        float gravitationalAcceleration = 16.0f,
        int maxHealth = 100,
        int health = 100,
        Vector2 hitBox = new Vector2(),
        long invincibleUntil = 0,
        Vector2 velocity = new Vector2()
    )
        : base(id, position, rotation, maxHealth, health, hitBox, invincibleUntil, velocity)
    {
        CollisionSize = collisionSize;
        GravitationalAcceleration = gravitationalAcceleration;
    }

    public override Cow Copy()
    {
        return (Cow)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, CollisionSize={CollisionSize}, GravitationalAcceleration={GravitationalAcceleration:F2}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(CollisionSize);
        writer.Write(Rotation);
        writer.Write(GravitationalAcceleration);
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
        var gravitationalAcceleration = reader.Read<float>();
        var maxHealth = reader.Read<int>();
        var health = reader.Read<int>();
        var hitBox = reader.Read<Vector2>();
        var invincibleUntil = reader.Read<long>();
        var velocity = reader.Read<Vector2>();

        return new Cow(id, position, collisionSize, rotation, gravitationalAcceleration, maxHealth, health, hitBox, invincibleUntil, velocity);
    }
}
