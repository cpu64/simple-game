using System;
using System.Linq;
using System.Numerics;

public class Slime : Enemy, IBinarySerializable
{
    private const float Gravity = 10.0f;
    private const float JumpSpeedX = 3.5f;
    private const float JumpSpeedY = -7.0f;
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

        Vector2 newPosition = PhysicsSystem.Move(Position, Size.X, Size.Y, Velocity * deltaTime, world.Blocks);

        bool isGrounded = newPosition.Y == Position.Y && Velocity.Y > 0;

        if (isGrounded)
        {
            Velocity = Vector2.Zero;
            Position = newPosition;

            JumpTimer -= deltaTime;
            if (JumpTimer <= 0)
            {
                Player? nearestPlayer = FindNearestPlayer(world);
                if (nearestPlayer != null)
                {
                    FacingDirection = nearestPlayer.Position.X >= Position.X ? 1.0f : -1.0f;
                }

                Velocity = new Vector2(FacingDirection * JumpSpeedX, JumpSpeedY);
                JumpTimer = DefaultJumpInterval;
            }
        }
        else
        {
            Position = newPosition;
        }
    }

    private Player? FindNearestPlayer(World world)
    {
        Player? nearest = null;
        float minDistanceSq = 400.0f; // Detect within 20 blocks

        foreach (Player player in world.Entities.OfType<Player>())
        {
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
