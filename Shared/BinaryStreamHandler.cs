using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

public class BinaryStreamHandler
{
    public static readonly ClassRegistry ClassRegistry = new ClassRegistry();

    private BinaryWriter writer = null!;
    private BinaryReader reader = null!;
    private Stream stream = null!;

    public void Write(Stream stream, IBinarySerializable value)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        this.stream = stream;

        WriteTagged(value);
    }

    public IBinarySerializable Read(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        this.stream = stream;

        return ReadTagged();
    }

    public void Write(byte value) => writer.Write(value);

    public void Write(sbyte value) => writer.Write(value);

    public void Write(short value) => writer.Write(value);

    public void Write(ushort value) => writer.Write(value);

    public void Write(int value) => writer.Write(value);

    public void Write(uint value) => writer.Write(value);

    public void Write(long value) => writer.Write(value);

    public void Write(ulong value) => writer.Write(value);

    public void Write(float value) => writer.Write(value);

    public void Write(double value) => writer.Write(value);

    public void Write(bool value) => writer.Write(value);

    public void Write(Guid value)
    {
        writer.Write(value.ToByteArray());
    }

    public void Write(BlockType value)
    {
        writer.Write((int)value);
    }

    public void Write(KeyState value)
    {
        writer.Write((int)value);
    }

    public void Write(Vector2 value)
    {
        Write(value.X);
        Write(value.Y);
    }

    public void Write(IBinarySerializable value)
    {
        value.Serialize(this);
    }

    public void WriteTagged(IBinarySerializable value)
    {
        Write(ClassRegistry.GetId(value.GetType()));
        value.Serialize(this);
    }

    public T Read<T>()
    {
        if (typeof(T) == typeof(byte))
            return (T)(object)reader.ReadByte();

        if (typeof(T) == typeof(sbyte))
            return (T)(object)reader.ReadSByte();

        if (typeof(T) == typeof(short))
            return (T)(object)reader.ReadInt16();

        if (typeof(T) == typeof(ushort))
            return (T)(object)reader.ReadUInt16();

        if (typeof(T) == typeof(int))
            return (T)(object)reader.ReadInt32();

        if (typeof(T) == typeof(uint))
            return (T)(object)reader.ReadUInt32();

        if (typeof(T) == typeof(long))
            return (T)(object)reader.ReadInt64();

        if (typeof(T) == typeof(ulong))
            return (T)(object)reader.ReadUInt64();

        if (typeof(T) == typeof(float))
            return (T)(object)reader.ReadSingle();

        if (typeof(T) == typeof(double))
            return (T)(object)reader.ReadDouble();

        if (typeof(T) == typeof(bool))
            return (T)(object)reader.ReadBoolean();

        if (typeof(T) == typeof(Guid))
        {
            byte[] bytes = reader.ReadBytes(16);

            if (bytes.Length != 16)
                throw new EndOfStreamException("Unable to read a complete Guid."); // TODO might cause problems

            return (T)(object)new Guid(bytes);
        }

        if (typeof(T) == typeof(BlockType))
            return (T)(object)(BlockType)reader.ReadInt32();

        if (typeof(T) == typeof(KeyState))
            return (T)(object)(KeyState)reader.ReadInt32();

        if (typeof(T) == typeof(Vector2))
            return (T)(object)new Vector2(Read<float>(), Read<float>());

        if (typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
            return (T)ClassRegistry.Deserialize(typeof(T), this);

        throw new NotSupportedException($"Type '{typeof(T)}' is not supported.");
    }

    public IBinarySerializable ReadTagged()
    {
        ulong classId = Read<ulong>();

        return ClassRegistry.Deserialize(classId, this);
    }
}
