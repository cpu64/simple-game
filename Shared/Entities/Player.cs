using System;
using System.Numerics;

public class Player : Entity, IFacing, IGravityAffected, IMoving, ICollidable, IDamageable, IBinarySerializable
{
    public Vector2 Size => new Vector2(1.0f, 1.0f);
    public Guid UserId { get; private set; }
    public long LastCommand { get; set; }
    public float Rotation { get; set; }
    public Vector2 Velocity { get; set; }

    public int Health { get; set; }
    public int MaxHealth => 100;
    public bool IsDead => Health <= 0;

    public Player(EntityId id, Vector2 position, Guid userId, long lastCommand = -1, float rotation = 0, Vector2 velocity = new Vector2(), int health = 100)
        : base(id, position)
    {
        UserId = userId;
        Rotation = rotation;
        Velocity = velocity;
        LastCommand = lastCommand;
        Health = health;
    }

    public void TakeDamage(int amount)
    {
        Health = Math.Max(0, Health - amount);
    }

    public void OnDeath(World world) { }

    public override Player Copy()
    {
        return (Player)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, UserId={UserId}, Health={Health}/{MaxHealth}, LastCommand={LastCommand}, Rotation={Rotation:F2}, Velocity={Velocity}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(UserId);
        writer.Write(LastCommand);
        writer.Write(Rotation);
        writer.Write(Velocity);
        writer.Write(Health);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        var id = reader.Read<EntityId>();
        var position = reader.Read<Vector2>();
        var userId = reader.Read<Guid>();
        var lastCommand = reader.Read<long>();
        var rotation = reader.Read<float>();
        var velocity = reader.Read<Vector2>();
        var health = reader.Read<int>();

        return new Player(id, position, userId, lastCommand, rotation, velocity, health);
    }
}
