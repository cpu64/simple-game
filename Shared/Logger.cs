using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5,
}

[Flags]
public enum LogCategory
{
    None = 0,
    Rendering = 1 << 0,
    Simulation = 1 << 1,
    Physics = 1 << 2,
    Network = 1 << 3,
}

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

public interface ILogSink : IDisposable
{
    void Write(LogEntry entry);
    void Flush();
}

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

public sealed class DebugSink : ILogSink
{
    public void Write(LogEntry entry)
    {
        Debug.WriteLine(entry);
    }

    public void Flush() { }

    public void Dispose() { }
}

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

// ============================================================================
// Fluent API interfaces
// ============================================================================
//
// The interfaces intentionally expose only the operations that are legal at
// each stage.
//
// Start
//   Category / Assert / Every / Time / Write
//
// After Category
//   Assert / Every / Time / Write
//
// After Assert
//   Every / Time / Write
//
// After Every
//   Time / Write
//
// This enforces:
//
// Category -> Assert -> Every -> Time
//
// while allowing every stage to be skipped.
// ============================================================================

public interface ILogStart
{
    ILogAfterCategory Category(LogCategory category);

    ILogAfterAssert Assert(Func<bool> condition);

    ILogAfterEvery Every(string key, long everyNCalls);

    ILogAfterEvery Every(string key, TimeSpan interval);

    Logger.LogScope Time();

    void Write();
}

public interface ILogAfterCategory
{
    ILogAfterAssert Assert(Func<bool> condition);

    ILogAfterEvery Every(string key, long everyNCalls);

    ILogAfterEvery Every(string key, TimeSpan interval);

    Logger.LogScope Time();

    void Write();
}

public interface ILogAfterAssert
{
    ILogAfterEvery Every(string key, long everyNCalls);

    ILogAfterEvery Every(string key, TimeSpan interval);

    Logger.LogScope Time();

    void Write();
}

public interface ILogAfterEvery
{
    Logger.LogScope Time();

    void Write();
}

