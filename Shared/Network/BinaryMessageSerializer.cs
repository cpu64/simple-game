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


// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using System.Runtime.CompilerServices;
// using System.Text;
//
// public static class BinaryMessageSerializer
// {
//     private const int MaxCollectionSize = 10_000_000;
//     private const int MaxStringByteLength = 256 * 1024 * 1024;
//
//     // ---------------------------------------------------------------------
//     // Public API
//     // ---------------------------------------------------------------------
//
//     public static byte[] Serialize<T>(T message) where T : IMessage
//     {
//         if (message == null)
//             throw new ArgumentNullException(nameof(message));
//
//         using var stream = new MemoryStream();
//         using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
//
//         WriteValue(writer, typeof(T), message);
//
//         return stream.ToArray();
//     }
//
//     public static void Serialize<T>(Stream stream, T message) where T : IMessage
//     {
//         if (stream == null)
//             throw new ArgumentNullException(nameof(stream));
//
//         if (message == null)
//             throw new ArgumentNullException(nameof(message));
//
//         using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
//         WriteValue(writer, typeof(T), message);
//         writer.Flush();
//     }
//
//     public static IMessage Deserialize(
//         byte[] data,
//         Type type)
//     {
//         if (data == null)
//             throw new ArgumentNullException(nameof(data));
//
//         if (type == null)
//             throw new ArgumentNullException(nameof(type));
//
//         if (!typeof(IMessage).IsAssignableFrom(type))
//         {
//             throw new InvalidOperationException(
//                 "Type '" + type.FullName +
//                 "' does not implement IMessage.");
//         }
//
//         using (MemoryStream stream =
//         new MemoryStream(data, false))
//         using (BinaryReader reader =
//         new BinaryReader(
//             stream,
//             Encoding.UTF8,
//             true))
//         {
//             IMessage result =
//             (IMessage)ReadValue(reader, type);
//
//             if (stream.Position != stream.Length)
//             {
//                 throw new InvalidDataException(
//                     "Trailing data detected after deserializing " +
//                     type.FullName + ".");
//             }
//
//             return result;
//         }
//     }
//
//
//     public static T Deserialize<T>(byte[] data) where T : IMessage
//     {
//         if (data == null)
//             throw new ArgumentNullException(nameof(data));
//
//         using var stream = new MemoryStream(data, writable: false);
//         using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
//
//         T result = (T)ReadValue(reader, typeof(T))!;
//
//         if (stream.Position != stream.Length)
//         {
//             throw new InvalidDataException(
//                 $"Trailing data detected after deserializing {typeof(T).FullName}.");
//         }
//
//         return result;
//     }
//
//     public static T Deserialize<T>(Stream stream) where T : IMessage
//     {
//         if (stream == null)
//             throw new ArgumentNullException(nameof(stream));
//
//         using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
//         return (T)ReadValue(reader, typeof(T))!;
//     }
//
//     // ---------------------------------------------------------------------
//     // Serialization
//     // ---------------------------------------------------------------------
//
//     private static void WriteValue(BinaryWriter writer, Type type, object? value)
//     {
//         // Nullable<T>
//         Type? nullableType = Nullable.GetUnderlyingType(type);
//
//         if (nullableType != null)
//         {
//             bool hasValue = value != null;
//             writer.Write(hasValue);
//
//             if (hasValue)
//                 WriteValue(writer, nullableType, value);
//
//             return;
//         }
//
//         // Reference type null marker.
//         if (!type.IsValueType)
//         {
//             bool hasValue = value != null;
//             writer.Write(hasValue);
//
//             if (!hasValue)
//                 return;
//         }
//
//         if (type == typeof(Guid))
//         {
//             writer.Write(((Guid)value!).ToByteArray());
//             return;
//         }
//
//         if (type == typeof(bool))
//         {
//             writer.Write((bool)value!);
//             return;
//         }
//
//         if (type == typeof(byte))
//         {
//             writer.Write((byte)value!);
//             return;
//         }
//
//         if (type == typeof(sbyte))
//         {
//             writer.Write((sbyte)value!);
//             return;
//         }
//
//         if (type == typeof(short))
//         {
//             writer.Write((short)value!);
//             return;
//         }
//
//         if (type == typeof(ushort))
//         {
//             writer.Write((ushort)value!);
//             return;
//         }
//
//         if (type == typeof(int))
//         {
//             writer.Write((int)value!);
//             return;
//         }
//
//         if (type == typeof(uint))
//         {
//             writer.Write((uint)value!);
//             return;
//         }
//
//         if (type == typeof(long))
//         {
//             writer.Write((long)value!);
//             return;
//         }
//
//         if (type == typeof(ulong))
//         {
//             writer.Write((ulong)value!);
//             return;
//         }
//
//         if (type == typeof(float))
//         {
//             writer.Write((float)value!);
//             return;
//         }
//
//         if (type == typeof(double))
//         {
//             writer.Write((double)value!);
//             return;
//         }
//
//         if (type == typeof(char))
//         {
//             writer.Write((char)value!);
//             return;
//         }
//
//         if (type == typeof(decimal))
//         {
//             int[] bits = decimal.GetBits((decimal)value!);
//
//             for (int i = 0; i < bits.Length; i++)
//                 writer.Write(bits[i]);
//
//             return;
//         }
//
//         if (type == typeof(string))
//         {
//             WriteString(writer, (string)value!);
//             return;
//         }
//
//         if (type == typeof(byte[]))
//         {
//             WriteByteArray(writer, (byte[])value!);
//             return;
//         }
//
//         if (type.IsEnum)
//         {
//             WriteEnum(writer, type, value!);
//             return;
//         }
//
//         if (type.IsArray)
//         {
//             WriteArray(writer, type.GetElementType()!, (Array)value!);
//             return;
//         }
//
//         if (IsList(type))
//         {
//             WriteList(writer, type.GetGenericArguments()[0], (IList)value!);
//             return;
//         }
//
//         if (IsDictionary(type))
//         {
//             Type[] args = type.GetGenericArguments();
//
//             WriteDictionary(
//                 writer,
//                 args[0],
//                 args[1],
//                 (IDictionary)value!);
//
//             return;
//         }
//
//         if (type.IsPrimitive)
//         {
//             throw new NotSupportedException(
//                 $"Primitive type '{type.FullName}' is not supported.");
//         }
//
//         WriteObject(writer, type, value!);
//     }
//
//     private static void WriteObject(BinaryWriter writer, Type type, object value)
//     {
//         MemberInfo[] members = GetSerializableMembers(type);
//
//         foreach (MemberInfo member in members)
//         {
//             Type memberType;
//             object? memberValue;
//
//             if (member is PropertyInfo property)
//             {
//                 memberType = property.PropertyType;
//                 memberValue = property.GetValue(value);
//             }
//             else if (member is FieldInfo field)
//             {
//                 memberType = field.FieldType;
//                 memberValue = field.GetValue(value);
//             }
//             else
//             {
//                 throw new InvalidOperationException();
//             }
//
//             WriteValue(writer, memberType, memberValue);
//         }
//     }
//
//     private static void WriteString(BinaryWriter writer, string value)
//     {
//         byte[] bytes = Encoding.UTF8.GetBytes(value);
//
//         if (bytes.Length > MaxStringByteLength)
//             throw new InvalidDataException(
//                 $"String is too large: {bytes.Length} bytes.");
//
//             Write7BitEncodedInt(writer, bytes.Length);
//         writer.Write(bytes);
//     }
//
//     private static void WriteByteArray(BinaryWriter writer, byte[] value)
//     {
//         if (value.Length > MaxCollectionSize)
//             throw new InvalidDataException(
//                 $"Byte array is too large: {value.Length} elements.");
//
//             Write7BitEncodedInt(writer, value.Length);
//         writer.Write(value);
//     }
//
//     private static void WriteArray(
//         BinaryWriter writer,
//         Type elementType,
//         Array array)
//     {
//         if (array.Length > MaxCollectionSize)
//             throw new InvalidDataException(
//                 $"Array is too large: {array.Length} elements.");
//
//             Write7BitEncodedInt(writer, array.Length);
//
//         foreach (object? item in array)
//             WriteValue(writer, elementType, item);
//     }
//
//     private static void WriteList(
//         BinaryWriter writer,
//         Type elementType,
//         IList list)
//     {
//         if (list.Count > MaxCollectionSize)
//             throw new InvalidDataException(
//                 $"List is too large: {list.Count} elements.");
//
//             Write7BitEncodedInt(writer, list.Count);
//
//         foreach (object? item in list)
//             WriteValue(writer, elementType, item);
//     }
//
//     private static void WriteDictionary(
//         BinaryWriter writer,
//         Type keyType,
//         Type valueType,
//         IDictionary dictionary)
//     {
//         if (dictionary.Count > MaxCollectionSize)
//             throw new InvalidDataException(
//                 $"Dictionary is too large: {dictionary.Count} elements.");
//
//             Write7BitEncodedInt(writer, dictionary.Count);
//
//         foreach (DictionaryEntry entry in dictionary)
//         {
//             WriteValue(writer, keyType, entry.Key);
//             WriteValue(writer, valueType, entry.Value);
//         }
//     }
//
//     private static void WriteEnum(
//         BinaryWriter writer,
//         Type enumType,
//         object value)
//     {
//         Type underlyingType = Enum.GetUnderlyingType(enumType);
//
//         if (underlyingType == typeof(byte))
//             writer.Write((byte)value);
//         else if (underlyingType == typeof(sbyte))
//             writer.Write((sbyte)value);
//         else if (underlyingType == typeof(short))
//             writer.Write((short)value);
//         else if (underlyingType == typeof(ushort))
//             writer.Write((ushort)value);
//         else if (underlyingType == typeof(int))
//             writer.Write((int)value);
//         else if (underlyingType == typeof(uint))
//             writer.Write((uint)value);
//         else if (underlyingType == typeof(long))
//             writer.Write((long)value);
//         else if (underlyingType == typeof(ulong))
//             writer.Write((ulong)value);
//         else
//             throw new NotSupportedException(
//                 $"Enum underlying type '{underlyingType}' is not supported.");
//     }
//
//     // ---------------------------------------------------------------------
//     // Deserialization
//     // ---------------------------------------------------------------------
//
//     private static object? ReadValue(BinaryReader reader, Type type)
//     {
//         // Nullable<T>
//         Type? nullableType = Nullable.GetUnderlyingType(type);
//
//         if (nullableType != null)
//         {
//             bool hasValue = reader.ReadBoolean();
//
//             if (!hasValue)
//                 return null;
//
//             return ReadValue(reader, nullableType);
//         }
//
//         // Reference type null marker.
//         if (!type.IsValueType)
//         {
//             bool hasValue = reader.ReadBoolean();
//
//             if (!hasValue)
//                 return null;
//         }
//
//         if (type == typeof(Guid))
//         {
//             return new Guid(ReadExactly(reader, 16));
//         }
//
//         if (type == typeof(bool))
//             return reader.ReadBoolean();
//
//         if (type == typeof(byte))
//             return reader.ReadByte();
//
//         if (type == typeof(sbyte))
//             return reader.ReadSByte();
//
//         if (type == typeof(short))
//             return reader.ReadInt16();
//
//         if (type == typeof(ushort))
//             return reader.ReadUInt16();
//
//         if (type == typeof(int))
//             return reader.ReadInt32();
//
//         if (type == typeof(uint))
//             return reader.ReadUInt32();
//
//         if (type == typeof(long))
//             return reader.ReadInt64();
//
//         if (type == typeof(ulong))
//             return reader.ReadUInt64();
//
//         if (type == typeof(float))
//             return reader.ReadSingle();
//
//         if (type == typeof(double))
//             return reader.ReadDouble();
//
//         if (type == typeof(char))
//             return reader.ReadChar();
//
//         if (type == typeof(decimal))
//         {
//             int lo = reader.ReadInt32();
//             int mid = reader.ReadInt32();
//             int hi = reader.ReadInt32();
//             int flags = reader.ReadInt32();
//
//             return new decimal(new[] { lo, mid, hi, flags });
//         }
//
//         if (type == typeof(string))
//             return ReadString(reader);
//
//         if (type == typeof(byte[]))
//             return ReadByteArray(reader);
//
//         if (type.IsEnum)
//             return ReadEnum(reader, type);
//
//         if (type.IsArray)
//             return ReadArray(reader, type.GetElementType()!);
//
//         if (IsList(type))
//             return ReadList(reader, type);
//
//         if (IsDictionary(type))
//             return ReadDictionary(reader, type);
//
//         if (type.IsPrimitive)
//         {
//             throw new NotSupportedException(
//                 $"Primitive type '{type.FullName}' is not supported.");
//         }
//
//         return ReadObject(reader, type);
//     }
//
//     private static object ReadObject(BinaryReader reader, Type type)
//     {
//         object instance;
//
//         try
//         {
//             instance = Activator.CreateInstance(type)
//             ?? throw new InvalidOperationException(
//                 $"Could not create instance of '{type.FullName}'.");
//         }
//         catch (Exception ex)
//         {
//             throw new InvalidOperationException(
//                 $"Type '{type.FullName}' must have a parameterless constructor.",
//                 ex);
//         }
//
//         MemberInfo[] members = GetSerializableMembers(type);
//
//         foreach (MemberInfo member in members)
//         {
//             Type memberType;
//             Action<object, object?> setter;
//
//             if (member is PropertyInfo property)
//             {
//                 memberType = property.PropertyType;
//
//                 if (!property.CanWrite)
//                 {
//                     throw new InvalidOperationException(
//                         $"Property '{type.FullName}.{property.Name}' must have a setter.");
//                 }
//
//                 setter = property.SetValue;
//             }
//             else if (member is FieldInfo field)
//             {
//                 memberType = field.FieldType;
//
//                 if (field.IsInitOnly)
//                 {
//                     throw new InvalidOperationException(
//                         $"Field '{type.FullName}.{field.Name}' is readonly.");
//                 }
//
//                 setter = field.SetValue;
//             }
//             else
//             {
//                 throw new InvalidOperationException();
//             }
//
//             object? value = ReadValue(reader, memberType);
//             setter(instance, value);
//         }
//
//         return instance;
//     }
//
//     private static string ReadString(BinaryReader reader)
//     {
//         int length = Read7BitEncodedInt(reader);
//
//         if (length < 0 || length > MaxStringByteLength)
//         {
//             throw new InvalidDataException(
//                 $"Invalid string length: {length}.");
//         }
//
//         byte[] bytes = ReadExactly(reader, length);
//
//         return Encoding.UTF8.GetString(bytes);
//     }
//
//     private static byte[] ReadByteArray(BinaryReader reader)
//     {
//         int length = Read7BitEncodedInt(reader);
//
//         if (length < 0 || length > MaxCollectionSize)
//         {
//             throw new InvalidDataException(
//                 $"Invalid byte array length: {length}.");
//         }
//
//         return ReadExactly(reader, length);
//     }
//
//     private static Array ReadArray(BinaryReader reader, Type elementType)
//     {
//         int count = ReadCollectionLength(reader);
//
//         Array array = Array.CreateInstance(elementType, count);
//
//         for (int i = 0; i < count; i++)
//         {
//             object? value = ReadValue(reader, elementType);
//             array.SetValue(value, i);
//         }
//
//         return array;
//     }
//
//     private static object ReadList(BinaryReader reader, Type listType)
//     {
//         Type elementType = listType.GetGenericArguments()[0];
//
//         int count = ReadCollectionLength(reader);
//
//         object list = Activator.CreateInstance(listType)
//         ?? throw new InvalidOperationException(
//             $"Could not create '{listType.FullName}'.");
//
//         MethodInfo addMethod = listType.GetMethod("Add")!;
//
//         for (int i = 0; i < count; i++)
//         {
//             object? value = ReadValue(reader, elementType);
//             addMethod.Invoke(list, new[] { value });
//         }
//
//         return list;
//     }
//
//     private static object ReadDictionary(BinaryReader reader, Type dictionaryType)
//     {
//         Type[] args = dictionaryType.GetGenericArguments();
//
//         Type keyType = args[0];
//         Type valueType = args[1];
//
//         int count = ReadCollectionLength(reader);
//
//         object dictionary = Activator.CreateInstance(dictionaryType)
//         ?? throw new InvalidOperationException(
//             $"Could not create '{dictionaryType.FullName}'.");
//
//         MethodInfo addMethod = dictionaryType.GetMethod("Add")!;
//
//         for (int i = 0; i < count; i++)
//         {
//             object? key = ReadValue(reader, keyType);
//             object? value = ReadValue(reader, valueType);
//
//             addMethod.Invoke(dictionary, new[] { key, value });
//         }
//
//         return dictionary;
//     }
//
//     private static object ReadEnum(BinaryReader reader, Type enumType)
//     {
//         Type underlyingType = Enum.GetUnderlyingType(enumType);
//
//         object value;
//
//         if (underlyingType == typeof(byte))
//             value = reader.ReadByte();
//         else if (underlyingType == typeof(sbyte))
//             value = reader.ReadSByte();
//         else if (underlyingType == typeof(short))
//             value = reader.ReadInt16();
//         else if (underlyingType == typeof(ushort))
//             value = reader.ReadUInt16();
//         else if (underlyingType == typeof(int))
//             value = reader.ReadInt32();
//         else if (underlyingType == typeof(uint))
//             value = reader.ReadUInt32();
//         else if (underlyingType == typeof(long))
//             value = reader.ReadInt64();
//         else if (underlyingType == typeof(ulong))
//             value = reader.ReadUInt64();
//         else
//             throw new NotSupportedException(
//                 $"Enum underlying type '{underlyingType}' is not supported.");
//
//             return Enum.ToObject(enumType, value);
//     }
//
//     // ---------------------------------------------------------------------
//     // Deterministic member ordering
//     // ---------------------------------------------------------------------
//
//     private static MemberInfo[] GetSerializableMembers(Type type)
//     {
//         return type
//         .GetMembers(
//             BindingFlags.Instance |
//             BindingFlags.Public |
//             BindingFlags.DeclaredOnly)
//         .Where(IsSerializableMember)
//         .OrderBy(GetMetadataToken)
//         .ToArray();
//     }
//
//     private static bool IsSerializableMember(MemberInfo member)
//     {
//         if (member is PropertyInfo property)
//         {
//             return property.GetMethod != null &&
//             property.GetMethod.IsPublic &&
//             property.GetIndexParameters().Length == 0 &&
//             property.SetMethod != null &&
//             property.SetMethod.IsPublic;
//         }
//
//         if (member is FieldInfo field)
//         {
//             return field.IsPublic &&
//             !field.IsStatic;
//         }
//
//         return false;
//     }
//
//     private static int GetMetadataToken(MemberInfo member)
//     {
//         return member.MetadataToken;
//     }
//
//     // ---------------------------------------------------------------------
//     // Type helpers
//     // ---------------------------------------------------------------------
//
//     private static bool IsList(Type type)
//     {
//         return type.IsGenericType &&
//         type.GetGenericTypeDefinition() == typeof(List<>);
//     }
//
//     private static bool IsDictionary(Type type)
//     {
//         return type.IsGenericType &&
//         type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
//     }
//
//     // ---------------------------------------------------------------------
//     // Binary helpers
//     // ---------------------------------------------------------------------
//
//     private static void Write7BitEncodedInt(BinaryWriter writer, int value)
//     {
//         uint v = (uint)value;
//
//         while (v >= 0x80)
//         {
//             writer.Write((byte)(v | 0x80));
//             v >>= 7;
//         }
//
//         writer.Write((byte)v);
//     }
//
//     private static int Read7BitEncodedInt(BinaryReader reader)
//     {
//         uint result = 0;
//         int shift = 0;
//
//         while (shift < 35)
//         {
//             byte b = reader.ReadByte();
//
//             result |= (uint)(b & 0x7F) << shift;
//
//             if ((b & 0x80) == 0)
//             {
//                 if (result > int.MaxValue)
//                     throw new InvalidDataException(
//                         "7-bit encoded integer is too large.");
//
//                     return (int)result;
//             }
//
//             shift += 7;
//         }
//
//         throw new InvalidDataException(
//             "Invalid 7-bit encoded integer.");
//     }
//
//     private static int ReadCollectionLength(BinaryReader reader)
//     {
//         int count = Read7BitEncodedInt(reader);
//
//         if (count < 0 || count > MaxCollectionSize)
//         {
//             throw new InvalidDataException(
//                 $"Invalid collection length: {count}.");
//         }
//
//         return count;
//     }
//
//     private static byte[] ReadExactly(BinaryReader reader, int count)
//     {
//         byte[] result = reader.ReadBytes(count);
//
//         if (result.Length != count)
//             throw new EndOfStreamException(
//                 $"Expected {count} bytes, but only received {result.Length}.");
//
//             return result;
//     }
// }
