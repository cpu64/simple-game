using System;
using System.Numerics;

public class PassiveGroundStrategy : IMobMovementStrategy
{
    public float Speed { get; }

    public PassiveGroundStrategy(float speed = 1.0f)
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

        mob.Velocity = new Vector2(direction * Speed, mob.Velocity.Y);
    }
}
