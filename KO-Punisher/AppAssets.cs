using System.Drawing;
using System.Drawing.Imaging;

namespace KOPunisher;

internal static class AppAssets
{
    private static Image? _logo;
    private static Image? _homeArt;

    public static Image? Logo => _logo ??= CropAlpha(SkillCatalog.LoadIcon("app-logo.png"));
    public static Image? HomeArt => _homeArt ??= KnockoutChecker(SkillCatalog.LoadIcon("gui-ko-object.png"));

    private static Bitmap? ToArgb(Image? src)
    {
        if (src == null) return null;
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.DrawImage(src, 0, 0, src.Width, src.Height);
        }
        src.Dispose();
        return bmp;
    }

    private static Image? CropAlpha(Image? src)
    {
        using var bmp = ToArgb(src);
        if (bmp == null) return null;
        int w = bmp.Width, h = bmp.Height;
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var px = new byte[Math.Abs(data.Stride) * h];
        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, px.Length);
        int stride = Math.Abs(data.Stride);
        bmp.UnlockBits(data);
        int l = w, t = h, r = 0, b = 0;
        for (int y = 0; y < h; y++)
        {
            int row = y * stride;
            for (int x = 0; x < w; x++)
            {
                if (px[row + x * 4 + 3] < 8) continue;
                if (x < l) l = x;
                if (y < t) t = y;
                if (x > r) r = x;
                if (y > b) b = y;
            }
        }
        if (r <= l || b <= t) return new Bitmap(bmp);
        int pad = 8;
        l = Math.Max(0, l - pad);
        t = Math.Max(0, t - pad);
        r = Math.Min(w - 1, r + pad);
        b = Math.Min(h - 1, b + pad);
        return bmp.Clone(new Rectangle(l, t, r - l + 1, b - t + 1), PixelFormat.Format32bppArgb);
    }

    private static Image? KnockoutChecker(Image? src)
    {
        var bmp = ToArgb(src);
        if (bmp == null) return null;
        int w = bmp.Width, h = bmp.Height;
        var seen = new bool[w * h];
        var stack = new Stack<Point>();
        void TryPush(int x, int y)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
            int i = y * w + x;
            if (seen[i]) return;
            Color c = bmp.GetPixel(x, y);
            if (c.A == 0 || !IsFringe(c)) return;
            seen[i] = true;
            stack.Push(new Point(x, y));
        }
        for (int x = 0; x < w; x++) { TryPush(x, 0); TryPush(x, h - 1); }
        for (int y = 0; y < h; y++) { TryPush(0, y); TryPush(w - 1, y); }
        while (stack.Count > 0)
        {
            var p = stack.Pop();
            bmp.SetPixel(p.X, p.Y, Color.Transparent);
            TryPush(p.X - 1, p.Y);
            TryPush(p.X + 1, p.Y);
            TryPush(p.X, p.Y - 1);
            TryPush(p.X, p.Y + 1);
        }
        return bmp;
    }

    private static bool IsFringe(Color c)
    {
        int max = Math.Max(c.R, Math.Max(c.G, c.B));
        int min = Math.Min(c.R, Math.Min(c.G, c.B));
        return max >= 140 && max - min <= 16;
    }
}
