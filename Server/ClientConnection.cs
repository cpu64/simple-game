using System;
using System.IO;
using System.Net.Sockets;

public sealed class ClientConnection : IDisposable
{
    private const int MaxMessageSize = 16 * 1024 * 1024;

    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly MessageRegistry messageRegistry;

    private readonly object sendLock =
    new object();

    private bool connected;

    public Guid PlayerId { get; private set; }

    public bool IsConnected
    {
        get { return connected; }
    }

    public ClientConnection(TcpClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        this.client = client;
        stream = client.GetStream();

        messageRegistry = new MessageRegistry();

        connected = true;
        PlayerId = Guid.Empty;
    }

    public void Send(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (!connected)
            throw new InvalidOperationException(
                "Client is not connected.");

            lock (sendLock)
            {
                SendMessage(message);
            }
    }

    private void SendMessage(IMessage message)
    {
        ulong messageId =
        messageRegistry.GetId(message.GetType());

        byte[] data =
        BinaryMessageSerializer.Serialize(message);

        int messageLength =
        sizeof(ulong) + data.Length;

        if (messageLength > MaxMessageSize)
        {
            throw new InvalidDataException(
                "Message is too large: " +
                messageLength + " bytes.");
        }

        byte[] packet =
        new byte[sizeof(int) + messageLength];

        using (MemoryStream packetStream =
        new MemoryStream(packet))
        using (BinaryWriter writer =
        new BinaryWriter(packetStream))
        {
            writer.Write(messageLength);
            writer.Write(messageId);
            writer.Write(data);
        }

        stream.Write(
            packet,
            0,
            packet.Length);
    }

    public IMessage Receive()
    {
        if (!connected)
            throw new InvalidOperationException(
                "Client is not connected.");

            byte[] lengthBuffer =
            new byte[sizeof(int)];

        ReadExactly(
            stream,
            lengthBuffer);

        int messageLength =
        BitConverter.ToInt32(
            lengthBuffer,
            0);

        if (messageLength < sizeof(ulong))
        {
            throw new InvalidDataException(
                "Invalid message length: " +
                messageLength);
        }

        if (messageLength > MaxMessageSize)
        {
            throw new InvalidDataException(
                "Message exceeds maximum size: " +
                messageLength);
        }

        byte[] messageBuffer =
        new byte[messageLength];

        ReadExactly(
            stream,
            messageBuffer);

        using (MemoryStream messageStream =
        new MemoryStream(messageBuffer))
        using (BinaryReader reader =
        new BinaryReader(messageStream))
        {
            ulong messageId =
            reader.ReadUInt64();

            int dataLength =
            messageLength - sizeof(ulong);

            byte[] data =
            reader.ReadBytes(dataLength);

            if (data.Length != dataLength)
            {
                throw new EndOfStreamException(
                    "Incomplete message payload.");
            }

            Type messageType;

            try
            {
                messageType =
                messageRegistry.GetType(messageId);
            }
            catch (Exception exception)
            {
                throw new InvalidDataException(
                    "Unknown message ID: " +
                    messageId,
                    exception);
            }

            if (!typeof(IMessage).IsAssignableFrom(
                messageType))
            {
                throw new InvalidDataException(
                    "Registered type '" +
                    messageType.FullName +
                    "' does not implement IMessage.");
            }

            return BinaryMessageSerializer.Deserialize(
                data,
                messageType);
        }
    }

    public void RegisterPlayer(Guid playerId)
    {
        PlayerId = playerId;
    }

    private static void ReadExactly(
        NetworkStream stream,
        byte[] buffer)
    {
        int offset = 0;

        while (offset < buffer.Length)
        {
            int read =
            stream.Read(
                buffer,
                offset,
                buffer.Length - offset);

            if (read == 0)
            {
                throw new EndOfStreamException(
                    "Client disconnected.");
            }

            offset += read;
        }
    }

    public void Dispose()
    {
        connected = false;

        try
        {
            stream.Close();
        }
        catch
        {
        }

        try
        {
            client.Close();
        }
        catch
        {
        }
    }
}
