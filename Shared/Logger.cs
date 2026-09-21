using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

// ========================================================================
// Log Level
// ========================================================================

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5,
}

// ========================================================================
// Log Category
// ========================================================================

[Flags]
public enum LogCategory
{
    None = 0,
    Physics = 1 << 0,
    Rendering = 1 << 1,
    Audio = 1 << 2,
    Network = 1 << 3,
    Input = 1 << 4,
    AI = 1 << 5,
    UI = 1 << 6,
    Gameplay = 1 << 7,
    Performance = 1 << 8,
}

// ========================================================================
// Log Entry
// ========================================================================

public sealed class LogEntry
{
    public DateTime Timestamp { get; }
    public LogLevel Level { get; }
    public LogCategory Category { get; }
    public int ThreadId { get; }
    public object? Message { get; }
    public Exception? Exception { get; }

    public LogEntry(LogLevel level, LogCategory category, object? message, Exception? exception)
    {
        Timestamp = DateTime.Now;
        Level = level;
        Category = category;
        ThreadId = Environment.CurrentManagedThreadId;
        Message = message;
        Exception = exception;
    }

    public override string ToString()
    {
        string message = Message?.ToString() ?? string.Empty;

        string category = Category == LogCategory.None ? string.Empty : $" [{Category}]";

        string result = $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} " + $"[{Level}]" + $"{category} " + $"[T{ThreadId}] " + message;

        if (Exception != null)
        {
            result += Environment.NewLine + Exception;
        }

        return result;
    }
}

// ========================================================================
// Log Sink
// ========================================================================

public interface ILogSink : IDisposable
{
    void Write(LogEntry entry);
    void Flush();
}

// ========================================================================
// Console Sink
// ========================================================================

public sealed class ConsoleSink : ILogSink
{
    private readonly object _lock = new();

    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            Console.WriteLine(entry);
        }
    }

    public void Flush()
    {
        lock (_lock)
        {
            Console.Out.Flush();
            Console.Error.Flush();
        }
    }

    public void Dispose()
    {
        Flush();
    }
}

// ========================================================================
// Debug Sink
// ========================================================================

public sealed class DebugSink : ILogSink
{
    public void Write(LogEntry entry)
    {
        Debug.WriteLine(entry);
    }

    public void Flush() { }

    public void Dispose() { }
}

// ========================================================================
// File Sink
// ========================================================================

public sealed class FileSink : ILogSink
{
    private readonly object _lock = new();
    private readonly StreamWriter _writer;

    public FileSink(string path)
    {
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = false };
    }

    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            _writer.WriteLine(entry);
        }
    }

    public void Flush()
    {
        lock (_lock)
        {
            _writer.Flush();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Flush();
            _writer.Dispose();
        }
    }
}

// ========================================================================
// Logger
// ========================================================================

public sealed class Logger : IDisposable
{
    public static Logger Instance { get; } = new();

    private readonly ConcurrentQueue<LogWorkItem> _queue = new();
    private readonly AutoResetEvent _signal = new(false);

    private readonly object _sinkLock = new();
    private readonly List<ILogSink> _sinks = new();

    private readonly object _rateLimitLock = new();

    private readonly Dictionary<string, DateTime> _rateLimitTimes = new();

    private readonly Dictionary<string, long> _rateLimitCounts = new();

    private readonly Thread _writerThread;

    private volatile bool _running = true;

    private LogLevel _minimumLevel = LogLevel.Trace;

    private LogCategory _enabledCategories =
        LogCategory.Physics
        | LogCategory.Rendering
        | LogCategory.Audio
        | LogCategory.Network
        | LogCategory.Input
        | LogCategory.AI
        | LogCategory.UI
        | LogCategory.Gameplay
        | LogCategory.Performance;

    private Logger()
    {
        _writerThread = new Thread(WriterLoop) { Name = "Logger", IsBackground = true };

        _writerThread.Start();
    }

    // ====================================================================
    // Configuration
    // ====================================================================

    public LogLevel MinimumLevel
    {
        get => _minimumLevel;
        set => _minimumLevel = value;
    }

    public LogCategory EnabledCategories
    {
        get => _enabledCategories;
        set => _enabledCategories = value;
    }

    /// <summary>
    /// Returns true when at least one bit in category is enabled.
    /// </summary>
    public bool IsCategoryEnabled(LogCategory category)
    {
        if (category == LogCategory.None)
            return true;

        return (_enabledCategories & category) != 0;
    }

    public void EnableCategory(LogCategory category)
    {
        _enabledCategories |= category;
    }

