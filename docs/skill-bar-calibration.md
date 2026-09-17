# Skill Bar ve görsel kalibrasyon

- Job sayfalarındaki **Skill Bar** sekmesi: Archer, Assassin (Rogue), Priest, Battle Priest, Mage ve Warrior.
- Sağdaki havuz, seçili job için paketlenmiş `JobSkillCatalog` ikonlarını gösterir. Arama ve kategori filtresi vardır. Bu katalog tüm oyun/sunucu sürümlerinin eksiksiz skill listesi değildir; kaynak/kapsam için `skill-catalog-sources.md`.
- Solda F1–F8, her barda 1–9/0 slotları. Havuzdan kopyala; slotlar arasında sürükleyerek taşı; sağ tık/Delete ile temizle.
- İlk düzenlemeden itibaren yerleşim eski sayısal skill atamalarının yerine geçer. **Skill bar kaydet** job taslağını diske kaydeder. Üstteki **Kaydet** geçerli ayarları profile kaydeder. Tamamlanmamış bar saklanabilir fakat eksik combo başlatılamaz.
- Aynı skill birden fazla slottaysa ilk F barı/slotu kullanılır. Çalışırken düzenleme ve tarama kapalıdır.

## Combo eşleştirmesi

Arrow Shower F1/5 ve Multiple Shot F1/6 ise:

- `3-5`: Multiple Shot → Arrow Shower, yani **6 → 5**.
- `5-3`: Arrow Shower → Multiple Shot, yani **5 → 6**.
- Slide presetlerinde aynı kimlik eşleştirmesi korunur; aralara hareket tuşu girer.

Motor skill kimliğinden F barı ve tuşu çözer. Aynı slot numarası farklı barlarda kullanılabilir. Bar değiştirme ve skill basma iki lane arasında kilitlidir. Odak kaybı, iptal, acil durdurma ve elle bar değişikliği kontrolleri korunur.

Diğer combo/presetlerde saldırı kategorisindeki skiller F barı/slot sırasına göre kullanılır; birincil saldırıyı önce yerleştirin. `70-72`, `70-60`, `70-72-60` adları için katalogda güvenilir seviye metadatası yoktur: ilk iki/üç saldırı sırası kullanılır, isimden seviye tahmini yapılmaz. Rogue rotasyonunda yerleştirilmiş Spike önceliklidir; kalan saldırılar sıra/cooldown ile yürütülür. Destek ikonları saldırı rotasyonuna girmez. Kimliği olmayan eski pot/insert gibi sayısal alanlar F1'e aittir. F barı tuşuyla çakışan başlat/durdur/tetik kısayolları çalıştırma öncesinde reddedilir.

## OCR ile kalibre et

Bu buton yazı OCR'ı değil **ikon şablonu eşleştirmesi** yapar:

1. Editörde hedef F barını, oyunda aynı F barını açın.
2. Butona basın; onaydan sonra uygulama gizlenir.
3. Tek yatay/dikey barın 10 slotunu (1–0) sıkı bir dikdörtgenle seçin. ESC iptal eder.
4. Slot sonuçlarını adlarıyla inceleyip onaylayın. Katalogdaki orijinal ikonlar yerleşir; ekran kırpımı ikon olarak saklanmaz.
5. Tanınmayan/birbirine çok benzeyen slotlar boş kalır. Elle doldurun ve kaydedin.

Sıfır eşleşme veya iptal yerleşimi değiştirmez. Onaylanan tarama yalnız seçili barı yeniler; diğer barlara dokunmaz. Bilinmeyen slotta eski bir skill bırakılmaz. Cooldown kararması, farklı ikon paketleri, hatalı kırpma ve düşük çözünürlük tanımayı engelleyebilir. Bir seferde yalnız bir bar taranır; çoklu bar ekranını tek seferde tarama desteklenmez.

## Doğrulama

- `dotnet run --project Tests/Tests.csproj -c Release -- --calibration`: gerçek katalog ikon örnekleri, belirsizlik/bilinmeyen ikon reddi, atomik uygulama, tüm job atamaları, Archer combo sırası, çapraz bar ve iptal testleri.
- `dotnet run --project Tests/Tests.csproj -c Release`: tüm platform bağımsız testler.
- `dotnet build KO-Punisher/KO-Punisher.csproj -c Release -warnaserror -p:EnableWindowsTargeting=true`: macOS çapraz derleme.

Windows WinForms sürükle-bırak/DPI, ekran seçimi ve canlı oyun kabulü ayrıca Windows üzerinde doğrulanmalıdır. RGB fixture testleri GDI ekran yakalama testi değildir.
