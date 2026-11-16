// ShipConfig.cs
// Geminin ve final töreninin TÜM sayıları (Data/Ship.asset): 5 inşa aşaması ve malzemeleri, her aşama bitince
// açılan bir sonraki hedef, suya indirme töreni (kayma, sıçrama, havai fişek, uzaklaşma).
// Hızlı test modunda malzeme miktarları GameConfig ile küçülür.

using System;
using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "Ship", menuName = "Kayıp Ada/Gemi ve Final Töreni")]
    public class ShipConfig : ScriptableObject
    {
        [Serializable]
        public class Requirement
        {
            public ResourceType Resource;
            [Min(1)] public int Amount = 10;
        }

        [Serializable]
        public class Stage
        {
            public string Name = "Aşama";
            public Requirement[] Requirements = new Requirement[0];
            [Tooltip("Bu aşama bitince geminin ne kadarı görünür (0 = hiç, 1 = tamamı).")]
            [Range(0f, 1f)] public float RevealAtEnd = 0.25f;
            [Tooltip("Bu aşama bitince açılan kilit: yeni bölge/bina/işçi karelerinin 'gereken alanı' budur.")]
            public AreaDefinition UnlockOnComplete;
            [Tooltip("Kutlamada görünen alt yazı. Örn: Palmiye Koyu açıldı!")]
            public string UnlockMessage = "";
        }

        [Header("İnşa")]
        public Stage[] Stages = new Stage[0];
        [Tooltip("Teslim alanının malzemeyi alma aralığı (saniye).")]
        [Min(0.01f)] public float DeliverInterval = 0.08f;

        [Header("Aşama kutlaması")]
        [Tooltip("\"Aşama tamamlandı!\" yazısının ekranda kalma süresi (saniye).")]
        [Min(0.1f)] public float CelebrationDuration = 2.6f;
        [Tooltip("Aşama bitince oyunun yavaşladığı oran (0.3 = %30 hız) ve süresi (gerçek saniye).")]
        [Range(0.05f, 1f)] public float StageSlowScale = 0.3f;
        [Min(0f)] public float StageSlowTime = 0.5f;
        [Tooltip("Aşama kutlamasında kameranın gemiye bakış açısı (derece), yatay açı farkı, uzaklığı ve geçiş hızı.")]
        public float StageShotPitch = 38f;
        public float StageShotYaw = 0f;
        [Min(1f)] public float StageShotDistance = 17f;
        [Min(0.1f)] public float StageShotBlend = 4f;
        [Tooltip("Kamera gemide ne kadar kalır, sonra oyuncuya döner (gerçek saniye).")]
        [Min(0f)] public float StageShotHold = 1.6f;

        [Header("Parçalar")]
        [Tooltip("Bir gemi parçasının gelme (düşme/yükselme/açılma) süresi (saniye).")]
        [Min(0.05f)] public float PieceAppearDuration = 0.45f;
        [Tooltip("Omurga ve kaburgaların düştüğü yükseklik (metre).")]
        [Min(0f)] public float PieceDropHeight = 1.8f;
        [Tooltip("Gövdenin belirme hızı (saniyede oran).")]
        [Min(0.01f)] public float HullRevealSpeed = 0.6f;
        [Tooltip("Gemi bitince inşa iskelesinin parçaları arası söküm aralığı (saniye).")]
        [Min(0f)] public float ScaffoldRemoveInterval = 0.08f;

        [Header("Final töreni")]
        [Tooltip("Gemi bitince işçilerin iskelede toplanması için beklenen süre (saniye).")]
        [Min(0f)] public float GatherTime = 3f;
        [Tooltip("Kameranın gemi etrafında dönme süresi (saniye), toplam açısı (derece), uzaklığı ve yüksekliği (metre).")]
        [Min(0f)] public float OrbitDuration = 5f;
        public float OrbitDegrees = 150f;
        [Min(1f)] public float OrbitDistance = 16f;
        public float OrbitHeight = 4.5f;
        [Tooltip("Geminin kızaktan suya kayma süresi (saniye).")]
        [Min(0.1f)] public float SlideDuration = 3f;
        [Tooltip("Kızakta kayılan mesafe (metre).")]
        [Min(0f)] public float SlideDistance = 10f;
        [Tooltip("Suya inince gemi ne kadar batar (yüksekliğinin oranı).")]
        [Range(0f, 0.5f)] public float SinkFraction = 0.14f;
        [Tooltip("Suya inen geminin burnunu açık denize çevirme süresi (saniye).")]
        [Min(0.1f)] public float TurnDuration = 2f;
        [Min(0.1f)] public float SailSpeed = 3.5f;
        [Tooltip("Uzaklaşırken kameranın gemiyi arkadan takip uzaklığı ve yüksekliği (metre).")]
        [Min(1f)] public float FollowDistance = 15f;
        public float FollowHeight = 3.5f;
        [Header("Gün batımı (final)")]
        [Tooltip("Törende ışığın gün batımına dönme süresi (saniye), güneşin rengi, gücü ve açısı; ortam ışığı renkleri.")]
        [Min(0.1f)] public float SunsetTime = 6f;
        public Color SunsetColor = new Color(1f, 0.6f, 0.35f);
        [Min(0f)] public float SunsetIntensity = 1.05f;
        public float SunsetPitch = 14f;
        public Color SunsetSky = new Color(0.62f, 0.5f, 0.68f);
        public Color SunsetEquator = new Color(0.95f, 0.62f, 0.45f);
        [Tooltip("Ekranı kaplayan konfetinin süresi (saniye).")]
        [Min(0f)] public float ConfettiDuration = 8f;
        [Tooltip("Gemi kaç saniye yelken açıp uzaklaştıktan sonra kaçış ekranı gelir.")]
        [Min(0.1f)] public float SailDuration = 7f;
        [Tooltip("Tören boyunca patlayan havai fişek sayısı.")]
        [Min(0)] public int FireworkCount = 18;
        [Tooltip("Havai fişekler arası süre (saniye).")]
        [Min(0.05f)] public float FireworkInterval = 0.55f;
    }
}
