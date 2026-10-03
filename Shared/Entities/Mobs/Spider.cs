using System.Numerics;

public class Spider : Mob, IBinarySerializable, ICollidable, IDamaging, IGravityAffected
{
    public Vector2 CollisionSize { get; set; }

    public int Damage { get; set; }
    public Vector2 DamageBox { get; set; }
    public float KnockBackMultiplier { get; set; }

    public float GravitationalAcceleration { get; set; }

    public Spider(
        EntityId id,
        Vector2 position,
        Vector2 collisionSize = new Vector2(),
        int damage = 10,
        Vector2 damageBox = new Vector2(),
        float knockBackMultiplier = 0,
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
        Damage = damage;
        DamageBox = damageBox;
        KnockBackMultiplier = knockBackMultiplier;
        GravitationalAcceleration = gravitationalAcceleration;
    }

    public override Spider Copy()
    {
        return (Spider)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, CollisionSize={CollisionSize}, Damage={Damage}, DamageBox={DamageBox}, KnockBackMultiplier={KnockBackMultiplier:F2}, GravitationalAcceleration={GravitationalAcceleration:F2}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(CollisionSize);
        writer.Write(Damage);
        writer.Write(DamageBox);
        writer.Write(KnockBackMultiplier);
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
        var damage = reader.Read<int>();
        var damageBox = reader.Read<Vector2>();
        var knockBackMultiplier = reader.Read<float>();
        var rotation = reader.Read<float>();
        var gravitationalAcceleration = reader.Read<float>();
        var maxHealth = reader.Read<int>();
        var health = reader.Read<int>();
        var hitBox = reader.Read<Vector2>();
        var invincibleUntil = reader.Read<long>();
        var velocity = reader.Read<Vector2>();

        return new Spider(
            id,
            position,
            collisionSize,
            damage,
            damageBox,
            knockBackMultiplier,
            rotation,
            gravitationalAcceleration,
            maxHealth,
            health,
            hitBox,
            invincibleUntil,
            velocity
        );
    }
}
