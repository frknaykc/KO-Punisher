using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

public sealed class SkillLayoutPreviewControl : UserControl
{
    private SkillLayout? _layout;
    private ClassType _job;
    private readonly ComboBox _bar = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToolTip _tips = new();

    public SkillLayoutPreviewControl()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Panel;
        ForeColor = UiTheme.Text;
        MinimumSize = new Size(200, 480);
        Width = 220;
        _bar.BackColor = UiTheme.Slot;
        _bar.ForeColor = UiTheme.Text;
        _bar.FlatStyle = FlatStyle.Flat;
        _bar.SelectedIndexChanged += (_, _) => Invalidate();
        Controls.Add(_bar);
    }

    public void LoadLayout(ClassType job, SkillLayout? layout)
    {
        _job = job;
        _layout = layout?.Clone();
        int selected = Math.Max(0, _bar.SelectedIndex);
        _bar.Items.Clear();
        int bars = _layout?.VisibleBars ?? 1;
        for (int i = 1; i <= bars; i++) _bar.Items.Add("F" + i);
        _bar.SelectedIndex = Math.Min(selected, _bar.Items.Count - 1);
        if (_bar.SelectedIndex < 0 && _bar.Items.Count > 0) _bar.SelectedIndex = 0;
        Invalidate();
    }

    internal static Rectangle SlotBounds(int width, int slotIndex)
    {
        int size = Math.Min(40, Math.Max(28, width - 116));
        return new Rectangle(12, 58 + slotIndex * 42, size, 38);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        TextRenderer.DrawText(e.Graphics, "Skill Bar", UiTheme.Title, new Rectangle(10, 30, Width - 20, 24),
            UiTheme.Gold, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        if (_layout == null || _layout.Assignments.Count == 0)
        {
            TextRenderer.DrawText(e.Graphics, "Yerleşim yok", Font, new Rectangle(10, 58, Width - 20, 40),
                UiTheme.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak);
            return;
        }

        int selectedBar = Math.Max(1, _bar.SelectedIndex + 1);
        var skills = JobSkillCatalog.ForJob(_job).ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        for (int slot = 1; slot <= 10; slot++)
        {
            Rectangle rect = SlotBounds(Width, slot - 1);
            using var fill = new SolidBrush(UiTheme.Slot);
            e.Graphics.FillRectangle(fill, rect);
            e.Graphics.DrawRectangle(Pens.DimGray, rect);
            string key = slot == 10 ? "0" : slot.ToString();
            TextRenderer.DrawText(e.Graphics, key, UiTheme.Small, new Rectangle(rect.Right + 6, rect.Y, 24, rect.Height),
                UiTheme.Gold, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            string? id = _layout.Assignments.FirstOrDefault(a => a.Bar == selectedBar && a.Slot == slot)?.SkillId;
            string name = "Boş";
            if (id != null && skills.TryGetValue(id, out var skill))
            {
                name = skill.Name;
                using Image? icon = LoadIcon(skill.IconFile);
                if (icon != null)
                    e.Graphics.DrawImage(icon, new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6));
            }
            TextRenderer.DrawText(e.Graphics, name, Font,
                new Rectangle(rect.Right + 36, rect.Y, Math.Max(20, Width - rect.Right - 42), rect.Height),
                name == "Boş" ? UiTheme.Muted : UiTheme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private static Image? LoadIcon(string iconFile)
    {
        if (string.IsNullOrWhiteSpace(iconFile)) return null;
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "images")) + Path.DirectorySeparatorChar;
        try
        {
            string path = Path.GetFullPath(Path.Combine(root, iconFile));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return null;
            using var source = Image.FromFile(path);
            return new Bitmap(source);
        }
        catch { return null; }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tips.Dispose();
        base.Dispose(disposing);
    }
}
