using System;
using System.Numerics;

public abstract class Enemy : Entity, IDamageable, ICollidable, IMoving, IKinematicBody
{
    public abstract int Health { get; protected set; }
    public abstract int MaxHealth { get; }
    public Vector2 Velocity { get; set; }
    public abstract Vector2 Size { get; }
    public MovementResult LastMovement { get; set; }
    public bool IsDead => Health <= 0;
    public override bool CanBePruned => IsDead;

    public virtual int ContactDamage => 10;
    public virtual float ContactKnockback => 6.0f;

    public virtual float GravityScale => 1.0f;
    public virtual bool CollidesWithBlocks => true;

    protected Enemy(EntityId id, Vector2 position, Vector2 velocity = default)
        : base(id, position)
    {
        Velocity = velocity;
    }

    public abstract void UpdateAI(in AISenses senses, float dt);

    public virtual bool TakeDamage(int amount)
    {
        if (IsDead)
            return false;

        Health = Math.Max(0, Health - amount);
        return true;
    }

    public virtual void OnDeath(World world) { }
}
