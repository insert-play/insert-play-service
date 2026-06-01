using Serilog.Core;
using Serilog.Events;

namespace InsertPlay.Service.Windows;

/// <summary>
/// Thread-safe Serilog sink that buffers the most recent log entries in memory
/// so the <see cref="LogViewerForm"/> can display them on demand.
/// </summary>
internal sealed class InMemoryLogSink : ILogEventSink
{
    public static readonly InMemoryLogSink Instance = new();

    private const int MaxEntries = 1000;

    private readonly Queue<LogEntry> _entries = new(MaxEntries + 1);
    private readonly object _lock = new();

    public event EventHandler<LogEntry>? EntryAdded;

    private InMemoryLogSink() { }

    public void Emit(LogEvent logEvent)
    {
        var writer = new StringWriter();
        logEvent.RenderMessage(writer);

        var entry = new LogEntry(
            logEvent.Timestamp.LocalDateTime,
            logEvent.Level,
            writer.ToString(),
            logEvent.Exception?.ToString());

        lock (_lock)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > MaxEntries)
                _entries.Dequeue();
        }

        EntryAdded?.Invoke(this, entry);
    }

    public IReadOnlyList<LogEntry> GetSnapshot()
    {
        lock (_lock)
            return [.. _entries];
    }
}

internal sealed record LogEntry(DateTime Timestamp, LogEventLevel Level, string Message, string? Exception);
