# Kayıp Ada — Oyun Tasarımı ve Kod Kuralları

> **Yön: 3D low-poly** (2D kutulardan geçildi; 2D son hali `main` dalında "2D son durum" kaydında).
> **Şu anki durum (gece çalışması sonrası):** Adım 1, 2, 3 ve 4'ün kayıt kısmı yapıldı; otomatik testler ve ekran
> görüntüleriyle doğrulandı, kullanıcının elle denemesi bekleniyor (iş `3d` dalında). Ayrıntı: `Docs/GeceRaporu.md`.

## Proje
- **Motor:** Unity 6 (6000.6), **URP 3D** (Universal Renderer, mobil ayarlı `Assets/_Game/Settings/URP_Mobile`)
- **Platform:** Android, **dikey (portrait)** ekran, referans çözünürlük 1080x1920, hedef **60 FPS**
- **Tür:** Idle arcade (kaynak topla → taşı → sat → geliştir). Hedef: popüler idle arcade mobil oyunlar kadar
  güzel görünen, akıcı, efektli ve sesli bir oyun.
- **Görünüm:** 3D, yukarıdan ~50° eğik perspektif kamera (FOV ~32, dar açı), oyun **XZ düzleminde** (y = yukarı).
- **Girdi:** Yalnızca **yeni Input System** etkin (`activeInputHandler: 1`). Eski `Input.GetAxis` vb. KULLANILMAZ.
  Dokunmatik ve fare, uGUI sanal joystick ile (EventSystem + `InputSystemUIInputModule`) çalışır;
  editörde test için WASD / ok tuşları da çalışır. Joystick yukarısı = kameranın baktığı yön (+Z).
- **Kullanıcı:** Unity'ye yeni başlıyor. Açıklamalar Türkçe, adım adım ve hangi menüye tıklanacağını söyleyerek yapılır.

## Oyun Tasarımı
Oyuncu bir adaya düşmüş bir kazazede kaptandır. Amaç: adadan kaçmak için gemi inşa etmek.

### Temel döngü
1. Oyuncu kaynak noktasına (palmiye, kaya, meyve ağacı) yaklaşır → kaynak kavis çizerek önündeki yığına uçar.
2. Yığının kapasitesi sınırlıdır.
3. Pazara girince yığın tek tek tezgaha uçar ve satılır → para zıplar, "+5" yazısı çıkar.
4. Para ile yeni bölgeler, binalar, işçiler ve geliştirmeler açılır.
5. Son hedef: gemiyi inşa edip son geceyi atlatarak adadan kaçmak.

## Görsel Stil Kuralları
- **Ana stil: Quaternius Pirate** (low-poly, tek renk atlası `Atlas_Pirate.png`). Önce her zaman Quaternius'a bakılır.
- **Kenney** paketleri (Pirate, Nature, Survival) **sadece Quaternius'ta olmayan eşyalar** için; renkleri uyumlu olanlar seçilir.
- **Sıcak renkler:** güneşli sarımsı directional ışık, hafif renkli (mavi gök / sıcak yan / kumlu yer) ortam ışığı,
  hafif bloom, hafif doygun renk düzenleme, hafif vignette, sıcak beyaz dengesi.
- **Yumuşak gölgeler:** tek kaskad, yumuşak ama ucuz (Low). Küçük süslerin gölgesi kapalı; karakterlerin altında ayrıca yumuşak yuvarlak gölge.
- **Mat malzemeler:** URP **Simple Lit**, parlama yok. Malzemeler **paylaşımlı** (`Art/Materials`), model başına kopya yok.
- **His:** her etkileşimde küçük bir hareket + ses (zıplama, sallanma, kavisli uçuş, konfeti). Harici animasyon paketi yok, `Tween` kullanılır.
- **Yazılar:** TextMeshPro, kalın, kenar çizgili + hafif gölgeli (`Art/Materials/TMP_Outline`), büyük ve okunaklı.
- Hangi nesne için hangi model seçildiği: **`Docs/Modeller.md`** (komut her çalıştığında yeniden yazılır).

## Plan (6 adım)
Her adım Unity'de test edilir, çalışıyorsa Git'e kaydedilir. Bir adım bitmeden sonrakine geçilmez.
Her adım sonunda bu dosya, sahne kurma komutu ve `Docs/Modeller.md` güncellenir.

