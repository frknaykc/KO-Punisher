using System.Text.Json;

namespace KOPunisher;

/// <summary>
/// Başlat yalnızca hazırlar. İki bağımsız döngü fiziksel tetik basılıyken (Hold)
/// veya tetik kenarı ile kilitlenince (Toggle) çalışır.
/// Beklemeler iptal edilebilir; UI thread'i ve Minor combo tarafından bloklanmaz.
/// </summary>
public sealed class MacroEngine
{
    private readonly Settings _settings;
    private readonly IMacroInput _input;
    private readonly SkillRotation _rotation;
    private readonly SkillDispatcher _dispatcher;
    private readonly IMonsterReader? _monsterReader;
    private readonly IResourceReader? _resourceReader;
    private readonly BuffScheduler _buffs;
    private string _monsterStatus = "";
    private string _resourceStatus = "";
    public string MonsterStatus => Volatile.Read(ref _monsterStatus);
    public string ResourceStatus => Volatile.Read(ref _resourceStatus);
    private CancellationTokenSource? _stop;
    private Task _completion = Task.CompletedTask;
    private volatile bool _armed, _comboActive, _minorActive;
    private string? _error;
    private int _focusEpoch;
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _testKeys = new();
    public string[] TestKeys => _testKeys.ToArray();
    public bool FocusPaused => IsArmed && !TargetAvailable();

    private bool TargetAvailable()
    {
        bool available = _input.IsTargetForeground(_settings.TargetProcess);
        if (!available) { Interlocked.Increment(ref _focusEpoch); _dispatcher.Invalidate(); }
        return available;
    }

    public Task RunOnceAsync(CancellationToken token = default)
    {
        if (!_completion.IsCompleted) throw new InvalidOperationException("Önce makroyu durdurun.");
        _stop?.Dispose();
        _stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        _armed = true;
        _error = null;
        _testKeys.Clear();
        _dispatcher.Invalidate();
        return _completion = RunSingleAsync(_stop.Token);
    }

