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

        [Header("Final töreni")]
        [Tooltip("Gemi bitince işçilerin iskelede toplanması için beklenen süre (saniye).")]
        [Min(0f)] public float GatherTime = 2.5f;
        [Tooltip("Geminin kızaktan suya kayma süresi (saniye).")]
        [Min(0.1f)] public float SlideDuration = 3f;
        [Tooltip("Kızakta kayılan mesafe (metre).")]
        [Min(0f)] public float SlideDistance = 9f;
        [Tooltip("Suya inince gemi ne kadar batar (yüksekliğinin oranı).")]
        [Range(0f, 0.5f)] public float SinkFraction = 0.14f;
        [Min(0.1f)] public float SailSpeed = 3.5f;
        [Tooltip("Gemi kaç saniye yelken açıp uzaklaştıktan sonra kaçış ekranı gelir.")]
        [Min(0.1f)] public float SailDuration = 6f;
        [Tooltip("Tören boyunca patlayan havai fişek sayısı.")]
        [Min(0)] public int FireworkCount = 10;
        [Tooltip("Havai fişekler arası süre (saniye).")]
        [Min(0.05f)] public float FireworkInterval = 0.45f;
    }
}
