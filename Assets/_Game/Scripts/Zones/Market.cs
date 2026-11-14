// Market.cs
// Pazar: İçerideki karakterlerin yığınındaki kaynaklar tek tek tezgaha uçar ve satılır (oyuncu ve Taşıyıcı işçiler).
// Her parça tezgaha varınca para kazanılır: para zıplar, "+5" yazısı çıkar, ses çalar.
// Tüm kaynak türlerini kabul eder; her parça kendi türünün fiyatından (ResourceType.SellPrice) satılır.

using UnityEngine;

namespace KayipAda
{
    public class Market : InteractionZone
    {
        [Tooltip("Satılan kaynakların uçtuğu nokta (tezgah).")]
        [SerializeField] private Transform dropPoint;

        private float timer;

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

        private void Sell(ResourceItem item, bool byPlayer)
        {
            int price = item.Type.SellPrice;
            Vector3 target = dropPoint != null ? dropPoint.position : transform.position;
            item.FlyToAndDestroy(target, config.ItemFlyHeight, config.ItemFlyDuration, () =>
            {
                CurrencyManager.Instance.Add(price);
                if (Effects.Instance != null) Effects.Instance.CoinPop(target, price);
                AudioManager.Play(SoundId.Sell, byPlayer ? 1f : 0.5f);
            });
        }
    }
}
