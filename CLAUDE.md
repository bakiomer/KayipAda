# Kayıp Ada — Oyun Tasarımı ve Kod Kuralları

> **Yön: 3D low-poly** (2D kutulardan geçildi; 2D son hali `main` dalında "2D son durum" kaydında).
> **Şu anki durum:** Düzeltme + Keşif + Oyuncu testi düzeltme turu (Düzeltme 3) yapıldı:
> - Düzeltme 3: bulut yerine kapalı bölgelerde çalılık (açılınca kesilir); bölge kimlikleri (taş ocağı, kemerli bahçe
>   kapısı + kuyu, kurutma rafları); Halat Atölyesi tarlanın kuzeyine, Dokuma Tezgahı güneyine; seviye kareleri yerine
>   "Yükselt" tabelası + alt panel; Batık Koyu Bıçkıhane'nin güneyinden dar uzun köprüyle (karşı kıyı önizlemesi, açılış
>   çekimi), Kayıp Tapınak batıda; tanıtım tabelası kartları; koleksiyon eşyası alınınca kaybolur; ağaç kütük → fidan →
>   genç → tam büyür, kaya yerden yükselir; iki raylı kızak + beşik; final geniş açı + sahil boyunca dağınık kalabalık;
>   denize uzanan uzun iskele (sandal, dürbün ucunda); kıyıya vuran dalga + ıslak kum + köpük + parıltı; Ambar (depo);
>   okunaklı/küçülen görev kartları; onarım göstergeleri ikon + sayı + çubuk.
> - Düzeltme: gemi+kızak tek, yuvarlak köşeli engel (takılma yok); yatay iskele; Tayfa Barakası (işçi kartları); procedural
>   halat çarkı / dokuma tezgahı (sadece üretirken döner); açık Liman Pazarı; ayak izi/yaprak/su halkası; toplanma kadrajı.
> - Keşif: bulutlu bölgeler, iki adacık (köprüyle), Demirci (hurda → çivi), 8 sandık, 5 koleksiyon, 15 yan görev,
>   bina seviyeleri (3), altın figür, kristal + 11 kostüm.
> Testler `Docs/Screenshots` (keşif `Docs/Screenshots/Kesif`, Düzeltme 3 önce/sonra `Docs/Screenshots/Duzeltme3`). İş `3d` dalında.

## Proje
- **Motor:** Unity 6 (6000.6), **URP 3D** (Universal Renderer, mobil ayarlı `Assets/_Game/Settings/URP_Mobile`)
- **Platform:** Android, **dikey (portrait)** ekran, referans çözünürlük 1080x1920, hedef **60 FPS**
- **Tür:** Idle arcade (kaynak topla → taşı → sat → geliştir). Hedef: popüler idle arcade mobil oyunlar kadar
  güzel görünen, akıcı, efektli ve sesli bir oyun.
- **Görünüm:** 3D, yukarıdan ~45° eğik perspektif kamera (FOV ~32, dar açı; oyuncu ekranın alt-ortasında), oyun **XZ düzleminde** (y = yukarı).
- **Girdi:** Yalnızca **yeni Input System** etkin (`activeInputHandler: 1`). Eski `Input.GetAxis` vb. KULLANILMAZ.
  Dokunmatik ve fare, uGUI sanal joystick ile (EventSystem + `InputSystemUIInputModule`) çalışır;
  editörde test için WASD / ok tuşları da çalışır. Joystick yukarısı = kameranın baktığı yön (+Z).
- **Kullanıcı:** Unity'ye yeni başlıyor. Açıklamalar Türkçe, adım adım ve hangi menüye tıklanacağını söyleyerek yapılır.

## Oyun Tasarımı
Oyuncu bir adaya düşmüş bir kazazede kaptandır. Amaç: adadan kaçmak için gemi inşa etmek.
**Saf bir üretim/ekonomi oyunu: savunma, düşman, savaş YOK.** Final anı geminin suya indirilme töreni.

### Temel döngü
1. Oyuncu kaynak noktasına (palmiye, kaya, meyve ağacı) yaklaşır → kaynak kavis çizerek önündeki yığına uçar.
2. Yığının kapasitesi sınırlıdır.
3. Pazara girince yığın tek tek tezgaha uçar ve satılır → para zıplar, "+5" yazısı çıkar.
4. Para ile yeni bölgeler, binalar, işçiler (Tayfa Barakası) ve geliştirmeler açılır.
5. Son hedef: üretim zincirleriyle gemiyi 5 aşamada inşa etmek; gemi bitince suya indirme töreniyle adadan kaçmak.
6. İsteğe bağlı keşif: çalılıklı bölgeler, köprüyle adacıklar, gizli sandıklar, koleksiyon, yan görevler, altın figür, Ambar.

### Üretim zincirleri
| Ham madde (nereden) | Bina | Ürün |
|---|---|---|
| Odun (sahil palmiyeleri) | Bıçkıhane (2 → 1) | Kalas |
| Taş (Kayalık) | Taş Atölyesi (2 → 1) | Tuğla |
| Lif (Palmiye Koyu keten tarlası) | Halat Atölyesi (3 → 1) | Halat |
| Lif | Dokuma Tezgahı (4 → 1) | Yelken Bezi |
| Meyve (Meyve Bahçesi çalıları) | — | (erzak) |
| Hurda Metal (Batık Koyu batık parçaları) | Demirci (2 → 1) | Çivi |

### Gemi (5 aşama, her biri bir sonraki hedefi açar)
| Aşama | Malzeme | Bitince açılan kare |
|---|---|---|
| Omurga | kalas | Palmiye Koyu |
| Gövde | kalas + tuğla + çivi | Halat Atölyesi |
| Direk | kalas + halat + çivi | Dokuma Tezgahı |
| Yelken | yelken bezi + halat | Meyve Bahçesi |
| Erzak | meyve | Suya indirme töreni → "KAÇTIN!" |

