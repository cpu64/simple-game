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
            List<InputCommand> result = new List<InputCommand>(playerQueues.Count * 2);

            foreach (var kvp in playerQueues)
            {
                Queue<InputCommand> queue = kvp.Value;
                if (queue.Count == 0)
                    continue;

                while (queue.Count > 60)
                {
                    queue.Dequeue();
                }

                result.Add(queue.Dequeue());
            }

            return result;
        }
    }

    public void RemovePlayer(Guid playerId)
    {
        lock (queueLock)
        {
            playerQueues.Remove(playerId);
        }
    }
}
