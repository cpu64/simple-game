using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class World
{
    public long Tick { get; set; }

    [JsonInclude]
    public Dictionary<Guid, Player> Players { get; private set; }

    public List<Block> Blocks { get; set; }

    public World()
    {
        Tick = 0;
        Players = new Dictionary<Guid, Player>();
        Blocks = new List<Block>();
    }

    public World(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty.", nameof(path));

        string json = File.ReadAllText(path);

        World? loaded = JsonSerializer.Deserialize<World>(json);

        if (loaded == null)
            throw new InvalidOperationException("Failed to load world from JSON.");

        Tick = loaded.Tick;
        Players = loaded.Players ?? new Dictionary<Guid, Player>();
        Blocks = loaded.Blocks ?? new List<Block>();
    }

    public World(World other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        Tick = other.Tick;

        Players = new Dictionary<Guid, Player>();

        foreach (KeyValuePair<Guid, Player> entry in other.Players)
        {
            Players.Add(entry.Key, new Player(entry.Value));
        }

        Blocks = new List<Block>();

        foreach (Block block in other.Blocks)
        {
            Blocks.Add(new Block(block.X, block.Y, block.Type));
        }
    }

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty.", nameof(path));

        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

        string json = JsonSerializer.Serialize(this, options);

        File.WriteAllText(path, json);
    }
}
