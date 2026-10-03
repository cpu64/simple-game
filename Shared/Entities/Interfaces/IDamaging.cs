using System.Numerics;

public interface IDamaging
{
    int Damage { get; set; }
    Vector2 DamageBox { get; set; }
    float KnockBackMultiplier { get; set; }
}
