// ShipDelivery.cs
// Geminin yanındaki teslim alanı. İçerideki oyuncunun (ve gemiyi hedefleyen Taşıyıcı işçilerin) yığınından,
// geminin şu anki aşamasında gereken malzemeleri tek tek alır ve gemiye uçurur.

using UnityEngine;

namespace KayipAda
{
    public class ShipDelivery : InteractionZone
    {
        [SerializeField] private ShipBuilder builder;

        private float timer;

        private void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return;
            }
            if (builder.IsComplete) return;

            bool moved = false;
            for (int i = 0; i < Visitors.Count; i++)
            {
                CarryStack visitor = Visitors[i];
                if (!CanServe(visitor)) continue;

                ResourceItem item = TakeNeeded(visitor);
                if (item == null) continue;
                builder.Deliver(item);
                moved = true;
                if (builder.IsComplete) break;
            }

            if (moved) timer = builder.Config.DeliverInterval;
        }

        // Yığından geminin ihtiyaç duyduğu ilk türü çıkarır (yoksa null).
        private ResourceItem TakeNeeded(CarryStack stack)
        {
            foreach (ShipConfig.Requirement req in builder.Config.Stages[builder.StageIndex].Requirements)
            {
                if (!builder.Needs(req.Resource)) continue;
                ResourceItem item = stack.RemoveTop(req.Resource);
                if (item != null) return item;
            }
            return null;
        }
    }
}
