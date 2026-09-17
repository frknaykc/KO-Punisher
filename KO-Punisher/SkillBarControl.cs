using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

internal sealed class SkillIconTile : Panel
{
    private static readonly ToolTip Tips = new();
    public SkillDef Skill { get; }

    public SkillIconTile(SkillDef skill)
    {
        Skill = skill;
        Size = new Size(42, 42);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        var pic = new PictureBox
        {
            Bounds = new Rectangle(1, 1, 40, 40),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        Image? icon = SkillCatalog.LoadIcon(skill.IconFile);
        if (icon != null) pic.Image = icon;
        Controls.Add(pic);
        AccessibleName = pic.AccessibleName = skill.DisplayName;
        Tips.SetToolTip(this, SkillCatalog.Description(skill.Id));
        Tips.SetToolTip(pic, SkillCatalog.Description(skill.Id));
        MouseDown += BeginDrag;
        pic.MouseDown += BeginDrag;
    }

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        DoDragDrop(Skill.Id, DragDropEffects.Copy);
    }
}

internal sealed class SkillBarSlot : Panel
{
    private static readonly ToolTip Tips = new();
    public int Index { get; }
    public string BoundKey { get; }
    public string SkillId { get; private set; } = "";
    public int CooldownMs { get; private set; }

    public event Action? Changed;

    private readonly PictureBox _icon = new()
    {
        Bounds = new Rectangle(18, 3, 32, 32),
        SizeMode = PictureBoxSizeMode.Zoom,
        AllowDrop = true,
        BackColor = Color.Transparent
    };

    public SkillBarSlot(int index, string boundKey)
    {
        Index = index;
        BoundKey = boundKey;
        Size = new Size(52, 42);
        BackColor = UiTheme.PanelAlt;
        AllowDrop = true;
        BorderStyle = BorderStyle.FixedSingle;

        var key = new Label
        {
            Text = boundKey,
            Bounds = new Rectangle(1, 6, 16, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = UiTheme.KeyChip,
            Font = UiTheme.Small
        };
        Controls.Add(_icon);
        Controls.Add(key);

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(typeof(string)) == true) e.Effect = DragDropEffects.Copy;
        };
        DragDrop += OnDrop;
        _icon.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(typeof(string)) == true) e.Effect = DragDropEffects.Copy;
        };
        _icon.AllowDrop = true;
        _icon.DragDrop += OnDrop;
        MouseDown += OnRightClear;
        _icon.MouseDown += OnRightClear;
    }

    public void SetSkill(string skillId, int cooldownMs)
    {
        SkillId = skillId ?? "";
        CooldownMs = cooldownMs;
        AccessibleName = _icon.AccessibleName = SkillCatalog.Find(SkillId)?.DisplayName ?? "Boş slot";
        string hint = SkillId.Length == 0 ? "Skill bırak" : SkillCatalog.Description(SkillId);
        Tips.SetToolTip(this, hint);
        Tips.SetToolTip(_icon, hint);
        _icon.Image?.Dispose();
        _icon.Image = SkillId.Length == 0 ? null : SkillCatalog.LoadIcon(SkillCatalog.Find(SkillId)?.IconFile ?? "");
        Invalidate();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        string? id = e.Data?.GetData(typeof(string)) as string;
        if (string.IsNullOrWhiteSpace(id)) return;
        var def = SkillCatalog.Find(id);
        SetSkill(id, def?.DefaultCooldownMs ?? 11000);
        Changed?.Invoke();
    }

    private void OnRightClear(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        SetSkill("", 0);
        Changed?.Invoke();
    }
}

internal sealed class SkillBarPanel : Panel
{
    public const int SlotCount = 10;
    public IReadOnlyList<SkillDef> ActiveCatalog { get; set; } = SkillCatalog.Assassin;
    private readonly SkillBarSlot[] _slots;
    private readonly NumericUpDown _cooldown = new() { Minimum = 0, Maximum = 120000, Width = 90 };
    private readonly Label _selected = new() { AutoSize = true, ForeColor = UiTheme.Muted, Text = "Slot seç, cooldown ayarla. Sağ tık siler." };
    private SkillBarSlot? _active;

    public event Action? Changed;

