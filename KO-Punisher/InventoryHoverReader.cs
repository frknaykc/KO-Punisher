using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KOPunisher;

internal sealed class InventoryHoverReader
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref Point point);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    private readonly IntPtr _window;
    private readonly Rectangle _client;
    private Rectangle _inventory;
    private Point _park;
    public Rectangle InventoryBounds => _inventory;
    public int Columns { get; private set; } = 7;
    public int Rows { get; private set; } = 4;
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    private Point? _expected;
    private readonly CancellationToken _token;

    public InventoryHoverReader(Rectangle inventory, string processName, CancellationToken token)
    {
        _window = GetForegroundWindow();
        GetWindowThreadProcessId(_window, out uint pid);
        using var process = Process.GetProcessById((int)pid);
        if (!string.Equals(process.ProcessName, Path.GetFileNameWithoutExtension(processName), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tooltip taraması için oyun ön planda olmalı. Tara'ya basınca oyunu öne getir.");
        var ownIntegrity = ForegroundDiagnostics.GetIntegrity((uint)Environment.ProcessId);
        var gameIntegrity = ForegroundDiagnostics.GetIntegrity(pid);
        if (ownIntegrity.Rid is int own && gameIntegrity.Rid is int game && game > own)
            throw new InvalidOperationException("Oyun daha yüksek yetkiyle çalışıyor. KO-Punisher'ı kapatıp Yönetici olarak çalıştır; tarama başlamadı.");
        _client = ClientBounds(); _inventory = inventory; _token = token;
        if (!GetCursorPos(out var initial)) throw new InvalidOperationException("Fare konumu okunamadı.");
        _expected = initial;
        if (inventory.IsEmpty) return;
        SetInventory(inventory);
    }
    private void SetInventory(Rectangle inventory)
    {
        _inventory = inventory;
        if (!_client.Contains(inventory)) throw new InvalidOperationException("Envanter crop'u oyun penceresinin dışında; yeniden seç.");
        var candidates = new[] { new Point(_client.Left + 12, _client.Top + 12), new Point(_client.Right - 12, _client.Top + 12), new Point(_client.Left + 12, _client.Bottom - 12) };
        _park = candidates.FirstOrDefault(p => !inventory.Contains(p));
        if (inventory.Contains(_park) || !_client.Contains(_park)) throw new InvalidOperationException("Tooltip kapatmak için envanter dışında boşluk yok.");
    }
    public async Task DiscoverAsync(bool permitOpen)
    {
        Guard();
        InventoryDetection? Find()
        {
            Guard(); using var frame = Capture(_client);
            var found = InventoryVision.FindInventory(frame.Width, frame.Height, ToRgb(frame));
            Guard(); return found;
        }
        var detection = Find();
        if (detection == null && permitOpen)
        {
            // Explicit one-shot user permission is required: custom game chat focus cannot
            // be reliably established with Win32 GUI-thread focus alone.
            Guard();
            foreach (int key in new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C, 0x49, 0x0D })
                if ((GetAsyncKeyState(key) & 0x8000) != 0)
                    throw new InvalidOperationException("Tuş/modifier basılı; I gönderilmedi. Tuşları bırakıp tekrar dene.");
            Guard();
            keybd_event(0x49, 0, 0, UIntPtr.Zero);
            try { await WaitAsync(40); }
            finally { keybd_event(0x49, 0, 2, UIntPtr.Zero); }
            var clock = Stopwatch.StartNew();
            while (detection == null && clock.ElapsedMilliseconds < 2500)
            { await WaitAsync(150); detection = Find(); }
        }
        if (detection == null)
            throw new InvalidOperationException("Envanter bulunamadı. Oyunda envanteri açıp Envanteri tara'ya bas; sohbet kapalıysa açık izinli 'I ile aç ve tara' kullanılabilir. Algılama başarısızsa manuel crop yedeğini kullan.");
        Columns = detection.Columns; Rows = detection.Rows;
        var bounds = detection.Bounds; bounds.Offset(_client.Location); SetInventory(bounds);
    }
    internal static byte[] ToRgb(Bitmap image)
    {
        var data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var rgb = new byte[checked(image.Width * image.Height * 3)];
            var row = new byte[image.Width * 4];
            for (int y = 0; y < image.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                for (int x = 0; x < image.Width; x++)
                { int d = (y * image.Width + x) * 3, s = x * 4; rgb[d] = row[s + 2]; rgb[d + 1] = row[s + 1]; rgb[d + 2] = row[s]; }
            }
            return rgb;
        }
        finally { image.UnlockBits(data); }
    }
    private Rectangle ClientBounds()
    {
        var point = new Point();
        if (!GetClientRect(_window, out var r) || !ClientToScreen(_window, ref point)) throw new InvalidOperationException("Oyun pencere boyutu okunamadı.");
        return new Rectangle(point.X, point.Y, r.Right - r.Left, r.Bottom - r.Top);
    }
    public void Guard()
    {
        _token.ThrowIfCancellationRequested();
        if ((GetAsyncKeyState(0x1B) & 0x8000) != 0) throw new OperationCanceledException("ESC ile durduruldu.");
        if (GetForegroundWindow() != _window || ClientBounds() != _client) throw new InvalidOperationException("Oyun odağı/konumu değişti; tarama durdu.");
        if (_expected is Point p && (!GetCursorPos(out var current) || Math.Abs(current.X - p.X) > 4 || Math.Abs(current.Y - p.Y) > 4))
            throw new OperationCanceledException("Fare elle hareket ettirildi; tarama durdu.");
    }
    private void Move(Point point)
    {
        Guard();
        if (!SetCursorPos(point.X, point.Y))
        {
            int error = Marshal.GetLastWin32Error();
            throw new System.ComponentModel.Win32Exception(error, $"Fare konumlandırılamadı (Win32={error}, X={point.X}, Y={point.Y}).");
        }
        _expected = point; Guard();
    }
    private async Task WaitAsync(int ms)
    {
        for (int n = 0; n < ms; n += 50) { Guard(); await Task.Delay(Math.Min(50, ms - n), _token); }
        Guard();
    }
    public async Task ParkAsync() { Move(_park); await WaitAsync(120); }
    private async Task<Bitmap?> WaitPanelAsync(Point? hover, bool absent = false)
    {
        var clock = Stopwatch.StartNew(); Rectangle? previous = null; byte[]? previousPixels = null;
        int stable = 0;
        while (clock.ElapsedMilliseconds < 2800)
        {
            Guard(); using var frame = Capture(_client);
            var panel = InventoryVision.FindTooltip(frame.Width, frame.Height, ToRgb(frame), hover);
            Guard();
            if (absent)
            {
                if (panel == null) { if (++stable >= 2) return null; }
                else stable = 0;
            }
            else if (panel is Rectangle r && new Rectangle(Point.Empty, frame.Size).Contains(r) && r.Width > 0 && r.Height > 0)
            {
                using var crop = frame.Clone(r, PixelFormat.Format32bppArgb);
                var pixels = ToRgb(crop);
                bool same = previous == panel && previousPixels != null && Similar(previousPixels, pixels);
                stable = same ? stable + 1 : 0; previous = panel; previousPixels = pixels;
                if (stable >= 2) return new Bitmap(crop);
            }
            else { stable = 0; previous = null; previousPixels = null; }
            await WaitAsync(clock.ElapsedMilliseconds < 600 ? 80 : 160);
        }
        if (absent) throw new InvalidOperationException("Önceki tooltip kapanmadı; eski panel okunmadı.");
        return null;
    }
    private static bool Similar(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        long difference = 0; int count = 0;
        for (int i = 0; i < a.Length; i += 9) { difference += Math.Abs(a[i] - b[i]); count++; }
        return count > 0 && difference / (double)count < 3;
    }
    public Bitmap CaptureInventory() { Guard(); return Capture(_inventory); }
    private static Bitmap Capture(Rectangle r)
    {
        var image = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
        try { using var g = Graphics.FromImage(image); g.CopyFromScreen(r.Location, Point.Empty, r.Size); return image; }
        catch { image.Dispose(); throw; }
    }
    private async Task<(string Text, Bitmap Processed)> OcrAsync(Bitmap image)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_token);
        deadline.CancelAfter(TimeSpan.FromSeconds(12));
        // OCR owns its clone until the native operation settles, even after timeout/cancel.
        var owned = new Bitmap(image);
        async Task<(string Text, Bitmap Processed)> ReadOwnedAsync()
        { using (owned) return await TooltipOcrWindows.ReadAsync(owned, deadline.Token); }
        var task = ReadOwnedAsync();
        try
        {
            var clock = Stopwatch.StartNew();
            while (!task.IsCompleted)
            {
                Guard();
                if (clock.Elapsed > TimeSpan.FromSeconds(12)) throw new TimeoutException("OCR zaman aşımı; tarama durdu.");
                await Task.WhenAny(task, Task.Delay(50, _token));
            }
            var result = await task; Guard(); return result;
        }
        catch
        {
            deadline.Cancel();
            _ = task.ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion) t.Result.Processed.Dispose();
                else if (t.IsFaulted) _ = t.Exception;
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw;
        }
    }
    public async Task<(InventoryItemReading Reading, Bitmap Evidence)> ReadAsync(Rectangle localSlot, bool emptyMatch)
    {
        if (!new Rectangle(Point.Empty, _inventory.Size).Contains(localSlot))
            throw new InvalidOperationException("Slot envanter dışında.");
        await ParkAsync();
        await WaitPanelAsync(null, absent: true);
        using (var firstSlotFrame = CaptureInventory())
        {
            bool empty = emptyMatch && InventoryVision.IsEmptySlot(firstSlotFrame.Width, firstSlotFrame.Height, ToRgb(firstSlotFrame), localSlot);
            if (empty)
            {
                await WaitAsync(120);
                using var secondSlotFrame = CaptureInventory();
                if (InventoryVision.IsEmptySlot(secondSlotFrame.Width, secondSlotFrame.Height, ToRgb(secondSlotFrame), localSlot))
                    return (new InventoryItemReading("", null, "", "Boş (iki görüntüyle doğrulandı)"), secondSlotFrame.Clone(localSlot, PixelFormat.Format32bppArgb));
            }
        }
        var center = new Point(_inventory.X + localSlot.X + localSlot.Width / 2, _inventory.Y + localSlot.Y + localSlot.Height / 2);
        var localHover = new Point(center.X - _client.X, center.Y - _client.Y);
        Move(center);
        var image = await WaitPanelAsync(localHover);
        if (image == null)
        {
            // Missing panel is never evidence that a slot is empty.
            using var inventory = CaptureInventory();
            return (new InventoryItemReading("", null, "", "Belirsiz: tooltip paneli bulunamadı"), inventory.Clone(localSlot, PixelFormat.Format32bppArgb));
        }
        Bitmap? processed = null;
        try
        {
            var first = await OcrAsync(image); processed = first.Processed;
            var reading = InventoryTooltip.Read(first.Text, first.Text, false);
            if (!reading.Readable || (reading.Kind == "Upgrade Item" && reading.Level == null))
            {
                await WaitAsync(160);
                var retry = await WaitPanelAsync(localHover);
                if (retry != null)
                {
                    image.Dispose(); image = retry;
                    var second = await OcrAsync(image);
                    processed.Dispose(); processed = second.Processed;
                    reading = InventoryTooltip.Read(first.Text, second.Text, false);
                    // A failed first OCR must not poison every retry. Recovery requires
                    // two fresh agreeing readings; a single differing result is not accepted.
                    if (!reading.Readable && InventoryTooltip.Read(second.Text, second.Text, false).Readable)
                    {
                        var confirmation = await WaitPanelAsync(localHover);
                        if (confirmation != null)
                        {
                            image.Dispose(); image = confirmation;
                            var third = await OcrAsync(image);
                            processed.Dispose(); processed = third.Processed;
                            reading = InventoryTooltip.Read(second.Text, third.Text, false);
                        }
                    }
                }
            }
            return (reading, ComposeEvidence(image, processed));
        }
        finally { image.Dispose(); processed?.Dispose(); }
    }
    private static Bitmap ComposeEvidence(Bitmap original, Bitmap processed)
    {
        const int heading = 30, gap = 16;
        var evidence = new Bitmap(original.Width + processed.Width + gap, Math.Max(original.Height, processed.Height) + heading,
            PixelFormat.Format32bppArgb);
        try
        {
            using var g = Graphics.FromImage(evidence); g.Clear(Color.DimGray);
            g.DrawString("Orijinal tooltip", SystemFonts.DefaultFont, Brushes.White, 4, 6);
            g.DrawString("OCR girdileri", SystemFonts.DefaultFont, Brushes.White, original.Width + gap, 6);
            g.DrawImageUnscaled(original, 0, heading);
            g.DrawImageUnscaled(processed, original.Width + gap, heading);
            return evidence;
        }
        catch { evidence.Dispose(); throw; }
    }
    public static bool MatchesEmpty(Bitmap slot, Bitmap? reference)
    {
        if (reference == null || slot.Size != reference.Size) return false;
        var a = ToRgb(slot); var b = ToRgb(reference);
        long difference = 0, samples = 0;
        for (int y = 4; y < slot.Height - 4; y += 2)
            for (int x = 4; x < slot.Width - 4; x += 2)
            {
                int i = (y * slot.Width + x) * 3;
                difference += Math.Abs(a[i] - b[i]) + Math.Abs(a[i + 1] - b[i + 1]) + Math.Abs(a[i + 2] - b[i + 2]); samples += 3;
            }
        return samples > 0 && difference / (double)samples <= 2;
    }
}
