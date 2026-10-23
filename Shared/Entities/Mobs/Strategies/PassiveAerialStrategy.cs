using System;
using System.Numerics;

public class PassiveAerialStrategy : IMobMovementStrategy
{
    public float Speed { get; }

    public PassiveAerialStrategy(float speed = 1.5f)
    {
        Speed = speed;
    }

    public void UpdateMovement(Mob mob, World world)
    {
        if (mob is not ICollidable collidable)
            return;

        float direction = mob.Rotation >= 0.0f && mob.Rotation < MathF.PI ? 1.0f : -1.0f;

        if (AiSystem.IsObstacleAhead(mob.Position, collidable.CollisionSize, direction, world.Blocks))
        {
            direction *= -1.0f;
            mob.Rotation = direction > 0.0f ? 0.0f : MathF.PI;
        }

        float vy = MathF.Abs(mob.Velocity.Y) < 0.05f ? 0.0f : mob.Velocity.Y * 0.85f;
        mob.Velocity = new Vector2(direction * Speed, vy);
    }
}
