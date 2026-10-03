using System.Numerics;

public class HostileMobFactory : IMobFactory
{
    public Mob CreateSkyMob(EntityId id, Vector2 position)
    {
        return new Harpy(
            id,
            position,
            collisionSize: new Vector2(1.0f, 1.0f),
            damage: 10,
            damageBox: new Vector2(1.0f, 1.0f),
            knockBackMultiplier: 1.0f,
            rotation: 0.0f,
            maxHealth: 80,
            health: 80,
            hitBox: new Vector2(1.0f, 1.0f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );
    }

    public Mob CreateGroundMob(EntityId id, Vector2 position)
    {
        return new Slime(
            id,
            position,
            jumpTimer: 0,
            collisionSize: new Vector2(1.0f, 1.0f),
            damage: 10,
            damageBox: new Vector2(1.0f, 1.0f),
            knockBackMultiplier: 1.0f,
            rotation: 0.0f,
            gravitationalAcceleration: 16.0f,
            maxHealth: 100,
            health: 100,
            hitBox: new Vector2(1.0f, 1.0f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );
    }

    public Mob CreateUndergroundMob(EntityId id, Vector2 position)
    {
        return new Spider(
            id,
            position,
            collisionSize: new Vector2(1.0f, 1.0f),
            damage: 10,
            damageBox: new Vector2(1.0f, 1.0f),
            knockBackMultiplier: 1.0f,
            rotation: 0.0f,
            gravitationalAcceleration: 16.0f,
            maxHealth: 80,
            health: 80,
            hitBox: new Vector2(1.0f, 1.0f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );
    }
}
