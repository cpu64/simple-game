using System.Numerics;

public abstract class Bullet : Entity, ILifespan
{
    public EntityId FiredBy { get; }
    public abstract long TicksLeft { get; set; }

    protected Bullet(EntityId id, Vector2 position, EntityId firedBy)
        : base(id, position)
    {
        FiredBy = firedBy;
    }

    public abstract ImpactResult Tick(World world, float deltaTime);

    public override Bullet Copy()
    {
        return (Bullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, FiredBy={FiredBy}";
    }
}
