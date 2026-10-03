using System;
using System.Linq;
using System.Numerics;

public static class AiSystem
{
    public static void Process(World world)
    {
        foreach (Mob mob in world.Entities.OfType<Mob>())
        {
            IHealth health = mob;

            if (health.IsDead)
                continue;

            if (mob is Slime slime)
            {
                ProcessSlime(world, slime);
            }
            else
            {
                ProcessWanderingMob(world, mob);
            }
        }
    }

    private static void ProcessSlime(World world, Slime slime)
    {
        bool isGrounded = CollisionService.IsGrounded(slime.Position, slime.CollisionSize, world.Blocks);

        Player? target = FindNearestPlayer(world, slime);

        if (target != null)
        {
            Vector2 offset = target.Position - slime.Position;

            if (offset.X != 0.0f)
                slime.Rotation = offset.X >= 0.0f ? 0.0f : MathF.PI;
        }

        if (slime.JumpTimer > 0)
        {
            slime.JumpTimer--;
            return;
        }

        if (!isGrounded)
            return;

        if (target != null)
        {
            Vector2 offset = target.Position - slime.Position;

            float direction = offset.X >= 0.0f ? 1.0f : -1.0f;
            float horizontalDistance = MathF.Abs(offset.X);

            bool obstacleAhead = IsObstacleAhead(slime.Position, slime.CollisionSize, direction, world.Blocks);

            // Use a weak jump when the target is nearby and on roughly the same level, provided there is no obstacle ahead.
            if (!obstacleAhead && horizontalDistance < 3.5f && offset.Y >= -0.5f)
            {
                slime.Velocity = new Vector2(Slime.WeakJumpVelocity.X * direction, Slime.WeakJumpVelocity.Y);
            }
            else
            {
                slime.Velocity = new Vector2(Slime.JumpVelocity.X * direction, Slime.JumpVelocity.Y);
            }
        }
        else
        {
            // No target: continue moving in the current facing direction.
            float facingDirection = slime.Rotation >= 0.0f && slime.Rotation < MathF.PI ? 1.0f : -1.0f;

            slime.Velocity = new Vector2(facingDirection * 2.0f, -4.5f);
        }

        slime.JumpTimer = Slime.JumpIntervalTicks;
    }

    private static void ProcessWanderingMob(World world, Mob mob)
    {
        if (mob is not ICollidable collidable)
            return;

        float direction = mob.Rotation >= 0.0f && mob.Rotation < MathF.PI ? 1.0f : -1.0f;

        bool obstacleAhead = IsObstacleAhead(mob.Position, collidable.CollisionSize, direction, world.Blocks);

        if (obstacleAhead)
        {
            direction *= -1.0f;
            mob.Rotation = direction > 0.0f ? 0.0f : MathF.PI;
        }

        float speed = mob switch
        {
            Harpy => 1.5f,
            Eagle => 1.5f,
            Bat => 1.5f,
            Spider => 1.0f,
            Cow => 1.0f,
            _ => 1.0f,
        };

        mob.Velocity = new Vector2(direction * speed, mob.Velocity.Y);
    }

    private static Player? FindNearestPlayer(World world, Slime slime)
    {
        Player? nearestPlayer = null;
        float nearestDistanceSquared = Slime.DetectionRange * Slime.DetectionRange;

        foreach (Player player in world.Entities.OfType<Player>())
        {
            IHealth health = player;

            if (health.IsDead)
                continue;

            Vector2 offset = player.Position - slime.Position;
            float distanceSquared = offset.LengthSquared();

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestPlayer = player;
            }
        }

        return nearestPlayer;
    }

    private static bool IsObstacleAhead(Vector2 position, Vector2 collisionSize, float direction, System.Collections.Generic.List<Block> blocks)
    {
        Vector2 checkPosition = position + new Vector2(direction * (collisionSize.X * 0.5f + 0.05f), 0.0f);

        return CollisionService.CollidesWithBlock(checkPosition, collisionSize, blocks);
    }
}
