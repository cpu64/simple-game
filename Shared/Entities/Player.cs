using System;
using System.Numerics;

public class Player : Entity, IFacing, IMoving, ICollidable, IDamageable, IKinematicBody, IBinarySerializable
{
    public Vector2 Size => new Vector2(1.0f, 1.0f);
    public float GravityScale => 1.0f;
    public float Drag => 0.85f;
    public bool CollidesWithBlocks => true;

    public Guid UserId { get; private set; }
    public long LastCommand { get; set; }
    public float Rotation { get; set; }
    public Vector2 Velocity { get; set; }

    public int Health { get; set; }
    public int MaxHealth => 100;
    public bool IsDead => Health <= 0;

    public float InvulnerabilityTimer { get; set; }
    public bool IsInvulnerable => InvulnerabilityTimer > 0;

    public float AttackCooldownTimer { get; set; }

    public Player(
        EntityId id,
        Vector2 position,
        Guid userId,
        long lastCommand = -1,
        float rotation = 0,
        Vector2 velocity = new Vector2(),
        int health = 100,
        float invulnerabilityTimer = 0.0f,
        float attackCooldownTimer = 0.0f
    )
        : base(id, position)
    {
        UserId = userId;
        Rotation = rotation;
        Velocity = velocity;
        LastCommand = lastCommand;
        Health = health;
        InvulnerabilityTimer = invulnerabilityTimer;
        AttackCooldownTimer = attackCooldownTimer;
    }

    public bool TakeDamage(int amount)
    {
        if (IsInvulnerable || IsDead)
            return false;

        Health = Math.Max(0, Health - amount);
        InvulnerabilityTimer = 0.5f;
        return true;
    }

    public void OnDeath(World world) { }

    public override Player Copy()
    {
        return (Player)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, UserId={UserId}, Health={Health}/{MaxHealth}, Invulnerable={IsInvulnerable}, LastCommand={LastCommand}, Rotation={Rotation:F2}, Velocity={Velocity}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(UserId);
        writer.Write(LastCommand);
        writer.Write(Rotation);
        writer.Write(Velocity);
        writer.Write(Health);
        writer.Write(InvulnerabilityTimer);
        writer.Write(AttackCooldownTimer);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        var id = reader.Read<EntityId>();
        var position = reader.Read<Vector2>();
        var userId = reader.Read<Guid>();
        var lastCommand = reader.Read<long>();
        var rotation = reader.Read<float>();
        var velocity = reader.Read<Vector2>();
        var health = reader.Read<int>();
        var invulnerabilityTimer = reader.Read<float>();
        var attackCooldownTimer = reader.Read<float>();

        return new Player(id, position, userId, lastCommand, rotation, velocity, health, invulnerabilityTimer, attackCooldownTimer);
    }
}
