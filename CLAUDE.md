# Kayıp Ada — Oyun Tasarımı ve Kod Kuralları

> **Şu anki aşama: Aşama 4 — İşçiler** (yapıldı, test bekliyor). Sıradaki: Aşama 5 — Geliştirmeler.

## Proje
- **Motor:** Unity 6 (6000.6), Universal Render Pipeline 2D şablonu
- **Platform:** Android, **dikey (portrait)** ekran, referans çözünürlük 1080x1920
- **Tür:** Idle arcade (kaynak topla → taşı → sat → geliştir)
- **Görünüm:** Yukarıdan (top-down), 2D. Görseller Aşama 10'a kadar renkli kutu.
- **Girdi:** Yalnızca **yeni Input System** etkin (`activeInputHandler: 1`). Eski `Input.GetAxis` vb. KULLANILMAZ.
  Dokunmatik ve fare, uGUI sanal joystick ile (EventSystem + `InputSystemUIInputModule`) çalışır;
  editörde test için WASD / ok tuşları da çalışır.
- **Kullanıcı:** Unity'ye yeni başlıyor. Açıklamalar Türkçe, adım adım ve hangi menüye tıklanacağını söyleyerek yapılır.

## Oyun Tasarımı
Oyuncu bir adaya düşmüş bir kazazededir. Amaç: adadan kaçmak için gemi inşa etmek.

### Temel döngü
1. Oyuncu kaynak noktasına (ağaç, kaya, çalı) yaklaşır → kaynak otomatik olarak sırtına eklenir.
2. Sırttaki yığının kapasitesi sınırlıdır.
3. Pazara (satış alanı) girince yığın tek tek satılır → para kazanılır.
4. Para ile yeni bölgeler, binalar, işçiler ve geliştirmeler açılır.
5. Son hedef: gemiyi inşa edip son geceyi atlatarak adadan kaçmak.

## Aşama Listesi
Her aşama tek istemle yapılır, Unity'de test edilir, çalışıyorsa Git'e kaydedilir. Bir aşama bitmeden sonrakine geçilmez.
Her aşama sonunda bu dosya ve sahne kurma komutu güncellenir.

| # | Aşama | Bitti sayılır, eğer... | Durum |
|---|---|---|---|
| 0 | Temel düzen | Prototip aynı çalışıyor ama kod düzenli, sayılar ayar dosyasında, kamera oyuncuyu takip ediyor | ✅ |
| 1 | Çoklu kaynaklar | Odun, taş ve meyve toplanıp satılabiliyor | ✅ |
| 2 | Açılabilir alanlar | Parayla yeni bölge açılıyor (UnlockPad) | ✅ |
| 3 | İşleme binaları | Odun kalasa, taş tuğlaya dönüşüyor | ✅ |
| 4 | İşçiler | İşçiler sen dokunmadan kaynak taşıyor | 🔄 Test bekliyor |
| 5 | Geliştirmeler | Kapasite, hız ve işçi sayısı artırılabiliyor | ⬜ |
| 6 | Gemi inşası | 4 aşamalı gemi bitince kaçış ekranı geliyor | ⬜ |
| 7 | Son savunma | Gemi suya inmeden yaratık dalgası püskürtülüyor | ⬜ |
| 8 | Kayıt ve menü | Oyun kapanıp açılınca kaldığı yerden devam ediyor, kaptanlık seviyesi kalıcı | ⬜ |
| 9 | Telefon testi | APK telefonda akıcı çalışıyor | ⬜ |
| 10 | Görseller | Kutuların yerinde gerçek görseller var | ⬜ |
| 11 | Efekt ve ses | Toplama, satma, açma anlarında animasyon, ses, titreşim var | ⬜ |
| 12 | Denge ve rehber | Yeni biri rehberle 30-45 dakikada bitirebiliyor | ⬜ |
| 13 | Reklam | Ödüllü reklam test modunda çalışıyor | ⬜ |
| 14 | Yayın | 12 testçiyle 14 gün tamam, oyun Play'de | ⬜ |

## Klasör Düzeni
Oyunun tüm dosyaları `Assets/_Game/` altındadır. `Assets/` altındaki diğer klasörler Unity/paketlere aittir:
`Assets/Settings` (URP render ayarları, Input System ayarları — dokunma) ve `Assets/Resources/BillingMode.json`
(satın alma paketinin dosyası — dokunma).

