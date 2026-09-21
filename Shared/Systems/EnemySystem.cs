using System.Numerics;

public static class EnemySystem
{
    public static void Update(World world)
    {
        float dt = (float)GameConstants.SimulationTickDuration;

        for (int i = world.Entities.Count - 1; i >= 0; i--)
        {
            if (world.Entities[i] is not Enemy enemy)
                continue;

            if (enemy.IsDead)
            {
                world.Entities.RemoveAt(i);
                continue;
            }

            enemy.Tick(world, dt);

            if (enemy.IsDead)
            {
                world.Entities.RemoveAt(i);
            }
        }
    }

    public static Slime SpawnSlime(World world, Vector2 position)
    {
        Slime slime = new Slime(world.NextEntityId++, position);
        world.Entities.Add(slime);
        return slime;
    }
}
