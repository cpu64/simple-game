using System;
using System.Numerics;

public abstract class Enemy : Entity, IDamageable, ICollidable, IMoving, IGravityAffected
{
    public abstract int Health { get; protected set; }
    public abstract int MaxHealth { get; }
    public Vector2 Velocity { get; set; }
    public abstract Vector2 Size { get; }
    public bool IsDead => Health <= 0;

    protected Enemy(EntityId id, Vector2 position, Vector2 velocity = default)
        : base(id, position)
    {
        Velocity = velocity;
    }

    public abstract void Tick(World world, float deltaTime);

    public virtual void TakeDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
    }

    public virtual void OnDeath(World world) { }
}
