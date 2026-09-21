using System;
using System.Linq;
using System.Numerics;

public class Slime : Enemy, IBinarySerializable
{
    private const float Gravity = 10.0f;
    private const float JumpSpeedX = 3.5f;
    private const float JumpSpeedY = -7.0f;
    private const float WeakJumpSpeedX = 2.2f;
    private const float WeakJumpSpeedY = -3.8f;
    private const float DefaultJumpInterval = 1.2f;

    public override int Health { get; protected set; }
    public override int MaxHealth => 20;
    public override Vector2 Size => new Vector2(1.0f, 0.8f);

    public float JumpTimer { get; set; }
    public float FacingDirection { get; set; }

    public Slime(
        EntityId id,
        Vector2 position,
        Vector2 velocity = default,
        int health = 20,
        float jumpTimer = DefaultJumpInterval,
        float facingDirection = 1.0f
    )
        : base(id, position, velocity)
    {
        Health = health;
        JumpTimer = jumpTimer;
        FacingDirection = facingDirection;
    }

    public override void Tick(World world, float deltaTime)
    {
        if (IsDead)
            return;

        Velocity += new Vector2(0, Gravity * deltaTime);

        MovementResult result = PhysicsSystem.MoveDetailed(Position, Size.X, Size.Y, Velocity * deltaTime, world.Blocks);
        Position = result.Position;

        if (result.HitCeiling && Velocity.Y < 0)
        {
            Velocity = new Vector2(Velocity.X, 0);
        }

        if (result.HitHorizontal)
        {
            Velocity = new Vector2(0, Velocity.Y);
            FacingDirection = -FacingDirection;
        }

        if (result.HitFloor)
        {
            Velocity = Vector2.Zero;

            JumpTimer -= deltaTime;
            if (JumpTimer <= 0)
            {
                Player? nearestPlayer = FindNearestPlayer(world);
                if (nearestPlayer != null)
                {
                    float dx = nearestPlayer.Position.X - Position.X;
                    float dy = nearestPlayer.Position.Y - Position.Y;
                    FacingDirection = dx >= 0 ? 1.0f : -1.0f;

                    float horizontalDistance = Math.Abs(dx);

                    // Choose a weaker, lower jump when player is nearby to hit them instead of jumping over
                    if (horizontalDistance < 3.5f && dy >= -1.0f)
                    {
                        Velocity = new Vector2(FacingDirection * WeakJumpSpeedX, WeakJumpSpeedY);
                    }
                    else
                    {
                        Velocity = new Vector2(FacingDirection * JumpSpeedX, JumpSpeedY);
                    }
                }
                else
                {
                    Velocity = new Vector2(FacingDirection * 2.0f, -4.5f);
                }

                JumpTimer = DefaultJumpInterval;
            }
        }

        CombatSystem.CheckEnemyContactDamage(world, this, damage: 10);
    }

    private Player? FindNearestPlayer(World world)
    {
        Player? nearest = null;
        float minDistanceSq = 400.0f; // Detect within 20 blocks

        foreach (Player player in world.Entities.OfType<Player>())
        {
            if (player.IsDead)
                continue;

            float distSq = Vector2.DistanceSquared(Position, player.Position);
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                nearest = player;
            }
        }

        return nearest;
    }

    public override Slime Copy()
    {
        return (Slime)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"Slime: Id={Id}, Position={Position}, Health={Health}/{MaxHealth}, Velocity={Velocity}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(Velocity);
        writer.Write(Health);
        writer.Write(JumpTimer);
        writer.Write(FacingDirection);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        var id = reader.Read<EntityId>();
        var position = reader.Read<Vector2>();
        var velocity = reader.Read<Vector2>();
        var health = reader.Read<int>();
        var jumpTimer = reader.Read<float>();
        var facing = reader.Read<float>();

        return new Slime(id, position, velocity, health, jumpTimer, facing);
    }
}
