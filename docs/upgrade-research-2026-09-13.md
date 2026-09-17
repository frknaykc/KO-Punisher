# Feature Ideas — KO-Punisher Upgrade
_Generated 2026-09-13 · ücretsiz, yerel uygulama yolları; uygulama değişikliği değil araştırma_

## Project snapshot
- **What it is:** Knight Online/SRGAME için Windows makro uygulaması.
- **Stack:** C# / .NET 8 WinForms; proje manifestinde üçüncü taraf paket referansı yok.
- **Current features:** `KO-Punisher/OcrReader.cs:10` ekran bölgesini okuyabiliyor; `:15` CopyFromScreen, `:20` Windows OCR kullanıyor. Çalışan klavye gönderimi önceki kullanıcı testinde keybd_event ile doğrulandı; bu, fare işlemlerinin oyun tarafından kabul edildiğini göstermez.
- **Key gaps / opportunities:** Kaynak aramasında Upgrade uygulaması veya SetCursorPos/mouse_event bağlantısı bulunmadı. Fare kabulü, pencere/DPI kalibrasyonu, upgrade durum takibi ayrıca doğrulanmalı.

## Shortlist (prioritized)
| # | Feature | Value | Effort | Free path (headline) |
|---|---------|-------|--------|----------------------|
| 1 | Kalibrasyonlu, kullanıcı onaylı tek deneme | High | M | Mevcut WinForms, ekran yakalama ve Windows API |
| 2 | Görsel doğrulama ve sonuç takibi | High | M/L | Windows OCR + isteğe bağlı OpenCvSharp |
| 3 | Sınırlı kuyruk ve durdurma kuralları | High | M | Yerel C# durum makinesi ve JSON kayıt |

## 1. Kalibrasyonlu, kullanıcı onaylı tek deneme
**Pitch:** Seçili item/scroll ve pencere konumunu göster; geri dönüşü olmayan upgrade onayını ilk sürümde kullanıcı versin.
**Why it matters:** Yanlış itemi veya yanlış scroll'u işleme riskini azaltır.
**How it fits:** Mevcut bölge yakalama yaklaşımını kullanır; yeni Upgrade sayfası, ayar modeli ve fare adaptörü gerekir. Bunlar öneridir, var olan semboller değildir.
**Free implementation:**
- Mevcut Windows/.NET kurulumu ve yerel C# kodu; ek bulut/abonelik gerektirmez.
- **Integration approach:** Ekran mutlak koordinatı yerine oyun penceresine göre kalibre edilmiş konumlar; DPI/çözünürlük değişiminde yeniden kalibrasyon. Klavye çalıştığı için fare çalışıyor varsayılmaz.
**Effort & phasing:** M — önce işaretleme/kuru prova, sonra değersiz eşya ile kullanıcı gözetimli tek deneme.
**Risks / caveats:** Sabit koordinat ve süre oynatımı kırılgandır. Sunucu otomasyon kuralları ayrıca kontrol edilmeli; izin doğrulanmadı.

## 2. Görsel doğrulama ve sonuç takibi
**Pitch:** Tıklama sayısından değil ekrandaki değişimlerden işlem durumunu belirle.
**Why it matters:** Gecikmiş sonuçta ikinci kez onay basılmasını ve başarının tahmin edilmesini önler.
**How it fits:** `OcrReader.cs` üzerindeki ekran/OCR altyapısı yeniden kullanılabilir. Upgrade için item adı, +seviye ve sonuç metni ayrı okuyucularla değerlendirilir.
**Free implementation:**
- Windows OCR: projede mevcut, yerel; ilgili OCR dilinin kurulması gerekir. Ekran görüntüsündeki küçük + işaretini kusursuz okuyacağı varsayılmaz.
- OpenCvSharp: Apache-2.0 lisansı doğrudan kaynak LICENSE dosyasından doğrulandı; native runtime ve dağıtım lisans bildirimleri gerekir. https://github.com/shimat/opencvsharp ve https://raw.githubusercontent.com/shimat/opencvsharp/main/LICENSE
- **Integration approach:** Şablon eşleme pencere/düğme konumunu, OCR yazıyı tespit eder; ikon tek başına item kimliği/seviyesi kanıtı değildir. Birden fazla tutarlı görüntü ve işlem öncesi/sonrası karşılaştırması kullanılır.
**Effort & phasing:** M/L — önce kayıtlı ekran örneklerinde ölçüm, sonra gözetimli canlı deneme.
**Risks / caveats:** Animasyon, örtüşen pencereler ve OCR belirsizliği. Sonuç bilinmiyorsa tekrar deneme değil durdurma ve kullanıcı incelemesi.

## 3. Sınırlı kuyruk ve durdurma kuralları
**Pitch:** Kullanıcının seçtiği itemleri hedef seviyeye, harcama ve deneme sınırlarına göre işle.
**Why it matters:** Hedefe ulaşmış itemin yeniden basılması ve kaynakların sınırsız tüketilmesi önlenir.
**How it fits:** Combo/Farm ile aynı anda çalışmayan ayrı iş akışı önerilir; mevcut motorun tamamlandığı veya Upgrade entegrasyonu bulunduğu iddiası yok.
**Free implementation:**
- Yerel C# durum makinesi, CancellationToken ve JSON günlük; harici servis yok.
- **Integration approach:** Hazır → pencere doğrulama → item/scroll doğrulama → kullanıcı onayı → sonuç bekleme → sonuç doğrulama → dur/sonraki item. Yanıt zaman aşımı otomatik tekrar değildir. Yeniden başlatma belirsiz işlemden kendiliğinden devam etmez.
**Effort & phasing:** M, ancak tek deneme/görsel doğrulama kabul testlerinden sonra.
**Risks / caveats:** Upgrade item kaybına yol açabilir. Korunan slotlar, hedef seviye, scroll türü, deneme/harcama sınırı, odak kaybında durma ve acil durdurma gerekir. Değerli kaynak kullanımı açık seçim olmalı.

## Oyun verileri ve kapsam
SRGAME'nin kendi sayfası doğrudan okunabildi: https://www.srgame.com.tr/upgrade_oranlari.php . Sayfa Blessed Upgrade Scroll, Trinas Piece + Blessed Upgrade Scroll, Rebirth, Reverse/Tears ve Accessory Compound gibi farklı gruplar sunuyor. Bunlar tek reçete sayılmamalı. İlk sürüm için yalnız normal item upgrade önerilir; aksesuar birleştirme/rebirth ayrı sonraki kapsamdır.

Tablo sayfada yayınlanan veridir; çalışan sunucu davranışının garantisi değildir. Kesin oranlar bu raporda aktarılmadı; item sınıfı, scroll ve sürüm eşleşmesi doğrulanmadan hesaplamaya alınmamalı. Yanıltıcı veya tutarsız görünen etiketler sessizce düzeltilmemeli.

Zamanlama, önce başka item yakma veya belirli tıklama ritminin başarı olasılığını artırdığına dair doğrulanmış kanıt bulunmadı. Hedef şansı değiştirmek değil, kullanıcının seçtiği akışı doğru ve kontrollü yürütmektir. İstemci belleği/packet manipülasyonu ve koruma atlatma bu önerinin kapsamı dışındadır.

## Doğrulama sınırı
Kaynak incelemesi ve canlı SRGAME sayfası/lisans kontrolü yapıldı. Genel web extract aracı gateway hatası verdi; iki doğrudan HTTP okuması alternatif olarak kullanıldı. Yeni paket kurulmadı, uygulama kodu değiştirilmedi, oyun üzerinde upgrade/fare denemesi yapılmadı.
