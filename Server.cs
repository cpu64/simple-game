using System;
using System.Diagnostics;
using System.Threading;

public class Server
{
    public long TickNumber { get; private set; }

    private const double PlayerSpeed = 200.0;

    private readonly World world;
    private readonly SharedInputState input;

    private readonly object worldLock = new object();

    private Thread simulationThread;
    private volatile bool running;

    public Server(SharedInputState input)
    {
        this.input = input;
        world = new World();
        TickNumber = 0;
    }

    public void Start()
    {
        if (running)
            return;

        running = true;

        simulationThread = new Thread(Run);
        simulationThread.IsBackground = true;
        simulationThread.Start();
    }

    public void Stop()
    {
        running = false;

        if (simulationThread != null)
            simulationThread.Join();
    }

    private void Run()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        double nextTick = stopwatch.Elapsed.TotalSeconds;

        while (running)
        {
            double now = stopwatch.Elapsed.TotalSeconds;

            if (now >= nextTick)
            {
                Tick();

                nextTick += GameConstants.SimulationTickDuration;

                if (now - nextTick > 0.25)
                    nextTick = now;
            }
            else
            {
                double remaining = nextTick - now;
                if (remaining > 0.001)
                    Thread.Sleep(1);
            }
        }
    }

    private void Tick()
    {
        InputState currentInput = input.Read();

        InputCommand command = new InputCommand(TickNumber, currentInput);

        lock (worldLock)
        {
            if ((command.Input & InputState.Left) != 0)
                world.Player.X -= PlayerSpeed * GameConstants.SimulationTickDuration;

            if ((command.Input & InputState.Right) != 0)
                world.Player.X += PlayerSpeed * GameConstants.SimulationTickDuration;

            if ((command.Input & InputState.Up) != 0)
                world.Player.Y -= PlayerSpeed * GameConstants.SimulationTickDuration;

            TickNumber++;
        }

        // Later:
        //
        // SendCommand(command);
    }

    public WorldSnapshot GetSnapshot()
    {
        lock (worldLock)
        {
            return new WorldSnapshot(world.Player);
        }
    }
}
