using System.Numerics;

public static class CombatSystem
{
    public static void Resolve(World world)
    {
        float dt = (float)GameConstants.SimulationTickDuration;
        ResolveProjectiles(world, dt);
        ResolveContactDamage(world);
    }

    private static void ResolveProjectiles(World world, float dt)
    {
        for (int i = 0; i < world.Entities.Count; i++)
        {
            if (world.Entities[i] is not Bullet bullet || bullet.IsDead)
                continue;

            bullet.TicksLeft--;
            if (bullet.TicksLeft <= 0)
                continue;

            float velY = bullet.Velocity.Y + (bullet.GravityScale * PhysicsSystem.GravityConstant * dt);
            bullet.Velocity = new Vector2(bullet.Velocity.X, velY);

            Vector2 candidatePos = bullet.Position + bullet.Velocity * dt;

            IDamageable? hitEntity = FindHitEntity(bullet, candidatePos, world);

            if (hitEntity != null)
            {
                if (hitEntity.TakeDamage(bullet.Damage))
                {
                    if (hitEntity is IMoving moving && bullet.Velocity.LengthSquared() > 0.001f)
                    {
                        Vector2 knockback = Vector2.Normalize(bullet.Velocity) * 3.0f;
                        moving.Velocity += knockback;
                    }

                    if (hitEntity.IsDead)
                    {
                        hitEntity.OnDeath(world);
                    }
                }

                bullet.TicksLeft = 0;
            }
            else if (bullet.CollidesWithBlocks && PhysicsSystem.CollidesWithBlock(candidatePos.X, candidatePos.Y, bullet.Size.X, bullet.Size.Y, world.Blocks))
            {
                bullet.TicksLeft = 0;
            }
            else
            {
                bullet.Position = candidatePos;
            }
        }
    }

    private static IDamageable? FindHitEntity(Bullet bullet, Vector2 candidatePos, World world)
    {
        for (int i = 0; i < world.Entities.Count; i++)
        {
            Entity entity = world.Entities[i];
            if (entity.Id == bullet.FiredBy)
                continue;

            if (entity is not (IDamageable damageable and ICollidable collidable))
                continue;

            if (damageable.IsDead)
                continue;

            if (
                PhysicsSystem.Overlaps(bullet.Position, bullet.Size, entity.Position, collidable.Size)
                || PhysicsSystem.Overlaps(candidatePos, bullet.Size, entity.Position, collidable.Size)
            )
            {
                return damageable;
            }
        }

        return null;
    }

    private static void ResolveContactDamage(World world, int damage = 10, float knockbackForce = 6.0f)
    {
        for (int i = 0; i < world.Entities.Count; i++)
        {
            if (world.Entities[i] is not Enemy enemy || enemy.IsDead)
                continue;

            for (int j = 0; j < world.Entities.Count; j++)
            {
                if (world.Entities[j] is not Player player || player.IsDead || player.IsInvulnerable)
                    continue;

                if (PhysicsSystem.Overlaps(enemy.Position, enemy.Size, player.Position, player.Size))
                {
                    if (player.TakeDamage(damage))
                    {
                        float dirX = player.Position.X >= enemy.Position.X ? 1.0f : -1.0f;
                        player.Velocity = new Vector2(dirX * knockbackForce, -4.5f);

                        if (player.IsDead)
                        {
                            player.OnDeath(world);
                        }
                    }
                }
            }
        }
    }
}
