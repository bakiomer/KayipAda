// GameConfig.cs
// Oyunun tüm ayar sayılarını (hız, kapasite, aralıklar, kamera, joystick...) tutan ayar dosyası.
// Assets/_Game/Data/GameConfig.asset olarak bulunur; Inspector'dan değiştirilir.
// Kural: oynanışı etkileyen hiçbir sayı kodun içine yazılmaz, buraya eklenir.

using UnityEngine;
using UnityEngine.Serialization;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Kayıp Ada/Oyun Ayarları")]
    public class GameConfig : ScriptableObject
    {
        [field: Header("Oyuncu")]
        [field: Tooltip("Oyuncunun yürüme hızı (birim/saniye).")]
        [field: SerializeField, Min(0f)] public float PlayerSpeed { get; private set; } = 5f;

        [field: Tooltip("Oyuncunun sırtında taşıyabileceği en fazla kaynak sayısı.")]
        [field: SerializeField, Min(1)] public int CarryCapacity { get; private set; } = 10;

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

        // Not: Adanın boyutu ve oyuncunun yürüyebileceği alan harita yerleşimidir → sahne kurma komutunda.

        [field: Header("Bölge Açma (UnlockPad)")]
        [field: Tooltip("Kareye basınca ödemenin başlaması için beklenen süre (saniye).")]
        [field: SerializeField, Min(0f)] public float UnlockStartDelay { get; private set; } = 0.3f;

        [field: Tooltip("Para parçalarının kaç saniyede bir aktığı.")]
        [field: SerializeField, Min(0.01f)] public float UnlockPayInterval { get; private set; } = 0.05f;

        [field: Tooltip("Paran yeterliyse fiyatın tamamının kaç saniyede ödeneceği (fiyat ne olursa olsun).")]
        [field: SerializeField, Min(0.1f)] public float UnlockPayDuration { get; private set; } = 2f;

        [field: Tooltip("Para parçasının oyuncudan kareye uçma süresi (saniye).")]
        [field: SerializeField, Min(0.01f)] public float CoinFlyDuration { get; private set; } = 0.25f;

        [field: Header("Kamera")]
        [field: Tooltip("Kameranın yakınlığı: küçük değer = daha yakın.")]
        [field: SerializeField, Min(1f)] public float CameraSize { get; private set; } = 8f;

        [field: SerializeField, Min(0f)] public float CameraFollowSpeed { get; private set; } = 6f;

        [field: Tooltip("Kamera oyuncunun ne kadar yukarısına bakar (oyuncu ekranın biraz altında durur).")]
        [field: SerializeField] public float CameraOffsetY { get; private set; } = 2f;

        [field: Header("Joystick")]
        [field: Tooltip("Joystick topunun merkezden en fazla uzaklaşabileceği mesafe (UI birimi).")]
        [field: SerializeField, Min(1f)] public float JoystickRange { get; private set; } = 110f;

        [field: Tooltip("Bu değerin altındaki küçük parmak hareketleri yok sayılır (0-1).")]
        [field: SerializeField, Range(0f, 0.9f)] public float JoystickDeadZone { get; private set; } = 0.1f;

        [field: Header("Yığın ve Efektler")]
        [field: Tooltip("Sırttaki kaynakların üst üste aralığı.")]
        [field: SerializeField, Min(0f)] public float StackSpacing { get; private set; } = 0.22f;

        [field: Tooltip("Kaynakların yığındaki yerine uçma hızı.")]
        [field: SerializeField, Min(0.1f)] public float ItemFlySpeed { get; private set; } = 15f;

        [field: Tooltip("Satılan kaynağın satış alanına uçma süresi (saniye).")]
        [field: SerializeField, Min(0.01f)] public float SellFlyDuration { get; private set; } = 0.25f;

        [field: Tooltip("Kaynak verirken noktanın büyüme oranı (1 = efekt yok).")]
        [field: SerializeField, Min(1f)] public float NodeBounceScale { get; private set; } = 1.15f;

        [field: Tooltip("Kaynak noktası görsel değişimlerinin (zıplama, küçülme, solma) hızı.")]
        [field: FormerlySerializedAs("<NodeBounceRecover>k__BackingField")]
        [field: SerializeField, Min(0.1f)] public float NodeAnimSpeed { get; private set; } = 10f;

        [field: Tooltip("Tükenen kaynak noktasının küçüleceği oran (0.5 = yarı boyut).")]
        [field: SerializeField, Range(0.1f, 1f)] public float NodeDepletedScale { get; private set; } = 0.55f;

        [field: Tooltip("Tükenen kaynak noktasının saydamlığı (0 = görünmez, 1 = tam görünür).")]
        [field: SerializeField, Range(0f, 1f)] public float NodeDepletedAlpha { get; private set; } = 0.35f;
    }
}
