namespace Client;

using System;
using System.Net;
using System.Text.Json;
using System.Threading;

public static class Client
{
    public static void Main(string[] args)
    {
        var console = new ConsoleUI(new ClientCommandFactory());
        Thread consoleThread = new Thread(console.Run) { IsBackground = true };
        consoleThread.Start();

        Logger.Instance.AddSink(new ConsoleUISink(console));
        Logger.Instance.MinimumLevel = LogLevel.Trace;
        console.WriteLine("Logger Minimum Level: Trace");

        string serverAddress = args.Length == 0 ? null : args[0];

        SharedInputState input = new SharedInputState();

        Guid playerId = Guid.NewGuid();

        console.WriteLine("Player ID: " + playerId);

        LocalServer localServer = null;
        RenderWorldUpdater renderWorldUpdater = new RenderWorldUpdater();

        if (string.IsNullOrEmpty(serverAddress))
        {
            localServer = new LocalServer(input, renderWorldUpdater, playerId, "world.json");
        }
        else
        {
            string[] parts = serverAddress.Split(':');

            if (parts.Length != 2)
            {
                console.WriteLine("Invalid server address. " + "Expected ip:port.");
                return;
            }

            IPAddress address;

            if (!IPAddress.TryParse(parts[0], out address))
            {
                console.WriteLine("Invalid IP address: " + parts[0]);
                return;
            }

            int port;

            if (!int.TryParse(parts[1], out port) || port < 1 || port > 65535)
            {
                console.WriteLine("Invalid port: " + parts[1]);
                return;
            }

            localServer = new LocalServer(input, renderWorldUpdater, playerId, new IPEndPoint(address, port));
        }

        localServer.Start();

        GameWindow.Run(localServer, input, renderWorldUpdater, playerId);
    }
}
