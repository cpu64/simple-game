using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;

public class World : IBinarySerializable
{
    public long Tick { get; set; }
    public List<Block> Blocks { get; set; }
    public List<Entity> Entities { get; }
    public EntityId NextEntityId { get; set; }

    public IEnumerable<T> Get<T>()
        where T : Entity => Entities.OfType<T>();

    public World()
    {
        Tick = 0;
        Blocks = new List<Block>();
        Entities = new List<Entity>();
        NextEntityId = new EntityId(0);
    }

    public World(long tick, List<Block> blocks, List<Entity> entities, EntityId nextEntityId)
    {
        Tick = tick;
        Blocks = blocks;
        Entities = entities;
        NextEntityId = nextEntityId;
    }

    public World(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty.", nameof(path));

        string json = File.ReadAllText(path);

        JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields = true };

        World? loaded = JsonSerializer.Deserialize<World>(json, options);

        if (loaded == null)
            throw new InvalidOperationException("Failed to load world from JSON.");

        Tick = loaded.Tick;
        Blocks = loaded.Blocks ?? new List<Block>();
        Entities = loaded.Entities ?? new List<Entity>();
        NextEntityId = loaded.NextEntityId;
    }

    public World Copy()
    {
        List<Entity> entities = new List<Entity>(Entities.Count);

        foreach (Entity entity in Entities)
        {
            entities.Add(entity.Copy());
        }

        return new World(Tick, Blocks, entities, NextEntityId);
    }

    public void PruneDead()
    {
        for (int i = Entities.Count - 1; i >= 0; i--)
        {
            if (Entities[i].CanBePruned)
            {
                Entities.RemoveAt(i);
            }
        }
    }

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty.", nameof(path));

        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };

        File.WriteAllText(path, JsonSerializer.Serialize(this, options));
    }

    public override string ToString()
    {
        string str = $"World: Tick={Tick}, NextEntityId={NextEntityId}";

        str += $"Blocks ({Blocks.Count}): ";

        foreach (Block block in Blocks)
        {
            str += $"{block}, ";
        }

        str += $"Entities ({Entities.Count}): ";

        foreach (Entity entity in Entities)
        {
            str += $"{entity}, ";
        }
        return str;
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Tick);
        writer.Write(Blocks.Count);
        foreach (Block block in Blocks)
        {
            writer.Write(block);
        }
        writer.Write(Entities.Count);
        foreach (Entity entity in Entities)
        {
            writer.WriteTagged((IBinarySerializable)entity);
        }
        writer.Write(NextEntityId);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        var tick = reader.Read<long>();
        var count = reader.Read<int>();
        var blocks = new List<Block>();
        for (int i = 0; i < count; i++)
        {
            blocks.Add(reader.Read<Block>());
        }
        count = reader.Read<int>();
        var entities = new List<Entity>();
        for (int i = 0; i < count; i++)
        {
            entities.Add((Entity)reader.ReadTagged());
        }
        var nextEntityId = reader.Read<EntityId>();

        return new World(tick, blocks, entities, nextEntityId);
    }
}
