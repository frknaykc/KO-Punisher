using System.Drawing;
using System.Drawing.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace KOPunisher;

internal static class OcrReader
{
    public static async Task<string> ReadAsync(Rectangle screen, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows() || screen.Width < 8 || screen.Height < 8) return "";
        using var bmp = new Bitmap(screen.Width, screen.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(screen.Location, Point.Empty, screen.Size);
        return await ReadBitmapAsync(bmp, token);
    }

    internal static async Task<string> ReadBitmapAsync(Bitmap bmp, CancellationToken token, bool preserveLines = false)
    {
        token.ThrowIfCancellationRequested();
        using var png = new MemoryStream();
        bmp.Save(png, ImageFormat.Png);
        png.Position = 0;
        token.ThrowIfCancellationRequested();
        var engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en"));
        if (engine == null) throw new InvalidOperationException("Windows OCR dili yüklü değil.");
        using var stream = png.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        using var software = await decoder.GetSoftwareBitmapAsync();
        var result = await engine.RecognizeAsync(software);
        token.ThrowIfCancellationRequested();
        return preserveLines ? string.Join('\n', result.Lines.Select(line => line.Text)) : result.Text ?? "";
    }

    public static bool LooksFailed(string text, string phrase)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string t = text.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
        string p = (phrase ?? "castingfailed").Replace(" ", "", StringComparison.OrdinalIgnoreCase);
        return t.Contains(p, StringComparison.OrdinalIgnoreCase)
            || t.Contains("castingfailed", StringComparison.OrdinalIgnoreCase)
            || t.Contains("castfailed", StringComparison.OrdinalIgnoreCase);
    }
}
