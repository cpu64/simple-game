using System.Numerics;

public readonly record struct Block(Vector2 Position, BlockType Type) : IBinarySerializable
{
    public override string ToString()
    {
        return $"Block: Position={Position}, Type={Type}";
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Position);
        writer.Write(Type);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        return new Block(reader.Read<Vector2>(), reader.Read<BlockType>());
    }
}
