namespace KOPunisher;

public readonly record struct ChatStroke(string Key, bool Shift, bool Control, bool Alt);

public interface IChatEncoder
{
    bool IsCurrent { get; }
    IReadOnlyList<ChatStroke> Encode(string text);
}

// Yalnız ayrı pazar modunda çalıştırılır; combo, Minor ve testlerle paylaşılmaz.
public sealed class MarketRunner
{
    private readonly MarketSettings _settings;
    private readonly IMacroInput _input;
    private readonly IChatEncoder _encoder;
    private readonly string _target, _emergency;
    public int SentCount { get; private set; }

    public MarketRunner(MarketSettings settings, IMacroInput input, IChatEncoder encoder, string target, string emergency)
    {
        _settings = Settings.Snapshot(settings);
        _settings.Validate();
        if (string.IsNullOrWhiteSpace(target) || !InputSender.TryGetVkCode(emergency, out _))
            throw new ArgumentException("Pazar hedef process/acil durdurma ayarı geçersiz.");
        _input = input; _encoder = encoder; _target = target; _emergency = emergency;
    }

    public async Task RunAsync(CancellationToken token)
    {
        SentCount = 0;
        try
        {
            // Desteklenmeyen karakteri sohbeti açmadan, tüm liste için yakala.
            var messages = _settings.Messages.Select(m => _encoder.Encode(m)).ToArray();
            for (int i = 0; i < _settings.RepeatCount; i++)
            {
                Check(token);
                await StrokeAsync(new("ENTER", false, false, false), token);
                await WaitAsync(250, token);
                foreach (var stroke in messages[i % messages.Length])
                {
                    await StrokeAsync(stroke, token);
                    await WaitAsync(20, token);
                }
                await WaitAsync(100, token);
                await StrokeAsync(new("ENTER", false, false, false), token);
                SentCount++;
                if (SentCount < _settings.RepeatCount) await WaitAsync(_settings.IntervalSeconds * 1000, token);
            }
        }
        finally { _input.ReleaseAll(); }
    }

    private void Check(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (_input.IsHeld(_emergency)) throw new OperationCanceledException("Acil durdurma.");
        if (!_input.IsTargetForeground(_target)) throw new InvalidOperationException("Oyun odağı kayboldu; pazar durdu.");
        if (InputDiagnostics.Active) throw new InvalidOperationException("Mesaj tuşlarını kaydetmemek için önce tanılama kaydını bitirin.");
        if (_input.IsHeld("SHIFT") || _input.IsHeld("CTRL") || _input.IsHeld("ALT") || _input.IsHeld("CAPSLOCK"))
            throw new InvalidOperationException("Fiziksel modifier tuşu basılı; pazar durdu. Sohbeti kontrol edin.");
        if (!_encoder.IsCurrent) throw new InvalidOperationException("Oyun penceresi veya klavye düzeni değişti; pazar durdu.");
    }

    private async Task StrokeAsync(ChatStroke stroke, CancellationToken token)
    {
        var keys = new List<string>();
        if (stroke.Control) keys.Add("CTRL");
        if (stroke.Alt) keys.Add("ALT");
        if (stroke.Shift) keys.Add("SHIFT");
        keys.Add(stroke.Key);
        var pressed = new List<string>();
        try
        {
            foreach (string key in keys)
            {
                Check(token);
                pressed.Add(key);
                _input.KeyDown(key);
            }
            await WaitAsync(25, token);
        }
        finally
        {
            // KeyDown hata verse de bırakmayı dene. Bir KeyUp hatası diğerlerini engellemesin.
            Exception? failure = null;
            foreach (string key in pressed.AsEnumerable().Reverse())
                try { _input.KeyUp(key); } catch (Exception ex) { failure ??= ex; }
            if (failure != null) throw failure;
        }
    }

    private async Task WaitAsync(int ms, CancellationToken token)
    {
        long end = Environment.TickCount64 + ms;
        do
        {
            Check(token);
            long left = end - Environment.TickCount64;
            if (left <= 0) return;
            await Task.Delay((int)Math.Min(left, 5), token).ConfigureAwait(false);
        } while (true);
    }
}
