using System.Numerics;

public abstract class Mob : Entity, IFacing, IHealth, IMoving
{
    public float Rotation { get; set; }

    public int MaxHealth { get; set; }
    public int Health { get; set; }
    public Vector2 HitBox { get; set; }
    public long InvincibleUntil { get; set; }

    public Vector2 Velocity { get; set; }

    protected Mob(EntityId id, Vector2 position, float rotation, int maxHealth, int health, Vector2 hitBox, long invincibleUntil, Vector2 velocity)
        : base(id, position)
    {
        Rotation = rotation;

        MaxHealth = maxHealth;
        Health = health;
        HitBox = hitBox;
        InvincibleUntil = invincibleUntil;

        Velocity = velocity;
    }

    public override Mob Copy()
    {
        return (Mob)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, Rotation={Rotation:F2}, Velocity={Velocity}, Health={Health}/{MaxHealth}, InvincibleUntil={InvincibleUntil}";
    }
}
