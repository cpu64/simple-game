using System.Numerics;

public static class SpawnSystem
{
    public static void Process(World world)
    {
        long cycleTick = world.Tick % GameConstants.FullDayNightCycleTicks;

        if (cycleTick == 0)
            SpawnMobs(world, new PassiveMobFactory());

        if (cycleTick == GameConstants.DayNightDurationTicks)
            SpawnMobs(world, new HostileMobFactory());
    }

    private static void SpawnMobs(World world, IMobFactory factory)
    {
        world.Entities.Add(factory.CreateSkyMob(world.NextEntityId++, new Vector2(20, 10)));

        world.Entities.Add(factory.CreateGroundMob(world.NextEntityId++, new Vector2(24, 17)));

        world.Entities.Add(factory.CreateUndergroundMob(world.NextEntityId++, new Vector2(20, 24)));
    }
}
