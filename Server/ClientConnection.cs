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
        BinaryStreamHandler io = new BinaryStreamHandler();

        io.Write(stream, message);
    }

    public IBinarySerializable Receive()
    {
        if (!Connected)
            throw new InvalidOperationException("Client is not connected.");

        BinaryStreamHandler io = new BinaryStreamHandler();

        return io.Read(stream);
    }

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
