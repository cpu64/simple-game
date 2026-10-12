using System;
using System.Numerics;

public static class BulletFactory
{
    public static Bullet Create(BulletType type, EntityId id, Vector2 position, EntityId firedBy, Vector2 direction)
    {
        BulletBuilder builder = type switch
        {
            BulletType.Simple => new SimpleBulletBuilder(),
            BulletType.Piercing => new PiercingBulletBuilder(),
            BulletType.Heavy => new HeavyBulletBuilder(),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        return builder.WithId(id).WithPosition(position).WithFiredBy(firedBy).WithDirection(direction).Build();
    }
}
