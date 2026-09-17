using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;

namespace KOPunisher;

internal sealed class InventoryUpgradeControl : UserControl
{
    private readonly string _path;
    private readonly Func<string> _targetProcess;
    private readonly List<InventoryItemReading> _readings = new();
    private readonly List<Bitmap?> _tooltips = new();
    private Bitmap? _emptyReference;
    private readonly Button _tooltip = UiTheme.NavButton("Manuel crop tooltip tara (yedek)");
    private readonly Button _openScan = UiTheme.NavButton("I ile aç ve tara (izinli)");
    private readonly Button _empty = UiTheme.NavButton("Seçili slot boş referans");
    private readonly Button _selectAll = UiTheme.NavButton("Hedef altındakileri seç");
    private readonly NumericUpDown _target = new() { Minimum = 1, Maximum = 10, Value = 5, Width = 45 };
    private InventoryCalibration? _calibration;
    private readonly InventoryReview _review = new();
    private readonly NumericUpDown _cols = new() { Minimum = 1, Maximum = 12, Value = 7, Width = 45 };
    private readonly NumericUpDown _rows = new() { Minimum = 1, Maximum = 12, Value = 4, Width = 45 };
    private readonly Label _status = new() { AutoSize = true, MaximumSize = new Size(770, 0), ForeColor = UiTheme.Text };
    private readonly PictureBox _preview = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
    private readonly PictureBox _detail = new() { Width = 100, Height = 100, SizeMode = PictureBoxSizeMode.Zoom };
    private readonly Label _text = new() { AutoSize = true, MaximumSize = new Size(620, 0), ForeColor = UiTheme.Text };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoGenerateColumns = false, RowHeadersVisible = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = UiTheme.Panel,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private readonly List<Bitmap> _tiles = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Button _crop = UiTheme.NavButton("Envanter alanını seç");
    private readonly Button _scan = UiTheme.NavButton("Envanteri tara");
    private readonly Button _imageScan = UiTheme.NavButton("Crop görüntüsü (yedek)");
    private readonly Button _confirm = UiTheme.NavButton("Seçimi kontrol et ve onayla");
    private bool _busy, _loading;
    public bool Busy => _busy;

