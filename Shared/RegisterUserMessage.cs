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
        var clientId = reader.Read<Guid>();

        return new RegisterUserMessage(clientId);
    }
}
