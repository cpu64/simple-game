public readonly record struct ImpactResult
{
    public ImpactKind Kind { get; init; }
    public EntityId? HitEntityId { get; init; }

    public static readonly ImpactResult None = new() { Kind = ImpactKind.None };

    public static ImpactResult Block() => new() { Kind = ImpactKind.Block };

    public static ImpactResult Entity(EntityId id) => new() { Kind = ImpactKind.Entity, HitEntityId = id };
}

public enum ImpactKind
{
    None,
    Block,
    Entity,
}
