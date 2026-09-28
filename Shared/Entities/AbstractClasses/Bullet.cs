// AbstractClasses/Bullet.cs
using System.Numerics;

public abstract class Bullet : Entity, IDamaging, IMoving
{
    public EntityId FiredBy { get; }

    public int Damage { get; set; }
    public Vector2 DamageBox { get; set; }
    public float KnockBackMultiplier { get; set; }

    public Vector2 Velocity { get; set; }

    protected Bullet(EntityId id, Vector2 position, EntityId firedBy, int damage, Vector2 damageBox, float knockBackMultiplier, Vector2 velocity)
        : base(id, position)
    {
        FiredBy = firedBy;
        Damage = damage;
        DamageBox = damageBox;
        KnockBackMultiplier = knockBackMultiplier;
        Velocity = velocity;
    }

    public override Bullet Copy()
    {
        return (Bullet)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, FiredBy={FiredBy}, Damage={Damage}, DamageBox={DamageBox}, KnockBackMultiplier={KnockBackMultiplier}, Velocity={Velocity}";
    }
}
