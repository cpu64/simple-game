using System.Numerics;

public static class EnemySystem
{
    public static void Update(World world)
    {
        float dt = (float)GameConstants.SimulationTickDuration;

        for (int i = 0; i < world.Entities.Count; i++)
        {
            if (world.Entities[i] is not Enemy enemy || enemy.IsDead)
                continue;

            bool isGrounded = enemy.LastMovement.HitFloor || PhysicsSystem.IsGrounded(enemy.Position, enemy.Size, world.Blocks);
            TargetInfo? nearestTarget = FindNearestTarget(enemy, world);

            float facing = nearestTarget.HasValue ? (nearestTarget.Value.Offset.X >= 0 ? 1.0f : -1.0f) : enemy.FacingDirection;
            bool obstacleAhead = PhysicsSystem.CollidesWithBlock(enemy.Position.X + facing * 0.8f, enemy.Position.Y, enemy.Size.X, enemy.Size.Y, world.Blocks);

            AISenses senses = new AISenses(isGrounded, enemy.LastMovement.HitHorizontal, enemy.LastMovement.HitCeiling, obstacleAhead, nearestTarget);

            enemy.UpdateAI(in senses, dt);

            enemy.LastMovement = PhysicsSystem.StepBody(enemy, world.Blocks, dt);
        }
    }

    private static TargetInfo? FindNearestTarget(Enemy enemy, World world)
    {
        TargetInfo? nearest = null;
        float minDistanceSq = 400.0f; // 20 blocks detection range

        for (int i = 0; i < world.Entities.Count; i++)
        {
            if (world.Entities[i] is not Player player || player.IsDead)
                continue;

            Vector2 offset = player.Position - enemy.Position;
            float distSq = offset.LengthSquared();
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                nearest = new TargetInfo(player.Id, offset, distSq);
            }
        }

        return nearest;
    }

    public static Slime SpawnSlime(World world, Vector2 position)
    {
        Slime slime = new Slime(world.NextEntityId++, position);
        world.Entities.Add(slime);
        return slime;
    }
}
