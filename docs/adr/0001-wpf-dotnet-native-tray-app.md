# ADR 0001 — .NET 10 + WPF, sürekli çalışan tepsi uygulaması

- **Durum:** Kabul edildi
- **Tarih:** 2026-11-11

## Bağlam

PrtScn'e basıldığı an ekran donmalı ve seçim arayüzü gecikmesiz açılmalı. Uygulama Windows ile başlayıp
arka planda çalışacak; çok monitör ve farklı ölçekleme (DPI) değerlerinde piksel-doğru kırpma gerekiyor.

## Karar

- **.NET 10 (LTS) + WPF.** Yerel Windows API'lerine (klavye kancası, DWM, MAPI, paylaşım) doğrudan erişim,
  donanım hızlandırmalı çizim ve küçük bellek ayak izi. Electron gibi web tabanlı seçenekler başlangıç
  süresi ve bellek açısından elendi.
- **Process tepsi uygulaması olarak sürekli açık.** PrtScn'de yeni process başlatılmaz; mevcut process ekranı
  yakalar ve overlay penceresini gösterir.
- **Düşük seviye klavye kancası (WH_KEYBOARD_LL) ayrı bir thread'de.** UI thread'i meşgul olsa bile
  Windows kancayı zaman aşımıyla kaldırmaz. PrtScn olayı yutulur; Windows'un Ekran Alıntısı Aracı açılmaz.
- **Per-Monitor V2 DPI farkındalığı.** Tüm yakalama ve seçim koordinatları fiziksel piksel. Overlay tek bir
  pencere olarak tüm sanal masaüstünü kaplar; içerik 1/DPI ölçekli bir Canvas'ta 1:1 piksel gösterilir.
- **Ek açıklamalar (annotation) tek bir çizim koduyla** hem canlı ekranda hem dışa aktarılan görüntüde
  render edilir, böylece "ekranda gördüğün = kaydedilen".
- **Tepsi simgesi için WinForms `NotifyIcon`.** WPF'te yerleşik tepsi simgesi yok; ek NuGet bağımlılığı
  yerine framework'ün kendi bileşeni kullanıldı.

## Sonuçlar

- Tek dosyalık, kendi runtime'ını içeren paket ~80 MB; .NET kurulu makineler için ~7 MB'lık
  framework-dependent paket de yayınlanır.
- Windows 10 2004 (19041) ve üzeri desteklenir (Windows paylaşım API'si için).
- WhatsApp/Teams'te belirli bir kişiye otomatik gönderim için resmi API yok; görüntü panoya kopyalanıp
  uygulama açılır ya da Windows paylaşım penceresi kullanılır.
