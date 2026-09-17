namespace KOPunisher;

// Tetikten bağımsız testin iptal/bırakma akışı; testlerde sahte input ile çalışır.
internal static class InputProbe
{
    internal static async Task RunAsync(IMacroInput input, string key, int holdMs, int countdownSeconds,
        Action<int> countdown, Action beforeSend, CancellationToken token)
    {
        if (holdMs < 0 || countdownSeconds < 0) throw new ArgumentOutOfRangeException(nameof(holdMs));
        for (int i = countdownSeconds; i > 0; i--)
        {
            token.ThrowIfCancellationRequested();
            countdown(i);
            await Task.Delay(1000, token);
        }
        token.ThrowIfCancellationRequested();
        beforeSend();
        token.ThrowIfCancellationRequested();
        try
        {
            input.KeyDown(key);
            await Task.Delay(holdMs, token);
        }
        finally { input.KeyUp(key); }
    }
}
