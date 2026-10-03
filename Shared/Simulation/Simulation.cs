using System.Collections.Generic;

public class Simulation
{
    public static World Tick(World world, List<InputCommand> commands)
    {
        CleanupSystem.Process(world); // remove dead 1 tick later to allow render to know about their death

        PlayerCommandSystem.Process(world, commands);

        SpawnSystem.Process(world);

        AiSystem.Process(world);

        MovementSystem.Process(world);

        DamageSystem.Process(world);

        world.Tick++;

        return world;
    }
}
