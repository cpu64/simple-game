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

    public static void ReplayLocalPlayer(World world, Player localPlayer, InputCommand command)
    {
        if (localPlayer.IsDead)
            return;

        float dt = (float)GameConstants.SimulationTickDuration;
        Vector2 inputMovement = PlayerSystem.ComputeInputMovement(command.Keys, dt);
        PhysicsSystem.StepBody(localPlayer, world.Blocks, dt, inputMovement);
        localPlayer.LastCommand = command.Sequence;
    }
}
