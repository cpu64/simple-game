using System.Numerics;

public class Slime : Entity, IBinarySerializable, ICollidable, IDamaging, IFacing, IGravityAffected, IHealth, IMoving
{
    public const float DetectionRange = 20.0f;

    public static readonly Vector2 JumpVelocity = new Vector2(3.5f, -7.0f);
    public static readonly Vector2 WeakJumpVelocity = new Vector2(2.2f, -3.8f);

    public const long JumpIntervalTicks = 72;

    public long JumpTimer { get; set; }

    public Vector2 CollisionSize { get; set; }

    public int Damage { get; set; }
    public Vector2 DamageBox { get; set; }
    public float KnockBackMultiplier { get; set; }

    public float Rotation { get; set; }

    public float GravitationalAcceleration { get; set; }

    public int MaxHealth { get; set; }
    public int Health { get; set; }
    public Vector2 HitBox { get; set; }
    public long InvincibleUntil { get; set; }

    public Vector2 Velocity { get; set; }

    public Slime(
        EntityId id,
        Vector2 position,
        long jumpTimer = 0,
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
        : base(id, position)
    {
        JumpTimer = jumpTimer;

        CollisionSize = collisionSize;

        Damage = damage;
        DamageBox = damageBox;
        KnockBackMultiplier = knockBackMultiplier;

        Rotation = rotation;

        GravitationalAcceleration = gravitationalAcceleration;

        MaxHealth = maxHealth;
        Health = health;
        HitBox = hitBox;
        InvincibleUntil = invincibleUntil;

        Velocity = velocity;
    }

    public override Slime Copy()
    {
        return (Slime)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, JumpTimer={JumpTimer}, Rotation={Rotation:F2}, Velocity={Velocity}, Health={Health}/{MaxHealth}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);

        writer.Write(JumpTimer);

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

        var jumpTimer = reader.Read<long>();

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

        return new Slime(
            id,
            position,
            jumpTimer,
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
