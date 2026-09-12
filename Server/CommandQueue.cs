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
}
