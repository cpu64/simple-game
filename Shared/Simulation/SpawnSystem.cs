using System.Numerics;

public static class SpawnSystem
{
    public static void Process(World world)
    {
        if (world.Tick % 60 * 5 == 0)
            world.Entities.Add(new Slime(world.NextEntityId++, new Vector2(20, 18), 0, new Vector2(1.0f, 1.0f)));
    }
}
