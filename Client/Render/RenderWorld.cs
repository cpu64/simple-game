using System;
using System.Collections;
using System.Collections.Generic;

public class RenderWorld
{
    public List<Block> Blocks { get; set; } = new();

    // Identity lookup makes matching snapshots easy.
    public Dictionary<EntityId, RenderEntity> Entities { get; } = new();

    public float Alpha { get; set; } = 0;
    public long Tick { get; set; } = 0;
}
