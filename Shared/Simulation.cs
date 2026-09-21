using System.Collections.Generic;
using System.Numerics;

public static class Simulation
{
    public static World Tick(World world, List<InputCommand> commands)
    {
        PlayerSystem.ProcessCommands(world, commands);

        EnemySystem.Update(world);

        CombatSystem.Resolve(world);

        world.PruneDead();

        world.Tick++;

        return world;
    }
}
