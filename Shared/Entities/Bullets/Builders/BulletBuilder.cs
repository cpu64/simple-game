using System.Numerics;

public abstract class BulletBuilder
{
    protected EntityId Id;
    protected Vector2 Position;
    protected EntityId FiredBy;
    protected int Damage;
    protected Vector2 DamageBox;
    protected float KnockBackMultiplier;
    protected Vector2 Direction;
    protected float Speed;
    protected long TicksLeft;

    public BulletBuilder WithId(EntityId id)
    {
        Id = id;
        return this;
    }

    public BulletBuilder WithPosition(Vector2 position)
    {
        Position = position;
        return this;
    }

    public BulletBuilder WithFiredBy(EntityId firedBy)
    {
        FiredBy = firedBy;
        return this;
    }

    public BulletBuilder WithDamage(int damage)
    {
        Damage = damage;
        return this;
    }

    public BulletBuilder WithDamageBox(Vector2 damageBox)
    {
        DamageBox = damageBox;
        return this;
    }

    public BulletBuilder WithKnockBackMultiplier(float multiplier)
    {
        KnockBackMultiplier = multiplier;
        return this;
    }

    public BulletBuilder WithDirection(Vector2 direction)
    {
        Direction = direction;
        return this;
    }

    public BulletBuilder WithSpeed(float speed)
    {
        Speed = speed;
        return this;
    }

    public BulletBuilder WithTicksLeft(long ticksLeft)
    {
        TicksLeft = ticksLeft;
        return this;
    }

    public abstract Bullet Build();
}
