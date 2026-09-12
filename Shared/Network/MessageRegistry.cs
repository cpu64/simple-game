using System;
using System.Collections.Generic;
using System.Text;

public class MessageRegistry
{
    private readonly Dictionary<ulong, Type> messages = new();

    public MessageRegistry()
    {
        RegisterMessages();
    }

    private void RegisterMessages()
    {
        foreach (Type type in typeof(MessageRegistry).Assembly.GetTypes())
        {
            if (!typeof(IMessage).IsAssignableFrom(type))
                continue;

            if (type == typeof(IMessage))
                continue;

            if (!type.IsClass || type.IsAbstract || type.IsInterface)
            {
                throw new InvalidOperationException($"All types implementing IMessage " + $"must be concrete classes. " + $"Invalid type: '{type.FullName}'.");
            }

            ulong id = GetId(type);

            if (messages.TryGetValue(id, out Type? existingType))
            {
                throw new InvalidOperationException(
                    $"Message ID collision detected: " + $"'{existingType.FullName}' and " + $"'{type.FullName}' both have ID {id}."
                );
            }

            messages.Add(id, type);
        }
    }

    public Type GetType(ulong id)
    {
        if (!messages.TryGetValue(id, out Type? type))
        {
            throw new InvalidOperationException($"Unknown message ID: {id}.");
        }

        return type;
    }

    public ulong GetId<T>()
        where T : IMessage
    {
        return GetId(typeof(T));
    }

    public ulong GetId(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (!typeof(IMessage).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Network message type '{type.FullName}' " + $"does not implement IMessage.");
        }

        if (type.FullName == null)
        {
            throw new InvalidOperationException($"Network message type '{type}' " + $"does not have a full name.");
        }

        return Hash(type.FullName);
    }

    private static ulong Hash(string value)
    {
        const ulong offsetBasis = 14695981039346656037UL;

        const ulong prime = 1099511628211UL;

        ulong hash = offsetBasis;

        byte[] bytes = Encoding.UTF8.GetBytes(value);

        foreach (byte b in bytes)
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }
}
