// BuildingInput.cs
// İşleme binasının giriş alanı. İçerideki karakterlerin (oyuncu, Oduncu işçi) sırtından SADECE binanın
// kabul ettiği kaynağı tek tek alır ve giriş yığınına koyar; diğer kaynaklar sırtta kalır.
// Giriş yığını doluysa almayı bırakır (getiren bekler).

using UnityEngine;

namespace KayipAda
{
    public class BuildingInput : InteractionZone
    {
        [SerializeField] private ProcessingBuilding building;

        private float timer;

        public ProcessingBuilding Building => building;

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
                if (building.InputPile.IsFull) break;

                CarryStack visitor = Visitors[i];
                if (!CanServe(visitor)) continue;

                // Yığının içinden bu binanın kabul ettiği türden en üsttekini al (yoksa null).
                ResourceItem item = visitor.RemoveTop(building.Type.Input);
                if (item == null) continue;

                building.InputPile.TryAdd(item);
                moved = true;
                if (visitor.IsPlayer) AudioManager.Play(SoundId.Drop, 0.5f);
            }

            if (moved) timer = config.BuildingTransferInterval;
        }
    }
}
