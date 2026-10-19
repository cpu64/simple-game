public sealed class ClientCommandFactory : CommandFactory
{
    public override Command? Create(string input)
    {
        return CreateSharedCommand(input);
    }
}
