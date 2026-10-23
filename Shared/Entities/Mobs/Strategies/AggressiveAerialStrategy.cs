using System;
using System.Numerics;

public class AggressiveAerialStrategy : IMobMovementStrategy
{
    public float Speed { get; }

    public AggressiveAerialStrategy(float speed = 1.5f)
    {
        Speed = speed;
    }

    public void UpdateMovement(Mob mob, World world)
    {
        if (mob is not ICollidable collidable)
            return;

        Player? target = AiSystem.FindNearestPlayer(world, mob.Position, 20.0f);

        float direction;
        float vy = mob.Velocity.Y;

        if (target != null)
        {
            Vector2 offset = target.Position - mob.Position;
            direction = offset.X >= 0.0f ? 1.0f : -1.0f;
            mob.Rotation = direction > 0.0f ? 0.0f : MathF.PI;

            if (offset.Y != 0.0f)
            {
                vy = Math.Clamp(offset.Y, -1.0f, 1.0f);
            }
        }
        else
        {
            direction = mob.Rotation >= 0.0f && mob.Rotation < MathF.PI ? 1.0f : -1.0f;
            vy = MathF.Abs(vy) < 0.05f ? 0.0f : vy * 0.85f;
        }

        if (AiSystem.IsObstacleAhead(mob.Position, collidable.CollisionSize, direction, world.Blocks))
        {
            direction *= -1.0f;
            mob.Rotation = direction > 0.0f ? 0.0f : MathF.PI;
        }

        mob.Velocity = new Vector2(direction * Speed, vy);
    }
}
