using System;

public class RegisterUserMessage : IBinarySerializable
{
    public Guid ClientId { get; set; }

    public RegisterUserMessage() { }

    public RegisterUserMessage(Guid clientId)
    {
        ClientId = clientId;
    }

    public void Serialize(BinaryStreamHandler writer)
    {
        writer.Write(ClientId);
    }

    public static IBinarySerializable Deserialize(BinaryStreamHandler reader)
    {
        return new RegisterUserMessage(reader.Read<Guid>());
    }
}
