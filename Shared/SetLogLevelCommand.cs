public sealed class SetLogLevelCommand : Command
{
    private readonly LogLevel _newLevel;

    private LogLevel _previousLevel;
    private bool _executed;

    public SetLogLevelCommand(LogLevel newLevel)
    {
        _newLevel = newLevel;
    }

    public override void Execute()
    {
        Logger logger = Logger.Instance;

        _previousLevel = logger.MinimumLevel;
        logger.MinimumLevel = _newLevel;
        _executed = true;
    }

    public override void Undo()
    {
        if (!_executed)
            return;

        Logger.Instance.MinimumLevel = _previousLevel;
        _executed = false;
    }
}
