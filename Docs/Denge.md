# Kayıp Ada — Denge Tablosu (ilk kaçış)

Hedef (Kullanılabilirlik turu): **ilk kaçış 25-30 dakika** (sadece ana yol), **yan içerikle ~40 dakika**; her **3-5
dakikada bir küçük başarı anı** (yeni bölge/bina, işçi, gemi aşaması, köprü), hiçbir yerde 3 dakikadan uzun boş bekleme yok.
Kısaltma sadece "daha az odun" ile değil: bölge/bina fiyatları ~%20-25, işçi fiyatları ~%20-25, gemi malzemeleri ve
köprüler ~%25 azaltıldı (tüm zincir orantılı kısaldı).
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
| 1 | Omurga | 30 kalas | Palmiye Koyu karesi (400) |
| 2 | Gövde | 30 kalas + 20 tuğla + **6 çivi** | Halat Atölyesi karesi (550) |
| 3 | Direk | 24 kalas + 15 halat + **8 çivi** | Dokuma Tezgahı karesi (700) |
| 4 | Yelken | 14 yelken bezi + 12 halat | Meyve Bahçesi karesi (450) |
| 5 | Erzak | 28 meyve | Suya indirme töreni → kaçış |

Toplam: 84 kalas (168 odun), 20 tuğla (40 taş), 27 halat + 14 bez (137 lif), 14 çivi (28 hurda), 28 meyve
(önceki tur: 110 kalas, 30 tuğla, 35 halat + 20 bez, 18 çivi, 40 meyve).
Çivi için önce **Batık Koyu köprüsü** (30 kalas) ve **Demirci** (450) gerekir; rehber oku Gövde aşamasında köprüyü gösterir.
Fiyatlar: Kayalık 120, Bıçkıhane 200, Taş Atölyesi 600, Palmiye Koyu 400, Halat Atölyesi 550, Dokuma 700, Meyve Bahçesi 450,
Demirci 450. İşçiler (1./2./3.): Oduncu 300/750/1.400, Taşıyıcı 450/1.050/1.800, Lifçi 400/900/1.600, Demirci Çırağı 550/1.150.
Tapınak köprüsü 24 kalas + 10 halat.

## Etkin gelir tahmini
Yeni oyuncunun gezinmesi ve kararsızlığı için %50 pay bırakıldı.

| Dönem | Gelir kaynağı | Etkin gelir |
|---|---|---|
| Başlangıç | Elle odun | ~3 para/sn |
| Bıçkıhane + Oduncu | Elle odun/kalas + Oduncu | ~4-5 para/sn |
| + Taşıyıcı | Kalaslar kendiliğinden satılır | ~6 para/sn |
| + Taş Atölyesi, geliştirmeler | Kalas + tuğla | ~8-10 para/sn |

## Açılma sırası — dakika dakika (ana yol)

| # | Dakika | Hedef | Fiyat / gereken | Süre | Başarı anı |
|---|---|---|---|---|---|
| 1 | 0:00-0:40 | Kayalık | 120 | ~0:40 | bölge açılır (orta kutlama) |
| 2 | 0:40-1:50 | Bıçkıhane | 200 | ~1:10 | bina kurulur |
| 3 | 1:50-3:10 | Oduncu (Tayfa Barakası) | 300 | ~1:20 | ilk işçi kapıdan çıkar |
| 4 | 3:10-4:40 | Taşıyıcı | 450 | ~1:30 | kalaslar kendiliğinden satılır |
| 5 | 4:40-6:10 | Taş Atölyesi | 600 | ~1:30 | bina kurulur |
| 6 | 6:10-9:10 | **Gemi 1: Omurga** (30 kalas; araya ilk geliştirmeler) | 60 odun | ~3:00 | **büyük kutlama** |
| 7 | 9:10-10:00 | Palmiye Koyu | 400 | ~0:50 | bölge açılır |
| 8 | 10:00-12:00 | Batık Koyu köprüsü | 30 kalas | ~2:00 | **adacık açılışı** |
| 9 | 12:00-13:00 | Demirci | 450 | ~1:00 | bina kurulur |
| 10 | 13:00-16:30 | **Gemi 2: Gövde** (30 kalas + 20 tuğla + 6 çivi) | — | ~3:30 | **büyük kutlama**, kamp kulübe olur |
| 11 | 16:30-17:40 | Halat Atölyesi | 550 | ~1:10 | bina kurulur |
| 12 | 17:40-18:20 | Lifçi | 400 | ~0:40 | işçi |
| 13 | 18:20-21:20 | **Gemi 3: Direk** (24 kalas + 15 halat + 8 çivi) | — | ~3:00 | **büyük kutlama** |
| 14 | 21:20-22:50 | Dokuma Tezgahı | 700 | ~1:30 | bina kurulur |
| 15 | 22:50-26:20 | **Gemi 4: Yelken** (14 bez = 56 lif + 12 halat = 36 lif) | — | ~3:30 | **büyük kutlama**, kamp ev olur |
| 16 | 26:20-27:00 | Meyve Bahçesi | 450 | ~0:40 | bölge açılır |
| 17 | 27:00-28:30 | **Gemi 5: Erzak** (28 meyve) | — | ~1:30 | — |
| 18 | 28:30-28:45 | Suya indirme töreni | — | ~0:15 | **final (zafer müziği)** |

