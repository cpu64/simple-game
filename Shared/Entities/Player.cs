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
        return $"Player {Id}: UserId={UserId}, Position={Position}, Rotation={Rotation:F2}, Velocity={Velocity}, LastCommand={LastCommand}";
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
        return new Player(
            reader.Read<EntityId>(),
            reader.Read<Vector2>(),
            reader.Read<Guid>(),
            reader.Read<long>(),
            reader.Read<float>(),
            reader.Read<Vector2>()
        );
    }
}