// ============================================================================
// Logger
// ============================================================================

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

    private LogCategory _enabledCategories = ~LogCategory.None;

    private Logger()
    {
        _writerThread = new Thread(WriterLoop) { Name = "Logger", IsBackground = true };

        _writerThread.Start();
    }

    // ========================================================================
    // Configuration
    // ========================================================================

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

    // ========================================================================
    // Sinks
    // ========================================================================

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

    // ========================================================================
    // Fluent entry points
    // ========================================================================

    public ILogStart Trace(object? message)
    {
        return new FluentLogOperation(this, LogLevel.Trace, message);
    }

    public ILogStart Debug(object? message)
    {
        return new FluentLogOperation(this, LogLevel.Debug, message);
    }

    public ILogStart Info(object? message)
    {
        return new FluentLogOperation(this, LogLevel.Info, message);
    }

    public ILogStart Warning(object? message)
    {
        return new FluentLogOperation(this, LogLevel.Warning, message);
    }

    public ILogStart Error(object? message, Exception? exception = null)
    {
        return new FluentLogOperation(this, LogLevel.Error, message, exception);
    }

    public ILogStart Fatal(object? message, Exception? exception = null)
    {
        return new FluentLogOperation(this, LogLevel.Fatal, message, exception, flushOnWrite: true);
    }

    // ========================================================================
    // Internal logging
    // ========================================================================

    internal void Log(LogLevel level, object? message, LogCategory category, Exception? exception = null, bool flush = false)
    {
        if (!ShouldLog(level, category))
            return;

        _queue.Enqueue(LogWorkItem.Log(new LogEntry(level, category, message, exception)));

        _signal.Set();

        if (flush)
        {
            Flush();
        }
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

    // ========================================================================
    // Rate limiting
    // ========================================================================

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

    private bool ShouldLogEvery(TimeSpan interval, string key)
    {
        DateTime now = DateTime.UtcNow;

        lock (_rateLimitLock)
        {
            if (_rateLimitTimes.TryGetValue(key, out DateTime last))
            {
                if (now - last < interval)
                    return false;
            }

            _rateLimitTimes[key] = now;

            return true;
        }
    }

    // ========================================================================
    // Flush / shutdown
    // ========================================================================

    public void Flush()
    {
        if (!_running)
            return;

        using ManualResetEventSlim completed = new(false);

        _queue.Enqueue(LogWorkItem.Flush(completed));

        _signal.Set();

        completed.Wait();
    }

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

    // ========================================================================
    // Fluent operation
    // ========================================================================

    private sealed class FluentLogOperation : ILogStart, ILogAfterCategory, ILogAfterAssert, ILogAfterEvery
    {
        private readonly Logger _logger;

        private readonly LogLevel _level;
        private readonly object? _message;
        private readonly Exception? _exception;
        private readonly bool _flushOnWrite;

        private LogCategory _category;

        private bool _enabled;

        internal FluentLogOperation(Logger logger, LogLevel level, object? message, Exception? exception = null, bool flushOnWrite = false)
        {
            _logger = logger;
            _level = level;
            _message = message;
            _exception = exception;
            _flushOnWrite = flushOnWrite;

            _category = LogCategory.None;

            // This is intentionally only the inexpensive initial filtering.
            //
            // No assertion is evaluated.
            // No rate-limit bookkeeping occurs.
            // No stopwatch is started.
            // No LogEntry is allocated.
            _enabled = logger.ShouldLog(level, LogCategory.None);
        }

        // ====================================================================
        // Category
        // ====================================================================

        public ILogAfterCategory Category(LogCategory category)
        {
            if (!_enabled)
                return this;

            _category = category;

            if (!_logger.ShouldLog(_level, _category))
            {
                _enabled = false;
            }

            return this;
        }

        // ====================================================================
        // Assert
        // ====================================================================

        public ILogAfterAssert Assert(Func<bool> condition)
        {
            ArgumentNullException.ThrowIfNull(condition);

#if DEBUG
            if (!_enabled)
                return this;

            if (!condition())
            {
                _enabled = false;
            }
#else
            // Deliberately do not invoke condition in RELEASE.
            //
            // The fluent call remains syntactically valid, but the predicate
            // is never evaluated.
#endif

            return this;
        }

        // ====================================================================
        // Every - count
        // ====================================================================

        public ILogAfterEvery Every(string key, long everyNCalls)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (everyNCalls <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(everyNCalls));
            }

            if (!_enabled)
                return this;

            if (!_logger.ShouldLogEvery(everyNCalls, key))
            {
                _enabled = false;
            }

            return this;
        }

        // ====================================================================
        // Every - interval
        // ====================================================================

        public ILogAfterEvery Every(string key, TimeSpan interval)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (interval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval));
            }

            if (!_enabled)
                return this;

            if (!_logger.ShouldLogEvery(interval, key))
            {
                _enabled = false;
            }

            return this;
        }

        // ====================================================================
        // Write
        // ====================================================================

        public void Write()
        {
            if (!_enabled)
                return;

            _logger.Log(_level, _message, _category, _exception, _flushOnWrite);
        }

        // ====================================================================
        // Time
        // ====================================================================

        public LogScope Time()
        {
            if (!_enabled)
            {
                return LogScope.Disabled;
            }

            // All filtering has already succeeded.
            //
            // Only now is the stopwatch started.
            return new LogScope(_logger, _level, _message, _category, _exception, _flushOnWrite);
        }
    }

    // ========================================================================
    // Timing scope
    // ========================================================================

    public sealed class LogScope : IDisposable
    {
        private readonly Logger? _logger;

        private readonly LogLevel _level;
        private readonly object? _message;
        private readonly LogCategory _category;
        private readonly Exception? _exception;
        private readonly bool _flushOnWrite;

        private readonly long _startTimestamp;

        private bool _disposed;

        internal static LogScope Disabled => new();

        private LogScope()
        {
            _logger = null;
            _level = LogLevel.Debug;
            _message = null;
            _category = LogCategory.None;
            _exception = null;
            _flushOnWrite = false;

            _startTimestamp = 0;
        }

        internal LogScope(Logger logger, LogLevel level, object? message, LogCategory category, Exception? exception, bool flushOnWrite)
        {
            _logger = logger;

            _level = level;
            _message = message;
            _category = category;
            _exception = exception;
            _flushOnWrite = flushOnWrite;

            // IMPORTANT:
            //
            // This is the first stopwatch operation in the entire fluent pipeline. Category, Assert and Every have already succeeded.
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_logger == null)
                return;

            long elapsedTicks = Stopwatch.GetTimestamp() - _startTimestamp;

            double milliseconds = elapsedTicks * 1000.0 / Stopwatch.Frequency;

            string message = $"{_message?.ToString() ?? string.Empty} " + $"took {milliseconds:F3} ms";

            _logger.Log(_level, message, _category, _exception, _flushOnWrite);
        }
    }

    // ========================================================================
    // Async writer
    // ========================================================================

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

    // ========================================================================
    // Work items
    // ========================================================================

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
