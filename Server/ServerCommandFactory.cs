using System;

public sealed class ServerCommandFactory : CommandFactory
{
    private readonly Action _requestStop;

    public ServerCommandFactory(Action requestStop)
    {
        _requestStop = requestStop;
    }

    public override Command? Create(string input)
    {
        if (input.Trim().Equals("stop", System.StringComparison.OrdinalIgnoreCase))
        {
            return new StopServerCommand(_requestStop);
        }

        return CreateSharedCommand(input);
    }
}
