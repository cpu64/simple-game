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
            Console.Out.Flush();
            Console.Error.Flush();
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

    public sealed class Logger : IDisposable
    {
        public static Logger Instance { get; } = new Logger();

        private readonly ConcurrentQueue<LogEntry> _queue = new();
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

        // ------------------------------------------------------------
        // Configuration
        // ------------------------------------------------------------

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

        // ------------------------------------------------------------
        // Standard logging
        // ------------------------------------------------------------

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

            // Fatal messages should be forced to disk/output.
            Flush();
        }

        public IDisposable Time(object? message, string? category = null, LogLevel level = LogLevel.Debug)
        {
            if (!_running || level < _minimumLevel)
                return NoOpDisposable.Instance;

            return new LogTimer(this, message, category, level);
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();

            private NoOpDisposable() { }

            public void Dispose() { }
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

        private void Log(LogLevel level, object? message, string? category, Exception? exception = null)
        {
            if (!_running)
                return;

            if (level < _minimumLevel)
                return;

            _queue.Enqueue(new LogEntry(level, message, category, exception));

            _signal.Set();
        }

        // ------------------------------------------------------------
        // Rate limiting
        // ------------------------------------------------------------

        /// <summary>
        /// Logs at most once during the specified time span.
        /// The key identifies the rate-limited message.
        /// </summary>
        public void LogEvery(TimeSpan interval, string key, LogLevel level, object? message, string? category = null, Exception? exception = null)
        {
            if (!_running || level < _minimumLevel)
                return;

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

        // ------------------------------------------------------------
        // Assertions
        // ------------------------------------------------------------

#if DEBUG

        /// <summary>
        /// Evaluates the condition only in DEBUG builds.
        ///
        /// If condition() returns true, the message is logged.
        /// If it returns false, nothing happens.
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

        // The conditional attribute means calls to Assert() are removed
        // by the compiler at the call site in RELEASE builds.
        [Conditional("DEBUG")]
        public void Assert(Func<bool> condition, object? message, string? category = null) { }
#endif

        // ------------------------------------------------------------
        // Flush / shutdown
        // ------------------------------------------------------------

        public void Flush()
        {
            // Wake writer and wait until all currently queued entries
            // have been processed.
            _signal.Set();

            while (!_queue.IsEmpty)
            {
                Thread.Yield();
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
                        // Never allow a sink failure to crash the game.
                    }
                }
            }
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
                        sink.Dispose();
                    }
                    catch
                    {
                        // Logging must never throw during shutdown.
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

        // ------------------------------------------------------------
        // Writer thread
        // ------------------------------------------------------------

        private void WriterLoop()
        {
            while (_running || !_queue.IsEmpty)
            {
                if (!_queue.TryDequeue(out LogEntry? entry))
                {
                    _signal.WaitOne(100);
                    continue;
                }

                WriteToSinks(entry);
            }

            // Make absolutely sure everything queued before shutdown
            // has been written.
            while (_queue.TryDequeue(out LogEntry? entry))
            {
                WriteToSinks(entry);
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
                }
            }
        }

        private void WriteToSinks(LogEntry entry)
        {
            lock (_sinkLock)
            {
                foreach (ILogSink sink in _sinks)
                {
                    try
                    {
                        sink.Write(entry);
                    }
                    catch
                    {
                        // A broken logging sink must never crash the game.
                    }
                }
            }
        }
    }
}