    public void DisableCategory(LogCategory category)
    {
        _enabledCategories &= ~category;
    }

    public void AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        lock (_sinkLock)
        {
            if (!_running)
                throw new ObjectDisposedException(nameof(Logger));

            _sinks.Add(sink);
        }
    }

    public void RemoveSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        lock (_sinkLock)
        {
            _sinks.Remove(sink);
        }
    }

    // ====================================================================
    // Logging
    // ====================================================================

    public void Trace(object? message, LogCategory category = LogCategory.None)
    {
        Log(LogLevel.Trace, message, category);
    }

    public void Debug(object? message, LogCategory category = LogCategory.None)
    {
        Log(LogLevel.Debug, message, category);
    }

    public void Info(object? message, LogCategory category = LogCategory.None)
    {
        Log(LogLevel.Info, message, category);
    }

    public void Warning(object? message, LogCategory category = LogCategory.None)
    {
        Log(LogLevel.Warning, message, category);
    }

    public void Error(object? message, LogCategory category = LogCategory.None, Exception? exception = null)
    {
        Log(LogLevel.Error, message, category, exception);
    }

    public void Fatal(object? message, LogCategory category = LogCategory.None, Exception? exception = null)
    {
        Log(LogLevel.Fatal, message, category, exception);

        Flush();
    }

    // ====================================================================
    // Internal Logging
    // ====================================================================

    internal void Log(LogLevel level, object? message, LogCategory category, Exception? exception = null)
    {
        if (!ShouldLog(level, category))
            return;

        _queue.Enqueue(LogWorkItem.Log(new LogEntry(level, category, message, exception)));

        _signal.Set();
    }

    private bool ShouldLog(LogLevel level, LogCategory category)
    {
        if (!_running)
            return false;

        if (level < _minimumLevel)
            return false;

        if (category != LogCategory.None && (_enabledCategories & category) == 0)
        {
            return false;
        }

        return true;
    }

    // ====================================================================
    // Timers
    // ====================================================================

    public LogTimer Time(object? message, LogCategory category = LogCategory.None, LogLevel level = LogLevel.Debug)
    {
        if (!ShouldLog(level, category))
            return LogTimer.Disabled;

        return new LogTimer(this, message, category, level);
    }

    public sealed class LogTimer : IDisposable
    {
        private readonly Logger? _logger;
        private readonly object? _message;
        private readonly LogCategory _category;
        private readonly LogLevel _level;

        private long _startTimestamp;

        private bool _enabled;
        private bool _disposed;

        private readonly string _rateLimitKey;

        internal static LogTimer Disabled => new();

        private LogTimer()
        {
            _logger = null;
            _message = null;
            _category = LogCategory.None;
            _level = LogLevel.Debug;

            _rateLimitKey = string.Empty;

            _enabled = false;
        }

        internal LogTimer(Logger logger, object? message, LogCategory category, LogLevel level)
        {
            _logger = logger;
            _message = message;
            _category = category;
            _level = level;

            // Each Time() expression gets its own counter key.
            //
            // This uses the call site rather than message.ToString()
            // so mutable objects cannot unexpectedly change the key.
            _rateLimitKey = $"{GetCallerKey()}:{category}:{message?.GetType().FullName}";

            _enabled = true;

            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public LogTimer Every(long everyNCalls)
        {
            if (everyNCalls <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(everyNCalls));
            }

            if (!_enabled)
                return this;

            if (!_logger!.ShouldLogEvery(everyNCalls, _rateLimitKey))
            {
                _enabled = false;
                return this;
            }

            // Restart the timer at the point where we know this
            // particular call should actually be measured.
            _startTimestamp = Stopwatch.GetTimestamp();

            return this;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (!_enabled)
                return;

            long elapsedTicks = Stopwatch.GetTimestamp() - _startTimestamp;

            double milliseconds = elapsedTicks * 1000.0 / Stopwatch.Frequency;

            _logger!.Log(_level, $"{_message?.ToString() ?? string.Empty} " + $"took {milliseconds:F3} ms", _category);
        }

        private static string GetCallerKey([CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return $"{file}:{line}";
        }
    }

    // ====================================================================
    // Rate Limiting
    // ====================================================================

    private bool ShouldLogEvery(long everyNCalls, string key)
    {
        lock (_rateLimitLock)
        {
            _rateLimitCounts.TryGetValue(key, out long count);

            count++;

            _rateLimitCounts[key] = count;

            return count % everyNCalls == 0;
        }
    }

    public void LogEvery(TimeSpan interval, string key, LogLevel level, object? message, LogCategory category = LogCategory.None, Exception? exception = null)
    {
        if (!ShouldLog(level, category))
            return;

        if (interval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        DateTime now = DateTime.UtcNow;

        lock (_rateLimitLock)
        {
            if (_rateLimitTimes.TryGetValue(key, out DateTime last))
            {
                if (now - last < interval)
                    return;
            }

            _rateLimitTimes[key] = now;
        }

        Log(level, message, category, exception);
    }

    public void LogEvery(long everyNCalls, string key, LogLevel level, object? message, LogCategory category = LogCategory.None, Exception? exception = null)
    {
        if (!ShouldLog(level, category))
            return;

        if (everyNCalls <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(everyNCalls));
        }

        if (!ShouldLogEvery(everyNCalls, key))
            return;

        Log(level, message, category, exception);
    }

    // ====================================================================
    // Assertions
    // ====================================================================

#if DEBUG

    public void Assert(Func<bool> condition, object? message, LogCategory category = LogCategory.None)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (condition())
        {
            Error(message, category);
        }
    }
