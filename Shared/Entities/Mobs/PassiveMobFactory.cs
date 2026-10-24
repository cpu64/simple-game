using System.Numerics;

public class PassiveMobFactory : IMobFactory
{
    private readonly Mob _eaglePrototype;
    private readonly Mob _cowPrototype;
    private readonly Mob _batPrototype;

    public PassiveMobFactory()
    {
        _eaglePrototype = new Eagle(
            new EntityId(0),
            Vector2.Zero,
            collisionSize: new Vector2(1.0f, 1.0f),
            rotation: 0.0f,
            maxHealth: 60,
            health: 60,
            hitBox: new Vector2(1.0f, 1.0f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );

        _cowPrototype = new Cow(
            new EntityId(0),
            Vector2.Zero,
            collisionSize: new Vector2(1.2f, 1.2f),
            rotation: 0.0f,
            gravitationalAcceleration: 16.0f,
            maxHealth: 100,
            health: 100,
            hitBox: new Vector2(1.2f, 1.2f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );

        _batPrototype = new Bat(
            new EntityId(0),
            Vector2.Zero,
            collisionSize: new Vector2(0.8f, 0.8f),
            rotation: 0.0f,
            maxHealth: 50,
            health: 50,
            hitBox: new Vector2(0.8f, 0.8f),
            invincibleUntil: 0,
            velocity: Vector2.Zero
        );
    }

    // Passive mobs use stateless wandering strategies.
    // ShallowCopy clones mob stats while safely sharing the strategy reference on the heap.
    public Mob CreateSkyMob(EntityId id, Vector2 position)
    {
        Mob mob = _eaglePrototype.ShallowCopy();
        mob.Id = id;
        mob.Position = position;
        return mob;
    }

    public Mob CreateGroundMob(EntityId id, Vector2 position)
    {
        Mob mob = _cowPrototype.ShallowCopy();
        mob.Id = id;
        mob.Position = position;
        return mob;
    }

    public Mob CreateUndergroundMob(EntityId id, Vector2 position)
    {
        Mob mob = _batPrototype.ShallowCopy();
        mob.Id = id;
        mob.Position = position;
        return mob;
    }
}
