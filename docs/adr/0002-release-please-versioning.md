# ADR 0002 — release-please ile otomatik sürümleme

- **Durum:** Kabul edildi
- **Tarih:** 2026-11-11

## Bağlam

Sürüm numarası, CHANGELOG ve GitHub Release'lerin elle yönetilmesi hataya açık; sürümlerin
SemVer'e uyması ve her değişikliğin izlenebilir olması isteniyor.

## Karar

- Commit (squash edilmiş PR başlığı) mesajları **Conventional Commits** formatında; CI'da denetlenir.
- **release-please** `main` üzerindeki commit'lerden bir sonraki sürümü hesaplar ve bir release PR'ı açık tutar.
- Tek doğruluk kaynağı `.release-please-manifest.json`; `Directory.Build.props` içindeki `<Version>`
  release-please tarafından güncellenir ve derlenen exe'ye işlenir.
- Release PR merge edildiğinde tag + GitHub Release oluşur, paketler aynı iş akışında üretilip eklenir.

## Alternatifler

- **Elle tag + MinVer:** Basit ama CHANGELOG üretmez ve insan hatasına açık.
- **semantic-release:** Node ekosistemine bağımlı; merge anında doğrudan yayınlar, gözden geçirme adımı yok.

## Sonuçlar

- Yayın, bir PR merge'ü kadar kolay ve gözden geçirilebilir.
- Squash merge zorunlu; PR başlıkları anlamlı olmalı.
