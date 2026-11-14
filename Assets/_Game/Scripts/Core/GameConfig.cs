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
        // Not: İşçi hızı, kapasitesi ve sayısı her göreve özeldir → Data/Workers; tutma fiyatı → Data/Areas (Hire...)

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
        [field: SerializeField, Range(20f, 89f)] public float CameraPitch { get; private set; } = 50f;

        [field: Tooltip("Kameranın yatay dönüşü (derece). 0 = kamera adanın güneyinden kuzeye bakar.")]
        [field: SerializeField] public float CameraYaw { get; private set; } = 0f;

        [field: Tooltip("Görüş açısı (derece). Dar açı (30-35) = daha az perspektif bozulması.")]
        [field: SerializeField, Range(10f, 80f)] public float CameraFov { get; private set; } = 32f;

        [field: Tooltip("Kameranın baktığı noktaya uzaklığı (metre). Büyük = daha geniş görüş.")]
        [field: SerializeField, Min(1f)] public float CameraDistance { get; private set; } = 34f;

        [field: Tooltip("Kamera oyuncunun ne kadar önüne bakar (metre). Oyuncu ekranın biraz altında durur.")]
        [field: SerializeField] public float CameraFocusOffset { get; private set; } = 3.5f;

        [field: SerializeField, Min(0f)] public float CameraFollowSpeed { get; private set; } = 6f;

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

        [field: Tooltip("Tükenen kaynak noktasının küçüleceği oran (0.5 = yarı boyut).")]
        [field: SerializeField, Range(0.05f, 1f)] public float NodeDepletedScale { get; private set; } = 0.35f;

        [field: Tooltip("Açılan nesnelerin zıplayarak belirme süresi (saniye).")]
        [field: SerializeField, Min(0.01f)] public float PopInDuration { get; private set; } = 0.45f;

        [field: Tooltip("Açılan nesneler arasındaki sıra gecikmesi (saniye).")]
        [field: SerializeField, Min(0f)] public float PopInStagger { get; private set; } = 0.04f;

        [field: Tooltip("Para göstergesinin her artışta ne kadar büyüdüğü (0.2 = %20).")]
        [field: SerializeField, Min(0f)] public float CoinPunch { get; private set; } = 0.2f;

        [field: Tooltip("Uçan '+5' yazısının ömrü (saniye).")]
        [field: SerializeField, Min(0.1f)] public float FloatingTextDuration { get; private set; } = 0.9f;

        [field: Header("Yönlendirme (sonraki hedef)")]
        [field: Tooltip("Sonraki açma karesinin üstündeki okun yüksekliği (metre).")]
        [field: SerializeField, Min(0f)] public float GuideArrowHeight { get; private set; } = 2.6f;

        [field: Tooltip("Okun aşağı yukarı sallanma miktarı (metre).")]
        [field: SerializeField, Min(0f)] public float GuideArrowBob { get; private set; } = 0.3f;

        [field: Tooltip("Sonraki hedef karesinin parlama/nabız hızı.")]
        [field: SerializeField, Min(0f)] public float GuidePulseSpeed { get; private set; } = 4f;

        [field: Header("Kaptanlık (kalıcı seviye)")]
        [field: Tooltip("Kaptanlık seviyesi başına hız/kapasite bonusu (0.03 = %3).")]
        [field: SerializeField, Min(0f)] public float CaptainBonusPerLevel { get; private set; } = 0.03f;

        [field: Tooltip("Bonusun hesaba katıldığı en yüksek kaptanlık seviyesi.")]
        [field: SerializeField, Min(0)] public int CaptainMaxLevel { get; private set; } = 10;

        [field: Tooltip("İlk kaçışta kazanılan kaptanlık XP'si.")]
        [field: SerializeField, Min(0)] public int EscapeXp { get; private set; } = 100;

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

        [field: Tooltip("Oyuncu kaç metrede bir adım sesi çıkarır.")]
        [field: SerializeField, Min(0.1f)] public float FootstepDistance { get; private set; } = 1.6f;

        [field: Tooltip("Aynı ses bu süre (sn) içinde tekrar çalarsa perdesi biraz yükselir.")]
        [field: SerializeField, Min(0f)] public float PitchComboWindow { get; private set; } = 0.35f;

        [field: Tooltip("Her tekrarda perdenin artışı.")]
        [field: SerializeField, Min(0f)] public float PitchComboStep { get; private set; } = 0.04f;

        [field: Tooltip("Perdenin en fazla ne kadar yükselebileceği.")]
        [field: SerializeField, Min(0f)] public float PitchComboMax { get; private set; } = 0.35f;
    }
}
