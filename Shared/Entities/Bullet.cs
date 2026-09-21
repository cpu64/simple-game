using System.Numerics;

public abstract class Bullet : Entity, ILifespan, ICollidable, IMoving
{
    public EntityId FiredBy { get; }
    public abstract long TicksLeft { get; set; }
    public abstract int Damage { get; }
    public abstract Vector2 Velocity { get; set; }
    public virtual Vector2 Size => new Vector2(0.25f, 0.25f);

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
        return $"{base.ToString()}, FiredBy={FiredBy}, Damage={Damage}";
    }
}
