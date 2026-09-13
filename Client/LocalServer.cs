using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

public class LocalServer
{
    private readonly SharedInputState input;
    private readonly RemoteServerConnection remoteServer;

    private readonly RingBuffer<World> authoritativeWorlds = new RingBuffer<World>(GameConstants.InterpolationBufferSize);

    private readonly object worldLock = new object();

    private readonly Guid playerId;

    // Commands generated locally that have not yet been acknowledged by the authoritative server.
    private readonly Queue<InputCommand> pendingCommands = new Queue<InputCommand>();

    // Monotonically increasing sequence number owned by this player.
    private long nextCommandSequence;

    private World world;

    private Thread simulationThread;
    private volatile bool running;

    public LocalServer(SharedInputState input, Guid playerId, string worldPath)
    {
        this.input = input;
        this.playerId = playerId;

        remoteServer = null;

        nextCommandSequence = 0;

        world = new World(worldPath);

        world.Players.Add(playerId, new Player(playerId, 20, 18));
    }

    public LocalServer(SharedInputState input, Guid playerId, IPEndPoint endpoint)
    {
        this.input = input;
        this.playerId = playerId;

        remoteServer = new RemoteServerConnection(endpoint);

        remoteServer.Connect(new RegisterUserMessage(playerId));

        nextCommandSequence = 0;

        IMessage message;

        while (world == null)
        {
            if (!remoteServer.IsConnected)
                throw new IOException("Disconnected before receiving initial world.");

            if (remoteServer.TryReceive(out message))
            {
                WorldMessage worldMessage = message as WorldMessage;

                if (worldMessage != null)
                {
                    world = worldMessage.World;

                    AddAuthoritativeWorld(worldMessage.World);
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
                {
                    Thread.Sleep((int)(remaining * 1000.0));
                }
                else
                {
                    Thread.Sleep(0);
                }
            }
        }
    }

    private void Tick()
    {
        lock (worldLock)
        {
            if (remoteServer != null)
            {
                ReceiveLatestAuthoritativeWorld();
            }

            InputCommand command = new InputCommand(nextCommandSequence++, playerId, input.Read());

            pendingCommands.Enqueue(command);

            if (remoteServer != null)
            {
                remoteServer.Send(new InputCommandMessage(command));
            }

            ApplyCommand(command);
        }
    }

    private void ApplyCommand(InputCommand command)
    {
        List<InputCommand> commands = new List<InputCommand>();

        commands.Add(command);

        world = Simulation.Tick(world, commands);
    }

    private void ReceiveLatestAuthoritativeWorld()
    {
        IMessage received;

        World authoritativeWorld = null;

        while (remoteServer.TryReceive(out received))
        {
            WorldMessage worldMessage = received as WorldMessage;

            if (worldMessage == null)
                continue;

            authoritativeWorld = worldMessage.World;
            AddAuthoritativeWorld(authoritativeWorld);
        }

        if (authoritativeWorld != null)
            Reconcile(authoritativeWorld);
    }

    private void Reconcile(World authoritativeWorld)
    {
        world = authoritativeWorld;

        long lastAcknowledgedSequence = GetLastAcknowledgedSequence(authoritativeWorld);

        // Remove commands that the authoritative server has already processed.
        while (pendingCommands.Count > 0 && pendingCommands.Peek().Sequence <= lastAcknowledgedSequence)
        {
            pendingCommands.Dequeue();
        }

        // Replay every command that the authoritative server has not processed yet.
        foreach (InputCommand command in pendingCommands)
        {
            ApplyCommand(command);
        }
    }

    private long GetLastAcknowledgedSequence(World authoritativeWorld)
    {
        if (!authoritativeWorld.Players.TryGetValue(playerId, out Player player))
        {
            return -1;
        }

        return player.LastCommand;
    }

    private void AddAuthoritativeWorld(World authoritativeWorld)
    {
        if (authoritativeWorld == null)
            return;

        if (authoritativeWorlds.Count > 0)
        {
            World latest = authoritativeWorlds.Get(authoritativeWorlds.Count - 1);

            if (authoritativeWorld.Tick <= latest.Tick)
            {
                return;
            }
        }

        authoritativeWorlds.Add(new World(authoritativeWorld));
    }

    public RenderInput GetRenderInput()
    {
        lock (worldLock)
        {
            return new RenderInput(world, authoritativeWorlds);
        }
    }
}
