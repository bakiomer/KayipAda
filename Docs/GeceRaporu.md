# Gece Raporu — Kayıp Ada

Her şey `3d` dalında; `main` dalına dokunulmadı, force push yapılmadı.

## 1. Hangi bölümler bitti?

| Bölüm | Durum |
|---|---|
| Hazırlık: kendi kendini test etme | ✅ Bitti (batchmode derleme, sahne kurma, ekran görüntüsü, PlayMode testleri) |
| A: 1. adımı doğrula ve düzelt | ✅ Bitti — kayıt: `3D: vitrin sahili dogrulandi` |
| B: 2. adım — tüm ada aynı kalitede | ✅ Bitti — kayıt: `Adim 2: tum ada ayni kalitede` |
| C: 3. adım — gemi, son savunma, kaçış | ✅ Bitti — kayıt: `Adim 3: gemi, son savunma, kacis` |
| D: kayıt sistemi | ✅ Bitti — kayıt: `Adim 4a: kayit sistemi` |

> Not: Bölümleri sırayla kaydettim ama kodun bir kısmı birlikte yazıldı. Örneğin kaçış ekranı süreyi ve kaptanlık
> XP'sini kayıt sisteminden aldığı için B, C ve D'nin ilk hali A'nın kaydına da girdi. Sonraki kayıtlar her bölümün
> düzeltmelerini ve doğrulamasını içeriyor.

## 2. Ne yapıldı?

