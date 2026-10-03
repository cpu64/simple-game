using System.Numerics;

public class PassiveMobFactory : IMobFactory
{
    public Mob CreateSkyMob(EntityId id, Vector2 position)
    {
        return new Eagle(
            id,
            position,
            collisionSize: new Vector2(1.0f, 1.0f),
            rotation: 0.0f,
            maxHealth: 60,
            health: 60,
            hitBox: new Vector2(1.0f, 1.0f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );
    }

    public Mob CreateGroundMob(EntityId id, Vector2 position)
    {
        return new Cow(
            id,
            position,
            collisionSize: new Vector2(1.2f, 1.2f),
            rotation: 0.0f,
            gravitationalAcceleration: 16.0f,
            maxHealth: 100,
            health: 100,
            hitBox: new Vector2(1.2f, 1.2f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );
    }

    public Mob CreateUndergroundMob(EntityId id, Vector2 position)
    {
        return new Bat(
            id,
            position,
            collisionSize: new Vector2(0.8f, 0.8f),
            rotation: 0.0f,
            maxHealth: 50,
            health: 50,
            hitBox: new Vector2(0.8f, 0.8f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );
    }
}
