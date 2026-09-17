# Ayrıntılı kullanım ve tanılama

Windows / .NET 8 / WinForms. Varsayılan gönderim `keybd_event`; `SendInput` tanılama alternatifi olarak kalır. Kullanıcı SRGAME istemcisinde sohbet ve makro tepkisini doğruladı; yeni job/odak/GUI özelliklerinin oyun testi bekleniyor. Driver, oyun belleği okuma veya koruma atlatma içermez.

## Kullanım

1. `KO-Punisher.exe` dosyasını Windows'ta açın. Kaynaktan derleme yapıyorsanız aşağıdaki komutları kullanın.
2. Skill tuşlarını oyun barınızla eşleştirin. Yeni görsel editör için [Skill Bar ve kalibrasyon](skill-bar-calibration.md) rehberini kullanın; bu bölümdeki sayısal alanlar eski yerleşim modunu anlatır. Varsayılan Spike `3`, filler tuşları `4–9`. Filler sırası virgülle ayrılır; isimler `Thrust,BloodyBeast,Stab,Cut,Shock,Jab`. Boş skill tuşu o filleri kapatır; boş filler sırası yalnızca Spike + W/R çalıştırır.
3. **Combo tetik tuşu** varsayılan `XBUTTON1` (farenin bir yan tuşu), **Minor tetik tuşu** `XBUTTON2` (diğer yan tuşu). Yan tuş yoksa örneğin `F6` ve `F7` seçebilirsiniz. Pedalınız klavye tuşu üretiyorsa onun tuşunu tetik olarak yazın.
4. **Minor skill tuşu**, tetik tuşundan farklıdır: oyunda Minor hangi slottaysa onu yazın; örneğin `2`. Başlangıçta boştur ve Minor kapalıdır.
5. **Ayarları kaydet** düğmesine basın. Başarılı kayıt yolu ekranda görünür: `%LOCALAPPDATA%\KO-Punisher\settings.json`. Sonraki açılışta buradan yüklenir. Başlat tek başına ayarları kalıcı kaydetmez.
6. **Başlat / Hazırla** düğmesi hiçbir combo başlatmaz. Oyuna geçin, tetik tuşlarını bırakın; ardından istediğiniz tetiği basılı tutun.
   - Combo tetiği: **Skill → W → R** döngüsü. Spike hazırsa önceliklidir; değilse hazır filler sıradan seçilir. Hiçbir skill hazır değilken W/R devam eder.
   - Minor tetiği: kendi tekrar periyodunda yalnızca Minor. Combo ile aynı anda veya tek başına kullanılabilir.
   - Tuşu bırakınca ilgili döngü iptal edilir. Diğer döngü devam edebilir.
7. **F12** (ayarlanabilir) iki döngüyü durdurur ve hazırlığı kapatır. Yeniden Başlat gerekir. **Durdur** ve pencereyi kapatma da devam eden basışları bırakır.

Normal makro ve tek combo turu yalnız `TargetProcess` (varsayılan `KnightOnLine`) ön plandayken gönderir. Alt+Tab sırasında iki döngünün basılı tuşları bırakılır; Hold/Toggle kilidi sıfırlanır. Oyuna dönünce tetiği bırakıp yeniden basmak gerekir. Hedef bulunamazsa koruma devre dışı bırakılmaz.
Kontrol polling ile yapılır; odak kontrolü ile native gönderim arasında çok kısa bir yarış aralığı mümkündür. Sıfır gecikme/milisaniye hassasiyeti garanti edilmez. Tetik yutma seçeneği ve oyundaki çakışan atamalara dikkat edin.

## Job ayarları, tek tur ve durum paneli

