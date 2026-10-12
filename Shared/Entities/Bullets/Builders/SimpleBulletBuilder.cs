using System.Numerics;

public class SimpleBulletBuilder : BulletBuilder
{
    private Vector2 _collisionSize;

    public SimpleBulletBuilder()
    {
        Damage = 10;
        DamageBox = new Vector2(0.1f, 0.1f);
        KnockBackMultiplier = 1.0f;
        Speed = 10.0f;
        _collisionSize = new Vector2(0.1f, 0.1f);
        TicksLeft = 300;
    }

    public SimpleBulletBuilder WithCollisionSize(Vector2 collisionSize)
    {
        _collisionSize = collisionSize;
        return this;
    }

    public override SimpleBullet Build()
    {
        return new SimpleBullet(Id, Position, FiredBy, Damage, DamageBox, KnockBackMultiplier, Direction * Speed, _collisionSize, TicksLeft);
    }
}
