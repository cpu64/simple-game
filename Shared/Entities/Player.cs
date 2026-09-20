using System;
using System.Numerics;

public class Player : Entity, IFacing, IGravityAffected, IMoving, IBinarySerializable
{
    public Guid UserId { get; private set; }
    public long LastCommand { get; set; }
    public float Rotation { get; set; }
    public Vector2 Velocity { get; set; }

    public Player(EntityId id, Vector2 position, Guid userId, long lastCommand = -1, float rotation = 0, Vector2 velocity = new Vector2())
        : base(id, position)
    {
        UserId = userId;
        Rotation = rotation;
        Velocity = velocity;
        LastCommand = lastCommand;
    }

    public override Player Copy()
    {
        return (Player)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{base.ToString()}, UserId={UserId}, LastCommand={LastCommand}, Rotation={Rotation:F2}, Velocity={Velocity}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Id);
        writer.Write(Position);
        writer.Write(UserId);
        writer.Write(LastCommand);
        writer.Write(Rotation);
        writer.Write(Velocity);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        var id = reader.Read<EntityId>();
        var position = reader.Read<Vector2>();
        var userId = reader.Read<Guid>();
        var lastCommand = reader.Read<long>();
        var rotation = reader.Read<float>();
        var velocity = reader.Read<Vector2>();

        return new Player(id, position, userId, lastCommand, rotation, velocity);
    }
}
