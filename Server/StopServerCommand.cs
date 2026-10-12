using System;

public sealed class StopServerCommand : Command
{
    private readonly Action _requestStop;

    public StopServerCommand(Action requestStop)
    {
        _requestStop = requestStop;
    }

    public override void Execute()
    {
        _requestStop();
    }

    public override void Undo()
    {
        // Stopping a server cannot be undone.
    }
}
