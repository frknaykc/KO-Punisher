namespace KOPunisher;

public sealed class FarmSettings
{
    public List<BuffSetting> Buffs { get; set; } = new[] { "Wolf", "DEF 200", "DEF 400", "DEF 800", "TS", "Magic Hammer", "Özel 1", "Özel 2" }
        .Select(name => new BuffSetting { Name = name }).ToList();
    public MonsterFilterSettings Monster { get; set; } = new();
    public MarketSettings Market { get; set; } = new();

    public void Validate(IEnumerable<string> occupiedKeys)
    {
        if (Buffs == null || Monster == null || Market == null || Buffs.Count > 8 || Buffs.Any(b => b == null))
            throw new ArgumentException("Farm ayar bölümleri eksik veya buff sayısı sekizden fazla.");
        var occupied = occupiedKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var buff in Buffs.Where(b => b.Enabled))
        {
            buff.Key = (buff.Key ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(buff.Name) || !InputSender.TryGetVkCode(buff.Key, out var vk) ||
                vk is 0x05 or 0x06 or 0x10 or 0x11 or 0x12 or 0x0D or 0x1B || !occupied.Add(buff.Key))
                throw new ArgumentException("Buff tuşu geçerli olmalı; diğer buff, saldırı ve tetik tuşlarıyla çakışmamalı.");
            if (buff.IntervalSeconds is < 1 or > 86400 || buff.CastWaitMs is < 20 or > 10000)
                throw new ArgumentException("Buff aralığı 1–86400 saniye, kullanım sonrası bekleme 20–10000 ms olmalı.");
        }
        Monster.Validate();
    }
}

public sealed class BuffSetting
{
    public bool Enabled { get; set; }
    public string Name { get; set; } = "Özel";
    public string Key { get; set; } = "";
    // Sunucuya göre kullanıcı ayarlar; gerçek buff süresi varsayılmaz.
    public int IntervalSeconds { get; set; } = 60;
    public int CastWaitMs { get; set; } = 1000;
}

public sealed class MonsterFilterSettings
{
    public bool Enabled { get; set; }
    public List<string> Names { get; set; } = [];
    // Oyun istemci alanına göre koordinat: pencere taşınırsa aynı bölge izlenir.
    public UiRegion Region { get; set; } = new();
    public int SettleMs { get; set; } = 250;
    public int ClientWidth { get; set; }
    public int ClientHeight { get; set; }
    public void Validate()
    {
        if (!Enabled) return;
        if (Names == null || Names.Count is < 1 or > 100 || Names.Any(n => string.IsNullOrWhiteSpace(n) || n.Length > 100 || n.Any(char.IsControl)))
            throw new ArgumentException("Mob filtresine her satırda tek tam isim girin (en fazla 100 isim).");
        if (Region == null || !Region.IsSet || Region.X < 0 || Region.Y < 0 || Region.W > 2000 || Region.H > 500)
            throw new ArgumentException("Önce oyundaki hedef adını kırpın.");
        if (SettleMs is < 100 or > 3000) throw new ArgumentException("Hedef değişim beklemesi 100–3000 ms olmalı.");
    }
    public bool Allows(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || Names == null) return false;
        static string Normalize(string s) => string.Join(" ", s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        string target = Normalize(text);
        return Names.Any(n => !string.IsNullOrWhiteSpace(n) && string.Equals(Normalize(n), target, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class MarketSettings
{
    public List<string> Messages { get; set; } = [];
    public int IntervalSeconds { get; set; } = 30;
    public int RepeatCount { get; set; } = 10;
    public void Validate()
    {
        if (Messages == null || Messages.Count is < 1 or > 20 || Messages.Any(m => string.IsNullOrWhiteSpace(m) || m.Length > 160 || m.Any(char.IsControl)))
            throw new ArgumentException("Her satırda 1–160 karakterlik bir mesaj girin (en fazla 20 mesaj).");
        if (IntervalSeconds is < 10 or > 3600 || RepeatCount is < 1 or > 1000)
            throw new ArgumentException("Pazar aralığı 10–3600 saniye, toplam gönderim sayısı 1–1000 olmalı.");
    }
}

internal sealed class BuffScheduler(List<BuffSetting> buffs)
{
    private readonly long[] _next = new long[buffs.Count];
    public IEnumerable<int> Due(long now) => Enumerable.Range(0, buffs.Count).Where(i => buffs[i].Enabled && now >= _next[i]).ToArray();
    public int? NextDue(long now)
    {
        for (int i = 0; i < buffs.Count; i++) if (buffs[i].Enabled && now >= _next[i]) return i;
        return null;
    }
    public void MarkSent(int index, long now) => _next[index] = now + buffs[index].IntervalSeconds * 1000L;
}

public interface IMonsterReader
{
    Task<string> ReadAsync(CancellationToken token);
}
