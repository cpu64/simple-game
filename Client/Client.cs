namespace Client;

using System;
using System.Linq;
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

        Logger.Instance.Attach(new ConsoleUISinkAdapter(console));

        string? logArg = args.FirstOrDefault(a => a.StartsWith("--log=", StringComparison.OrdinalIgnoreCase))?[6..];
        if (logArg != null)
        {
            string[] targets = logArg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (targets.Contains("append", StringComparer.OrdinalIgnoreCase))
                Logger.Instance.Attach(new AppendFileSink("client.log"));
            if (targets.Contains("file", StringComparer.OrdinalIgnoreCase))
                Logger.Instance.Attach(new FileSink("client.log"));
            if (targets.Contains("console", StringComparer.OrdinalIgnoreCase))
                Logger.Instance.Attach(new ConsoleSink());
            if (targets.Contains("debug", StringComparer.OrdinalIgnoreCase))
                Logger.Instance.Attach(new DebugSink());
        }

        Logger.Instance.MinimumLevel = LogLevel.Trace;
        console.WriteLine("Logger Minimum Level: Trace");

        string serverAddress = null;

        foreach (string arg in args)
        {
            if (arg.StartsWith("--"))
                continue;

            serverAddress = arg;
            break;
        }

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
