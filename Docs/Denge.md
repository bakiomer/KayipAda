# Kayıp Ada — Denge Tablosu (ilk kaçış)

Hedef: **ilk kaçış 30-45 dakika**, hiçbir yerde **3 dakikadan uzun boş bekleme yok**.
Oyun saf bir üretim/ekonomi oyunu: savunma yok, final anı geminin suya indirilme töreni.
Tüm sayılar ayar dosyalarındadır: `Data/Areas`, `Data/Upgrades`, `Data/Ship.asset`, `Data/ResourceTypes`,
`Data/Buildings`, `Data/NodeTypes`, `Data/Workers`.
Hızlı Test Modu (Tools > Kayıp Ada > Hızlı Test Modu) fiyatları %8'e, gemi malzemesini %20'ye indirir.

## Üretim zincirleri

| Ham madde | Nereden | Ürün | Bina (tarif, süre) | Satış |
|---|---|---|---|---|
| Odun | Sahil palmiyeleri (3 × 8 stok, 10 sn'de dolar) | Kalas | Bıçkıhane (2 odun → 1, 2 sn) | odun 5, kalas 14 |
| Taş | Kayalık (3 × 6 stok, 15 sn) | Tuğla | Taş Atölyesi (2 taş → 1, 3 sn) | taş 8, tuğla 22 |
| Lif | Palmiye Koyu (3 × 8 stok, 10 sn) | Halat | Halat Atölyesi (3 lif → 1, 3 sn) | lif 3, halat 14 |
| Lif | Palmiye Koyu | Yelken Bezi | Dokuma Tezgahı (4 lif → 1, 4 sn) | bez 20 |
| Meyve | Meyve Bahçesi (3 × 5 stok, 12 sn) | — | — | meyve 4 |

İşçiler: **Oduncu** (odun → Bıçkıhane), **Taşıyıcı** (tüm bina ürünleri → gemi ya da Pazar),
**Lifçi** (lif → Halat Atölyesi). Taşıyıcı, oyuncu gemiye ilk malzemeyi getirene kadar Pazar'da satar; sonra
geminin istediği ürünü (kalas, tuğla, halat, yelken bezi) gemiye, kalanı Pazar'a götürür.

## Gemi: 5 aşama (her biri bir sonraki hedefi açar)

| # | Aşama | Malzeme | Bitince açılan |
|---|---|---|---|
| 1 | Omurga | 40 kalas | Palmiye Koyu karesi (500) |
| 2 | Gövde | 40 kalas + 30 tuğla | Halat Atölyesi karesi (700) |
| 3 | Direk | 30 kalas + 20 halat | Dokuma Tezgahı (900) ve Lifçi işçi (500) kareleri |
| 4 | Yelken | 20 yelken bezi + 15 halat | Meyve Bahçesi karesi (600) |
| 5 | Erzak | 40 meyve | Suya indirme töreni → kaçış |

Toplam: 110 kalas (220 odun), 30 tuğla (60 taş), 35 halat + 20 bez (185 lif), 40 meyve.

## Etkin gelir tahmini
Yeni oyuncunun gezinmesi ve kararsızlığı için %50 pay bırakıldı.

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
| 3 | Oduncu | 400 | ~1:45 | 4:00 |
| 4 | Taşıyıcı | 600 | ~2:00 | 6:00 |
| 5 | Taş Atölyesi | 800 | ~2:00 | 8:00 |
| 6 | **Gemi 1: Omurga** (40 kalas; araya ilk geliştirmeler girer) | 80 odun | ~4:00 | 12:00 |
| 7 | Palmiye Koyu | 500 | ~1:00 | 13:00 |
| 8 | **Gemi 2: Gövde** (40 kalas + 30 tuğla) | — | ~4:00 | 17:00 |
| 9 | Halat Atölyesi | 700 | ~1:30 | 18:30 |
| 10 | **Gemi 3: Direk** (30 kalas + 20 halat = 60 lif) | — | ~3:30 | 22:00 |
| 11 | Dokuma Tezgahı + Lifçi | 900 + 500 | ~2:30 | 24:30 |
| 12 | **Gemi 4: Yelken** (20 bez = 80 lif + 15 halat = 45 lif) | — | ~5:00 | 29:30 |
| 13 | Meyve Bahçesi | 600 | ~1:00 | 30:30 |
| 14 | **Gemi 5: Erzak** (40 meyve) | — | ~2:00 | 32:30 |
| 15 | Suya indirme töreni | — | ~0:15 | **~33 dk** |

Geliştirmeler (~5.000 para, isteğe bağlı) adım 6-12 arasında para birikirken alınır ve zincirleri hızlandırır.

**Sonuç:** İlk kaçış tahmini **~33 dakika** (hedef 30-45). Çok hızlı bir oyuncu ~25, yavaş bir oyuncu ~45 dakikada bitirir.

## Boş bekleme kontrolü
- Her an en az bir iş var: toplanacak kaynak, gemiye taşınacak malzeme ya da satılacak ürün.
- En uzun para bekleme: Taş Atölyesi (800) ve Dokuma Tezgahı (900) öncesi ~2-2,5 dakika. Bu sürede oyuncu elle
  lif/odun toplayıp satıyor ya da gemiye malzeme taşıyor; boş durmuyor.
- Kaynaklar 10-15 sn'de doluyor: biri bitince diğerine yürümek bekleme süresinden kısa.
- En uzun "üretim bekleme": Yelken aşaması (125 lif). Lifçi işçi + oyuncu birlikte ~1,2 lif/sn toplar; bina süreleri
  (3-4 sn) lif toplama hızından kısa olduğu için binalar darboğaz değil. Kimse 3 dakika boş durmuyor.
- Aşama bitince açılan yeni kare fiyatları (500-900), o ana kadar biriken parayla 1-2,5 dakikada alınabiliyor.

## Değiştirilen / eklenen sayılar
| Ayar | Değer | Not |
|---|---|---|
| Lif, Halat, Yelken Bezi | 3 / 14 / 20 para | Yeni kaynaklar |
| Lif palmiyesi | 8 stok, 0,35 sn, 10 sn'de dolar | Sahil palmiyesiyle aynı |
| Halat Atölyesi, Dokuma Tezgahı | 3 lif → 1 (3 sn), 4 lif → 1 (4 sn) | Yeni binalar |
| Palmiye Koyu / Halat / Dokuma / Lifçi | 500 / 700 / 900 / 500 | Gemi aşamalarıyla açılır |
| Meyve Bahçesi | 600 (artık Yelken aşamasından sonra) | Meyve sadece erzak için |
| Gemi | 5 aşama (yukarıdaki tablo) | Eskiden 4 aşama + son gece |
| Son gece, toplar | **kaldırıldı** | Oyun saf üretim oyunu |

> Bu tablo tahmindir; asıl doğrulama telefonda gerçek oyuncularla yapılmalı (Plan Adım 5).
