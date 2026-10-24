using System.Numerics;

public class HostileMobFactory : IMobFactory
{
    private readonly Mob _harpyPrototype;
    private readonly Mob _slimePrototype;
    private readonly Mob _spiderPrototype;

    public HostileMobFactory()
    {
        _harpyPrototype = new Harpy(
            new EntityId(0),
            Vector2.Zero,
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

        _slimePrototype = new Slime(
            new EntityId(0),
            Vector2.Zero,
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

        _spiderPrototype = new Spider(
            new EntityId(0),
            Vector2.Zero,
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

    // Hostile mobs engage in dynamic combat AI that mutates strategies at runtime.
    // DeepCopy ensures each hostile mob owns an independent strategy instance on the heap.
    public Mob CreateSkyMob(EntityId id, Vector2 position)
    {
        Mob mob = _harpyPrototype.DeepCopy();
        mob.Id = id;
        mob.Position = position;
        return mob;
    }

    public Mob CreateGroundMob(EntityId id, Vector2 position)
    {
        Mob mob = _slimePrototype.DeepCopy();
        mob.Id = id;
        mob.Position = position;
        return mob;
    }

    public Mob CreateUndergroundMob(EntityId id, Vector2 position)
    {
        Mob mob = _spiderPrototype.DeepCopy();
        mob.Id = id;
        mob.Position = position;
        return mob;
    }
}
