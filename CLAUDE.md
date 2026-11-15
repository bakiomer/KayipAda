# Kayıp Ada — Oyun Tasarımı ve Kod Kuralları

> **Yön: 3D low-poly** (2D kutulardan geçildi; 2D son hali `main` dalında "2D son durum" kaydında).
> **Şu anki durum:** Savunma (son gece, toplar, iskeletler) çıkarıldı; yerine lif → halat / yelken bezi zinciri ve
> 5 aşamalı gemi + suya indirme töreni geldi. Görsel düzeltme turu (dik palmiyeler, kütük, font, etiket kutuları, yumuşak
> renkler, kıvrımlı yollar, 45° kamera) ve Test Menüsü eklendi. 15/15 test geçiyor (ekran görüntüleri `Docs/Screenshots/L_*`).
> Kullanıcının elle denemesi bekleniyor (iş `3d` dalında). Gece çalışması raporu: `Docs/GeceRaporu.md`.

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
4. Para ile yeni bölgeler, binalar, işçiler ve geliştirmeler açılır.
5. Son hedef: üretim zincirleriyle gemiyi 5 aşamada inşa etmek; gemi bitince suya indirme töreniyle adadan kaçmak.

### Üretim zincirleri
| Ham madde (nereden) | Bina | Ürün |
|---|---|---|
| Odun (sahil palmiyeleri) | Bıçkıhane (2 → 1) | Kalas |
| Taş (Kayalık) | Taş Atölyesi (2 → 1) | Tuğla |
| Lif (Palmiye Koyu palmiyeleri) | Halat Atölyesi (3 → 1) | Halat |
| Lif | Dokuma Tezgahı (4 → 1) | Yelken Bezi |
| Meyve (Meyve Bahçesi) | — | (erzak) |

### Gemi (5 aşama, her biri bir sonraki hedefi açar)
| Aşama | Malzeme | Bitince açılan kare |
|---|---|---|
| Omurga | kalas | Palmiye Koyu |
| Gövde | kalas + tuğla | Halat Atölyesi |
| Direk | kalas + halat | Dokuma Tezgahı + Lifçi işçi |
| Yelken | yelken bezi + halat | Meyve Bahçesi |
| Erzak | meyve | Suya indirme töreni → "KAÇTIN!" |

