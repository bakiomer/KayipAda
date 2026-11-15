// ResourceNode.cs
// Kaynak noktası: ağaç, kaya ve meyve ağacı aynı bileşeni kullanır; farkı ResourceNodeType ayar dosyasıdır.
// Karakter alanın içindeyken aralıklarla kaynak verir: her toplamada nokta sallanır ve sıkışıp genişler,
// kaynak karakterin yığınına kavis çizerek uçar. Toplandıkça nokta küçülür; tükenince yerinde kütük (kaya için
// kırık taşlar) kalır, yeniden dolarken kütükten büyüyerek eski boyuna döner.

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
        [Tooltip("Stok azaldıkça tek tek kaybolan, dolarken tek tek geri gelen parçalar (çalıdaki meyveler). Boş olabilir.")]
        [SerializeField] private Transform[] stockVisuals = new Transform[0];
        [Tooltip("Toplandıkça nokta küçülsün mü? (Meyve çalısı küçülmez, sadece meyveleri azalır.)")]
        [SerializeField] private bool shrinks = true;
        [Tooltip("Küçülürken sadece boyu mu kısalsın? (Keten/saz tarlası)")]
        [SerializeField] private bool shrinkHeightOnly;
        [Tooltip("Tükenince yerinde kalan parça (kütük, kırık taşlar). Boş olabilir: o zaman nokta sadece küçülür.")]
        [SerializeField] private GameObject remnant;
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
            if (remnant != null) Tween.PopOut(remnant.transform, config.PopInDuration, () => remnant.SetActive(false));
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

            if (IsDepleted)
            {
                regrowTimer = nodeType.RegrowTime;
                if (remnant != null)
                {
                    remnant.SetActive(true);
                    Tween.PopIn(remnant.transform, Vector3.one, config.PopInDuration);
                }
            }
        }

        // Görünmesi gereken boyut: stok azaldıkça küçülür; tükenince kütük kalır (ya da küçük kalır),
        // yeniden dolma süresince büyüyerek eski boyuna döner.
        private float TargetSize()
        {
            if (!shrinks) return 1f;
            float empty = remnant != null ? 0f : config.NodeDepletedScale;
            if (IsDepleted)
            {
                float grown = 1f - Mathf.Clamp01(regrowTimer / Mathf.Max(0.01f, nodeType.RegrowTime));
                return Mathf.Lerp(empty, config.NodeMinStockScale, grown);
            }
            float left = nodeType.Stock <= 1 ? 1f : (Stock - 1f) / (nodeType.Stock - 1f);
            return Mathf.Lerp(config.NodeMinStockScale, 1f, left);
        }

        private void Hit()
        {
            hitTimer = config.NodeHitDuration;
            shakeAxis = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.right;
        }

        // Boyut (tükenince küçülür) + vuruş sallanması ve sıkışması.
        private void UpdateVisual()
        {
            size = Mathf.Lerp(size, TargetSize(), 1f - Mathf.Exp(-8f * Time.deltaTime));
            UpdateStockVisuals();
            bool visible = size > 0.02f;
            if (visual.gameObject.activeSelf != visible) visual.gameObject.SetActive(visible);

            float wave = 0f;
            if (hitTimer > 0f)
            {
                hitTimer -= Time.deltaTime;
                float k = 1f - Mathf.Clamp01(hitTimer / config.NodeHitDuration);
                wave = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k);
            }

            float squash = wave * config.NodeSquash;
            float wide = shrinkHeightOnly ? 1f : size;
            visual.localScale = new Vector3(visualBaseScale.x * wide * (1f - squash * 0.6f),
                                            visualBaseScale.y * size * (1f + squash),
                                            visualBaseScale.z * wide * (1f - squash * 0.6f));
            visual.localRotation = visualBaseRotation * Quaternion.AngleAxis(wave * config.NodeShakeAngle, shakeAxis);
        }

        // Meyveler: stokla orantılı sayıda görünür; toplandıkça tek tek kaybolur, dolarken tek tek belirir.
        private int visibleStock = -1;
        private Vector3[] stockScales;

        private void UpdateStockVisuals()
        {
            if (stockVisuals == null || stockVisuals.Length == 0) return;
            if (stockScales == null)
            {
                stockScales = new Vector3[stockVisuals.Length];
                for (int i = 0; i < stockVisuals.Length; i++) stockScales[i] = stockVisuals[i].localScale;
            }
            int n = stockVisuals.Length;
            int want = IsDepleted
                ? Mathf.FloorToInt((1f - Mathf.Clamp01(regrowTimer / Mathf.Max(0.01f, nodeType.RegrowTime))) * n)
                : Mathf.CeilToInt((float)Stock / Mathf.Max(1, nodeType.Stock) * n);
            if (want == visibleStock) return;
            for (int i = 0; i < n; i++)
            {
                Transform fruit = stockVisuals[i];
                bool show = i < want;
                if (fruit.gameObject.activeSelf == show) continue;
                if (show)
                {
                    fruit.gameObject.SetActive(true);
                    if (visibleStock >= 0) Tween.PopIn(fruit, stockScales[i], config.PopInDuration);
                }
                else if (visibleStock >= 0)
                {
                    Tween.PopOut(fruit, 0.15f, () => fruit.gameObject.SetActive(false));
                }
                else
                {
                    fruit.gameObject.SetActive(false);
                }
            }
            visibleStock = want;
        }
    }
}
