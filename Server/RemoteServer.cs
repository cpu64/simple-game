using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;

public class RemoteServer
{
    private readonly CommandQueue commandQueue;

    private readonly object worldLock = new object();

    private readonly object clientsLock = new object();

    private readonly List<ClientConnection> clients = new List<ClientConnection>();

    private World world;

    private Thread simulationThread;
    private Thread networkThread;

    private volatile bool running;

    private TcpListener listener;

    public RemoteServer(string worldPath)
    {
        commandQueue = new CommandQueue();
        world = new World(worldPath);
        EnemySystem.SpawnSlime(world, new Vector2(25, 18));
    }

    public void Start(int port)
    {
        if (running)
            return;

        running = true;

        listener = new TcpListener(IPAddress.Any, port);

        listener.Start();

        simulationThread = new Thread(RunSimulation);

        simulationThread.IsBackground = true;
        simulationThread.Name = "Simulation";

        simulationThread.Start();

        networkThread = new Thread(RunNetwork);

        networkThread.IsBackground = true;
        networkThread.Name = "Network";

        networkThread.Start();

        Console.WriteLine("Remote server listening on port " + port + ".");
    }

    public void Stop()
    {
        running = false;

        if (listener != null)
            listener.Stop();

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                client.Dispose();
            }

            clients.Clear();
        }

        if (networkThread != null)
            networkThread.Join();

        if (simulationThread != null)
            simulationThread.Join();
    }

    private void RunNetwork()
    {
        while (running)
        {
            try
            {
                Console.WriteLine("Waiting for client...");

                TcpClient tcpClient = listener.AcceptTcpClient();

                ClientConnection client = new ClientConnection(tcpClient);

                lock (clientsLock)
                {
                    clients.Add(client);
                }

                Console.WriteLine("Client connected.");

                Thread clientThread = new Thread(() => HandleClient(client));

                clientThread.IsBackground = true;
                clientThread.Name = "Client";

                clientThread.Start();
            }
            catch (SocketException)
            {
                if (running)
                    throw;
            }
            catch (Exception exception)
            {
                if (running)
                {
                    Console.WriteLine("Network error: " + exception.Message);
                }
            }
        }
    }

    private void HandleClient(ClientConnection client)
    {
        try
        {
            while (running && client.Connected)
            {
                IBinarySerializable message = client.Receive();

                RegisterUserMessage registerMessage = message as RegisterUserMessage;

                if (registerMessage != null)
                {
                    RegisterPlayer(client, registerMessage.ClientId);
                    continue;
                }

                if (message is InputCommand command)
                {
                    commandQueue.Add(command);
                    continue;
                }
            }
        }
        catch (Exception exception)
        {
            if (running)
            {
                Console.WriteLine("Client disconnected: " + exception.Message);
            }
        }
        finally
        {
            lock (clientsLock)
            {
                clients.Remove(client);
            }

            client.Dispose();
        }
    }

    private void RegisterPlayer(ClientConnection client, Guid playerId)
    {
        Console.WriteLine("Registering player: " + playerId);

        World snapshot;

        lock (worldLock)
        {
            Player? player = world.Get<Player>().FirstOrDefault(p => p.UserId == playerId);

            if (player == null)
            {
                world.Entities.Add(new Player(world.NextEntityId++, new Vector2(20, 18), playerId));
            }

            snapshot = world.Copy();
        }

        Console.WriteLine("Sending initial world.");

        client.Send(snapshot);

        Console.WriteLine("Initial world sent.");
    }

    private void RunSimulation()
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
            world = Simulation.Tick(world, commandQueue.TakeAll());
        }

        if (world.Tick % GameConstants.SnapshotIntervalTicks == 0)
        {
            SendSnapshot();
        }
    }

    private void SendSnapshot()
    {
        World snapshot;

        lock (worldLock)
        {
            snapshot = world.Copy();
        }

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                if (!client.Connected)
                    continue;

                try
                {
                    client.Send(snapshot);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Failed to send snapshot: " + exception.Message);
                }
            }
        }
    }
}
