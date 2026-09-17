using System.Collections.Concurrent;
using KOPunisher;

internal static class FarmTests
{
    public static async Task<int> RunAsync()
    {
        int passed = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); }
        void Pass(string name) { passed++; Console.WriteLine("PASS " + name); }
        async Task Until(Func<bool> predicate)
        {
            using var timeout = new CancellationTokenSource(3000);
            while (!predicate()) await Task.Delay(5, timeout.Token);
        }
        Settings MakeSettings() => new()
        {
            ComboPreset = "r", ComboSpeedMs = 50, EnableJitter = false,
            Skills = new Dictionary<string, string> { ["Spike"] = "3" }, FillerOrder = [], SkillCooldowns = new(),
            Farm = new FarmSettings { Monster = new MonsterFilterSettings
            {
                Enabled = true, Names = ["Apostle"], Region = new UiRegion { W = 120, H = 30 }
            }}
        };
        var filter = MakeSettings().Farm.Monster;
        Check(filter.Allows("  aPoStLe  ") && !filter.Allows("Deruvish") && !filter.Allows("") &&
            !filter.Allows("Apostle Guard") && !filter.Allows("Apostle\nGuard"), "Exact fail-closed monster matching");
        Pass("Mob filtresi: tam isim, boş/başka/kısmi isim reddi");

        var buffs = new List<BuffSetting> { new() { Enabled = true, Name = "Wolf", Key = "8", IntervalSeconds = 10 } };
        var scheduler = new BuffScheduler(buffs);
        Check(scheduler.NextDue(100) == 0, "Initial buff due");
        scheduler.MarkSent(0, 100);
        Check(scheduler.NextDue(10099) == null && scheduler.NextDue(10100) == 0, "Buff cooldown boundary");
        Pass("Buff zamanlayıcı: ilk kullanım ve tekrar sınırı");

        var s = MakeSettings();
        s.EnsureDefaultProfiles();
        s.Farm.Buffs[0].Enabled = true; s.Farm.Buffs[0].Key = "8";
        s.Farm.Market.Messages = ["Selling item"];
        s.StoreJobDraft(); s.SwitchJob(ClassType.Archer);
        Check(!s.Farm.Buffs[0].Enabled && !s.Farm.Monster.Enabled, "Farm settings leaked to another job");
        s.Farm.Market.Messages = ["Buying item"];
        s.SwitchJob(ClassType.Assassin);
        Check(s.Farm.Buffs[0].Enabled && s.Farm.Monster.Enabled && s.Farm.Market.Messages[0] == "Selling item", "Job restoration");
        string path = Path.Combine(Path.GetTempPath(), "ko-farm-" + Guid.NewGuid() + ".json");
        try
        {
            s.Save(path);
            var loaded = Settings.Load(path);
            Check(loaded.Farm.Buffs[0].Key == "8" && loaded.Farm.Monster.Names[0] == "Apostle", "Farm persistence");
        }
        finally { File.Delete(path); }
        Pass("Farm ayarları: profil/job ayrımı ve disk roundtrip");

        foreach (Action<Settings> change in new Action<Settings>[]
        {
            a => a.Farm.Monster.Names = [],
            a => a.Farm.Monster.Region = new(),
            a => { a.Farm.Buffs[0].Enabled = true; a.Farm.Buffs[0].Key = a.ComboTrigger; },
            a => { a.Farm.Buffs[0].Enabled = true; a.Farm.Buffs[0].Key = "3"; },
            a => { a.Farm.Buffs[0].Enabled = true; a.Farm.Buffs[0].Key = "8"; a.Farm.Buffs[0].IntervalSeconds = 0; }
        })
        {
            var bad = MakeSettings(); change(bad);
            bool rejected = false;
            try { bad.Validate(); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Invalid farm settings accepted");
        }
        Pass("Farm doğrulama: eksik OCR bölgesi/isim, buff tuş çakışması ve süre");

        foreach (string text in new[] { "", "Deruvish", "Apostle Guard" })
        {
            var io = new FarmInput(); var reader = new FakeMonster { Text = text };
            var engine = new MacroEngine(MakeSettings(), io, reader);
            await engine.RunOnceAsync();
            Check(io.Events.IsEmpty && engine.Error != null, "Rejected target attacked in single cycle");
        }
        {
            var io = new FarmInput(); var engine = new MacroEngine(MakeSettings(), io, new FakeMonster { Text = "Apostle" });
            await engine.RunOnceAsync();
            Check(io.Count("R") == 1 && io.Down.IsEmpty && engine.Error == null, "Allowed target not attacked");
        }
        Pass("Gerçek motor: tek turda mob filtresi saldırı kapısı");

        foreach (bool error in new[] { false, true })
        {
            var io = new FarmInput(); var reader = new FakeMonster { Text = "Apostle", Failure = error };
            if (!error) reader.BeforeRead = () => io.Focused = false;
            var engine = new MacroEngine(MakeSettings(), io, reader);
            await engine.RunOnceAsync();
            Check(io.Events.IsEmpty && engine.Error != null, "OCR failure/focus change sent input");
        }
        Pass("OCR hata/odak yarışı: saldırı yok");

        {
            s = MakeSettings(); s.ZKey = "Z"; s.ZAttackKey = "9"; s.ZInterval = 500;
            var io = new FarmInput(); var reader = new FakeMonster { Text = "Deruvish" };
            var engine = new MacroEngine(s, io, reader);
            engine.Arm(); await Task.Delay(30); io.Held[s.ComboTrigger] = true;
            await Until(() => io.Count("Z") >= 2);
            Check(io.Count("Z") > 0 && io.Count("9") == 0 && io.Count("R") == 0, "ZAttack bypassed filter");
            reader.Text = "Apostle";
            await Until(() => io.Count("R") > 0);
            io.Focused = false;
            await Task.Delay(100); int count = io.Events.Count;
            io.Focused = true; await Task.Delay(100);
            Check(io.Events.Count == count, "Focus return resumed automatically");
            await engine.StopAsync();
            Check(io.Down.IsEmpty, "Stuck farm keys");
        }
        Pass("Farm döngüsü: Z serbest, saldırı filtreli, odak sonrası yeni tetik");

        {
            s = MakeSettings(); s.Farm.Monster.Enabled = false;
            s.Farm.Buffs[0] = new() { Enabled = true, Name = "Wolf", Key = "8", IntervalSeconds = 10, CastWaitMs = 20 };
            var io = new FarmInput(); var engine = new MacroEngine(s, io);
            engine.Arm(); await Task.Delay(30);
            Check(io.Events.IsEmpty, "Buff fired before trigger");
            io.Held[s.ComboTrigger] = true;
            await Until(() => io.Count("R") >= 2);
            Check(io.Count("8") == 1 && io.Events.First().Key == "8", "Buff ordering/repeat");
            await engine.StopAsync(); Check(io.Down.IsEmpty, "Buff cancellation cleanup");
        }
        Pass("Gerçek motor: buff tetikle başlar, saldırıdan önce, her comboda tekrarlanmaz");

        foreach (Action<MarketSettings> change in new Action<MarketSettings>[]
        {
            m => m.Messages = [], m => m.Messages = ["x\ny"], m => m.Messages = [new string('x', 161)],
            m => m.IntervalSeconds = 0, m => m.RepeatCount = 0
        })
        {
            var market = new MarketSettings { Messages = ["Selling"] }; change(market);
            bool rejected = false;
            try { market.Validate(); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Invalid market settings accepted");
        }
        Pass("Pazar: boş/çok satırlı/uzun mesaj, aralık ve tekrar doğrulama");

        {
            var io = new FarmInput(); var encoder = new FakeEncoder();
            var runner = new MarketRunner(new MarketSettings { Messages = ["ab"], RepeatCount = 1 }, io, encoder, "KnightOnLine", "F12");
            await runner.RunAsync(CancellationToken.None);
            Check(io.Events.Where(e => e.Down).Select(e => e.Key).SequenceEqual(new[] { "ENTER", "A", "B", "ENTER" }), "Market sequence");
            Check(runner.SentCount == 1 && io.Down.IsEmpty, "Market count/cleanup");
        }
        Pass("Pazar: Enter–metin–Enter, tekrar sınırı ve bırakma");

        foreach (string fault in new[] { "focus", "emergency", "layout", "cancel", "input", "modifier" })
        {
            var io = new FarmInput(); var encoder = new FakeEncoder();
            using var stop = new CancellationTokenSource();
            io.AfterDown = key =>
            {
                if (key != "A") return;
                if (fault == "focus") io.Focused = false;
                if (fault == "modifier") io.Held["CTRL"] = true;
                if (fault == "emergency") io.Held["F12"] = true;
                if (fault == "layout") encoder.Current = false;
                if (fault == "cancel") stop.Cancel();
                if (fault == "input") throw new InvalidOperationException("injected input failure");
            };
            var runner = new MarketRunner(new MarketSettings { Messages = ["ab"], RepeatCount = 1 }, io, encoder, "KnightOnLine", "F12");
            bool failed = false;
            try { await runner.RunAsync(stop.Token); } catch (Exception) { failed = true; }
            Check(failed && io.Count("ENTER") == 1 && io.Count("B") == 0 && io.Down.IsEmpty && runner.SentCount == 0,
                "Interrupted market submitted text / left key held: " + fault);
        }
        Pass("Pazar: odak/acil/layout/iptal/input hatasında gönderme yok, tuşlar bırakılır");
        {
            s = MakeSettings(); s.ComboPreset = "rr";
            var io = new FarmInput(); var reader = new FakeMonster { Text = "Apostle" };
            io.AfterDown = _ => reader.Text = "Deruvish";
            var engine = new MacroEngine(s, io, reader);
            await engine.RunOnceAsync();
            Check(io.Count("R") == 1 && engine.Error != null && io.Down.IsEmpty, "Target changed inside combo but second attack sent");
        }
        Pass("Mob filtresi: aynı combo içinde hedef değişirse sonraki saldırı engellenir");

        {
            s = MakeSettings(); s.Farm.Monster.Enabled = false;
            s.Farm.Buffs[0] = new() { Enabled = true, Name = "Wolf", Key = "8", IntervalSeconds = 10, CastWaitMs = 20 };
            var io = new FarmInput(); var engine = new MacroEngine(s, io);
            io.AfterDown = key => { if (key == "8") io.Held[s.ComboTrigger] = false; };
            engine.Arm(); await Task.Delay(30); io.Held[s.ComboTrigger] = true;
            await Until(() => io.Count("8") == 1);
            await Task.Delay(50); io.Held[s.ComboTrigger] = true;
            await Until(() => io.Count("R") > 0);
            await engine.StopAsync();
            Check(io.Count("8") == 1 && io.Down.IsEmpty, "Interrupted buff was immediately repeated");
        }
        Pass("Buff: basış sırasında bırakıp yeniden tetiklemek buff cooldown'unu sıfırlamaz");

        foreach (bool encodingError in new[] { false, true })
        {
            var io = new FarmInput { Focused = encodingError };
            var encoder = new FakeEncoder { Failure = encodingError };
            var runner = new MarketRunner(new() { Messages = ["ab"], RepeatCount = 1 }, io, encoder, "KnightOnLine", "F12");
            bool failed = false;
            try { await runner.RunAsync(CancellationToken.None); } catch (Exception) { failed = true; }
            Check(failed && io.Events.IsEmpty, "Focus/unsupported character was not rejected before opening chat");
        }
        Pass("Pazar: odak yok/desteklenmeyen karakter, sohbeti açmadan ret");

        {
            var io = new FarmInput();
            var runner = new MarketRunner(new() { Messages = ["a", "b"], RepeatCount = 3, IntervalSeconds = 10 }, io, new FakeEncoder(), "KnightOnLine", "F12");
            using var stop = new CancellationTokenSource();
            long firstDone = 0;
            var task = runner.RunAsync(stop.Token);
            await Until(() => runner.SentCount == 1);
            firstDone = Environment.TickCount64;
            await Task.Delay(100);
            Check(io.Count("ENTER") == 2 && io.Count("B") == 0, "Market ignored interval");
            using var deadline = new CancellationTokenSource(12000);
            while (runner.SentCount < 2) await Task.Delay(5, deadline.Token);
            long elapsed = Environment.TickCount64 - firstDone;
            stop.Cancel();
            try { await task; } catch (OperationCanceledException) { }
            Check(elapsed >= 9900 && io.Count("A") == 1 && io.Count("B") == 1 && io.Count("ENTER") == 4 && io.Down.IsEmpty,
                "Market list rotation/interval/cancel between messages");
        }
        Pass("Pazar: gerçek 10 saniye aralık, liste sırası ve mesajlar arasında iptal");

        string logs = Path.Combine(Path.GetTempPath(), "ko-market-private-" + Guid.NewGuid());
        try
        {
            InputDiagnostics.Start(logs);
            var io = new FarmInput();
            var runner = new MarketRunner(new() { Messages = ["ab"], RepeatCount = 1 }, io, new FakeEncoder(), "KnightOnLine", "F12");
            bool failed = false;
            try { await runner.RunAsync(CancellationToken.None); } catch (InvalidOperationException) { failed = true; }
            Check(failed && io.Events.IsEmpty, "Market typed while diagnostics were recording");
        }
        finally { await InputDiagnostics.StopAsync(); if (Directory.Exists(logs)) Directory.Delete(logs, true); }
        Pass("Pazar: tanılama kaydı açıkken mesaj tuşları gönderilmez");
        return passed;
    }

    private sealed class FakeMonster : IMonsterReader
    {
        public volatile string Text = "";
        public volatile bool Failure;
        public int Reads;
        public Action? BeforeRead;
        public Task<string> ReadAsync(CancellationToken token)
        {
            Interlocked.Increment(ref Reads); BeforeRead?.Invoke();
            return Failure ? Task.FromException<string>(new InvalidOperationException("OCR failed")) : Task.FromResult(Text);
        }
    }
    private sealed class FakeEncoder : IChatEncoder
    {
        public bool Current = true;
        public bool Failure;
        public bool IsCurrent => Current;
        public IReadOnlyList<ChatStroke> Encode(string text) => Failure ? throw new ArgumentException("unsupported text") : text.Select(c => new ChatStroke(c.ToString().ToUpperInvariant(), false, false, false)).ToArray();
    }
    private sealed class FarmInput : IMacroInput
    {
        public readonly ConcurrentDictionary<string, bool> Held = new(), Down = new();
        public readonly ConcurrentQueue<(string Key, bool Down)> Events = new();
        public volatile bool Focused = true;
        public Action<string>? AfterDown;
        public bool IsHeld(string key) => Held.GetValueOrDefault(key);
        public bool IsTargetForeground(string processName) => Focused;
        public int Count(string key) => Events.Count(e => e.Down && e.Key == key);
        public void KeyDown(string key) { Down[key] = true; Events.Enqueue((key, true)); AfterDown?.Invoke(key); }
        public void KeyUp(string key) { if (Down.TryRemove(key, out _)) Events.Enqueue((key, false)); }
        public void ReleaseAll() { foreach (string key in Down.Keys) KeyUp(key); }
    }
}
