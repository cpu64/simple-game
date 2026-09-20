using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public sealed class RemoteServerConnection : IDisposable
{
    private const int MaxMessageSize = 16 * 1024 * 1024;
    private const int ReconnectTimeoutMilliseconds = 5000;
    private const int ReconnectDelayMilliseconds = 100;

    private readonly IPEndPoint endpoint;

    private readonly ConcurrentQueue<object> sendQueue = new ConcurrentQueue<object>();

    private readonly ConcurrentQueue<object> receiveQueue = new ConcurrentQueue<object>();

    private readonly SemaphoreSlim sendSignal = new SemaphoreSlim(0);

    private readonly object stateLock = new object();

    private TcpClient client;
    private NetworkStream stream;

    private CancellationTokenSource cancellation;

    private Thread sendThread;
    private Thread receiveThread;

    private volatile bool connected;
    private volatile bool stopping;

    // This message is required after every successful TCP connection.
    private IBinarySerializable reconnectMessage;

    public bool IsConnected
    {
        get { return connected; }
    }

    public RemoteServerConnection(IPEndPoint endpoint)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        this.endpoint = endpoint;
    }

    public void Connect(IBinarySerializable registrationMessage)
    {
        if (registrationMessage == null)
            throw new ArgumentNullException(nameof(registrationMessage));

        lock (stateLock)
        {
            if (connected)
                throw new InvalidOperationException("Already connected.");

            reconnectMessage = registrationMessage;
            stopping = false;

            cancellation = new CancellationTokenSource();

            try
            {
                ConnectSocket(cancellation.Token);

                SendRegistration();

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
                CloseSocket();

                cancellation.Dispose();
                cancellation = null;

                throw;
            }
        }
    }

    public void Disconnect()
    {
        Thread sender;
        Thread receiver;
        CancellationTokenSource cts;

        lock (stateLock)
        {
            if (stopping)
                return;

            stopping = true;
            connected = false;

            sender = sendThread;
            receiver = receiveThread;

            sendThread = null;
            receiveThread = null;

            cts = cancellation;
            cancellation = null;
        }

        try
        {
            cts?.Cancel();
        }
        catch { }

        try
        {
            sendSignal.Release();
        }
        catch { }

        CloseSocket();

        if (sender != null && sender != Thread.CurrentThread)
            sender.Join();

        if (receiver != null && receiver != Thread.CurrentThread)
            receiver.Join();

        cts?.Dispose();
    }

    public void Dispose()
    {
        Disconnect();
        sendSignal.Dispose();
    }

    public void Send(IBinarySerializable message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (stopping)
            throw new InvalidOperationException("Connection is stopping.");

        // A disconnect/reconnect can happen here.
        // Wait for the connection layer to restore the connection instead of immediately telling LocalServer about the disconnect.
        WaitForConnection();

        lock (stateLock)
        {
            if (stopping)
                throw new InvalidOperationException("Connection is stopping.");

            if (!connected)
                throw new IOException("Unable to reconnect to server.");

            sendQueue.Enqueue(message);
        }

        sendSignal.Release();
    }

    private void WaitForConnection()
    {
        StopwatchTimer timer = new StopwatchTimer();

        while (!connected)
        {
            if (stopping)
                throw new InvalidOperationException("Connection is stopping.");

            if (timer.ElapsedMilliseconds >= ReconnectTimeoutMilliseconds)
                throw new IOException("Could not reconnect to server within " + ReconnectTimeoutMilliseconds + " ms.");

            Thread.Sleep(10);
        }
    }

    private void ConnectSocket(CancellationToken token)
    {
        TcpClient newClient = new TcpClient();

        newClient.NoDelay = true;

        // Connect() itself can block indefinitely, so use BeginConnect with our own timeout.
        IAsyncResult asyncResult = newClient.BeginConnect(endpoint.Address, endpoint.Port, null, null);

        try
        {
            if (!asyncResult.AsyncWaitHandle.WaitOne(1000))
            {
                newClient.Close();
                throw new IOException("Connection attempt timed out.");
            }

            token.ThrowIfCancellationRequested();

            newClient.EndConnect(asyncResult);

            NetworkStream newStream = newClient.GetStream();

            lock (stateLock)
            {
                if (stopping)
                {
                    newStream.Close();
                    newClient.Close();
                    throw new OperationCanceledException();
                }

                client = newClient;
                stream = newStream;
            }
        }
        catch
        {
            try
            {
                newClient.Close();
            }
            catch { }

            throw;
        }
    }

    private void SendLoop()
    {
        CancellationToken token = cancellation?.Token ?? default(CancellationToken);

        try
        {
            while (!token.IsCancellationRequested)
            {
                sendSignal.Wait(token);

                Object obj = null;

                IBinarySerializable message = obj as IBinarySerializable;

                while (sendQueue.TryDequeue(out obj))
                {
                    message = obj as IBinarySerializable;

                    token.ThrowIfCancellationRequested();

                    if (!connected)
                    {
                        // Put it back. It must not disappear during a transient disconnect.
                        sendQueue.Enqueue(message);
                        break;
                    }

                    SendMessage(message);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            HandleConnectionFailure(exception);
        }
    }

    private void SendMessage(IBinarySerializable message)
    {
        NetworkStream currentStream;

        lock (stateLock)
        {
            currentStream = stream;

            if (currentStream == null || !connected)
                throw new IOException("Network stream is unavailable.");
        }

        try
        {
            BinaryStreamHandler io = new BinaryStreamHandler();
            io.Write(stream, message);
        }
        catch (Exception exception)
        {
            HandleConnectionFailure(exception);
            throw;
        }
    }

    private void ReceiveLoop()
    {
        CancellationToken token = cancellation?.Token ?? default(CancellationToken);

        try
        {
            while (!token.IsCancellationRequested)
            {
                NetworkStream currentStream;

                lock (stateLock)
                {
                    currentStream = stream;
                }

                if (currentStream == null)
                    throw new IOException("Network stream is unavailable.");

                ReceiveMessages(currentStream, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            HandleConnectionFailure(exception);
        }
    }

    public bool TryReceive(out IBinarySerializable message)
    {
        Object obj = null;

        bool ret = receiveQueue.TryDequeue(out obj);

        message = obj as IBinarySerializable;

        return ret;
    }

    private void ReceiveMessages(NetworkStream currentStream, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            BinaryStreamHandler io = new BinaryStreamHandler();

            IBinarySerializable message = io.Read(currentStream);

            receiveQueue.Enqueue(message);
        }
    }

    private void HandleConnectionFailure(Exception exception)
    {
        if (stopping)
            return;

        Console.WriteLine("Network connection lost: " + exception.Message);

        connected = false;

        CloseSocket();

        TryReconnect();
    }

    private void TryReconnect()
    {
        StopwatchTimer timer = new StopwatchTimer();

        CancellationToken token = cancellation?.Token ?? default(CancellationToken);

        while (!stopping && !token.IsCancellationRequested && timer.ElapsedMilliseconds < ReconnectTimeoutMilliseconds)
        {
            try
            {
                Console.WriteLine("Attempting to reconnect...");

                ConnectSocket(token);

                // Registration is part of establishing the application-level connection, not merely the TCP connection.
                SendRegistration();

                connected = true;

                Console.WriteLine("Reconnected.");

                sendSignal.Release();

                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception reconnectException)
            {
                CloseSocket();

                Console.WriteLine("Reconnect failed: " + reconnectException.Message);

                Thread.Sleep(ReconnectDelayMilliseconds);
            }
        }

        if (stopping)
            return;

        connected = false;

        Console.WriteLine("Could not reconnect within " + ReconnectTimeoutMilliseconds + " ms.");
    }

    private void SendRegistration()
    {
        try
        {
            BinaryStreamHandler io = new BinaryStreamHandler();
            io.Write(stream, reconnectMessage);
        }
        catch (Exception exception)
        {
            HandleConnectionFailure(exception);
            throw;
        }
    }

    private void CloseSocket()
    {
        NetworkStream oldStream;
        TcpClient oldClient;

        lock (stateLock)
        {
            oldStream = stream;
            oldClient = client;

            stream = null;
            client = null;
        }

        try
        {
            oldStream?.Close();
        }
        catch { }

        try
        {
            oldClient?.Close();
        }
        catch { }
    }

    private static void ReadExactly(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int offset = 0;

        while (offset < buffer.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int read = stream.Read(buffer, offset, buffer.Length - offset);

            if (read == 0)
            {
                throw new EndOfStreamException("Remote endpoint disconnected.");
            }

            offset += read;
        }
    }

    private sealed class StopwatchTimer
    {
        private readonly DateTime start = DateTime.UtcNow;

        public long ElapsedMilliseconds
        {
            get { return (long)(DateTime.UtcNow - start).TotalMilliseconds; }
        }
    }
}
