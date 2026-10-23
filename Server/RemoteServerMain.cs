using System;
using System.Threading;

public static class RemoteServerMain
{
    public static void Main(string[] args)
    {
        using var stopRequested = new ManualResetEventSlim(false);
        var console = new ConsoleUI(new ServerCommandFactory(() => stopRequested.Set()));
        Thread consoleThread = new Thread(console.Run) { IsBackground = true };
        consoleThread.Start();

        Logger.Instance.Attach(new ConsoleUISink(console));
        Logger.Instance.MinimumLevel = LogLevel.Trace;
        console.WriteLine("Logger Minimum Level: Trace");

        int port = 1234;

        if (args.Length > 0 && (!int.TryParse(args[0], out port) || port < 1 || port > 65535))
        {
            console.WriteLine("Invalid port: " + args[0]);
            return;
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
