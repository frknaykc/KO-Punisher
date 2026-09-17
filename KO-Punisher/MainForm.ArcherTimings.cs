using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

public partial class MainForm
{
    private static readonly (string Key, string Title, string Hint, int Default, int Min)[] ArcherTimingFields =
    [
        ("ArcherSkillHold", "Skill basışı", "Basılı tutma", 230, 5),
        ("ArcherSkillAfter", "Atış beklemesi", "Skill → hareket", 230, 0),
        ("ArcherSlideHold", "Hareket basışı", "Basılı tutma", 19, 5),
        ("ArcherSlideAfter", "İlk hareket sonrası", "Hareket → skill", 19, 0),
        ("ArcherCycleAfter", "Son hareket sonrası", "Tur dönüşü", 0, 0)
    ];
    private Label? _archerTimingSequence;

    private void AddArcherTimingControls(Control parent)
    {
        Add(parent, Lbl("Hareketli okçu · 5 Slide 3 Slide / 3 Slide 5 Slide · tüm süreler ms", 12, 126));
        Add(parent, Lbl("Yalnız bu bölüm kullanılır; yukarıdaki genel süreler, Adım arası ve jitter uygulanmaz.", 12, 150));
        for (int i = 0; i < ArcherTimingFields.Length; i++)
        {
            var field = ArcherTimingFields[i];
            int x = 12 + i * 157;
            var label = Lbl(field.Title, x, 180);
            Add(parent, label);
            var number = Num(field.Key, x, 200, field.Min, 2000);
            Add(parent, number);
            number.ValueChanged -= ArcherTimingChanged;
            number.ValueChanged += ArcherTimingChanged;
            _help.SetToolTip(label, field.Hint);
            _help.SetToolTip(number, field.Hint);
        }
        var reference = UiTheme.NavButton("SteelSeries başlangıcı uygula");
        reference.Name = "ArcherReference";
        reference.Location = new Point(12, 245);
        reference.Width = 255;
        _help.SetToolTip(reference, "Referans süreler");
        reference.Click += (_, _) =>
        {
            if (_engine?.IsArmed == true || !_probeTask.IsCompleted)
            {
                _status.Text = "Referans ayarları uygulamadan önce Durdur.";
                return;
            }
            if (_page != "archery")
            {
                _status.Text = "Önce Okçu jobunu seç.";
                return;
            }
            _preset.SelectedItem = ComboCatalog.FiveSlideThreeSlide;
            SetKey("ComboKey1", "6");
            SetKey("ComboKey2", "5");
            SetKey("SlideKey", "W");
            foreach (var field in ArcherTimingFields) SetNum(field.Key, field.Default);
            _runMode.SelectedIndex = 0;
            _quickCombo.Checked = false;
            _jitter.Checked = false;
            UpdateComboGuide();
            _status.Text = "5 → W → 6 → W · basılıyken tekrar · 230/230/19/19/0 ms. Kalıcı olması için Kaydet.";
        };
        Add(parent, reference);
        _archerTimingSequence = new Label
        {
            Location = new Point(12, 290), Size = new Size(790, 110), ForeColor = UiTheme.Gold
        };
        Add(parent, _archerTimingSequence);
        UpdateArcherTimingSequence();
    }

    private void ArcherTimingChanged(object? sender, EventArgs e) => UpdateArcherTimingSequence();

    private void UpdateArcherTimingSequence()
    {
        if (_archerTimingSequence == null) return;
        string preset = _preset.SelectedItem as string ?? _settings.ComboPreset;
        if (!ComboCatalog.IsSlide(preset))
        {
            _archerTimingSequence.Text = "Bu süreleri kullanmak için hareketli okçu combosunu seç veya başlangıç düğmesine bas.";
            return;
        }
        string Key(string name, string fallback) => _keys.TryGetValue(name, out var box) && box.Text.Length > 0 ? box.Text : fallback;
        int Ms(string name) => _nums.TryGetValue(name, out var box) ? (int)box.Value : 0;
        string three = Key("ComboKey1", "3"), five = Key("ComboKey2", "5"), slide = Key("SlideKey", "W");
        string first = preset == ComboCatalog.ThreeSlideFiveSlide ? three : five;
        string second = preset == ComboCatalog.ThreeSlideFiveSlide ? five : three;
        string Row(string key, int after) => $"{key} bas → {Ms("ArcherSkillHold")} ms → {key} bırak → {Ms("ArcherSkillAfter")} ms → " +
            $"{slide} bas → {Ms("ArcherSlideHold")} ms → {slide} bırak → {after} ms";
        _archerTimingSequence.Text = Row(first, Ms("ArcherSlideAfter")) + "\n" +
            Row(second, Ms("ArcherCycleAfter")) + " → başa dön\n\n" +
            "Basılı tut: tetik bırakılınca durur. Tek tur: iki skill ve iki hareketten sonra durur.\n" +
            "0 ms = ek bekleme yok; işletim sistemi zamanlaması birebir garanti edilmez.";
    }
}