Her aşama bitince ekranda "… tamamlandı!" kutlaması ve konfeti. Final töreni: işçiler iskelede toplanır, gemi kızaktan
suya kayar, su sıçrar, havai fişekler patlar, kamera uzaklaşan gemiyi takip eder, ardından kaçış ekranı
(süre, kaptanlık XP'si, "Yeni adalar yakında", "Tekrar oyna"). Miktarlar ve süreler `Data/Ship.asset`'te.

## Görsel Stil Kuralları
- **Ana stil: Quaternius Pirate** (low-poly, tek renk atlası `Atlas_Pirate.png`). Önce her zaman Quaternius'a bakılır.
- **Kenney** paketleri (Pirate, Nature, Survival) **sadece Quaternius'ta olmayan eşyalar** için; renkleri uyumlu olanlar seçilir.
- **Sıcak ama yumuşak renkler:** güneşli sarımsı directional ışık, hafif renkli ortam ışığı, çok hafif bloom ve
  doygunluk (göz yormasın), hafif vignette, sıcak beyaz dengesi. Çimen tek renk değil: doğal tonlarda açık-koyu lekeler.
- **Yollar** cetvel gibi değil: `MapLayout.Paths` köşe noktalarından yumuşatılıp hafifçe kıvrılır (`CurvedPaths`), kenarı yumuşak.
- **Ağaçlar** dik durur (eğri palmiye modeli `ModelSpec.Upright` ile dikleştirilir), her biri rastgele ±8° eğik ve
  biraz farklı boyda; gövde dibi zemine oturur. Havada/gömülü nesne kalmamalı: **Tools > Kayıp Ada > Zemin Kontrolü**.
- **Kaynak noktaları** toplandıkça küçülür, tükenince kütük (kayada kırık taşlar) kalır, yeniden dolarken büyür.
- **Yumuşak gölgeler:** tek kaskad, yumuşak ama ucuz (Low). Küçük süslerin gölgesi kapalı; karakterlerin altında ayrıca yumuşak yuvarlak gölge.
- **Mat malzemeler:** URP **Simple Lit**, parlama yok. Malzemeler **paylaşımlı** (`Art/Materials`), model başına kopya yok.
- **His:** her etkileşimde küçük bir hareket + ses (zıplama, sallanma, kavisli uçuş, konfeti). Harici animasyon paketi yok, `Tween` kullanılır.
- **Yazılar:** font **Paytone One** (Google Fonts, SIL OFL 1.1 — ticari kullanım serbest; `Art/Fonts/PaytoneOne-Regular.ttf`
  + `OFL.txt`; Türkçe harflerin hepsi var). TMP font asset'i `Art/Fonts/PaytoneOne SDF.asset` (komut yoksa üretir).
  Kalın koyu kenar çizgisi + hafif gölge (`Art/Materials/TMP_Outline`). Dünyadaki yazılar (`UiFactory.CreateWorldLabel`)
  kameraya dönük durur ve arkalarında yarı saydam, yuvarlak köşeli koyu kutu vardır (`LabelBackground`).
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
    Upgrades/        Geliştirmeler: tür, yönetici, kamptaki tezgah, alttan açılan panel
    Ship/            Gemi inşası, teslim alanı, suya indirme töreni
    Save/            Kayıt sistemi (JSON, otomatik)
    DevTools/        Test Menüsü (sadece Editor ve geliştirme sürümü)
    UI/              Ekran arayüzü
    Editor/          Editör komutları (oyuna dahil edilmez): kurulum, içe aktarma, görsel/prefab üretimi, sahne
  Shaders/           Water (dalgalı su), UnlitTransparent (yumuşak gölge, yer işaretleri, partiküller), BuildReveal (gemi aşağıdan yukarı belirir)
  Tests/             PlayMode testleri (KayipAda.Tests asmdef): temel döngü, geliştirme, lif zinciri, gemi aşamaları, tören, kayıt, Test Menüsü, kütük/yeniden büyüme + ekran görüntüleri
  Data/              Ayar dosyaları (aşağıda)
  Prefabs/           Characters, Enemies, Items, Nodes, Buildings, World (Pazar, açma karesi, kamp), Decor, FX
  Animation/         Character.controller (Idle/Walk/Run karışımı) + karakter başına override
  Art/Generated/     Kodla üretilen: ada zemini modeli ve dokusu, su ağı, kıyı uzaklık dokusu, arayüz dokuları
  Art/Materials/     Paylaşımlı malzemeler (komut üretir)
  Art/Fonts/         Oyun fontu (Paytone One, OFL) + lisansı + TMP font asset'i
  Settings/          URP_Mobile (+ Renderer), PostProcess (bloom, renk, vignette)
  Scenes/            Prototype.unity (sahne kurma komutu üretir — elle düzenleme kaybolur)
  ThirdParty/        Quaternius_Pirate (+ Textures/Atlas_Pirate.png), Kenny_Pirate, Kenny_Nature, Kenny_Survival, Audio
Docs/Modeller.md     Model ve ses seçim tablosu (proje kökünde)
Docs/Denge.md        Fiyat/hız denge tablosu (ilk kaçış ~33 dk)
Docs/GeceRaporu.md   Gece çalışmasının raporu
Docs/Screenshots/    Otomatik ekran görüntüleri (testler üretir)
_BatchCopy/          Batchmode testleri için projenin kopyası (Git'e girmez)
```

### Ayar dosyaları (`Data/`)
```
GameConfig.asset     Genel sayılar: hız, kapasite, kamera, yığın sallanması, efekt, ses, açma hızı...
ResourceTypes/       Wood 5, Stone 8, Fruit 4, Plank 14, Brick 22, Fiber 3, Rope 14, Cloth 20 (ad, renk, fiyat, model prefab'ı)
NodeTypes/           Tree, Rock, FruitBush, FiberPalm (stok, toplama aralığı, yeniden dolma süresi, vuruş sesi)
Areas/               Rocky 150, Orchard 600, Sawmill 250, StoneWorkshop 800, HireLumberjack 400, HireCarrier 600,
                     Cove 500, RopeWorkshop 700, Loom 900, HireFiber 500,
                     ShipStage1-4 (karesi yok: gemi aşaması bitince kod açar; sonraki hedefin karesi bunu bekler)
Buildings/           Sawmill (2 odun → 1 kalas, 2 sn), StoneWorkshop (2 taş → 1 tuğla, 3 sn),
                     RopeWorkshop (3 lif → 1 halat, 3 sn), Loom (4 lif → 1 yelken bezi, 4 sn)
Workers/             Lumberjack (hız 3.2, kapasite 6, sayı 1), Carrier (hız 3.6, kapasite 8, sayı 1), FiberGatherer (Lifçi: 3.2, 6, 1)
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
| `LabelBackground` | Core | Dünya yazısının arkasındaki yarı saydam yuvarlak kutu; yazı değişince boyutlanır |
| `IslandShape` | Core | Adanın organik kıyı şekli (elips + dalgalar). `SignedDistance`, `Contains`, `ClampInside`. Zemin/su/doku da bundan üretilir |
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
| `WorkerMover` / `WorkerSpawner` | Workers | XZ'de yol bularak yürüme; görev tutulunca gizli örnekten işçi çıkarma (zıplayarak) |
| `NavObstacle` / `Pathfinder` | Navigation | XZ dikdörtgen engeller (bina gövdesi, ağaç gövdesi, çadır, tezgah); köşelerden en kısa yol, denize düşen köşeler atlanır |
| `VirtualJoystick` / `CurrencyUI` / `SafeArea` | UI | Joystick; üstteki para paneli (sayarak artar, artışta zıplar); çentikli ekran güvenli alanı |
| `EscapePanel` | UI | Kaçış ekranı: süre, kaptanlık XP'si, "Yeni adalar yakında", "Tekrar oyna" |
| `ProgressionGuide` | Core | Sonraki hedef: en ucuz açılabilir karenin üstünde zıplayan ok + halkası nabız atar; ekran dışındaysa kenarda ok. Kare kalmadıysa gemi |
| `ScreenshotUtil` | Core | Test/kontrol için 1080x1920 PNG ekran görüntüsü (arayüz dahil) |
| `UpgradeType` / `UpgradeManager` | Upgrades | Geliştirme ayarı; seviyeleri tek yerde tutar. `Multiplier(stat)` (hızlar), `Extra(stat, temel)` (kapasite), kaptanlık bonusu dahil |
| `UpgradeStation` / `UpgradePanel` / `UpgradeRow` | Upgrades | Kamptaki tezgah; üstüne gelince alttan panel açılır (açıkken oyuncu yürümez, `PlayerController.InputBlocked`); satırlar seviye noktaları, fiyat, "MAKS" |
| `ShipConfig` | Ship | ScriptableObject: 5 aşama (malzeme, `UnlockOnComplete` kilidi, kutlama yazısı), tören süreleri |
| `ShipBuilder` / `ShipDelivery` | Ship | Kızaktaki gemi inşası (malzeme geldikçe aşağıdan yukarı belirir, erzakta sandıklar çıkar, 3D "Gövde (2/5) Kalas: 12/40" göstergesi); aşama bitince kilit açar + kutlama; teslim alanı (oyuncu + Taşıyıcı işçiler) |
| `DeliveryMarker` | Ship | Gemi teslim alanında zıplayan ok + nabız atan yer işareti (yerde gemiyi gösteren oklar da var) |
| `DevCommands` / `DevMenu` | DevTools | Test Menüsü komutları ve sol üstteki "TEST" butonlu oyun içi menü. **Sadece Editor ve Development Build**; satış sürümünde menü kendini siler, komutlar çalışmaz |
| `LaunchCeremony` | Ship | Final töreni: işçileri toplar (`Worker.Gather`), gemiyi kızaktan suya kaydırır, sıçrama + havai fişek, kamera gemiyi takip eder, kaçış ekranı |
| `CelebrationBanner` | UI | Ekranın ortasında kısa kutlama yazısı ("Gövde tamamlandı!" + açılan yeni hedef) |
| `SaveData` / `SaveManager` | Save | JSON kayıt (persistentDataPath/kayipada_save.json): para, alanlar, geliştirmeler, gemi; 10 sn'de bir, arka planda ve kapanırken; kaptanlık XP kalıcı (seviye başına %3, en fazla 10) |

### Editör komutları (`Scripts/Editor`)
| Dosya | Görevi |
|---|---|
| `PrototypeSceneBuilder` | **Tools > Kayıp Ada > Prototip Sahnesini Kur**: hepsini sırayla çalıştırır ve sahneyi kurar |
| `ProjectSetup3D` | URP_Mobile oluşturur/ayarlar (MSAA 2x, yumuşak ucuz gölge, HDR kapalı, ek ışık yok), Graphics + tüm Quality'ye atar, Linear renk, TMP kaynakları |
| `ThirdPartyImportProcessor` | ThirdParty içe aktarma ayarları: kamera/ışık yok, karakterlerde Generic rig + animasyonlar ayrılır ve döngüye alınır, renk dokuları keskin, sesler mono. Ayar değişince `Version` artırılır |
| `ModelCatalog` | **Hangi nesne → hangi model + hedef boyut** (tek yer). Modeli değiştirmek için burası |
| `PrefabFactory` | Prefab üretimi: model ölçeklenir, tabanı yere oturur, malzemeler paylaşımlı olanlarla değişir; karakter, kaynak, nokta, bina, Pazar, açma karesi, kamp, süs, efekt |
| `CharacterAnimation` | Animator Controller (Speed ile Idle/Walk/Run) + karakter başına override |
| `ArtGenerator` | Ada zemini modeli/dokusu (kum, çimen, yollar, bölge tonları), su ağı, kıyı uzaklık dokusu, arayüz dokuları, malzemeler |
| `MapLayout` | Harita yerleşimi (konumlar, bölgeler, yollar) |
| `UiFactory` / `BuilderUtil` / `DocsWriter` | TMP/arayüz yardımcıları; alan atama yardımcıları; Docs/Modeller.md yazıcısı |
| `UiBuilder` | Geliştirme paneli, aşama kutlama yazısı, kaçış ekranı |
| `GameMenus` | **Tools > Kayıp Ada > Hızlı Test Modu** (aç/kapat, fiyatlar %8, malzeme %20) ve **Kaydı Sil** |
| `DevMenus` | **Tools > Kayıp Ada > Test > …**: Test Menüsü'nün aynı komutları (sadece Play modunda) |
| `GroundCheck` | **Tools > Kayıp Ada > Zemin Kontrolü**: havada duran / gömülü nesneleri Console'a yazar (BuildScene de çalıştırır) |
| `BatchTools` | Komut satırı: `BuildScene` (sahneyi soru sormadan kurar), `CaptureEditShots` |

### Otomatik test (batchmode)
- Unity açıkken aynı proje batchmode'da açılamaz; testler `_BatchCopy` kopyasında koşar (kaynaklar oraya eşitlenir).
- Sahne: `Unity.exe -batchmode -projectPath _BatchCopy -executeMethod KayipAda.EditorTools.BatchTools.BuildScene -quit`
- Testler: `Unity.exe -batchmode -projectPath _BatchCopy -runTests -testPlatform PlayMode -testResults sonuc.xml -shotsDir Docs\Screenshots -shotPrefix <ad>`
  (grafikle çalıştırılır, `-nographics` YOK; ekran görüntüleri `Docs/Screenshots/<ad>_*.png`).
- Testler `SaveManager.Enabled = false` ile kayıt okumadan/yazmadan temiz başlar.

## Test Menüsü (geliştirici araçları)
- Oyun içinde sol üstte küçük **TEST** butonu (sadece Editor ve Development Build). Aynı komutlar
  **Tools > Kayıp Ada > Test** menüsünde. Komutlar: Sınırsız Para, +1000 Para, Tüm Bölgeleri Aç, Tüm Binaları Aç,
  Her İşçiden 1 Tane, Sırttaki Yığını Seçili Malzemeyle Doldur, Gemi Aşamasını Tamamla, Final Törenine Atla,
  Oyun Hızı x1/x2/x5, Kaydı Sil ve Yeniden Başlat. Sayılar `GameConfig` > "Test Menüsü".
- Satış sürümü: Build Settings'te **Development Build kapalı** olmalı; o zaman menü hiç görünmez.

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
                [iskele]   GEMİ (kızakta, pruva denize)    z ≈ 22-26
        ┌──── KAYALIK ────┐   yol   ┌─ MEYVE BAH. ─┐  DOKUMA      z 6..15 (çitli, başta kapalı)
        │ kaya  TAŞ ATÖLY.│    |    │ ağaç   ağaç  │  HALAT
        └──────[kare]─────┘    |    └────[kare]────┘             z 4.4
               palmiye    palmiye    palmiye    [PALMİYE KOYU]   z -6..2 (lif palmiyeleri)
  KAMP  [oduncu kare]      oyuncu      [taşıyıcı kare]           z -6
            PAZAR                    BIÇKIHANE                   z -12
```
- Ada: merkez (0, 3), yarıçap (20, 24) + kıyı dalgaları. Kenarda kum şeridi (~3.4 m), içi çimen, yollar açık toprak.
- Kareler: Kayalık 150, Bıçkıhane 250 (baştan); Taş Atölyesi 800 (Kayalık sonrası); Oduncu 400, Taşıyıcı 600 (sırayla);
  Palmiye Koyu 500 (Omurga sonrası), Halat Atölyesi 700 (Gövde sonrası), Dokuma Tezgahı 900 + Lifçi 500 (Direk sonrası),
  Meyve Bahçesi 600 (Yelken sonrası).
- Gemi iskelenin doğusunda kumsalda, kızağın üstünde; gemi karadayken `NavObstacle` ile içinden geçilmez, törende kapanır.
- Bina düzeni (kök = binanın önü): gövde arkada (z +0.9), giriş solda-önde, çıkış sağda-önde (z -1.5), halka üstte.

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
