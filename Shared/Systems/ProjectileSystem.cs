using System.Numerics;

public static class ProjectileSystem
{
    public const float BulletSpeed = 20.0f;
    public const float HeavyBulletSpeed = 16.0f;

    public static void SpawnSimpleBullet(World world, Player player, Vector2 clickPosition)
    {
        Vector2 spawnPos = player.Position + player.Size * 0.5f;
        Vector2 direction = clickPosition - spawnPos;

        if (direction.LengthSquared() <= 0.0001f)
            return;

        direction = Vector2.Normalize(direction);

        world.Entities.Add(new SimpleBullet(world.NextEntityId++, spawnPos, player.Id, direction * BulletSpeed, ticksLeft: 300, firedByPlayer: true));
    }

    public static void SpawnHeavyBullet(World world, Player player, Vector2 clickPosition)
    {
        Vector2 spawnPos = player.Position + player.Size * 0.5f;
        Vector2 direction = clickPosition - spawnPos;

        if (direction.LengthSquared() <= 0.0001f)
            return;

        direction = Vector2.Normalize(direction);

        world.Entities.Add(new HeavyBullet(world.NextEntityId++, spawnPos, player.Id, direction * HeavyBulletSpeed, ticksLeft: 300, firedByPlayer: true));
    }
}
