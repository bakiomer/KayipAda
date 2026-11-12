// BuildingOutput.cs
// İşleme binasının çıkış alanı. Karakter içerideyken çıkış yığınındaki ürünleri
// tek tek sırtına verir (sırtında yer olduğu sürece).

using UnityEngine;

namespace KayipAda
{
    public class BuildingOutput : InteractionZone
    {
        [SerializeField] private ProcessingBuilding building;

        private float timer;

        protected override void OnVisitorEnter()
        {
            timer = 0f;
        }

        private void Update()
        {
            if (Visitor == null || Visitor.IsFull || building.OutputPile.IsEmpty) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.BuildingTransferInterval;

            Visitor.TryAdd(building.OutputPile.RemoveTop());
        }
    }
}
