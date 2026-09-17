using System.Runtime.InteropServices;

namespace KOPunisher;

/// <summary>
/// Windows SendInput API kullanarak klavye inputu gönderir.
/// Input yalnızca ön plandaki pencereye gider; oyun kabulü garanti edilmez.
/// </summary>
public static class InputSender
{
    public enum InputMethod { SendInputScanCode, LegacyKeybdEvent }
    private static InputMethod _method = InputMethod.LegacyKeybdEvent;
    public static InputMethod Method
    {
        get { lock (KeyLock) return _method; }
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            lock (KeyLock)
            {
                if (HeldKeys.Count != 0) throw new InvalidOperationException("Önce basılı tuşları bırak / makroyu durdur.");
                _method = value;
            }
        }
    }

    [DllImport("user32.dll", EntryPoint = "keybd_event")]
    private static extern void KeybdEvent(byte vk, byte scan, uint flags, IntPtr extraInfo);

    // ---------- Windows API tanımları ----------

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUnion u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    // ---------- Sabitler ----------

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const ushort KEYEVENTF_KEYDOWN = 0x0000;
    private const ushort KEYEVENTF_KEYUP = 0x0002;

    // ---------- VK Kodları (Türkçe Q Klavye) ----------

    private static readonly Dictionary<string, ushort> VkCodes = new()
    {
        // F tuşları (F1-F20)
        { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
        { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
        { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },
        { "F13", 0x7C }, { "F14", 0x7D }, { "F15", 0x7E }, { "F16", 0x7F },
        { "F17", 0x80 }, { "F18", 0x81 }, { "F19", 0x82 }, { "F20", 0x83 },

        { "ENTER", 0x0D }, { "ESCAPE", 0x1B },
        { "OEM1", 0xBA }, { "OEMPLUS", 0xBB }, { "OEMCOMMA", 0xBC }, { "OEMMINUS", 0xBD },
        { "OEMPERIOD", 0xBE }, { "OEM2", 0xBF }, { "OEM3", 0xC0 }, { "OEM4", 0xDB },
        { "OEM5", 0xDC }, { "OEM6", 0xDD }, { "OEM7", 0xDE }, { "OEM102", 0xE2 },
        { "SPACE", 0x20 }, { "XBUTTON1", 0x05 }, { "XBUTTON2", 0x06 },
        { "SHIFT", 0x10 }, { "CTRL", 0x11 }, { "ALT", 0x12 }, { "CAPSLOCK", 0x14 },

        // Harfler
        { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 },
        { "E", 0x45 }, { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 },
        { "I", 0x49 }, { "J", 0x4A }, { "K", 0x4B }, { "L", 0x4C },
        { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F }, { "P", 0x50 },
        { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
        { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 },
        { "Y", 0x59 }, { "Z", 0x5A },

        // Sayısal tuş takımı (üst sıra rakamlarından farklı VK kodları).
        { "NUMPAD0", 0x60 }, { "NUMPAD1", 0x61 }, { "NUMPAD2", 0x62 },
        { "NUMPAD3", 0x63 }, { "NUMPAD4", 0x64 }, { "NUMPAD5", 0x65 },
        { "NUMPAD6", 0x66 }, { "NUMPAD7", 0x67 }, { "NUMPAD8", 0x68 }, { "NUMPAD9", 0x69 },

        // Rakamlar
        { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 },
        { "4", 0x34 }, { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 },
        { "8", 0x38 }, { "9", 0x39 }
    };

    /// <summary>
    /// Tuş adını VK koduna çevirir. Bulamazsa false döner.
    /// Internal: HotkeyManager tarafından da kullanılır.
    /// </summary>
    internal static string? KeyName(ushort vk) => VkCodes.FirstOrDefault(p => p.Value == vk).Key;

    internal static bool TryGetVkCode(string keyName, out ushort vkCode)
    {
        return VkCodes.TryGetValue((keyName ?? "").Trim().ToUpperInvariant(), out vkCode);
    }

    /// <summary>
    /// Tek bir tuşa bas (KeyDown + KeyUp).
    /// </summary>
    public static void PressKey(string keyName)
    {
        if (!TryGetVkCode(keyName, out var vk) || vk is 0x05 or 0x06)
            throw new ArgumentException($"Desteklenmeyen klavye çıkış tuşu: {keyName}", nameof(keyName));

        KeyDown(vk);
        KeyUp(vk);
    }

    /// <summary>
    /// Tuşa bas (KeyDown) — serbest bırakılmaz.
    /// </summary>
    public static void KeyDown(string keyName)
    {
        if (!TryGetVkCode(keyName, out var vk) || vk is 0x05 or 0x06)
            throw new ArgumentException($"Desteklenmeyen klavye çıkış tuşu: {keyName}", nameof(keyName));
        KeyDown(vk);
    }

    private static readonly object KeyLock = new();
    private static readonly HashSet<ushort> HeldKeys = new();

    private static void SendChecked(ushort flags, ushort vk)
    {
        bool trace = InputDiagnostics.Active;
        var target = trace ? ForegroundDiagnostics.Current() : default;
        string method = _method.ToString();
        if (trace) InputDiagnostics.Record("send_attempt", new { method, vk, up = flags == KEYEVENTF_KEYUP, target });
        if (_method == InputMethod.LegacyKeybdEvent)
        {
            var keyboard = CreateLegacyKeyboardInput(flags, vk, MapVirtualKey(vk, 4));
            long legacyStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            KeybdEvent(checked((byte)keyboard.wVk), checked((byte)keyboard.wScan), keyboard.dwFlags, keyboard.dwExtraInfo);
            // keybd_event void döner: kabul / hata sayısı ölçülemez, başarı uydurma.
            if (trace) InputDiagnostics.Record("send_result", new
            {
                method, vk, scan = keyboard.wScan, flags = keyboard.dwFlags,
                requested = 1, accepted = (int?)null, win32Error = (int?)null,
                apiReturned = true, acceptance = "unknown_void_api",
                durationMs = System.Diagnostics.Stopwatch.GetElapsedTime(legacyStarted).TotalMilliseconds,
                targetBefore = target, targetAfter = ForegroundDiagnostics.Current()
            });
            return;
        }
        var input = CreateKeyboardInput(flags, vk);
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        uint accepted = SendInput(1, ref input, Marshal.SizeOf<INPUT>());
        int error = Marshal.GetLastWin32Error(); // Başka native çağrıdan önce al.
        if (trace) InputDiagnostics.Record("send_result", new
        {
            method, vk, scan = input.u.ki.wScan, flags = input.u.ki.dwFlags, requested = 1, accepted,
            win32Error = accepted == 1 ? (int?)null : error,
            durationMs = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            targetBefore = target, targetAfter = ForegroundDiagnostics.Current()
        });
        if (accepted != 1)
            throw new InvalidOperationException($"SendInput başarısız (Win32 {error}). Yetki farkı veya başka bir Windows giriş engeli olabilir; bu sonuç oyunun reddettiğini kanıtlamaz.");
    }

    private static void KeyDown(ushort vk)
    {
        lock (KeyLock)
        {
            SendChecked(KEYEVENTF_KEYDOWN, vk);
            HeldKeys.Add(vk);
        }
    }

    /// <summary>
    /// Tuşu bırak (KeyUp).
    /// </summary>
    public static void KeyUp(string keyName)
    {
        if (!TryGetVkCode(keyName, out var vk) || vk is 0x05 or 0x06)
            throw new ArgumentException($"Desteklenmeyen klavye çıkış tuşu: {keyName}", nameof(keyName));
        KeyUp(vk);
    }

    private static void KeyUp(ushort vk)
    {
        lock (KeyLock)
        {
            if (!HeldKeys.Contains(vk)) return;
            SendChecked(KEYEVENTF_KEYUP, vk);
            HeldKeys.Remove(vk);
        }
    }

    // Sadece uygulamanın bastığı tuşları bırak; fiziksel tuşlara müdahale etme.
    public static void CleanupAllKeys()
    {
        Exception? failure = null;
        lock (KeyLock)
        {
            foreach (ushort vk in HeldKeys.ToArray())
            {
                try { KeyUp(vk); }
                catch (Exception ex) { failure = ex; }
            }
        }
        if (failure != null) throw failure;
    }

    /// <summary>
    /// VK gönderiminde wScan doldurmak tek başına scan-code modu seçmez.
    /// Oyun uyumluluğu için KEYEVENTF_SCANCODE kullan; bu koruma atlatmaz.
    /// </summary>
    private static INPUT CreateKeyboardInput(ushort flags, ushort vk)
        => CreateScanCodeInput(flags, MapVirtualKey(vk, 4)); // MAPVK_VK_TO_VSC_EX

    private static KEYBDINPUT CreateLegacyKeyboardInput(ushort flags, ushort vk, uint scanCode)
    {
        var keyboard = CreateScanCodeInput(flags, scanCode).u.ki;
        keyboard.wVk = vk;
        keyboard.dwFlags &= ~0x0008u; // keybd_event yalnızca EXTENDEDKEY / KEYUP kullanır.
        return keyboard;
    }

    private static INPUT CreateScanCodeInput(ushort flags, uint scanCode)
    {
        // Desteklenen tuşlar tek tarama kodu veya E0 öneki kullanır.
        // E1 dizilerini ve eşlenemeyen tuşları sessizce göndermeyelim.
        if ((scanCode & 0xFF) == 0 || (scanCode & 0xFFFFFF00) is not (0 or 0xE000))
            throw new InvalidOperationException($"Desteklenmeyen klavye tarama kodu: 0x{scanCode:X}.");
        uint dwFlags = flags | 0x0008u; // KEYEVENTF_SCANCODE
        if ((scanCode & 0xFF00) == 0xE000) dwFlags |= KEYEVENTF_EXTENDEDKEY;

        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0, // SCANCODE modunda VK kullanılmaz.
                    wScan = (ushort)(scanCode & 0xFF),
                    dwFlags = dwFlags,
                    time = 0,
                    dwExtraInfo = InputDiagnostics.Active ? InputDiagnostics.EventMarker : IntPtr.Zero
                }
            }
        };
    }

    /// <summary>
    /// Rastgele gecikme (jitter) ekler; oyun kabulünü veya koruma atlatmayı sağlamaz.
    /// BaseMs ± jitterRange içinde rastgele bir bekleme üretir.
    /// </summary>
    public static int ApplyJitter(int baseMs, int jitterRange)
    {
        if (jitterRange <= 0) return Math.Max(0, baseMs);
        int min = Math.Max(0, baseMs - jitterRange);
        int max = baseMs + jitterRange;
        return Random.Shared.Next(min, max + 1);
    }

    /// <summary>
    /// Mevcut kullanıcı yetkisinin yüksek mi yoksa düşük bütünlükte mi olduğunu döndürür.
    /// Oyun yüksek bütünlükse, bu uygulama da admin olarak çalışmalıdır.
    /// </summary>
    public static bool IsHighIntegrity()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    /// <summary>
    /// Uygulamanın yönetici olarak çalışıp çalışmadığını döndürür.
    /// </summary>
    public static bool IsRunningAsAdmin() => IsHighIntegrity();
}
