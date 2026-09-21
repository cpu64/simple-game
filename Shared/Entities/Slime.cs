using System;
using System.Numerics;

public class Slime : Enemy, IBinarySerializable
{
    private const float JumpSpeedX = 3.5f;
    private const float JumpSpeedY = -7.0f;
    private const float WeakJumpSpeedX = 2.2f;
    private const float WeakJumpSpeedY = -3.8f;
    private const float DefaultJumpInterval = 1.2f;

    public override int Health { get; protected set; }
    public override int MaxHealth => 20;
    public override Vector2 Size => new Vector2(1.0f, 0.8f);

    public float JumpTimer { get; set; }

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

    public override void UpdateAI(in AISenses senses, float dt)
    {
        if (IsDead)
            return;

        // Only flip facing if actually moving towards the wall that was hit
        if (senses.HitHorizontal && Velocity.X * FacingDirection >= 0)
        {
            FacingDirection = -FacingDirection;
        }

        if (!senses.IsGrounded)
            return;

        JumpTimer -= dt;
        if (JumpTimer > 0)
            return;

        if (senses.NearestTarget.HasValue)
        {
            TargetInfo target = senses.NearestTarget.Value;
            FacingDirection = target.Offset.X >= 0 ? 1.0f : -1.0f;

            float horizontalDistance = Math.Abs(target.Offset.X);

            // Use weak jump only on open flat ground nearby; if an obstacle is ahead or hit, or target is elevated, use full jump
            if (!senses.ObstacleAhead && !senses.HitHorizontal && horizontalDistance < 3.5f && target.Offset.Y >= -0.5f)
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
