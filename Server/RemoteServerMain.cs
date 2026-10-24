using System;
using System.Linq;
using System.Threading;

public static class RemoteServerMain
{
    public static void Main(string[] args)
    {
        using var stopRequested = new ManualResetEventSlim(false);
        var console = new ConsoleUI(new ServerCommandFactory(() => stopRequested.Set()));
        Thread consoleThread = new Thread(console.Run) { IsBackground = true };
        consoleThread.Start();

        Logger.Instance.Attach(new ConsoleUISinkAdapter(console));

        string? logArg = args.FirstOrDefault(a => a.StartsWith("--log=", StringComparison.OrdinalIgnoreCase))?[6..];
        if (logArg != null)
        {
            string[] targets = logArg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (targets.Contains("append", StringComparer.OrdinalIgnoreCase))
                Logger.Instance.Attach(new AppendFileSink("server.log"));
            if (targets.Contains("file", StringComparer.OrdinalIgnoreCase))
                Logger.Instance.Attach(new FileSink("server.log"));
            if (targets.Contains("console", StringComparer.OrdinalIgnoreCase))
                Logger.Instance.Attach(new ConsoleSink());
            if (targets.Contains("debug", StringComparer.OrdinalIgnoreCase))
                Logger.Instance.Attach(new DebugSink());
        }

        Logger.Instance.MinimumLevel = LogLevel.Trace;
        console.WriteLine("Logger Minimum Level: Trace");

        int port = 1234;

        foreach (string arg in args)
        {
            if (arg.StartsWith("--"))
                continue;

            if (!int.TryParse(arg, out port) || port < 1 || port > 65535)
            {
                console.WriteLine("Invalid port: " + arg);
                return;
            }
            break;
        }

        RemoteServer server = new RemoteServer("world.json");

        try
        {
            server.Start(port);

            console.WriteLine($"Server started on port {port}.");
            console.WriteLine("Type 'stop' to shut down the server.");

            stopRequested.Wait();

            console.WriteLine("Stopping server...");
            server.Stop();

            console.WriteLine("Server stopped.");
        }
        finally
        {
            server.Stop();
        }
    }
}