```
Assets/_Game/
  Scripts/
    Core/            Oyun geneli sistemler ve ayarlar
    Player/          Sadece oyuncuya özel davranışlar
    ResourceSystem/  Kaynak türleri, kaynak parçaları, taşıma yığını, kaynak noktaları
    Zones/           Üzerine girilince çalışan alanlar (Pazar, açma kareleri, ileride bina girişleri)
    Areas/           Açılabilir bölgeler: tanım, hangi bölgenin açık olduğunu tutan yönetici, sahnedeki bölge kökü
    Buildings/       İşleme binaları: bina türü, genel bina, giriş ve çıkış alanları
    Workers/         İşçiler: işçi türü, ortak işçi temeli, Oduncu, Taşıyıcı, yürüme, kamptan çıkarma
    Navigation/      İşçilerin engellerin etrafından dolaşması: engel ve yol bulucu
    UI/              Ekran arayüzü
    Editor/          Editör menü komutları (oyuna dahil edilmez)
  Data/
    GameConfig.asset           Tüm genel ayar sayıları (bölge açma hızı, bina aktarma hızı dahil)
    ResourceTypes/             Kaynak türleri: Wood 5, Stone 8, Fruit 4, Plank 14, Brick 22 (ad, renk, satış fiyatı)
    NodeTypes/                 Kaynak noktası türleri: Tree, Rock, FruitBush (stok, toplama aralığı, yeniden dolma süresi)
    Areas/                     Açılabilir alanlar: Rocky 60, Orchard 150, Sawmill 80, StoneWorkshop 200,
                               işçi tutma: HireLumberjack 120, HireCarrier 180 (kimlik, ad, fiyat)
    Buildings/                 İşleme binası türleri: Sawmill (2 odun → 1 kalas, 2 sn), StoneWorkshop (2 taş → 1 tuğla, 3 sn)
    Workers/                   İşçi türleri: Lumberjack (hız 3, kapasite 6, sayı 1), Carrier (hız 3.5, kapasite 8, sayı 1)
  Prefabs/           ResourceItem.prefab (tüm kaynak parçaları için tek prefab), Coin.prefab (açma karesine uçan para)
  Art/               WhiteSquare.png (tüm kutuların kullandığı beyaz kare; Aşama 10'da gerçek görseller)
  Scenes/            Prototype.unity (sahne kurma komutu üretir — elle düzenleme kaybolur)
```

> Not: Kaynak scriptleri klasörünün adı bilerek `ResourceSystem`'dır, `Resources` değil. Unity'de
> `Resources` adlı her klasör özel anlam taşır (içindeki her şey oyuna gömülür), karışıklık olmasın.

