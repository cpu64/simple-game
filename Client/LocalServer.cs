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
    private readonly RenderWorldUpdater renderWorldUpdater;
    private readonly RemoteServerConnection remoteServer;

    private readonly RingBuffer<RemotePlayerSnapshot> remoteSnapshots = new RingBuffer<RemotePlayerSnapshot>(GameConstants.InterpolationBufferSize);

    private World currentWorld;

    private Dictionary<Guid, Vector2> currentRemotePlayerPosition = new Dictionary<Guid, Vector2>();

    private readonly object worldLock = new object();

    private readonly Guid playerId;

    // Commands generated locally that have not yet been acknowledged by the authoritative server.
    private readonly Queue<InputCommand> pendingCommands = new Queue<InputCommand>();

    // Monotonically increasing sequence number owned by this player.
    private long nextCommandSequence;

    private World world;

    private Thread simulationThread;
    private volatile bool running;

    public LocalServer(SharedInputState input, RenderWorldUpdater renderWorldUpdater, Guid playerId, string worldPath)
    {
        this.input = input;
        this.renderWorldUpdater = renderWorldUpdater;
        this.playerId = playerId;

        remoteServer = null;

        nextCommandSequence = 0;

        world = new World(worldPath);

        world.Entities.Add(new Player(world.NextEntityId++, new Vector2(20, 18), playerId, -1, new Vector2(1.0f, 1.0f)));
        currentWorld = world.Copy();
    }

    public LocalServer(SharedInputState input, RenderWorldUpdater renderWorldUpdater, Guid playerId, IPEndPoint endpoint)
    {
        this.input = input;
        this.renderWorldUpdater = renderWorldUpdater;
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
                    currentWorld = world.Copy();

                    AddRemoteSnapshot(worldMessage);
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

    public Player? GetLocalPlayer(Guid playerId)
    {
        lock (worldLock)
        {
            return currentWorld?.Entities.OfType<Player>().FirstOrDefault(p => p.UserId == playerId);
        }
    }

    public Player? GetLocalPlayer()
    {
        return GetLocalPlayer(playerId);
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

            if (remoteServer != null)
            {
                pendingCommands.Enqueue(command);
                remoteServer.Send(command);
            }

            World oldWorld = currentWorld;

            Dictionary<Guid, Vector2> previousRemotePlayerPosition = currentRemotePlayerPosition;

            ApplyCommand(command);

            currentRemotePlayerPosition = GetRemotePlayerPositions(world.Tick - GameConstants.InterpolationDelayTicks);

            currentWorld = world.Copy();

            renderWorldUpdater.UpdateData(oldWorld, currentWorld, previousRemotePlayerPosition, currentRemotePlayerPosition);
        }
    }

    private Dictionary<Guid, Vector2> GetRemotePlayerPositions(long tick)
    {
        Dictionary<Guid, Vector2> positions = new Dictionary<Guid, Vector2>();

        if (remoteSnapshots.Count == 0)
            return positions;

        RemotePlayerSnapshot first = remoteSnapshots.Get(0);

        if (tick <= first.Tick)
        {
            foreach (var pair in first.Positions)
                positions[pair.Key] = pair.Value;

            return positions;
        }

        for (int i = 0; i < remoteSnapshots.Count - 1; i++)
        {
            RemotePlayerSnapshot before = remoteSnapshots.Get(i);
            RemotePlayerSnapshot after = remoteSnapshots.Get(i + 1);

            if (before.Tick <= tick && tick <= after.Tick)
            {
                float alpha = (float)((tick - before.Tick) / (double)(after.Tick - before.Tick));

                foreach (var pair in before.Positions)
                {
                    Guid playerId = pair.Key;

                    if (!after.Positions.TryGetValue(playerId, out Vector2 nextPosition))
                        continue;

                    positions[playerId] = Vector2.Lerp(pair.Value, nextPosition, alpha);
                }

                return positions;
            }
        }

        // No future snapshot exists: hold the latest known positions.
        RemotePlayerSnapshot latest = remoteSnapshots.Get(remoteSnapshots.Count - 1);

        foreach (var pair in latest.Positions)
            positions[pair.Key] = pair.Value;

        return positions;
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

            AddRemoteSnapshot(authoritativeWorld);
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
        Player? player = authoritativeWorld.Entities.OfType<Player>().FirstOrDefault(p => p.UserId == playerId);

        if (player == null)
            return -1;

        return player.LastCommand;
    }

    private void AddRemoteSnapshot(World authoritativeWorld)
    {
        if (authoritativeWorld == null)
            return;

        if (remoteSnapshots.Count > 0)
        {
            RemotePlayerSnapshot latest = remoteSnapshots.Get(remoteSnapshots.Count - 1);

            if (authoritativeWorld.Tick <= latest.Tick)
            {
                return;
            }
        }

        remoteSnapshots.Add(new RemotePlayerSnapshot(authoritativeWorld, playerId));
    }
}
