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
        return new InputCommand(reader.Read<long>(), reader.Read<Guid>(), reader.Read<KeyState>(), reader.Read<Vector2>());
    }
}

// public class InputCommand
// {
//     public long Sequence { get; private set; }
//
//     public Guid PlayerId { get; private set; }
//
//     public KeyState Keys { get; private set; }
//
//     public Vector2? Pointer { get; private set; }
//
//     public InputCommand(long sequence, Guid playerId, KeyState keys, Vector2? pointer = null)
//     {
//         Sequence = sequence;
//         PlayerId = playerId;
//         Keys = keys;
//         Pointer = pointer;
//     }
// }
