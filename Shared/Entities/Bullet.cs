using System.Numerics;

public abstract class Bullet : Entity, ILifespan, ICollidable, IMoving, IKinematicBody
{
    public EntityId FiredBy { get; }
    public bool FiredByPlayer { get; }
    public abstract long TicksLeft { get; set; }
    public abstract int Damage { get; }
    public abstract Vector2 Velocity { get; set; }
    public virtual Vector2 Size => new Vector2(0.25f, 0.25f);

    public virtual float GravityScale => 0.0f;
    public virtual bool CollidesWithBlocks => true;
    public virtual float KnockbackForce => 3.0f;
    public bool IsDead => TicksLeft <= 0;
    public override bool CanBePruned => IsDead;

    protected Bullet(EntityId id, Vector2 position, EntityId firedBy, bool firedByPlayer = true)
        : base(id, position)
    {
        FiredBy = firedBy;
        FiredByPlayer = firedByPlayer;
    }

    public override Bullet Copy()
    {
        return (Bullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, FiredBy={FiredBy}, Damage={Damage}";
    }
}