    private async Task RunSingleAsync(CancellationToken token)
    {
        var state = new LaneState { Single = true, Epoch = Volatile.Read(ref _focusEpoch) };
        try
        {
            CheckLane(_settings.ComboTrigger, state, token);
            _comboActive = true;
            InputDiagnostics.Record("combo_probe_begin", new { _settings.ComboPreset });
            await MaybeZAsync(_settings.ComboTrigger, state, Environment.TickCount64, token);
            await ComboLoopAsync(_settings.ComboTrigger, state, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { _error = "Tek tur iptal edildi."; }
        catch (HoldReleasedException) { _error = "Oyun odağı kayboldu; tek tur iptal edildi."; }
        catch (Exception ex) { _error = ex.Message; }
        finally
        {
            try { _input.ReleaseAll(); }
            catch (Exception ex) { _error ??= ex.Message; }
            _comboActive = _minorActive = _armed = false;
            InputDiagnostics.Record("combo_probe_end", new { error = Error });
        }
    }

    public bool IsArmed => _armed;
    public bool ComboActive => _comboActive;
    public bool MinorActive => _minorActive;
    public string? Error => Volatile.Read(ref _error);
    public double RemainingSpikeSec => _rotation.SpikeRemaining(Environment.TickCount64) / 1000.0;
    public Task Completion => _completion;

    public MacroEngine(Settings settings, IMacroInput input, IMonsterReader? monsterReader = null, IResourceReader? resourceReader = null)
    {
        _settings = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(settings))!;
        SkillCalibration.Apply(_settings, requireComplete: true);
        _settings.LightFeetKey = string.IsNullOrEmpty(settings.LightFeetKey) ? "" : _settings.LightFeetKey;
        _settings.MinorPedalKey = string.IsNullOrEmpty(settings.MinorPedalKey) ? "" : _settings.MinorPedalKey;
        _settings.Validate();
        _input = input;
        _dispatcher = new SkillDispatcher(input);
        _rotation = new SkillRotation(_settings);
        _monsterReader = monsterReader;
        _resourceReader = resourceReader;
#if WINDOWS
        _resourceReader ??= _settings.HealthMana.AnyEnabled
            ? new WindowResourceReader(_settings.TargetProcess, _settings.HealthMana) : null;
#endif
        _buffs = new BuffScheduler(_settings.Farm.Buffs);
        if (_settings.Farm.Monster.Enabled && _monsterReader == null)
            throw new ArgumentException("Mob filtresi için OCR okuyucusu gerekli.");
    }

    public void Arm()
    {
        if (!_completion.IsCompleted) return;
        _stop?.Dispose();
        _stop = new CancellationTokenSource();
        _error = null;
        _dispatcher.Invalidate();
        _armed = true;
        InputDiagnostics.Record("engine_armed", new { _settings.ComboTrigger, _settings.MinorTrigger, runMode = _settings.RunMode.ToString(), _settings.ComboPreset, _settings.ComboKey1, _settings.ComboKey2, _settings.ComboKey3, _settings.MinorPedalKey, _settings.ComboSpeedMs, _settings.Timings });
        CancellationToken token = _stop.Token;
        _completion = RunAsync(token);
    }

    public async Task StopAsync()
    {
        _stop?.Cancel();
        await _completion.ConfigureAwait(false);
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            await Task.WhenAll(
                RunLaneAsync(minor: false, token),
                RunLaneAsync(minor: true, token),
                RunPotionLaneAsync(token)).ConfigureAwait(false);
        }
        finally
        {
            try { _input.ReleaseAll(); }
            catch (Exception ex) { Interlocked.CompareExchange(ref _error, ex.Message, null); }
            _comboActive = _minorActive = _armed = false;
            InputDiagnostics.Record("engine_stopped", new { error = Error });
        }
    }

    private bool Held(string trigger) => _input.IsHeld(trigger);

    private int JitterRange => _settings.EnableJitter ? Math.Max(0, _settings.JitterRange) : 0;

    private void CheckEmergency(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (_input.IsHeld(_settings.Hotkeys.EmergencyStop))
        {
            _stop!.Cancel();
            token.ThrowIfCancellationRequested();
        }
    }

