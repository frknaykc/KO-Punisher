using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

internal sealed class InventoryCropForm : Form
{
    private readonly Bitmap _screen;
    private Point? _start;
    private Rectangle _selection;
    public Rectangle SelectedBounds { get; private set; }
    private readonly string _instruction;
    public InventoryCropForm(Bitmap screen, Rectangle desktop, string instruction = "Yalnız envanter slot ızgarasını sürükleyerek seç · ESC: iptal")
    {
        _screen = screen;
        _instruction = instruction;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = desktop;
        TopMost = true; DoubleBuffered = true; KeyPreview = true;
        Cursor = Cursors.Cross;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
        MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _start = e.Location; Capture = true; } };
        MouseMove += (_, e) =>
        {
            if (_start is not Point p) return;
            _selection = Rectangle.FromLTRB(Math.Min(p.X, e.X), Math.Min(p.Y, e.Y), Math.Max(p.X, e.X), Math.Max(p.Y, e.Y));
            Invalidate();
        };
        MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || _start == null) return;
            Capture = false; _start = null;
            if (_selection.Width < 16 || _selection.Height < 16) return;
            SelectedBounds = new Rectangle(desktop.X + _selection.X, desktop.Y + _selection.Y, _selection.Width, _selection.Height);
            DialogResult = DialogResult.OK; Close();
        };
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(_screen, 0, 0);
        using var shade = new SolidBrush(Color.FromArgb(90, Color.Black));
        e.Graphics.FillRectangle(shade, ClientRectangle);
        if (!_selection.IsEmpty)
        {
            e.Graphics.DrawImage(_screen, _selection, _selection, GraphicsUnit.Pixel);
            using var pen = new Pen(Color.Lime, 2);
            e.Graphics.DrawRectangle(pen, _selection);
        }
        e.Graphics.DrawString(_instruction, Font, Brushes.White, 20, 20);
    }
}
