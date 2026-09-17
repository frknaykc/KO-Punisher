using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

public partial class MainForm
{
    private readonly Label[] _comboKeyLabels = Enumerable.Range(0, 3)
        .Select(_ => new Label { AutoSize = true, ForeColor = UiTheme.Text }).ToArray();
    private readonly Label _comboGuide = new()
    {
        Location = new Point(12, 328), Size = new Size(780, 128), ForeColor = UiTheme.Muted
    };

    private static string ComboTitle(string id) => ComboCatalog.Title(id);

    private void UpdateComboGuide()
    {
        string preset = _preset.SelectedItem as string ?? _settings.ComboPreset;
        UpdateComboPreview();
        bool slideTiming = ComboCatalog.IsSlide(preset);
        foreach (string name in new[] { "SkillKeyHold", "SkillToWDelay", "WKeyHold", "WToRDelay", "RKeyHold", "NextSkillDelay", "ComboSpeedMs", "JitterRange" })
            if (_nums.TryGetValue(name, out var number)) number.Enabled = !slideTiming;
        _jitter.Enabled = !slideTiming;
        bool rotation = preset is "rotation" or "assassin-rr" or "assassin-skill-r" or "assassin-r-skill";
        string[] roles = preset switch
        {
            ComboCatalog.FiveSlideThreeSlide or "3-5-w-3-5-w" or ComboCatalog.ThreeSlideFiveSlide => ["Multiple Shot (boş: 3)", "Arrow Shower (boş: 5)", "Kullanılmıyor"],
            "3-5" => ["Multiple Shot", "Arrow Shower", "Kullanılmıyor"],
            "5-3" => ["Arrow Shower", "Multiple Shot", "Kullanılmıyor"],
            "70-72" => ["70 skill tuşu", "72 skill tuşu", "Kullanılmıyor"],
            "70-60" => ["70 skill tuşu", "60 skill tuşu", "Kullanılmıyor"],
            "70-72-60" => ["70 skill tuşu", "72 skill tuşu", "60 skill tuşu"],
            "staff-r" => ["Staff saldırı skilli", "Kullanılmıyor", "Kullanılmıyor"],
            "bp-rr" => ["BP saldırı skilli", "Kullanılmıyor", "Kullanılmıyor"],
            _ when rotation => ["Skill bar kullanılır", "Skill bar kullanılır", "Skill bar kullanılır"],
            _ => ["Saldırı skilli (isteğe bağlı)", "Kullanılmıyor", "Kullanılmıyor"]
        };
        for (int i = 0; i < roles.Length; i++)
        {
            _comboKeyLabels[i].Text = roles[i];
            if (_keys.TryGetValue("ComboKey" + (i + 1), out var box))
                box.Enabled = !rotation && roles[i] != "Kullanılmıyor";
        }
        if (_checks.TryGetValue("WsCombo", out var slideMode)) slideMode.Enabled = preset == "rotation";
        string sequence = preset switch
        {
            "assassin-skill-r" => "Cooldown'lu skill → R. Slide gönderilmez.",
            "assassin-rr" => "Cooldown'lu skill → R → R. İki R ayrı basılıp bırakılır.",
            "assassin-r-skill" => "R → cooldown'lu skill. Slide gönderilmez.",
            "rotation" => "Skill bar rotasyonu; seçime göre Skill → Slide → R veya Skill → R.",
            ComboCatalog.ThreeSlideFiveSlide => "Multiple Shot → Slide → Arrow Shower → Slide. Özel tuşlar boşsa 3/5 kullanılır.",
            "staff-r" => "Yakın mesafe staff skilli → R. Tuşu oyun barındaki staff saldırısına ata.",
            "bp-rr" => "BP saldırı skilli → R → R. Örneğin barındaki Helis/Judgment saldırı tuşu.",
            "rr" => "Varsa saldırı skilli → R → R. Tuş boşsa yalnız R → R.",
            "r" => "Varsa saldırı skilli → R. Tuş boşsa yalnız R.",
            ComboCatalog.FiveSlideThreeSlide or "3-5-w-3-5-w" => "Arrow Shower → Slide → Multiple Shot → Slide. Tuş atamaları aşağıdadır; boşsa 3/5 kullanılır.",
            _ => "Yukarıdaki skill tuşları soldan sağa gönderilir; R veya hareket eklenmez."
        };
        string job = _page switch
        {
            "archery" => "3-5, üçlü/beşli ok skilleridir; klavyedeki 3/5 zorunlu değildir. Mesafe ve sunucu davranışı sonucu etkiler.",
            "assassin" => "Spike önceliği ve filler cooldown'ları korunur. Minor ayrı tetiktedir; iyileşme sonucu okunmaz.",
            "mage" => "Staff combo ile nova/meteor alan büyüsü farklıdır. Bu mod otomatik alan hedefleme yapmaz.",
            "bp" or "priest" => "Heal/HP/MP ayrı tetiktedir. Otomatik HP ölçümü veya cure/debuff önceliklendirmesi yoktur.",
            _ => "Her job için ayrı profil kaydet; tuşlar kendiliğinden değiştirilmez."
        };
        string timing = preset is ComboCatalog.FiveSlideThreeSlide or ComboCatalog.ThreeSlideFiveSlide or "3-5-w-3-5-w"
            ? "Diğer → Hareketli okçu: bağımsız basış/beklemeler. Genel süreler, Adım arası ve jitter uygulanmaz."
            : rotation ? "Skill sonrası (ms), R öncesi (ms), Tur aralığı (ms): rotasyon beklemeleri. Adım arası kullanılmaz."
            : "Skill sonrası (ms): skillden sonraki bekleme. Adım arası (ms): turun sonundaki ek bekleme.";
        _comboGuide.Text = sequence + "\n" + timing + "\n\n" + job +
            "\n\nHız ve Diğer sekmesindeki gecikmeler ayarlanabilir; evrensel ideal ms yoktur. Yeni combolar oyunda test edilmelidir.";
    }
}