    private async Task RunLaneAsync(bool minor, CancellationToken token)
    {
        string trigger = minor ? _settings.MinorTrigger : _settings.ComboTrigger;
        var state = new LaneState { Toggle = _settings.RunMode == RunMode.Toggle };

        try
        {
            while (true)
            {
                CheckEmergency(token);
                bool down = Held(trigger);
                if (!TargetAvailable() || state.Epoch != Volatile.Read(ref _focusEpoch))
                {
                    state.Released = state.Latched = false;
                    state.PrevDown = down;
                    state.Epoch = Volatile.Read(ref _focusEpoch);
                    await Task.Delay(10, token).ConfigureAwait(false);
                    continue;
                }
                if (!down) state.Released = true;

                if (state.Toggle)
                {
                    if (state.Released && down && !state.PrevDown)
                        state.Latched = !state.Latched;
                    state.PrevDown = down;
                    if (!state.Latched || (minor && _settings.MinorPedalKey.Length == 0))
                    {
                        await Task.Delay(10, token).ConfigureAwait(false);
                        continue;
                    }
                }
                else if (!down || !state.Released || (minor && _settings.MinorPedalKey.Length == 0))
                {
                    await Task.Delay(10, token).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    if (minor) _minorActive = true; else _comboActive = true;
                    InputDiagnostics.Record("lane_active", new { minor, trigger });
                    long nextZ = Environment.TickCount64;

                    while (true)
                    {
                        CheckLane(trigger, state, token);

                        if (minor)
                            await MinorLoopAsync(trigger, state, token);
                        else
                        {
                            await MaybeBuffAsync(trigger, state, token);
                            try
                            {
                                nextZ = await MaybeZAsync(trigger, state, nextZ, token);
                                await ComboLoopAsync(trigger, state, token);
                            }
                            catch (MonsterRejectedException)
                            {
                                await WaitHeldAsync(100, trigger, state, token);
                            }
                        }
                    }
                }
                catch (HoldReleasedException)
                {
                    state.Released = false;
                    state.Latched = false;
                }
                finally
                {
                    if (minor) _minorActive = false; else _comboActive = false;
                    InputDiagnostics.Record("lane_inactive", new { minor, trigger });
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Interlocked.CompareExchange(ref _error, ex.Message, null);
            InputDiagnostics.Record("engine_error", new { minor, error = ex.Message, type = ex.GetType().Name });
            _stop!.Cancel();
        }
    }

    private void CheckLane(string trigger, LaneState state, CancellationToken token)
    {
        CheckEmergency(token);
        if (!TargetAvailable() || state.Epoch != Volatile.Read(ref _focusEpoch))
            throw new HoldReleasedException();
        if (state.Single) return;
        bool down = Held(trigger);
        if (state.Toggle)
        {
            if (!down) state.Released = true;
            if (state.Released && down && !state.PrevDown)
                state.Latched = !state.Latched;
            state.PrevDown = down;
            if (!state.Latched) throw new HoldReleasedException();
        }
        else if (!down)
        {
            throw new HoldReleasedException();
        }
    }

    private async Task<long> MaybeZAsync(string trigger, LaneState state, long nextZ, CancellationToken token)
    {
        string zKey = _settings.ZKey;
        if (zKey.Length == 0) return nextZ;
        if (Environment.TickCount64 < nextZ) return nextZ;
        await TapAsync(zKey, 30, trigger, state, token, attack: false);
        if (_settings.Farm.Monster.Enabled)
            await WaitHeldAsync(_settings.Farm.Monster.SettleMs, trigger, state, token);
        if (_settings.ZAttackKey.Length > 0)
        {
            await WaitHeldAsync(InputSender.ApplyJitter(20, JitterRange), trigger, state, token);
            try { await TapAsync(_settings.ZAttackKey, 30, trigger, state, token); }
            catch (MonsterRejectedException) when (!state.Single) { /* Sonraki Z aralığını yine de ilerlet. */ }
        }
        await WaitHeldAsync(InputSender.ApplyJitter(20, JitterRange), trigger, state, token);
        int interval = _settings.ZInterval > 0 ? _settings.ZInterval : 3000;
        return Environment.TickCount64 + interval;
    }

    private async Task ComboLoopAsync(string trigger, LaneState state, CancellationToken token)
    {
        if (_settings.InsertTrigger.Length > 0 && _settings.InsertKey.Length > 0 && Held(_settings.InsertTrigger))
            await TapAsync(_settings.InsertKey, _settings.Timings.SkillKeyHold, trigger, state, token);

        if (_settings.LightFeetKey.Length > 0)
            await TapAsync(_settings.LightFeetKey, _settings.Timings.SkillKeyHold, trigger, state, token,
                _settings.SkillLayout == null ? null : "LightFeet", attack: false);

        if (_settings.SkillLayout != null && await CalibratedComboAsync(trigger, state, token)) return;

        if (ComboCatalog.IsSlide(_settings.ComboPreset))
        {
            // Referans zamanlaması: jitter ve genel adım arası uygulanmaz.
            foreach (var (key, hold, after) in ComboCatalog.SlideSteps(_settings))
            {
                await TapAsync(key, hold, trigger, state, token);
                await WaitHeldAsync(after, trigger, state, token);
            }
            return;
        }

        var seq = ComboSteps();
        if (seq.Count > 0)
        {
            int gap = _settings.Timings.SkillToWDelay;
            foreach (var (key, hold) in seq)
            {
                await TapAsync(key, hold, trigger, state, token);
                await WaitHeldAsync(InputSender.ApplyJitter(gap, JitterRange), trigger, state, token);
            }
            await WaitHeldAsync(InputSender.ApplyJitter(_settings.ComboSpeedMs, JitterRange), trigger, state, token);
            return;
        }

        if (_settings.ComboPreset is "rr" or "r" or "staff-r" or "bp-rr")
        {
            if (_settings.ComboKey1.Length > 0)
            {
                await TapAsync(_settings.ComboKey1, _settings.Timings.SkillKeyHold, trigger, state, token);
                await WaitHeldAsync(_settings.Timings.SkillToWDelay, trigger, state, token);
            }
            await TapAsync("R", _settings.Timings.RKeyHold, trigger, state, token);
            if (_settings.ComboPreset is "rr" or "bp-rr")
            {
                await WaitHeldAsync(_settings.Timings.WToRDelay, trigger, state, token);
                await TapAsync("R", _settings.Timings.RKeyHold, trigger, state, token);
            }
            await WaitHeldAsync(InputSender.ApplyJitter(_settings.ComboSpeedMs, JitterRange), trigger, state, token);
            return;
        }

        string? skill = _rotation.Next(Environment.TickCount64);
        SkillRSkillMode mode = _settings.ComboPreset switch
        {
            "assassin-skill-r" or "assassin-rr" => SkillRSkillMode.SkillR,
            "assassin-r-skill" => SkillRSkillMode.RSkill,
            _ => _settings.SkillRSkillMode
        };
        string slide = string.IsNullOrEmpty(_settings.SlideKey) ? "W" : _settings.SlideKey;

        switch (mode)
        {
            case SkillRSkillMode.SkillR:
                if (skill != null)
                {
                    await TapSkillAsync(skill, trigger, state, token);
                    await WaitHeldAsync(InputSender.ApplyJitter(_settings.Timings.SkillToWDelay, JitterRange), trigger, state, token);
                }
                await TapAsync("R", _settings.Timings.RKeyHold, trigger, state, token);
                break;

            case SkillRSkillMode.RSkill:
                await TapAsync("R", _settings.Timings.RKeyHold, trigger, state, token);
                if (skill != null)
                {
                    await WaitHeldAsync(InputSender.ApplyJitter(_settings.Timings.WToRDelay, JitterRange), trigger, state, token);
                    await TapSkillAsync(skill, trigger, state, token);
                }
                break;

            default:
                if (skill != null)
                {
                    await TapSkillAsync(skill, trigger, state, token);
                    await WaitHeldAsync(InputSender.ApplyJitter(_settings.Timings.SkillToWDelay, JitterRange), trigger, state, token);
                }
                await TapAsync(slide, _settings.Timings.WKeyHold, trigger, state, token);
                await WaitHeldAsync(InputSender.ApplyJitter(_settings.Timings.WToRDelay, JitterRange), trigger, state, token);
                await TapAsync("R", _settings.Timings.RKeyHold, trigger, state, token);
                break;
        }

        if (_settings.ComboPreset == "assassin-rr")
        {
            await WaitHeldAsync(_settings.Timings.WToRDelay, trigger, state, token);
            await TapAsync("R", _settings.Timings.RKeyHold, trigger, state, token);
        }
        await WaitHeldAsync(InputSender.ApplyJitter(_settings.Timings.NextSkillDelay, JitterRange), trigger, state, token);
    }

    private async Task<bool> CalibratedComboAsync(string trigger, LaneState state, CancellationToken token)
    {
        string preset = _settings.ComboPreset;
        if (preset is "rotation" or "assassin-rr" or "assassin-skill-r" or "assassin-r-skill") return false;
        string[] ids = SkillCalibration.ComboSkills(_settings);
        var t = _settings.Timings;
        async Task Skill(string id, int hold) => await TapAsync(
            _settings.SkillLayout!.Resolve(id)!.Key, hold, trigger, state, token, id);
        if (ComboCatalog.IsSlide(preset))
        {
            bool threeFirst = preset == ComboCatalog.ThreeSlideFiveSlide;
            foreach (string id in threeFirst ? ids.Take(2) : ids.Take(2).Reverse())
            {
                await Skill(id, t.ArcherSkillHold);
                await WaitHeldAsync(t.ArcherSkillAfter, trigger, state, token);
                await TapAsync(_settings.SlideKey, t.ArcherSlideHold, trigger, state, token);
                bool last = id == (threeFirst ? ids[1] : ids[0]);
                await WaitHeldAsync(last ? t.ArcherCycleAfter : t.ArcherSlideAfter, trigger, state, token);
            }
        }
        else
        {
            int count = preset == "70-72-60" ? 3 : preset is "3-5" or "5-3" or "70-72" or "70-60" ? 2 : 1;
            var sequence = ids.Take(count);
            if (preset == "5-3") sequence = sequence.Reverse();
            foreach (string id in sequence)
            {
                await Skill(id, t.SkillKeyHold);
                await WaitHeldAsync(t.SkillToWDelay, trigger, state, token);
            }
            if (preset is "r" or "rr" or "staff-r" or "bp-rr")
            {
                await TapAsync("R", t.RKeyHold, trigger, state, token);
                if (preset is "rr" or "bp-rr")
                {
                    await WaitHeldAsync(t.WToRDelay, trigger, state, token);
                    await TapAsync("R", t.RKeyHold, trigger, state, token);
                }
            }
            await WaitHeldAsync(_settings.ComboSpeedMs, trigger, state, token);
        }
        return true;
    }

    private async Task MinorLoopAsync(string trigger, LaneState state, CancellationToken token)
    {
        await TapAsync(_settings.MinorPedalKey, _settings.MinorHoldMs, trigger, state, token,
            _settings.SkillLayout == null ? null : "MinorHealing", attack: false);

        bool forceHp = _settings.ForceHpTrigger.Length > 0 && Held(_settings.ForceHpTrigger);
        if (!_settings.HealthMana.Hp.Enabled && _settings.MinorPotKey.Length > 0)
            await TapAsync(_settings.MinorPotKey, 30, trigger, state, token, attack: false);

        if (!forceHp && !_settings.HealthMana.Mp.Enabled && _settings.MinorManaKey.Length > 0)
            await TapAsync(_settings.MinorManaKey, 30, trigger, state, token, attack: false);

        int rest = Math.Max(0, _settings.MinorRepeatMs - _settings.MinorHoldMs);
        await WaitHeldAsync(InputSender.ApplyJitter(rest, JitterRange), trigger, state, token);
    }

    private List<(string Key, int Hold)> ComboSteps()
    {
        string p = _settings.ComboPreset ?? "rotation";
        int sk = _settings.Timings.SkillKeyHold;
        if (p is ComboCatalog.FiveSlideThreeSlide or "3-5-w-3-5-w" or ComboCatalog.ThreeSlideFiveSlide)
        {
            return ComboCatalog.SlideSteps(_settings).Select(step => (step.Key, step.Hold)).ToList();
        }
        return p switch
        {
            "5-3" or "70-72" or "70-60" or "3-5" => NonEmpty(_settings.ComboKey1, _settings.ComboKey2).Select(k => (k, sk)).ToList(),
            "70-72-60" => NonEmpty(_settings.ComboKey1, _settings.ComboKey2, _settings.ComboKey3).Select(k => (k, sk)).ToList(),
            _ => []
        };
    }

    private static List<string> NonEmpty(params string[] keys) =>
        keys.Where(k => !string.IsNullOrEmpty(k)).ToList();

    private Task TapSkillAsync(string skill, string trigger, LaneState state, CancellationToken token)
    {
        string key = _settings.Skills.GetValueOrDefault(skill, skill);
        return TapAsync(key, _settings.Timings.SkillKeyHold, trigger, state, token, skill);
    }

    private async Task TapAsync(string key, int hold, string trigger, LaneState state, CancellationToken token, string? skill = null, bool attack = true, Action? onSent = null)
    {
        CheckLane(trigger, state, token);
        if (attack && _settings.Farm.Monster.Enabled) await CheckMonsterAsync(trigger, state, token);
        CheckLane(trigger, state, token);
        if (_settings.SkillLayout != null || _settings.HealthMana.AnyEnabled)
        {
            SkillAddress? address = null;
            if (skill != null && _settings.SkillLayout != null)
                address = _settings.SkillLayout.Resolve(skill)
                    ?? throw new InvalidOperationException($"Skill bar'da bulunamadı: {skill}");
            // Kimliği olmayan eski pot/insert tuşları açıkça F1'e aittir.
            if (address == null && key.Length == 1 && char.IsAsciiDigit(key[0]))
                address = new SkillAddress(1, key == "0" ? 10 : key[0] - '0');
            await _dispatcher.TapAsync(address, key, hold, 20, 30,
                () => CheckLane(trigger, state, token),
                ms => WaitHeldAsync(ms, trigger, state, token), token, output =>
                {
                    if (state.Single) _testKeys.Enqueue(output);
                    if (output == (address?.Key ?? key))
                    {
                        onSent?.Invoke();
                        if (skill != null) _rotation.Sent(skill, Environment.TickCount64);
                    }
                });
            return;
        }
        if (InputDiagnostics.Active) InputDiagnostics.Record("engine_tap", new { key, holdMs = hold, trigger, skill });
        try
        {
            _input.KeyDown(key);
            if (state.Single) _testKeys.Enqueue(key);
            onSent?.Invoke();
            if (skill != null) _rotation.Sent(skill, Environment.TickCount64);
            await WaitHeldAsync(hold, trigger, state, token);
        }
        finally
        {
            _input.KeyUp(key);
        }
    }

    private async Task WaitHeldAsync(int milliseconds, string trigger, LaneState state, CancellationToken token)
    {
        if (milliseconds <= 0)
        {
            CheckLane(trigger, state, token);
            return;
        }

        long end = Environment.TickCount64 + milliseconds;
        while (true)
        {
            CheckLane(trigger, state, token);
            long left = end - Environment.TickCount64;
            if (left <= 0) return;
            await Task.Delay((int)Math.Min(left, 5), token).ConfigureAwait(false);
        }
    }

    private sealed class LaneState
    {
        public bool Single;
        public int Epoch;
        public bool Toggle;
        public bool Released;
        public bool Latched;
        public bool PrevDown;
    }

    private async Task MaybeBuffAsync(string trigger, LaneState state, CancellationToken token)
    {
        foreach (int index in _buffs.Due(Environment.TickCount64))
        {
            var buff = _settings.Farm.Buffs[index];
            // KeyDown başarılıysa basış iptal edilse bile aynı buff'ı hemen spamleme.
            await TapAsync(buff.Key, _settings.Timings.SkillKeyHold, trigger, state, token, attack: false,
                onSent: () => _buffs.MarkSent(index, Environment.TickCount64));
            await WaitHeldAsync(buff.CastWaitMs, trigger, state, token);
        }
    }

    private async Task CheckMonsterAsync(string trigger, LaneState state, CancellationToken token)
    {
        using var readStop = CancellationTokenSource.CreateLinkedTokenSource(token);
        Task<string> reading = _monsterReader!.ReadAsync(readStop.Token);
        // Odak/tetik kaybını OCR bitene kadar bekletme; geç biten hata da gözlensin.
        _ = reading.ContinueWith(t => _ = t.Exception, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        try
        {
            long deadline = Environment.TickCount64 + 2000;
            while (!reading.IsCompleted)
            {
                await WaitHeldAsync(5, trigger, state, token);
                if (Environment.TickCount64 >= deadline) throw new TimeoutException("Hedef OCR zaman aşımı; saldırı durduruldu.");
            }
            string text = await reading.ConfigureAwait(false);
            CheckLane(trigger, state, token);
            bool allowed = _settings.Farm.Monster.Allows(text);
            string name = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (name.Length > 80) name = name[..80];
            Volatile.Write(ref _monsterStatus, allowed ? "Hedef uygun: " + name : "Saldırı engellendi: " + (name.Length == 0 ? "isim okunamadı" : name));
            if (!allowed) throw new MonsterRejectedException(MonsterStatus);
        }
        finally { readStop.Cancel(); }
    }

    private sealed class MonsterRejectedException(string message) : Exception(message) { }
    private sealed class HoldReleasedException : Exception { }
    private sealed class PotionReadingStaleException : Exception { }

    private async Task RunPotionLaneAsync(CancellationToken token)
    {
        if (!_settings.HealthMana.AnyEnabled) return;
        var nextRead = new Dictionary<ResourceKind, long> { [ResourceKind.Hp] = 0, [ResourceKind.Mp] = 0 };
        var nextUse = new Dictionary<ResourceKind, long> { [ResourceKind.Hp] = 0, [ResourceKind.Mp] = 0 };
        var state = new LaneState { Epoch = Volatile.Read(ref _focusEpoch) };
        try
        {
            while (true)
            {
                try
                {
                    CheckEmergency(token);
                    if (!TargetAvailable() || state.Epoch != Volatile.Read(ref _focusEpoch))
                    {
                        state.Epoch = Volatile.Read(ref _focusEpoch);
                        Volatile.Write(ref _resourceStatus, "HP/MP: oyun ön planda değil");
                        await Task.Delay(100, token).ConfigureAwait(false);
                        continue;
                    }
                    long now = Environment.TickCount64;
                    await MaybePotionAsync(ResourceKind.Hp, nextRead, nextUse, state, token, now).ConfigureAwait(false);
                    await MaybePotionAsync(ResourceKind.Mp, nextRead, nextUse, state, token, now).ConfigureAwait(false);
                    await Task.Delay(25, token).ConfigureAwait(false);
                }
                catch (HoldReleasedException)
                {
                    state.Epoch = Volatile.Read(ref _focusEpoch);
                    Volatile.Write(ref _resourceStatus, "HP/MP: odak değişti, beklemede");
                    _dispatcher.Invalidate();
                    await Task.Delay(100, token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Volatile.Write(ref _resourceStatus, "HP/MP hata: " + ex.Message);
            Interlocked.CompareExchange(ref _error, ex.Message, null);
            InputDiagnostics.Record("resource_error", new { error = ex.Message, type = ex.GetType().Name });
            _stop!.Cancel();
        }
    }

    private async Task MaybePotionAsync(ResourceKind kind, Dictionary<ResourceKind, long> nextRead,
        Dictionary<ResourceKind, long> nextUse, LaneState state, CancellationToken token, long now)
    {
        var lane = kind == ResourceKind.Hp ? _settings.HealthMana.Hp : _settings.HealthMana.Mp;
        if (!lane.Enabled) return;
        string name = kind == ResourceKind.Hp ? "HP" : "MP";
        if (!lane.Region.IsSet)
        {
            Volatile.Write(ref _resourceStatus, $"{name}: bölge çizilmedi");
            nextRead[kind] = now + Math.Max(500, lane.ReadIntervalMs);
            return;
        }
        if (now < nextRead[kind]) return;
        nextRead[kind] = now + lane.ReadIntervalMs;
        if (_resourceReader == null)
        {
            Volatile.Write(ref _resourceStatus, $"{name}: OCR okuyucu yok");
            return;
        }
        using var readStop = CancellationTokenSource.CreateLinkedTokenSource(token);
        Task<ResourceReading?> reading = _resourceReader.ReadAsync(kind, readStop.Token);
        _ = reading.ContinueWith(t => _ = t.Exception, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        try
        {
            long deadline = Environment.TickCount64 + 1500;
            while (!reading.IsCompleted)
            {
                CheckLaneForPotion(state, token);
                if (Environment.TickCount64 >= deadline)
                {
                    readStop.Cancel();
                    Volatile.Write(ref _resourceStatus, $"{name}: OCR zaman aşımı");
                    return;
                }
                await Task.Delay(5, token).ConfigureAwait(false);
            }
            var value = await reading.ConfigureAwait(false);
            CheckLaneForPotion(state, token);
            if (!IsFreshReading(value, kind))
            {
                Volatile.Write(ref _resourceStatus, $"{name}: okunamadı/eski");
                return;
            }
            Volatile.Write(ref _resourceStatus, $"{name}: %{value.Value.Percent}");
            if (value.Value.Percent >= lane.ThresholdPercent || Environment.TickCount64 < nextUse[kind]) return;
            SkillAddress? address = _settings.SkillLayout?.Resolve(lane.SkillId);
            string key = address?.Key ?? lane.FallbackKey;
            if (_settings.SkillLayout != null && address == null && key.Length > 0)
            {
                if (key.Length == 1 && char.IsAsciiDigit(key[0]))
                    address = new SkillAddress(1, key == "0" ? 10 : key[0] - '0');
                else
                {
                    Volatile.Write(ref _resourceStatus, $"{name}: fallback SkillLayout ile F1 sayı tuşu olmalı");
                    return;
                }
            }
            if (address == null && key.Length == 0)
            {
                Volatile.Write(ref _resourceStatus, $"{name}: pot tuşu bulunamadı");
                return;
            }
            bool marked = false;
            void Guard()
            {
                CheckLaneForPotion(state, token);
                if (!IsFreshReading(value, kind)) throw new PotionReadingStaleException();
            }
            try
            {
                await _dispatcher.TapAsync(address, key, 30, 20, 30, Guard,
                    ms => WaitPotionAsync(ms, state, token), token, output =>
                    {
                        string actual = address?.Key ?? key;
                        if (!marked && output.Equals(actual, StringComparison.OrdinalIgnoreCase))
                        {
                            marked = true;
                            nextUse[kind] = Environment.TickCount64 + lane.CooldownMs;
                        }
                    }).ConfigureAwait(false);
                Volatile.Write(ref _resourceStatus, $"{name}: pot basıldı (%{value.Value.Percent})");
            }
            catch (PotionReadingStaleException)
            {
                Volatile.Write(ref _resourceStatus, $"{name}: eski okuma atlandı");
            }
        }
        finally { readStop.Cancel(); }
    }

    private static bool IsFreshReading(ResourceReading? reading, ResourceKind expected)
    {
        if (reading == null || reading.Value.Kind != expected || reading.Value.Percent is < 0 or > 100)
            return false;
        long age = Environment.TickCount64 - reading.Value.CapturedAt;
        return age is >= 0 and <= 1500;
    }

    private void CheckLaneForPotion(LaneState state, CancellationToken token)
    {
        CheckEmergency(token);
        if (!TargetAvailable() || state.Epoch != Volatile.Read(ref _focusEpoch))
            throw new HoldReleasedException();
    }

    private async Task WaitPotionAsync(int milliseconds, LaneState state, CancellationToken token)
    {
        long end = Environment.TickCount64 + Math.Max(0, milliseconds);
        do
        {
            CheckLaneForPotion(state, token);
            long left = end - Environment.TickCount64;
            if (left <= 0) return;
            await Task.Delay((int)Math.Min(left, 5), token).ConfigureAwait(false);
        } while (true);
    }
}
