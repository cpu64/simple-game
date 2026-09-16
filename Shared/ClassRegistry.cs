using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public class ClassRegistry
{
    private readonly Dictionary<ulong, Type> types = new();
    private readonly Dictionary<ulong, Func<BinaryStreamHandler, object>> deserializers = new();

    public ClassRegistry()
    {
        RegisterTypes();
    }

    private void RegisterTypes()
    {
        foreach (Type type in typeof(ClassRegistry).Assembly.GetTypes())
        {
            if (!typeof(IBinarySerializable).IsAssignableFrom(type))
                continue;

            // Don't register the interface itself.
            if (type == typeof(IBinarySerializable))
                continue;

            // Don't register abstract classes.
            if (type.IsAbstract)
                continue;

            ulong id = GetId(type);

            if (types.TryGetValue(id, out Type? existingType))
            {
                throw new InvalidOperationException(
                    $"Class ID collision detected: " + $"'{existingType.FullName}' and " + $"'{type.FullName}' both have ID {id}."
                );
            }

            MethodInfo? deserializeMethod = type.GetMethod(
                "Deserialize",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(BinaryStreamHandler) },
                modifiers: null
            );

            if (deserializeMethod == null)
            {
                throw new InvalidOperationException($"Type '{type.FullName}' must implement a public static " + $"Deserialize(BinaryStreamHandler) method.");
            }

            Func<BinaryStreamHandler, object> deserializer =
                (Func<BinaryStreamHandler, object>)deserializeMethod.CreateDelegate(typeof(Func<BinaryStreamHandler, object>));

            types.Add(id, type);
            deserializers.Add(id, deserializer);
        }
    }

    public Type GetType(ulong id)
    {
        if (!types.TryGetValue(id, out Type? type))
        {
            throw new InvalidOperationException($"Unknown class ID: {id}.");
        }

        return type;
    }

    public ulong GetId<T>()
        where T : IBinarySerializable
    {
        return GetId(typeof(T));
    }

    public ulong GetId(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (!typeof(IBinarySerializable).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Type '{type.FullName}' " + $"does not implement IBinarySerializable.");
        }

        if (type.FullName == null)
        {
            throw new InvalidOperationException($"Type '{type}' does not have a full name.");
        }

        return Hash(type.FullName);
    }

    public IBinarySerializable Deserialize(ulong id, BinaryStreamHandler reader)
    {
        if (!deserializers.TryGetValue(id, out var deserializer))
        {
            throw new InvalidOperationException($"Unknown class ID: {id}.");
        }

        return (IBinarySerializable)deserializer(reader);
    }

    public IBinarySerializable Deserialize(Type type, BinaryStreamHandler reader)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        ulong id = GetId(type);

        if (!deserializers.TryGetValue(id, out var deserializer))
        {
            throw new InvalidOperationException($"No deserializer registered for type '{type.FullName}'.");
        }

        return (IBinarySerializable)deserializer(reader);
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
