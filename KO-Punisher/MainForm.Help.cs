using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

public partial class MainForm
{
    private readonly ToolTip _help = new() { InitialDelay = 350, ReshowDelay = 100, AutoPopDelay = 4000 };
    private readonly HashSet<Control> _helpControls = new();
    private readonly FlowLayoutPanel _comboPreview = new()
    {
        Location = new Point(12, 68), Size = new Size(790, 160), WrapContents = false,
        BackColor = UiTheme.PanelAlt
    };
    private string _previewSignature = "";

    private static string FeatureHint(Control control) => control.Name switch
    {
        _ when ArcherTimingFields.Any(f => f.Key == control.Name) => ArcherTimingFields.First(f => f.Key == control.Name).Hint,
        "ArcherReference" => "Referans süreler",
        "ComboTrigger" => "Atak tetiği",
        "ComboSpeedMs" => "Combo beklemesi",
        "SkillKeyHold" => "Skill basışı",
        "SkillToWDelay" => "Skill sonrası",
        "WKeyHold" => "Hareket basışı",
        "WToRDelay" => "R öncesi",
        "RKeyHold" => "R basışı",
        "NextSkillDelay" => "Tur aralığı",
        "SlideKey" => "Hareket tuşu",
        _ when control.Name.StartsWith("ComboKey") => "Skill tuşu",
        _ when control.Name.StartsWith("BuffKey") => "Buff tuşu",
        _ when control.Name.StartsWith("BuffSeconds") => "Tekrar aralığı",
        _ when control.Name.StartsWith("BuffCast") => "Casting beklemesi",
        _ => control.Text switch
        {
            "Başlat" => "Makroyu hazırla", "Durdur" => "Gönderimi durdur",
            "Hızlı test" => "Kısa zamanlama", "3 sn sonra tek tur" => "Combo testi",
            "Durum paneli" => "Canlı durum", "Buff zamanlayıcı" => "Otomatik buff",
            "Mob filtresi" => "Hedef seçimi", "Pazar mesajları" => "Aralıklı mesaj",
            "Jitter" => "Rastgele gecikme", "Üstte" => "Üstte tut",
            "Yalnız listedeki mob isimlerine saldır" => "Hedef filtresi",
            "3 sn sonra hedef adını kırp" => "Bölge seçimi",
            "3 sn sonra yalnız OCR oku" => "Hedef okuma",
            "3 sn sonra pazarı başlat" => "Mesaj gönderimi",
            _ when control is TextBox => "Tuş ataması",
            _ when control is NumericUpDown => "Süre ayarı",
            _ when control is CheckBox => "Özellik seçimi",
            _ when control is Button => "İşlem seçimi",
            _ => ""
        }
    };

    private void AttachHelp(Control control)
    {
        if (!_helpControls.Add(control)) return;
        string hint = FeatureHint(control);
        if (hint.Length > 0) _help.SetToolTip(control, hint);
        control.ControlAdded += (_, e) => { if (e.Control != null) AttachHelp(e.Control); };
        control.Disposed += (_, _) => _helpControls.Remove(control);
        foreach (Control child in control.Controls) AttachHelp(child);
    }

    private void InitializeComboHelp()
    {
        AttachHelp(this);
        _help.SetToolTip(_monsterNames, "Hedef listesi");
        _help.SetToolTip(_marketMessages, "Mesaj listesi");
        _help.SetToolTip(_chatClosed, "Sohbet onayı");
        foreach (var box in _keys.Where(k => k.Key.StartsWith("ComboKey") || k.Key == "SlideKey").Select(k => k.Value))
            box.TextChanged += (_, _) => UpdateComboPreview();
        _preset.DrawMode = DrawMode.OwnerDrawFixed;
        _preset.ItemHeight = 22;
        _preset.DropDownWidth = 420;
        _preset.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            if (_preset.Items[e.Index] is not string id) return;
            e.DrawBackground();
            string text = ComboTitle(id);
            if ((e.State & DrawItemState.ComboBoxEdit) == 0) text += " — " + ComboCatalog.Description(id);
            TextRenderer.DrawText(e.Graphics, text, e.Font, e.Bounds, e.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        };
    }

    private void UpdateComboPreview()
    {
        UpdateArcherTimingSequence();
        string id = _preset.SelectedItem as string ?? _settings.ComboPreset;
        _help.SetToolTip(_preset, ComboCatalog.Description(id));
        _comboPreview.Visible = _page == "archery";
        string Key(int i, string fallback = "") => _keys.TryGetValue("ComboKey" + i, out var b) && b.Text.Length > 0 ? b.Text : fallback;
        string slide = _keys.TryGetValue("SlideKey", out var box) && box.Text.Length > 0 ? box.Text : "W";
        string signature = string.Join("|", _page, id, Key(1), Key(2), Key(3), slide);
        if (_previewSignature == signature) return;
        _previewSignature = signature;
        foreach (Control child in _comboPreview.Controls.Cast<Control>().ToArray()) child.Dispose();
        (string Skill, string Key)[] steps = id switch
        {
            ComboCatalog.FiveSlideThreeSlide or "3-5-w-3-5-w" => [("ArrowShower", Key(2, "5")), ("Slide", slide), ("MultipleShot", Key(1, "3")), ("Slide", slide)],
            ComboCatalog.ThreeSlideFiveSlide => [("MultipleShot", Key(1, "3")), ("Slide", slide), ("ArrowShower", Key(2, "5")), ("Slide", slide)],
            "3-5" => [("MultipleShot", Key(1)), ("ArrowShower", Key(2))],
            "5-3" => [("ArrowShower", Key(1)), ("MultipleShot", Key(2))],
            "70-72" => [("70", Key(1)), ("72", Key(2))],
            "70-60" => [("70", Key(1)), ("60", Key(2))],
            "70-72-60" => [("70", Key(1)), ("72", Key(2)), ("60", Key(3))],
            _ => []
        };
        foreach (var (skill, key) in steps)
        {
            var definition = SkillCatalog.Find(skill);
            var tile = new Panel { Size = new Size(165, 130), Margin = new Padding(8) };
            var image = new PictureBox { Location = new Point(58, 4), Size = new Size(40, 40), SizeMode = PictureBoxSizeMode.Zoom };
            if (definition != null) image.Image = SkillCatalog.LoadIcon(definition.IconFile);
            image.Disposed += (_, _) => image.Image?.Dispose();
            string hint = definition == null ? "Hareket adımı" : SkillCatalog.Description(definition.Id);
            _help.SetToolTip(image, hint);
            string name = definition?.DisplayName ?? skill;
            var caption = new Label { Location = new Point(0, 50), Size = new Size(165, 70), ForeColor = UiTheme.Text,
                TextAlign = ContentAlignment.TopCenter, Text = $"{name}\nTuş: {(key.Length == 0 ? "ATANMADI" : key)}\n→" };
            _help.SetToolTip(caption, definition == null && skill != "Slide" ? "Skill adımı" : hint);
            tile.Controls.Add(image); tile.Controls.Add(caption); _comboPreview.Controls.Add(tile);
        }
    }
}