- Her job için tuşlar, combo, Minor, tetik, zamanlamalar ve cooldown ayarları bağımsızdır; aynı job farklı adlandırılmış profillerde de ayrılır. İlk ziyaret diğer job tuşlarını kopyalamaz.
- Job geçişinde ve normal kapanışta taslaklar `%LOCALAPPDATA%\KO-Punisher\settings.json` içindeki `JobDrafts` alanına atomik kaydedilir. Eksik tuşlu taslak saklanabilir, fakat Başlat/tek tur doğrulamasından geçemez. Zorla kapatma son düzenlemeleri kaybettirebilir. Eski ayar dosyaları desteklenir; bozuk dosya otomatik ezilmez.
- **Atak → 3 sn sonra tek tur**: oyuna geçmek için üç saniye tanır. Seçili combo'nun tam bir motor turunu çalıştırır, tekrarlamaz; fiziksel tetik ve Minor döngüsü gerekmez. Asasta bir tur, sıradaki hazır skill ve seçili R/slide adımlarıdır; bütün filler listesini tek seferde tüketmez. Durdur, acil tuş, odak kaybı veya gönderim hatasında tuşlar bırakılır.
- **Durum paneli**: aktif job, combo, hazır/çalışıyor/duraklatıldı durumu ve tetikleri sol üstte gösterir. Odak almayan, tıklamayı geçiren ayrı masaüstü penceresidir; oyun içine enjekte edilmez. Exclusive fullscreen üzerinde görünmeyebilir; pencere/borderless modunda denenmelidir. Atak sekmesinden kapatılabilir.
- Normal makro odak korumasına rağmen **Tanılama → tek tuş** Not Defteri karşılaştırması için bilinçli olarak korunmuştur. OCR combo testi ise oyun odağı kaybında iptal edilir.

## Farm: buff, mob filtresi ve pazar mesajları

Tüm seçenekler başlangıçta kapalıdır; Farm sayfasındaki üç sekmeden ayarlanır ve job/profille birlikte saklanır. `keybd_event` varsayılanı değişmedi.

- **Buff zamanlayıcı:** Wolf, DEF 200/400/800, TS, Magic Hammer ve iki özel slot. Etkinleştir, oyun barındaki tuşunu, tekrar saniyesini ve kullanım sonrası beklemesini gir. Atak tetiği aktifken combo aralarına eklenir; Minor tetiği tek başına buff başlatmaz. Başlat/hazırla veya tek combo turu tek başına buff göndermez. Atak/Minor/tetik/buff tuşları çakışamaz. Süre, tuşun gönderildiği andan hesaplanır; ikon, gerçek uygulama ve eşya miktarı okunmaz. Magic Hammer için gereken eşya ve bütün süreler kullanıcı tarafından doğrulanmalıdır. Motor yeniden başlatılınca süreler sıfırlanır.
- **Mob filtresi:** Her satıra tam hedef adı gir. **3 sn sonra hedef adını kırp** ile oyuna geçip yalnız hedef adını seç; sonra **3 sn sonra yalnız OCR oku** ile metni kontrol et. Filtre açıkken her saldırı tuşundan önce OCR tam ad karşılaştırması yapılır; boş/farklı/kısmi metinde tuş gönderilmez. Z ile hedef seçimi devam eder, Z sonrası saldırı da filtrelenir. OCR hatası/zaman aşımı motoru durdurur. Bölge oyun penceresine görelidir: aynı boyutta taşınabilir; boyut/UI ölçeği değişince yeniden kırp. OCR hız ve doğruluğu ekran/fonta bağlıdır; exclusive fullscreen yakalama çalışmayabilir. Filtre **oyunda zaten başlamış otomatik saldırıyı durdurmaz**; OCR ile gönderim arasındaki hedef değişimi veya OCR yanlış okuması nedeniyle yanlış moba hiç vurulmayacağı garanti edilmez.
- **Pazar mesajları:** Satır başına bir mesaj, en fazla 160 karakter; 10–3600 saniye aralık ve 1–1000 toplam gönderim. Liste sırayla döner. Sohbeti ve Caps Lock'u kapat, onay kutusunu işaretle, **3 sn sonra pazarı başlat** düğmesine basıp oyuna geç. Combo, Minor, probe ve tek turla eşzamanlı çalışmaz. F9/Durdur, acil tuş, odak/pencere/klavye düzeni değişimi veya input hatası durdurur; dönüşte kendiliğinden devam etmez. Pano kullanılmaz; hedef klavye düzeni üzerinden gerçek tuşlar gönderilir. AltGr/dead-key gerektiren veya düzende bulunmayan karakterler sohbet açılmadan reddedilir. Yazarken klavyeyi kullanma. Sohbetin açılması ve mesajın sunucuya ulaşması otomatik doğrulanmaz; ilk denemede **toplam gönderim 1** kullan. Kesilirse kalan metni kontrol edip sohbeti ESC ile kapat. Pazar çalışırken tanılama kaydı açılmaz; aktif kaydı önce bitirmen gerekir, mesaj tuşları loglanmaz.

