using System;
using System.Collections.Generic;

public class CommandQueue
{
    private readonly object queueLock = new object();

    private readonly Queue<InputCommand> commands = new Queue<InputCommand>();

    public void Add(InputCommand command)
    {
        lock (queueLock)
        {
            commands.Enqueue(command);
        }
    }

    public List<InputCommand> TakeAll()
    {
        lock (queueLock)
        {
            List<InputCommand> result = new List<InputCommand>(commands);

            commands.Clear();

            return result;
        }
    }

    public void RemovePlayer(Guid playerId)
    {
        lock (queueLock)
        {
            Queue<InputCommand> remaining = new Queue<InputCommand>();

            while (commands.Count > 0)
            {
                InputCommand command = commands.Dequeue();

                if (command.PlayerId != playerId)
                    remaining.Enqueue(command);
            }

            while (remaining.Count > 0)
            {
                commands.Enqueue(remaining.Dequeue());
            }
        }
    }
}
