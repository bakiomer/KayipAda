// ResourceNode.cs
// Kaynak noktası: ağaç, kaya ve meyve ağacı aynı bileşeni kullanır; farkı ResourceNodeType ayar dosyasıdır.
// Karakter alanın içindeyken aralıklarla kaynak verir: her toplamada nokta sallanır ve sıkışıp genişler,
// kaynak karakterin yığınına kavis çizerek uçar. Stok bitince nokta küçülür, süre dolunca yeniden büyür.

using UnityEngine;

namespace KayipAda
{
    public class ResourceNode : InteractionZone
    {
        [SerializeField] private ResourceNodeType nodeType;
        [Tooltip("Sallanma/sıkışma/küçülme efektlerinin uygulandığı görsel kök (model).")]
        [SerializeField] private Transform visual;
        [Tooltip("Kaynağın çıktığı yükseklik (metre).")]
        [SerializeField] private float spawnHeight = 1.2f;
        [Tooltip("İşçinin durduğu yerin merkezden uzaklığı (gövdenin içine girmesin).")]
        [SerializeField] private float standRadius = 1.1f;
        [Tooltip("Her toplamada saçılan parçacıklar (yaprak, taş kırıntısı). Boş olabilir.")]
        [SerializeField] private ParticleSystem hitEffect;
        [Tooltip("Her vuruşta kaç parçacık saçılır.")]
        [SerializeField] private int hitParticles = 5;

        public int Stock { get; private set; }
        public bool IsDepleted => Stock <= 0;

        private float gatherTimer;
        private float regrowTimer;
        private float size = 1f;
        private float hitTimer;
        private Vector3 shakeAxis = Vector3.right;
        private Vector3 visualBaseScale;
        private Quaternion visualBaseRotation;

        private void Awake()
        {
            if (nodeType == null || nodeType.Resource == null || nodeType.Resource.ItemPrefab == null || visual == null)
            {
                Debug.LogError(name + ": Node Type, kaynağı, kaynağın modeli veya Visual atanmamış!", this);
                enabled = false; // Hata yağmuru olmasın diye bu noktayı kapat
                return;
            }

            visualBaseScale = visual.localScale;
            visualBaseRotation = visual.localRotation;
            Stock = nodeType.Stock;
        }

        // İşçi gövdenin içine değil, kendi tarafındaki kenara gider.
        public override Vector3 GetStandPoint(Vector3 from)
        {
            Vector3 dir = from - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.back;
            return transform.position + dir.normalized * standRadius;
        }

        private void Update()
        {
            UpdateRegrow();
            UpdateGather();
            UpdateVisual();
        }

        private void UpdateRegrow()
        {
            if (!IsDepleted) return;

            regrowTimer -= Time.deltaTime;
            if (regrowTimer > 0f) return;

            Stock = nodeType.Stock;
            Hit(); // dolunca küçük bir zıplama
        }

        private void UpdateGather()
        {
            if (IsDepleted) return;

            if (gatherTimer > 0f)
            {
                gatherTimer -= Time.deltaTime;
                return;
            }

            // Her aralıkta içerideki her karaktere (oyuncu, Oduncu işçi) birer kaynak ver.
            bool gave = false;
            bool playerGot = false;
            for (int i = 0; i < Visitors.Count && !IsDepleted; i++)
            {
                CarryStack visitor = Visitors[i];
                if (!CanServe(visitor) || visitor.IsFull) continue;

                ResourceItem item = nodeType.Resource.Spawn(transform.position + Vector3.up * spawnHeight);
                visitor.TryAdd(item);
                Stock--;
                gave = true;
                playerGot |= visitor.IsPlayer;
            }

            if (!gave) return;
            gatherTimer = nodeType.GatherInterval;
            Hit();
            if (playerGot) AudioManager.Play(nodeType.HitSound, 0.6f);
            if (hitEffect != null) hitEffect.Emit(hitParticles);

            if (IsDepleted) regrowTimer = nodeType.RegrowTime;
        }

        private void Hit()
        {
            hitTimer = config.NodeHitDuration;
            shakeAxis = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.right;
        }

        // Boyut (tükenince küçülür) + vuruş sallanması ve sıkışması.
        private void UpdateVisual()
        {
            float targetSize = IsDepleted ? config.NodeDepletedScale : 1f;
            size = Mathf.Lerp(size, targetSize, 1f - Mathf.Exp(-8f * Time.deltaTime));

            float wave = 0f;
            if (hitTimer > 0f)
            {
                hitTimer -= Time.deltaTime;
                float k = 1f - Mathf.Clamp01(hitTimer / config.NodeHitDuration);
                wave = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k);
            }

            float squash = wave * config.NodeSquash;
            visual.localScale = new Vector3(visualBaseScale.x * size * (1f - squash * 0.6f),
                                            visualBaseScale.y * size * (1f + squash),
                                            visualBaseScale.z * size * (1f - squash * 0.6f));
            visual.localRotation = visualBaseRotation * Quaternion.AngleAxis(wave * config.NodeShakeAngle, shakeAxis);
        }
    }
}
