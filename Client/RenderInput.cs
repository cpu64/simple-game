using System;
using System.Collections.Generic;
using System.Linq;

public sealed class RenderInput
{
    public World World { get; }

    public IReadOnlyList<World> AuthoritativeSnapshots { get; }

    public RenderInput(World world, RingBuffer<World> authoritativeSnapshots)
    {
        if (world == null)
            throw new ArgumentNullException(nameof(world));

        if (authoritativeSnapshots == null)
            throw new ArgumentNullException(nameof(authoritativeSnapshots));

        World = world;

        AuthoritativeSnapshots = Enumerable
            .Range(0, authoritativeSnapshots.Count)
            .Select(authoritativeSnapshots.Get)
            .OrderBy(snapshot => snapshot.Tick)
            .ToList();
    }
}
