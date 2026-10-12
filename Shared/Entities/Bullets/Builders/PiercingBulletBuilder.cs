using System.Numerics;

public class PiercingBulletBuilder : BulletBuilder
{
    public PiercingBulletBuilder()
    {
        Damage = 10;
        DamageBox = new Vector2(0.1f, 0.1f);
        KnockBackMultiplier = 1.0f;
        Speed = 10.0f;
        TicksLeft = 300;
    }

    public override PiercingBullet Build()
    {
        return new PiercingBullet(Id, Position, FiredBy, Damage, DamageBox, KnockBackMultiplier, Direction * Speed, TicksLeft);
    }
}
