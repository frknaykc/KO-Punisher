using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace KOPunisher;

internal static class GameWindow
{
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref Point point);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    internal static IntPtr RequireForeground(string processName)
    {
        IntPtr window = GetForegroundWindow();
        if (window == IntPtr.Zero) throw new InvalidOperationException("Oyun ön planda değil.");
        GetWindowThreadProcessId(window, out uint id);
        using var process = Process.GetProcessById((int)id);
        if (!process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Hedef oyun ön planda değil.");
        return window;
    }
    internal static Rectangle ClientBounds(IntPtr window)
    {
        var origin = new Point();
        if (IsIconic(window) || !GetClientRect(window, out var rect) || !ClientToScreen(window, ref origin) || rect.Right < 8 || rect.Bottom < 8)
            throw new InvalidOperationException("Oyun istemci alanı okunamadı.");
        return new Rectangle(origin, new Size(rect.Right, rect.Bottom));
    }
}

internal sealed class WindowMonsterReader(string process, MonsterFilterSettings settings) : IMonsterReader
{
    private readonly MonsterFilterSettings _settings = Settings.Snapshot(settings);
    public async Task<string> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        IntPtr window = GameWindow.RequireForeground(process);
        Rectangle bounds = GameWindow.ClientBounds(window);
        var r = _settings.Region;
        if (_settings.ClientWidth != bounds.Width || _settings.ClientHeight != bounds.Height)
            throw new InvalidOperationException("Oyun boyutu değişti veya bölge kalibre edilmedi; hedef adını yeniden kırpın.");
        var crop = new Rectangle(bounds.X + r.X, bounds.Y + r.Y, r.W, r.H);
        if (!bounds.Contains(crop) || !System.Windows.Forms.SystemInformation.VirtualScreen.Contains(crop))
            throw new InvalidOperationException("Hedef adı bölgesi oyun/ekran dışında.");
        string text = await OcrReader.ReadAsync(crop, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        // OCR sırasında pencere/yerleşim değiştiyse önceki resim izin sayılamaz.
        if (GameWindow.RequireForeground(process) != window || GameWindow.ClientBounds(window) != bounds)
            throw new InvalidOperationException("OCR sırasında oyun penceresi değişti; tekrar başlatın.");
        return text;
    }
}

internal sealed class WindowsChatEncoder : IChatEncoder
{
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint threadId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern short VkKeyScanEx(char ch, IntPtr layout);
    [DllImport("user32.dll")] private static extern short GetKeyState(int key);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint MapVirtualKeyEx(uint code, uint type, IntPtr layout);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int ToUnicodeEx(uint vk, uint scan, byte[] state, StringBuilder text, int length, uint flags, IntPtr layout);
    private readonly IntPtr _window, _layout;
    private readonly uint _thread;
    public WindowsChatEncoder(string process)
    {
        _window = GameWindow.RequireForeground(process);
        _thread = GameWindow.GetWindowThreadProcessId(_window, out _);
        _layout = GetKeyboardLayout(_thread);
        if (!IsCurrent) throw new InvalidOperationException("Caps Lock kapalı ve oyun ön planda olmalı.");
    }
    public bool IsCurrent => GameWindow.GetForegroundWindow() == _window && GetKeyboardLayout(_thread) == _layout && (GetKeyState(0x14) & 1) == 0;
    public IReadOnlyList<ChatStroke> Encode(string text)
    {
        var result = new List<ChatStroke>();
        foreach (char ch in text)
        {
            short mapped = VkKeyScanEx(ch, _layout);
            ushort vk = (ushort)(mapped & 0xff);
            int modifiers = (mapped >> 8) & 0xff;
            string? key = InputSender.KeyName(vk);
            // AltGr/dead-key kombinasyonları oyun kısayollarına dönüşebilir; sessiz bozmak yerine reddet.
            if (mapped == -1 || key == null || (modifiers & ~1) != 0)
                throw new ArgumentException($"'{ch}' mevcut oyun klavye düzeninde güvenle yazılamıyor. Metni veya klavye düzenini değiştirin.");
            var state = new byte[256];
            if ((modifiers & 1) != 0) state[0x10] = 0x80;
            var decoded = new StringBuilder(8);
            int count = ToUnicodeEx(vk, MapVirtualKeyEx(vk, 0, _layout), state, decoded, decoded.Capacity, 4, _layout);
            if (count != 1 || decoded[0] != ch)
                throw new ArgumentException($"'{ch}' doğrudan tuşla yazılamıyor; birleşik/dead-key karakter kullanmayın.");
            result.Add(new ChatStroke(key, (modifiers & 1) != 0, false, false));
        }
        return result;
    }
}
