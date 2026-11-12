# Kayıp Ada — Oyun Tasarımı ve Kod Kuralları

> **Son tamamlanan: Aşama 1 — Çoklu kaynaklar.** Sıradaki: Aşama 2 — Açılabilir alanlar.

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
| 2 | Açılabilir alanlar | Parayla yeni bölge açılıyor (UnlockPad) | ⬜ |
| 3 | İşleme binaları | Odun kalasa, taş tuğlaya dönüşüyor | ⬜ |
| 4 | İşçiler | İşçiler sen dokunmadan kaynak taşıyor | ⬜ |
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
    Zones/           Üzerine girilince çalışan alanlar (Pazar, ileride açma kareleri, bina girişleri)
    UI/              Ekran arayüzü
    Editor/          Editör menü komutları (oyuna dahil edilmez)
  Data/
    GameConfig.asset           Tüm genel ayar sayıları
    ResourceTypes/             Kaynak türleri: Wood, Stone, Fruit (ad, renk, satış fiyatı)
    NodeTypes/                 Kaynak noktası türleri: Tree, Rock, FruitBush (stok, toplama aralığı, yeniden dolma süresi)
  Prefabs/           ResourceItem.prefab (tüm kaynak parçaları için tek prefab)
  Art/               WhiteSquare.png (tüm kutuların kullandığı beyaz kare; Aşama 10'da gerçek görseller)
  Scenes/            Prototype.unity (sahne kurma komutu üretir — elle düzenleme kaybolur)
```

> Not: Kaynak scriptleri klasörünün adı bilerek `ResourceSystem`'dır, `Resources` değil. Unity'de
> `Resources` adlı her klasör özel anlam taşır (içindeki her şey oyuna gömülür), karışıklık olmasın.

## Sınıfların Görevleri
| Sınıf | Klasör | Görevi |
|---|---|---|
| `GameConfig` | Core | ScriptableObject. Oyuncu hızı/kapasitesi, satma aralığı, kamera, joystick, efekt sayıları (tükenen noktanın küçülme/solma oranı dahil) |
| `SortOrder` | Core | Sprite çizim sıraları (sabitler) |
| `CurrencyManager` | Core | Parayı tek yerde tutar (`Coins`), `Add` / `TrySpend`, değişince `CoinsChanged` olayı. Singleton |
| `CameraFollow` | Core | Kamera oyuncuyu yumuşakça takip eder; oyuncu ekranın biraz altında kalır (`CameraOffsetY`) |
| `PlayerController` | Player | Joystick/klavye ile hareket, ada sınırı, oyuncu ayarlarını (kapasite) `CarryStack`'e uygular |
| `ResourceType` | ResourceSystem | ScriptableObject. Kimlik (`Id`), görünen ad, renk, satış fiyatı |
| `ResourceItem` | ResourceSystem | Tek bir kaynak parçası; `Setup(type)` ile türü/rengi atanır, satılınca uçup kaybolur |
| `CarryStack` | ResourceSystem | Karakterin sırtındaki karışık türlü yığın: `TryAdd`, `RemoveTop()`, `RemoveTop(type)`, `CountOf(type)`. İleride işçiler de kullanır |
| `ResourceNodeType` | ResourceSystem | ScriptableObject. Nokta türü: verdiği kaynak, stok, toplama aralığı, yeniden dolma süresi |
| `ResourceNode` | ResourceSystem | Kaynak noktası (ağaç, kaya, çalı — hepsi aynı bileşen). İçerideki karaktere aralıklarla kaynak verir; stok bitince küçülüp solar, süre dolunca yeniden dolar. Görünümü `Visual` alt nesnesindedir |
| `InteractionZone` | Zones | Trigger alanların ortak temeli; içerideki `CarryStack`'i `Visitor` olarak tutar |
| `Market` | Zones | Pazar; tüm türleri kabul eder, yığından tek tek alır, her parçayı kendi `SellPrice`'ından satar |
| `VirtualJoystick` | UI | Ekranın neresine dokunulursa orada beliren joystick; `Direction` verir |
| `CurrencyUI` | UI | Üstteki para göstergesi; `CoinsChanged` olayını dinler |
| `PrototypeSceneBuilder` | Editor | **Tools > Kayıp Ada > Prototip Sahnesini Kur** komutu |

## Sahne Kurma Komutu
Sahne elle kurulmaz. **Tools > Kayıp Ada > Prototip Sahnesini Kur**
(`Assets/_Game/Scripts/Editor/PrototypeSceneBuilder.cs`):
- Her çalıştırmada sahneyi **sıfırdan** kurar ve `Assets/_Game/Scenes/Prototype.unity` üzerine kaydeder.
- Ayar dosyalarını (`Data/` altındakiler) **sadece yoksa** oluşturur; kullanıcının değiştirdiği değerler korunur.
- `ResourceItem.prefab` ve `WhiteSquare.png` her seferinde yeniden üretilir (içlerinde ayar yok).
- Sahneye yeni bir nesne eklenecekse **her aşamada** önce bu komut güncellenir.
- Haritanın yerleşimi (konumlar, boyutlar, kutu renkleri) bu dosyanın başındaki "Sahne düzeni" bölümündedir.
- **Yerleşim kuralı:** Oyun başlarken görünen nesneler üstteki para panelinin altında kalmamalı ve dar (20:9)
  telefonlarda yandan taşmamalı. Başlangıç ekranında güvenli bölge yaklaşık x = -3.3..3.3, y = -8..6.
  Komşu noktaların trigger alanları çakışmamalı; oyuncunun başlangıç noktası hiçbir alanın içinde olmamalı.
- Kaynak noktaları `CreateNode(ad, konum, nokta türü, ..., görünüm metodu)` ile kurulur; yeni bir nokta türü için
  sadece `BuildXxxVisual` metodu ve bir `CreateNodeTypeIfMissing` satırı eklenir.
- Özel alanlara değer atamak için `SetField(bileşen, "alanAdı", değer)` yardımcısı kullanılır.
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
  `ResourceNodeType`'a eklenir. Builder bu dosyaları ilk kez oluştururken başlangıç değerlerini yazar
  (`CreateResourceTypeIfMissing`, `CreateNodeTypeIfMissing` satırları); sonra değerler sadece Inspector'dan değişir. İleride yeni ayar dosyaları
  (örn. `UnlockPadConfig`, `BuildingConfig`) gerekirse `Data/` altına konur.
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
  2D collider + `InteractionZone`'dan türeyen bileşen.
- Unity'deki `Tree` sınıfıyla karışmasın diye kaynak noktası `ResourceNode` adını taşır.

## Çizim Sırası (`SortOrder`)
| Nesne | Order |
|---|---|
| Zemin (ada) | 0 |
| Alanlar (Pazar) | 1 |
| Kaynak noktası alt / üst | 2 / 3 |
| Dünya yazıları | 5 |
| Karakterler | 10 |
| Taşınan kaynaklar | 20 + yığındaki sırası |

## Git Çalışma Düzeni
- Depo: https://github.com/bakiomer/KayipAda (dal: `main`). Kullanıcı Git bilmiyor; tüm Git işlemlerini Claude yapar ve kısaca Türkçe anlatır.
- Her **çalışan** özellikten sonra kullanıcıya **"Kaydedeyim mi?"** diye sorulur. "Evet" derse: commit + `git push`.
  Commit mesajları kısa, Türkçe ve ASCII karakterli (örn. `Asama 1: coklu kaynaklar`).
- Kullanıcı **"geri al"** derse: son commit'ten sonraki tüm değişiklikler atılır ve proje son çalışan kayda döner
  (`git reset --hard HEAD` + `git clean -fd -- Assets ProjectSettings Packages`). Önce neyin silineceği kısaca gösterilir.
  Unity açıksa geri almadan sonra Unity'nin dosyaları yeniden yüklemesi beklenir.
