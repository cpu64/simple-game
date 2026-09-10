using System.Collections.Generic;

public class CommandQueue
{
    private readonly object queueLock = new object();

    private readonly Dictionary<long, List<InputCommand>> commands =
    new Dictionary<long, List<InputCommand>>();

    public void Add(InputCommand command)
    {
        lock (queueLock)
        {
            List<InputCommand> commandsForTick;

            if (!commands.TryGetValue(
                command.Tick,
                out commandsForTick))
            {
                commandsForTick =
                new List<InputCommand>();

                commands.Add(
                    command.Tick,
                    commandsForTick
                );
            }

            commandsForTick.Add(command);
        }
    }

    public List<InputCommand> Take(long tick)
    {
        lock (queueLock)
        {
            List<InputCommand> result;

            if (commands.TryGetValue(
                tick,
                out result))
            {
                commands.Remove(tick);
                return result;
            }

            return new List<InputCommand>();
        }
    }
}