    public InventoryUpgradeControl(string path, Func<string> targetProcess)
    {
        _path = path; _targetProcess = targetProcess;
        UiTheme.PaintDark(this);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(8) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        actions.Controls.AddRange([_scan, _openScan, _crop, _imageScan, new Label { Text = "Sütun", AutoSize = true, ForeColor = UiTheme.Text }, _cols,
            new Label { Text = "Satır", AutoSize = true, ForeColor = UiTheme.Text }, _rows, _confirm]);
        actions.Controls.AddRange([_tooltip, _empty, new Label { Text = "Hedef +", AutoSize = true, ForeColor = UiTheme.Text }, _target, _selectAll]);
        layout.Controls.Add(actions, 0, 0); layout.Controls.Add(_status, 0, 1);
        layout.Controls.Add(_preview, 0, 2); layout.Controls.Add(_grid, 0, 3);
        var details = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
        details.Controls.AddRange([_detail, _text]); layout.Controls.Add(details, 0, 4); Controls.Add(layout);
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "İşle", FillWeight = 35 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Slot", ReadOnly = true, FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewImageColumn { HeaderText = "Görüntü", ReadOnly = true, ImageLayout = DataGridViewImageCellLayout.Zoom, FillWeight = 45 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tooltip ham metni", ReadOnly = true, FillWeight = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Durum", ReadOnly = true, FillWeight = 85 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Item adı", ReadOnly = true, FillWeight = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Seviye", ReadOnly = true, FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hedef", ReadOnly = true, FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tür", ReadOnly = true, FillWeight = 65 });
        foreach (DataGridViewColumn col in _grid.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
        _grid.DefaultCellStyle.BackColor = UiTheme.Panel; _grid.DefaultCellStyle.ForeColor = UiTheme.Text;
        _grid.RowTemplate.Height = 42;
        _grid.CurrentCellDirtyStateChanged += (_, _) => { if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        _grid.CellValueChanged += (_, e) =>
        {
            if (_loading || e.RowIndex < 0 || e.ColumnIndex != 0 || e.RowIndex >= _review.Count) return;
            bool selected = _grid.Rows[e.RowIndex].Cells[0].Value is true;
            if (selected && (e.RowIndex >= _readings.Count || !InventoryTooltip.NeedsUpgrade(_readings[e.RowIndex], (int)_target.Value)))
            {
                _loading = true; _grid.Rows[e.RowIndex].Cells[0].Value = false; _loading = false;
                _status.Text = "Yalnız tooltip'i okunan ve hedef altındaki item seçilebilir."; return;
            }
            _review.Select(e.RowIndex, selected);
            RefreshRows();
            _status.Text = $"{_review.Selected.Count} slot seçildi. Seçimler henüz onaylanmadı.";
        };
        _grid.SelectionChanged += (_, _) =>
        {
            int i = _grid.CurrentRow?.Index ?? -1;
            if (i < 0 || i >= _tiles.Count) return;
            _detail.Image = i < _tooltips.Count && _tooltips[i] != null ? _tooltips[i] : _tiles[i];
            _text.Text = $"Slot {i + 1}\n{_grid.Rows[i].Cells[5].Value} {_grid.Rows[i].Cells[6].Value}\nOCR: {_grid.Rows[i].Cells[3].Value}\nGörüntüyü büyütmek için çift tıkla.";
        };
        _detail.DoubleClick += (_, _) =>
        {
            if (_detail.Image == null) return;
            using var form = new Form { Text = "Tooltip kanıtı", Width = 1000, Height = 800 };
            form.Controls.Add(new PictureBox { Image = _detail.Image, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom });
            form.ShowDialog(this);
        };
        _tooltip.Click += async (_, _) => await RunAsync(() => TooltipScanAsync());
        _empty.Click += (_, _) => SetEmptyReference();
        _selectAll.Click += (_, _) => SelectBelowTarget();
        _target.ValueChanged += (_, _) => SelectBelowTarget();
        _crop.Click += async (_, _) => await RunAsync(CropAsync);
        _scan.Click += async (_, _) => await RunAsync(() => TooltipScanAsync(automatic: true));
        _imageScan.Click += async (_, _) => await RunAsync(ScanAsync);
        _openScan.Click += async (_, _) =>
        {
            if (MessageBox.Show(this, "Oyunda sohbet, arama ve yazı girişini kapat. Envanter bulunamazsa yalnız bir kez I gönderilmesine izin veriyor musun? Odak hedef oyunda değilse hiçbir tuş gönderilmez.",
                "Bir defalık I izni", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                await RunAsync(() => TooltipScanAsync(automatic: true, permitOpen: true));
        };
        _confirm.Click += async (_, _) => await RunAsync(ConfirmAsync);
        _cols.ValueChanged += (_, _) => GridChanged(); _rows.ValueChanged += (_, _) => GridChanged();
        _status.Text = "Envanteri tara: oyun öndeyken ızgara ve itemler otomatik bulunur. Sohbeti kapat; fareye dokunma, ESC durdurur. Upgrade yapılmaz.";
        try
        {
            if (File.Exists(path))
            {
                _calibration = JsonSerializer.Deserialize<InventoryCalibration>(File.ReadAllText(path))
                    ?? throw new InvalidOperationException("Boş kalibrasyon kaydı.");
                _calibration.Slots(SystemInformation.VirtualScreen, DeviceDpi);
                _loading = true; _cols.Value = _calibration.Columns; _rows.Value = _calibration.Rows; _loading = false;
                _status.Text = "Manuel yedek crop yüklendi. Envanteri tara otomatik algılar; eski crop gerektirmez.";
            }
        }
        catch (Exception ex) { _loading = false; _calibration = null; _status.Text = "Crop yüklenemedi: " + ex.Message; }
    }
    private void GridChanged()
    {
        if (_loading) return;
        _emptyReference?.Dispose(); _emptyReference = null;
        ClearScan();
        _status.Text = "Izgara değişti. Yeniden tara; çerçevelerin slotlara oturduğunu kontrol et.";
    }
    private void ClearScan()
    {
        _loading = true; _review.Reset(); _detail.Image = null; _text.Text = "";
        _grid.Rows.Clear();
        foreach (var image in _tooltips) image?.Dispose(); _tooltips.Clear(); _readings.Clear();
        foreach (var tile in _tiles) tile.Dispose(); _tiles.Clear();
        var old = _preview.Image; _preview.Image = null; old?.Dispose(); _loading = false;
    }
    private async Task RunAsync(Func<Task> work)
    {
        if (_busy) return;
        _busy = true;
        _openScan.Enabled = _imageScan.Enabled = _tooltip.Enabled = _empty.Enabled = _selectAll.Enabled = _target.Enabled = false;
        _crop.Enabled = _scan.Enabled = _confirm.Enabled = _grid.Enabled = _cols.Enabled = _rows.Enabled = false;
        try { await work(); }
        catch (OperationCanceledException) { if (!IsDisposed) { ClearScan(); _status.Text = "Tarama iptal edildi; seçimler temizlendi."; } }
        catch (Exception ex) { if (!IsDisposed) { ClearScan(); _status.Text = "Durdu: " + ex.Message; } }
        finally
        {
            _busy = false;
            if (!IsDisposed)
                _openScan.Enabled = _imageScan.Enabled = _tooltip.Enabled = _empty.Enabled = _selectAll.Enabled = _target.Enabled = _crop.Enabled = _scan.Enabled = _confirm.Enabled = _grid.Enabled = _cols.Enabled = _rows.Enabled = true;
        }
    }
    private static Bitmap CaptureArea(Rectangle r)
    {
        var bitmap = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
        try { using var g = Graphics.FromImage(bitmap); g.CopyFromScreen(r.Location, Point.Empty, r.Size); return bitmap; }
        catch { bitmap.Dispose(); throw; }
    }
    private async Task<T> WithoutWindowAsync<T>(Func<T> action)
    {
        var owner = FindForm() ?? throw new InvalidOperationException("Pencere bulunamadı.");
        owner.Hide();
        try { await Task.Delay(400, _stop.Token); return action(); }
        finally { if (!owner.IsDisposed) { owner.Show(); owner.Activate(); } }
    }
    private async Task CropAsync()
    {
        _emptyReference?.Dispose(); _emptyReference = null;
        ClearScan();
        var desktop = SystemInformation.VirtualScreen;
        Rectangle chosen = await WithoutWindowAsync(() =>
        {
            using var screen = CaptureArea(desktop);
            using var selector = new InventoryCropForm(screen, desktop);
            return selector.ShowDialog() == DialogResult.OK ? selector.SelectedBounds : Rectangle.Empty;
        });
        if (chosen.IsEmpty) { _status.Text = "Alan seçimi iptal edildi; önceki crop korunuyor."; return; }
        var next = new InventoryCalibration { Bounds = chosen, Desktop = desktop, Dpi = DeviceDpi, Columns = (int)_cols.Value, Rows = (int)_rows.Value };
        next.Slots(desktop, DeviceDpi); SaveCalibration(next); _calibration = next;
        _status.Text = $"Crop kaydedildi: {chosen.Width} × {chosen.Height}. Tara ile slotları kontrol et.";
    }
    private void SaveCalibration(InventoryCalibration calibration)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string temp = _path + ".tmp";
        try { File.WriteAllText(temp, JsonSerializer.Serialize(calibration)); File.Move(temp, _path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private Rectangle[] CurrentSlots()
    {
        if (_calibration == null) throw new InvalidOperationException("Önce envanter alanını seç.");
        _calibration.Columns = (int)_cols.Value; _calibration.Rows = (int)_rows.Value;
        return _calibration.Slots(SystemInformation.VirtualScreen, DeviceDpi);
    }
    private static string Hash(Bitmap bitmap)
    {
        using var stream = new MemoryStream(); bitmap.Save(stream, ImageFormat.Png);
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    private async Task ScanAsync()
    {
        ClearScan(); var slots = CurrentSlots(); SaveCalibration(_calibration!);
        using var capture = await WithoutWindowAsync(() => CaptureArea(_calibration!.Bounds));
        _stop.Token.ThrowIfCancellationRequested();
        ShowCapture(capture, slots);
        _status.Text = "Izgarayı kontrol et. Varsa bir boş slotu referans seç; sonra Tooltipleri tara. Tararken fareye dokunma; ESC durdurur.";
    }
    private void ShowCapture(Bitmap capture, Rectangle[] slots)
    {
        var annotated = new Bitmap(capture);
        using (var g = Graphics.FromImage(annotated))
        using (var pen = new Pen(Color.Lime, 1))
            for (int i = 0; i < slots.Length; i++)
            {
                var r = slots[i]; g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
                g.DrawString((i + 1).ToString(), Font, Brushes.Lime, r.X + 2, r.Y + 2);
            }
        _preview.Image = annotated; _review.Scan(Hash(capture), slots.Length);
        _loading = true;
        try
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var tile = capture.Clone(slots[i], PixelFormat.Format32bppArgb); _tiles.Add(tile);
                _readings.Add(new("", null, "", "Tooltip bekleniyor")); _tooltips.Add(null);
                _grid.Rows.Add(false, (i + 1).ToString(), tile, "", "Tooltip bekleniyor", "", "?", "+" + _target.Value, "Bilinmiyor");
            }
        }
        finally { _loading = false; }
    }
    private void RefreshRows()
    {
        for (int i = 0; i < _readings.Count; i++)
        {
            var reading = _readings[i]; var row = _grid.Rows[i];
            row.Cells[3].Value = reading.RawText;
            row.Cells[4].Value = row.Cells[0].Value is true ? "Onay bekliyor" : reading.Status;
            row.Cells[5].Value = reading.Name; row.Cells[6].Value = reading.Level is int level ? "+" + level : "?";
            row.Cells[7].Value = "+" + _target.Value; row.Cells[8].Value = reading.Kind;
        }
    }
    private void SetEmptyReference()
    {
        int i = _grid.CurrentRow?.Index ?? -1;
        if (i < 0 || i >= _tiles.Count) { _status.Text = "Önce Tara, sonra gerçekten boş bir slotun satırını seç."; return; }
        if (MessageBox.Show(this, $"Slot {i + 1} kesinlikle boş mu? Dolu itemi referans seçme. Referans yalnız bu oturumda kullanılır.",
            "Boş slot referansı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _emptyReference?.Dispose(); _emptyReference = new Bitmap(_tiles[i]);
        _review.Scan("reference-changed", _tiles.Count);
        _loading = true;
        foreach (DataGridViewRow row in _grid.Rows) row.Cells[0].Value = false;
        _loading = false;
        for (int slot = 0; slot < _readings.Count; slot++) _readings[slot] = new("", null, "", "Tooltip bekleniyor");
        RefreshRows(); _status.Text = "Boş referans alındı. Tooltipleri yeniden tara; eski seçimler geçersiz.";
    }
    private void SelectBelowTarget()
    {
        for (int i = 0; i < _readings.Count; i++)
        {
            bool selected = InventoryTooltip.NeedsUpgrade(_readings[i], (int)_target.Value);
            _review.Select(i, selected);
            _loading = true; _grid.Rows[i].Cells[0].Value = selected; _loading = false;
        }
        RefreshRows(); _status.Text = $"Hedef +{_target.Value}: {_review.Selected.Count} okunmuş item seçildi. Seçimi kontrol et ve onayla.";
    }
    private string Signature() => string.Join("\n---\n", _readings.Select((reading, i) => $"{i}:{reading.Identity}:{reading.Kind}:{reading.Upgradeable}"));
    private async Task WithHoverAsync(Func<InventoryHoverReader, Task> action, bool automatic = false, bool permitOpen = false)
    {
        var owner = FindForm() ?? throw new InvalidOperationException("Pencere bulunamadı.");
        _status.Text = "Oyun öne gelmeli. Tarama 2 saniye sonra başlar; fareye dokunma, ESC durdurur.";
        owner.Hide();
        try
        {
            await Task.Delay(2000, _stop.Token);
            var reader = new InventoryHoverReader(automatic ? Rectangle.Empty : _calibration!.Bounds, _targetProcess(), _stop.Token);
            if (automatic)
            {
                await reader.DiscoverAsync(permitOpen);
                _calibration = new InventoryCalibration { Bounds = reader.InventoryBounds, Desktop = SystemInformation.VirtualScreen,
                    Dpi = DeviceDpi, Columns = reader.Columns, Rows = reader.Rows };
                _loading = true;
                try { _cols.Value = reader.Columns; _rows.Value = reader.Rows; }
                finally { _loading = false; }
            }
            await action(reader);
            await reader.ParkAsync();
        }
        finally { if (!owner.IsDisposed) { owner.Show(); owner.Activate(); } }
    }
    private async Task TooltipScanAsync(bool automatic = false, bool permitOpen = false)
    {
        if (!automatic) { CurrentSlots(); SaveCalibration(_calibration!); }
        else { _emptyReference?.Dispose(); _emptyReference = null; }
        ClearScan();
        Rectangle[] slots = [];
        await WithHoverAsync(async reader =>
        {
            slots = CurrentSlots();
            await reader.ParkAsync();
            using var capture = reader.CaptureInventory(); ShowCapture(capture, slots);
            for (int i = 0; i < slots.Length; i++)
            {
                var result = await reader.ReadAsync(slots[i], _emptyReference == null || InventoryHoverReader.MatchesEmpty(_tiles[i], _emptyReference));
                _readings[i] = result.Reading; _tooltips[i] = result.Evidence;
                RefreshRows(); _status.Text = $"Tooltip taranıyor: {i + 1}/{slots.Length} — {result.Reading.Status}";
            }
        }, automatic, permitOpen);
        _review.Scan(Signature(), slots.Length);
        int read = _readings.Count(x => x.Readable), empty = _readings.Count(x => x.Status.StartsWith("Boş"));
        _status.Text = $"{read} item okundu, {empty} boş, {_readings.Count - read - empty} belirsiz. Hedefi seç ve kontrol et. Otomatik upgrade yok.";
    }
    private async Task ConfirmAsync()
    {
        if (_review.Selected.Count == 0) throw new InvalidOperationException("Önce tooltip tara ve hedef altındaki itemleri seç.");
        var slots = CurrentSlots(); var selected = _review.Selected.ToArray();
        await WithHoverAsync(async reader =>
        {
            foreach (int i in selected)
            {
                var result = await reader.ReadAsync(slots[i], false);
                using var evidence = result.Evidence;
                if (!InventoryTooltip.SameItem(_readings[i], result.Reading) || !InventoryTooltip.NeedsUpgrade(result.Reading, (int)_target.Value))
                    throw new InvalidOperationException($"Slot {i + 1} isim/seviye doğrulanamadı veya değişti. Yeniden tara.");
                _readings[i] = result.Reading;
                if (ReferenceEquals(_detail.Image, _tooltips[i])) _detail.Image = null;
                _tooltips[i]?.Dispose(); _tooltips[i] = new Bitmap(evidence);
            }
        });
        if (!_review.Confirm(Signature())) throw new InvalidOperationException("Tarama değişti. Yeniden tara.");
        _detail.Image = null; RefreshRows();
        foreach (int i in selected) _grid.Rows[i].Cells[4].Value = "İsim/seviye yeniden okundu; onaylandı";
        _status.Text = $"{selected.Length} itemin taze tooltip'i kontrol edildi. Hedef +{_target.Value}. Upgrade BAŞLATILMADI.";
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _stop.Cancel(); ClearScan(); _emptyReference?.Dispose(); }
        base.Dispose(disposing);
    }
}
