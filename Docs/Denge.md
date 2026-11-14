# Kayıp Ada — Denge Tablosu (ilk kaçış)

Hedef: Gemi dahil **ilk kaçış 30-45 dakika**, hiçbir yerde **3 dakikadan uzun boş bekleme yok**.
Tüm sayılar ayar dosyalarındadır (`Data/Areas`, `Data/Upgrades`, `Data/Ship.asset`, `Data/ResourceTypes`, `Data/Buildings`).
Hızlı Test Modu (Tools > Kayıp Ada > Hızlı Test Modu) fiyatları %8'e, malzeme ve düşman sayısını %20'ye indirir.

## Temel hızlar (varsayımlar)

| Kaynak | Değer | Not |
|---|---|---|
| Odun | 5 para, 0,35 sn'de 1 | 3 palmiye × 8 stok, 10 sn'de dolar → en fazla ~2,4 odun/sn |
| Taş | 8 para, 0,6 sn'de 1 | 3 kaya × 6 stok, 15 sn'de dolar |
| Meyve | 4 para, 0,4 sn'de 1 | 3 ağaç × 5 stok, 12 sn'de dolar |
| Kalas | 14 para (2 odun → 1, 2 sn) | Bıçkıhane |
| Tuğla | 22 para (2 taş → 1, 3 sn) | Taş Atölyesi |
| Oyuncu | 6 m/sn, 10 kapasite | Bir tur (topla + yürü + sat) ~9 sn |

**Etkin gelir tahmini** (yeni oyuncunun gezinmesi ve kararsızlığı için %50 pay bırakıldı):

| Dönem | Gelir kaynağı | Etkin gelir |
|---|---|---|
| Başlangıç | Elle odun | ~3 para/sn |
| Bıçkıhane + Oduncu | Elle odun/kalas + Oduncu | ~4-5 para/sn |
| + Taşıyıcı | Kalaslar kendiliğinden satılır | ~6 para/sn |
| + Taş Atölyesi, geliştirmeler | Kalas + tuğla | ~8-10 para/sn |

## Açılma sırası ve süre tahmini

| # | Hedef | Fiyat / gereken | Tahmini süre | Toplam (dk) |
|---|---|---|---|---|
| 1 | Kayalık | 150 | ~0:50 | 0:50 |
| 2 | Bıçkıhane | 250 | ~1:25 | 2:15 |
| 3 | Oduncu (işçi) | 400 | ~1:45 | 4:00 |
| 4 | Taşıyıcı (işçi) | 600 | ~2:00 | 6:00 |
| 5 | Taş Atölyesi | 800 | ~2:00 | 8:00 |
| 6 | Meyve Bahçesi | 600 | ~1:30 | 9:30 |
| 7 | Geliştirmeler (oyuncunun seçtiği ~5.000 para) | 120-1.900 / seviye | ~10:00 (araya gemi işleri girer) | 19:30 |
| 8 | Gemi: Omurga | 40 kalas (80 odun) | ~3:00 | 22:30 |
| 9 | Toplar (3 adet, omurgadan sonra) | 400 + 550 + 700 | ~3:00 (gemiyle paralel) | — |
| 10 | Gemi: Gövde | 50 kalas + 40 tuğla | ~4:30 | 27:00 |
| 11 | Gemi: Direk ve Yelken | 40 kalas + 60 odun | ~3:30 | 30:30 |
| 12 | Gemi: Erzak | 45 meyve | ~2:00 | 32:30 |
| 13 | Son gece (3 dalga: 5 + 8 + 11 iskelet) | — | ~2:00 | **~34:30** |

**Sonuç:** İlk kaçış tahmini **~33-36 dakika** (hedef aralıkta). Çok hızlı bir oyuncu ~25 dakikada, yavaş bir oyuncu ~45 dakikada bitirir.

## Boş bekleme kontrolü
- Her adımda en az bir "yapılacak iş" var: para yetmezken bile toplanacak kaynak, taşınacak malzeme ya da satılacak ürün var.
- En uzun para bekleme: Taşıyıcı (600) ve Taş Atölyesi (800) öncesi ~2 dakika. Bu sürede oyuncu kendi eliyle odun/kalas satıyor, yani boş beklemiyor.
- Ağaçlar 10 sn'de, kayalar 15 sn'de doluyor: bir kaynak bitince diğerine yürümek bekleme süresinden kısa.
- Gemi aşamalarında Taşıyıcı işçiler kalas/tuğlayı **gemiye** taşıyor; oyuncu bu sırada odun ve meyve getiriyor. Kimse 3 dakika boş durmuyor.
- Son gecede toplar taşla doluyor (oyuncu kayalıktan taş taşır), yani gece de aktif.

## Değiştirilen sayılar (eski → yeni)
| Ayar | Eski | Yeni | Neden |
|---|---|---|---|
| Kayalık | 60 | 150 | Başlangıç çok hızlıydı (10 sn) |
| Bıçkıhane | 80 | 250 | |
| Oduncu | 120 | 400 | İşçiler pasif gelir sağlıyor; biraz emek istesin |
| Taşıyıcı | 180 | 600 | |
| Taş Atölyesi | 200 | 800 | Tuğla en değerli ürün |
| Meyve Bahçesi | 150 | 600 | Meyve artık gemi erzakı için gerekli |
| Geliştirme fiyatları | — | 120-1.900 | 5 seviye, her biri ~2× |
| Gemi malzemesi | — | 130 kalas, 40 tuğla, 60 odun, 45 meyve | Gemi ~13 dakikalık iş |

> Bu tablo tahmindir; asıl doğrulama telefonda gerçek oyuncularla yapılmalı (Plan Adım 5).
