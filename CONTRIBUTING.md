# Katkı rehberi

Bu depo için henüz açık kaynak lisansı seçilmedi. Yeniden kullanım ve dağıtım hakları için README'deki haklar bölümünü inceleyin.

## Geliştirme

- .NET 8 SDK kullanın. Arayüz ve canlı input testleri Windows gerektirir.
- Değişiklik öncesinde [AGENTS.md](AGENTS.md) ve ilgili kodu okuyun.
- PR'ları tek bir davranış veya hata düzeltmesiyle sınırlayın; ayar dosyası uyumluluğunu koruyun.
- Kullanıcıya görünen açıklamalar Türkçe, mevcut C# adlandırma biçimi korunmalıdır.

## Doğrulama

```sh
dotnet build KO-Punisher/KO-Punisher.csproj -c Release -warnaserror
dotnet run --project Tests/Tests.csproj -c Release
dotnet run --project Tests.Ocr.Windows/Pure/Preprocessing.csproj -c Release
```

Windows dışındaki build komutuna `-p:EnableWindowsTargeting=true` ekleyin. Davranış değişikliklerine regresyon testi ekleyin. PR açıklamasında çalıştırılan kontrolleri ve canlı Windows/oyun testinin yapılıp yapılmadığını ayrı belirtin.

## Hata bildirimi

İşletim sistemi, job/preset, tekrar adımları, beklenen ve gözlenen sonucu belirtin. Ekran görüntülerindeki oyuncu/hesap bilgilerini ve loglardaki yerel yolları paylaşmadan önce temizleyin. Kişisel `settings.json`, tanılama logları, `.env`, derlenmiş paketler ve `bin/obj` dosyaları göndermeyin.

Güvenlik sorunları için [SECURITY.md](SECURITY.md) yönergelerini izleyin. Driver, oyun belleği okuma, enjeksiyon veya koruma atlatma bu projenin kapsamı dışındadır.
