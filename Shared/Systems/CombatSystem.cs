using System.Linq;

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
            damageable.TakeDamage(bullet.Damage);

            if (damageable.IsDead)
            {
                damageable.OnDeath(world);
            }
        }
    }

    public static void CheckEnemyContactDamage(World world, Enemy enemy, int damage = 10)
    {
        if (enemy.IsDead)
            return;

        foreach (Player player in world.Entities.OfType<Player>())
        {
            if (player.IsDead)
                continue;

            if (PhysicsSystem.Overlaps(enemy.Position, enemy.Size, player.Position, player.Size))
            {
                player.TakeDamage(damage);

                if (player.IsDead)
                {
                    player.OnDeath(world);
                }
            }
        }
    }
}
