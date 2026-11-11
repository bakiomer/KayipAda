# Kayıp Ada — Oyun Tasarımı ve Kod Kuralları

## Proje
- **Motor:** Unity 6 (6000.6), Universal Render Pipeline 2D şablonu
- **Platform:** Android, **dikey (portrait)** ekran
- **Tür:** Idle arcade (kaynak topla → taşı → sat → geliştir)
- **Görünüm:** Yukarıdan (top-down), 2D
- **Girdi:** Yalnızca **yeni Input System** etkin (`activeInputHandler: 1`). Eski `Input.GetAxis` vb. KULLANILMAZ.
  Dokunmatik kontrol uGUI sanal joystick ile yapılır; editörde test için WASD / ok tuşları da çalışır.

## Oyun Tasarımı
Oyuncu bir adaya düşmüş bir kazazededir. Amaç: adadan kaçmak için gemi inşa etmek.

### Temel döngü
1. Oyuncu kaynak noktasına (ağaç, taş, ...) yaklaşır → kaynak otomatik olarak sırtına eklenir.
2. Sırtındaki yığın kapasitesi sınırlıdır (başlangıçta 10).
3. Satış alanına girince yığın tek tek satılır → para kazanılır.
4. Para ile yeni binalar, işçiler ve yükseltmeler açılır.
5. Son hedef: gemiyi parça parça inşa edip adadan kaçmak.

### Yol haritası
- [x] **Aşama 1 — Prototip:** joystick, 3 ağaç, odun yığını (max 10), satış alanı, para göstergesi
- [ ] Aşama 2: Kaydetme (PlayerPrefs/JSON), "MAX" uyarısı, ağaçların tükenip yeniden büyümesi
- [ ] Aşama 3: Satın alınabilir alanlar (para dökerek bina açma), kapasite/hız yükseltmeleri
- [ ] Aşama 4: Yeni kaynaklar (taş, meyve), işleme binaları (kereste atölyesi)
- [ ] Aşama 5: İşçiler (kaynakları otomatik taşıyan NPC'ler)
- [ ] Aşama 6: Gemi inşası ve oyun sonu
- [ ] Aşama 7: Gerçek görseller, ses, efektler

## Klasör Yapısı
Oyunun tüm dosyaları `Assets/_Game/` altındadır (Unity'nin kendi dosyalarıyla karışmasın diye).

```
Assets/_Game/
  Scripts/
    Core/      MoneyManager, CameraFollow — oyun geneli sistemler
    Player/    PlayerMovement, ResourceStack — oyuncu davranışları
    World/     ResourceTree, SellZone, ResourceItem — dünyadaki etkileşimli nesneler
    UI/        VirtualJoystick, MoneyUI — arayüz
  Editor/      PrototypeSceneBuilder — editör menü komutları (oyuna dahil edilmez)
  Art/         Sprite'lar (şimdilik otomatik üretilen beyaz kare)
  Prefabs/     Wood.prefab vb.
  Scenes/      Prototype.unity
```

## Sahne Kurulumu
Sahne elle kurulmaz. **Tools > Kayıp Ada > Prototip Sahnesini Kur** menüsü
(`Assets/_Game/Editor/PrototypeSceneBuilder.cs`) sahneyi sıfırdan oluşturur ve
`Assets/_Game/Scenes/Prototype.unity` olarak kaydeder. Sahneye yeni bir nesne eklenecekse
önce bu builder güncellenir.

## Kod Kuralları
- **Basit ve okunaklı** kod. Gereksiz soyutlama, interface ağacı veya framework yok.
- Her dosyanın başında 2-4 satırlık **Türkçe açıklama yorumu** bulunur (dosya ne işe yarar).
- Kod içi isimler (sınıf, değişken, metod) **İngilizce**; yorumlar ve oyuncuya görünen metinler **Türkçe**.
- Tüm oyun kodu `KayipAda` namespace'i içindedir.
- Bir dosyada bir `MonoBehaviour` sınıfı, dosya adı = sınıf adı.
- Inspector'da görünecek alanlar `[SerializeField] private` olur; `public` alan kullanılmaz.
  Dışarıya okuma gerekiyorsa `public int Money => money;` gibi salt-okunur özellik açılır.
- İsimlendirme: sınıf/metod/özellik `PascalCase`, alanlar/değişkenler `camelCase`.
- Tek örnekli yöneticiler basit singleton kullanır: `public static MoneyManager Instance { get; private set; }`.
- Sistemler arası haberleşme için C# `event` (örn. `MoneyChanged`) tercih edilir; UI her karede değer sorgulamaz.
- `Update` içinde `GetComponent` / `Find` çağrılmaz; referanslar Awake'te ya da Inspector'dan alınır.
- Fizik: oyuncuda **Kinematic Rigidbody2D** (yerçekimi 0, `NeverSleep`) vardır. Etkileşim alanları
  `isTrigger = true` olan 2D collider'lardır ve `OnTriggerEnter2D/Exit2D` ile çalışır.
- Unity'deki `Tree` sınıfıyla çakışmamak için ağaç bileşeninin adı `ResourceTree`'dir.
- Sihirli sayılar yerine Inspector'dan ayarlanabilen alanlar kullanılır (hız, kapasite, fiyat...).

## Çizim Sırası (Sorting Order)
| Nesne | Order |
|---|---|
| Zemin (ada) | 0 |
| Satış alanı | 1 |
| Ağaç | 2 |
| Oyuncu | 10 |
| Taşınan kaynaklar | 20 + sıra |
