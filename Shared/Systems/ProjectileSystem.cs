using System.Numerics;

public static class ProjectileSystem
{
    public const float BulletSpeed = 20.0f;
    public const float HeavyBulletSpeed = 16.0f;

    public static void SpawnSimpleBullet(World world, Player player, Vector2 clickPosition)
    {
        Vector2 direction = clickPosition - player.Position;

        if (direction.LengthSquared() <= 0.0001f)
            return;

        direction = Vector2.Normalize(direction);

        world.Entities.Add(new SimpleBullet(world.NextEntityId++, player.Position, player.Id, direction * BulletSpeed, ticksLeft: 300));
    }

    public static void SpawnHeavyBullet(World world, Player player, Vector2 clickPosition)
    {
        Vector2 direction = clickPosition - player.Position;

        if (direction.LengthSquared() <= 0.0001f)
            return;

        direction = Vector2.Normalize(direction);

        world.Entities.Add(new HeavyBullet(world.NextEntityId++, player.Position, player.Id, direction * HeavyBulletSpeed, ticksLeft: 300));
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
                    world.Entities.RemoveAt(i);
                    break;
                case ImpactKind.Entity:
                    CombatSystem.ApplyImpact(world, bullet, impact);
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
        foreach (Entity entity in world.Entities)
        {
            if (entity.Id == bullet.FiredBy)
                continue;

            if (entity is not (IDamageable damageable and ICollidable collidable))
                continue;

            if (damageable.IsDead)
                continue;

            if (PhysicsSystem.Overlaps(bullet.Position, bullet.Size, entity.Position, collidable.Size))
            {
                return entity.Id;
            }
        }

        return null;
    }
}