## Sınıfların Görevleri
| Sınıf | Klasör | Görevi |
|---|---|---|
| `GameConfig` | Core | ScriptableObject. Oyuncu hızı/kapasitesi, satma aralığı, bölge açma hızı, işçi çıkış aralığı, kamera, joystick, efekt sayıları |
| `SortOrder` | Core | Sprite çizim sıraları (sabitler) |
| `CurrencyManager` | Core | Parayı tek yerde tutar (`Coins`), `Add` / `TrySpend`, değişince `CoinsChanged` olayı. Singleton, erken çalışır (-100) |
| `CameraFollow` | Core | Kamera oyuncuyu yumuşakça takip eder; oyuncu ekranın biraz altında kalır (`CameraOffsetY`). Kamera görüntüsünü üst arayüz çubuğunun **altında** bitirir (`cam.rect`), böylece arayüz hiçbir nesneyi kapatmaz |
| `FlyingVisual` | Core | Bir noktadan diğerine uçup kaybolan basit görsel (açma karesine akan para) |
| `ProgressBar` | Core | Dünyada duran ilerleme çubuğu (arka plan + soldan dolan kutu), `SetProgress(0..1)` |
| `PlayerController` | Player | Joystick/klavye ile hareket, yürünebilir alan (`moveArea`, builder adaya göre ayarlar), oyuncu ayarlarını (kapasite) `CarryStack`'e uygular |
| `ResourceType` | ResourceSystem | ScriptableObject. Kimlik (`Id`), görünen ad, renk, satış fiyatı |
| `ResourceItem` | ResourceSystem | Tek bir kaynak parçası; `Setup(type)` ile türü/rengi atanır, satılınca uçup kaybolur |
| `CarryStack` | ResourceSystem | Karakterin (oyuncu ve işçiler) sırtındaki karışık türlü yığın: `TryAdd`, `RemoveTop()`, `RemoveTop(type)`, `CountOf(type)`. `IsPlayer` (oyuncu mu), `WorkTarget` (işçinin o an iş yaptığı alan) |
| `ItemPile` | ResourceSystem | Yerde duran kaynak yığını (bina giriş/çıkışı); sütunlar halinde dizer. `TryAdd`, `RemoveTop`, `Capacity` sahibi belirler |
| `ResourceNodeType` | ResourceSystem | ScriptableObject. Nokta türü: verdiği kaynak, stok, toplama aralığı, yeniden dolma süresi |
| `ResourceNode` | ResourceSystem | Kaynak noktası (ağaç, kaya, çalı — hepsi aynı bileşen). İçerideki karaktere aralıklarla kaynak verir; stok bitince küçülüp solar, süre dolunca yeniden dolar. Görünümü `Visual` alt nesnesindedir |
| `InteractionZone` | Zones | Trigger alanların ortak temeli; içerideki **tüm** `CarryStack`'leri `Visitors` listesinde tutar. `CanServe`: oyuncuya her alan, işçiye sadece hedefi (`WorkTarget`) olan alan hizmet eder — yoldan geçen işçi başka alana takılmaz. Alanlar her aralıkta içerideki herkese birer parça verir/alır |
| `Market` | Zones | Pazar; tüm türleri kabul eder, yığından tek tek alır, her parçayı kendi `SellPrice`'ından satar |
| `UnlockPad` | Zones | Açma karesi (bölge, bina ve işçi tutma için). Sadece oyuncu üstündeyken parayı parça parça öder (para görseli uçar), kalan fiyatı yazar. Ödenen kısım `AreaManager`'da kalır. `requiredArea` açılmadan görünmez. Fiyat bitince bölge açılır, kare gizlenir |
| `AreaDefinition` | Areas | ScriptableObject. Bölge kimliği (`Id`, kayıtta kullanılır), görünen ad, fiyat |
| `AreaManager` | Areas | **Hangi bölgelerin açık olduğunu ve yarım ödemeleri tek yerde tutar** (kayıt sistemi bunu kullanacak). `IsUnlocked`, `GetRemaining`, `AddPayment`, `Unlock`, `AreaUnlocked` olayı. Singleton, erken çalışır (-100) |
| `UnlockableArea` | Areas | Sahnedeki bölge/bina kökü. Kapalıyken `Locked` (çit + ad ya da silik zemin) görünür, `Content` gizli; açılınca tersi |
| `BuildingType` | Buildings | ScriptableObject. Girdi türü/adedi, çıktı türü/adedi, işleme süresi, giriş ve çıkış kapasitesi |
| `ProcessingBuilding` | Buildings | Genel işleme binası. Girişte yeterli ham madde + çıkışta yer varsa işler; çıkış dolunca bekler. `InputPile`, `OutputPile`, `HasEnoughInput` |
| `BuildingInput` | Buildings | Giriş alanı: sırttan **sadece binanın kabul ettiği türü** (`CarryStack.RemoveTop(type)`) alır, diğerleri sırtta kalır |
| `BuildingOutput` | Buildings | Çıkış alanı: çıkış yığınındaki ürünleri sırta verir (`Pile`, `Building`) |
| `WorkerType` | Workers | ScriptableObject. İşçi görevi: ad, yürüme hızı, taşıma kapasitesi, **işçi sayısı** (`Count`). Tutma fiyatı `Data/Areas/Hire...`'da |
| `Worker` | Workers | İşçilerin ortak temeli: ayarları her karede uygular, `GoTo(alan)` (hedefi işaretleyip yürür), `Wait()`; durum makinesi alt sınıfta (`Think`) |
| `LumberjackWorker` | Workers | Oduncu: ağaç seç → git → topla → Bıçkıhane girişine git → bırak. Ağaçlar bitmişse elindekini götürür ya da bekler; giriş doluysa girişte bekler |
| `CarrierWorker` | Workers | Taşıyıcı: en dolu açık bina çıkışını seç → git → al → Pazar'a git → sat. Çıkış boşalınca başka çıkışa gider, bina çalışıyorsa bekler, yoksa elindekini satar |
| `WorkerMover` | Workers | İşçinin yürümesi: `MoveTo`, `Stop`, `HasArrived`. Yolu `Pathfinder` ile bulur, engeller değişince yeniden hesaplar. Kinematic gövde: kimseyi itmez |
| `WorkerSpawner` | Workers | Kamptaki çıkış noktası (görev başına bir tane). Görevin açma karesi ödenince gizli hazır işçiyi (`template`) `WorkerType.Count` kadar kopyalayıp kamptan çıkarır |
| `NavObstacle` | Navigation | İşçilerin etrafından dolaştığı dikdörtgen engel (bina gövdesi, çadır). Aktif engeller tek listede; değişince `Version` artar |
| `Pathfinder` | Navigation | Basit yol bulucu: düz yol açıksa düz, değilse engel köşelerinden en kısa yol. Başlangıç/hedef bir engelin içindeyse o engel yok sayılır |
| `VirtualJoystick` | UI | Ekranın neresine dokunulursa orada beliren joystick; `Direction` verir |
| `CurrencyUI` | UI | Üst çubuktaki para göstergesi; `CoinsChanged` olayını dinler |
| `PrototypeSceneBuilder` | Editor | **Tools > Kayıp Ada > Prototip Sahnesini Kur** komutu |

