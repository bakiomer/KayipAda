// Market.cs
// Pazar: Karakter içerideyken sırtındaki kaynaklar tek tek alınır ve satılır.
// Tüm kaynak türlerini kabul eder; her parça kendi türünün fiyatından (ResourceType.SellPrice) paraya çevrilir.

using UnityEngine;

namespace KayipAda
{
    public class Market : InteractionZone
    {
        private float timer;

        protected override void OnVisitorEnter()
        {
            timer = 0f;
        }

        private void Update()
        {
            if (Visitor == null || Visitor.IsEmpty) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.SellInterval;

            ResourceItem item = Visitor.RemoveTop();
            CurrencyManager.Instance.Add(item.Type.SellPrice);
            item.FlyToAndDestroy(transform.position, config.SellFlyDuration);
        }
    }
}