#else

    [Conditional("DEBUG")]
    public void Assert(Func<bool> condition, object? message, LogCategory category = LogCategory.None) { }
#endif

    // ====================================================================
    // Flush
    // ====================================================================

    public void Flush()
    {
        if (!_running)
            return;

        using ManualResetEventSlim completed = new(false);

        _queue.Enqueue(LogWorkItem.Flush(completed));

        _signal.Set();

        completed.Wait();
    }

    // ====================================================================
    // Shutdown
    // ====================================================================

    public void Shutdown()
    {
        if (!_running)
            return;

        _running = false;

        _signal.Set();

        if (Thread.CurrentThread != _writerThread)
        {
            _writerThread.Join();
        }

        lock (_sinkLock)
        {
            foreach (ILogSink sink in _sinks)
            {
                try
                {
                    sink.Flush();
                }
                catch { }

                try
                {
                    sink.Dispose();
                }
                catch { }
            }

            _sinks.Clear();
        }

        _signal.Dispose();
    }

    public void Dispose()
    {
        Shutdown();
    }

    // ====================================================================
    // Writer Thread
    // ====================================================================

    private void WriterLoop()
    {
        while (_running || !_queue.IsEmpty)
        {
            if (!_queue.TryDequeue(out LogWorkItem? work))
            {
                _signal.WaitOne(100);
                continue;
            }

            ProcessWorkItem(work);
        }

        while (_queue.TryDequeue(out LogWorkItem? work))
        {
            ProcessWorkItem(work);
        }

        FlushSinks();
    }

    private void ProcessWorkItem(LogWorkItem work)
    {
        try
        {
            switch (work.Type)
            {
                case LogWorkItemType.Log:

                    if (work.Entry != null)
                    {
                        WriteToSinks(work.Entry);
                    }

                    break;

                case LogWorkItemType.Flush:

                    FlushSinks();

                    work.FlushEvent?.Set();

                    break;
            }
        }
        catch
        {
            work.FlushEvent?.Set();
        }
    }

    private void WriteToSinks(LogEntry entry)
    {
        ILogSink[] sinks;

        lock (_sinkLock)
        {
            sinks = _sinks.ToArray();
        }

        foreach (ILogSink sink in sinks)
        {
            try
            {
                sink.Write(entry);
            }
            catch { }
        }
    }

    private void FlushSinks()
    {
        ILogSink[] sinks;

        lock (_sinkLock)
        {
            sinks = _sinks.ToArray();
        }

        foreach (ILogSink sink in sinks)
        {
            try
            {
                sink.Flush();
            }
            catch { }
        }
    }

    // ====================================================================
    // Queue Work Item
    // ====================================================================

    private enum LogWorkItemType
    {
        Log,
        Flush,
    }

    private sealed class LogWorkItem
    {
        public LogWorkItemType Type { get; }

        public LogEntry? Entry { get; }

        public ManualResetEventSlim? FlushEvent { get; }

        private LogWorkItem(LogWorkItemType type, LogEntry? entry, ManualResetEventSlim? flushEvent)
        {
            Type = type;
            Entry = entry;
            FlushEvent = flushEvent;
        }

        public static LogWorkItem Log(LogEntry entry)
        {
            return new LogWorkItem(LogWorkItemType.Log, entry, null);
        }

        public static LogWorkItem Flush(ManualResetEventSlim flushEvent)
        {
            return new LogWorkItem(LogWorkItemType.Flush, null, flushEvent);
        }
    }
}