Windows/oyun testi: önce yalnız OCR okuma, ardından tek combo turunda izinli/izinsiz hedef; buff için tek etkin slot; pazar için tek kısa mesaj. Gerçek GUI, ekran yakalama, klavye düzeni ve SRGAME davranışı Windows'ta doğrulanmalıdır. `Tests.Windows` içindeki `--sendinput` seçeneği açıkça verilen native input testini test TextBox'ına uygular (Caps Lock kapalı olmalı); oyun testi değildir.

Sonraki kapsam: VIP depo, pet besleme, dayanıklılığı sıfırlanan silahı değiştirme, Upgrade. Discord/Telegram bildirimleri gelecek planıdır; bu sürümde uygulanmadı.

## Hızlı combo testi ve adlar

- **Hızlı test** başlangıçta kapalıdır; hareketli okçuda bağımsız süreler her zaman korunur. Diğer combolarda Atak sekmesinde **Hızlı test** açıkken **3 sn sonra tek tur**, mevcut tuşlarla skill basışı 50 ms, hareket/R basışı 30 ms ve aralar 100 ms olacak şekilde bir tur gönderir. Normal ayarlar değiştirilmez. Hızlı test, casting başarısı değil tuş sırası kontrolüdür; kutuyu kapatınca kendi zamanlamalarınız kullanılır.
- Sonuçta gönderilen tuşların sırası ve basış sayısı kalır; hata/odak kaybı ayrıca gösterilir. OS gönderimi oyunun skilli kabul ettiğini kanıtlamaz. Ekran görüntüsü yerine sonuç satırı paylaşılabilir.
- Combo adları `ComboCatalog` üzerinden liste, açıklama ve durum panelinde aynıdır. Eski JSON kimlikleri uyumluluk için korunur. Okçu önizlemesi seçili sırayı, gerçek tuşları ve Multiple Shot/Arrow Shower ikonlarını gösterir; 70/72/60 için doğrulanmamış ikon eşlemesi yapılmaz. Kısa açıklamalar kontrollerde ve skill görsellerinde tooltip, combo listesinde satır açıklaması olarak görünür.
- **5 Slide 3 Slide / 3 Slide 5 Slide:** yalnız **Diğer → Hareketli okçu** süreleri kullanılır; genel süreler, `Adım arası (ms)` ve jitter uygulanmaz. **3-5 / 5-3:** `Skill sonrası (ms)` her skill sonrasıdır; `Adım arası (ms)` tur sonuna eklenir. `Skill basışı (ms)` ve `Hareket basışı (ms)` tuşun basılı kaldığı süredir.

## Hareketli okçu: SteelSeries başlangıcı

Okçu → Diğer → **SteelSeries başlangıcı uygula**: `5 Slide 3 Slide`, Multiple Shot `6`, Arrow Shower `5`, hareket `W`, **Basılı tut** seçilir. Kalıcı olması için Kaydet. Mevcut diğer job ayarları değiştirilmez.

| Arayüz alanı (ms) | Kod / JSON alanı | Başlangıç |
|---|---|---:|
| Skill basışı | Timings.ArcherSkillHold | 230 |
| Atış beklemesi | Timings.ArcherSkillAfter | 230 |
| Hareket basışı | Timings.ArcherSlideHold | 19 |
| İlk hareket sonrası | Timings.ArcherSlideAfter | 19 |
| Son hareket sonrası | Timings.ArcherCycleAfter | 0 |

`5 bas → 230 → 5 bırak → 230 → W bas → 19 → W bırak → 19 → 6 bas → 230 → 6 bırak → 230 → W bas → 19 → W bırak → 0 → başa dön`.

