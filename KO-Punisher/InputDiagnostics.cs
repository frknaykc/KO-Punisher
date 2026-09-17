using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;

namespace KOPunisher;

// Yalnızca kullanıcı başlatınca kayıt. Hook üzerinde disk I/O veya bekleme yok.
public static class InputDiagnostics
{
    private static Session? _session;
    public static bool Active => Volatile.Read(ref _session)?.Accepting == true;
    public static string? CurrentPath => Volatile.Read(ref _session)?.Path;
    public static string? LastError => Volatile.Read(ref _session)?.Error;
    public static readonly IntPtr EventMarker = new(0x4B500000 | (Environment.ProcessId & 0xFFFF));

    public static string Start(string directory, int maxEvents = 20000)
    {
        if (_session != null) throw new InvalidOperationException("Önce mevcut tanılama kaydını bitirin.");
        if (maxEvents < 1) throw new ArgumentOutOfRangeException(nameof(maxEvents));
        Directory.CreateDirectory(directory);
        string path = System.IO.Path.Combine(directory, $"input-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.jsonl");
        var writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        var session = new Session(path, writer, maxEvents);
        _session = session;
        Record("session_start", new
        {
            schema = 1, build = "input-diagnostics-2", method = InputSender.Method.ToString(), module = typeof(InputSender).Module.ModuleVersionId,
            os = Environment.OSVersion.VersionString, architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            pid = Environment.ProcessId, admin = InputSender.IsRunningAsAdmin(), maxEvents,
            inputMarker = EventMarker.ToInt64(),
            privacy = "Only configured trigger transitions, generated keys and foreground process metadata. No text, window titles, screenshots or account settings.",
            interpretation = "SendInput success means Windows accepted events, NOT game consumption. Hook observation is NOT a game acknowledgement. UIPI cannot be identified by GetLastError alone."
        });
        return path;
    }

    public static void Record(string kind, object? data = null) => Volatile.Read(ref _session)?.Enqueue(kind, data);

    public static async Task StopAsync()
    {
        var session = Interlocked.Exchange(ref _session, null);
        if (session == null) return;
        session.Complete();
        await session.WriterTask.ConfigureAwait(false);
        if (session.Error != null) throw new IOException("Tanılama kaydı tamamlanamadı: " + session.Error);
    }

    private sealed class Session
    {
        private readonly Channel<object> _queue = Channel.CreateBounded<object>(new BoundedChannelOptions(4096)
        { SingleReader = true, FullMode = BoundedChannelFullMode.Wait, AllowSynchronousContinuations = false });
        private readonly StreamWriter _writer;
        private readonly int _maxEvents;
        private int _attempted, _dropped, _complete;
        private string? _error;
        private readonly long _started = Stopwatch.GetTimestamp();
        public string Path { get; }
        public bool Accepting => Volatile.Read(ref _complete) == 0;
        public string? Error => Volatile.Read(ref _error);
        public Task WriterTask { get; }

        public Session(string path, StreamWriter writer, int maxEvents)
        {
            Path = path; _writer = writer; _maxEvents = maxEvents;
            WriterTask = Task.Run(WriteAsync);
        }

        public void Enqueue(string kind, object? data)
        {
            if (!Accepting) return;
            int sequence = Interlocked.Increment(ref _attempted);
            if (sequence > _maxEvents) { Complete(); return; }
            var item = new { utc = DateTime.UtcNow, elapsedMs = Stopwatch.GetElapsedTime(_started).TotalMilliseconds, sequence, kind, data };
            if (!_queue.Writer.TryWrite(item)) Interlocked.Increment(ref _dropped);
        }

        public void Complete()
        {
            Interlocked.Exchange(ref _complete, 1);
            _queue.Writer.TryComplete();
        }

        private async Task WriteAsync()
        {
            try
            {
                await foreach (object item in _queue.Reader.ReadAllAsync())
                    await _writer.WriteLineAsync(JsonSerializer.Serialize(item));
                await _writer.WriteLineAsync(JsonSerializer.Serialize(new
                {
                    utc = DateTime.UtcNow, kind = "session_end", attempted = _attempted,
                    dropped = _dropped, eventLimitReached = _attempted > _maxEvents,
                    interpretation = "No conclusion about game rejection can be inferred from this log alone."
                }));
            }
            catch (Exception ex) { Volatile.Write(ref _error, ex.GetType().Name); Complete(); }
            finally
            {
                try { _writer.Dispose(); }
                catch (Exception ex) { Volatile.Write(ref _error, ex.GetType().Name); }
            }
        }
    }
}
