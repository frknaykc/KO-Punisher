using System.Collections.Concurrent;
using KOPunisher;

internal static class SkillDispatcherTests
{
    public static async Task<int> RunAsync()
    {
        int passed = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        void Pass(string name) { passed++; Console.WriteLine("PASS " + name); }
        var io = new TraceInput();
        var dispatcher = new SkillDispatcher(io);
        var waits = new List<int>();
        Task Wait(int ms) { waits.Add(ms); return Task.CompletedTask; }
        Task Tap(SkillAddress address) => dispatcher.TapAsync(address, "", 230, 20, 30, () => { }, Wait, default);
        await Tap(new(2, 4));
        await Tap(new(2, 7));
        await Tap(new(8, 10));
        Check(io.Events.SequenceEqual(new[] { "D:F2", "U:F2", "D:4", "U:4", "D:7", "U:7", "D:F8", "U:F8", "D:0", "U:0" }),
            "Bar/slot sequence or same-bar cache failed");
        Check(waits.SequenceEqual(new[] { 20, 30, 230, 230, 20, 30, 230 }), "Bar timings changed skill hold time");
        Pass("Bar-aware dispatch ordering, F8/0, same-bar cache and independent holds");

        io.Events.Clear();
        io.Epoch++;
        await Tap(new(8, 10));
        dispatcher.Invalidate();
        await Tap(new(8, 10));
        Check(io.Events.Count(x => x == "D:F8") == 2, "Physical change or re-arm invalidation reused stale bar");
        Pass("Physical F change and explicit lifecycle invalidation force bar re-selection");

        io.Events.Clear();
        io.Tracking = false;
        await Tap(new(8, 10));
        await Tap(new(8, 10));
        Check(io.Events.Count(x => x == "D:F8") == 2, "Untracked input assumed a known active bar");
        io.Tracking = true;
        Pass("Inputs without a physical bar tracker never assume cached selection");

        io = new TraceInput();
        dispatcher = new SkillDispatcher(io);
        var down = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task BlockingWait(int ms)
        {
            if (ms == 230) { down.TrySetResult(); return release.Task; }
            return Task.CompletedTask;
        }
        Task attack = dispatcher.TapAsync(new(1, 4), "", 230, 20, 0, () => { }, BlockingWait, default);
        await down.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task minor = dispatcher.TapAsync(new(2, 4), "", 25, 20, 0,
            () => pending.TrySetResult(), _ => Task.CompletedTask, default);
        await pending.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Check(!io.Events.Contains("D:F2"), "Minor changed bar while attack key was held");
        release.TrySetResult();
        await Task.WhenAll(attack, minor).WaitAsync(TimeSpan.FromSeconds(2));
        Check(io.Events.SequenceEqual(new[] { "D:F1", "U:F1", "D:4", "U:4", "D:F2", "U:F2", "D:4", "U:4" }),
            "Concurrent lanes interleaved bar/slot holds");
        Pass("Concurrent attack and Minor on the same number in different bars remain atomic");

        foreach (string failure in new[] { "cancel", "focus", "bar-change", "send", "callback" })
        {
            io = new TraceInput();
            dispatcher = new SkillDispatcher(io);
            using var stop = new CancellationTokenSource();
            bool focused = true;
            if (failure == "send") io.FailDown = "F3";
            Task FailWait(int ms)
            {
                if (failure == "cancel") stop.Cancel();
                if (failure == "focus") focused = false;
                if (failure == "bar-change") io.Epoch++;
                return Task.CompletedTask;
            }
            bool failed = false;
            try
            {
                await dispatcher.TapAsync(new(3, 6), "", 50, 20, 10,
                    () => { if (!focused) throw new InvalidOperationException("focus"); }, FailWait, stop.Token,
                    _ => { if (failure == "callback") throw new InvalidOperationException("callback"); });
            }
            catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException) { failed = true; }
            Check(failed && !io.Events.Contains("D:6") && io.Events.Contains("U:F3") && io.Held.Count == 0,
                "Failure before skill did not abort and release bar: " + failure);
            io.FailDown = "";
            await dispatcher.TapAsync(new(3, 6), "", 50, 20, 10, () => { }, _ => Task.CompletedTask, default);
            Check(io.Events.Count(x => x == "D:F3") == 2, "Failure retained cache or leaked gate: " + failure);
            Pass("Bar selection failure closes skill path and releases key/gate: " + failure);
        }

        io = new TraceInput();
        dispatcher = new SkillDispatcher(io);
        down = new(TaskCreationOptions.RunContinuationsAsynchronously);
        release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        attack = dispatcher.TapAsync(new(1, 3), "", 230, 20, 0, () => { }, BlockingWait, default);
        await down.Task.WaitAsync(TimeSpan.FromSeconds(2));
        using (var cancel = new CancellationTokenSource())
        {
            minor = dispatcher.TapAsync(new(2, 4), "", 25, 20, 0, () => { }, _ => Task.CompletedTask, cancel.Token);
            cancel.Cancel();
            bool cancelled = false;
            try { await minor.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled && !io.Events.Contains("D:F2"), "Cancelled lane sent a key while waiting for gate");
        }
        release.TrySetResult();
        await attack.WaitAsync(TimeSpan.FromSeconds(2));
        Check(io.Held.Count == 0, "Cancellation leaked the other lane's held key");
        Pass("Queued lane cancellation sends nothing and does not release another lane's key");
        return passed;
    }

    private sealed class TraceInput : IMacroInput
    {
        public ConcurrentQueue<string> Events { get; } = new();
        public HashSet<string> Held { get; } = new();
        public bool Tracking = true;
        public long Epoch;
        public string FailDown = "";
        public bool CanTrackBarSelection => Tracking;
        public long BarSelectionEpoch => Epoch;
        public bool IsHeld(string key) => false;
        public bool IsTargetForeground(string processName) => true;
        public void KeyDown(string key)
        {
            Events.Enqueue("D:" + key);
            if (key == FailDown) throw new InvalidOperationException("send");
            lock (Held) Held.Add(key);
        }
        public void KeyUp(string key) { Events.Enqueue("U:" + key); lock (Held) Held.Remove(key); }
        public void ReleaseAll() { lock (Held) Held.Clear(); }
    }
}
