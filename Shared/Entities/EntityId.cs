public readonly record struct EntityId(uint Value) : IBinarySerializable
{
    public static EntityId operator ++(EntityId id) => new EntityId(id.Value + 1);

    public static bool operator <(EntityId left, EntityId right) => left.Value < right.Value;

    public static bool operator >(EntityId left, EntityId right) => left.Value > right.Value;

    public static bool operator <=(EntityId left, EntityId right) => left.Value <= right.Value;

    public static bool operator >=(EntityId left, EntityId right) => left.Value >= right.Value;

    public override string ToString() => Value.ToString();

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(Value);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        uint value = reader.Read<uint>();

        return new EntityId(value);
    }
}
