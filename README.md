# ScreenCut

[![CI](https://github.com/mehmedozdemir/ScreenCut/actions/workflows/ci.yml/badge.svg)](https://github.com/mehmedozdemir/ScreenCut/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/mehmedozdemir/ScreenCut?include_prereleases&sort=semver)](https://github.com/mehmedozdemir/ScreenCut/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Windows için **PrtScn tuşuyla anında açılan** ekran alıntısı ve işaretleme aracı.
Arka planda çalışır, Windows ile başlar. Ekranı dondurur; bir alan ya da pencere seçip üzerine
kare, ok, kalem, metin, numara ve bulanıklaştırma ekleyebilir, tek tuşla kopyalayabilir,
kaydedebilir ya da WhatsApp, Teams veya e-posta ile paylaşabilirsiniz.

## Özellikler

- **Anında yakalama:** Uygulama tepside hazır bekler; PrtScn'e basınca ekran dondurulur.
- **Yakalama modları**

  | Kısayol         | Ne yapar |
  |-----------------|----------|
  | `PrtScn`        | Alan seç (sürükle) ya da pencerenin üzerine gelip tıkla |
  | `Alt+PrtScn`    | Etkin pencere |
  | `Shift+PrtScn`  | Farenin bulunduğu ekran |
  | `Ctrl+PrtScn`   | Tüm ekranlar |
  | Tepsi menüsü    | 3 saniye gecikmeli yakalama |

- **İşaretleme araçları:** dikdörtgen, elips, çizgi, ok, kalem, fosforlu kalem, metin
  (arka planlı ya da kenar çizgili), numaralı adımlar ①②③, bulanıklaştırma (pikselleştirme).
  8 renk, 3 kalınlık. Seç aracıyla şekiller taşınır, rengi değiştirilir, silinir. Sınırsız geri al / yinele.
- **Yeniden seçim:** Seçimi kenar tutamaçlarıyla ayarlayın, aynı görüntüde baştan seçin (`Ctrl+R`,
  çizimler korunur) ya da ekranı yeniden yakalayın (`F5`).
- **Hızlı işlemler araç çubuğu**

  | Kısayol            | İşlem |
  |--------------------|-------|
  | `Ctrl+C` / `Enter` | Görüntüyü panoya kopyala ve kapat |
  | `Ctrl+S`           | **Hızlı kaydet:** sormadan kaydeder, panoya **dosyanın kendisini ve tam yolunu** koyar. Explorer/Teams/Outlook'a yapıştırınca dosya eklenir, metin alanına yapıştırınca yol yazılır. |
  | `Ctrl+Shift+S`     | Farklı kaydet (PNG / JPEG) |
  | Paylaş menüsü      | Windows paylaşım penceresi · WhatsApp · Teams · E-posta (görüntü ekli) |
  | `Esc`              | İptal |

- **Araç kısayolları:** `V` seç · `R` dikdörtgen · `E` elips · `L` çizgi · `A` ok · `P` kalem ·
  `H` fosforlu kalem · `T` metin · `N` numara · `B` bulanıklaştır · `Shift` kare/daire/45° ·
  `Delete` seçili şekli sil.
- **Çoklu monitör ve yüksek DPI:** Farklı ölçeklemeli monitörlerde piksel-doğru kırpma.

## Kurulum

1. [Releases](https://github.com/mehmedozdemir/ScreenCut/releases) sayfasından
   `ScreenCut-X.Y.Z-win-x64.zip` dosyasını indirin. ARM cihazlar için `win-arm64` paketini kullanın.
2. Zip'i kalıcı bir klasöre açın, örneğin `%LOCALAPPDATA%\Programs\ScreenCut`.
3. `ScreenCut.exe`'yi çalıştırın. Tepside simge belirir ve uygulama Windows açılışına kendini ekler.

> **Not:** Exe dijital olarak imzalı değildir. İlk açılışta Windows SmartScreen uyarısı görürseniz
> *Ek bilgi → Yine de çalıştır* seçeneğini kullanın. Dosya bütünlüğünü `SHA256SUMS.txt` ile doğrulayabilirsiniz.

### Gereksinimler

- Windows 10 sürüm 2004 (19041) veya üzeri / Windows 11
- Önerilen paket için ek kurulum gerekmez. `framework-dependent` paket için
  [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) gerekir.

### Kaldırma

```powershell
ScreenCut.exe --uninstall
```

Bu komut otomatik başlatmayı kaldırır ve PrtScn tuşunu Windows'un Ekran Alıntısı Aracı'na geri verir.
Ardından uygulama klasörünü silebilirsiniz.

## Yapılandırma

Tepsi menüsü → **Ayarlar**:

| Ayar               | Varsayılan |
|--------------------|------------|
| Kayıt klasörü      | `Resimler\ScreenCut` |
| Dosya adı şablonu  | `ScreenCut_{yyyy}-{MM}-{dd}_{HH}-{mm}-{ss}` |
| Dosya biçimi       | PNG (JPEG kalitesi ayarlanabilir) |
| Windows ile başlat | Açık |

Dosya konumları:

- Ayarlar: `%APPDATA%\ScreenCut\settings.json`
- Günlük: `%LOCALAPPDATA%\ScreenCut\logs\screencut.log`
- Paylaşım için geçici dosyalar: `%TEMP%\ScreenCut` (1 gün sonra silinir)

Uygulama ortam değişkeni, ağ bağlantısı ya da gizli anahtar kullanmaz.

### PrtScn ve Windows Ekran Alıntısı Aracı

Windows 11'de PrtScn varsayılan olarak Ekran Alıntısı Aracı'nı açar. ScreenCut bu tuşu kendi alır ve
ilgili Windows ayarını (*Ayarlar → Erişilebilirlik → Klavye → Ekran yakalamayı açmak için Print Screen
tuşunu kullan*) kapatır. `Win+PrtScn` Windows'a bırakılır.

## Paylaşım nasıl çalışır?

- **Windows ile paylaş:** Sistem paylaşım penceresi açılır. Yüklü WhatsApp, Teams, Outlook ve
  Yakındaki Paylaşım burada listelenir.
- **WhatsApp / Teams:** Görüntü panoya kopyalanır ve uygulama açılır. Sohbeti seçip `Ctrl+V` yapın.
  Masaüstü uygulaması yoksa web sürümü açılır. Bu uygulamalar belirli bir kişiye otomatik gönderim
  için resmi bir arayüz sunmaz.
- **E-posta:** Varsayılan e-posta istemcisinde (ör. klasik Outlook) görüntü ekli yeni bir ileti açılır.
  MAPI desteklemeyen istemcilerde (ör. yeni Outlook) ekli bir `.eml` taslağı açılır.

## Mimari

```
src/ScreenCut
├── App.xaml.cs        Kompozisyon kökü: ayarlar, tepsi, klavye kancası
├── Native/            Win32: düşük seviye klavye kancası, pencere listesi, öne getirme
├── Capture/           Sanal masaüstü yakalama, yakalama yaşam döngüsü
├── Overlay/           Tam ekran seçim + çizim penceresi, araç çubuğu, dışa aktarım
├── Annotations/       Şekil modelleri ve geri al/yinele belgesi
├── Output/            Pano, dosya kaydı, Windows paylaşımı, WhatsApp/Teams, e-posta
├── Tray/              Bildirim alanı simgesi ve menüsü
├── Settings/          Ayarlar penceresi
└── Core/              WPF'ten bağımsız saf mantık (birim testli)
```

Tasarım kararları [docs/adr](docs/adr) klasöründedir.

## Geliştirme

```powershell
dotnet build ScreenCut.slnx
dotnet test ScreenCut.slnx
dotnet run --project src/ScreenCut
```

Gerekenler: Windows ve .NET 10 SDK. Dal stratejisi, commit formatı ve PR süreci
[CONTRIBUTING.md](CONTRIBUTING.md) dosyasında, sürüm çıkarma süreci [docs/RELEASING.md](docs/RELEASING.md) dosyasında.

## Lisans

[MIT](LICENSE)
