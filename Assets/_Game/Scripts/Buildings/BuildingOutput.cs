// BuildingOutput.cs
// İşleme binasının çıkış alanı. İçerideki karakterlere (oyuncu, Taşıyıcı işçi) çıkış yığınındaki
// ürünleri tek tek sırtına verir (sırtında yer olduğu sürece).

using UnityEngine;

namespace KayipAda
{
    public class BuildingOutput : InteractionZone
    {
        [SerializeField] private ProcessingBuilding building;

        private float timer;

        public ProcessingBuilding Building => building;
        public ItemPile Pile => building.OutputPile;

        private void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return;
            }

            bool moved = false;
            for (int i = 0; i < Visitors.Count; i++)
            {
                if (Pile.IsEmpty) break;

                CarryStack visitor = Visitors[i];
                if (!CanServe(visitor) || visitor.IsFull) continue;

                visitor.TryAdd(Pile.RemoveTop());
                moved = true;
            }

            if (moved) timer = config.BuildingTransferInterval;
        }
    }
}
