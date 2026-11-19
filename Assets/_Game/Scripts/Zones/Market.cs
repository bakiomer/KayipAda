// Market.cs
// Pazar: İçerideki karakterlerin yığınındaki kaynaklar tek tek tezgaha uçar ve satılır (oyuncu ve Taşıyıcı işçiler).
// Her parça tezgaha varınca para kazanılır: para zıplar, "+5" yazısı çıkar, ses çalar; para kasaya uçar (kasa kapağı
// açılıp kapanır) ve satılan kaynağın tezgahtaki yığını kısa süre büyür. Tüm kaynak türlerini kabul eder; her parça
// kendi türünün fiyatından (ResourceType.SellPrice) satılır. Sayılar GameConfig > "Pazar".

using UnityEngine;

namespace KayipAda
{
    public class Market : InteractionZone
    {
        [Tooltip("Satılan kaynakların uçtuğu nokta (tezgah).")]
        [SerializeField] private Transform dropPoint;
        [Tooltip("Paraların uçtuğu kasa (sandık) ve açılan kapağı (menteşe noktası).")]
        [SerializeField] private Transform cashBox;
        [SerializeField] private Transform cashLid;
        [Tooltip("Tezgahtaki ürün yığınları: kaynak kimliği (wood, stone...) ve yığın. Satılan türün yığını kısa süre büyür.")]
        [SerializeField] private string[] displayIds = new string[0];
        [SerializeField] private Transform[] displays = new Transform[0];
        [Tooltip("Kimliği listede olmayan kaynaklar için yığın.")]
        [SerializeField] private Transform defaultDisplay;

        private float timer;
        private float lidOpenUntil;
        private float lidAngle;
        private Vector3[] baseScales;
        private float[] boosts;

        // Herhangi bir parça satılınca (yan görevler): tür, oyuncu mu sattı.
        public static event System.Action<ResourceType, bool> Sold;

        public float LidAngle => lidAngle;
        public int SoldCount { get; private set; }

        private void Awake()
        {
            baseScales = new Vector3[displays.Length];
            boosts = new float[displays.Length];
            for (int i = 0; i < displays.Length; i++) if (displays[i] != null) baseScales[i] = displays[i].localScale;
        }

        private void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return;
            }

            // Her aralıkta içerideki her karakterden birer parça satılır.
            bool sold = false;
            for (int i = 0; i < Visitors.Count; i++)
            {
                CarryStack visitor = Visitors[i];
                if (!CanServe(visitor) || visitor.IsEmpty) continue;

                Sell(visitor.RemoveTop(), visitor.IsPlayer);
                sold = true;
            }

            if (sold) timer = config.SellInterval;
        }

        // Kasa kapağı ve ürün yığınları (satıştan bağımsız, her karede yumuşakça).
        private void LateUpdate()
        {
            if (cashLid != null)
            {
                float target = Time.time < lidOpenUntil ? config.MarketLidAngle : 0f;
                lidAngle = Mathf.MoveTowards(lidAngle, target, config.MarketLidSpeed * Time.deltaTime);
                cashLid.localRotation = Quaternion.Euler(-lidAngle, 0f, 0f);
            }
            for (int i = 0; i < displays.Length; i++)
            {
                if (displays[i] == null || boosts[i] <= 0f) continue;
                boosts[i] = Mathf.Max(0f, boosts[i] - Time.deltaTime / Mathf.Max(0.05f, config.MarketPileGrowTime));
                float k = Mathf.Sin(Mathf.Min(1f, boosts[i] * 1.6f) * Mathf.PI * 0.5f); // hızlı büyür, yavaş iner
                displays[i].localScale = baseScales[i] * (1f + config.MarketPileGrow * k);
            }
        }

        private void Sell(ResourceItem item, bool byPlayer)
        {
            ResourceType type = item.Type;
            int price = type.SellPrice;
            Sold?.Invoke(type, byPlayer);
            SoldCount++;
            Vector3 target = dropPoint != null ? dropPoint.position : transform.position;
            item.FlyToAndDestroy(target, config.ItemFlyHeight, config.ItemFlyDuration, () =>
            {
                CurrencyManager.Instance.Add(price);
                if (Effects.Instance != null)
                {
                    Effects.Instance.CoinPop(target, price);
                    if (cashBox != null)
                    {
                        lidOpenUntil = Time.time + config.MarketLidOpenTime;
                        Effects.Instance.FlyCoin(target + Vector3.up * 0.6f, cashBox.position + Vector3.up * 0.25f,
                                                 () => { if (cashBox != null) Tween.PunchScale(cashBox, 0.12f, 0.2f); });
                    }
                }
                Boost(type);
                AudioManager.Play(SoundId.Sell, byPlayer ? 1f : 0.5f);
            });
        }

        // Satılan kaynağın tezgahtaki yığını kısa süre büyür.
        private void Boost(ResourceType type)
        {
            int index = -1;
            for (int i = 0; i < displayIds.Length && i < displays.Length; i++)
                if (type != null && displayIds[i] == type.Id) { index = i; break; }
            if (index < 0 && defaultDisplay != null)
                for (int i = 0; i < displays.Length; i++) if (displays[i] == defaultDisplay) { index = i; break; }
            if (index >= 0 && boosts != null) boosts[index] = 1f;
        }

        // Testler için: türün yığını şu an büyümüş mü?
        public bool IsDisplayBoosted(string id)
        {
            for (int i = 0; i < displayIds.Length && i < displays.Length; i++)
                if (displayIds[i] == id) return boosts != null && boosts[i] > 0f;
            return false;
        }
    }
}
