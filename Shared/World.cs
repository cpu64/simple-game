using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class World
{
    public long Tick { get; set; }

    [JsonInclude]
    public Dictionary<Guid, Player> Players { get; private set; }

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

            Players.Add(entry.Key, new Player(entry.Value));
        }
    }
}
