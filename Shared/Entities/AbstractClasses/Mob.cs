using System.Numerics;

public abstract class Mob : Entity, IFacing, IHealth, IMoving
{
    public float Rotation { get; set; }

    public int MaxHealth { get; set; }
    public int Health { get; set; }
    public Vector2 HitBox { get; set; }
    public long InvincibleUntil { get; set; }

    public Vector2 Velocity { get; set; }

    public IMobMovementStrategy? MovementStrategy { get; set; }

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

    public virtual Mob ShallowCopy()
    {
        return (Mob)MemberwiseClone();
    }

    public virtual Mob DeepCopy()
    {
        Mob clone = (Mob)MemberwiseClone();
        if (MovementStrategy != null)
        {
            clone.MovementStrategy = (IMobMovementStrategy)Activator.CreateInstance(MovementStrategy.GetType())!;
        }
        return clone;
    }

    public override Mob Copy() => DeepCopy();

    public static void PrintPrototypeAddressReport()
    {
        Mob prototype = new Slime(new EntityId(0), Vector2.Zero);
        Mob shallow = prototype.ShallowCopy();
        Mob deep = prototype.DeepCopy();

        static string Addr(object? obj) => obj == null ? "null" : $"0x{System.Runtime.CompilerServices.Unsafe.As<object, nint>(ref obj):X16}";

        Console.WriteLine("Mob Copy Type  | Mob Instance       | MovementStrategy Instance");
        Console.WriteLine(new string('-', 68));
        Console.WriteLine($"{"Prototype",-14} | {Addr(prototype)} | {Addr(prototype.MovementStrategy)}");
        Console.WriteLine($"{"ShallowCopy",-14} | {Addr(shallow)} | {Addr(shallow.MovementStrategy)} (Shared pointer)");
        Console.WriteLine($"{"DeepCopy",-14} | {Addr(deep)} | {Addr(deep.MovementStrategy)} (New allocation)");
        Console.WriteLine(new string('-', 68));
    }

    public override string ToString()
    {
        return $"{base.ToString()}, Rotation={Rotation:F2}, Velocity={Velocity}, Health={Health}/{MaxHealth}, InvincibleUntil={InvincibleUntil}";
    }
}
