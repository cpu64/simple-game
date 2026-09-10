using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Net;

public class LocalServer
{
    private readonly SharedInputState input;
    private readonly RemoteServerConnection remoteServer;

    private readonly object worldLock = new object();
    private readonly Guid playerId;

    // Commands generated locally that are not yet included in the latest
    // authoritative world.
    //
    // For now we assume there is exactly one command per tick and that
    // commands are added in tick order.
    private readonly Queue<InputCommand> pendingCommands =
    new Queue<InputCommand>();

    private World world;

    private Thread simulationThread;
    private volatile bool running;

    public LocalServer(SharedInputState input, Guid playerId)
    {
        this.input = input;
        this.playerId = playerId;

        remoteServer = null;

        world = new World();
        world.Players.Add(playerId, new Player(playerId, 400, 300));
    }

    public LocalServer(
        SharedInputState input,
        Guid playerId,
        IPEndPoint endpoint)
    {
        this.input = input;
        this.playerId = playerId;

        remoteServer = new RemoteServerConnection(endpoint);
        remoteServer.Connect();

        remoteServer.Send(new RegisterUserMessage(playerId));

        IMessage message;

        while (world == null)
        {
            if (remoteServer.TryReceive(out message))
            {
                WorldMessage worldMessage = message as WorldMessage;

                if (worldMessage != null)
                {
                    world = worldMessage.World;
                }
            }

            Thread.Sleep(1);
        }
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

        if (remoteServer != null)
            remoteServer.Disconnect();
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
                    Thread.Sleep((int)(remaining * 1000.0));
                else
                    Thread.Sleep(0);
            }
        }
    }

    private void Tick()
    {
        // world.Print();

        if (remoteServer != null)
        {
            ReceiveLatestAuthoritativeWorld();
        }

        // A command advances the world by one tick.
        //
        // Therefore:
        //
        //     World @ 3 + Command @ 4 -> World @ 4
        //
        // The important invariant is:
        //
        //     Command.Tick == World.Tick
        //
        // means that command has already been applied to that world.
        //
        // Therefore the next command is always one tick ahead of the
        // current world.
        InputCommand command = new InputCommand(
            world.Tick + 1,
            playerId,
            input.Read());

        pendingCommands.Enqueue(command);

        if (remoteServer != null)
        {
            remoteServer.Send(new InputCommandMessage(command));
        }

        List<InputCommand> commands = new List<InputCommand>();
        commands.Add(command);

        world = Simulation.Tick(world, commands);
    }

    private void ReceiveLatestAuthoritativeWorld()
    {
        IMessage received;
        World latestAuthoritativeWorld = null;

        while (remoteServer.TryReceive(out received))
        {
            WorldMessage worldMessage = received as WorldMessage;

            if (worldMessage != null)
            {
                worldMessage.World.Print();
                latestAuthoritativeWorld = worldMessage.World;
            }
        }

        if (latestAuthoritativeWorld == null)
            return;

        lock (worldLock)
        {
            world = latestAuthoritativeWorld;

            // The authoritative world already contains every command
            // through its tick.
            //
            // Since Command.Tick == World.Tick means the command has
            // already been applied, commands <= world.Tick are no
            // longer needed for prediction/replay.
            while (pendingCommands.Count > 0 &&
                pendingCommands.Peek().Tick <= world.Tick)
            {
                pendingCommands.Dequeue();
            }

            // Replay every unacknowledged command in tick order.
            foreach (InputCommand command in pendingCommands)
            {
                List<InputCommand> commands = new List<InputCommand>();
                commands.Add(command);

                world = Simulation.Tick(world, commands);
            }
        }
    }

    public RenderWorldSnapshot GetRenderWorldSnapshot()
    {
        lock (worldLock)
        {
            return new RenderWorldSnapshot(world);
        }
    }
}
