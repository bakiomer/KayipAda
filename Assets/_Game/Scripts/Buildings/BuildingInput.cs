// BuildingInput.cs
// İşleme binasının giriş alanı. Karakter içerideyken sırtından SADECE binanın kabul ettiği
// kaynağı tek tek alır ve giriş yığınına koyar; diğer kaynaklar sırtta kalır.
// Giriş yığını doluysa almayı bırakır.

using UnityEngine;

namespace KayipAda
{
    public class BuildingInput : InteractionZone
    {
        [SerializeField] private ProcessingBuilding building;

        private float timer;

        protected override void OnVisitorEnter()
        {
            timer = 0f;
        }

        private void Update()
        {
            if (Visitor == null || building.InputPile.IsFull) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.BuildingTransferInterval;

            // Yığının içinden bu binanın kabul ettiği türden en üsttekini al (yoksa null).
            ResourceItem item = Visitor.RemoveTop(building.Type.Input);
            if (item == null) return;

            building.InputPile.TryAdd(item);
        }
    }
}
