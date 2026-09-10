using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class RemoteServer
{
    private readonly CommandQueue commandQueue;

    private readonly object worldLock =
    new object();

    private readonly object clientsLock =
    new object();

    private readonly List<ClientConnection> clients =
    new List<ClientConnection>();

    private World world;

    private Thread simulationThread;
    private Thread networkThread;

    private volatile bool running;

    private TcpListener listener;

    public RemoteServer()
    {
        commandQueue = new CommandQueue();
        world = new World();
    }

    public void Start(int port)
    {
        if (running)
            return;

        running = true;

        listener =
        new TcpListener(
            IPAddress.Any,
            port);

        listener.Start();

        simulationThread =
        new Thread(RunSimulation);

        simulationThread.IsBackground = true;
        simulationThread.Name = "Simulation";

        simulationThread.Start();

        networkThread =
        new Thread(RunNetwork);

        networkThread.IsBackground = true;
        networkThread.Name = "Network";

        networkThread.Start();

        Console.WriteLine(
            "Remote server listening on port " +
            port + ".");
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

                TcpClient tcpClient =
                listener.AcceptTcpClient();

                ClientConnection client =
                new ClientConnection(tcpClient);

                lock (clientsLock)
                {
                    clients.Add(client);
                }

                Console.WriteLine("Client connected.");

                Thread clientThread =
                new Thread(() => HandleClient(client));

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
                    Console.WriteLine(
                        "Network error: " +
                        exception.Message);
                }
            }
        }
    }

    private void HandleClient(ClientConnection client)
    {
        try
        {
            while (running && client.IsConnected)
            {
                IMessage message =
                client.Receive();

                Console.WriteLine(
                    "Received message: " +
                    message.GetType().Name);


                RegisterUserMessage registerMessage =
                message as RegisterUserMessage;

                if (registerMessage != null)
                {
                    RegisterPlayer(
                        client,
                        registerMessage.ClientId);

                    continue;
                }

                InputCommandMessage commandMessage =
                message as InputCommandMessage;

                if (commandMessage != null)
                {
                    InputCommand command =
                    new InputCommand(
                        commandMessage.Command.Tick,
                        commandMessage.Command.PlayerId,
                        commandMessage.Command.Input);

                    commandQueue.Add(command);
                }
            }
        }
        catch (Exception exception)
        {
            if (running)
            {
                Console.WriteLine(
                    "Client disconnected: " +
                    exception.Message);
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

    private void RegisterPlayer(
        ClientConnection client,
        Guid playerId)
    {
        Console.WriteLine(
            "Registering player: " + playerId);

        client.RegisterPlayer(playerId);

        World snapshot;

        lock (worldLock)
        {
            if (!world.Players.ContainsKey(playerId))
            {
                world.Players.Add(
                    playerId,
                    new Player(
                        playerId,
                        400,
                        300));
            }

            snapshot = new World(world);
        }
        Console.WriteLine("Sending initial world.");

        client.Send(
            new WorldMessage(snapshot));
        Console.WriteLine("Initial world sent.");

    }

    private void RunSimulation()
    {
        Stopwatch stopwatch =
        Stopwatch.StartNew();

        double nextTick =
        stopwatch.Elapsed.TotalSeconds;

        while (running)
        {
            double now =
            stopwatch.Elapsed.TotalSeconds;

            if (now >= nextTick)
            {
                Tick();

                nextTick +=
                GameConstants.SimulationTickDuration;

                if (now - nextTick > 0.25)
                    nextTick = now;
            }
            else
            {
                double remaining =
                nextTick - now;

                if (remaining > 0.001)
                {
                    Thread.Sleep(
                        (int)(remaining * 1000.0));
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
        List<InputCommand> commands;

        lock (worldLock)
        {
            long nextTick =
            world.Tick + 1;

            commands =
            commandQueue.Take(nextTick);

            world =
            Simulation.Tick(
                world,
                commands);
        }

        if (world.Tick %
            GameConstants.SnapshotIntervalTicks == 0)
        {
            SendSnapshot();
        }
    }

    private void SendSnapshot()
    {
        WorldMessage message;

        lock (worldLock)
        {
            message =
            new WorldMessage(
                new World(world));
        }

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                if (!client.IsConnected)
                    continue;

                try
                {
                    message.World.Print();
                    client.Send(message);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(
                        "Failed to send snapshot: " +
                        exception.Message);
                }
            }
        }
    }

    public World GetSnapshot()
    {
        lock (worldLock)
        {
            return new World(world);
        }
    }
}
