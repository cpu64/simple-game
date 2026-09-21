using System;
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

            bool hitBlock =
                bullet.CollidesWithBlocks && PhysicsSystem.CollidesWithBlock(candidatePos.X, candidatePos.Y, bullet.Size.X, bullet.Size.Y, world.Blocks);

            IDamageable? hitEntity = FindHitEntity(bullet, candidatePos, world);

            if (hitEntity != null && hitBlock)
            {
                Entity targetEntity = (Entity)hitEntity;
                ICollidable collidable = (ICollidable)hitEntity;

                float closestEntityX = Math.Clamp(bullet.Position.X, targetEntity.Position.X, targetEntity.Position.X + collidable.Size.X);
                float closestEntityY = Math.Clamp(bullet.Position.Y, targetEntity.Position.Y, targetEntity.Position.Y + collidable.Size.Y);
                float entityDistSq = Vector2.DistanceSquared(bullet.Position, new Vector2(closestEntityX, closestEntityY));

                float minBlockDistSq = float.MaxValue;
                for (int b = 0; b < world.Blocks.Count; b++)
                {
                    Block block = world.Blocks[b];
                    if (PhysicsSystem.Overlaps(candidatePos, bullet.Size, block.Position, Vector2.One))
                    {
                        float closestBlockX = Math.Clamp(bullet.Position.X, block.Position.X, block.Position.X + 1.0f);
                        float closestBlockY = Math.Clamp(bullet.Position.Y, block.Position.Y, block.Position.Y + 1.0f);
                        float distSq = Vector2.DistanceSquared(bullet.Position, new Vector2(closestBlockX, closestBlockY));
                        if (distSq < minBlockDistSq)
                        {
                            minBlockDistSq = distSq;
                        }
                    }
                }

                if (entityDistSq > minBlockDistSq + 0.05f)
                {
                    hitEntity = null;
                }
            }

            if (hitEntity != null)
            {
                if (hitEntity.TakeDamage(bullet.Damage))
                {
                    if (hitEntity is IMoving moving && bullet.Velocity.LengthSquared() > 0.001f)
                    {
                        Vector2 knockback = Vector2.Normalize(bullet.Velocity) * bullet.KnockbackForce;
                        moving.Velocity += knockback;
                    }

                    if (hitEntity.IsDead)
                    {
                        hitEntity.OnDeath(world);
                    }
                }

                bullet.TicksLeft = 0;
            }
            else if (hitBlock)
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
        bool firedByPlayer = bullet.FiredByPlayer;

        for (int i = 0; i < world.Entities.Count; i++)
        {
            Entity entity = world.Entities[i];
            if (entity.Id == bullet.FiredBy)
                continue;

            if (firedByPlayer && entity is Player)
                continue;
            if (!firedByPlayer && entity is Enemy)
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

    private static void ResolveContactDamage(World world)
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
                    if (player.TakeDamage(enemy.ContactDamage))
                    {
                        float dirX = player.Position.X >= enemy.Position.X ? 1.0f : -1.0f;
                        player.Velocity = new Vector2(dirX * enemy.ContactKnockback, -4.5f);

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
