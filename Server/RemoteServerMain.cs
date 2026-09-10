using System;

public static class RemoteServerMain
{
    public static void Main(string[] args)
    {
        int port = 1234;

        if (args.Length > 0 && (!int.TryParse(args[0], out port) || port < 1 || port > 65535)) {
            Console.WriteLine("Invalid port: " + args[0]);
            return;
        }

        RemoteServer server = new RemoteServer();

        server.Start(port);
        Console.WriteLine("Press Enter to stop.");
        Console.ReadLine();
        server.Stop();
    }
}
