using System.Numerics;

public abstract class Bullet : Entity
{
    public EntityId FiredBy { get; }

    protected Bullet(EntityId id, Vector2 position, EntityId firedBy)
        : base(id, position)
    {
        FiredBy = firedBy;
    }

    public override Bullet Copy()
    {
        return (Bullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, FiredBy={FiredBy}";
    }
}
