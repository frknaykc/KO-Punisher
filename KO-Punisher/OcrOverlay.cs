using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

internal sealed class OcrOverlay : Form
{
    private Point _a;
    private Rectangle _box;
    private bool _drag;

    public Rectangle ScreenRect { get; private set; }

    public OcrOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        BackColor = Color.Black;
        Opacity = 0.28;
        TopMost = true;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
        MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _drag = true;
            _a = e.Location;
            _box = new Rectangle(_a, Size.Empty);
        };
        MouseMove += (_, e) =>
        {
            if (!_drag) return;
            _box = Rect(_a, e.Location);
            Invalidate();
        };
        MouseUp += (_, e) =>
        {
            if (!_drag || e.Button != MouseButtons.Left) return;
            _drag = false;
            var r = Rect(_a, e.Location);
            if (r.Width < 8 || r.Height < 8) { DialogResult = DialogResult.Cancel; Close(); return; }
            var vs = SystemInformation.VirtualScreen;
            ScreenRect = new Rectangle(vs.X + r.X, vs.Y + r.Y, r.Width, r.Height);
            DialogResult = DialogResult.OK;
            Close();
        };
        Paint += (_, e) =>
        {
            e.Graphics.DrawString("Sürükleyerek kırp  ·  Esc iptal", UiTheme.Title, Brushes.White, 24, 24);
            if (_box.Width > 0)
            {
                using var pen = new Pen(UiTheme.Gold, 2);
                e.Graphics.DrawRectangle(pen, _box);
            }
        };
    }

    private static Rectangle Rect(Point a, Point b)
    {
        int x = Math.Min(a.X, b.X), y = Math.Min(a.Y, b.Y);
        return new Rectangle(x, y, Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
}
