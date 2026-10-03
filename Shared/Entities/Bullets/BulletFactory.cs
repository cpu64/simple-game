using System;
using System.Numerics;

public static class BulletFactory
{
    public static Bullet Create(BulletType type, EntityId id, Vector2 position, EntityId firedBy, Vector2 direction)
    {
        return type switch
        {
            BulletType.Simple => new SimpleBullet(
                id,
                position,
                firedBy,
                damage: 10,
                damageBox: new Vector2(0.1f, 0.1f),
                knockBackMultiplier: 1.0f,
                velocity: direction * 10.0f,
                collisionSize: new Vector2(0.1f, 0.1f),
                ticksLeft: 300
            ),

            BulletType.Piercing => new PiercingBullet(
                id,
                position,
                firedBy,
                damage: 10,
                damageBox: new Vector2(0.1f, 0.1f),
                knockBackMultiplier: 1.0f,
                velocity: direction * 10.0f,
                ticksLeft: 300
            ),

            BulletType.Heavy => new HeavyBullet(
                id,
                position,
                firedBy,
                damage: 25,
                damageBox: new Vector2(0.2f, 0.2f),
                knockBackMultiplier: 1.5f,
                velocity: direction * 8.0f,
                collisionSize: new Vector2(0.2f, 0.2f),
                gravitationalAcceleration: 0.5f,
                ticksLeft: 300
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
