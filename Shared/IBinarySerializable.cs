public interface IBinarySerializable
{
    void Serialize(BinaryStreamHandler writer);
    static abstract IBinarySerializable Deserialize(BinaryStreamHandler reader);
}
