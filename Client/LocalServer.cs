using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
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

        world.Entities.Add(new Player(world.NextEntityId++, new Vector2(20, 18), playerId));
        EnemySystem.SpawnSlime(world, new Vector2(25, 18));
    }

    public LocalServer(SharedInputState input, Guid playerId, IPEndPoint endpoint)
    {
        this.input = input;
        this.playerId = playerId;

        remoteServer = new RemoteServerConnection(endpoint);

        remoteServer.Connect(new RegisterUserMessage(playerId));

        nextCommandSequence = 0;

        IBinarySerializable message;

        while (world == null)
        {
            if (!remoteServer.IsConnected)
                throw new IOException("Disconnected before receiving initial world.");

            if (remoteServer.TryReceive(out message))
            {
                World worldMessage = message as World;

                if (worldMessage != null)
                {
                    world = worldMessage;

                    AddAuthoritativeWorld(worldMessage);
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

            (KeyState Keys, Vector2 Pointer) = input.Read();

            InputCommand command = new InputCommand(nextCommandSequence++, playerId, Keys, Pointer);

            pendingCommands.Enqueue(command);

            if (remoteServer != null)
            {
                remoteServer.Send(command);
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
        IBinarySerializable received;

        World authoritativeWorld = null;

        while (remoteServer.TryReceive(out received))
        {
            authoritativeWorld = received as World;

            if (authoritativeWorld == null)
                continue;

            AddAuthoritativeWorld(authoritativeWorld);
        }

        if (authoritativeWorld != null)
            Reconcile(authoritativeWorld);
    }

    private void Reconcile(World authoritativeWorld)
    {
        world = authoritativeWorld.Copy();

        long lastAcknowledgedSequence = GetLastAcknowledgedSequence(authoritativeWorld);

        // Remove commands that the authoritative server has already processed.
        while (pendingCommands.Count > 0 && pendingCommands.Peek().Sequence <= lastAcknowledgedSequence)
        {
            pendingCommands.Dequeue();
        }

        // Replay every command that the authoritative server has not processed yet.
        // Only re-simulate local player kinematics during rollback replay to prevent desyncing autonomous systems.
        if (pendingCommands.Count > 0)
        {
            Player localPlayer = null;
            for (int i = 0; i < world.Entities.Count; i++)
            {
                if (world.Entities[i] is Player p && p.UserId == playerId)
                {
                    localPlayer = p;
                    break;
                }
            }

            if (localPlayer != null)
            {
                foreach (InputCommand command in pendingCommands)
                {
                    Simulation.ReplayLocalPlayer(world, localPlayer, command);
                }
            }
        }

        // Advance world.Tick to match predicted clock so remote player interpolation does not hitch
        world.Tick = authoritativeWorld.Tick + pendingCommands.Count;
    }

    private long GetLastAcknowledgedSequence(World authoritativeWorld)
    {
        for (int i = 0; i < authoritativeWorld.Entities.Count; i++)
        {
            if (authoritativeWorld.Entities[i] is Player p && p.UserId == playerId)
            {
                return p.LastCommand;
            }
        }

        return -1;
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

        authoritativeWorlds.Add(authoritativeWorld.Copy());
    }

    public RenderInput GetRenderInput()
    {
        lock (worldLock)
        {
            return new RenderInput(world.Copy(), authoritativeWorlds);
        }
    }
}
