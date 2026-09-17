# KO-Punisher

- Uygulama: `KO-Punisher/`, C# / .NET 8 / WinForms, Windows hedefi.
- Windows doğrulaması: `dotnet build KO-Punisher/KO-Punisher.csproj -c Release -warnaserror`.
- macOS üzerinde çapraz derleme için `-p:EnableWindowsTargeting=true` ekle; bu Windows çalışma zamanı testi değildir.
- Değişiklikleri küçük patchlerle uygula; dosyaları gereksiz yere yeniden yazma.
- ZIP içine yalnızca kaynak dosyaları al; bin/obj klasörlerini ekleme.
- Derleme doğrulanmadan başarı iddia etme.
