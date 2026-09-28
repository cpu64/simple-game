using System.Collections.Generic;

public class Simulation
{
    public static World Tick(World world, List<InputCommand> commands)
    {
        PlayerCommandSystem.Process(world, commands);

        SpawnSystem.Process(world);

        AiSystem.Process(world);

        MovementSystem.Process(world);

        DamageSystem.Process(world);

        CleanupSystem.Process(world);

        world.Tick++;

        return world;
    }
}
