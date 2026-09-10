using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public sealed class RemoteServerConnection : IDisposable
{
    private const int MaxMessageSize = 16 * 1024 * 1024;

    private readonly IPEndPoint endpoint;
    private readonly MessageRegistry messageRegistry;

    private readonly ConcurrentQueue<IMessage> sendQueue =
    new ConcurrentQueue<IMessage>();

    private readonly ConcurrentQueue<IMessage> receiveQueue =
    new ConcurrentQueue<IMessage>();

    private readonly SemaphoreSlim sendSignal =
    new SemaphoreSlim(0);

    private readonly object stateLock =
    new object();

    private TcpClient client;
    private NetworkStream stream;

    private CancellationTokenSource cancellation;
    private Thread sendThread;
    private Thread receiveThread;

    private volatile bool connected;

    public bool IsConnected
    {
        get { return connected; }
    }

    public RemoteServerConnection(IPEndPoint endpoint)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        this.endpoint = endpoint;
        messageRegistry = new MessageRegistry();
    }

    public void Connect()
    {
        lock (stateLock)
        {
            if (connected)
                throw new InvalidOperationException(
                    "Already connected.");

                client = new TcpClient();

            try
            {
                client.Connect(endpoint);
                stream = client.GetStream();

                cancellation =
                new CancellationTokenSource();

                connected = true;

                sendThread = new Thread(SendLoop);
                sendThread.IsBackground = true;
                sendThread.Name = "Network Send";

                receiveThread = new Thread(ReceiveLoop);
                receiveThread.IsBackground = true;
                receiveThread.Name = "Network Receive";

                sendThread.Start();
                receiveThread.Start();
            }
            catch
            {
                stream?.Dispose();
                client?.Dispose();

                stream = null;
                client = null;

                cancellation?.Dispose();
                cancellation = null;

                connected = false;

                throw;
            }
        }
    }

    public void Disconnect()
    {
        CancellationTokenSource cts;

        lock (stateLock)
        {
            if (!connected && client == null)
                return;

            connected = false;

            cts = cancellation;
            cancellation = null;
        }

        try
        {
            cts?.Cancel();
        }
        catch
        {
        }

        try
        {
            sendSignal.Release();
        }
        catch
        {
        }

        try
        {
            stream?.Close();
        }
        catch
        {
        }

        try
        {
            client?.Close();
        }
        catch
        {
        }

        stream = null;
        client = null;

        cts?.Dispose();
    }

    public void Dispose()
    {
        Disconnect();
        sendSignal.Dispose();
    }

    public void Send(IMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (!connected)
            throw new InvalidOperationException(
                "Not connected.");

            sendQueue.Enqueue(message);
        sendSignal.Release();
    }

    private void SendLoop()
    {
        CancellationToken token =
        cancellation?.Token ?? default(CancellationToken);

        try
        {
            while (!token.IsCancellationRequested)
            {
                sendSignal.Wait(token);

                while (sendQueue.TryDequeue(out IMessage message))
                {
                    if (token.IsCancellationRequested)
                        break;

                    SendMessage(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            HandleNetworkError(exception);
        }
    }

    private void SendMessage(IMessage message)
    {
        NetworkStream currentStream =
        stream ?? throw new IOException(
            "Network stream is unavailable.");

        ulong messageId =
        messageRegistry.GetId(message.GetType());

        byte[] data =
        BinaryMessageSerializer.Serialize(message);

        // Console.WriteLine(
        //     System.Text.Encoding.UTF8.GetString(data));


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

        currentStream.Write(packet, 0, packet.Length);
    }

    private void ReceiveLoop()
    {
        CancellationToken token =
        cancellation?.Token ?? default(CancellationToken);

        try
        {
            NetworkStream currentStream =
            stream ?? throw new IOException(
                "Network stream is unavailable.");

            byte[] lengthBuffer =
            new byte[sizeof(int)];

            while (!token.IsCancellationRequested)
            {
                ReadExactly(
                    currentStream,
                    lengthBuffer,
                    token);

                int messageLength =
                BitConverter.ToInt32(lengthBuffer, 0);

                if (messageLength < sizeof(ulong))
                {
                    throw new InvalidDataException(
                        "Invalid message length: " +
                        messageLength + ".");
                }

                if (messageLength > MaxMessageSize)
                {
                    throw new InvalidDataException(
                        "Message exceeds maximum size: " +
                        messageLength + " bytes.");
                }

                byte[] messageBuffer =
                new byte[messageLength];

                ReadExactly(
                    currentStream,
                    messageBuffer,
                    token);

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
                            messageId + ".",
                            exception);
                    }

                    IMessage message =
                    BinaryMessageSerializer.Deserialize(
                        data,
                        messageType);

                    receiveQueue.Enqueue(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (EndOfStreamException)
        {
            HandleDisconnected();
        }
        catch (IOException exception)
        {
            if (!token.IsCancellationRequested)
                HandleNetworkError(exception);
        }
        catch (Exception exception)
        {
            HandleNetworkError(exception);
        }
    }

    public bool TryReceive(out IMessage message)
    {
        return receiveQueue.TryDequeue(out message);
    }

    private static void ReadExactly(
        NetworkStream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        int offset = 0;

        while (offset < buffer.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int read =
            stream.Read(
                buffer,
                offset,
                buffer.Length - offset);

            if (read == 0)
            {
                throw new EndOfStreamException(
                    "Remote endpoint disconnected.");
            }

            offset += read;
        }
    }

    private void HandleDisconnected()
    {
        connected = false;

        try
        {
            stream?.Close();
        }
        catch
        {
        }

        try
        {
            client?.Close();
        }
        catch
        {
        }
    }

    private void HandleNetworkError(Exception exception)
    {
        if (!connected)
            return;

        Console.WriteLine(
            "Network error: " + exception);

        HandleDisconnected();
    }
}
