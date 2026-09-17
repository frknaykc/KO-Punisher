using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KOPunisher;

internal static class ForegroundDiagnostics
{
    internal readonly record struct Target(long Window, uint Pid, uint Thread, long KeyboardLayout);
    internal readonly record struct Integrity(int? Rid, int? Win32Error);

    internal static Target Current()
    {
        if (!OperatingSystem.IsWindows()) return default;
        IntPtr window = GetForegroundWindow();
        uint thread = GetWindowThreadProcessId(window, out uint pid);
        return new Target(window.ToInt64(), pid, thread, GetKeyboardLayout(thread).ToInt64());
    }

    internal static object Describe(Target target)
    {
        string? name = null, processError = null;
        try
        {
            using var process = Process.GetProcessById((int)target.Pid);
            name = process.ProcessName;
        }
        catch (Exception ex) { processError = ex.GetType().Name; }
        Integrity own = GetIntegrity((uint)Environment.ProcessId);
        Integrity foreground = GetIntegrity(target.Pid);
        return new
        {
            target, process = name, processError, ownIntegrity = own, targetIntegrity = foreground,
            higherIntegrityTarget = own.Rid.HasValue && foreground.Rid.HasValue ? (bool?)(foreground.Rid > own.Rid) : null,
            modifiers = OperatingSystem.IsWindows() ? new
            {
                shift = Down(0x10), ctrl = Down(0x11), alt = Down(0x12), win = Down(0x5B) || Down(0x5C)
            } : null
        };
    }

    // TokenIntegrityLevel yalnızca yetki metadata'sıdır; oyun belleği okunmaz.
    internal static Integrity GetIntegrity(uint pid)
    {
        if (!OperatingSystem.IsWindows()) return new(null, null);
        IntPtr process = OpenProcess(0x1000, false, pid); // PROCESS_QUERY_LIMITED_INFORMATION
        if (process == IntPtr.Zero) return new(null, Marshal.GetLastWin32Error());
        IntPtr token = IntPtr.Zero, buffer = IntPtr.Zero;
        try
        {
            if (!OpenProcessToken(process, 0x0008, out token)) return new(null, Marshal.GetLastWin32Error());
            GetTokenInformation(token, 25, IntPtr.Zero, 0, out int size);
            if (size <= 0) return new(null, Marshal.GetLastWin32Error());
            buffer = Marshal.AllocHGlobal(size);
            if (!GetTokenInformation(token, 25, buffer, size, out _)) return new(null, Marshal.GetLastWin32Error());
            IntPtr sid = Marshal.ReadIntPtr(buffer);
            byte count = Marshal.ReadByte(GetSidSubAuthorityCount(sid));
            if (count == 0) return new(null, 1337); // ERROR_INVALID_SID
            return new(Marshal.ReadInt32(GetSidSubAuthority(sid, (uint)(count - 1))), null);
        }
        finally
        {
            if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            if (token != IntPtr.Zero) CloseHandle(token);
            CloseHandle(process);
        }
    }

    private static bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint thread);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vk);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool GetTokenInformation(IntPtr token, int infoClass, IntPtr info, int length, out int needed);
    [DllImport("advapi32.dll")] private static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);
    [DllImport("advapi32.dll")] private static extern IntPtr GetSidSubAuthority(IntPtr sid, uint index);
}
