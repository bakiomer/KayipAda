// ShipConfig.cs
// Gemi, son gece ve kaçışın TÜM sayıları (Data/Ship.asset): inşa aşamaları ve malzemeleri, düşman dalgaları,
// gemi canı, toplar, gece ışığı ve kaçış. Hızlı test modunda malzeme ve düşman sayıları GameConfig ile küçülür.

using System;
using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "Ship", menuName = "Kayıp Ada/Gemi ve Son Gece")]
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
        }

        [Header("İnşa")]
        public Stage[] Stages = new Stage[0];
        [Tooltip("Teslim alanının malzemeyi alma aralığı (saniye).")]
        [Min(0.01f)] public float DeliverInterval = 0.08f;
        [Tooltip("İlk aşama (omurga) bitince açılan alan: topların açma kareleri bunu bekler.")]
        public AreaDefinition KeelDoneArea;

        [Header("Son gece: dalgalar")]
        [Tooltip("Her dalgadaki iskelet sayısı (dizi uzunluğu = dalga sayısı).")]
        public int[] WaveSizes = { 5, 8, 11 };
        [Min(0.1f)] public float SpawnInterval = 1.4f;
        [Tooltip("Dalgalar arası bekleme (saniye).")]
        [Min(0f)] public float TimeBetweenWaves = 6f;
        [Tooltip("Gece başlamadan önceki kısa hazırlık (saniye).")]
        [Min(0f)] public float NightStartDelay = 4f;

        [Header("Düşman")]
        [Min(0.1f)] public float EnemySpeed = 1.8f;
        [Min(1)] public int EnemyHealth = 3;
        [Tooltip("Gemiye saniyede verdiği hasar.")]
        [Min(0f)] public float EnemyDps = 3f;
        [Tooltip("Gemiye bu kadar yaklaşınca saldırır (metre).")]
        [Min(0.1f)] public float EnemyAttackRange = 1.6f;

        [Header("Gemi canı")]
        [Min(1f)] public float ShipHealth = 100f;

        [Header("Toplar")]
        [Min(1)] public int CannonAmmoCapacity = 10;
        [Tooltip("Toplar başta kaç gülleyle gelir.")]
        [Min(0)] public int CannonStartAmmo = 4;
        [Tooltip("Gülle yapılan kaynak (taş).")]
        public ResourceType AmmoResource;
        [Min(0.1f)] public float CannonFireInterval = 1.6f;
        [Min(1f)] public float CannonRange = 15f;
        [Min(1)] public int CannonDamage = 2;
        [Tooltip("Patlamanın etki yarıçapı (metre).")]
        [Min(0.1f)] public float ExplosionRadius = 2f;
        [Min(0.1f)] public float CannonBallFlightTime = 0.9f;
        [Min(0f)] public float CannonBallArc = 4f;
        [Tooltip("Topa taş yükleme aralığı (saniye).")]
        [Min(0.01f)] public float CannonLoadInterval = 0.15f;

        [Header("Gece görünümü")]
        public Color NightSunColor = new Color(0.55f, 0.62f, 0.95f);
        [Min(0f)] public float NightSunIntensity = 0.45f;
        public Color NightAmbientSky = new Color(0.20f, 0.24f, 0.42f);
        public Color NightAmbientEquator = new Color(0.22f, 0.20f, 0.32f);
        public Color NightAmbientGround = new Color(0.10f, 0.10f, 0.14f);
        public Color NightFog = new Color(0.10f, 0.14f, 0.28f);
        [Tooltip("Gündüz ↔ gece geçiş süresi (saniye).")]
        [Min(0.1f)] public float DayNightTransition = 3f;

        [Header("Kaçış")]
        [Min(0.1f)] public float SailSpeed = 3.5f;
        [Tooltip("Gemi kaç saniye yelken açıp uzaklaştıktan sonra kaçış ekranı gelir.")]
        [Min(0.1f)] public float SailDuration = 7f;
    }
}