| # | Adım | Bitti sayılır, eğer... | Durum |
|---|---|---|---|
| 1 | 3D + vitrin sahili | Başlangıç sahili 3D, Quaternius modelleri, animasyonlu karakterler, su, ışık, efektler ve seslerle "vitrin" kalitesinde; 2D'deki tüm oynanış çalışıyor | ✅ (otomatik testlerle) |
| 2 | Tüm ada aynı kalitede | Bütün bölgeler aynı kalitede; geliştirme tezgahı (5 geliştirme × 5 seviye); sonraki hedefi gösteren ok; denge tablosu | ✅ (otomatik testlerle) |
| 3 | Gemi + son savunma + kaçış | 4 aşamalı gemi inşası, son gece (3 dalga iskelet), toplar, "Tekrar dene", kaçış ekranı, Hızlı Test Modu | ✅ (otomatik testlerle) |
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
    Ship/            Gemi inşası, teslim alanı, gündüz/gece, son gece savunması, düşman, top
    Save/            Kayıt sistemi (JSON, otomatik)
    UI/              Ekran arayüzü
    Editor/          Editör komutları (oyuna dahil edilmez): kurulum, içe aktarma, görsel/prefab üretimi, sahne
  Shaders/           Water (dalgalı su), UnlitTransparent (yumuşak gölge, yer işaretleri, partiküller), BuildReveal (gemi aşağıdan yukarı belirir)
  Tests/             PlayMode testleri (KayipAda.Tests asmdef): temel döngü, geliştirme, gemi, gece, kaçış + ekran görüntüleri
  Data/              Ayar dosyaları (aşağıda)
  Prefabs/           Characters, Enemies, Items, Nodes, Buildings, World (Pazar, açma karesi, kamp), Decor, FX
  Animation/         Character.controller (Idle/Walk/Run karışımı) + karakter başına override
  Art/Generated/     Kodla üretilen: ada zemini modeli ve dokusu, su ağı, kıyı uzaklık dokusu, arayüz dokuları
  Art/Materials/     Paylaşımlı malzemeler (komut üretir)
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
ResourceTypes/       Wood 5, Stone 8, Fruit 4, Plank 14, Brick 22 (ad, renk, fiyat, model prefab'ı)
NodeTypes/           Tree, Rock, FruitBush (stok, toplama aralığı, yeniden dolma süresi)
Areas/               Rocky 150, Orchard 600, Sawmill 250, StoneWorkshop 800, HireLumberjack 400, HireCarrier 600,
                     ShipKeel (gemi omurgası bitince açılır), Cannon1-3 400/550/700 (Docs/Denge.md)
Buildings/           Sawmill (2 odun → 1 kalas, 2 sn), StoneWorkshop (2 taş → 1 tuğla, 3 sn)
Workers/             Lumberjack (hız 3.2, kapasite 6, sayı 1), Carrier (hız 3.6, kapasite 8, sayı 1)
Upgrades/            PlayerCapacity, PlayerSpeed, WorkerSpeed, WorkerCapacity, BuildingSpeed (5 seviye fiyat + etki)
Ship.asset           Gemi aşamaları ve malzemeleri, son gece dalgaları, düşman, gemi canı, toplar, gece ışığı, kaçış
SoundLibrary.asset   Hangi olayda hangi ses (Docs/Modeller.md'de liste; komut sadece eksik sesleri ekler)
```

> Not: Kaynak scriptleri klasörünün adı bilerek `ResourceSystem`'dır, `Resources` değil. Unity'de
> `Resources` adlı her klasör özel anlam taşır (içindeki her şey oyuna gömülür), karışıklık olmasın.

## Sınıfların Görevleri
| Sınıf | Klasör | Görevi |
|---|---|---|
| `GameConfig` | Core | ScriptableObject. Oyuncu hızı/kapasitesi, satış, açma hızı, kamera (açı, FOV, uzaklık), joystick, yığın sallanması, efekt ve ses sayıları |
| `CurrencyManager` | Core | Parayı tek yerde tutar (`Coins`), `Add` / `TrySpend`, `CoinsChanged` olayı. Singleton (-100) |
| `CameraFollow` | Core | ~50° eğik, dar açılı perspektif kamera; oyuncuyu yumuşak takip eder, oyuncunun biraz önüne bakar |
| `Tween` / `TweenRunner` | Core | Basit animasyon yardımcısı: `PunchScale`, `Shake`, `PopIn`, `PopOut`, `MoveArc`, `Value`. Runner sahneye kendini ekler |
| `Effects` | Core | Efektlerin tek giriş noktası: `Confetti`, `CoinPop` (+ "+5" yazısı), `FlyCoin`, `ShowText`. Havuz kullanır |
| `FloatingText` | Core | Yükselip kaybolan dünya yazısı |
| `ProgressRing` | Core | Dairesel dolan halka (bina üstü, açma karesi kenarı) |
| `Billboard` | Core | Nesneyi kameraya döndürür (yazılar, halkalar) |
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
| `ResourceNode` | ResourceSystem | Kaynak noktası: her toplamada sallanır + sıkışıp genişler; tükenince küçülür. `GetStandPoint` (işçi gövdeye girmesin) |
| `InteractionZone` | Zones | 3D trigger alanların temeli; içerideki **tüm** `CarryStack`'ler `Visitors`'ta. `CanServe`: oyuncuya her alan, işçiye sadece hedefi |
| `Market` | Zones | Pazar: parça tezgaha uçar, varınca para + zıplayan para + "+5" + ses |
| `UnlockPad` | Zones | Yerde yuvarlak açma karesi: dolan altın halka, büyük fiyat, paralar oyuncudan kareye uçar; bitince konfeti + ses, kare küçülerek kaybolur; `requiredArea` açılınca zıplayarak belirir |
| `AreaDefinition` / `AreaManager` | Areas | Bölge/bina/işçi tutma tanımı ve hangilerinin açık olduğu (kayıt sistemi bunu kullanacak) |
| `UnlockableArea` | Areas | Bölge/bina kökü: açılınca çit küçülerek kaybolur, içerik sırayla zıplayarak belirir |
| `BuildingType` / `ProcessingBuilding` | Buildings | Tarif; bina: ham madde binaya uçar, ürün çıkışa uçar, üstte halka, çalışırken duman/talaş, ürün çıkınca zıplar |
| `BuildingInput` / `BuildingOutput` | Buildings | Giriş: sırttan sadece kabul edilen türü alır. Çıkış: ürünü sırta verir |
| `WorkerType` / `Worker` / `LumberjackWorker` / `CarrierWorker` | Workers | İşçi ayarları; ortak temel (`GoTo`, `Wait`); Oduncu ve Taşıyıcı durum makineleri |
| `WorkerMover` / `WorkerSpawner` | Workers | XZ'de yol bularak yürüme; görev tutulunca gizli örnekten işçi çıkarma (zıplayarak) |
| `NavObstacle` / `Pathfinder` | Navigation | XZ dikdörtgen engeller (bina gövdesi, ağaç gövdesi, çadır, tezgah); köşelerden en kısa yol, denize düşen köşeler atlanır |
| `VirtualJoystick` / `CurrencyUI` / `SafeArea` | UI | Joystick; üstteki para paneli (sayarak artar, artışta zıplar); çentikli ekran güvenli alanı |
| `EscapePanel` | UI | Kaçış ekranı: süre, kaptanlık XP'si, "Yeni adalar yakında", "Tekrar oyna" |
| `ProgressionGuide` | Core | Sonraki hedef: en ucuz açılabilir karenin üstünde zıplayan ok + halkası nabız atar; ekran dışındaysa kenarda ok. Kare kalmadıysa gemi |
| `ScreenshotUtil` | Core | Test/kontrol için 1080x1920 PNG ekran görüntüsü (arayüz dahil) |
| `UpgradeType` / `UpgradeManager` | Upgrades | Geliştirme ayarı; seviyeleri tek yerde tutar. `Multiplier(stat)` (hızlar), `Extra(stat, temel)` (kapasite), kaptanlık bonusu dahil |
| `UpgradeStation` / `UpgradePanel` / `UpgradeRow` | Upgrades | Kamptaki tezgah; üstüne gelince alttan panel açılır (açıkken oyuncu yürümez, `PlayerController.InputBlocked`); satırlar seviye noktaları, fiyat, "MAKS" |
| `ShipConfig` | Ship | ScriptableObject: aşamalar/malzemeler, dalgalar, düşman, gemi canı, toplar, gece rengi, kaçış |
| `ShipBuilder` / `ShipDelivery` | Ship | Gemi inşası (malzeme geldikçe aşağıdan yukarı belirir, erzakta sandıklar çıkar, 3D "Gövde: 12/30" göstergesi); teslim alanı (oyuncu + Taşıyıcı işçiler) |
| `DayNightController` | Ship | Gündüz ↔ gece ışık/ortam/sis geçişi |
| `NightDefense` | Ship | Son gece: 3 dalga iskelet, gemi canı, kaybedince "Tekrar dene" (ilerleme silinmez), kazanınca gün doğar, gemi yelken açar, kaçış ekranı |
| `Enemy` | Ship | İskelet: gemiye yürür, saldırır, gülleyle ölür (animasyonlu) |
| `Cannon` / `CannonLoader` | Ship | Top: en yakın düşmana kavisli gülle, patlama; oyuncu taş getirip doldurur, üstte "Gülle 3/10" |
| `SaveData` / `SaveManager` | Save | JSON kayıt (persistentDataPath/kayipada_save.json): 10 sn'de bir, arka planda ve kapanırken; kaptanlık XP kalıcı (seviye başına %3, en fazla 10) |

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
| `UiBuilder` | Geliştirme paneli, gece arayüzü (can çubuğu, duyuru, tekrar dene), kaçış ekranı |
| `GameMenus` | **Tools > Kayıp Ada > Hızlı Test Modu** (aç/kapat, fiyatlar %8, malzeme/düşman %20) ve **Kaydı Sil** |
| `BatchTools` | Komut satırı: `BuildScene` (sahneyi soru sormadan kurar), `CaptureEditShots` |

### Otomatik test (batchmode)
- Unity açıkken aynı proje batchmode'da açılamaz; testler `_BatchCopy` kopyasında koşar (kaynaklar oraya eşitlenir).
- Sahne: `Unity.exe -batchmode -projectPath _BatchCopy -executeMethod KayipAda.EditorTools.BatchTools.BuildScene -quit`
- Testler: `Unity.exe -batchmode -projectPath _BatchCopy -runTests -testPlatform PlayMode -testResults sonuc.xml -shotsDir Docs\Screenshots -shotPrefix <ad>`
  (grafikle çalıştırılır, `-nographics` YOK; ekran görüntüleri `Docs/Screenshots/<ad>_*.png`).
- Testler `SaveManager.Enabled = false` ile kayıt okumadan/yazmadan temiz başlar.

## Sahne Kurma Komutu
Sahne elle kurulmaz. **Tools > Kayıp Ada > Prototip Sahnesini Kur**:
- İlk çalıştırmada TextMeshPro kaynaklarını ekler; içe aktarma bitince kurulum **kendiliğinden devam eder**.
- Sahneyi her seferinde **sıfırdan** kurar ve `Assets/_Game/Scenes/Prototype.unity` üzerine kaydeder.
- Ayar dosyaları (`Data/`, `Settings/PostProcess`) **sadece yoksa** oluşturulur; kullanıcının değiştirdiği değerler korunur.
  (Builder'daki başlangıç değerini değiştirmek mevcut dosyayı değiştirmez; Inspector kullanılır ya da dosya silinir.)
- Prefab'lar, üretilen görseller, malzemeler ve animasyon override'ları her seferinde yeniden üretilir (GUID korunur).
- Sahneye yeni bir nesne eklenecekse **her adımda** önce bu komut güncellenir.

### Harita (Adım 1) — `MapLayout`
```
                    ~~~~~~~~ deniz ~~~~~~~~
                         [iskele]   (batık gemi)      z ≈ 25+
        ┌──── KAYALIK ────┐   yol   ┌─ MEYVE BAHÇESİ ─┐
        │ kaya  TAŞ ATÖLY.│    |    │ ağaç     ağaç   │   z 6..15 (çitli, başta kapalı)
        │ kaya       kaya │    |    │      ağaç       │
        └──────[kare]─────┘    |    └─────[kare]──────┘   z 4.4
               palmiye    palmiye    palmiye             z -1..2  BAŞLANGIÇ SAHİLİ
  KAMP  [oduncu kare]      oyuncu      [taşıyıcı kare]    z -6
            PAZAR                    BIÇKIHANE           z -12
```
- Ada: merkez (0, 3), yarıçap (17, 22) + kıyı dalgaları. Kenarda kum şeridi (~3.4 m), içi çimen, yollar açık toprak.
- Kareler: Kayalık 60 (baştan), Meyve Bahçesi 150 (Kayalık açılınca), Bıçkıhane 80 (baştan, binanın yerinde),
  Taş Atölyesi 200 (Kayalık açılınca), Oduncu 120 (Bıçkıhane açılınca), Taşıyıcı 180 (Oduncu tutulunca).
- Bina düzeni (kök = binanın önü): gövde arkada (z +0.9), giriş solda-önde, çıkış sağda-önde (z -1.5), halka üstte.
  Giriş ile çıkış arası boşluk oyuncunun çapından büyük.

### Yerleşim kuralları
- Joystick tüm ekranda; para paneli üstte ortada, güvenli alanın (SafeArea) içinde. Yeni arayüz parçaları ekranın kenarlarına.
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
