public static class GameConstants
{
    // Networking
    public const int SnapshotIntervalTicks = 10;

    // Client rendering
    public const int TargetFrameRate = 60;
    public const int InterpolationBufferTicks = 5;
    public const int InterpolationDelayTicks = InterpolationBufferTicks + SnapshotIntervalTicks;
    public const int InterpolationBufferSize = 2 + (InterpolationDelayTicks - 1) / SnapshotIntervalTicks;

    // Simulation
    public const int SimulationTickRate = 60;
    public const double SimulationTickDuration = 1.0 / SimulationTickRate;
}
