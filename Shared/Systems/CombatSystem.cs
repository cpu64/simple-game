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

            if (damageable.IsDead && target is Enemy enemy)
            {
                enemy.OnDeath(world);
            }
        }
    }
}
