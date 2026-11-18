// GameConfig.cs
// Oyunun tüm genel ayar sayılarını (hız, kapasite, aralıklar, kamera, efekt, ses...) tutan ayar dosyası.
// Assets/_Game/Data/GameConfig.asset olarak bulunur; Inspector'dan değiştirilir.
// Kural: oynanışı ya da hissi etkileyen hiçbir sayı kodun içine yazılmaz, buraya eklenir.

using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Kayıp Ada/Oyun Ayarları")]
    public class GameConfig : ScriptableObject
    {
        [field: Header("Oyuncu")]
        [field: Tooltip("Oyuncunun koşma hızı (metre/saniye).")]
        [field: SerializeField, Min(0f)] public float PlayerSpeed { get; private set; } = 6f;

        [field: Tooltip("Karakterin yürüdüğü yöne dönme hızı (büyük = daha çabuk döner).")]
        [field: SerializeField, Min(0.1f)] public float TurnSpeed { get; private set; } = 14f;

        [field: Tooltip("Oyuncunun taşıyabileceği en fazla kaynak sayısı.")]
        [field: SerializeField, Min(1)] public int CarryCapacity { get; private set; } = 10;

        [field: Tooltip("Animasyonda koşu hızına karşılık gelen hız (m/sn). Bunun altında yürüme/koşu karışır.")]
        [field: SerializeField, Min(0.1f)] public float AnimRunSpeed { get; private set; } = 6f;

        // Not: Toplama hızı, stok ve yeniden dolma süresi her nokta türüne özeldir → Data/NodeTypes
        [field: Header("Satış")]
        [field: Tooltip("Pazar kaç saniyede bir kaynak satar.")]
        [field: SerializeField, Min(0.01f)] public float SellInterval { get; private set; } = 0.1f;

        [field: Header("Ekonomi")]
        [field: SerializeField, Min(0)] public int StartingCoins { get; private set; } = 0;

        [field: Header("İşleme Binaları")]
        [field: Tooltip("Bina giriş/çıkışında parçaların kaç saniyede bir alınıp verildiği.")]
        [field: SerializeField, Min(0.01f)] public float BuildingTransferInterval { get; private set; } = 0.12f;
        // Not: Tarif, işleme süresi ve kapasiteler her binaya özeldir → Data/Buildings

        [field: Header("İşçiler")]
        [field: Tooltip("Aynı görevde birden fazla işçi varsa kamptan kaç saniye arayla çıktıkları.")]
        [field: SerializeField, Min(0f)] public float WorkerSpawnInterval { get; private set; } = 0.6f;
        // Not: İşçi hızı, kapasitesi, tutma fiyatları ve en fazla sayısı her göreve özeldir → Data/Workers

        [field: Tooltip("Barakadan yeni çıkan işçinin kapının önünde sevinç süresi (saniye).")]
        [field: SerializeField, Min(0f)] public float HireCheerTime { get; private set; } = 1.3f;

        [field: Header("Bölge Açma (UnlockPad)")]
        [field: Tooltip("Kareye basınca ödemenin başlaması için beklenen süre (saniye).")]
        [field: SerializeField, Min(0f)] public float UnlockStartDelay { get; private set; } = 0.3f;

        [field: Tooltip("Para parçalarının kaç saniyede bir aktığı.")]
        [field: SerializeField, Min(0.01f)] public float UnlockPayInterval { get; private set; } = 0.05f;

        [field: Tooltip("Paran yeterliyse fiyatın tamamının kaç saniyede ödeneceği (fiyat ne olursa olsun).")]
        [field: SerializeField, Min(0.1f)] public float UnlockPayDuration { get; private set; } = 2f;

        [field: Tooltip("Para parçasının oyuncudan kareye uçma süresi (saniye).")]
        [field: SerializeField, Min(0.01f)] public float CoinFlyDuration { get; private set; } = 0.3f;

        [field: Header("Kamera")]
        [field: Tooltip("Kameranın aşağı bakma açısı (derece). 50 civarı: idle arcade görünümü.")]
        [field: SerializeField, Range(20f, 89f)] public float CameraPitch { get; private set; } = 45f;

        [field: Tooltip("Kameranın yatay dönüşü (derece). 0 = kamera adanın güneyinden kuzeye bakar.")]
        [field: SerializeField] public float CameraYaw { get; private set; } = 0f;

        [field: Tooltip("Görüş açısı (derece). Dar açı (30-35) = daha az perspektif bozulması.")]
        [field: SerializeField, Range(10f, 80f)] public float CameraFov { get; private set; } = 32f;

        [field: Tooltip("Kameranın baktığı noktaya uzaklığı (metre). Büyük = daha geniş görüş.")]
        [field: SerializeField, Min(1f)] public float CameraDistance { get; private set; } = 34f;

        [field: Tooltip("Kamera oyuncunun ne kadar önüne bakar (metre). Oyuncu ekranın biraz altında durur.")]
        [field: SerializeField] public float CameraFocusOffset { get; private set; } = 4.5f;

        [field: SerializeField, Min(0f)] public float CameraFollowSpeed { get; private set; } = 6f;

        [field: Header("Paneller (Barak, Tezgah, Ambar, Görev Panosu, Yükselt)")]
        [field: SerializeField, Tooltip("Oyuncu alanda bu kadar sn durunca (joystick bırakılmış) panel açılır.")]
        public float PanelOpenDelay { get; private set; } = 0.5f;

        [field: Header("Joystick")]
        [field: Tooltip("Joystick topunun merkezden en fazla uzaklaşabileceği mesafe (UI birimi).")]
        [field: SerializeField, Min(1f)] public float JoystickRange { get; private set; } = 110f;

        [field: Tooltip("Bu değerin altındaki küçük parmak hareketleri yok sayılır (0-1).")]
        [field: SerializeField, Range(0f, 0.9f)] public float JoystickDeadZone { get; private set; } = 0.1f;

        [field: Header("Sırttaki Yığın")]
        [field: Tooltip("Taşınan kaynakların üst üste aralığı (metre).")]
        [field: SerializeField, Min(0f)] public float StackSpacing { get; private set; } = 0.28f;

        [field: Tooltip("En alttaki kaynağın takip sertliği (büyük = sıkı, küçük = sallanan).")]
        [field: SerializeField, Min(0.1f)] public float StackFollowSharpness { get; private set; } = 25f;

        [field: Tooltip("Her kat yukarıda takip ne kadar gecikir (büyük = üstler daha çok sallanır).")]
        [field: SerializeField, Min(0f)] public float StackSwayPerLevel { get; private set; } = 0.04f;

        [field: Tooltip("Her katın düz duruştan en fazla ne kadar kayabileceği (metre). Küçük = daha dik yığın.")]
        [field: SerializeField, Min(0f)] public float StackMaxLeanPerLevel { get; private set; } = 0.02f;

        [field: Tooltip("Sırttaki yığının genişliği (metre): parça genişliğine göre bu kadar yere 1-3 sütun sığar.")]
        [field: SerializeField, Min(0.1f)] public float StackBlockWidth { get; private set; } = 0.78f;

        [field: Tooltip("Sırttaki yığının en fazla sütun sayısı.")]
        [field: SerializeField, Range(1, 4)] public int StackMaxColumns { get; private set; } = 3;

        [field: Tooltip("Yığının görünen en fazla yüksekliği (metre, yığın noktasından). Fazlası \"x40\" etiketiyle gösterilir.")]
        [field: SerializeField, Min(0.1f)] public float StackMaxHeight { get; private set; } = 0.9f;

        [field: Tooltip("Sayı etiketinin yığının üstünden yüksekliği (metre).")]
        [field: SerializeField, Min(0f)] public float StackLabelGap { get; private set; } = 0.35f;

        [field: Tooltip("Kaynağın yığına/yere/pazara kavis çizerek uçma süresi (saniye).")]
        [field: SerializeField, Min(0.01f)] public float ItemFlyDuration { get; private set; } = 0.3f;

        [field: Tooltip("Uçan kaynağın kavis yüksekliği (metre).")]
        [field: SerializeField, Min(0f)] public float ItemFlyHeight { get; private set; } = 1.2f;

        [field: Header("Efektler")]
        [field: Tooltip("Kaynak noktası her toplamada ne kadar sıkışıp genişler (0.15 = %15).")]
        [field: SerializeField, Min(0f)] public float NodeSquash { get; private set; } = 0.15f;

        [field: Tooltip("Kaynak noktası her toplamada kaç derece sallanır.")]
        [field: SerializeField, Min(0f)] public float NodeShakeAngle { get; private set; } = 6f;

        [field: Tooltip("Toplama sallanmasının süresi (saniye).")]
        [field: SerializeField, Min(0.01f)] public float NodeHitDuration { get; private set; } = 0.25f;

        [field: Tooltip("Kaynak noktasının son kaynağı kalınca boyu (1 = hiç küçülmez). Toplandıkça bu boya doğru küçülür; yeniden dolarken bu boydan tam boya büyür.")]
        [field: SerializeField, Range(0.2f, 1f)] public float NodeMinStockScale { get; private set; } = 0.6f;

        [field: Tooltip("Kütüğü olmayan (tükenince yerinde parça kalmayan) kaynak noktasının tükenince boyu.")]
        [field: SerializeField, Range(0.05f, 1f)] public float NodeDepletedScale { get; private set; } = 0.35f;

        [field: Tooltip("Açılan nesnelerin zıplayarak belirme süresi (saniye).")]
        [field: SerializeField, Min(0.01f)] public float PopInDuration { get; private set; } = 0.45f;

        [field: Tooltip("Bölge açılınca çit kapısının açılma süresi (saniye).")]
        [field: SerializeField, Min(0.05f)] public float GateOpenDuration { get; private set; } = 0.8f;

        [field: Tooltip("Ekranın ortasındaki \"AÇILDI!\" yazısının toplam süresi (saniye).")]
        [field: SerializeField, Min(0.2f)] public float BurstTextDuration { get; private set; } = 1.8f;

        [field: Tooltip("Açılan nesneler arasındaki sıra gecikmesi (saniye).")]
        [field: SerializeField, Min(0f)] public float PopInStagger { get; private set; } = 0.04f;

        [field: Tooltip("Para göstergesinin her artışta ne kadar büyüdüğü (0.2 = %20).")]
        [field: SerializeField, Min(0f)] public float CoinPunch { get; private set; } = 0.2f;

        [field: Tooltip("Uçan '+5' yazısının ömrü (saniye).")]
        [field: SerializeField, Min(0.1f)] public float FloatingTextDuration { get; private set; } = 0.9f;

        [field: Tooltip("Bu uzaklıktaki (metre) aynı uçan yazı yeniden açılmaz, eskisi baştan oynar; farklı yazılar üst üste kayar.")]
        [field: SerializeField, Min(0f)] public float FloatingTextMergeDistance { get; private set; } = 2.5f;

        [field: Tooltip("Yeni bir uçan yazı gelince eskilerin yukarı kayma mesafesi (metre).")]
        [field: SerializeField, Min(0f)] public float FloatingTextStackGap { get; private set; } = 0.7f;

        [field: Header("Yönlendirme (sonraki hedef)")]
        [field: Tooltip("Sonraki açma karesinin üstündeki okun yüksekliği (metre).")]
        [field: SerializeField, Min(0f)] public float GuideArrowHeight { get; private set; } = 2.6f;

        [field: Tooltip("Okun aşağı yukarı sallanma miktarı (metre).")]
        [field: SerializeField, Min(0f)] public float GuideArrowBob { get; private set; } = 0.3f;

        [field: Tooltip("Sonraki hedef karesinin parlama/nabız hızı.")]
        [field: SerializeField, Min(0f)] public float GuidePulseSpeed { get; private set; } = 4f;

        [field: Tooltip("Gemi teslim alanındaki yer işaretinin nabız atarken büyüyüp küçülme oranı (0.06 = %6).")]
        [field: SerializeField, Min(0f)] public float GuidePulseScale { get; private set; } = 0.06f;

        [field: Header("Kaptanlık (kalıcı seviye)")]
        [field: Tooltip("Kaptanlık seviyesi başına hız/kapasite bonusu (0.03 = %3).")]
        [field: SerializeField, Min(0f)] public float CaptainBonusPerLevel { get; private set; } = 0.03f;

        [field: Tooltip("Bonusun hesaba katıldığı en yüksek kaptanlık seviyesi.")]
        [field: SerializeField, Min(0)] public int CaptainMaxLevel { get; private set; } = 10;

        [field: Tooltip("İlk kaçışta kazanılan kaptanlık XP'si.")]
        [field: SerializeField, Min(0)] public int EscapeXp { get; private set; } = 100;

        [field: Tooltip("Kaçış ekranı: bu süreden (saniye) kısa sürede kaçan 3 yıldız, TwoStarSeconds'tan kısa 2 yıldız, değilse 1 yıldız alır.")]
        [field: SerializeField, Min(1f)] public float ThreeStarSeconds { get; private set; } = 2100f;
        [field: SerializeField, Min(1f)] public float TwoStarSeconds { get; private set; } = 3000f;

        [field: Tooltip("Kaçış ekranı animasyonu: yıldızlar arası bekleme, XP/para sayma süresi, buton nabzı, konfeti süresi.")]
        [field: SerializeField, Min(0f)] public float StarDelay { get; private set; } = 0.35f;
        [field: SerializeField, Min(0.1f)] public float CountUpDuration { get; private set; } = 1.2f;
        [field: SerializeField, Min(0f)] public float PulseSpeed { get; private set; } = 5f;
        [field: SerializeField, Min(0f)] public float PulseAmount { get; private set; } = 0.06f;
        [field: SerializeField, Min(0f)] public float EscapeConfettiDuration { get; private set; } = 20f;

        [field: Tooltip("Bir kaptanlık seviyesi için gereken XP.")]
        [field: SerializeField, Min(1)] public int XpPerCaptainLevel { get; private set; } = 100;

        [field: Header("Kayıt")]
        [field: Tooltip("Oyun kaç saniyede bir otomatik kaydedilir.")]
        [field: SerializeField, Min(1f)] public float AutoSaveInterval { get; private set; } = 10f;

        [field: Header("Hızlı Test Modu (Tools > Kayıp Ada > Hızlı Test Modu)")]
        [field: Tooltip("Açıkken fiyatlar ve gereken malzemeler çok düşer, oyun 3-5 dakikada biter. Asıl değerler değişmez.")]
        [field: SerializeField] public bool QuickTestMode { get; private set; }

        [field: Tooltip("Hızlı test modunda fiyat çarpanı.")]
        [field: SerializeField, Range(0.01f, 1f)] public float QuickTestPriceScale { get; private set; } = 0.08f;

        [field: Tooltip("Hızlı test modunda gemi malzemesi ve düşman sayısı çarpanı.")]
        [field: SerializeField, Range(0.01f, 1f)] public float QuickTestAmountScale { get; private set; } = 0.2f;

        // Fiyatı (hızlı test modundaysa) küçültür; en az 1.
        public int ScalePrice(int price) => QuickTestMode ? Mathf.Max(1, Mathf.RoundToInt(price * QuickTestPriceScale)) : price;

        // Miktarı (gemi malzemesi, düşman sayısı) hızlı test modunda küçültür; en az 1.
        public int ScaleAmount(int amount) => QuickTestMode ? Mathf.Max(1, Mathf.RoundToInt(amount * QuickTestAmountScale)) : amount;

#if UNITY_EDITOR
        public void EditorSetQuickTest(bool on) => QuickTestMode = on;
#endif

        [field: Header("Ses")]
        [field: SerializeField, Range(0f, 1f)] public float SfxVolume { get; private set; } = 0.8f;
        [field: SerializeField, Range(0f, 1f)] public float AmbientVolume { get; private set; } = 0.25f;

        [field: Tooltip("Arka plandaki kuş cıvıltılarının ses seviyesi.")]
        [field: SerializeField, Range(0f, 1f)] public float BirdVolume { get; private set; } = 0.12f;

        [field: Tooltip("Müziğin (final töreni) ses seviyesi.")]
        [field: SerializeField, Range(0f, 1f)] public float MusicVolume { get; private set; } = 0.9f;

        [field: Tooltip("Arka plan müziğinin ses seviyesi (döngülü, sakin).")]
        [field: SerializeField, Range(0f, 1f)] public float BackgroundMusicVolume { get; private set; } = 0.28f;

        [field: Tooltip("Kutlamalarda arka plan müziğinin kısıldığı oran (0.3 = %30), kısık kalma süresi ve kısılıp açılma hızı (saniye).")]
        [field: SerializeField, Range(0f, 1f)] public float MusicDuckLevel { get; private set; } = 0.3f;
        [field: SerializeField, Min(0f)] public float MusicDuckTime { get; private set; } = 3f;
        [field: SerializeField, Min(0.05f)] public float MusicDuckFade { get; private set; } = 0.4f;

        [field: Tooltip("Finalde arka plan müziğinin zafer müziğine geçiş süresi (saniye).")]
        [field: SerializeField, Min(0.05f)] public float MusicCrossfade { get; private set; } = 1.5f;

        [field: Tooltip("Oyuncu kaç metrede bir adım sesi çıkarır.")]
        [field: SerializeField, Min(0.1f)] public float FootstepDistance { get; private set; } = 1.6f;

        [field: Tooltip("Aynı ses bu süre (sn) içinde tekrar çalarsa perdesi biraz yükselir.")]
        [field: SerializeField, Min(0f)] public float PitchComboWindow { get; private set; } = 0.35f;

        [field: Tooltip("Her tekrarda perdenin artışı.")]
        [field: SerializeField, Min(0f)] public float PitchComboStep { get; private set; } = 0.04f;

        [field: Tooltip("Perdenin en fazla ne kadar yükselebileceği.")]
        [field: SerializeField, Min(0f)] public float PitchComboMax { get; private set; } = 0.35f;

        [field: Header("Bitkiler")]
        [field: Tooltip("Keten/saz bitkilerinin rüzgârda sallanma açısı (derece) ve hızı.")]
        [field: SerializeField, Min(0f)] public float SwayAngle { get; private set; } = 5f;
        [field: SerializeField, Min(0f)] public float SwaySpeed { get; private set; } = 1.8f;

        [field: Header("Canlılar (yengeç, martı, balık, kelebek)")]
        [field: Tooltip("Sahildeki yengeç sayısı, yürüme hızı, oyuncu bu kadar yaklaşınca kuma gömülür, gömülü kalma süresi.")]
        [field: SerializeField, Min(0)] public int CrabCount { get; private set; } = 5;
        [field: SerializeField, Min(0f)] public float CrabSpeed { get; private set; } = 0.6f;
        [field: SerializeField, Min(0f)] public float CrabFleeDistance { get; private set; } = 2.6f;
        [field: SerializeField, Min(0f)] public float CrabHideTime { get; private set; } = 4f;
        [field: Tooltip("Yengeçlerin dolaştığı kum şeridi: kıyıdan içeri en az / en çok uzaklık (metre).")]
        [field: SerializeField, Min(0f)] public float CrabBeachInner { get; private set; } = 0.6f;
        [field: SerializeField, Min(0f)] public float CrabBeachOuter { get; private set; } = 2.8f;
        [field: Tooltip("Martı sayısı, uçuş yüksekliği, hızı, daire yarıçapı; martı sesi aralığı (saniye).")]
        [field: SerializeField, Min(0)] public int GullCount { get; private set; } = 3;
        [field: SerializeField] public float GullHeightMin { get; private set; } = 8f;
        [field: SerializeField] public float GullHeightMax { get; private set; } = 11f;
        [field: SerializeField, Min(0f)] public float GullSpeed { get; private set; } = 3f;
        [field: SerializeField, Min(1f)] public float GullCircleMin { get; private set; } = 7f;
        [field: SerializeField, Min(1f)] public float GullCircleMax { get; private set; } = 13f;
        [field: SerializeField, Min(1f)] public float GullCallIntervalMin { get; private set; } = 10f;
        [field: SerializeField, Min(1f)] public float GullCallIntervalMax { get; private set; } = 20f;
        [field: Tooltip("Kelebek sayısı, uçuş yüksekliği ve hızı.")]
        [field: SerializeField, Min(0)] public int ButterflyCount { get; private set; } = 3;
        [field: SerializeField] public float ButterflyHeight { get; private set; } = 1.2f;
        [field: SerializeField, Min(0f)] public float ButterflySpeed { get; private set; } = 0.6f;
        [field: Tooltip("Balık zıplama aralığı (saniye), yüksekliği ve süresi.")]
        [field: SerializeField, Min(1f)] public float FishJumpInterval { get; private set; } = 7f;
        [field: SerializeField, Min(0f)] public float FishJumpHeight { get; private set; } = 1.2f;
        [field: SerializeField, Min(0.1f)] public float FishJumpDuration { get; private set; } = 0.8f;

        [field: Header("Dünyadaki yazılar")]
        [field: Tooltip("Bu uzaklığa (metre) kadar yazılar tam görünür.")]
        [field: SerializeField, Min(0f)] public float LabelFadeNear { get; private set; } = 9f;

        [field: Tooltip("Bu uzaklıktan sonra yazılar tamamen kaybolur.")]
        [field: SerializeField, Min(0f)] public float LabelFadeFar { get; private set; } = 15f;

        [field: Tooltip("Ekran kenarına bu kadar (ekranın oranı) yaklaşınca yazı kaybolmaya başlar.")]
        [field: SerializeField, Range(0.01f, 0.4f)] public float LabelEdgeFade { get; private set; } = 0.12f;

        [field: Tooltip("Yazıların belirme/kaybolma hızı (saniyede).")]
        [field: SerializeField, Min(0.1f)] public float LabelFadeSpeed { get; private set; } = 3f;

        [field: Header("Bina tarifleri ve makineler")]
        [field: Tooltip("Oyuncu binaya bu kadar (metre) yaklaşınca tarif balonu (3 lif → 1 halat) çıkar.")]
        [field: SerializeField, Min(0f)] public float RecipeShowDistance { get; private set; } = 5.5f;

        [field: Tooltip("Makine sesleri bu uzaklığa kadar tam, sonra azalarak duyulur.")]
        [field: SerializeField, Min(0f)] public float MachineSoundNear { get; private set; } = 4f;

        [field: Tooltip("Makine sesleri bu uzaklıktan sonra duyulmaz.")]
        [field: SerializeField, Min(0f)] public float MachineSoundFar { get; private set; } = 13f;

        [field: Tooltip("Makinenin dönme/vuruş hızı (1 = normal). Bina hızlandıkça ayrıca artar.")]
        [field: SerializeField, Min(0.1f)] public float MachineSpeed { get; private set; } = 1f;

        [field: Tooltip("Makine animasyonunun üretim fazını izleme hızı (büyük = sıkı; küçük = daha yumuşak başlar/durur).")]
        [field: SerializeField, Min(0.5f)] public float MachineFollow { get; private set; } = 5f;

        [field: Header("Karakter izi (ayak izi, yaprak, su halkası)")]
        [field: Tooltip("Ayak izi, yaprak ve su halkaları açık mı.")]
        [field: SerializeField] public bool TrailsEnabled { get; private set; } = true;

        [field: Tooltip("Aynı anda en fazla iz sayısı (havuz).")]
        [field: SerializeField, Min(0)] public int TrailMaxCount { get; private set; } = 20;

        [field: Tooltip("Ayak izinin kaybolma süresi (saniye).")]
        [field: SerializeField, Min(0.1f)] public float FootprintLifetime { get; private set; } = 3f;

        [field: Tooltip("Oyuncu kaç metrede bir iz bırakır.")]
        [field: SerializeField, Min(0.1f)] public float FootprintStep { get; private set; } = 0.55f;

        [field: Tooltip("İşçiler kaç metrede bir iz bırakır (daha seyrek).")]
        [field: SerializeField, Min(0.1f)] public float WorkerFootprintStep { get; private set; } = 1.6f;

        [field: Tooltip("Çimende her adımda yaprak/ot parçası çıkma olasılığı (0-1).")]
        [field: SerializeField, Range(0f, 1f)] public float GrassLeafChance { get; private set; } = 0.18f;

        [field: Tooltip("Kıyıya bu kadar (metre) yakınken adımlar suda halka bırakır.")]
        [field: SerializeField, Min(0f)] public float RippleCoastDistance { get; private set; } = 0.75f;

        [field: Header("Keşif: sandıklar, koleksiyon")]
        [field: Tooltip("Oyuncu sandığa bu kadar yaklaşınca kapak açılır (metre).")]
        [field: SerializeField, Min(0.5f)] public float ChestOpenDistance { get; private set; } = 1.7f;

        [field: Tooltip("Koleksiyon / altın figür eşyası bu uzaklıkta alınır (metre).")]
        [field: SerializeField, Min(0.3f)] public float CollectibleDistance { get; private set; } = 1.2f;

        [field: Tooltip("Daha önce bulunmuş sandık/eşyanın soluk tonu.")]
        [field: SerializeField] public Color FoundItemTint { get; private set; } = new Color(0.55f, 0.55f, 0.6f, 1f);

        [field: Tooltip("Koleksiyon 5/5 olunca verilen ek kristal (bir kez).")]
        [field: SerializeField, Min(0)] public int CollectionBonusCrystals { get; private set; } = 15;

        [field: Tooltip("Koleksiyon kartının ekranda kaldığı süre (saniye).")]
        [field: SerializeField, Min(0.5f)] public float CollectionPopupTime { get; private set; } = 3.2f;

        [field: Header("Yan görevler ve altın figür")]
        [field: Tooltip("Aynı anda en fazla kaç yan görev görünür.")]
        [field: SerializeField, Range(1, 3)] public int MaxActiveQuests { get; private set; } = 2;

        [field: Tooltip("Görev bitince yenisinin gelmesi için bekleme (saniye).")]
        [field: SerializeField, Min(0f)] public float QuestRefillDelay { get; private set; } = 1.4f;

        [field: Tooltip("Gemiye altın figür takılması için gereken parça sayısı.")]
        [field: SerializeField, Min(1)] public int FigurePartsNeeded { get; private set; } = 3;

        [field: Tooltip("Kaçışta kazanılan kristal; efsanevi gemiyle (altın figür) ek kristal.")]
        [field: SerializeField, Min(0)] public int EscapeCrystals { get; private set; } = 10;
        [field: SerializeField, Min(0)] public int LegendaryCrystals { get; private set; } = 20;

        [field: Header("Bina seviyeleri (1, 2, 3)")]
        [field: Tooltip("Seviyeye göre üretim hızı çarpanı.")]
        [field: SerializeField] public float[] BuildingLevelSpeed { get; private set; } = { 1f, 1.35f, 1.75f };

        [field: Tooltip("Seviyeye göre giriş/çıkış kapasitesine eklenen.")]
        [field: SerializeField] public float[] BuildingLevelCapacity { get; private set; } = { 0f, 6f, 12f };

        [field: Tooltip("Seviyeye göre binanın görünen boyu (büyür).")]
        [field: SerializeField] public float[] BuildingLevelScale { get; private set; } = { 1f, 1.1f, 1.2f };

        [field: Header("Test Menüsü (sadece Editor ve geliştirme sürümü)")]
        [field: Tooltip("Test Menüsü'ndeki \"+Para\" butonunun verdiği para.")]
        [field: SerializeField, Min(1)] public int DevMoneyAmount { get; private set; } = 1000;

        [field: Tooltip("Sınırsız para açıkken ekranda görünen para.")]
        [field: SerializeField, Min(1)] public int DevInfiniteCoins { get; private set; } = 999999;

        [field: Tooltip("Test Menüsü'ndeki oyun hızı seçenekleri.")]
        [field: SerializeField] public float[] DevSpeeds { get; private set; } = { 1f, 2f, 5f };

        [field: Tooltip("Test Menüsü'ndeki \"+Kristal\" butonunun verdiği kristal.")]
        [field: SerializeField, Min(1)] public int DevCrystalAmount { get; private set; } = 100;

        [field: Header("Ağaç/kaya yeniden büyüme")]
        [field: Tooltip("Yeniden dolma süresinin ne kadarı boyunca kütük/kırık taş yerinde kalır (0.4 = %40).")]
        [field: SerializeField, Range(0.05f, 0.8f)] public float NodeStumpStayFraction { get; private set; } = 0.4f;

        [field: Tooltip("Kütüğün yere gömülerek kaybolma süresi (saniye).")]
        [field: SerializeField, Min(0.1f)] public float NodeStumpSinkTime { get; private set; } = 0.6f;

        [field: Tooltip("Büyüme aşaması sayısı (fidan → genç ağaç → tam ağaç = 3).")]
        [field: SerializeField, Range(2, 5)] public int NodeGrowStages { get; private set; } = 3;

        [field: Tooltip("Fidanın boyu (tam ağacın oranı).")]
        [field: SerializeField, Range(0.05f, 0.8f)] public float NodeSaplingScale { get; private set; } = 0.25f;

        [field: Tooltip("Kaya yeniden belirirken yerin ne kadar altından yükselir (metre).")]
        [field: SerializeField, Min(0.1f)] public float NodeRiseDepth { get; private set; } = 1.1f;

        [field: Header("Kapalı bölge çalılığı")]
        [field: Tooltip("Çalıların rüzgârda sallanma hızı ve açısı (derece).")]
        [field: SerializeField, Min(0f)] public float ThicketSwaySpeed { get; private set; } = 1.3f;
        [field: SerializeField, Min(0f)] public float ThicketSwayAngle { get; private set; } = 3f;

        [field: Tooltip("Bölge açılınca kameranın bölgeye odaklandığı toplam süre (saniye).")]
        [field: SerializeField, Min(0.5f)] public float ThicketFocusTime { get; private set; } = 2.2f;

        [field: Tooltip("Çalıların tek tek kesilip savrulması bu süreye yayılır (saniye).")]
        [field: SerializeField, Min(0.1f)] public float ThicketCutTime { get; private set; } = 1.2f;

        [field: Tooltip("Kesilen her çalıdan saçılan yaprak sayısı.")]
        [field: SerializeField, Min(0)] public int ThicketLeaves { get; private set; } = 10;

        [field: Tooltip("Odak çekiminde kameranın yüksekliği ve bölgeden uzaklığı (metre).")]
        [field: SerializeField, Min(1f)] public float ThicketShotHeight { get; private set; } = 14f;
        [field: SerializeField, Min(1f)] public float ThicketShotDistance { get; private set; } = 12f;

        [field: Header("Köprüler: karşı kıyı ve açılış")]
        [field: Tooltip("Köprü başına gelince kamera karşı kıyıyı bu kadar gösterir (saniye; 0 = kapalı).")]
        [field: SerializeField, Min(0f)] public float BridgePreviewTime { get; private set; } = 2.4f;

        [field: Tooltip("Karşı kıyı gösterimi tekrar en erken bu kadar sonra (saniye).")]
        [field: SerializeField, Min(0f)] public float BridgePreviewCooldown { get; private set; } = 30f;

        [field: Tooltip("Köprü bitince kameranın köprü boyunca karşıya kayma süresi (saniye).")]
        [field: SerializeField, Min(0.3f)] public float BridgeRevealGlide { get; private set; } = 2.2f;

        [field: Tooltip("Karşı kıyıda nesneler zıplayarak belirdikten sonra kameranın bekleme süresi (saniye).")]
        [field: SerializeField, Min(0f)] public float BridgeRevealHold { get; private set; } = 1.8f;

        [field: Tooltip("Köprü çekimlerinde kameranın yüksekliği ve hedeften uzaklığı (metre).")]
        [field: SerializeField, Min(1f)] public float BridgeShotHeight { get; private set; } = 12f;
        [field: SerializeField, Min(1f)] public float BridgeShotDistance { get; private set; } = 11f;

        [field: Header("Tanıtım tabelaları ve yükseltme tabelaları")]
        [field: Tooltip("Oyuncu tabelaya bu kadar yaklaşınca tanıtım kartı açılır (metre).")]
        [field: SerializeField, Min(0.5f)] public float InfoCardDistance { get; private set; } = 3.4f;

        [field: Header("Kamp")]
        [field: Tooltip("Boşta kalan işçi bu kadar bekleyince ateşin başına gelir (saniye).")]
        [field: SerializeField, Min(0.5f)] public float CampRestAfter { get; private set; } = 4f;
        [field: Tooltip("Ateşin başında dinlenme süresi (saniye).")]
        [field: SerializeField, Min(0.5f)] public float CampRestTime { get; private set; } = 5f;

        [field: Header("Bölge açılma efekti")]
        [field: Tooltip("Açılma sırasının toplam süresi (kamera odak + yaprak girdabı + belirme), saniye.")]
        [field: SerializeField, Min(1f)] public float RevealTime { get; private set; } = 2.6f;
        [field: Tooltip("Odak çekiminde kameranın yüksekliği ve uzaklığı (metre).")]
        [field: SerializeField, Min(1f)] public float RevealShotHeight { get; private set; } = 14f;
        [field: SerializeField, Min(1f)] public float RevealShotDistance { get; private set; } = 12f;

        [field: Header("Yakınlık görünürlüğü")]
        [field: Tooltip("Oyuncu bu kadar yakınken binanın halkaları/ikonları tam görünür, Yükselt tabelası açılır (metre).")]
        [field: SerializeField, Min(1f)] public float InteractNear { get; private set; } = 6f;
        [field: Tooltip("Bu kadar uzakta halkalar/ikonlar en soluk ve en küçük halde (metre).")]
        [field: SerializeField, Min(2f)] public float InteractFar { get; private set; } = 13f;
        [field: Tooltip("Uzaktaki halkaların saydamlığı ve boyu (gizlenmez).")]
        [field: SerializeField, Range(0f, 1f)] public float InteractFarAlpha { get; private set; } = 0.42f;
        [field: SerializeField, Range(0.3f, 1f)] public float InteractFarScale { get; private set; } = 0.82f;

        [field: Header("Ambar (depo)")]
        [field: Tooltip("Ambar seviyesine göre kapasite (birim).")]
        [field: SerializeField] public int[] StorageCapacity { get; private set; } = { 100, 160, 240 };

        [field: Tooltip("Bırakma/alma alanında iki parça arası (saniye).")]
        [field: SerializeField, Min(0.01f)] public float StorageTransferInterval { get; private set; } = 0.07f;

        [field: Tooltip("Ambar dolu uyarısı tekrar en erken bu kadar sonra (saniye).")]
        [field: SerializeField, Min(0.1f)] public float StorageFullWarningInterval { get; private set; } = 2.5f;

        [field: Header("Deniz ve kıyı (su shader'ı)")]
        [field: Tooltip("Kıyıya vuran dalganın hızı (tur/saniye) ve kıyıdan içeri ulaştığı mesafe (0-1, kıyı dokusu ölçeğinde).")]
        [field: SerializeField, Min(0f)] public float ShoreWaveSpeed { get; private set; } = 0.12f;
        [field: SerializeField, Range(0f, 1f)] public float ShoreWaveReach { get; private set; } = 0.35f;

        [field: Tooltip("Köpük şeridinin yoğunluğu (0 = yok).")]
        [field: SerializeField, Range(0f, 1.5f)] public float ShoreFoamStrength { get; private set; } = 0.85f;

        [field: Tooltip("Islak kum izinin koyuluğu (0 = yok).")]
        [field: SerializeField, Range(0f, 1f)] public float WetSandStrength { get; private set; } = 0.35f;

        [field: Tooltip("Deniz yüzeyindeki güneş parıltılarının yoğunluğu (0 = yok) ve sıklığı.")]
        [field: SerializeField, Range(0f, 2f)] public float SeaGlintStrength { get; private set; } = 0.7f;
        [field: SerializeField, Range(0.001f, 0.2f)] public float SeaGlintDensity { get; private set; } = 0.03f;

        [field: Tooltip("Deniz yüzeyinin hafif dalgalanması (0 = düz).")]
        [field: SerializeField, Range(0f, 2f)] public float SeaRippleStrength { get; private set; } = 0.6f;
    }
}
