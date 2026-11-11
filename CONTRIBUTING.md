# Katkı rehberi

## Geliştirme ortamı

- Windows 10 2004+ / Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (sürüm `global.json` ile sabitlenmiştir)

```powershell
dotnet restore ScreenCut.slnx
dotnet build ScreenCut.slnx
dotnet test ScreenCut.slnx
dotnet run --project src/ScreenCut
```

> Debug derlemeleri kendini Windows açılışına **kaydetmez**; bu yalnızca Release derlemelerinde yapılır.

## Dal stratejisi

`main` korumalıdır ve her zaman yayınlanabilir durumdadır. Doğrudan `main`'e commit atılmaz.

| Dal         | Amaç                          |
|-------------|-------------------------------|
| `feature/…` | Yeni özellik                  |
| `fix/…`     | Hata düzeltme                 |
| `docs/…`    | Yalnızca dokümantasyon        |
| `refactor/…`| Davranışı değiştirmeyen düzen |
| `chore/…`   | Bakım işleri                  |

Dal adları küçük harf ve tire ile yazılır: `feature/scrolling-capture`.

## Commit ve PR başlıkları

[Conventional Commits](https://www.conventionalcommits.org/tr/v1.0.0/) zorunludur. PR'lar **squash merge**
edildiği için **PR başlığı** `main`'deki commit mesajı olur ve sürüm numarasını belirler.

```
feat(overlay): seçim alanına büyüteç ekle
fix(capture): 150% ölçeklemede kayan seçim düzeltildi
docs(readme): kısayol tablosu güncellendi
```

Kapsam (scope) önerileri: `capture`, `overlay`, `annotations`, `output`, `share`, `tray`, `settings`, `ci`, `deps`.

Geriye dönük uyumsuz değişiklikler `feat!:` ile ya da `BREAKING CHANGE:` footer'ı ve geçiş notuyla belirtilir.

## PR kontrol listesi

- CI yeşil (format, derleme, test, bağımlılık güvenlik taraması)
- Yeni davranış için test var
- Kullanıcıya görünen değişiklik README'ye yansıtıldı
- Mümkünse gerçek ekranda denendi (tek/çok monitör, farklı ölçekleme)

## Kod kuralları

- `.editorconfig` kuralları derlemede uygulanır; uyarılar hata sayılır.
- `dotnet format ScreenCut.slnx` ile biçimlendirin.
- Saf mantık (`Core/`) WPF penceresinden bağımsız tutulur ve birim testlerle kapsanır.
- Bağımlılık sürümleri `Directory.Packages.props` içinde sabitlenir; `packages.lock.json` dosyaları commit edilir.

Sürüm yayınlama süreci için: [docs/RELEASING.md](docs/RELEASING.md).