    public SkillBarPanel()
    {
        Height = 96;
        BackColor = UiTheme.Panel;
        _slots = new SkillBarSlot[SlotCount];
        var keys = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        for (int i = 0; i < SlotCount; i++)
        {
            var slot = new SkillBarSlot(i, keys[i]);
            slot.Location = new Point(8 + i * 54, 26);
            slot.Changed += () => Changed?.Invoke();
            slot.Click += (_, _) => Select(slot);
            slot.MouseClick += (_, _) => Select(slot);
            _slots[i] = slot;
            Controls.Add(slot);
        }

        Controls.Add(new Label
        {
            Text = "Skill bar  ·  sürükle / sağ tık sil",
            Location = new Point(8, 4),
            AutoSize = true,
            ForeColor = UiTheme.Gold,
            Font = UiTheme.Title
        });

        _selected.Location = new Point(8, 72);
        _cooldown.Location = new Point(430, 68);
        UiTheme.StyleNumeric(_cooldown);
        _cooldown.ValueChanged += (_, _) =>
        {
            if (_active == null) return;
            _active.SetSkill(_active.SkillId, (int)_cooldown.Value);
            Changed?.Invoke();
        };
        Controls.Add(_selected);
        Controls.Add(new Label { Text = "CD ms", Location = new Point(380, 72), AutoSize = true, ForeColor = UiTheme.Muted });
        Controls.Add(_cooldown);
    }

    public IEnumerable<SkillBarSlot> Slots => _slots;

    public void LoadFrom(Settings settings)
    {
        foreach (var slot in _slots)
        {
            string? skill = settings.Skills.FirstOrDefault(kv =>
                !string.IsNullOrEmpty(kv.Key) && kv.Value.Equals(slot.BoundKey, StringComparison.OrdinalIgnoreCase)).Key;
            if (string.IsNullOrEmpty(skill))
            {
                slot.SetSkill("", 0);
                continue;
            }
            int cd = skill == "Spike" ? settings.Cooldowns.Spike : settings.SkillCooldowns.GetValueOrDefault(skill, 11000);
            slot.SetSkill(skill, cd);
        }
    }

    public void ApplyTo(Settings settings)
    {
        // Bu bar yalnızca 0–9 slotlarını gösterir. F/harf/numpad atamalarını
        // görünmüyorlar diye silme; ayar dosyasındaki atamaları koru.
        var skills = settings.Skills.Where(kv => !string.IsNullOrEmpty(kv.Value) &&
            !_slots.Any(slot => slot.BoundKey.Equals(kv.Value, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        var filler = new List<string>();
        var cds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var slot in _slots)
        {
            if (slot.SkillId.Length == 0) continue;
            skills[slot.SkillId] = slot.BoundKey;
            var def = SkillCatalog.Find(slot.SkillId);
            if (slot.SkillId == "Spike") settings.Cooldowns.Spike = Math.Max(100, slot.CooldownMs);
            else cds[slot.SkillId] = slot.CooldownMs;
            if (def?.Rotation == true && !filler.Contains(slot.SkillId)) filler.Add(slot.SkillId);
        }
        foreach (var known in ActiveCatalog)
        {
            if (!skills.ContainsKey(known.Id)) skills[known.Id] = "";
        }
        if (settings.ClassType == ClassType.Assassin)
        {
            if (!skills.ContainsKey("Spike") || string.IsNullOrEmpty(skills["Spike"]))
                skills["Spike"] = settings.Skills.GetValueOrDefault("Spike", "3");
        }
        else
        {
            foreach (var kv in settings.Skills)
            {
                if (!skills.ContainsKey(kv.Key)) skills[kv.Key] = kv.Value;
            }
        }
        settings.Skills = skills;
        foreach (string id in settings.FillerOrder)
            if (skills.TryGetValue(id, out string? key) && !string.IsNullOrEmpty(key) && !filler.Contains(id)) filler.Add(id);
        settings.FillerOrder = filler;
        foreach (var kv in cds) settings.SkillCooldowns[kv.Key] = kv.Value;
    }

    private void Select(SkillBarSlot slot)
    {
        _active = slot;
        _selected.Text = slot.SkillId.Length == 0
            ? $"Slot {slot.BoundKey} boş"
            : $"{SkillCatalog.Find(slot.SkillId)?.DisplayName ?? slot.SkillId}  ·  tuş {slot.BoundKey}";
        _cooldown.Value = Math.Clamp(slot.CooldownMs, 0, 120000);
    }
}

