using System.Linq;
using System.Numerics;

public static class CombatSystem
{
    public static void ApplyImpact(World world, Bullet bullet, ImpactResult impact)
    {
        if (impact.Kind != ImpactKind.Entity || !impact.HitEntityId.HasValue)
            return;

        EntityId targetId = impact.HitEntityId.Value;
        Entity? target = world.Entities.FirstOrDefault(e => e.Id == targetId);

        if (target is IDamageable damageable)
        {
            if (damageable.TakeDamage(bullet.Damage))
            {
                if (target is IMoving moving && bullet.Velocity.LengthSquared() > 0.001f)
                {
                    Vector2 knockback = Vector2.Normalize(bullet.Velocity) * 3.0f;
                    moving.Velocity += knockback;
                }

                if (damageable.IsDead)
                {
                    damageable.OnDeath(world);
                }
            }
        }
    }

    public static void CheckEnemyContactDamage(World world, Enemy enemy, int damage = 10, float knockbackForce = 6.0f)
    {
        if (enemy.IsDead)
            return;

        foreach (Player player in world.Entities.OfType<Player>())
        {
            if (player.IsDead || player.IsInvulnerable)
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
