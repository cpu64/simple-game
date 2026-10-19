using System.Numerics;

public class HeavyBulletBuilder : BulletBuilder
{
    private Vector2 _collisionSize;
    private float _gravitationalAcceleration;

    public HeavyBulletBuilder()
    {
        Damage = 25;
        DamageBox = new Vector2(0.2f, 0.2f);
        KnockBackMultiplier = 1.5f;
        Speed = 8.0f;
        _collisionSize = new Vector2(0.2f, 0.2f);
        _gravitationalAcceleration = 0.5f;
        TicksLeft = 300;
    }

    public HeavyBulletBuilder WithCollisionSize(Vector2 collisionSize)
    {
        _collisionSize = collisionSize;
        return this;
    }

    public HeavyBulletBuilder WithGravitationalAcceleration(float acceleration)
    {
        _gravitationalAcceleration = acceleration;
        return this;
    }

    public override HeavyBullet Build()
    {
        return new HeavyBullet(
            Id,
            Position,
            FiredBy,
            Damage,
            DamageBox,
            KnockBackMultiplier,
            Direction * Speed,
            _collisionSize,
            _gravitationalAcceleration,
            TicksLeft
        );
    }
}
