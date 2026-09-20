using System;
using System.Numerics;

public readonly record struct InputCommand(long Sequence, Guid PlayerId, KeyState Keys, Vector2 Pointer) : IBinarySerializable
{
    public override string ToString()
    {
        return $"InputCommand: Sequence={Sequence}, PlayerId={PlayerId}, Keys={Keys}, Pointer={Pointer}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Sequence);
        writer.Write(PlayerId);
        writer.Write(Keys);
        writer.Write(Pointer);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        var sequence = reader.Read<long>();
        var playerId = reader.Read<Guid>();
        var keys = reader.Read<KeyState>();
        var pointer = reader.Read<Vector2>();

        return new InputCommand(sequence, playerId, keys, pointer);
    }
}
