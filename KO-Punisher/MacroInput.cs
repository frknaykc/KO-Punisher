using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KOPunisher;

// Testlerde gerçek klavyeye dokunmadan aynı motoru çalıştırmak için sınır.
public interface IMacroInput
{
    bool IsHeld(string key);
    bool IsTargetForeground(string processName);
    void KeyDown(string key);
    void KeyUp(string key);
    void ReleaseAll();
    // Takip edemeyen test/alternatif input bar cache kullanmaz.
    bool CanTrackBarSelection => false;
    long BarSelectionEpoch => 0;
}

public sealed class WindowsMacroInput : IMacroInput, IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100, WmKeyUp = 0x0101, WmSysKeyDown = 0x0104, WmSysKeyUp = 0x0105;
    private const int WmXButtonDown = 0x020B, WmXButtonUp = 0x020C;
    private const uint LlkhfInjected = 0x10;
    private const uint LlmhfInjected = 0x01;
    private const int VkLShift = 0xA0, VkRShift = 0xA1, VkLControl = 0xA2, VkRControl = 0xA3, VkLMenu = 0xA4, VkRMenu = 0xA5;

    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint vkCode, scanCode, flags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        public int ptX, ptY;
        public uint mouseData, flags, time;
        public IntPtr dwExtraInfo;
    }

    private readonly object _sync = new();
    private readonly HashSet<string> _held = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _swallowed = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _passed = new(StringComparer.OrdinalIgnoreCase);
    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;
    private IntPtr _keyboardHook, _mouseHook;
    private volatile bool _armed, _disposed;
    private long _barSelectionEpoch;
    public bool CanTrackBarSelection => HooksInstalled;
    public long BarSelectionEpoch => Interlocked.Read(ref _barSelectionEpoch);
    private string _combo = "XBUTTON1", _minor = "XBUTTON2", _emergency = "F12";

    public WindowsMacroInput()
    {
        _keyboardProc = KeyboardHook;
        _mouseProc = MouseHook;
    }

    public void InstallHooks()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows gerekli.");
        if (HooksInstalled) return;
        IntPtr module = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, module, 0);
        int error = Marshal.GetLastWin32Error();
        if (_keyboardHook != IntPtr.Zero)
        {
            _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProc, module, 0);
            error = Marshal.GetLastWin32Error();
        }
        if (HooksInstalled) return;
        if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
        if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
        _keyboardHook = _mouseHook = IntPtr.Zero;
        throw new System.ComponentModel.Win32Exception(error, "Klavye/fare tetik yakalayıcısı kurulamadı.");
    }

    public void Configure(Settings settings)
    {
        _combo = (settings.ComboTrigger ?? "").Trim().ToUpperInvariant();
        _minor = (settings.MinorTrigger ?? "").Trim().ToUpperInvariant();
        _emergency = (settings.Hotkeys.EmergencyStop ?? "").Trim().ToUpperInvariant();
    }

    public void SetArmed(bool armed) => _armed = armed;

    public bool HooksInstalled => _keyboardHook != IntPtr.Zero && _mouseHook != IntPtr.Zero;

    public bool IsHeld(string key)
    {
        string name = (key ?? "").Trim().ToUpperInvariant();
        lock (_sync)
        {
            if (_held.Contains(name)) return true;
        }
        // Hook durumunda yalnızca fiziksel olaylar: kendi SendInput olayımız
        // tetik/acil tuşu gibi geri okunmamalı.
        return !HooksInstalled && HardwareDown(name);
    }

    public bool IsTargetForeground(string processName)
    {
        IntPtr window = GetForegroundWindow();
        if (window == IntPtr.Zero) return false;
        GetWindowThreadProcessId(window, out uint id);
        if (id == (uint)Environment.ProcessId) return false;
        try
        {
            using var process = Process.GetProcessById((int)id);
            return process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    public void KeyDown(string key) => InputSender.KeyDown(key);
    public void KeyUp(string key) => InputSender.KeyUp(key);
    public void ReleaseAll() => InputSender.CleanupAllKeys();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _armed = false;
        if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
        if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
        _keyboardHook = _mouseHook = IntPtr.Zero;
    }

    private IntPtr KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if (InputDiagnostics.Active && (info.flags & LlkhfInjected) != 0 && info.dwExtraInfo == InputDiagnostics.EventMarker)
                InputDiagnostics.Record("own_injected_hook", new { vk = info.vkCode, scan = info.scanCode, info.flags, info.time, message = wParam.ToInt32() });
            if ((info.flags & LlkhfInjected) == 0)
            {
                int msg = wParam.ToInt32();
                bool down = msg is WmKeyDown or WmSysKeyDown;
                bool up = msg is WmKeyUp or WmSysKeyUp;
                string? name = NameFromVk(info.vkCode);
                if (name != null && (down || up))
                {
                    bool changed = SetHeld(name, down);
                    bool swallowed = Swallow(name, down);
                    if (down && !swallowed && info.vkCode is >= 0x70 and <= 0x77)
                        Interlocked.Increment(ref _barSelectionEpoch);
                    TraceTrigger(name, down, changed, swallowed);
                    if (swallowed) return (IntPtr)1;
                }
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
            if ((info.flags & LlmhfInjected) == 0)
            {
                int msg = wParam.ToInt32();
                if (msg is WmXButtonDown or WmXButtonUp)
                {
                    int which = (int)(info.mouseData >> 16);
                    string name = which == 2 ? "XBUTTON2" : "XBUTTON1";
                    bool down = msg == WmXButtonDown;
                    bool changed = SetHeld(name, down);
                    bool swallowed = Swallow(name, down);
                    TraceTrigger(name, down, changed, swallowed);
                    if (swallowed) return (IntPtr)1;
                }
            }
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private bool SetHeld(string name, bool down)
    {
        lock (_sync) return down ? _held.Add(name) : _held.Remove(name);
    }

    private void TraceTrigger(string name, bool down, bool changed, bool swallowed)
    {
        if (!InputDiagnostics.Active || !changed || (name != _combo && name != _minor && name != _emergency)) return;
        InputDiagnostics.Record("physical_trigger", new { key = name, down, armed = _armed, swallowed, target = ForegroundDiagnostics.Current() });
    }

    private bool Swallow(string name, bool down)
    {
        // Durdur basış ile bırakış arasına girse de olay çifti tutarlı kalır.
        if (!down)
        {
            _passed.Remove(name);
            return _swallowed.Remove(name);
        }
        if (_passed.Contains(name)) return false;
        if (_swallowed.Contains(name)) return true;
        if (!_armed || !IsSwallowedTrigger(name))
        {
            _passed.Add(name);
            return false;
        }
        _swallowed.Add(name);
        return true;
    }

    private bool IsSwallowedTrigger(string name) =>
        name != "CAPSLOCK" && (name == _combo || name == _minor);

    private static string? NameFromVk(uint vk) => vk switch
    {
        0x10 or VkLShift or VkRShift => "SHIFT",
        0x11 or VkLControl or VkRControl => "CTRL",
        0x12 or VkLMenu or VkRMenu => "ALT",
        0x05 => "XBUTTON1",
        0x06 => "XBUTTON2",
        0x14 => "CAPSLOCK",
        0x20 => "SPACE",
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        >= 0x41 and <= 0x5A => ((char)vk).ToString(),
        >= 0x60 and <= 0x69 => "NUMPAD" + (vk - 0x60),
        >= 0x70 and <= 0x83 => "F" + (vk - 0x6F),
        _ => null
    };

    private static bool HardwareDown(string name)
    {
        if (name is "CAPSLOCK" or "CAPS")
            return (GetAsyncKeyState(0x14) & 0x8000) != 0;
        if (!InputSender.TryGetVkCode(name, out ushort vk)) return false;
        if ((GetAsyncKeyState(vk) & 0x8000) != 0) return true;
        return vk switch
        {
            0x10 => Down(VkLShift) || Down(VkRShift),
            0x11 => Down(VkLControl) || Down(VkRControl),
            0x12 => Down(VkLMenu) || Down(VkRMenu),
            _ => false
        };
    }

    private static bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;
}
