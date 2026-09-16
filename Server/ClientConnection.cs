using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;

public sealed class ClientConnection : IDisposable
{
    private const int MaxMessageSize = 16 * 1024 * 1024;

    private readonly TcpClient client;
    private readonly NetworkStream stream;

    private readonly object sendLock = new object();

    public bool Connected { get; private set; }

    public ClientConnection(TcpClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        this.client = client;
        stream = client.GetStream();
        Connected = true;
    }

    public void Send(IBinarySerializable message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (!Connected)
            throw new InvalidOperationException("Client is not connected.");

        lock (sendLock)
        {
            SendMessage(message);
        }
    }

    private void SendMessage(IBinarySerializable message)
    {
        // ulong messageId = messageRegistry.GetId(message.GetType());
        //
        // byte[] data = BinaryMessageSerializer.Serialize(message);
        //
        // int messageLength = sizeof(ulong) + data.Length;
        //
        // if (messageLength > MaxMessageSize)
        // {
        //     throw new InvalidDataException("Message is too large: " + messageLength + " bytes.");
        // }
        //
        // byte[] packet = new byte[sizeof(int) + messageLength];
        //
        // BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, sizeof(int)), messageLength);
        // BinaryPrimitives.WriteUInt64LittleEndian(packet.AsSpan(sizeof(int), sizeof(ulong)), messageId);
        // Buffer.BlockCopy(data, 0, packet, sizeof(int) + sizeof(ulong), data.Length);
        //
        // stream.Write(packet, 0, packet.Length);
        BinaryStreamHandler io = new BinaryStreamHandler();
        io.Write(stream, message);
    }

    public IBinarySerializable Receive()
    {
        if (!Connected)
            throw new InvalidOperationException("Client is not connected.");

        // byte[] lengthBuffer = new byte[sizeof(int)];
        //
        // ReadExactly(stream, lengthBuffer);
        //
        // int messageLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        //
        // if (messageLength < sizeof(ulong))
        // {
        //     throw new InvalidDataException("Invalid message length: " + messageLength);
        // }
        //
        // if (messageLength > MaxMessageSize)
        // {
        //     throw new InvalidDataException("Message exceeds maximum size: " + messageLength);
        // }
        //
        // byte[] messageBuffer = new byte[messageLength];
        //
        // ReadExactly(stream, messageBuffer);
        //
        // ulong messageId = BinaryPrimitives.ReadUInt64LittleEndian(messageBuffer.AsSpan(0, sizeof(ulong)));
        //
        // int dataLength = messageLength - sizeof(ulong);
        //
        // byte[] data = new byte[dataLength];
        //
        // Buffer.BlockCopy(messageBuffer, sizeof(ulong), data, 0, dataLength);
        //
        // Type messageType;
        //
        // try
        // {
        //     messageType = messageRegistry.GetType(messageId);
        // }
        // catch (Exception exception)
        // {
        //     throw new InvalidDataException("Unknown message ID: " + messageId, exception);
        // }
        //
        // if (!typeof(IMessage).IsAssignableFrom(messageType))
        // {
        //     throw new InvalidDataException("Registered type '" + messageType.FullName + "' does not implement IMessage.");
        // }

        BinaryStreamHandler io = new BinaryStreamHandler();

        return io.Read(stream);

        // return BinaryMessageSerializer.Deserialize(data, messageType);
    }

    // private static void ReadExactly(NetworkStream stream, byte[] buffer)
    // {
    //     int offset = 0;
    //
    //     while (offset < buffer.Length)
    //     {
    //         int read = stream.Read(buffer, offset, buffer.Length - offset);
    //
    //         if (read == 0)
    //         {
    //             throw new EndOfStreamException("Client disconnected.");
    //         }
    //
    //         offset += read;
    //     }
    // }

    public void Dispose()
    {
        Connected = false;

        try
        {
            stream.Close();
        }
        catch { }

        try
        {
            client.Close();
        }
        catch { }
    }
}
