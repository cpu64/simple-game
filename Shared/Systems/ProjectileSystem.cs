using System.Numerics;

public static class ProjectileSystem
{
    public static void SpawnSimpleBullet(World world, Player player, Vector2 clickPosition)
    {
        Vector2 direction = clickPosition - player.Position;

        if (direction.LengthSquared() <= 0.0001f)
            return;

        direction = Vector2.Normalize(direction);

        world.Entities.Add(new SimpleBullet(world.NextEntityId++, player.Position, player.Id, direction, ticksLeft: 300));
    }

    public static void Update(World world)
    {
        float dt = (float)GameConstants.SimulationTickDuration;

        for (int i = world.Entities.Count - 1; i >= 0; i--)
        {
            if (world.Entities[i] is not Bullet bullet)
                continue;

            ImpactResult impact = bullet.Tick(world, dt);

            if (impact.Kind == ImpactKind.None)
            {
                EntityId? hitEntity = FindHitEntity(bullet, world);
                if (hitEntity.HasValue)
                    impact = ImpactResult.Entity(hitEntity.Value);
            }

            switch (impact.Kind)
            {
                case ImpactKind.Block:
                case ImpactKind.Entity:
                    // TODO: CombatSystem.ApplyImpact(world, bullet, impact)
                    world.Entities.RemoveAt(i);
                    break;
                case ImpactKind.None:
                    if (bullet.TicksLeft <= 0)
                        world.Entities.RemoveAt(i);
                    break;
            }
        }
    }

    private static EntityId? FindHitEntity(Bullet bullet, World world)
    {
        // When enemies are added, check AABB overlap between bullet and each enemy here.
        return null;
    }
}
