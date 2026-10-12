using System;
using System.Collections.Generic;
using System.Numerics;

public sealed class RenderInput
{
    public World Old { get; }
    public World New { get; }

    public IReadOnlyDictionary<Guid, Vector2> PreviousRemotePlayerPositions { get; }
    public IReadOnlyDictionary<Guid, Vector2> CurrentRemotePlayerPositions { get; }

    public RenderInput(
        World oldWorld,
        World newWorld,
        IReadOnlyDictionary<Guid, Vector2> previousRemotePlayerPositions,
        IReadOnlyDictionary<Guid, Vector2> currentRemotePlayerPositions
    )
    {
        Old = oldWorld;
        New = newWorld;

        PreviousRemotePlayerPositions = previousRemotePlayerPositions;
        CurrentRemotePlayerPositions = currentRemotePlayerPositions;
    }
}
