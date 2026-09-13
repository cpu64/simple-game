using System;
using System.IO;
using System.Text.Json;

public static class BinaryMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };

    public static byte[] Serialize<T>(T message)
        where T : IMessage
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return JsonSerializer.SerializeToUtf8Bytes(message, message.GetType(), Options);
    }

    public static void Serialize<T>(Stream stream, T message)
        where T : IMessage
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        JsonSerializer.Serialize(stream, message, Options);
    }

    public static IMessage Deserialize(byte[] data, Type type)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!typeof(IMessage).IsAssignableFrom(type))
        {
            throw new InvalidOperationException("Type '" + type.FullName + "' does not implement IMessage.");
        }

        object? result = JsonSerializer.Deserialize(data, type, Options);

        if (result is not IMessage message)
        {
            throw new InvalidDataException("Could not deserialize message type '" + type.FullName + "'.");
        }

        return message;
    }

    public static T Deserialize<T>(byte[] data)
        where T : IMessage
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        T? result = JsonSerializer.Deserialize<T>(data, Options);

        if (result == null)
        {
            throw new InvalidDataException("Could not deserialize message type '" + typeof(T).FullName + "'.");
        }

        return result;
    }

    public static T Deserialize<T>(Stream stream)
        where T : IMessage
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        T? result = JsonSerializer.Deserialize<T>(stream, Options);

        if (result == null)
        {
            throw new InvalidDataException("Could not deserialize message type '" + typeof(T).FullName + "'.");
        }

        return result;
    }
}
