using System;
using System.Collections.Generic;

public class CommandQueue
{
    private readonly object queueLock = new object();

    private readonly Dictionary<Guid, Queue<InputCommand>> playerQueues = new Dictionary<Guid, Queue<InputCommand>>();

    public void Add(InputCommand command)
    {
        lock (queueLock)
        {
            if (!playerQueues.TryGetValue(command.PlayerId, out Queue<InputCommand> queue))
            {
                queue = new Queue<InputCommand>();
                playerQueues[command.PlayerId] = queue;
            }

            queue.Enqueue(command);
        }
    }

    public List<InputCommand> TakeOnePerPlayer()
    {
        lock (queueLock)
        {
            List<InputCommand> result = new List<InputCommand>(playerQueues.Count);

            foreach (var kvp in playerQueues)
            {
                if (kvp.Value.Count > 0)
                {
                    result.Add(kvp.Value.Dequeue());
                }
            }

            return result;
        }
    }

    public List<InputCommand> TakeAll()
    {
        lock (queueLock)
        {
            List<InputCommand> result = new List<InputCommand>();

            foreach (var queue in playerQueues.Values)
            {
                while (queue.Count > 0)
                {
                    result.Add(queue.Dequeue());
                }
            }

            return result;
        }
    }
}
