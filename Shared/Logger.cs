using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Game.Logging
{
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
    // Log Entry
    // ========================================================================

    public sealed class LogEntry
    {
        public DateTime Timestamp { get; }
        public LogLevel Level { get; }
        public string? Category { get; }
        public int ThreadId { get; }
        public object? Message { get; }
        public Exception? Exception { get; }

        public LogEntry(LogLevel level, object? message, string? category, Exception? exception)
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

            string category = string.IsNullOrEmpty(Category) ? string.Empty : $" [{Category}]";

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
        // Standard Logging
        // ====================================================================

        public void Trace(object? message, string? category = null)
        {
            Log(LogLevel.Trace, message, category);
        }

        public void Debug(object? message, string? category = null)
        {
            Log(LogLevel.Debug, message, category);
        }

        public void Info(object? message, string? category = null)
        {
            Log(LogLevel.Info, message, category);
        }

        public void Warning(object? message, string? category = null)
        {
            Log(LogLevel.Warning, message, category);
        }

        public void Error(object? message, Exception? exception = null, string? category = null)
        {
            Log(LogLevel.Error, message, category, exception);
        }

        public void Fatal(object? message, Exception? exception = null, string? category = null)
        {
            Log(LogLevel.Fatal, message, category, exception);

            Flush();
        }

        // ====================================================================
        // Timers
        // ====================================================================

        /// <summary>
        /// Creates a timer that logs the elapsed time when disposed.
        ///
        /// If the requested log level is disabled, this returns a shared
        /// no-op disposable and does not start a timer.
        /// </summary>
        public IDisposable Time(object? message, string? category = null, LogLevel level = LogLevel.Debug)
        {
            if (!_running || level < _minimumLevel)
                return NoOpDisposable.Instance;

            return new LogTimer(this, message, category, level);
        }

        private sealed class LogTimer : IDisposable
        {
            private readonly Logger _logger;
            private readonly object? _message;
            private readonly string? _category;
            private readonly LogLevel _level;

            private readonly long _startTimestamp;

            private bool _disposed;

            public LogTimer(Logger logger, object? message, string? category, LogLevel level)
            {
                _logger = logger;
                _message = message;
                _category = category;
                _level = level;

                _startTimestamp = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                long elapsedTicks = Stopwatch.GetTimestamp() - _startTimestamp;

                double milliseconds = elapsedTicks * 1000.0 / Stopwatch.Frequency;

                _logger.Log(_level, $"{_message?.ToString() ?? string.Empty} " + $"took {milliseconds:F3} ms", _category);
            }
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();

            private NoOpDisposable() { }

            public void Dispose() { }
        }

        // ====================================================================
        // Internal Logging
        // ====================================================================

        private void Log(LogLevel level, object? message, string? category, Exception? exception = null)
        {
            if (!_running)
                return;

            if (level < _minimumLevel)
                return;

            _queue.Enqueue(LogWorkItem.Log(new LogEntry(level, message, category, exception)));

            _signal.Set();
        }

        // ====================================================================
        // Rate Limiting
        // ====================================================================

        /// <summary>
        /// Logs at most once during the specified interval.
        /// </summary>
        public void LogEvery(TimeSpan interval, string key, LogLevel level, object? message, string? category = null, Exception? exception = null)
        {
            if (!_running || level < _minimumLevel)
                return;

            if (interval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval));

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

        /// <summary>
        /// Logs every Nth call.
        /// </summary>
        public void LogEvery(long everyNCalls, string key, LogLevel level, object? message, string? category = null, Exception? exception = null)
        {
            if (!_running || level < _minimumLevel)
                return;

            if (everyNCalls <= 0)
                throw new ArgumentOutOfRangeException(nameof(everyNCalls));

            lock (_rateLimitLock)
            {
                _rateLimitCounts.TryGetValue(key, out long count);

                count++;

                _rateLimitCounts[key] = count;

                if (count % everyNCalls != 0)
                    return;
            }

            Log(level, message, category, exception);
        }

        // ====================================================================
        // Assertions
        // ====================================================================

#if DEBUG

        /// <summary>
        /// Evaluates the condition only in DEBUG builds.
        ///
        /// If condition() returns true, the message is logged.
        /// If condition() returns false, nothing happens.
        /// </summary>
        public void Assert(Func<bool> condition, object? message, string? category = null)
        {
            ArgumentNullException.ThrowIfNull(condition);

            if (condition())
            {
                Error(message, category: category);
            }
        }
#else

        /// <summary>
        /// This method call is removed completely from RELEASE builds.
        /// Therefore condition() is never evaluated.
        /// </summary>
        [Conditional("DEBUG")]
        public void Assert(Func<bool> condition, object? message, string? category = null) { }
#endif

        // ====================================================================
        // Flush
        // ====================================================================

        /// <summary>
        /// Waits until every log entry that was queued before this call
        /// has been written to all sinks.
        /// </summary>
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

            // Wake the writer so it can drain the remaining queue.
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
                    catch
                    {
                        // Logging must never crash the game.
                    }

                    try
                    {
                        sink.Dispose();
                    }
                    catch
                    {
                        // Logging must never crash the game.
                    }
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
                    // The logger itself must never bring down the game.
                    work.FlushEvent?.Set();
                }
            }

            // The loop condition should already have drained the queue,
            // but drain once more as a final guarantee.
            while (_queue.TryDequeue(out LogWorkItem? work))
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

            FlushSinks();
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
                catch
                {
                    // A broken sink must never crash the game.
                }
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
                catch
                {
                    // A broken sink must never crash the game.
                }
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
}