### Hazırlık (test altyapısı)
- **Unity Editor açıktı.** Siz kapalı sanıyordunuz ama proje Unity'de açıktı; editörü kapatmama izin verilmedi.
  Açık editöre hiç dokunmadım. Projenin bir kopyasını `C:\Oyunlar\KayipAda\_BatchCopy` klasörüne aldım (Git'e girmez)
  ve bütün batchmode işlerini orada yaptım. Kaynak kodu asıl projede yazdım ve her testten önce kopyaya eşitledim.
  Testten sonra üretilen sahne/prefab/malzemeleri asıl projeye geri aldım.
- İlk denemede kopya geçici klasördeydi; Windows'un 260 karakterlik yol sınırı yüzünden paket dosyaları okunamadı.
  Kısa yola taşıyınca düzeldi.
- Testlerin oyun koduna erişebilmesi için derleme tanımları eklendi: `KayipAda`, `KayipAda.Editor`, `KayipAda.Tests`.
- `BatchTools.BuildScene`: sahne kurma komutu soru sormadan batchmode'da çalışıyor.
- `ScreenshotUtil`: oyun kamerasından ve yukarıdan/eğik genel açılardan, arayüz dahil 1080x1920 PNG ekran görüntüsü.
- **14 PlayMode testi** (temel döngü, geliştirme, gemi, gece, top, kaçış, hızlı test modu, kayıt + ekran görüntüleri).

### Bölüm A — 3D sahili doğrulama ve düzeltme
Ekran görüntülerinde gördüğüm ve düzelttiğim sorunlar:
- Sahne kurma komutu ayar dosyasını yeni sahne açılmadan yüklüyordu. Bu yüzden kamera, para ve ses yöneticilerinde
  ayar dosyası boş kalıyordu; dünkü "kamera takip etmiyor" hatası buydu.
- Kenney Nature bitkileri camgöbeği renkte geliyordu (renk uzayı sorunu). Quaternius tonlarına uyan elle bir palet eklendi.
- 2 ve 3 numaralı palmiye modelleri yatık/küçüktü. Hepsi dik duran 1 numaralı modelle değişti. Eğik palmiyeler kameradan
  uzağa yatırıldı; tepeleri oyuncuyu kapatmıyor.
- İskele direği modeli yatay bir kütük çıktı ve "yüksekliğe" göre ölçeklendiği için 20 metreye büyüyordu.
- Kamera çerçevesi: oyuncu ekranın alt-ortasında, önündeki alan daha geniş görünüyor (uzaklık 34, ileri bakış 3,5 m).
- Taşıyıcı işçi ürünleri gemiye götürdüğü için hiç para kazandırmıyordu. Artık oyuncu gemiye ilk malzemeyi getirene
  kadar Pazar'da satıyor, sonra gemiye yardım ediyor.
- Ekran görüntüleri: `F_1_baslangic`, `F_2_odun_tasiyor`, `F_7_yukaridan`, `F_8_egik`

### Bölüm B — Tüm ada aynı kalitede
- **Geliştirme tezgahı** kampta. Üstüne gelince alttan modern bir panel açılıyor; açıkken joystick yürütmüyor
  (dışarıya ya da X'e dokununca kapanıyor). 5 geliştirme × 5 seviye: Taşıma Kapasitesi, Koşu Hızı, İşçi Hızı,
  İşçi Kapasitesi, Bina Hızı. Seviye noktaları var, maksimumda "MAKS" yazıyor. Satın alınca parlama, "Seviye 3!" yazısı
  ve ses çıkıyor. Fiyatlar ve etkiler `Data/Upgrades` dosyalarında.
- **Sonraki hedef:** Açılabilecek en ucuz karenin üstünde zıplayan altın bir ok duruyor, karenin halkası nabız gibi atıyor.
  Hedef ekran dışındaysa ekranın kenarında yön oku çıkıyor. Kare kalmadıysa ok gemiyi gösteriyor.
- **Bölge kalitesi:**
  - Kayalık: çakıl ve küçük taşlar, kazmada taş kırıntısı ve kazma sesi.
  - Meyve bahçesi: çiçekler, çalılar, meyveli ağaçlar.
  - Ağaçlarda yaprak saçılması var.
  - Taş Atölyesi bayraklı bir ev modeli oldu.
- **Denge:** `Docs/Denge.md`. İlk kaçış tahmini ~33-36 dakika (hedef 30-45); 3 dakikadan uzun boş bekleme yok.
  Fiyatlar artırıldı: Kayalık 150, Bıçkıhane 250, Oduncu 400, Taşıyıcı 600, Atölye 800, Bahçe 600.
- Ekran görüntüleri: `F_B_gelistirme_paneli`, `F_4_kayalik`, `F_5_bahce`, `F_3_hepsi_acik_orta`

### Bölüm C — Gemi, son savunma, kaçış
- **Gemi (Quaternius Ship_Large), 4 aşama** (`Data/Ship.asset`):

  | Aşama | Malzeme |
  |---|---|
  | Omurga | 40 kalas |
  | Gövde | 50 kalas + 40 tuğla |
  | Direk ve Yelken | 40 kalas + 60 odun |
  | Erzak | 45 meyve |

  - Model tek parça olduğu için malzeme geldikçe aşağıdan yukarıya belirtiyorum (yeni `BuildReveal` shader'ı, kesilen
    kenarda sıcak parlama).
  - Erzakta güvertede variller ve sandıklar tek tek zıplayarak çıkıyor. Etrafta inşa direkleri var, gemi bitince kayboluyor.
  - Teslim alanında yerde dolan halka ve "Gövde (2/4) Kalas: 12/50" göstergesi var. Teslimde kıvılcım, aşama bitince konfeti.
  - **Karar:** "Gövde (kalas + taş)" aşamasındaki taşı **tuğla** olarak yorumladım. Böylece Taşıyıcı işçiler de gemiye
    malzeme getirebiliyor ve Taş Atölyesi anlam kazanıyor; ham taş toplara gülle oluyor.
- **Son gece:** Gemi bitince duyuru çıkıyor, ışık geceye dönüyor, sahilden 3 dalga (5 + 8 + 11) iskelet korsan gemiye
  yürüyor (yürüme, saldırı ve ölme animasyonlarıyla). Geminin can çubuğu ekranın üstünde.
- **Toplar:** İskele çevresinde 3 top (Quaternius Prop_Cannon). Omurga bitince açma kareleri beliriyor (400 / 550 / 700).
  En yakın düşmana kavisli gülle atıp patlatıyorlar (patlama efekti + ses). Oyuncu kayalıktan taş getirip "Taş → Gülle"
  alanından dolduruyor; topun üstünde "Gülle 3/10" yazıyor.
- **Kaybedince:** "Gemi ağır hasar aldı!" paneli ve "TEKRAR DENE". Gece baştan başlıyor, ilerleme silinmiyor.
- **Kaçış:** Dalgalar bitince gün doğuyor, gemi yelken açıp uzaklaşıyor, kamera gemiyi takip ediyor. Ardından
  "KAÇTIN!" ekranı çıkıyor: süre, +100 kaptanlık XP'si ve seviye, "Yeni adalar yakında!", "TEKRAR OYNA".
- **Hızlı Test Modu:** Tools > Kayıp Ada > Hızlı Test Modu (tikli/tiksiz). Fiyatlar %8'e, gemi malzemesi ve düşman sayısı
  %20'ye iner; kapatınca asıl değerlere dönülür (asıl sayılar hiç değişmez).
- Ekran görüntüleri: `F_C_gemi_omurga`, `F_C_gemi_govde`, `F_C_son_gece`, `F_C_son_gece_genel`, `F_C_tekrar_dene`, `F_C_kacis_ekrani`

### Bölüm D — Kayıt sistemi
- Kaydedilenler: para, açılan alanlar, yarım ödemeler, geliştirmeler, gemi aşaması ve teslimler, top gülleleri, oyun süresi.
  JSON olarak `persistentDataPath/kayipada_save.json` dosyasına yazılıyor.
- 10 saniyede bir (ayar dosyasında), uygulama arka plana alınınca ve kapanırken otomatik kaydediyor.
- İşçiler, açılan işçi alanlarından kendiliğinden yeniden çıkıyor.
- **Kaptanlık:** Kaçışta +100 XP, her 100 XP bir seviye. Seviye başına %3 hız ve kapasite, en fazla 10 seviye.
  "Tekrar oyna" adadaki ilerlemeyi sıfırlıyor ama kaptanlık kalıyor.
- **Tools > Kayıp Ada > Kaydı Sil** menüsü eklendi.

## 3. Test sonuçları
Son tur (`F`): **14 testin 14'ü geçti.**

| Test | Sonuç |
|---|---|
| Oyuncu ağaçtan odun topluyor | ✅ |
| Pazar'da satınca para artıyor | ✅ |
| Açma karesi parayı alıp alanı açıyor | ✅ |
| Bina ham maddeyi işliyor | ✅ |
| İşçiler kendi kendine para kazandırıyor | ✅ |
| Geliştirme alınca kapasite artıyor, MAKS'tan sonra alınamıyor | ✅ |
| Tezgahta panel açılıyor ve oyuncu duruyor | ✅ |
| Gemi aşamaları ilerliyor, omurgadan sonra toplar açılabiliyor | ✅ |
| Son gecede düşmanlar geliyor, top ateş ediyor | ✅ |
| Dalgalar bitince kaçış ekranı açılıyor | ✅ |
| Kaybedince "Tekrar dene" ilerlemeyi silmeden geceyi baştan başlatıyor | ✅ |
| Hızlı Test Modu fiyatları/malzemeleri düşürüyor, kapatınca normale dönüyor | ✅ |
| Kayıt: para/alan/geliştirme/gemi geri geliyor; kaçıştan sonra kaptanlık kalıyor | ✅ |
| Ekran görüntüleri | ✅ |

## 4. Bilinen sorunlar ve çözemediklerim
- **Elle oynanmadı.** Testler oyuncuyu ışınlayarak ve süreyi hızlandırarak oynuyor; joystick hissi, ses seviyeleri ve
  gerçek zamanlı denge sizin denemenizi bekliyor.
- **Animasyonlar yalnızca ekran görüntülerinden kontrol edildi.** İskeletlerin yürüme/saldırı/ölme animasyonlarını ve
  oyuncunun taşıma pozunu hareket halinde göremedim; durağan görüntülerde doğru görünüyorlar.
- **Model yönleri otomatik düzeltiliyor.** Gemi modeli yan yatık geliyordu; komut bunu kendisi algılayıp düzeltiyor.
  Benzer bir model değişikliğinde (ModelCatalog) yön yine ters çıkabilir.
- **Taşıyıcının gemiye gidişi uzun yol:** İskele adanın kuzey ucunda; Taşıyıcı kalasları oraya taşırken Pazar'a satış durur.
  Bu bilinçli bir seçim (gemi inşasında para yerine malzeme).
- **Ses dengesi:** Kenney seslerinin seviyeleri `Data/SoundLibrary.asset`'te; kulakla ince ayar yapılmadı.
- **`_BatchCopy` klasörü:** Testler için projenin kopyası (Git'e girmez) ve diskte birkaç GB yer kaplar. Testleri
  tekrar çalıştırmayacaksan silebilirsin; Claude bir dahaki gece çalışmasında yeniden oluşturur.
- **Unity açıkken dosyalar değişti:** Gece boyunca açık editörün altındaki dosyalar (kod, sahne, prefab) güncellendi.
  Unity'ye tıklayınca hepsini yeniden yükler. "Sahne diskte değişti, yeniden yükle?" diye sorarsa **Reload** de.
- **İndirilen içerik:** Bu gece internetten hiçbir şey indirmedim. (Önceki oturumda Quaternius'un resmi Google Drive
  paylaşımından `Atlas_Pirate.png` (CC0) indirilmişti; `Docs/Modeller.md`'de yazıyor.)

## 5. Sabah Unity'de elle denemen gerekenler (Hızlı Test Modu ile)
1. Unity'de proje açık kaldıysa önce **File > Save** yapmana gerek yok; sadece Unity'ye tıkla. Gece eklenen dosyaları
   yükleyip derlemesini bekle (birkaç dakika sürebilir).
2. Üst menüden **Tools > Kayıp Ada > Prototip Sahnesini Kur** komutunu çalıştır ve bitmesini bekle.
3. **Tools > Kayıp Ada > Kaydı Sil** (temiz başlangıç için).
4. **Tools > Kayıp Ada > Hızlı Test Modu** (menüde yanında tik çıkmalı).
5. **Play**'e bas. Fareyle ekranda basılı tutup sürükleyerek yürü (telefondaki gibi).
6. Kontrol listesi:
   - Altın ok bir sonraki kareyi gösteriyor mu?
   - Palmiyeden odun topla, Pazar'da sat: para paneli zıplıyor mu, "+5" yazısı çıkıyor mu?
   - Kayalık → Bıçkıhane → Oduncu → Taşıyıcı → Taş Atölyesi → Meyve Bahçesi kareleri açılıyor mu (konfeti, zıplama)?
   - Kamptaki **GELİŞTİR** tezgahına gir: panel alttan açılıyor mu, alırken parlama ve ses var mı, açıkken yürüyemiyor musun?
   - Kuzeydeki iskeleye kalas götür: gemi aşağıdan yukarı yükseliyor mu? Omurga bitince "Top" kareleri çıkıyor mu?
   - Kayalıktan taş taşıyıp topların "Taş → Gülle" alanına bırak.
   - Gemiyi bitir (gövdeye tuğla, direğe odun, erzağa meyve): gece oluyor mu, iskeletler geliyor mu, toplar ateş ediyor mu?
   - Kazanınca gemi yelken açıp gidiyor ve kaçış ekranı çıkıyor mu? "TEKRAR OYNA" yeni oyunu başlatıyor mu?
7. İş bitince **Hızlı Test Modu**'nu kapat (tikini kaldır).
8. Kaydı da dene: biraz oyna, Play'i durdur, tekrar Play'e bas. Para ve açılan yerler aynı kalmalı.

## 6. Öneriler (en fazla 5)
1. **Gerçek kişilerle süre ölçümü:** Denge tablosu tahmin. 2-3 kişiye Hızlı Test Modu kapalı oynatıp her bölgenin açılma
   dakikasını not etmek en doğru ayarı verir.
2. **Gemi için ayrı bir "tersane" karesi:** Şu an teslim alanı baştan açık. Taş Atölyesi'nden sonra açılan bir kare,
   oyuncuya "artık gemi zamanı" duygusunu daha net verir.
3. **Kayalık taşı için Taşıyıcı görevi:** Topları doldurmak tamamen oyuncunun işi. Gecede tek kişi zorlanırsa bir
   "Gülleci" işçi eklenebilir.
4. **Ana menü ve ayarlar (Plan Adım 4'ün kalanı):** Ses aç/kapa, titreşim, kaydı sıfırlama; şu an sadece Tools menüsünden var.
5. **Telefonda performans testi (Plan Adım 5):** Gölge mesafesi ve partikül sayıları mobil için ayarlı, ama gerçek bir
   orta seviye Android'de FPS ölçülmeli.
