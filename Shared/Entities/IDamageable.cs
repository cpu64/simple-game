public interface IDamageable
{
    int Health { get; }
    int MaxHealth { get; }
    bool IsDead => Health <= 0;
    void TakeDamage(int amount);
    void OnDeath(World world) { }
}
