using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KOPunisher;

/// <summary>
/// Windows RegisterHotKey API kullanarak global hotkey yönetir.
/// Uygulama arka planda olsa bile tuşları yakalar.
/// </summary>
public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Modifier tuşları
    private const uint MOD_NOREPEAT = 0x4000;

    // Hotkey ID'leri — benzersiz olmalı
    private const int HOTKEY_START = 1000;
    private const int HOTKEY_STOP = 1001;
    private const int HOTKEY_EMERGENCY = 1002;

    // Callback delegate — hotkey basıldığında çağrılır
    public event Action<string>? HotKeyPressed;

    // Window handle — form'un handle'ı set edilmeli
    private IntPtr _windowHandle = IntPtr.Zero;

    /// <summary>
    /// Form handle'ını set et — hotkey registration için gerekli.
    /// </summary>
    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    /// <summary>
    /// Hotkeyleri kaydet. Ayarlardan okuyarak register eder.
    /// </summary>
    public void RegisterHotkeys(Settings settings)
    {
        UnregisterAll();

        var hotkeyMap = new Dictionary<string, string>
        {
            { "Start", settings.Hotkeys.Start },
            { "Stop", settings.Hotkeys.Stop },
            { "EmergencyStop", settings.Hotkeys.EmergencyStop }
        };

        var idMap = new Dictionary<string, int>
        {
            { "Start", HOTKEY_START },
            { "Stop", HOTKEY_STOP },
            { "EmergencyStop", HOTKEY_EMERGENCY }
        };

        foreach (var kvp in hotkeyMap)
        {
            string action = kvp.Key;
            string keyName = (kvp.Value ?? "").Trim().ToUpperInvariant();
            // Global kayıt aynı tuşla gönderilen skill'i WM_HOTKEY'e dönüştürür.
            // Çıkış/tetik olarak kullanılan Start/Stop tuşlarını rezerve etme.
            var reserved = settings.Skills.Values.Concat(new[]
            {
                settings.ComboTrigger, settings.MinorTrigger, settings.InsertTrigger,
                settings.ForceHpTrigger, settings.MinorPedalKey, settings.MinorPotKey,
                settings.MinorManaKey, settings.ZKey, settings.ZAttackKey,
                settings.ComboKey1, settings.ComboKey2, settings.ComboKey3,
                settings.InsertKey, settings.LightFeetKey, settings.SlideKey, "R"
            });
            if (reserved.Contains(keyName, StringComparer.OrdinalIgnoreCase))
            {
                InputDiagnostics.Record("hotkey_skipped", new { action, key = keyName, reason = "output_or_trigger_collision" });
                continue;
            }

            // VK kodunu bul (InputSender.TryGetVkCode kullan)
            if (InputSender.TryGetVkCode(keyName, out var vk))
            {
                // Hotkey kaydet — modifier yok (sadece tuş)
                bool success = RegisterHotKey(
                    _windowHandle,
                    idMap[action],
                    MOD_NOREPEAT,
                    vk
                );

                int error = Marshal.GetLastWin32Error();
                InputDiagnostics.Record("hotkey_registration", new { action, key = keyName, success, win32Error = success ? (int?)null : error });
            }
        }
    }

    /// <summary>
    /// Tüm hotkeyleri kaldır.
    /// </summary>
    private void UnregisterAll()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_START);
            UnregisterHotKey(_windowHandle, HOTKEY_STOP);
            UnregisterHotKey(_windowHandle, HOTKEY_EMERGENCY);
        }
    }

    /// <summary>
    /// Hotkey event'ine mesaj gönder — form'dan çağrılır.
    /// WM_HOTKEY mesajı wParam'ında hotkey ID'si bulunur.
    /// </summary>
    public void ProcessHotkeyMessage(int wParam)
    {
        string? action = null;

        switch (wParam)
        {
            case HOTKEY_START:
                action = "Start";
                break;
            case HOTKEY_STOP:
                action = "Stop";
                break;
            case HOTKEY_EMERGENCY:
                action = "EmergencyStop";
                break;
        }

        if (action != null && HotKeyPressed != null)
        {
            HotKeyPressed.Invoke(action);
        }
    }

    public void Dispose()
    {
        UnregisterAll();
    }
}
