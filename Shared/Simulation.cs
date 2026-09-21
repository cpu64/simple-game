using System.Collections.Generic;

public static class Simulation
{
    public static World Tick(World world, List<InputCommand> commands)
    {
        PlayerSystem.ProcessCommands(world, commands);

        ProjectileSystem.Update(world);

        world.Tick++;

        return world;
    }
}
