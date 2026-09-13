using System;
using System.Net;

public static class Client
{
    public static void Main(string[] args)
    {
        string serverAddress = args.Length == 0 ? null : args[0];

        SharedInputState input = new SharedInputState();

        Guid playerId = Guid.NewGuid();

        Console.WriteLine("Player ID: " + playerId);

        LocalServer localServer = null;

        if (string.IsNullOrEmpty(serverAddress))
        {
            localServer = new LocalServer(input, playerId, "world.json");
        }
        else
        {
            string[] parts = serverAddress.Split(':');

            if (parts.Length != 2)
            {
                Console.WriteLine("Invalid server address. " + "Expected ip:port.");
                return;
            }

            IPAddress address;

            if (!IPAddress.TryParse(parts[0], out address))
            {
                Console.WriteLine("Invalid IP address: " + parts[0]);
                return;
            }

            int port;

            if (!int.TryParse(parts[1], out port) || port < 1 || port > 65535)
            {
                Console.WriteLine("Invalid port: " + parts[1]);
                return;
            }

            localServer = new LocalServer(input, playerId, new IPEndPoint(address, port));
        }

        localServer.Start();

        GameWindow.Run(localServer, input, playerId);
    }
}
