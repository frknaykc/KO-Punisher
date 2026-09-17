using System.Drawing;

namespace KOPunisher;

public sealed class InventoryCalibration
{
    public int Version { get; set; } = 1;
    public Rectangle Bounds { get; set; }
    public Rectangle Desktop { get; set; }
    public int Dpi { get; set; }
    public int Columns { get; set; } = 7;
    public int Rows { get; set; } = 4;

    public Rectangle[] Slots(Rectangle desktop, int dpi)
    {
        if (Version != 1 || Desktop != desktop || Dpi != dpi)
            throw new InvalidOperationException("Ekran/ölçek değişti. Envanter alanını yeniden seç.");
        if (Columns is < 1 or > 12 || Rows is < 1 or > 12 ||
            Bounds.Width < Columns * 16 || Bounds.Height < Rows * 16 || !desktop.Contains(Bounds))
            throw new InvalidOperationException("Crop veya slot sayısı geçersiz. Yalnız slot ızgarasını seç.");
        var slots = new List<Rectangle>();
        for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Columns; col++)
            {
                int x = col * Bounds.Width / Columns, y = row * Bounds.Height / Rows;
                slots.Add(Rectangle.FromLTRB(x, y, (col + 1) * Bounds.Width / Columns,
                    (row + 1) * Bounds.Height / Rows));
            }
        return slots.ToArray();
    }
}

public sealed class InventoryReview
{
    private string? _snapshot;
    private readonly HashSet<int> _selected = new();
    public bool Approved { get; private set; }
    public int Count { get; private set; }
    public IReadOnlyCollection<int> Selected => _selected.ToArray();
    public void Reset() { _snapshot = null; Count = 0; _selected.Clear(); Approved = false; }
    public void Scan(string hash, int count)
    {
        Reset();
        if (string.IsNullOrWhiteSpace(hash) || count is < 1 or > 144)
            throw new ArgumentException("Geçersiz tarama.");
        _snapshot = hash; Count = count;
    }
    public void Select(int slot, bool selected)
    {
        if (slot < 0 || slot >= Count) throw new ArgumentOutOfRangeException(nameof(slot));
        Approved = false;
        if (selected) _selected.Add(slot); else _selected.Remove(slot);
    }
    public bool Confirm(string currentHash)
    {
        Approved = false;
        if (_snapshot == null || currentHash != _snapshot) { Reset(); return false; }
        return Approved = _selected.Count > 0;
    }
}
