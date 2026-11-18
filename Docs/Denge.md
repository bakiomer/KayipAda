# Kayıp Ada — Denge Tablosu (ilk kaçış)

Hedef: **ilk kaçış 30-45 dakika** (sadece ana yol), **yan görevlerle 45-60 dakika**; hiçbir yerde **3 dakikadan uzun
boş bekleme yok**.
Oyun saf bir üretim/ekonomi oyunu: savunma yok, final anı geminin suya indirilme töreni.
Tüm sayılar ayar dosyalarındadır: `Data/Areas`, `Data/Upgrades`, `Data/Ship.asset`, `Data/ResourceTypes`,
`Data/Buildings`, `Data/NodeTypes`, `Data/Workers`, `Data/Quests`, `Data/Costumes`; genel sayılar `GameConfig`.
Hızlı Test Modu (Tools > Kayıp Ada > Hızlı Test Modu) fiyatları %8'e, gemi malzemesini %20'ye indirir.

## Üretim zincirleri

| Ham madde | Nereden | Ürün | Bina (tarif, süre) | Satış |
|---|---|---|---|---|
| Odun | Sahil palmiyeleri (3 × 8 stok, 10 sn'de dolar) | Kalas | Bıçkıhane (2 odun → 1, 2 sn) | odun 5, kalas 14 |
| Taş | Kayalık (3 × 6 stok, 15 sn) | Tuğla | Taş Atölyesi (2 taş → 1, 3 sn) | taş 8, tuğla 22 |
| Lif | Palmiye Koyu (3 × 8 stok, 10 sn) | Halat | Halat Atölyesi (3 lif → 1, 3 sn) | lif 3, halat 14 |
| Lif | Palmiye Koyu | Yelken Bezi | Dokuma Tezgahı (4 lif → 1, 4 sn) | bez 20 |
| Meyve | Meyve Bahçesi (3 × 5 stok, 12 sn) | — | — | meyve 4 |
| Hurda Metal | Batık Koyu batık parçaları (3 × 6 stok, 14 sn) | Çivi | Demirci (2 hurda → 1, 3 sn) | hurda 6, çivi 18 |

İşçiler (Tayfa Barakası'ndan tutulur, her türden en fazla 2-3): **Oduncu** (odun → Bıçkıhane), **Taşıyıcı** (tüm bina
ürünleri → gemi ya da Pazar), **Lifçi** (lif → Halat Atölyesi), **Demirci Çırağı** (hurda → Demirci). Taşıyıcı, oyuncu gemiye ilk malzemeyi getirene kadar Pazar'da satar; sonra
geminin istediği ürünü (kalas, tuğla, halat, yelken bezi) gemiye, kalanı Pazar'a götürür.

## Gemi: 5 aşama (her biri bir sonraki hedefi açar)

| # | Aşama | Malzeme | Bitince açılan |
|---|---|---|---|
| 1 | Omurga | 40 kalas | Palmiye Koyu karesi (500) |
| 2 | Gövde | 40 kalas + 30 tuğla + **8 çivi** | Halat Atölyesi karesi (700) |
| 3 | Direk | 30 kalas + 20 halat + **10 çivi** | Dokuma Tezgahı karesi (900) |
| 4 | Yelken | 20 yelken bezi + 15 halat | Meyve Bahçesi karesi (600) |
| 5 | Erzak | 40 meyve | Suya indirme töreni → kaçış |

Toplam: 110 kalas (220 odun), 30 tuğla (60 taş), 35 halat + 20 bez (185 lif), 18 çivi (36 hurda), 40 meyve.
Çivi için önce **Batık Koyu köprüsü** (40 kalas) ve **Demirci** (600) gerekir; rehber oku Gövde aşamasında köprüyü gösterir.

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
| 3 | Oduncu (Tayfa Barakası) | 400 | ~1:45 | 4:00 |
| 4 | Taşıyıcı (Tayfa Barakası) | 600 | ~2:00 | 6:00 |
| 5 | Taş Atölyesi | 800 | ~2:00 | 8:00 |
| 6 | **Gemi 1: Omurga** (40 kalas; araya ilk geliştirmeler girer) | 80 odun | ~4:00 | 12:00 |
| 7 | Palmiye Koyu | 500 | ~1:00 | 13:00 |
| 8 | Batık Koyu köprüsü | 40 kalas | ~2:30 | 15:30 |
| 9 | Demirci (+ isteğe bağlı Demirci Çırağı 700) | 600 | ~1:30 | 17:00 |
| 10 | **Gemi 2: Gövde** (40 kalas + 30 tuğla + 8 çivi) | — | ~4:30 | 21:30 |
| 11 | Halat Atölyesi | 700 | ~1:30 | 23:00 |
| 12 | Lifçi (Tayfa Barakası) | 500 | ~1:00 | 24:00 |
| 13 | **Gemi 3: Direk** (30 kalas + 20 halat + 10 çivi) | — | ~4:00 | 28:00 |
| 14 | Dokuma Tezgahı | 900 | ~2:00 | 30:00 |
| 15 | **Gemi 4: Yelken** (20 bez = 80 lif + 15 halat = 45 lif) | — | ~5:00 | 35:00 |
| 16 | Meyve Bahçesi | 600 | ~1:00 | 36:00 |
| 17 | **Gemi 5: Erzak** (40 meyve) | — | ~2:00 | 38:00 |
| 18 | Suya indirme töreni | — | ~0:15 | **~38 dk** |

Geliştirmeler (~5.000 para, isteğe bağlı) adım 6-12 arasında para birikirken alınır ve zincirleri hızlandırır.

**Sonuç:** İlk kaçış tahmini (sadece ana yol) **~38 dakika** (hedef 30-45). Çok hızlı bir oyuncu ~30, yavaş bir oyuncu
~45 dakikada bitirir.

## Keşif ve yan içerik (isteğe bağlı: +10-20 dk → 45-60 dk)

| İçerik | Ne ister | Ödül | Tahmini ek süre |
|---|---|---|---|
| 15 yan görev (en fazla 2 tanesi aynı anda) | topla / sat / üret / onar / sandık / keşif / figür | toplam ~1.000 para + ~34 kristal | ana yola gömülü, +6-10 dk |
| Kuyu, Çit, Sandal onarımı | 8 kalas + 6 taş · 6 kalas · 10 kalas + 4 halat | görev ödülü | +2 dk |
| Kayıp Tapınak köprüsü | 30 kalas + 12 halat | adacık, 2 sandık, altın kafatası, altın figür | +3 dk |
| 8 gizli sandık | yürüyerek bulmak | toplam 660 para + 26 kristal (+3 nadir kostüm) | +2-3 dk |
| 5 koleksiyon eşyası | yürüyerek bulmak | 5/5 → +15 kristal (bir kez) | +1-2 dk |
| 3 altın figür parçası | tapınakta | gemide altın figür, "EFSANEVİ GEMİ!", kaçışta +20 kristal | +1 dk |
| Bina seviyeleri (5 bina × 2) | 350-1.700 para ("Yükselt" tabelası) | hız ×1,35 / ×1,75, kapasite +6 / +12, bina büyür | para birikirken |
| Ambar (Kayalık'tan sonra) | 450 para (+ seviye 700 / 1.300) | 100 / 160 / 240 birim saklama; Taşıyıcı ambardan gemiye/binalara taşır | 0 (zaman kazandırır) |

Ana yolu ve keşfi birlikte yapan oyuncu için tahmin **~50 dakika** (hedef 45-60).

## Kristal (kalıcı ikinci para)
- Sadece sandık, görev, koleksiyon ve kaçıştan gelir (kaçış: 10, efsanevi gemiyle +20). "Tekrar oyna" ile sıfırlanmaz.
- Kostümler: 7 karakter + 4 yelken/bayrak; fiyatlar 30-120 kristal; İskelet Korsan, Altın Kaptan ve Altın Yelken sadece
  sandıktan çıkar. İlk adada ~60-80 kristal toplanır → 1-2 kostüm. Bu turda mağaza ekranı yok (Test Menüsü'nden giyilir).

## Ambar ve ilk kaçış süresi
- Ambar isteğe bağlıdır: rehber oku göstermez, ana yolun fiyatları ve miktarları değişmedi. Bu yüzden ilk kaçış tahmini
  (sadece ana yol) yine **~38 dakika** (hedef 30-45).
- Ambarı alan oyuncu fazla kaynağı satmadan saklar; Taşıyıcı işçi ambardaki gemi malzemesini teslim alanına kendisi götürür.
  Bu, Gövde/Direk aşamalarında oyuncunun yürüme süresini ~1-2 dk kısaltabilir (450 para ~1 dk'lık kazanç), yani ilk kaçış
  en hızlı ~35 dakikaya iner; 30 dakikanın altına düşmez.
- Ambar doluyken bırakma durur ("Ambar dolu!"); kaçışta ambar boşalır.

## Bina kimliği ve doğal başlangıç turu (kontrol)
- Fiyatlar, tarifler, üretim süreleri ve gemi miktarları değişmedi; makineler sadece görünüş (animasyon üretim hızına
  senkron, üretimi yavaşlatmaz). Kayıt/kaçış akışı aynı.
- Kamp adanın merkezine taşındı: Geliştirme Tezgahı ve Tayfa Barakası başlangıca 3-5 m (eskiden ~12 m) → ilk dakikalarda
  ~20-30 sn kazanç. Ambar burna taşındı (isteğe bağlı, rehber göstermez).
- Bölge açılma efekti ~2,6 sn sürer (oyuncu yürümeye devam edebilir); 8 açılışta toplam ~20 sn.
- Sonuç: ilk kaçış tahmini hâlâ **~38 dakika** (hızlı oyuncu ~30, yavaş ~45) → hedef 30-45 dk içinde.

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
| İşçi tutma | Tayfa Barakası: Oduncu 400/950/1.700, Taşıyıcı 600/1.300/2.200, Lifçi 500/1.100/1.900, Demirci Çırağı 700/1.400 | İlk fiyatlar eski karelerle aynı; Lifçi artık Halat Atölyesi açılınca |
| Gövde / Direk | +8 / +10 çivi | Batık Koyu ve Demirci ana yola girdi |
| Köprüler | Batık Koyu 40 kalas; Kayıp Tapınak 30 kalas + 12 halat | Köprü bitince adacık bulutu dağılır |
| Bina seviyesi | `GameConfig` > Bina seviyeleri; fiyatlar `Data/Areas/*Lv2, *Lv3` | Kare yerine binanın yanında "Yükselt" tabelası + alt panel |
| Ambar | `Data/Areas/Storage` 450, `StorageLv2/Lv3` 700/1.300; kapasite `GameConfig.StorageCapacity` 100/160/240 | İsteğe bağlı (rehber göstermez) |
| Son gece, toplar | **kaldırıldı** | Oyun saf üretim oyunu |

> Bu tablo tahmindir; asıl doğrulama telefonda gerçek oyuncularla yapılmalı (Plan Adım 5).