Arayüzde olay sırası tuş/süre değişiklikleriyle güncellenir. Tetik basılıyken tekrar eder; bırakınca devam eden basış bırakılır, sıra durur. Tek tur aynı sürelerle yalnız bir kez çalışır. 0 ms yalnız ek bekleme olmadığı anlamındadır; OS scheduling, hedef/odak kontrolleri ve etkin farm işleri süre ekleyebilir. OCR combo testi tur içinde okumaz, her tam turdan sonra okur; OCR süresi tur arasına eklendiğinden sürekli döngü hızının testi değildir. OCR'da hata metni görülmemesi başarı kanıtı değildir. Hasar anı algılanmaz; casting failed vermeyeceği garanti edilmez.

## Zamanlama

- Varsayılan: skill basış `50 ms`, skill→W ara `30 ms`, W basış `30 ms`, W→R ara `20 ms`, R basış `30 ms`, döngü arası `120 ms`.
- Minor varsayılan periyot `100 ms`, basış `25 ms`; bunlar evrensel/garantili sunucu değerleri değil, ayarlanabilir başlangıç değerleridir. Periyot iki basışın başlangıçları arasındaki yaklaşık süredir.
- Spike `11000 ms`; filler cooldown'ları da başlangıç tahmini olarak `11000 ms`. Gerçek sunucu değerlerine göre ayrı ayrı değiştirin; filler `0` değeri cooldown kontrolünü o skill için kapatır.
- Cooldown, OS'ye başarılı gönderim anından hesaplanır; oyunda skillin gerçekten uygulandığı bilinmez. Tetiği bırakıp basmak cooldown'u sıfırlamaz. Uygulamayı/motoru yeniden başlatmak tahminleri sıfırlar.
- Bu sürüm HP/MP kontrol etmez; Minor basılı kaldığı sürece mana tüketebilir. Otomatik pot yoktur.
- **Tanılama** sekmesinden isteğe bağlı, en fazla 20.000 olaylık kayıt açılır. Normal kullanımda dosyaya input kaydı yapılmaz.
- Global Başlat/Durdur kısayollarını uygulamadaki ayarlardan kontrol edin. Skill Bar etkin olduğunda motor F1–F8 geçişlerini skill adresine göre yapar; bu barlarla çakışan tetik/kontrol kısayollarını değiştirin.

## Tuşlar hâlâ oyuna gitmiyorsa

1. Normal makro Not Defteri ön plandayken artık özellikle duraklar.
2. Not Defteri karşılaştırması için **Tanılama → 3 sn sonra tek tuş** kullanın; normal makroyu açmayın.
3. Oyun yönetici olarak çalışıyorsa uygulamanın yetki seviyesi de eşleşmelidir. Windows `SendInput`, daha yüksek bütünlük seviyeli uygulamalara gönderimi UIPI nedeniyle engelleyebilir. Bu, koruma atlatma yöntemi değildir.
4. Gönderim API'si başarısız olursa motor durur ve Win32 hata kodunu gösterir. `0` hata kodu, engel olmadığını kanıtlamaz; UIPI nedeni özel olarak raporlanmayabilir.
5. Not Defteri'nde çalışıp oyunda çalışmıyorsa oyun sentetik inputu kabul etmiyor olabilir. Daha hızlı tekrar bunu çözmez. Driver kurmayın veya güvenlik yazılımını kapatmayın; bu sürüm için oyun içinde çalışıyor garantisi verilmez.

## Tanılama kaydı gönderme

1. **Tanılama → Kaydı başlat**, ardından üstteki **Başlat**. Oyuna geçip tetik tuşunu birkaç kez basıp bırakın; sonra **Durdur**.
2. Tanılama ekranında ortamı seçin: Not Defteri / Oyun sohbeti / Oyun skill-kontrol. Tuş varsayılan `3`, basma süresi `100 ms`.
3. **3 sn sonra tek tuş** düğmesine basıp hedef pencereye geçin. Bu test tetik hook'una ihtiyaç duymadan gönderim yapar. Her denemeden sonra **Tepki var / Tepki yok** ile kendi gözleminizi kaydedin. Kontrol tuşları çıkış testi olarak kabul edilmez. Durdur/acil tuşu geri sayımı ve basışı iptal eder.
4. **Kaydı bitir → Log klasörünü aç**. Oluşan `input-*.jsonl` dosyasını paylaşın. Kayıt normalde EXE yanındaki `logs/` içindedir; yazma izni yoksa `%LOCALAPPDATA%\KO-Punisher\logs` kullanılır. Kesin yol ekranda gösterilir.

