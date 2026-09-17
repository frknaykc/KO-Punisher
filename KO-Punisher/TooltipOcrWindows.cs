using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace KOPunisher;

internal enum TooltipOcrMode { Mask, Color, SoftContrast, TesseractColor, TesseractSoftContrast }

internal static class TooltipOcrWindows
{
    internal static async Task<(string Text, Bitmap Processed)> ReadAsync(Bitmap panel, CancellationToken token, TooltipOcrMode mode = TooltipOcrMode.TesseractSoftContrast)
    {
        ArgumentNullException.ThrowIfNull(panel);
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows OCR is required.");
        bool useTesseract = mode is TooltipOcrMode.TesseractColor or TooltipOcrMode.TesseractSoftContrast;
        var engine = useTesseract ? null : OcrEngine.TryCreateFromLanguage(new Language("en-US"))
            ?? OcrEngine.TryCreateFromLanguage(new Language("en"));
        if (!useTesseract && engine == null) throw new InvalidOperationException("Install the Windows English OCR language capability (en-US).");
        using var tesseract = useTesseract
            ? new Tesseract.TesseractEngine(Path.Combine(AppContext.BaseDirectory, "tessdata"), "eng", Tesseract.EngineMode.LstmOnly)
            : null;
        var renderMode = mode == TooltipOcrMode.TesseractColor ? TooltipOcrMode.Color
            : mode == TooltipOcrMode.TesseractSoftContrast ? TooltipOcrMode.SoftContrast : mode;
        int maximum = useTesseract ? 8192 : (int)OcrEngine.MaxImageDimension;
        var prepared = TooltipTextImage.Prepare(ReadRgb(panel), panel.Width, panel.Height);
        var inputs = new List<(string Label, Bitmap Image)>();
        var texts = new List<(string Label, string Text)>();
        Bitmap? montage = null;
        try
        {
            foreach (var region in prepared.Regions)
            {
                token.ThrowIfCancellationRequested();
                var image = mode == TooltipOcrMode.Mask
                    ? Render(prepared, region, maximum)
                    : RenderContinuous(panel, region, maximum, renderMode);
                inputs.Add((region.Label, image)); // ownership is established before any await
                using var png = new MemoryStream();
                image.Save(png, ImageFormat.Png);
                if (tesseract != null)
                {
                    // Each detected band is one line (PSM 7). No character substitutions.
                    using var pix = Tesseract.Pix.LoadFromMemory(png.ToArray());
                    string line = await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        using var page = tesseract.Process(pix, Tesseract.PageSegMode.SingleLine);
                        return page.GetText().Trim();
                    }, token);
                    token.ThrowIfCancellationRequested();
                    texts.Add((region.Label, line));
                    continue;
                }
                png.Position = 0;
                using var stream = png.AsRandomAccessStream();
                var decoder = await BitmapDecoder.CreateAsync(stream);
                token.ThrowIfCancellationRequested();
                using var decoded = await decoder.GetSoftwareBitmapAsync();
                using var software = SoftwareBitmap.Convert(decoded, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                token.ThrowIfCancellationRequested();
                var result = await engine!.RecognizeAsync(software);
                token.ThrowIfCancellationRequested();
                string text = string.Join("\n", result.Lines.OrderBy(l => l.Words.Count == 0 ? 0 : l.Words.Min(w => w.BoundingRect.Y))
                    .Select(l => l.Text));
                texts.Add((region.Label, text));
            }
            montage = Montage(inputs, texts);
            token.ThrowIfCancellationRequested();
            string combined = TooltipTextImage.Compose(prepared.HeaderKnown, texts);
            var resultBitmap = montage;
            montage = null;
            return (combined, resultBitmap);
        }
        finally
        {
            montage?.Dispose();
            foreach (var input in inputs) input.Image.Dispose();
        }
    }

    private static byte[] ReadRgb(Bitmap source)
    {
        using var copy = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(copy)) g.DrawImageUnscaled(source, 0, 0);
        var data = copy.LockBits(new Rectangle(0, 0, copy.Width, copy.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            var rgb = new byte[checked(copy.Width * copy.Height * 3)];
            var row = new byte[copy.Width * 3];
            for (int y = 0; y < copy.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                for (int x = 0; x < copy.Width; x++)
                {
                    int i = (y * copy.Width + x) * 3;
                    rgb[i] = row[x * 3 + 2]; rgb[i + 1] = row[x * 3 + 1]; rgb[i + 2] = row[x * 3];
                }
            }
            return rgb;
        }
        finally { copy.UnlockBits(data); }
    }

    private static Bitmap Render(TooltipTextImage.Prepared source, TooltipTextImage.Region region, int maximum)
    {
        // Target ~36 px glyph height, retain source geometry and punctuation; no morphology.
        const int padding = 16;
        double scale = Math.Min(4, Math.Max(1, 36.0 / region.Height));
        scale = Math.Min(scale, (maximum - 2.0 * padding) / Math.Max(region.Width, region.Height));
        int width = Math.Max(1, (int)Math.Floor(region.Width * scale));
        int height = Math.Max(1, (int)Math.Floor(region.Height * scale));
        var output = new Bitmap(width + 2 * padding, height + 2 * padding, PixelFormat.Format24bppRgb);
        try
        {
            using (var g = Graphics.FromImage(output)) g.Clear(Color.White);
            var data = output.LockBits(new Rectangle(0, 0, output.Width, output.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                var row = new byte[Math.Abs(data.Stride)];
                for (int y = 0; y < output.Height; y++)
                {
                    Array.Fill(row, (byte)255);
                    if (y >= padding && y < padding + height)
                    {
                        int sy = region.Y + Math.Min(region.Height - 1, (int)((y - padding) / scale));
                        for (int x = 0; x < width; x++)
                        {
                            int sx = region.X + Math.Min(region.Width - 1, (int)(x / scale));
                            if (source.Mask[sy * source.Width + sx] != 0)
                                row[(x + padding) * 3] = row[(x + padding) * 3 + 1] = row[(x + padding) * 3 + 2] = 0;
                        }
                    }
                    Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * data.Stride), row.Length);
                }
            }
            finally { output.UnlockBits(data); }
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    private static Bitmap RenderContinuous(Bitmap panel, TooltipTextImage.Region region, int maximum, TooltipOcrMode mode)
    {
        var area = Rectangle.Intersect(new Rectangle(0, 0, panel.Width, panel.Height),
            new Rectangle(region.X - 2, region.Y - 2, region.Width + 4, region.Height + 4));
        using var crop = panel.Clone(area, PixelFormat.Format24bppRgb);
        if (mode == TooltipOcrMode.SoftContrast)
        {
            for (int y = 0; y < crop.Height; y++)
            for (int x = 0; x < crop.Width; x++)
            {
                var c = crop.GetPixel(x, y);
                int brightness = Math.Max(c.R, Math.Max(c.G, c.B));
                int ink = 255 - Math.Clamp((brightness - 55) * 255 / 170, 0, 255);
                crop.SetPixel(x, y, Color.FromArgb(ink, ink, ink));
            }
        }
        const int padding = 16;
        double scale = Math.Min(4, (maximum - 2.0 * padding) / Math.Max(crop.Width, crop.Height));
        int width = Math.Max(1, (int)(crop.Width * scale)), height = Math.Max(1, (int)(crop.Height * scale));
        var output = new Bitmap(width + padding * 2, height + padding * 2, PixelFormat.Format24bppRgb);
        try
        {
            using var g = Graphics.FromImage(output);
            g.Clear(mode == TooltipOcrMode.Color ? Color.Black : Color.White);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            using var attributes = new ImageAttributes();
            attributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
            g.DrawImage(crop, new Rectangle(padding, padding, width, height), 0, 0, crop.Width, crop.Height, GraphicsUnit.Pixel, attributes);
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    private static Bitmap Montage(List<(string Label, Bitmap Image)> inputs, List<(string Label, string Text)> texts)
    {
        const int labelHeight = 24;
        int width = Math.Max(320, inputs.Count == 0 ? 0 : inputs.Max(x => x.Image.Width));
        int height = Math.Max(48, inputs.Sum(x => x.Image.Height + labelHeight));
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        try
        {
            using var g = Graphics.FromImage(bitmap);
            using var font = new Font(FontFamily.GenericSansSerif, 9);
            g.Clear(Color.LightGray);
            if (inputs.Count == 0) g.DrawString("No text regions detected; no OCR inputs", font, Brushes.Black, 4, 4);
            int y = 0;
            bool afterType = false;
            for (int i = 0; i < inputs.Count; i++)
            {
                string label = inputs[i].Label;
                if (label == "header")
                {
                    if (TooltipTextImage.CanonicalType(texts[i].Text).Length > 0) { label = "type (exact recognition)"; afterType = true; }
                    else label = afterType ? "header detail" : "title candidate";
                }
                g.DrawString($"{i + 1}: {label} | English OCR | {inputs[i].Image.Width}x{inputs[i].Image.Height}", font, Brushes.Black, 4, y + 3);
                y += labelHeight;
                // Unscaled: these are exactly the bitmaps passed to OCR, not a recreation.
                g.DrawImageUnscaled(inputs[i].Image, 0, y);
                y += inputs[i].Image.Height;
            }
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }
}
