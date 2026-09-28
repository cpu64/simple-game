using System.Collections.Generic;

public static class CleanupSystem
{
    public static void Process(World world)
    {
        world.Entities.RemoveAll(entity =>
        {
            if (entity is IHealth alive && alive.IsDead)
                return true;

            if (entity is ILifespan lifespan && lifespan.TicksLeft-- <= 0)
                return true;

            return false;
        });
    }
}
