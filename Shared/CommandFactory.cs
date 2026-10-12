public abstract class CommandFactory
{
    public abstract Command? Create(string input);

    protected static Command? CreateSharedCommand(string input)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (
            parts.Length == 2
            && parts[0].Equals("loglevel", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse(parts[1], true, out LogLevel level)
            && Enum.IsDefined(typeof(LogLevel), level)
        )
        {
            return new SetLogLevelCommand(level);
        }

        return null;
    }
}
