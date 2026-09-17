namespace KOPunisher;

// Cooldown, basılı tutmayı bırakınca sıfırlanmaz. Oyun belleği okunmaz;
// yalnızca başarılı OS gönderiminin zamanı üzerinden tahmin yapılır.
public sealed class SkillRotation
{
    private readonly Settings _settings;
    private readonly Dictionary<string, long> _readyAt = new();
    private readonly object _sync = new();
    private int _fillerIndex;

    public SkillRotation(Settings settings) => _settings = settings;

    public string? Next(long now)
    {
        lock (_sync)
        {
            bool hasSpike = _settings.SkillLayout == null || _settings.SkillLayout.Resolve("Spike") != null;
            if (hasSpike && Ready("Spike", now)) return "Spike";
            if (hasSpike && _readyAt.GetValueOrDefault("Spike") - now < _settings.Timings.EstimatedComboDuration) return null;
            for (int i = 0; i < _settings.FillerOrder.Count; i++)
            {
                int index = (_fillerIndex + i) % _settings.FillerOrder.Count;
                string name = _settings.FillerOrder[index];
                if (!string.IsNullOrEmpty(_settings.Skills[name]) && Ready(name, now))
                {
                    _fillerIndex = (index + 1) % _settings.FillerOrder.Count;
                    return name;
                }
            }
            return null;
        }
    }

    private bool Ready(string name, long now) => !_readyAt.TryGetValue(name, out long deadline) || now >= deadline;

    public void Sent(string name, long now)
    {
        lock (_sync)
        {
            int cooldown = name == "Spike" ? _settings.Cooldowns.Spike : _settings.SkillCooldowns.GetValueOrDefault(name, 11000);
            _readyAt[name] = now + cooldown;
        }
    }

    public long SpikeRemaining(long now)
    {
        lock (_sync) return Math.Max(0, _readyAt.GetValueOrDefault("Spike", now) - now);
    }
}
