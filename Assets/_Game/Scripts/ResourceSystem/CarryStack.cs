// CarryStack.cs
// Bir karakterin (oyuncu ya da işçi) önünde taşıdığı kaynak yığını, 3D.
// Kaynaklar yığına kavis çizerek uçar, sonra üst üste dizilir. Her kat bir alttakini biraz gecikmeyle
// takip eder: karakter yürürken yığın hafifçe sallanır (üst katlar daha çok).
// Farklı türler karışık dizilebilir. Kapasiteyi sahibi belirler (oyuncu: GameConfig, işçi: WorkerType).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class CarryStack : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Yığının başladığı nokta (karakterin göğsünün önü). Karakterle birlikte döner.")]
        [SerializeField] private Transform stackRoot;

        private readonly List<ResourceItem> items = new List<ResourceItem>();

        public int Capacity { get; set; }
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public bool IsEmpty => items.Count == 0;

        // Oyuncunun yığını mı? (Para ödemek gibi işleri sadece oyuncu yapar.) PlayerController ayarlar.
        public bool IsPlayer { get; set; }

        // İşçiler için: şu an iş yaptığı alan. Alanlar işçiye sadece hedefiyse hizmet eder.
        public InteractionZone WorkTarget { get; set; }

        // Yer varsa kaynağı yığının en üstüne ekler (kaynak yerine uçar).
        public bool TryAdd(ResourceItem item)
        {
            if (IsFull || item == null) return false;

            item.transform.SetParent(null, true); // dünyada serbest: gecikmeli takip için
            items.Add(item);
            item.FlyTo(() => IdealSlot(item), config.ItemFlyHeight, config.ItemFlyDuration);
            if (IsPlayer) AudioManager.Play(SoundId.Collect);
            return true;
        }

        // En üstteki kaynağı (türü fark etmeksizin) çıkarır; boşsa null döner.
        public ResourceItem RemoveTop()
        {
            if (IsEmpty) return null;
            return RemoveAt(items.Count - 1);
        }

        // Belirli türden en üstteki kaynağı çıkarır; o türden yoksa null döner.
        public ResourceItem RemoveTop(ResourceType type)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].Type == type) return RemoveAt(i);
            }
            return null;
        }

        // Yığında bu türden kaç tane olduğunu sayar.
        public int CountOf(ResourceType type)
        {
            int count = 0;
            foreach (ResourceItem item in items)
            {
                if (item.Type == type) count++;
            }
            return count;
        }

        private ResourceItem RemoveAt(int index)
        {
            ResourceItem item = items[index];
            items.RemoveAt(index);
            return item;
        }

        // Parçanın yığındaki düz (sallanmasız) yeri; uçan parçalar buraya iner.
        private Vector3 IdealSlot(ResourceItem item)
        {
            int index = Mathf.Max(0, items.IndexOf(item));
            return stackRoot.position + stackRoot.up * (index * config.StackSpacing);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < items.Count; i++)
            {
                ResourceItem item = items[i];
                if (item == null || item.IsFlying) continue;

                // Her kat kendi düz yerine gider; yukarı çıktıkça biraz daha geç kalır → hafif sallanma.
                Vector3 ideal = stackRoot.position + stackRoot.up * (i * config.StackSpacing);
                float sharpness = config.StackFollowSharpness / (1f + i * config.StackSwayPerLevel);
                float blend = 1f - Mathf.Exp(-sharpness * dt);

                Transform t = item.transform;
                Vector3 pos = Vector3.Lerp(t.position, ideal, blend);

                // Yığın fazla eğilmesin: her kat düz duruştan en fazla belli bir mesafe kayabilir.
                Vector3 offset = pos - ideal;
                float maxLean = config.StackMaxLeanPerLevel * (i + 1);
                if (offset.sqrMagnitude > maxLean * maxLean) pos = ideal + offset.normalized * maxLean;

                t.position = pos;
                t.rotation = Quaternion.Slerp(t.rotation, stackRoot.rotation, blend);
            }
        }
    }
}
