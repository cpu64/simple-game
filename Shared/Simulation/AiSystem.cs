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

            // Note: State pattern candidate for complex mob behavior later
            if (mob is Spider)
            {
                if (mob.MovementStrategy is not AggressiveGroundStrategy && FindNearestPlayer(world, mob.Position, 16.0f) != null)
                {
                    mob.MovementStrategy = new AggressiveGroundStrategy();
                }
                else if (mob.MovementStrategy is not PassiveGroundStrategy && FindNearestPlayer(world, mob.Position, 20.0f) == null)
                {
                    mob.MovementStrategy = new PassiveGroundStrategy();
                }
            }
            else if (mob is Harpy)
            {
                if (mob.MovementStrategy is not AggressiveAerialStrategy && FindNearestPlayer(world, mob.Position, 16.0f) != null)
                {
                    mob.MovementStrategy = new AggressiveAerialStrategy();
                }
                else if (mob.MovementStrategy is not PassiveAerialStrategy && FindNearestPlayer(world, mob.Position, 20.0f) == null)
                {
                    mob.MovementStrategy = new PassiveAerialStrategy();
                }
            }

            mob.MovementStrategy?.UpdateMovement(mob, world);
        }
    }

    public static Player? FindNearestPlayer(World world, Vector2 position, float maxDistance)
    {
        Player? nearestPlayer = null;
        float nearestDistanceSquared = maxDistance * maxDistance;

        foreach (Player player in world.Entities.OfType<Player>())
        {
            IHealth health = player;

            if (health.IsDead)
                continue;

            Vector2 offset = player.Position - position;
            float distanceSquared = offset.LengthSquared();

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestPlayer = player;
            }
        }

        return nearestPlayer;
    }

    public static bool IsObstacleAhead(Vector2 position, Vector2 collisionSize, float direction, System.Collections.Generic.List<Block> blocks)
    {
        Vector2 checkPosition = position + new Vector2(direction * (collisionSize.X * 0.5f + 0.05f), 0.0f);

        return CollisionService.CollidesWithBlock(checkPosition, collisionSize, blocks);
    }
}
