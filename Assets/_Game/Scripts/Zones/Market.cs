// Market.cs
// Pazar: İçerideki karakterlerin sırtındaki kaynaklar tek tek alınır ve satılır (oyuncu ve Taşıyıcı işçiler).
// Tüm kaynak türlerini kabul eder; her parça kendi türünün fiyatından (ResourceType.SellPrice) paraya çevrilir.

using UnityEngine;

namespace KayipAda
{
    public class Market : InteractionZone
    {
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

                ResourceItem item = visitor.RemoveTop();
                CurrencyManager.Instance.Add(item.Type.SellPrice);
                item.FlyToAndDestroy(transform.position, config.SellFlyDuration);
                sold = true;
            }

            if (sold) timer = config.SellInterval;
        }
    }
}
