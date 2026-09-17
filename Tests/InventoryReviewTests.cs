using System.Drawing;
using System.Text.Json;
using KOPunisher;

internal static class InventoryReviewTests
{
    public static int Run()
    {
        int passed = 0;
        void Check(bool ok) { if (!ok) throw new Exception("Inventory regression"); }
        void Reject(Action action)
        {
            try { action(); } catch (InvalidOperationException) { return; }
            throw new Exception("Invalid inventory calibration accepted");
        }
        var desktop = new Rectangle(-1920, 0, 3840, 1080);
        var c = new InventoryCalibration { Bounds = new Rectangle(-1800, 200, 353, 205), Desktop = desktop, Dpi = 96 };
        var slots = c.Slots(desktop, 96);
        Check(slots.Length == 28 && slots[0].Location == Point.Empty && slots[^1].Right == 353 && slots[^1].Bottom == 205);
        Check(slots.Sum(s => s.Width * s.Height) == 353 * 205); passed++;
        var restored = JsonSerializer.Deserialize<InventoryCalibration>(JsonSerializer.Serialize(c))!;
        Check(restored.Slots(desktop, 96).SequenceEqual(slots)); passed++;
        Reject(() => c.Slots(desktop, 144)); Reject(() => c.Slots(new Rectangle(0, 0, 1920, 1080), 96)); passed++;
        c.Rows = 0; Reject(() => c.Slots(desktop, 96)); c.Rows = 4;
        c.Bounds = new Rectangle(0, 0, 10, 10); Reject(() => c.Slots(desktop, 96)); passed++;
        var review = new InventoryReview();
        Check(!review.Confirm("a")); review.Scan("a", 28); Check(!review.Approved && !review.Confirm("a")); passed++;
        review.Select(0, true); review.Select(27, true); Check(review.Confirm("a") && review.Selected.Count == 2); passed++;
        review.Select(27, false); Check(!review.Approved && review.Confirm("a")); passed++;
        Check(!review.Confirm("changed") && review.Count == 0 && review.Selected.Count == 0); passed++;
        review.Scan("b", 1); review.Select(0, true); review.Confirm("b"); review.Scan("c", 2);
        Check(!review.Approved && review.Selected.Count == 0); passed++;
        try { review.Select(2, true); throw new Exception("out of bounds selection accepted"); } catch (ArgumentOutOfRangeException) { }
        review.Reset(); Check(review.Count == 0 && !review.Approved); passed++;
        Console.WriteLine($"PASS {passed} inventory calibration/review test groups");
        return passed;
    }
}
