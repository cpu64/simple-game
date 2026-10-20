using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    AI = 1 << 3,
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

    public DecoratedString ToDecoratedString()
    {
        DecoratedString result = new ItalicString(new CyanString($"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}")) + " " + FormatLevel(Level);

        if (Category != LogCategory.None)
        {
            result += new PurpleString($" [{Category}]");
        }

        result += new BlueString($" [T{ThreadId}] ");

        result += Message?.ToString() ?? string.Empty;

        if (Exception != null)
        {
            result += Environment.NewLine + new RedString(new BoldString(Exception.ToString()));
        }

        return result;
    }

    private static DecoratedString FormatLevel(LogLevel level)
    {
        string label = $"[{level}]";

        return level switch
        {
            LogLevel.Trace => new ItalicString(label),
            LogLevel.Debug => new CyanString(label),
            LogLevel.Info => new GreenString(label),
            LogLevel.Warning => new BoldString(new OrangeString(label)),
            LogLevel.Error => new BoldString(new RedString(label)),
            LogLevel.Fatal => new SlowBlinkString(new BoldString(new RedString(label))),
            _ => new PlainString(label),
        };
    }

    public override string ToString() => ToDecoratedString().PlainText();
}

public interface ILogSink : IDisposable
{
    void Write(LogEntry entry);
    void Flush();
}

public sealed class ConsoleUISink : ILogSink
{
    private readonly ConsoleUI _console;

    public ConsoleUISink(ConsoleUI console)
    {
        _console = console;
    }

    public void Write(LogEntry entry)
    {
        _console.WriteLine(entry.ToDecoratedString().StyledText());
    }

    public void Flush() { }

    public void Dispose() { }
}

public sealed class ConsoleSink : ILogSink
{
    private readonly object _lock = new();

    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            Console.WriteLine(entry.ToDecoratedString().StyledText());
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
        Debug.WriteLine(entry.ToDecoratedString().StyledText());
    }

    public void Flush() { }

    public void Dispose() { }
}

public sealed class AppendFileSink : ILogSink
{
    private readonly object _lock = new();
    private readonly StreamWriter _writer;

    public AppendFileSink(string path)
    {
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = false };
    }

    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            _writer.WriteLine(entry.ToString());
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

public sealed class FileSink : ILogSink
{
    private readonly object _lock = new();
    private readonly StreamWriter _writer;

    public FileSink(string path)
    {
        _writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = false };
    }

    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            _writer.WriteLine(entry.ToString());
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

public interface ILogStart
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

    public ILogStart Trace(object? message, LogCategory category = LogCategory.None)
    {
        return new FluentLogOperation(this, LogLevel.Trace, message, category);
    }

    public ILogStart Debug(object? message, LogCategory category = LogCategory.None)
    {
        return new FluentLogOperation(this, LogLevel.Debug, message, category);
    }

    public ILogStart Info(object? message, LogCategory category = LogCategory.None)
    {
        return new FluentLogOperation(this, LogLevel.Info, message, category);
    }

    public ILogStart Warning(object? message, LogCategory category = LogCategory.None)
    {
        return new FluentLogOperation(this, LogLevel.Warning, message, category);
    }

    public ILogStart Error(object? message, LogCategory category = LogCategory.None, Exception? exception = null)
    {
        return new FluentLogOperation(this, LogLevel.Error, message, category, exception);
    }

    public ILogStart Fatal(object? message, LogCategory category = LogCategory.None, Exception? exception = null)
    {
        return new FluentLogOperation(this, LogLevel.Fatal, message, category, exception, flushOnWrite: true);
    }

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

    private sealed class FluentLogOperation : ILogStart, ILogAfterAssert, ILogAfterEvery
    {
        private readonly Logger _logger;

        private readonly LogLevel _level;
        private readonly object? _message;
        private readonly Exception? _exception;
        private readonly bool _flushOnWrite;

        private LogCategory _category;

        private bool _enabled;

        internal FluentLogOperation(
            Logger logger,
            LogLevel level,
            object? message,
            LogCategory category,
            Exception? exception = null,
            bool flushOnWrite = false
        )
        {
            _logger = logger;
            _level = level;
            _message = message;
            _exception = exception;
            _flushOnWrite = flushOnWrite;
            _category = category;
            _enabled = logger.ShouldLog(level, category);
        }

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

#endif

            return this;
        }

        // Every - count
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

        // Every - interval
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

        public void Write()
        {
            if (!_enabled)
                return;

            _logger.Log(_level, _message, _category, _exception, _flushOnWrite);
        }

        public LogScope Time()
        {
            if (!_enabled)
            {
                return LogScope.Disabled;
            }

            return new LogScope(_logger, _level, _message, _category, _exception, _flushOnWrite);
        }
    }

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
