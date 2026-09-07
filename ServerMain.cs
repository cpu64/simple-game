using System;
using System.Diagnostics;
using System.Threading;

public static class ServerMain
{
    private const double TickDuration = 1.0 / 120.0;

    public static void Main()
    {
        Server server = new Server();

        Stopwatch stopwatch = Stopwatch.StartNew();

        double nextTick = stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine("Server started.");

        while (true)
        {
            double now = stopwatch.Elapsed.TotalSeconds;

            if (now >= nextTick)
            {
                // No input yet.
                server.Tick(new InputState());

                nextTick += TickDuration;

                // If we fell very far behind, don't try
                // to execute thousands of ticks.
                if (now - nextTick > 0.25)
                    nextTick = now;
            }
            else
            {
                Thread.Sleep(1);
            }
        }
    }
}
