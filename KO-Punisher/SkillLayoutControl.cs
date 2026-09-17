using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

/// <summary>Job-scoped, draft-only skill path editor. The owner persists SaveRequested.</summary>
public sealed class SkillLayoutControl : UserControl
{
    private SkillLayout _layout = new();
    private SkillLayout? _saved;
    private IReadOnlySet<string> _allowed = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<string, JobSkillDef> _skills = new(StringComparer.Ordinal);
    private readonly ToolTip _tips = new() { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 3500 };
    private readonly FlowLayoutPanel _bars = Row();
    private readonly FlowLayoutPanel _slots = Row();
    private readonly FlowLayoutPanel _palette = new()
    {
        Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true,
        BackColor = UiTheme.Panel, Padding = new Padding(4), MinimumSize = new Size(0, 110)
    };
    private readonly TextBox _search = new() { Width = 190, PlaceholderText = "Skill ara…" };
    private readonly ComboBox _category = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList, Width = 110,
        BackColor = UiTheme.Slot, ForeColor = UiTheme.Text, FlatStyle = FlatStyle.Flat
    };
    private readonly Button _add = UiTheme.NavButton("+ Bar ekle");
    private readonly Label _state = new() { AutoSize = true, Padding = new Padding(6), ForeColor = UiTheme.Muted };
    private readonly Label _feedback = new() { AutoSize = true, ForeColor = UiTheme.Muted, Margin = new Padding(6) };
    private readonly List<Button> _barButtons = new();
    private readonly List<SkillTile> _slotTiles = new();
    private SkillAddress? _dragSource;
    private string? _dragId;
    private int _selectedBar = 1;
    private bool _loading;

    public bool Busy { get; private set; }
    public event EventHandler? LayoutChanged;
    public event EventHandler? SaveRequested;

    public SkillLayoutControl()
    {
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        UiTheme.PaintDark(this);
        AutoScroll = true;
        Size = new Size(720, 400);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4,
            BackColor = UiTheme.Bg, Padding = new Padding(4), AutoScroll = true
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        for (int i = 0; i < 3; i++) grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var actions = Row();
        var save = UiTheme.NavButton("Skill bar kaydet");
        save.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);
        actions.Controls.Add(save);
        var scan = UiTheme.NavButton("OCR ile kalibre et");
        scan.Click += async (_, _) => await ScanAsync();
        actions.Controls.Add(scan);
        actions.Controls.Add(_state);
        grid.Controls.Add(actions, 0, 0);
        grid.Controls.Add(_bars, 0, 1);
        grid.Controls.Add(_slots, 0, 2);
        _feedback.Text = "Havuzdan sürükle · slotu taşı · sağ tık temizle";
        grid.Controls.Add(_feedback, 0, 3);
        var filters = Row();
        UiTheme.StyleField(_search);
        _category.Items.AddRange(new object[] { "Tümü", "Atak", "Buff", "Heal", "Destek" });
        _category.SelectedIndex = 0;
        filters.Controls.Add(new Label { Text = "Full Skill Sembolleri · seçili job", AutoSize = true, ForeColor = UiTheme.Gold, Margin = new Padding(4, 5, 40, 5) });
        filters.Controls.Add(_search);
        filters.Controls.Add(_category);
        grid.SetColumnSpan(actions, 2);
        grid.Controls.Add(filters, 1, 1);
        grid.Controls.Add(_palette, 1, 2);
        grid.SetRowSpan(_palette, 2);
        _feedback.MaximumSize = new Size(400, 0);
        Controls.Add(grid);
        _search.TextChanged += (_, _) => { if (!_loading) RenderPalette(); };
        _category.SelectedIndexChanged += (_, _) => { if (!_loading) RenderPalette(); };
        _add.Click += (_, _) =>
        {
            if (_layout.VisibleBars >= 8) return;
            _layout.VisibleBars++;
            _selectedBar = _layout.VisibleBars;
            RenderBars();
            RenderSlots();
            Changed();
        };
        for (int bar = 1; bar <= 8; bar++)
        {
            int number = bar;
            var button = UiTheme.NavButton($"F{bar}");
            button.MinimumSize = new Size(40, 28);
            button.Padding = new Padding(6, 0, 6, 0);
            button.AllowDrop = true;
            button.Click += (_, _) => SelectBar(number);
            button.DragEnter += (_, e) =>
            {
                e.Effect = DropEffect(e);
                if (e.Effect != DragDropEffects.None) SelectBar(number);
            };
            _tips.SetToolTip(button, $"F{bar} · taşırken üzerine gelerek bar değiştir");
            _barButtons.Add(button);
            _bars.Controls.Add(button);
        }
        _bars.Controls.Add(_add);
        for (int slot = 1; slot <= 10; slot++)
        {
            int number = slot;
            var tile = new SkillTile { Size = new Size(62, 84), AllowDrop = true };
            tile.AccessibleRole = AccessibleRole.ListItem;
            tile.DragEnter += (_, e) => PreviewDrop(tile, number, e);
            tile.DragOver += (_, e) => PreviewDrop(tile, number, e);
            tile.DragLeave += (_, _) => tile.SetDropHighlight(false);
            tile.DragDrop += (_, e) => DropOnSlot(tile, number, e);
            tile.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Right) ClearSlot(number);
            };
            tile.DragStarted += (_, _) =>
            {
                string? id = IdAt(_selectedBar, number);
                if (id != null) BeginSkillDrag(tile, id, new SkillAddress(_selectedBar, number));
            };
            tile.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Delete) { ClearSlot(number); e.Handled = true; }
            };
            _slotTiles.Add(tile);
            _slots.Controls.Add(tile);
        }
        LoadLayout(ClassType.Assassin, null);
    }

    private async Task ScanAsync()
    {
        if (Busy) return;
        var owner = FindForm();
        Busy = true;
        Enabled = false;
        try
        {
            if (MessageBox.Show(this,
                $"Oyunda F{_selectedBar} barını açın. Tek satır/sütundaki 10 slotu (1–0) seçin.\n" +
                "Cooldown kararması olmamalı. Tanınmayan slotlar boş kalır; sonuç onayınızla uygulanır.",
                "Skill bar tarama", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            owner?.Hide();
            await Task.Delay(250);
            var desktop = SystemInformation.VirtualScreen;
            using var screen = new Bitmap(desktop.Width, desktop.Height);
            using (var graphics = Graphics.FromImage(screen))
                graphics.CopyFromScreen(desktop.Location, Point.Empty, desktop.Size);
            using var crop = new InventoryCropForm(screen, desktop,
                $"F{_selectedBar}: 1–0 arası 10 skill slotunu seç · ESC: iptal");
            if (crop.ShowDialog() != DialogResult.OK) return;
            Rectangle area = crop.SelectedBounds;
            area.Offset(-desktop.X, -desktop.Y);
            using var image = screen.Clone(area, screen.PixelFormat);
            var skills = _skills.Values.ToArray();
            var matches = await Task.Run(() => SkillBarVision.Scan(image, skills));
            owner?.Show();
            int found = matches.Count(m => m.SkillId != null);
            if (found == 0) { _feedback.Text = "Skill tanınamadı. Seçimi/ölçeği kontrol edin veya sürükleyerek atayın."; return; }
            string preview = string.Join("\n", matches.Select((m, i) =>
                $"{(i == 9 ? 0 : i + 1)} → {(m.SkillId == null ? "Tanınmadı — boş kalacak" : NameOf(m.SkillId))}"));
            if (MessageBox.Show(this, preview + $"\n\n{found}/10 tanındı. F{_selectedBar} yerleşimi değiştirilsin mi?",
                "Kalibrasyon önizlemesi", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            _layout = SkillIconMatcher.Merge(_layout, _selectedBar, matches, _allowed);
            RenderSlots();
            Changed();
            _feedback.Text = $"F{_selectedBar}: {found}/10 tanındı; {10 - found} slot elle doldurulmalı.";
        }
        catch (Exception ex) { _feedback.Text = "Kalibrasyon başarısız: " + ex.Message; }
        finally
        {
            Busy = false;
            if (!IsDisposed) Enabled = true;
            if (owner is { IsDisposed: false }) { owner.Show(); owner.Activate(); }
        }
    }

    private static FlowLayoutPanel Row() => new()
    {
        Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true, Margin = new Padding(0), Padding = new Padding(2)
    };

    public void LoadLayout(ClassType job, SkillLayout? layout)
    {
        // Validate before changing the current draft; an invalid load is not a partial load.
        var allowed = JobSkillCatalog.IdsForJob(job);
        layout?.Validate(allowed);
        var draft = layout?.Clone() ?? new SkillLayout();
        var skills = JobSkillCatalog.ForJob(job).ToDictionary(s => s.Id, StringComparer.Ordinal);
        _loading = true;
        try
        {
            _allowed = allowed;
            _skills = skills;
            _layout = draft;
            _saved = layout == null ? null : draft.Clone();
            _selectedBar = 1;
            _dragSource = null;
            _dragId = null;
            _search.Clear();
            _category.SelectedIndex = 0;
            RenderBars();
            RenderSlots();
            RenderPalette();
            _feedback.Text = "Havuzdan sürükle · slotu taşı · sağ tık temizle";
            UpdateSaveState();
        }
        finally { _loading = false; }
    }

    public SkillLayout GetLayout() => _layout.Clone();

    public void MarkSaved() { _saved = _layout.Clone(); UpdateSaveState(); }

    private string? IdAt(int bar, int slot) =>
        _layout.Assignments.FirstOrDefault(a => a.Bar == bar && a.Slot == slot)?.SkillId;

    private string NameOf(string? id) => id != null && _skills.TryGetValue(id, out var skill) ? skill.Name : "Boş";

    private void SelectBar(int bar)
    {
        if (bar < 1 || bar > _layout.VisibleBars || bar == _selectedBar) return;
        _selectedBar = bar;
        RenderBars();
        RenderSlots();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData >= Keys.F1 && keyData <= Keys.F8)
        {
            int bar = (int)keyData - (int)Keys.F1 + 1;
            if (bar <= _layout.VisibleBars) { SelectBar(bar); return true; }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void RenderBars()
    {
        for (int i = 0; i < _barButtons.Count; i++)
        {
            _barButtons[i].Visible = i < _layout.VisibleBars;
            _barButtons[i].BackColor = i + 1 == _selectedBar ? UiTheme.KeyChip : UiTheme.Panel;
        }
        _add.Enabled = _layout.VisibleBars < 8;
    }

    private void RenderSlots()
    {
        for (int i = 0; i < _slotTiles.Count; i++)
        {
            string? id = IdAt(_selectedBar, i + 1);
            var skill = id != null ? _skills.GetValueOrDefault(id) : null;
            string key = i == 9 ? "0" : (i + 1).ToString();
            var tile = _slotTiles[i];
            tile.Bind(skill, key);
            tile.AccessibleName = $"F{_selectedBar} → {key}: {NameOf(id)}";
            _tips.SetToolTip(tile, id == null ? $"F{_selectedBar} → {key} · Skill bırak" :
                $"{skill?.Name ?? id} · sürükle taşı / sağ tık temizle");
        }
    }

    private void RenderPalette()
    {
        _palette.SuspendLayout();
        try
        {
            // Controls.Clear does not dispose controls or their GDI images.
            while (_palette.Controls.Count > 0)
            {
                var old = _palette.Controls[0];
                _tips.SetToolTip(old, null);
                _palette.Controls.Remove(old);
                old.Dispose();
            }
            string query = _search.Text.Trim();
            string category = _category.SelectedIndex switch
            {
                1 => "Attack", 2 => "Buff", 3 => "Heal", 4 => "Utility", _ => ""
            };
            foreach (var skill in _skills.Values.Where(s =>
                (category.Length == 0 || s.Category.ToString() == category) &&
                (query.Length == 0 || s.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                 s.Id.Contains(query, StringComparison.OrdinalIgnoreCase))))
            {
                var tile = new SkillTile
                {
                    Size = new Size(Scale(132), Scale(62)), AccessibleName = skill.Name,
                    AccessibleRole = AccessibleRole.ListItem
                };
                tile.Bind(skill, "");
                _tips.SetToolTip(tile, $"{skill.Name} · slota sürükle");
                tile.DragStarted += (_, _) => BeginSkillDrag(tile, skill.Id, null);
                _palette.Controls.Add(tile);
            }
            if (_palette.Controls.Count == 0)
                _palette.Controls.Add(new Label { Text = "Bu filtrede skill yok.", AutoSize = true, ForeColor = UiTheme.Muted });
        }
        finally { _palette.ResumeLayout(true); }
    }

    private int Scale(int pixels) => (int)Math.Round(pixels * DeviceDpi / 96f);

    private void BeginSkillDrag(Control source, string id, SkillAddress? address)
    {
        if (!_allowed.Contains(id) || !_skills.ContainsKey(id)) return;
        _dragSource = address;
        _dragId = id;
        try { source.DoDragDrop(id, address == null ? DragDropEffects.Copy : DragDropEffects.Move); }
        finally
        {
            _dragSource = null;
            _dragId = null;
            foreach (var tile in _slotTiles) tile.SetDropHighlight(false);
        }
    }

    private string? ValidDropId(DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(typeof(string)) != true) return null;
        string? id = e.Data.GetData(typeof(string)) as string;
        if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || id != id.Trim() ||
            !_allowed.Contains(id) || !_skills.ContainsKey(id)) return null;
        if (_dragSource != null && (_dragId != id || IdAt(_dragSource.Bar, _dragSource.Slot) != id)) return null;
        return id;
    }

    private DragDropEffects DropEffect(DragEventArgs e)
    {
        if (ValidDropId(e) == null) return DragDropEffects.None;
        var desired = _dragSource == null ? DragDropEffects.Copy : DragDropEffects.Move;
        return (e.AllowedEffect & desired) != 0 ? desired : DragDropEffects.None;
    }

    private void PreviewDrop(SkillTile tile, int slot, DragEventArgs e)
    {
        e.Effect = DropEffect(e);
        bool same = _dragSource == new SkillAddress(_selectedBar, slot);
        tile.SetDropHighlight(e.Effect != DragDropEffects.None && !same);
        if (e.Effect == DragDropEffects.None) { _feedback.Text = "Bu job için geçersiz skill; bırakma reddedilir."; return; }
        string? target = IdAt(_selectedBar, slot);
        _feedback.Text = same ? "Aynı slot · değişiklik yok" : target == null ? "Boş slota bırak" :
            $"Üzerine yazılacak: {NameOf(target)} → {NameOf(ValidDropId(e))}";
    }

    private void DropOnSlot(SkillTile tile, int slot, DragEventArgs e)
    {
        tile.SetDropHighlight(false);
        e.Effect = DropEffect(e);
        string? id = ValidDropId(e);
        if (id == null || e.Effect == DragDropEffects.None) return;
        var target = new SkillAddress(_selectedBar, slot);
        if (_dragSource == target) return;
        string? previous = IdAt(target.Bar, target.Slot);
        if (_dragSource == null && previous == id) return;
        var draft = _layout.Clone();
        // Move is atomic: a rejected assignment must never clear its source.
        if (_dragSource != null) draft.Clear(_dragSource.Bar, _dragSource.Slot);
        try
        {
            draft.Assign(target.Bar, target.Slot, id, _allowed);
            draft.Validate(_allowed);
        }
        catch (ArgumentException ex) { e.Effect = DragDropEffects.None; _feedback.Text = ex.Message; return; }
        _layout = draft;
        _dragSource = null;
        _dragId = null;
        RenderSlots();
        _feedback.Text = previous == null ? $"{target}: {NameOf(id)}" :
            $"{target}: {NameOf(previous)} yerine {NameOf(id)} yerleştirildi.";
        Changed();
    }

    private void ClearSlot(int slot)
    {
        if (IdAt(_selectedBar, slot) == null) return;
        _layout.Clear(_selectedBar, slot);
        RenderSlots();
        _feedback.Text = $"{new SkillAddress(_selectedBar, slot)} temizlendi.";
        Changed();
    }

    private void Changed()
    {
        UpdateSaveState();
        if (!_loading) LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateSaveState()
    {
        bool saved = _saved != null && _saved.VisibleBars == _layout.VisibleBars &&
            _saved.Assignments.Count == _layout.Assignments.Count &&
            _saved.Assignments.All(a => IdAt(a.Bar, a.Slot) == a.SkillId);
        _state.Text = saved ? "Kaydedildi" : "Kaydedilmedi";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tips.Dispose();
        base.Dispose(disposing);
    }

    private sealed class SkillTile : Control
    {
        private Image? _image;
        private string? _iconFile;
        private string _name = "Boş";
        private string _key = "";
        private bool _highlight;
        private Point? _pressed;
        public event EventHandler? DragStarted;

        public SkillTile()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = UiTheme.Slot;
            ForeColor = UiTheme.Text;
            Cursor = Cursors.Hand;
            Margin = new Padding(3);
        }

        public void Bind(JobSkillDef? skill, string key)
        {
            _name = skill?.Name ?? "Boş";
            _key = key;
            if (_iconFile != skill?.IconFile)
            {
                _image?.Dispose();
                _image = null;
                _iconFile = skill?.IconFile;
                // Exact catalog asset only. Legacy LoadIcon probes other jobs' filenames.
                if (!string.IsNullOrWhiteSpace(_iconFile))
                {
                    string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "images")) + Path.DirectorySeparatorChar;
                    try
                    {
                        string path = Path.GetFullPath(Path.Combine(root, _iconFile));
                        if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                        {
                            using var original = Image.FromFile(path);
                            _image = new Bitmap(original);
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or
                        OutOfMemoryException or System.Runtime.InteropServices.ExternalException or NotSupportedException)
                    { /* Missing or corrupt assets use the actual skill name, never another icon. */ }
                }
            }
            SetDropHighlight(false);
            Invalidate();
        }

        public void SetDropHighlight(bool highlighted)
        {
            if (_highlight == highlighted) return;
            _highlight = highlighted;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            _pressed = e.Button == MouseButtons.Left ? e.Location : null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_pressed is not Point start || e.Button != MouseButtons.Left) return;
            Size distance = SystemInformation.DragSize;
            if (new Rectangle(start.X - distance.Width / 2, start.Y - distance.Height / 2,
                distance.Width, distance.Height).Contains(e.Location)) return;
            _pressed = null;
            DragStarted?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseUp(MouseEventArgs e) { _pressed = null; base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            float scale = DeviceDpi / 96f;
            int pad = Math.Max(2, (int)(4 * scale));
            int header = _key.Length == 0 ? 0 : (int)(18 * scale);
            using var border = new Pen(_highlight ? Color.Gold : Focused ? UiTheme.Text : UiTheme.GoldDim,
                _highlight ? 2 * scale : scale);
            e.Graphics.DrawRectangle(border, 1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
            if (_key.Length != 0)
                TextRenderer.DrawText(e.Graphics, _key, Font, new Rectangle(pad, 1, Width - pad * 2, header),
                    UiTheme.Text, UiTheme.KeyChip, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            int imageHeight = _image == null ? 0 : Math.Min((int)(32 * scale), Math.Max(0, Height - header - (int)(26 * scale)));
            if (_image != null && imageHeight > 0)
            {
                float ratio = Math.Min((float)(Width - pad * 2) / _image.Width, (float)imageHeight / _image.Height);
                int w = (int)(_image.Width * ratio), h = (int)(_image.Height * ratio);
                e.Graphics.DrawImage(_image, new Rectangle((Width - w) / 2, header + pad, w, h));
            }
            int top = header + pad + imageHeight;
            TextRenderer.DrawText(e.Graphics, _name, Font, new Rectangle(pad, top, Math.Max(0, Width - pad * 2), Math.Max(0, Height - top - pad)),
                _name == "Boş" ? UiTheme.Muted : ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _image?.Dispose(); _image = null; }
            base.Dispose(disposing);
        }
    }
}
