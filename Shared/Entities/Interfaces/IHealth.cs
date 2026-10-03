using System.Numerics;

public interface IHealth
{
    int MaxHealth { get; set; }
    int Health { get; set; }
    Vector2 HitBox { get; set; }
    long InvincibleUntil { get; set; }

    public bool IsDead => Health <= 0;
}