## Sahne Kurma Komutu
Sahne elle kurulmaz. **Tools > Kayıp Ada > Prototip Sahnesini Kur**
(`Assets/_Game/Scripts/Editor/PrototypeSceneBuilder.cs`):
- Her çalıştırmada sahneyi **sıfırdan** kurar ve `Assets/_Game/Scenes/Prototype.unity` üzerine kaydeder.
- Ayar dosyalarını (`Data/` altındakiler) **sadece yoksa** oluşturur; kullanıcının değiştirdiği değerler korunur.
- `ResourceItem.prefab`, `Coin.prefab` ve `WhiteSquare.png` her seferinde yeniden üretilir (içlerinde ayar yok).
- **Dikkat:** Ayar dosyaları sadece yoksa oluşturulduğu için builder'daki başlangıç değerlerini değiştirmek mevcut
  dosyaları değiştirmez. Mevcut bir değeri değiştirmek için Inspector kullanılır (ya da dosya silinip komut yeniden çalıştırılır).
- Sahneye yeni bir nesne eklenecekse **her aşamada** önce bu komut güncellenir.
- Haritanın yerleşimi (ada boyutu, konumlar, bölge dikdörtgenleri, renkler) dosyanın başındaki "Harita yerleşimi"
  bölümündedir; orada ASCII bir harita çizimi de var.

### Harita (Aşama 4)
```
            ~~~~ deniz ~~~~
               [iskele]                y 13..17
   ┌──────── İSKELE ALANI ────────┐    y 10.8..12.8 (gemi için, ileride)
   │ TAŞ ATÖLYESİ  │               │    y 7..10
   │ KAYALIK       │ MEYVE BAHÇESİ │    y 4..10 (başta çitle kapalı)
   │ kaya    kaya  │ çalı    çalı  │
   └───────────────┴──────────────┘
      [kare 1]          [kare 2]        y 2.9
    ağaç      ağaç      ağaç            BAŞLANGIÇ SAHİLİ
 KAMP [oduncu] oyuncu [taşıyıcı]        y -3 (kamp x -5.4..-4; işçi kareleri x ±2.3)
     PAZAR          BIÇKIHANE           y -8..-5
```
- Ada: x -6..6, y -10..13. Oyuncu kenardan 0.5 içeride kalır.
- Başta sadece başlangıç sahili açık (ağaçlar + Pazar). Kareler:
  - Kayalık (60): baştan görünür.
  - Meyve Bahçesi (150): Kayalık açılınca görünür.
  - Bıçkıhane (80): baştan görünür, binanın yerinde durur.
  - Taş Atölyesi (200): kayalığın içinde; Kayalık açılınca görünür.
  - Oduncu (120): Bıçkıhane açılınca görünür. Taşıyıcı (180): Oduncu tutulunca görünür.
