using System;
using System.Collections.Generic;

public class World
{
    // World @ N means that all commands through tick N
    // have already been applied to this world.
    public long Tick { get; set; }

    public Dictionary<Guid, Player> Players { get; set; }

    public World()
    {
        Tick = 0;
        Players = new Dictionary<Guid, Player>();
    }

    public World(World other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        Tick = other.Tick;
        Players = new Dictionary<Guid, Player>();

        foreach (KeyValuePair<Guid, Player> entry in other.Players)
        {
            Player player = entry.Value;

            Players.Add(
                entry.Key,
                new Player(
                    player.Id,
                    player.X,
                    player.Y));
        }
    }

    public void Print()
    {
        Console.WriteLine("World:");
        Console.WriteLine("  Tick: " + Tick);
        Console.WriteLine("  Players: " + Players.Count);

        foreach (KeyValuePair<Guid, Player> entry in Players)
        {
            Player player = entry.Value;

            Console.WriteLine(
                "  Player " +
                player.Id +
                " @ (" +
                player.X +
                ", " +
                player.Y +
                ")");
        }
    }

}
