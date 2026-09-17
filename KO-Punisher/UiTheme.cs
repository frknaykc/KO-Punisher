using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

internal static class UiTheme
{
    public static readonly Color Bg = Color.FromArgb(14, 14, 14);
    public static readonly Color Panel = Color.FromArgb(26, 26, 26);
    public static readonly Color PanelAlt = Color.FromArgb(40, 40, 40);
    public static readonly Color Line = Color.FromArgb(168, 28, 28);
    public static readonly Color Gold = Color.FromArgb(210, 36, 36);
    public static readonly Color GoldDim = Color.FromArgb(120, 24, 24);
    public static readonly Color Text = Color.FromArgb(245, 245, 245);
    public static readonly Color Muted = Color.FromArgb(158, 158, 158);
    public static readonly Color Slot = Color.FromArgb(48, 48, 48);
    public static readonly Color KeyChip = Color.FromArgb(176, 28, 28);
    public static readonly Color Danger = Color.FromArgb(200, 40, 40);
    public static readonly Color Ok = Color.FromArgb(176, 28, 28);

    public static Font Body { get; } = new("Segoe UI", 9f);
    public static Font Title { get; } = new("Segoe UI Semibold", 11f);
    public static Font Small { get; } = new("Segoe UI", 8f);

    public static void PaintDark(Control root)
    {
        root.BackColor = Bg;
        root.ForeColor = Text;
        root.Font = Body;
    }

    public static Button NavButton(string text)
    {
        var b = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Height = 28,
            AutoSize = true,
            MinimumSize = new Size(72, 28),
            ForeColor = Text,
            BackColor = Panel,
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 0, 12, 0)
        };
        b.FlatAppearance.BorderColor = Line;
        b.FlatAppearance.MouseOverBackColor = PanelAlt;
        return b;
    }

    public static Button SideButton(string text)
    {
        var b = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Height = 34,
            Dock = DockStyle.Top,
            ForeColor = Text,
            BackColor = Panel,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = PanelAlt;
        return b;
    }

    public static void StyleField(TextBox box)
    {
        box.BackColor = Slot;
        box.ForeColor = Text;
        box.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleNumeric(NumericUpDown box)
    {
        box.BackColor = Slot;
        box.ForeColor = Text;
        box.BorderStyle = BorderStyle.FixedSingle;
    }

    public static Label Hint(string text) => new()
    {
        Text = text,
        ForeColor = Muted,
        AutoSize = true,
        MaximumSize = new Size(360, 0)
    };
}
