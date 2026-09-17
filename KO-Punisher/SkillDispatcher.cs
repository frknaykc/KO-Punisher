namespace KOPunisher;

// Yalnız Skill Path etkin olduğunda kullanılır. Eski iki bağımsız lane davranışı değişmez.
public sealed class SkillDispatcher(IMacroInput input)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _bar;
    private long _physicalEpoch = -1;
    private int _generation;
    private int _barGeneration = -1;

    public void Invalidate() => Interlocked.Increment(ref _generation);

    public async Task TapAsync(SkillAddress? address, string key, int holdMs,
        int barHoldMs, int barAfterMs, Action check, Func<int, Task> wait,
        CancellationToken token, Action<string>? onDown = null)
    {
        if (address != null && (address.Bar is < 1 or > 8 || address.Slot is < 1 or > 10))
            throw new ArgumentException("Geçersiz skill bar adresi.");
        if (holdMs is < 5 or > 2000 || barHoldMs is < 5 or > 500 || barAfterMs is < 0 or > 2000)
            throw new ArgumentException("Geçersiz bar/skill basış süresi.");
        if (address == null && !InputSender.TryGetVkCode(key, out _))
            throw new ArgumentException("Geçersiz çıkış tuşu.");

        // Kilitte bekleyen Minor/atak da tetik, odak ve acil durdurmaya cevap verir.
        check();
        while (!await _gate.WaitAsync(5, token).ConfigureAwait(false)) check();
        try
        {
            check();
            token.ThrowIfCancellationRequested();
            long epoch = input.BarSelectionEpoch;
            int generation = Volatile.Read(ref _generation);
            void Guard()
            {
                token.ThrowIfCancellationRequested();
                check();
                if (input.CanTrackBarSelection && input.BarSelectionEpoch != epoch)
                    throw new InvalidOperationException("Skill basışı sırasında bar elle değiştirildi; makro durduruldu.");
            }
            async Task Tap(string output, int milliseconds)
            {
                Guard();
                try
                {
                    input.KeyDown(output);
                    onDown?.Invoke(output);
                    await wait(milliseconds).ConfigureAwait(false);
                    Guard();
                }
                finally { input.KeyUp(output); }
            }

            if (address != null)
            {
                if (!input.CanTrackBarSelection || _bar != address.Bar || _physicalEpoch != epoch || _barGeneration != generation)
                {
                    await Tap(address.BarKey, barHoldMs).ConfigureAwait(false);
                    await wait(barAfterMs).ConfigureAwait(false);
                    Guard();
                    _bar = address.Bar;
                    _physicalEpoch = epoch;
                    _barGeneration = generation;
                }
                await Tap(address.Key, holdMs).ConfigureAwait(false);
            }
            else
            {
                // Z/R/W gibi bar dışı tuşlar da aynı kilitten geçer. Elle seçilmiş F tuşu cache'i bozar.
                if (key.Length > 1 && key[0] == 'F') Invalidate();
                await Tap(key, holdMs).ConfigureAwait(false);
            }
        }
        catch
        {
            Invalidate();
            throw;
        }
        finally { _gate.Release(); }
    }
}
