using System.Numerics;

public static class DamageSystem
{
    public static void Process(World world)
    {
        foreach (Entity attacker in world.Entities)
        {
            if (attacker is not IDamaging damaging)
                continue;

            Vector2 attackerMin = attacker.Position - damaging.DamageBox * 0.5f;

            Vector2 attackerMax = attacker.Position + damaging.DamageBox * 0.5f;

            foreach (Entity target in world.Entities)
            {
                if (ReferenceEquals(attacker, target))
                    continue;

                if (attacker.GetType() == target.GetType())
                    continue;

                if (target is not IHealth health)
                    continue;

                if (health.IsDead)
                    continue;

                if (attacker is Bullet bullet && bullet.FiredBy == target.Id)
                    continue;

                Vector2 targetMin = target.Position - health.HitBox * 0.5f;

                Vector2 targetMax = target.Position + health.HitBox * 0.5f;

                if (!Intersects(attackerMin, attackerMax, targetMin, targetMax))
                {
                    continue;
                }

                if (health.InvincibleUntil > world.Tick)
                    continue;

                health.Health -= damaging.Damage;
                health.Health = System.Math.Max(health.Health, 0);
                health.InvincibleUntil = world.Tick + 10;

                ApplyKnockBack(attacker, target, damaging);
            }
        }
    }

    private static bool Intersects(Vector2 minA, Vector2 maxA, Vector2 minB, Vector2 maxB)
    {
        return maxA.X > minB.X && minA.X < maxB.X && maxA.Y > minB.Y && minA.Y < maxB.Y;
    }

    private static void ApplyKnockBack(Entity attacker, Entity target, IDamaging damaging)
    {
        if (target is not IMoving moving)
            return;

        Vector2 direction = target.Position - attacker.Position;

        if (direction.LengthSquared() <= 0.0001f)
            return;

        direction = Vector2.Normalize(direction);

        moving.Velocity += direction * damaging.KnockBackMultiplier;
    }
}
