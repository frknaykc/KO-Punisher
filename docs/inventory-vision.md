# Envanter ve tooltip görüntü algılama

`KOPunisher.InventoryVision` yalnızca BCL kullanır. `Rectangle`/`Point` değer tipleri dışında System.Drawing bağımlılığı yoktur; Bitmap, WinForms, OCR, ağ veya oyun belleği kullanmaz. Girdi tam satır-sıralı RGB'dir (piksel başına R,G,B). Null girdi `ArgumentNullException`, eksik/fazla bayt, geçersiz boyut veya kare dışı slot `ArgumentException` üretir. Bulunamayan ya da birden fazla ayrı aday bulunan sahne `null`; boşluğu kanıtlanamayan slot `false` döndürür.

## Algoritma ve sınırlar

- Envanter: verilen altın/teal skin'in başlık dokusu ve **Inventory yazısının piksel imzası** aranır. Ekranın mutlak konumu kullanılmaz. Bulunan başlığın skin-geometrisine göre 7×4, 49 px aralıklı ızgara önerilir; yatay çizgiler ve sekiz dikey sınırın dört satırdaki örnekleri ayrı doğrulanır. Başlık tek başına yeterli değildir. Bag/equipment alanı sonuç dikdörtgenine dahil edilmez.
- Bu sürümün doğrulanmış envanter ölçekleri **1, 1.25, 1.5 ve 2** (nearest-neighbor büyütme). 0.75 küçültme desteklenmez ve testte reddedilir. Diğer DPI, anti-alias/bilinear ölçekleme, başka skin/dil, renk filtresi veya ağır örtülme için genelleme garantisi yoktur. Başlık ve ızgaranın tamamı kare içinde olmalıdır. Büyütmede birkaç piksellik sınır sapması mümkündür.
- Tooltip: yaklaşık RGB(136,119,68) altın yatay ayırıcılar, aynı başlangıç/genişlik, ikon ile başlık/kategori bölümünün ayırıcı aralığı, parlak ikon/başlık kanıtı ve çoğunlukla koyu yarı saydam panel birlikte aranır. Sabit ekran konumu veya tüm ekran metni okunmaz. Tooltip'in envanter üstüne gelmesi, solda/yukarıda olması sorun değildir. `hover` yalnızca geniş bir uzamsal eleme uygular; imlecin panel içinde olması beklenmez.
- Tooltip genişliği 210–630 piksel altın ayırıcı ile sınırlıdır. İç ayırıcılar dinamik yüksekliği belirler; bilinen skin'in son ayırıcı altındaki 28 px footer ve üstteki 24 px Inventory bandı ölçeklenir. Sonuç kareye kırpılır. Alt kenarı kırpılmış örnek test edilir. İkon/ilk iki ayırıcı kayıpsa güvenilir algılama beklenmez; görünmeyen metin yeniden oluşturulmaz. Birbirine değen/örtüşen karşılaştırma panelleri destek kapsamı dışındadır.
- Boş slot: yalnız karanlık olmak yeterli değildir. Bilinen boş slotun 38×38 iç dokusu **her pikselde** düşük toleransla eşleşmelidir; ayrıca seyrek ön kontrol vardır. Pozitif boşluk desteği yalnız 49×49, 1× ölçektedir. Farklı boş dokuların false-negative olması bilinçlidir. Görsel olarak aynı piksel verisine sahip gizli bir nesneyi hiçbir görüntü algılayıcı ayıramaz; bu çıktı oyun durumunun kesin kanıtı değildir.

## Fixture'lar ve test

`Tests/Fixtures/Inventory/` içindeki `elemental.rgb.gz`, `no-tooltip.rgb.gz`, `helmet.rgb.gz`, `potion.rgb.gz`, `enchant-hp.rgb.gz`, `pathos.rgb.gz` kullanıcının altı PNG görüntüsünden Pillow `Image.open(path).convert("RGB")` ile kayıpsız çözülmüştür. Gzip içeriği little-endian int32 width, int32 height ve tam RGB baytlarıdır. Tam ekran fixture'lar oyun sohbet/HUD verisini de taşır; paylaşmadan önce gözden geçirilmelidir.

`InventoryVisionTests.Run()` doğrulama sayısını döndürür; başarısızlıkta exception üretir. Test projesi iki C# kaynağını linklemeli ve `Tests/Fixtures/Inventory/*.gz` dosyalarını çıktıdaki `Fixtures/Inventory/` dizinine kopyalamalıdır. Testler gerçek altı kareyi, tooltip yokluğunu, beklenen 7×4 sınırlarını, çevirme/kırpma/büyütmeyi, yarı saydam Pathos panelini, alt kırpmayı, birden fazla adayı, ızgarasız başlığı, ikonsuz paneli, salt altın çizgileri, karanlık nesne işaretini ve bozuk girdileri kapsar.

Bu çalışma için bağımsız net8 harness doğrulama komutu:

```sh
/tmp/ko-dotnet/dotnet run --project /tmp/ivtest/ivtest.csproj -c Release -warnaserror
```

Harness geçicidir; kalıcı test projesine bağlantı/kopyalama üst entegrasyon tarafından yapılır. Canlı Windows oyunu, ekran yakalama gecikmesi, farklı donanım ve sürekli tarama performansı ölçülmedi; gerçek zamanlı performans garantisi verilmez. İlk regresyon çalıştırmaları desteklenmeyen küçültme ve örnekleme fazı sorunlarını yakaladı; kapsam doğrulanmış büyütmelerle sınırlandırıldı.
