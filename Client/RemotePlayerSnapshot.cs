using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public sealed class RemotePlayerSnapshot
{
    public long Tick { get; }
    public IReadOnlyDictionary<Guid, Vector2> Positions { get; }

    public RemotePlayerSnapshot(World world, Guid localPlayerId)
    {
        Tick = world.Tick;
        Positions = world
            .Entities.OfType<Player>()
            .Where(player => player.UserId != localPlayerId)
            .ToDictionary(player => player.UserId, player => player.Position);
    }
}
