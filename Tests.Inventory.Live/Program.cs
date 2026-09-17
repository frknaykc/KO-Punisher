using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using KOPunisher;

// Opt-in interactive-session smoke test. Uses the production reader, not a copy.
// No activation, clicks, item selection, Anvil or scroll actions are implemented.
internal static class Program
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length != 3 || args[0] != "--read-only" || !int.TryParse(args[1], out int pid))
        {
            Console.Error.WriteLine("Usage: Tests.Inventory.Live --read-only PID NEW_OUTPUT_DIRECTORY");
            return 2;
        }
        string output = Path.GetFullPath(args[2]);
        if (Directory.Exists(output)) throw new InvalidOperationException("Output must be a new directory.");
        Directory.CreateDirectory(output);
        try
        {
            RunAsync(pid, output).GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(output, "complete.txt"), "Read-only scan completed; OCR accuracy requires visual review.");
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(output, "error.txt"), ex.ToString());
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static async Task RunAsync(int pid, string output)
    {
        using var process = Process.GetProcessById(pid);
        if (!process.ProcessName.Equals("KnightOnLine", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Expected KnightOnLine PID.");
        if (process.SessionId != Process.GetCurrentProcess().SessionId)
            throw new InvalidOperationException("Run from the same interactive session as the game.");
        // Let the interactive launcher close. Do not force focus or inject keys.
        await Task.Delay(3000);
        GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
        if (foreground != pid) throw new InvalidOperationException("Requested game PID must already be foreground.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var assembly = typeof(MainForm).Assembly;
        var diagnostics = assembly.GetType("KOPunisher.ForegroundDiagnostics", throwOnError: true)!;
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var target = diagnostics.GetMethod("Current", flags)!.Invoke(null, null)!;
        var description = diagnostics.GetMethod("Describe", flags)!.Invoke(null, new[] { target });
        File.WriteAllText(Path.Combine(output, "foreground.json"), JsonSerializer.Serialize(description));
        var type = assembly.GetType("KOPunisher.InventoryHoverReader", throwOnError: true)!;
        var reader = Activator.CreateInstance(type, Rectangle.Empty, process.ProcessName, timeout.Token)!;
        async Task Call(string method, params object[] parameters)
            => await (Task)type.GetMethod(method)!.Invoke(reader, parameters)!;
        T Property<T>(string name) => (T)type.GetProperty(name)!.GetValue(reader)!;
        await Call("DiscoverAsync", false); // Never opens inventory; no keyboard input.
        var calibration = new InventoryCalibration
        {
            Bounds = Property<Rectangle>("InventoryBounds"), Desktop = SystemInformation.VirtualScreen,
            Dpi = 96, Columns = Property<int>("Columns"), Rows = Property<int>("Rows")
        };
        var slots = calibration.Slots(calibration.Desktop, calibration.Dpi);
        using (var inventory = (Bitmap)type.GetMethod("CaptureInventory")!.Invoke(reader, null)!)
            inventory.Save(Path.Combine(output, "inventory.png"), ImageFormat.Png);
        File.WriteAllText(Path.Combine(output, "identity.json"), JsonSerializer.Serialize(new
        {
            Pid = pid, process.ProcessName, process.StartTime, Assembly = assembly.Location,
            AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))),
            ModelSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "tessdata", "eng.traineddata")))),
            OcrMode = "TesseractSoftContrast", Columns = calibration.Columns, Rows = calibration.Rows,
            Bounds = calibration.Bounds.ToString(), Slots = slots.Length, PermitOpen = false
        }, new JsonSerializerOptions { WriteIndented = true }));
        await Call("ParkAsync");
        for (int i = 0; i < slots.Length; i++)
        {
            var task = (Task)type.GetMethod("ReadAsync")!.Invoke(reader, new object[] { slots[i], true })!;
            await task;
            object tuple = task.GetType().GetProperty("Result")!.GetValue(task)!;
            object reading = tuple.GetType().GetField("Item1")!.GetValue(tuple)!;
            using var evidence = (Bitmap)tuple.GetType().GetField("Item2")!.GetValue(tuple)!;
            evidence.Save(Path.Combine(output, $"slot-{i + 1:00}.png"), ImageFormat.Png);
            File.AppendAllText(Path.Combine(output, "readings.jsonl"), JsonSerializer.Serialize(new { Slot = i + 1, Reading = reading }) + "\n");
        }
        await Call("ParkAsync");
    }
}