Başarı anları arasında en uzun ara ~3:30 (gemi aşamaları); aralara yan görev kartları ve bina seviyeleri girer.
Geliştirmeler (~5.000 para, isteğe bağlı) adım 6-12 arasında alınır ve zincirleri hızlandırır.

**Sonuç:** İlk kaçış tahmini (sadece ana yol) **~28-29 dakika** (hedef 25-30; önceki tur ~38). Çok hızlı oyuncu ~24,
yavaş oyuncu ~33 dakika.

## Keşif ve yan içerik (isteğe bağlı: +10-12 dk → toplam ~40 dk)

| İçerik | Ne ister | Ödül | Tahmini ek süre |
|---|---|---|---|
| 15 yan görev (en fazla 2 tanesi aynı anda) | topla / sat / üret / onar / sandık / keşif / figür | toplam ~1.000 para + ~34 kristal | ana yola gömülü, +6-10 dk |
| Kuyu, Çit, Sandal onarımı | 8 kalas + 6 taş · 6 kalas · 10 kalas + 4 halat | görev ödülü | +2 dk |
| Kayıp Tapınak köprüsü + bulmaca | 24 kalas + 10 halat; 3 taş levhaya bas | adacık, bulmaca sandığı (altın kafatası + kristal), 1 sandık, altın figür | +3 dk |
| 8 gizli sandık | yürüyerek bulmak | toplam 660 para + 26 kristal (+3 nadir kostüm) | +2-3 dk |
| 5 koleksiyon eşyası | yürüyerek bulmak | 5/5 → +15 kristal (bir kez) | +1-2 dk |
| 3 altın figür parçası | tapınakta | gemide altın figür, "EFSANEVİ GEMİ!", kaçışta +20 kristal | +1 dk |
| Bina seviyeleri (5 bina × 2) | 350-1.700 para ("Yükselt" tabelası) | hız ×1,35 / ×1,75, kapasite +6 / +12, bina büyür | para birikirken |
| Ambar (Kayalık'tan sonra) | 450 para (+ seviye 700 / 1.300) | 100 / 160 / 240 birim saklama; Taşıyıcı ambardan gemiye/binalara taşır | 0 (zaman kazandırır) |

Ana yolu ve keşfi birlikte yapan oyuncu için tahmin **~40 dakika** (hedef ~40).

## Kristal (kalıcı ikinci para)
- Sadece sandık, görev, koleksiyon ve kaçıştan gelir (kaçış: 10, efsanevi gemiyle +20). "Tekrar oyna" ile sıfırlanmaz.
- Kostümler: 7 karakter + 4 yelken/bayrak; fiyatlar 30-120 kristal; İskelet Korsan, Altın Kaptan ve Altın Yelken sadece
  sandıktan çıkar. İlk adada ~60-80 kristal toplanır → 1-2 kostüm. Bu turda mağaza ekranı yok (Test Menüsü'nden giyilir).

## Ambar ve ilk kaçış süresi
- Ambar isteğe bağlıdır: rehber oku göstermez, ana yolun fiyatları ve miktarları değişmedi. Bu yüzden ilk kaçış tahmini
  (sadece ana yol) yine **~28-29 dakika** (hedef 25-30).
- Ambarı alan oyuncu fazla kaynağı satmadan saklar; Taşıyıcı işçi ambardaki gemi malzemesini teslim alanına kendisi götürür.
  Bu, Gövde/Direk aşamalarında oyuncunun yürüme süresini ~1 dk kısaltabilir; ilk kaçış en hızlı ~26-27 dakikaya iner.
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