Her aşama bitince ekranda "… tamamlandı!" kutlaması ve konfeti. Final töreni: geniş açı (ada + uzun iskele + gemi),
oyuncu ve işçiler sahil boyunca dağılır (kimi kameraya dönüp zıplar / şapka atar / el sallar, kimi gemiye el sallar;
farklı yönler, farklı zamanlar), gemi iki rayın üstünde beşiğiyle kayarak suya iner, su sıçrar, havai fişekler patlar,
kamera adanın üstünden uzaklaşan gemiyi takip eder (kadrajı kapatan yüksek nesneler gizlenir), ardından kaçış ekranı
(süre, kaptanlık XP'si, kristal, "Koleksiyon 3/5 · Sandıklar 6/8", altın figür varsa "EFSANEVİ GEMİ!", "Tekrar oyna").
Miktarlar ve süreler `Data/Ship.asset`'te.

### Tayfa Barakası (işçiler)
Kampın kuzeyindeki korsan evi (Quaternius House1). Üstündeki daireye girince alttan panel açılır: her işçi türü için kart
(resim, "1/3", "TUT" + fiyat; binası kapalıysa "Bıçkıhane gerekli") + İşçi Hızı / İşçi Kapasitesi geliştirmeleri.
Tutulan işçi kapıdan çıkar, kısa sevinir (konfeti), işe koşar. Sayılar `CrewManager`'da, kayıtta `Crew`.
Kamptaki tezgahta sadece oyuncu geliştirmeleri (+ Bina Hızı) kaldı.

### Keşif
- **Çalılık** (`RegionThicket`): kapalı ana ada bölgeleri (Kayalık, Bahçe, Koy) sık çalı + sarmaşıkla kaplı, geçilmez
  (içi kısmen görünür). Açılınca kamera kısa odaklanır, çalılar girişten başlayarak kesilip savrulur, yapraklar saçılır.
  Adacıklarda örtü yok (köprü kurulmadan ulaşılamaz). Bulutlar kaldırıldı.
- **Bölge kimlikleri** (`RegionBuilder`): Kayalık = taş ocağı (arkada kaya yüzü, ahşap iskele, raylar + maden arabası,
  kazma rafı, taş yığınları); Meyve Bahçesi (alçak süslü çit, kemerli ahşap kapı, sulama kuyusu, meyve kasaları, ağaç
  sıraları); Palmiye Koyu / keten tarlası (kurutma rafları, bağlı lif demetleri). Kayalık ve Koy'da çit yok.
- **Tanıtım tabelaları** (`InfoSign`): kapalı bölgelerin ve köprülerin girişinde; yaklaşınca kart (ad, kaynaklar ve
  binalar ikonlarla, gizli sandık "? ?", açma bedeli). Rehber oku tabelayı önce gösterir.
- **Adacıklar ve köprüler**: Batık Koyu (güney; Bıçkıhane'nin güneyinden dar uzun köprü; hurda + Demirci) ve Kayıp
  Tapınak (batı; harabe, sandıklar, altın kafatası, altın figür parçaları). Köprü başına gelince kamera karşı kıyıyı
  gösterir (batık, Demirci iskeleti, hurda). Kalaslar köprü başından karşıya tek tek tahta olur (kalın direkler, halat
  korkuluk). Bitince kamera köprü boyunca kayar, karşıdakiler zıplayarak belirir, "BATIK KOYU AÇILDI!", kamera döner.
- **Uzun iskele** (`Pier`): geminin batısında denize uzanır, ucuna kadar yürünür (halat korkuluk, fenerler, variller);
  ucunda kaptan dürbünü; yanında onarılacak sandal (yan görev "Sandalı Onar").
- **Ambar** (`Storage`): gemi teslim alanının doğusunda, Kayalık'tan sonra 450 (rehber göstermez). Bırakma alanında
  sırttakiler ambara akar; alma alanında alttan panel (ikon + miktar, "Al"). Kapasite 100/160/240 (Yükselt tabelası);
  doluyken "Ambar dolu!". Taşıyıcılar ambardan gemiye ve bina girişlerine taşır. Kayıtta; kaçışta boşalır.
- **Sandıklar** (8, parlar; yaklaşınca açılır: para + kristal, bazılarından nadir kostüm) ve **koleksiyon** (5 eşya; dönen
  3D kart "Koleksiyon 2/5"; 5/5 ek kristal). Profilde kalır; yeniden oynayınca bulunanlar soluk görünür.
- **Yan görevler** (`Data/Quests`, 15 tane): sol üstte en fazla 2 kart; topla / sat / üret / onar (kuyu, çit, iskele) /
  sandık bul / bölge keşfet / altın figür. Ödül para ve/veya kristal + kısa kutlama.
- **Bina seviyeleri**: binanın yanında ahşap "Yükselt" tabelası; yaklaşınca alttan panel (seviye, hız/kapasite artışı,
  fiyat, Yükselt). Hız ve kapasite artar, bina büyür.
- **Ağaç/kaya yeniden belirme**: kütük bir süre kalır, gömülür; fidan → genç ağaç → tam ağaç zıplayarak büyür; kaya
  ve hurda yerden yükselir (`ResourceNode.RegrowStyle`).
- **Altın figür**: tapınaktaki 3 parça → geminin burnunda altın figür, kaçışta "EFSANEVİ GEMİ!" + ek kristal.
- **Kristal** kalıcı ikinci para (sadece sandık, görev, koleksiyon, kaçış). **Kostümler** (`Data/Costumes`): 7 karakter +
  4 yelken/bayrak; bazıları sadece sandıktan. Bu turda mağaza yok; Test Menüsü'nden giyilir.

## Sesler
- Hepsi **CC0** (ya da ticari kullanıma açık); kaynak/yazar/lisans **Docs/Lisanslar.md**'de. 8-bit/chiptune ses kullanılmaz.
  Adaylar `ThirdParty/Audio/Adaylar/<ihtiyaç>/`, yedekler `ThirdParty/Audio/Claude_Uretim/`; daha iyisi için elle indirilecekler
  **Docs/IndirilecekSesler.md**. Bağlantılar `PrototypeSceneBuilder.CreateSoundLibraryIfMissing` (değiştirince `SoundVersion`++).
- Toplama sesleri 3-4 çeşit, perde ±%8, kısık; para ve kutlamalar öne çıkar. Arka plan müziği (Dizzy Crow "Pirate's Orchestra")
  döngüde kısık çalar, kutlamalarda kısılır (`AudioManager.Duck`), finalde zafer müziğine (RandomMind, 28 sn) yumuşak geçer.
  Ortam: dalga + kuş döngüsü; ara sıra martı sesi. Uzun müzikler telefonda akışla (Streaming) okunur.

## Görsel Stil Kuralları
- **Ana stil: Quaternius Pirate** (low-poly, tek renk atlası `Atlas_Pirate.png`). Önce her zaman Quaternius'a bakılır.
- **Kenney** paketleri (Pirate, Nature, Survival) **sadece Quaternius'ta olmayan eşyalar** için; renkleri uyumlu olanlar seçilir.
- **Sıcak ama yumuşak renkler:** güneşli sarımsı directional ışık, hafif renkli ortam ışığı, çok hafif bloom ve
  doygunluk (göz yormasın), hafif vignette, sıcak beyaz dengesi. Çimen tek renk değil: doğal tonlarda açık-koyu lekeler.
- **Yollar** cetvel gibi değil: `MapLayout.Paths` köşe noktalarından yumuşatılıp hafifçe kıvrılır (`CurvedPaths`), kenarı yumuşak.
- **Ağaçlar** dik durur (eğri palmiye modeli `ModelSpec.Upright` ile dikleştirilir), her biri rastgele ±8° eğik ve
  biraz farklı boyda; gövde dibi zemine oturur. Havada/gömülü nesne kalmamalı: **Tools > Kayıp Ada > Zemin Kontrolü**.
- **Kaynak noktaları** toplandıkça küçülür, tükenince kütük (kayada kırık taşlar) kalır, yeniden dolarken büyür.
- **Sırttaki yığın** 2-3 sütunlu sıkışık blok (parça genişliğine göre), en fazla omuz hizasının biraz üstüne kadar
  (`GameConfig.StackMaxHeight`); fazlası görünmez, yığının üstünde "x40" etiketi. İşçilerde de aynı.
- **Canlılık** (abartısız, çarpışmasız): sahilde yengeçler (yaklaşınca kuma gömülür), martılar (gölgeli, ara sıra ses),
  zıplayan balıklar, bahçede kelebekler, rüzgârda sallanan otlar ve süs palmiyeleri. Sayılar `GameConfig` > "Canlılar".
- **Tabelalar**: bölge adı yalnızca ahşap bölge tabelasında; bina adı binanın üstündeki ahşap tabelada; açma karesinde
  sadece para ikonu + fiyat. Tabelalar hep okunur (uzakta kaybolmaz); diğer dünya yazıları uzakta ve sinematikte kaybolur.
- **Yumuşak gölgeler:** tek kaskad, yumuşak ama ucuz (Low). Küçük süslerin gölgesi kapalı; karakterlerin altında ayrıca yumuşak yuvarlak gölge.
- **Mat malzemeler:** URP **Simple Lit**, parlama yok. Malzemeler **paylaşımlı** (`Art/Materials`), model başına kopya yok.
- **His:** her etkileşimde küçük bir hareket + ses (zıplama, sallanma, kavisli uçuş, konfeti). Harici animasyon paketi yok, `Tween` kullanılır.
- **Yazılar:** font **Kayip Display** = Google Fonts **Lilita One** (SIL OFL 1.1) + eklenen Türkçe harfler (İ Ğ ğ Ş ş;
  Lilita'da yoktu, fontun kendi harf/işaretlerinden birleştirildi; OFL gereği ad değişti). `Art/Fonts/KayipDisplay-Regular.ttf`,
  `OFL.txt`, `BENIOKU.txt`. TMP font asset'i `Art/Fonts/KayipDisplay SDF.asset` (komut yoksa üretir). Krem dolgu, kalın koyu
  kahverengi kenar, hafif alt gölge.
  Dünyadaki yazılarda **kutu yok**; kameraya dönük durur, uzaktayken ve ekran kenarına yaklaşınca yumuşakça kaybolur
  (`LabelFader`). Bölge adları ahşap tabelalarda; bina giriş/çıkışında yazı yerine yerde büyük kaynak ikonu + ok.
  Uçan yazılar üst üste binmez (aynı yazı tazelenir, farklılar yukarı kayar).
- Hangi nesne için hangi model seçildiği: **`Docs/Modeller.md`** (komut her çalıştığında yeniden yazılır).

## Plan (6 adım)
Her adım Unity'de test edilir, çalışıyorsa Git'e kaydedilir. Bir adım bitmeden sonrakine geçilmez.
Her adım sonunda bu dosya, sahne kurma komutu ve `Docs/Modeller.md` güncellenir.

| # | Adım | Bitti sayılır, eğer... | Durum |
|---|---|---|---|
| 1 | 3D + vitrin sahili | Başlangıç sahili 3D, Quaternius modelleri, animasyonlu karakterler, su, ışık, efektler ve seslerle "vitrin" kalitesinde; 2D'deki tüm oynanış çalışıyor | ✅ (otomatik testlerle) |
| 2 | Tüm ada aynı kalitede | Bütün bölgeler aynı kalitede; geliştirme tezgahı (5 geliştirme × 5 seviye); sonraki hedefi gösteren ok; denge tablosu | ✅ (otomatik testlerle) |
| 3 | Gemi + üretim zincirleri + final töreni | Lif → halat/yelken bezi zinciri, 5 aşamalı gemi (her aşama sonraki hedefi açar), suya indirme töreni, kaçış ekranı, Hızlı Test Modu. (Savunma tasarım kararıyla kaldırıldı.) | 🔄 Yapıldı, otomatik testler geçti; elle deneme bekliyor |
| 4 | Kayıt + menü + arayüz + ses | Kaldığı yerden devam ✅ (kayıt sistemi, kaptanlık kalıcı); ana menü, ayarlar, müzik ⬜ | 🔄 Kayıt kısmı yapıldı |
| 5 | Telefon + denge + rehber | APK telefonda 60 FPS akıcı; yeni biri rehberle 30-45 dakikada bitirebiliyor | ⬜ |
| 6 | Reklam + yayın | Ödüllü reklam test modunda; 12 testçiyle 14 gün tamam, oyun Play'de | ⬜ |

## Klasör Düzeni
Oyunun tüm dosyaları `Assets/_Game/` altındadır. `Assets/` altındaki diğer klasörler Unity/paketlere aittir:
`Assets/Settings` (eski 2D URP ayarları, Input System ayarları — dokunma), `Assets/TextMesh Pro` (TMP kaynakları,
komut ekler) ve `Assets/Resources/BillingMode.json` (satın alma paketinin dosyası — dokunma).

```
Assets/_Game/
  Scripts/
    Core/            Genel sistemler: ayarlar, para, kamera, Tween (animasyon yardımcısı), efektler, ada şekli
    Audio/           Ses yöneticisi, ses türleri, ses kütüphanesi
    Player/          Sadece oyuncuya özel davranışlar
    Characters/      Oyuncu ve işçilerin görünüşü (animasyon, dönme, taşıma pozu, toz)
    ResourceSystem/  Kaynak türleri, kaynak parçaları, taşıma yığını, yerdeki yığın, kaynak noktaları
    Zones/           Üzerine girilince çalışan alanlar (Pazar, açma kareleri)
    Areas/           Açılabilir bölgeler: tanım, yönetici, sahnedeki bölge kökü
    Buildings/       İşleme binaları: bina türü, genel bina, giriş ve çıkış alanları
    Workers/         İşçiler: işçi türü, ortak temel, Oduncu, Taşıyıcı, yürüme, kamptan çıkarma
    Navigation/      Engeller ve yol bulucu (XZ düzlemi)
    Upgrades/        Geliştirmeler: tür, yönetici, kamptaki tezgah, alttan açılan panel (+ Tayfa Barakası işçi kartı)
    Discovery/       Keşif: bulut, köprü, onarım (ortak teslim alanı), sandık, koleksiyon/figür eşyası
    Quests/          Yan görevler: görev türü (ScriptableObject) ve yönetici
    Costumes/        Kostüm türü, oyuncu kostümü, yelken rengi
    Ship/            Gemi inşası, teslim alanı, suya indirme töreni
    Save/            Kayıt sistemi (JSON, otomatik)
    DevTools/        Test Menüsü (sadece Editor ve geliştirme sürümü)
    Life/            Adadaki canlılar: yengeç, martı/kelebek (Flyer), yönetici (IslandLife)
    UI/              Ekran arayüzü
    Editor/          Editör komutları (oyuna dahil edilmez): kurulum, içe aktarma, görsel/prefab üretimi, sahne
  Shaders/           Water (dalgalı su, parıltı), Shore (kıyıya vuran dalga, köpük, ıslak kum), UnlitTransparent, BuildReveal
  Tests/             PlayMode testleri (KayipAda.Tests asmdef): temel döngü, geliştirme, lif zinciri, gemi aşamaları, tören, kayıt, Test Menüsü, kütük/yeniden büyüme + ekran görüntüleri
  Data/              Ayar dosyaları (aşağıda)
  Prefabs/           Characters, Enemies, Items, Nodes, Buildings, World (Pazar, açma karesi, kamp), Decor, FX
  Animation/         Character.controller (Idle/Walk/Run karışımı) + karakter başına override
  Art/Generated/     Kodla üretilen: ada zemini modeli ve dokusu, su ağı, kıyı uzaklık dokusu, arayüz dokuları
  Art/Materials/     Paylaşımlı malzemeler (komut üretir)
  Art/Fonts/         Oyun fontu (Kayip Display = Lilita One + Türkçe, OFL) + lisansı + açıklama + TMP font asset'i
  Audio/Generated/   Kodla üretilen sesler (pop, vuuş)
  Settings/          URP_Mobile (+ Renderer), PostProcess (bloom, renk, vignette)
  Scenes/            Prototype.unity (sahne kurma komutu üretir — elle düzenleme kaybolur)
  ThirdParty/        Quaternius_Pirate (+ Textures/Atlas_Pirate.png), Kenny_Pirate, Kenny_Nature, Kenny_Survival,
                     Audio (Impact, Interface, RPG, Jingles = Kenney Music Jingles CC0)
Docs/Modeller.md     Model ve ses seçim tablosu (proje kökünde)
Docs/Denge.md        Fiyat/hız denge tablosu (ilk kaçış ~33 dk)
Docs/Lisanslar.md    Her sesin/modelin/fontun kaynağı, yazarı ve lisansı
Docs/IndirilecekSesler.md  Elle indirilecek daha iyi ses adayları (Freesound/Pixabay/Mixkit linkleri)
Docs/GeceRaporu.md   Gece çalışmasının raporu
Docs/Screenshots/    Otomatik ekran görüntüleri (testler üretir)
Docs/Screenshots/Kesif/  Keşif turu ekran görüntüleri
Docs/Screenshots/Duzeltme3/  Oyuncu testi düzeltme turu: once/ ve sonra/
```

### Ayar dosyaları (`Data/`)
```
GameConfig.asset     Genel sayılar: hız, kapasite, kamera, yığın sallanması, efekt, ses, açma hızı...
ResourceTypes/       Wood 5, Stone 8, Fruit 4, Plank 14, Brick 22, Fiber 3, Rope 14, Cloth 20, Scrap 6, Nail 18 (ad, renk, fiyat, model)
NodeTypes/           Tree, Rock, FruitBush, FiberPalm (= keten tarlası), Scrap (batık parçası) (stok, aralık, dolma, vuruş sesi)
Areas/               Rocky 150, Orchard 600, Sawmill 250, StoneWorkshop 800, Cove 500, RopeWorkshop 700, Loom 900, Forge 600,
                     Islet1/Islet2 (köprü bitince açılır), <Bina>Lv2/Lv3 (Yükselt tabelası, 350-1.700), Storage 450 +
                     StorageLv2/Lv3 700/1.300 (Ambar),
                     ShipStage1-4 (karesi yok: gemi aşaması bitince kod açar), HireX (eski kayıtlar için; artık kare yok)
Buildings/           Sawmill (2 odun → 1 kalas, 2 sn), StoneWorkshop (2 taş → 1 tuğla, 3 sn),
                     RopeWorkshop (3 lif → 1 halat, 3 sn), Loom (4 lif → 1 yelken bezi, 4 sn), Forge (2 hurda → 1 çivi, 3 sn)
Workers/             Lumberjack, Carrier, FiberGatherer (Lifçi), Smith (Demirci Çırağı): hız, kapasite, Tayfa Barakası fiyatları
                     (dizinin uzunluğu = en fazla sayı), gereken bina, kilit yazısı, kart resmi
Quests/              Q01-Q15 yan görevler (tür, hedef, miktar, ödül, gereken bölge; sıra önemli)
Costumes/            C_* karakter kostümleri (7), S_* yelken/bayrak (4): ad, kristal fiyatı, nadirlik, sadece sandık, model, renk
Upgrades/            PlayerCapacity, PlayerSpeed, WorkerSpeed, WorkerCapacity, BuildingSpeed (5 seviye fiyat + etki)
Ship.asset           5 gemi aşaması (malzeme, açtığı kilit, kutlama yazısı), final töreni süreleri (toplanma, kayma, havai fişek, uzaklaşma)
SoundLibrary.asset   Hangi olayda hangi ses (Docs/Modeller.md'de liste; komut sadece eksik sesleri ekler)
```

> Not: Kaynak scriptleri klasörünün adı bilerek `ResourceSystem`'dır, `Resources` değil. Unity'de
> `Resources` adlı her klasör özel anlam taşır (içindeki her şey oyuna gömülür), karışıklık olmasın.

## Sınıfların Görevleri
| Sınıf | Klasör | Görevi |
|---|---|---|
| `GameConfig` | Core | ScriptableObject. Oyuncu hızı/kapasitesi, satış, açma hızı, kamera (açı, FOV, uzaklık), joystick, yığın sallanması, efekt ve ses sayıları |
| `CurrencyManager` | Core | Parayı tek yerde tutar (`Coins`), `Add` / `TrySpend`, `CoinsChanged` olayı. Singleton (-100) |
| `CameraFollow` | Core | ~45° eğik, dar açılı perspektif kamera; oyuncuyu yumuşak takip eder, oyuncunun biraz önüne bakar |
| `Tween` / `TweenRunner` | Core | Basit animasyon yardımcısı: `PunchScale`, `Shake`, `PopIn`, `PopOut`, `MoveArc`, `Value`. Runner sahneye kendini ekler |
| `Effects` | Core | Efektlerin tek giriş noktası: `Confetti`, `CoinPop` (+ "+5" yazısı), `FlyCoin`, `ShowText`. Havuz kullanır |
| `FloatingText` | Core | Yükselip kaybolan dünya yazısı |
| `ProgressRing` | Core | Dairesel dolan halka (bina üstü, açma karesi kenarı) |
| `Billboard` | Core | Nesneyi kameraya döndürür (yazılar, halkalar) |
| `LabelFader` | Core | Dünyadaki yazı/işaretleri uzaklığa ve ekran kenarına göre yumuşakça gösterir/gizler |
| `Haptics` | Core | Telefon titreşimi (kutlamalar); ayarlardan kapatılır (PlayerPrefs) |
| `Sway` | Core | Bitkilerin rüzgârda sallanması (keten tarlası) |
| `FenceGate` | Areas | Bölge çitinin kapısı: kapalıyken geçilmez, bölge açılınca kanatlar dışa açılır |
| `IslandNavMesh` | Navigation | İşçilerin NavMesh'i (ada zemini − engeller); engel değişince arka planda yeniden kurulur |
| `Crab` / `Flyer` / `IslandLife` | Life | Yengeç (yan yürür, kaçıp kuma gömülür), martı/kelebek uçuşu, canlıları oluşturan yönetici (+ zıplayan balık, martı sesi) |
| `ConfettiOverlay` | UI | Ekranı kaplayan konfeti (final ve kaçış ekranı), arayüzün en üstünde |
| `BurstText` / `SettingsPanel` | UI | Ekran ortasında esneyen "AÇILDI!" yazısı; sağ üstte "AYAR" (titreşim/ses) |
| `RegionThicket` / `InfoSign` / `Pier` | Discovery | Kapalı bölge çalılığı; tanıtım tabelası kartı; denize uzanan iskele (yürünebilir) |
| `BuildingUpgradeSign` / `BuildingUpgradePanel` | Buildings / UI | Binanın (ya da Ambar'ın) "Yükselt" tabelası; alttan açılan yükseltme paneli (yürümeyi engellemez) |
| `Storage` / `StorageDropZone` / `StoragePickZone` / `StoragePanel` | Zones / UI | Ambar: içerik, kapasite, kayıt; bırakma ve alma alanları; alttan Ambar paneli |
| `SeaSettings` / `TextUtil` | Core | Deniz/kıyı shader yoğunlukları (GameConfig → global); Türkçe büyük harf |
| `IslandShape` | Core | Kara şekli: ana ada + adacıklar (elips + dalgalar) ve açılmış köprüler. `SignedDistance`, `Contains`, `ClampInside`, `OpenBridge` |
| `AppSettings` | Core | 60 FPS hedefi, ekran kararmasın |
| `AudioManager` | Audio | Tek ses yöneticisi: `AudioManager.Play(SoundId.X)`. Üst üste çalınca perde yükselir; ortam dalga sesi (yoksa üretir); ses yoksa hata vermez |
| `SoundLibrary` / `SoundId` | Audio | Hangi olayda hangi ses dosyaları (rastgele seçilir) |
| `PlayerController` | Player | Joystick/klavye ile XZ'de hareket; binalara çarpar (`NavObstacle.PushOut`), denize çıkamaz (`IslandShape`) |
| `CharacterVisual` | Characters | Animator `Speed` (Idle→Walk→Run), yürüme yönüne dönme, **taşıma pozu** (yığın varken kollar öne, kodla), koşarken toz, adım sesi |
| `ResourceType` | ResourceSystem | ScriptableObject. Kimlik, ad, renk, fiyat, **model prefab'ı** (`ItemPrefab`), `Spawn(pos)` |
| `ResourceItem` | ResourceSystem | Tek kaynak parçası. `FlyTo` (kavisli uçuş; yenisi eskisini iptal eder), `FlyToAndDestroy` |
| `CarryStack` | ResourceSystem | Karakterin önündeki 3D yığın. Her kat alttakini gecikmeyle takip eder (sallanma). `TryAdd`, `RemoveTop()`, `RemoveTop(type)`, `CountOf`, `IsPlayer`, `WorkTarget` |
| `ItemPile` | ResourceSystem | Yerdeki 3D yığın (sütun x satır x kat). Bina giriş/çıkışı |
| `ResourceNodeType` | ResourceSystem | ScriptableObject. Nokta türü: kaynak, stok, toplama aralığı, yeniden dolma |
| `ResourceNode` | ResourceSystem | Kaynak noktası: her toplamada sallanır + sıkışıp genişler; stok azaldıkça küçülür (`NodeMinStockScale`), tükenince kütük/kırık taş (`remnant`) kalır, dolarken büyür. `GetStandPoint` (işçi gövdeye girmesin) |
| `InteractionZone` | Zones | 3D trigger alanların temeli; içerideki **tüm** `CarryStack`'ler `Visitors`'ta. `CanServe`: oyuncuya her alan, işçiye sadece hedefi |
| `Market` | Zones | Pazar: parça tezgaha uçar, varınca para + zıplayan para + "+5" + ses |
| `UnlockPad` | Zones | Yerde yuvarlak açma karesi: dolan altın halka, büyük fiyat, paralar oyuncudan kareye uçar; bitince konfeti + ses, kare küçülerek kaybolur; `requiredArea` açılınca zıplayarak belirir |
| `AreaDefinition` / `AreaManager` | Areas | Bölge/bina/işçi tutma tanımı ve hangilerinin açık olduğu (kayıt sistemi bunu kullanacak) |
| `UnlockableArea` | Areas | Bölge/bina kökü: açılınca çit küçülerek kaybolur, içerik sırayla zıplayarak belirir |
| `BuildingType` / `ProcessingBuilding` | Buildings | Tarif; bina: ham madde binaya uçar, ürün çıkışa uçar, üstte halka, çalışırken duman/talaş, ürün çıkınca zıplar |
| `BuildingInput` / `BuildingOutput` | Buildings | Giriş: sırttan sadece kabul edilen türü alır. Çıkış: ürünü sırta verir |
| `WorkerType` / `Worker` / `LumberjackWorker` / `CarrierWorker` | Workers | İşçi ayarları; ortak temel (`GoTo`, `Wait`, törende `Gather`); Oduncu (Lifçi de aynı sınıf: koy palmiyeleri → Halat Atölyesi) ve Taşıyıcı (tüm bina ürünleri → gemi ya da Pazar) |
| `WorkerMover` / `WorkerSpawner` | Workers | XZ'de yol bularak yürüme; tutulan sayıya ulaşana kadar baraka kapısından işçi çıkarma (oyun içinde tutulan sevinir) |
| `CrewManager` / `CrewCard` | Workers / Upgrades | Tutulan işçi sayıları (kayıt), fiyat, kilit; barakadaki işçi kartı (UpgradePanel kartları da yönetir) |
| `Machine` / `RecipeBalloon` | Buildings | Halat çarkı / dokuma tezgahı / örs: sadece bina işlerken hareket + ses; yaklaşınca "3 [lif] → 1 [halat]" balonu |
| `TrailPool` / `TrailEmitter` | Core / Characters | Kumda ayak izi, kıyıda su halkası, çimende yaprak (en fazla 20, havuz; `GameConfig.TrailsEnabled`) |
| `ProfileManager` | Core | Kalıcı profil: kristal, bulunan sandıklar, koleksiyon, kostümler (kaçıştan sonra da kalır) |
| `CostumeDefinition` / `PlayerCostume` / `SailCostume` | Costumes | Kostüm ayarı; oyuncu modelini değiştirir; yelken/bayrak rengini boyar |
| `ConstructionSite` / `BridgeBuilder` / `RepairSite` | Discovery | Malzeme teslimiyle yapılan yapı (ortak); aşamalı köprü (adacığı açar); kırık kuyu/çit/iskele |
| `TreasureChest` / `Collectible` | Discovery | Gizli sandık; koleksiyon / altın figür eşyası (alınınca kaybolur) |
| `QuestDefinition` / `QuestManager` / `QuestCard` | Quests / UI | Yan görev ayarı; görevleri sıralar, oyun olaylarıyla ilerletir, ödül verir; sol üst kart |
| `CollectionPopup` / `CrystalUI` | UI | Koleksiyon kartı (vitrin kamerasıyla dönen 3D eşya); para panelinin altında kristal |
| `ShipFigurehead` | Ship | Geminin burnundaki altın figür (3 parça + gövde) |
| `NavObstacle` / `Pathfinder` | Navigation | XZ dikdörtgen engeller; istenirse dönük (`oriented`) ve köşeleri yuvarlak (`cornerRadius`): oyuncu takılmadan kayar (gemi+kızak tek engel). Yol bulucu yedek |
| `VirtualJoystick` / `CurrencyUI` / `SafeArea` | UI | Joystick; üstteki para paneli (sayarak artar, artışta zıplar); çentikli ekran güvenli alanı |
| `EscapePanel` | UI | Kaçış ekranı: zıplayan başlık, süreye göre 3 yıldız, sayarak artan XP ve para, konfeti, nabız atan "Tekrar oyna". Test modunda süre sayılmaz |
| `ProgressionGuide` | Core | Sonraki hedef: en ucuz açılabilir karenin üstünde zıplayan ok + halkası nabız atar; ekran dışındaysa kenarda ok. Kare kalmadıysa gemi |
| `ScreenshotUtil` | Core | Test/kontrol için 1080x1920 PNG ekran görüntüsü (arayüz dahil) |
| `UpgradeType` / `UpgradeManager` | Upgrades | Geliştirme ayarı; seviyeleri tek yerde tutar. `Multiplier(stat)` (hızlar), `Extra(stat, temel)` (kapasite), kaptanlık bonusu dahil |
| `UpgradeStation` / `UpgradePanel` / `UpgradeRow` | Upgrades | Kamptaki tezgah; üstüne gelince alttan panel açılır (açıkken oyuncu yürümez, `PlayerController.InputBlocked`); satırlar seviye noktaları, fiyat, "MAKS" |
| `ShipConfig` | Ship | ScriptableObject: 5 aşama (malzeme, `UnlockOnComplete` kilidi, kutlama yazısı), tören süreleri |
| `ShipBuilder` / `ShipDelivery` | Ship | Gemi inşası: her aşamanın parçaları tek tek gelir (omurga+kaburgalar düşer, gövde aşağıdan kapanır, direkler yükselir, yelkenler açılır, erzak+bayrak zıplar; toz+kıvılcım). Aşama bitince yavaşlama, kamera gemiye, altın ışık, üst band, müzik, titreşim, kilit açma; teslim alanı (oyuncu + Taşıyıcı) |
| `ShipProgressView` | Ship | Teslim alanı üstündeki kutusuz gösterge: aşama adı + malzeme ikonu + "12/30" + ince çubuk |
| `DeliveryMarker` | Ship | Gemi teslim alanında zıplayan ok + nabız atan yer işareti (yerde gemiyi gösteren oklar da var) |
| `DevCommands` / `DevMenu` | DevTools | Test Menüsü komutları ve sol üstteki "TEST" butonlu oyun içi menü. **Sadece Editor ve Development Build**; satış sürümünde menü kendini siler, komutlar çalışmaz |
| `LaunchCeremony` | Ship | ~20 sn final: ışık gün batımına döner; kamera alçaktan ve yandan (ada arkada); toplanma + sevinç (Cheer), kamera gemi çevresinde döner, yana kayma + büyük sıçrama, zafer müziği, iz bırakan havai fişekler, ekranı kaplayan konfeti, gemi burnunu denize çevirip uzaklaşır (kamera arkadan), kaçış ekranı |
| `CelebrationBanner` | UI | Ekranın ortasında kısa kutlama yazısı ("Gövde tamamlandı!" + açılan yeni hedef) |
| `SaveData` / `SaveManager` | Save | JSON kayıt (persistentDataPath/kayipada_save.json): para, alanlar, geliştirmeler, gemi; 10 sn'de bir, arka planda ve kapanırken; kaptanlık XP kalıcı (seviye başına %3, en fazla 10) |

### Editör komutları (`Scripts/Editor`)
| Dosya | Görevi |
|---|---|
| `PrototypeSceneBuilder` | **Tools > Kayıp Ada > Prototip Sahnesini Kur**: hepsini sırayla çalıştırır ve sahneyi kurar |
| `ProjectSetup3D` | URP_Mobile oluşturur/ayarlar (MSAA 2x, yumuşak ucuz gölge, HDR kapalı, ek ışık yok), Graphics + tüm Quality'ye atar, Linear renk, TMP kaynakları |
| `ThirdPartyImportProcessor` | ThirdParty içe aktarma ayarları: kamera/ışık yok, karakterlerde Generic rig + animasyonlar ayrılır ve döngüye alınır, renk dokuları keskin, sesler mono. Ayar değişince `Version` artırılır |
| `ModelCatalog` | **Hangi nesne → hangi model + hedef boyut** (tek yer). Modeli değiştirmek için burası |
| `ShipParts` | Gemi modelini gövde / direkler / yelkenler olarak böler (renk atlasına göre), omurga+kaburgaları kodla üretir |
| `IconGenerator` | Kaynak ikonlarını kodla çizer (`Art/Generated/Icon_*.png`) |
| `PrefabFactory` | Prefab üretimi: model ölçeklenir, tabanı yere oturur, malzemeler paylaşımlı olanlarla değişir; karakter, kaynak, nokta, bina, Pazar, açma karesi, kamp, süs, efekt |
| `CharacterAnimation` | Animator Controller (Speed ile Idle/Walk/Run) + karakter başına override |
| `ArtGenerator` | Ada zemini modeli/dokusu (kum, çimen, yollar, bölge tonları), su ağı, kıyı uzaklık dokusu, arayüz dokuları, malzemeler |
| `MapLayout` | Harita yerleşimi (konumlar, bölgeler, yollar) |
| `UiFactory` / `BuilderUtil` / `DocsWriter` | TMP/arayüz yardımcıları; alan atama yardımcıları; Docs/Modeller.md yazıcısı |
| `CampBuilder` | Tayfa Barakası, işçi kartı resimleri (karakter kodla çekilir), açık Liman Pazarı (satıcı el sallar) |
| `MachineBuilder` | Halat çarkı, dokuma tezgahı, Demirci ocağı (tente + direkler; hareketli parçalar `Machine`'e bağlanır) |
| `DiscoveryBuilder` | Adacık zemini/dokusu, köprüler, sandık ve koleksiyon prefab'ları, tapınak, onarım yerleri, altın figür |
| `RegionBuilder` | Çalılık, taş ocağı / bahçe / koy kimliği, tanıtım kartı, Yükselt tabelası, Ambar, uzun iskele, kızak rayları + beşik |
| `UiBuilder` | Geliştirme paneli, aşama kutlama yazısı, kaçış ekranı |
| `GameMenus` | **Tools > Kayıp Ada > Hızlı Test Modu** (aç/kapat, fiyatlar %8, malzeme %20) ve **Kaydı Sil** |
| `DevMenus` | **Tools > Kayıp Ada > Test > …**: Test Menüsü'nün aynı komutları (sadece Play modunda) |
| `GroundCheck` | **Tools > Kayıp Ada > Zemin Kontrolü**: havada duran / gömülü nesneleri Console'a yazar (BuildScene de çalıştırır) |
| `BatchTools` | Komut satırı: `BuildScene` (sahneyi soru sormadan kurar), `CaptureEditShots` |

### Otomatik test (batchmode)
- Unity kapalıyken doğrudan projede koşar (Unity açıkken aynı proje batchmode'da açılamaz).
- Sahne: `Unity.exe -batchmode -projectPath . -executeMethod KayipAda.EditorTools.BatchTools.BuildScene -quit`
- Testler: `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults sonuc.xml -shotsDir Docs\Screenshots -shotPrefix <ad>`
  (grafikle çalıştırılır, `-nographics` YOK; ekran görüntüleri `Docs/Screenshots/<ad>_*.png`).
- Testler `SaveManager.Enabled = false` ile kayıt okumadan/yazmadan temiz başlar.

## Test Menüsü (geliştirici araçları)
- Oyun içinde sol üstte küçük **TEST** butonu (sadece Editor ve Development Build). Aynı komutlar
  **Tools > Kayıp Ada > Test** menüsünde. Komutlar: Sınırsız Para, +1000 Para, Tüm Bölgeleri Aç, Tüm Binaları Aç,
  Her İşçiden 1 Tane, Sırttaki Yığını Seçili Malzemeyle Doldur, Gemi Aşamasını Tamamla, Final Törenine Atla,
  Oyun Hızı x1/x2/x5, +100 Kristal, Tüm Kostümleri Aç, Sıradaki Kostümü Giy, Tüm Sandıkları Göster (ışık sütunu),
  Koleksiyonu Tamamla, Görevleri Tamamla, Köprüleri Tamamla, Çalılıkları Kaldır/Getir, Ambarı Aç ve Doldur,
  Kaydı Sil ve Yeniden Başlat.
  Sayılar `GameConfig` > "Test Menüsü".
- Satış sürümü: Build Settings'te **Development Build kapalı** olmalı; o zaman menü hiç görünmez.
- Menü sol üstten kayan dar yan paneldir (ekranın ortasını kapatmaz). Test komutu kullanılınca ya da Hızlı Test Modu açıkken
  oyun süresi sayılmaz (kaçış ekranında "test modu").

## Sahne Kurma Komutu
Sahne elle kurulmaz. **Tools > Kayıp Ada > Prototip Sahnesini Kur**:
- İlk çalıştırmada TextMeshPro kaynaklarını ekler; içe aktarma bitince kurulum **kendiliğinden devam eder**.
- Sahneyi her seferinde **sıfırdan** kurar ve `Assets/_Game/Scenes/Prototype.unity` üzerine kaydeder.
- Ayar dosyaları (`Data/`, `Settings/PostProcess`) **sadece yoksa** oluşturulur; kullanıcının değiştirdiği değerler korunur.
  (Builder'daki başlangıç değerini değiştirmek mevcut dosyayı değiştirmez; Inspector kullanılır ya da dosya silinir.)
- Prefab'lar, üretilen görseller, malzemeler ve animasyon override'ları her seferinde yeniden üretilir (GUID korunur).
- Sahneye yeni bir nesne eklenecekse **her adımda** önce bu komut güncellenir.

### Harita — `MapLayout`
```
                         ~~~~~~~~ deniz ~~~~~~~~
                [uzun iskele]  GEMİ (iki raylı kızak)   AMBAR       z ≈ 17-26
 KAYIP   köprü  ┌─ KAYALIK (taş ocağı) ─┐ yol ┌─ BAHÇE ─┐ HALAT ATÖLYESİ   z 6..15 (çalılıklı, başta kapalı)
 TAPINAK ═════  │ kaya yüzü  TAŞ ATÖLY.  │  |  │ kemer   │ KETEN TARLASI    z -3.5..3.5 (Palmiye Koyu)
                └────────[kare]─────────┘  |  └─[kare]──┘ DOKUMA TEZGAHI   z -8
 TAYFA BARAKASI      palmiye    palmiye    palmiye
  KAMP                          oyuncu    kuyu                       z -6
            PAZAR (açık tezgah)        BIÇKIHANE                     z -12
                                          ║ köprü (güneye)
                                     BATIK KOYU (Demirci, hurda)     z -35
```
- Ada: merkez (0, 3), yarıçap (20, 24) + kıyı dalgaları. Kenarda kum şeridi (~3.4 m), içi çimen, yollar açık toprak.
- Kareler: Kayalık 150, Bıçkıhane 250 (baştan); Taş Atölyesi 800 (Kayalık sonrası); Palmiye Koyu 500 (Omurga sonrası),
  Halat Atölyesi 700 (Gövde sonrası), Dokuma Tezgahı 900 (Direk sonrası), Meyve Bahçesi 600 (Yelken sonrası),
  Demirci 600 (Batık Koyu köprüsünden sonra). İşçiler kare yerine Tayfa Barakası'ndan tutulur.
- Adacıklar: Batık Koyu (6, -35) ve Kayıp Tapınak (-31, 9), kendi zemin dokuları; köprü uçları kıyıların içinde.
- Gemi iskelenin doğusunda kumsalda, **kıyıya paralel** (burnu 20° denize dönük), ahşap kızağın (altlık + denize uzanan
  raylar) üstünde; teslim alanı geminin kara tarafında, inşa iskelesinden uzakta açık zeminde. Gemi + kızak + iskele tek, dönük,
  köşeleri yuvarlak engel (deniz tarafında kıyıya kadar; dar boşluk yok); törende kalkar; gemi yana kayıp suya iner.
- Bölgeler kapalıyken çalılıkla kaplı (geçilmez). Sadece Bahçe'de alçak süslü çit + kemerli kapı (karenin tarafında).
- Çarpışma: binalar, ağaç/kaya gövdeleri, büyük süs palmiyesi ve kayalar, çitler, tabelalar, gemi `NavObstacle`; otlar,
  çiçekler, çakıllar, keten tarlası geçilebilir. Oyuncu engele çarpınca yanından kayar; işçiler NavMesh ile dolaşır.
- Bina düzeni (kök = binanın önü): gövde arkada (z +0.9), giriş solda-önde, çıkış sağda-önde (z -1.5), halka üstte.
  Giriş/çıkışta yerde büyük ikon + üstünde süzülen ikon (hep görünür); oyuncu yaklaşınca tarif balonu.
- Quaternius çevre/eşya modelleri (Environment_*, Prop_*) dosyadaki dönüşleriyle kullanılır (`ModelSpec.KeepImportRotation`);
  yoksa yan yatık görünürler (eski "dik duvar" iskele sorunu buydu).

### Yerleşim kuralları
- Joystick tüm ekranda; para paneli üstte ortada, güvenli alanın (SafeArea) içinde. Yeni arayüz parçaları ekranın kenarlarına.
- Sol üst köşe Test Menüsü butonuna ayrılmıştır (sadece geliştirme sürümünde görünür).
- Oyun başında görünen önemli nesneler dar (20:9) telefonlarda yandan taşmamalı: oyuncu çevresinde x ≈ ±5.
- Komşu alanların trigger'ları çakışmamalı; oyuncunun başlangıç noktası hiçbir alanın içinde olmamalı.
- Süsler yollara, oyun alanlarına ve kapalı bölgelerin içine konmaz (`ScatterDecor`, `reserved` listesi).

## Kod Kuralları
- **Basit ve okunaklı** kod. Gereksiz soyutlama, interface ağacı veya framework yok. Harici paket yok (Tween kendi kodumuz).
- Her dosyanın başında 2-4 satırlık **Türkçe açıklama yorumu** bulunur (dosya ne işe yarar).
- Kod içi isimler (sınıf, değişken, metod) **İngilizce**; yorumlar ve oyuncuya görünen metinler **Türkçe**.
- Tüm oyun kodu `KayipAda` namespace'i içindedir (editör kodu `KayipAda.EditorTools`).
- Bir dosyada bir sınıf, dosya adı = sınıf adı.

### Sayılar ayar dosyasında
- **Oynanışı ya da hissi etkileyen hiçbir sayı (fiyat, hız, kapasite, süre, aralık, efekt gücü, ses) kodun içine yazılmaz.**
  Genel sayılar `GameConfig`'e, kaynağa özel `ResourceType`'a, nokta türüne özel `ResourceNodeType`'a, fiyatlar
  `AreaDefinition`'a, bina tarifleri `BuildingType`'a, işçi hızı/kapasitesi/sayısı `WorkerType`'a, sesler `SoundLibrary`'ye.
- Kod ayarları **her kullanımda** dosyadan okur (Awake'te kopyalamaz) ki Play sırasında yapılan değişiklik hemen etkilesin.
- Haritanın yerleşimi (konumlar, boyutlar, renkler) oynanış ayarı değildir: `MapLayout`, `ModelCatalog`, builder.

### İsimlendirme
- Sınıf / metod / özellik / olay: `PascalCase`; alan ve yerel değişken: `camelCase`; sabit: `PascalCase`
- Ayar dosyası asset'leri: `PascalCase` (`GameConfig.asset`); `ResourceType.Id` küçük harf (`wood`)
- Bool özellikler soru gibi: `IsFull`; deneyip sonuç dönenler `Try...`: `TryAdd`, `TrySpend`
- Prefab'lar: `Prefabs/<Tür>/<Ad>.prefab`, içinde model `Model` adlı alt nesnede; malzemeler `M_<Ad>`

### Unity kalıpları (3D)
- Inspector alanları `[SerializeField] private`; ScriptableObject'lerde `[field: SerializeField] public X { get; private set; }`.
- Tek örnekli yöneticiler basit singleton (`Instance`); erken çalışanlar `[DefaultExecutionOrder(-100)]` (Effects/Audio -90).
- Sistemler arası haberleşme C# `event` ile; UI her karede değer sorgulamaz.
- `Update` içinde `GetComponent` / `Find` çağrılmaz.
- **Hareket XZ düzleminde**, y = 0 zemin. 2D hesaplar `Vector2(x, z)` ile yapılır.
- Fizik: karakterlerde **Kinematic Rigidbody** + CapsuleCollider (`MovePosition`). Kinematic gövdeler birbirine çarpmaz:
  oyuncu ve işçiler iç içe geçebilir (bilerek). Binalara/ağaçlara çarpma fizikle değil `NavObstacle` ile.
- Etkileşim alanları **3D trigger** collider (`isTrigger`) + `InteractionZone`'dan türeyen bileşen. Taşınan parçalarda collider yok.
- Animasyon/efekt için `Tween` ya da bileşenin kendi `Update`'i; Coroutine yerine bunlar tercih edilir.
- Unity'deki `Tree` sınıfıyla karışmasın diye kaynak noktası `ResourceNode` adını taşır.

### Performans (mobil 60 FPS)
- Hareket etmeyen süsler ve ada **statik** işaretlenir (`BuilderUtil.MarkStatic`). Açılınca zıplayan nesneler statik değildir.
- Küçük süslerin ve taşınan parçaların gölgesi kapalı; gölge sadece büyük nesnelerde ve karakterlerde.
- Partiküller az (en fazla ~70 konfeti, toz 16, duman 14); malzemeler paylaşımlı; su derinlik dokusu kullanmaz.
- URP: MSAA 2x, HDR kapalı, tek kaskad, ek ışık yok, SRP Batcher açık.

## Git Çalışma Düzeni
- Depo: https://github.com/bakiomer/KayipAda. 2D son hali `main` dalında; **3D işi `3d` dalında**.
  Kullanıcı Git bilmiyor; tüm Git işlemlerini Claude yapar ve kısaca Türkçe anlatır.
- Her **çalışan** özellikten sonra kullanıcıya **"Kaydedeyim mi?"** diye sorulur. "Evet" derse: commit + `git push`.
  Commit mesajları kısa, Türkçe ve ASCII karakterli (örn. `3D adim 1: vitrin sahili`).
- Kullanıcı **"geri al"** derse: son commit'ten sonraki tüm değişiklikler atılır ve proje son çalışan kayda döner
  (`git reset --hard HEAD` + `git clean -fd -- Assets ProjectSettings Packages`). Önce neyin silineceği kısaca gösterilir.
  Unity açıksa geri almadan sonra Unity'nin dosyaları yeniden yüklemesi beklenir.
