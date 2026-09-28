using System;
using System.Numerics;

public class Player : Entity, IBinarySerializable, ICollidable, IDamaging, IFacing, IGravityAffected, IHealth, IMoving
{
    public Guid UserId { get; private set; }
    public long LastCommand { get; set; }

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

    public Player(
        EntityId id,
        Vector2 position,
        Guid userId,
        long lastCommand = -1,
        Vector2 collisionSize = new Vector2(),
        int damage = 10,
        Vector2 damageBox = new Vector2(),
        float knockBackMultiplier = 0,
        float rotation = 0,
        float gravitationalAcceleration = 16.0f,
        int maxHealth = 10000,
        int health = 10000,
        Vector2 hitBox = new Vector2(),
        long invincibleUntil = 0,
        Vector2 velocity = new Vector2()
    )
        : base(id, position)
    {
        UserId = userId;
        LastCommand = lastCommand;

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

    public override Player Copy()
    {
        return (Player)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, UserId={UserId}, LastCommand={LastCommand}, " + $"Rotation={Rotation:F2}, Velocity={Velocity}, Health={Health}/{MaxHealth}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);

        writer.Write(UserId);
        writer.Write(LastCommand);

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

        var userId = reader.Read<Guid>();
        var lastCommand = reader.Read<long>();

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

        return new Player(
            id,
            position,
            userId,
            lastCommand,
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