- Açılabilir bölge kurulumu: `CreateArea(...)` bölgenin `Content` nesnesini döndürür; bölgeye ait her şey (kaynak
  noktaları, binalar) bu `Content`'in altına kurulur. Açma karesi: `CreateUnlockPad(ad, konum, alan, gereken alan, ...)`.
- İşleme binası kurulumu: `CreateBuilding(ad, ebeveyn, konum, bina türü, alan, renk, ...)`. Bina da bir açılabilir
  alandır (kendi `AreaDefinition`'ı var); karesi `konum + BuildingPadOffset`'te durur. Binanın parçalarının yerleşimi
  `Building...Offset` sabitlerindedir: gövde üstte, ilerleme çubuğu en üstte, giriş solda-altta, çıkış sağda-altta.
  Giriş ile çıkış arasındaki boşluk oyuncunun çapından büyüktür (ikisine birden değmesin).
  Bina gövdesi bir `NavObstacle`'dır (işçiler etrafından dolaşır; oyuncu yine içinden geçebilir).
- İşçiler: `CreateCamp` kampı kurar; `CreateWorkerTemplate<T>` gizli hazır işçiyi kurar (oyuncu gibi Kinematic gövde +
  collider + `CarryStack`, ayrıca `WorkerMover`); görev bağlantıları (ağaçlar, bina girişi/çıkışları, Pazar) builder'da
  `SetField` / `SetArray` ile atanır; `CreateSpawner` hazır işçiyi bir `WorkerSpawner`'ın altına koyup gizler.
  Yeni bir işçi görevi: `XxxWorker : Worker` sınıfı + `WorkerType` dosyası + `Hire...` alanı + kare + spawner.

### Yerleşim kuralları
- Üst arayüz çubuğu kamera görüntüsünün dışındadır, yani hiçbir dünya nesnesini kapatamaz. Yeni arayüz parçaları
  da dünyanın üstüne binmemeli (üst çubuğa ya da ekranın kenarına konmalı).
- Oyun başlarken (kamera y≈-1) görünen nesneler dar (20:9) telefonlarda yandan taşmamalı: x = -3.3..3.3.
- Komşu noktaların trigger alanları çakışmamalı; oyuncunun başlangıç noktası hiçbir alanın içinde olmamalı.

### Yardımcılar
- Kaynak noktaları `CreateNode(ad, ebeveyn, konum, nokta türü, ..., görünüm metodu)` ile kurulur; yeni bir nokta türü için
  sadece `BuildXxxVisual` metodu ve bir `CreateNodeTypeIfMissing` satırı eklenir.
- Özel alanlara değer atamak için `SetField(bileşen, "alanAdı", nesne)` ve `SetArray / SetRect / SetFloat / SetInt / SetVector2`
  yardımcıları kullanılır.
- Erken çalışması gereken yöneticiler `[DefaultExecutionOrder(-100)]` taşır; diğer scriptler Awake'te `Instance`'a güvenebilir.
- Yeni sahne açıldıktan **sonra** varlıklar `AssetDatabase.LoadAssetAtPath` ile yeniden yüklenmelidir
  (yoksa Unity referansları boşaltır ve alanlar `None` kalır).

## Kod Kuralları
- **Basit ve okunaklı** kod. Gereksiz soyutlama, interface ağacı veya framework yok.
- Her dosyanın başında 2-4 satırlık **Türkçe açıklama yorumu** bulunur (dosya ne işe yarar).
- Kod içi isimler (sınıf, değişken, metod) **İngilizce**; yorumlar ve oyuncuya görünen metinler **Türkçe**.
- Tüm oyun kodu `KayipAda` namespace'i içindedir (editör kodu `KayipAda.EditorTools`).
- Bir dosyada bir sınıf, dosya adı = sınıf adı.

### Sayılar ayar dosyasında
- **Oynanışı etkileyen hiçbir sayı (fiyat, hız, kapasite, süre, aralık) kodun içine yazılmaz.**
  Genel sayılar `GameConfig`'e, kaynağa özel sayılar `ResourceType`'a, nokta türüne özel sayılar
  `ResourceNodeType`'a, bölge/bina/işçi tutma fiyatları `AreaDefinition`'a, bina tarifleri `BuildingType`'a, işçi hızı/kapasitesi/sayısı `WorkerType`'a eklenir. Builder bu dosyaları ilk kez oluştururken
  başlangıç değerlerini yazar (`Create...IfMissing` satırları); sonra değerler sadece Inspector'dan değişir.
  İleride yeni ayar dosyaları (örn. `BuildingConfig`) gerekirse `Data/` altına konur.
- Kod ayarları **her kullanımda** dosyadan okur (Awake'te kopyalamaz) ki Play sırasında yapılan değişiklik hemen etkilesin.
- Haritanın yerleşimi (nesne konumları, kutu boyutları/renkleri) oynanış ayarı değildir; sahne kurma komutundadır.
- Çizim sıraları `SortOrder` sınıfındadır.

### İsimlendirme
- Sınıf / metod / özellik / olay: `PascalCase` (`CarryStack`, `TryAdd`, `CoinsChanged`)
- Alan ve yerel değişken: `camelCase` (`stackRoot`, `timer`)
- Sabit: `PascalCase` (`SortOrder.Character`)
- Ayar dosyası asset'leri: `PascalCase` (`GameConfig.asset`, `Wood.asset`); `ResourceType.Id` küçük harf (`wood`)
- Bool özellikler soru gibi: `IsFull`, `IsEmpty`; deneyip sonuç dönenler `Try...`: `TryAdd`, `TrySpend`

### Unity kalıpları
- Inspector alanları `[SerializeField] private` olur; `public` alan kullanılmaz.
  ScriptableObject'lerde salt-okunur ayar için `[field: SerializeField] public float X { get; private set; }` kullanılır.
- Tek örnekli yöneticiler basit singleton: `public static CurrencyManager Instance { get; private set; }`.
- Sistemler arası haberleşme C# `event` ile (örn. `CoinsChanged`); UI her karede değer sorgulamaz.
- `Update` içinde `GetComponent` / `Find` çağrılmaz; referanslar Awake'te ya da Inspector'dan (builder ile) alınır.
- Fizik: karakterlerde **Kinematic Rigidbody2D** (yerçekimi 0, `NeverSleep`). Etkileşim alanları `isTrigger = true`
  2D collider + `InteractionZone`'dan türeyen bileşen. Kinematic gövdeler birbirine çarpmaz: oyuncu ve işçiler
  iç içe geçebilir, kimse kimseyi itmez (bilerek böyle).
- Unity'deki `Tree` sınıfıyla karışmasın diye kaynak noktası `ResourceNode` adını taşır.

## Çizim Sırası (`SortOrder`)
| Nesne | Order |
|---|---|
| Zemin (ada) | 0 |
| Bölge zeminleri, iskele | 1 |
| Kapalı binanın silik zemini | 2 |
| Alanlar (Pazar, açma kareleri, bina giriş/çıkışı) | 3 |
| Çitler | 4 |
| Kaynak noktası alt / üst | 5 / 6 |
| Bina gövdesi / çatısı (kamp çadırı dahil) | 7 / 8 |
| Dünya yazıları | 9 |
| Yerdeki yığınlar | 10 + sırası |
| İlerleme çubuğu arka / dolan | 35 / 36 |
| Karakterler (oyuncu, işçiler) | 40 |
| Taşınan kaynaklar | 50 + yığındaki sırası |
| Efektler (uçan para) | 100 |

## Git Çalışma Düzeni
- Depo: https://github.com/bakiomer/KayipAda (dal: `main`). Kullanıcı Git bilmiyor; tüm Git işlemlerini Claude yapar ve kısaca Türkçe anlatır.
- Her **çalışan** özellikten sonra kullanıcıya **"Kaydedeyim mi?"** diye sorulur. "Evet" derse: commit + `git push`.
  Commit mesajları kısa, Türkçe ve ASCII karakterli (örn. `Asama 1: coklu kaynaklar`).
- Kullanıcı **"geri al"** derse: son commit'ten sonraki tüm değişiklikler atılır ve proje son çalışan kayda döner
  (`git reset --hard HEAD` + `git clean -fd -- Assets ProjectSettings Packages`). Önce neyin silineceği kısaca gösterilir.
  Unity açıksa geri almadan sonra Unity'nin dosyaları yeniden yüklemesi beklenir.
