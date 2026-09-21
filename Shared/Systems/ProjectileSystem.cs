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
        CombatSystem.Resolve(world);
    }
}
