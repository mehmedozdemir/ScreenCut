# Sürüm yönetimi

ScreenCut sürümleri tamamen otomatiktir. Elle sürüm numarası değiştirilmez, tag atılmaz, CHANGELOG yazılmaz.

## Akış

```
feature/… dalı ──PR (squash)──▶ main ──▶ release-please "release PR"'ı günceller
                                                  │
                                    release PR merge edilir
                                                  │
                                 tag vX.Y.Z + GitHub Release + CHANGELOG
                                                  │
                                 publish işi zip'leri ve SHA256SUMS'u ekler
```

1. Her değişiklik bir daldan (`feature/…`, `fix/…`) **PR** olarak gelir.
2. PR başlığı [Conventional Commits](https://www.conventionalcommits.org/tr/v1.0.0/) formatındadır;
   `PR Title` iş akışı bunu denetler. PR'lar **squash** ile birleştirilir, yani başlık `main`'deki commit mesajı olur.
3. `main`'e her push'ta [release-please](https://github.com/googleapis/release-please) açık bir
   **release PR** tutar (`chore(main): release X.Y.Z`). Bu PR şunları içerir:
   - `CHANGELOG.md` içine Türkçe başlıklarla gruplanmış yeni bölüm,
   - `Directory.Build.props` içindeki `<Version>` güncellemesi,
   - `.release-please-manifest.json` ve `version.txt`.
4. Yayına hazır olunca release PR **merge edilir**. Bu:
   - `vX.Y.Z` tag'ini ve GitHub Release'i oluşturur,
   - `publish` işini tetikler; testler koşar, paketler üretilip Release'e eklenir.

## Sürüm numarası nasıl belirlenir?

| Commit                                  | 0.x döneminde | 1.0 sonrası |
|-----------------------------------------|---------------|-------------|
| `fix: …`                                | PATCH         | PATCH       |
| `feat: …`                               | MINOR         | MINOR       |
| `feat!: …` / `BREAKING CHANGE:` footer  | MINOR         | MAJOR       |
| `perf`, `refactor`, `revert`            | PATCH         | PATCH       |
| `docs`, `chore`, `ci`, `build`, `test`, `style` | sürüm yok | sürüm yok |

Sürüm çıkarmayan tipler CHANGELOG'da görünmez; bir sonraki sürümde kodla birlikte yayına girer.

Proje `0.x` ile başlar; API/davranış kararlı hale gelince `Release-As: 1.0.0` footer'ı olan bir commit ile 1.0'a geçilir.

## Yayın paketleri

| Dosya                                            | Açıklama |
|--------------------------------------------------|----------|
| `ScreenCut-X.Y.Z-win-x64.zip`                    | Önerilen. Tek dosya, .NET kurulumu gerekmez. |
| `ScreenCut-X.Y.Z-win-arm64.zip`                  | ARM tabanlı Windows cihazlar için. |
| `ScreenCut-X.Y.Z-win-x64-framework-dependent.zip`| Küçük boyut; .NET 10 Desktop Runtime kurulu olmalı. |
| `SHA256SUMS.txt`                                 | Bütünlük doğrulaması. |

## Yerel paket üretmek

```powershell
./scripts/publish.ps1 -Version 0.1.0
# çıktı: artifacts/
```

## Acil düzeltme (hotfix)

`main` her zaman yayınlanabilir durumdadır. Hotfix, `fix/…` dalında yapılır, PR ile `main`'e girer;
release-please bir PATCH sürümü önerir, release PR merge edilerek yayınlanır.

## Yayınlanmış sürümler değiştirilmez

Tag'ler yeniden atılmaz, Release dosyaları değiştirilmez. Hatalı bir sürüm yeni bir PATCH ile düzeltilir.
