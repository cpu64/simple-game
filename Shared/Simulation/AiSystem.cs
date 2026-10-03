using System;
using System.Linq;
using System.Numerics;

public static class AiSystem
{
    public static void Process(World world)
    {
        foreach (Slime slime in world.Entities.OfType<Slime>())
        {
            IHealth health = slime;

            if (health.IsDead)
                continue;

            ProcessSlime(world, slime);
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

            bool obstacleAhead = IsObstacleAhead(slime, direction, world.Blocks);

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

    private static bool IsObstacleAhead(Slime slime, float direction, System.Collections.Generic.List<Block> blocks)
    {
        // Check slightly in front of the slime at its feet/body height.
        Vector2 checkPosition = slime.Position + new Vector2(direction * (slime.CollisionSize.X * 0.5f + 0.05f), 0.0f);

        return CollisionService.CollidesWithBlock(checkPosition, slime.CollisionSize, blocks);
    }
}