Kayıt: yapılandırılmış combo/minor/acil tetik geçişleri, uygulamanın tetiği yutup yutmadığı, motor başlangıcı/döngüleri/hataları, gönderilen VK/scan-code/down-up, `SendInput` dönüşü ve hata kodu, kendi sentetik olaylarının hook gözlemi, ön plan PID/süreç adı/klavye düzeni ve okunabiliyorsa bütünlük seviyesi. Log thread'i hook üzerinde disk I/O yapmaz; kuyruk dolarsa düşen kayıt sayısı dosya sonunda belirtilir. Her satır hemen diske yazılır; normal bitirişte kuyruk boşaltılır. Zorla kapatmada kuyruktaki son olaylar kaybolabilir.

Normal metin, diğer fiziksel tuşlar, pencere başlıkları, ekran görüntüleri ve hesap ayarları kaydedilmez. `physical_trigger` yokluğu tetiğin gözlenmediğini gösterir; tek başına koruma kanıtı değildir. `swallowed=true` makronun kendi tetik olayını oyuna geçirmediğini belirtir, oyun reddi değildir. `send_result.accepted=1` yalnızca Windows giriş kuyruğuna kabul anlamındadır; `own_injected_hook` da oyun onayı değildir. `GetLastError=0` UIPI engelini dışlamaz. Oyun kabulü yalnızca kullanıcı gözlemiyle ayrıca işaretlenir. Korunan sürecin yetkisi okunamazsa `null` ve Win32 hata kodu yazılır; yetkili olduğu varsayılmaz.

## Derleme ve test

Proje kökünde, .NET 8 SDK ile:

```cmd
dotnet build KO-Punisher/KO-Punisher.csproj -c Release -warnaserror
dotnet run --project Tests/Tests.csproj -c Release -warnaserror
```

Çalıştırılabilir normal build çıktısı: `KO-Punisher\bin\Release\KO-Punisher.exe`. Normal build'de exe'yi yanındaki DLL/runtime dosyalarından ayırmayın.

macOS çapraz derleme: build komutuna `-p:EnableWindowsTargeting=true` ekleyin. Bu, Windows'ta uygulamayı çalıştırmak değildir. Testler bağımsız .NET console test düzeneğidir; harici test paketi gerektirmez, sahte input ile motoru sınar.

Windows x64 self-contained paket oluşturmak için:

```cmd
dotnet publish KO-Punisher/KO-Punisher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -warnaserror -o publish
```

Paket .NET Runtime içerir; Windows x64 içindir. EXE, DLL, images ve tessdata dosyalarını birlikte taşıyın. Yayınlama başarısı Windows GUI/oyun testi yerine geçmez.

## Araştırma ve karar dayanakları

- [Kalais' Library — Rogue Assassin rehberi](http://ko.kalais.net/guide-rogsin.php): `skill-w-r` ve HP/mana yönetimi. Bu sürüm mevcut Skill→W→R modelini korur.
- [Steam topluluk rehberi](https://steamcommunity.com/sharedfiles/filedetails/?id=616541396): W basılı takip ve R/R/skill gibi alternatiflerden, doğru zamanlama ihtiyacından söz eder. Bunlar farklı tekniklerdir; tümünü aynı anda karıştırmak yerine bu sürümde tek açık sıra uygulanır. Başlığı Minor olsa da gövdesi büyük ölçüde attack combo anlatır; Minor zamanlaması için kanıt sayılmadı.
- [Assassin combo rehberi](https://kozymacro.com/en/blog/assassin-combo-guide/) ve [Minor rehberi](https://kozymacro.com/en/blog/minor-macro-guide/): ayrı Minor kontrolü ve cooldown takibi için ürün örnekleri. Ticari kaynaklardaki kusursuzluk/uyumluluk iddiaları doğrulanmış veri kabul edilmedi; kod veya driver alınmadı.
- [Microsoft — SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput): doğru `INPUT` boyutu, gönderilen olay sayısı, ön plandaki uygulama ve UIPI sınırları. Eksik native union düzeltildi; dönüş değeri artık kontrol ediliyor.

Kesin sunucu cooldown tablosu veya USKO/PVP oyun testi elde edilmedi. Süreler bu nedenle düzenlenebilir tahminlerdir.
