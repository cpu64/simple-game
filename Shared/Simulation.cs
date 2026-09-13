using System.Collections.Generic;

public class Simulation
{
    private const double PlayerSpeed = 200.0;

    public static World Tick(World world, List<InputCommand> commands)
    {
        foreach (InputCommand command in commands)
        {
            Player player;

            if (!world.Players.TryGetValue(command.PlayerId, out player))
            {
                continue;
            }

            if (player.LastCommand >= command.Sequence)
                continue;

            player.LastCommand = command.Sequence;

            InputState input = command.Input;

            if ((input & InputState.Left) != 0)
                player.X -= PlayerSpeed * GameConstants.SimulationTickDuration;

            if ((input & InputState.Right) != 0)
                player.X += PlayerSpeed * GameConstants.SimulationTickDuration;

            if ((input & InputState.Up) != 0)
                player.Y -= PlayerSpeed * GameConstants.SimulationTickDuration;
        }

        world.Tick++;

        return world;
    }
}
