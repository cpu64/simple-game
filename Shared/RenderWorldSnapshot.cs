using System;
using System.Collections.Generic;

public class RenderWorldSnapshot
{
    // This is the world tick represented by this snapshot.
    //
    // Like World.Tick:
    // Snapshot @ N means that all commands through tick N
    // have already been applied.
    public long Tick { get; private set; }

    public Dictionary<Guid, Player> Players { get; private set; }

    public RenderWorldSnapshot(World world)
    {
        Tick = world.Tick;
        Players = new Dictionary<Guid, Player>();

        foreach (KeyValuePair<Guid, Player> entry in world.Players)
        {
            Player player = entry.Value;

            Players.Add(
                entry.Key,
                new Player(player.Id, player.X, player.Y)
            );
        }
    }
}
