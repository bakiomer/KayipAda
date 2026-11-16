// CarryStack.cs
// Bir karakterin (oyuncu ya da işçi) önünde taşıdığı kaynak yığını, 3D.
// Kaynaklar yığına kavis çizerek uçar ve 2-3 sütunlu sıkışık bir blok halinde dizilir (sütun sayısı kaynağın
// genişliğine göre). Yığın en fazla omuz hizasının biraz üstüne kadar yükselir (GameConfig.StackMaxHeight);
// fazlası görünmez ve yığının üstünde küçük bir sayı etiketi ("x40") çıkar. Her kat alttakini biraz gecikmeyle
// takip eder: karakter yürürken yığın hafifçe sallanır. Kapasiteyi sahibi belirler (oyuncu: GameConfig, işçi: WorkerType).

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class CarryStack : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Yığının başladığı nokta (karakterin göğsünün önü). Karakterle birlikte döner.")]
        [SerializeField] private Transform stackRoot;
        [Tooltip("Görünen sınır aşılınca yığının üstünde çıkan sayı etiketi (\"x40\"). Boş olabilir.")]
        [SerializeField] private TMP_Text countLabel;

        private readonly List<ResourceItem> items = new List<ResourceItem>();
        private int shownCount = -1;

        public int Capacity { get; set; }
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public bool IsEmpty => items.Count == 0;

        // Oyuncunun yığını mı? (Para ödemek gibi işleri sadece oyuncu yapar.) PlayerController ayarlar.
        public bool IsPlayer { get; set; }

        // İşçiler için: şu an iş yaptığı alan. Alanlar işçiye sadece hedefiyse hizmet eder.
        public InteractionZone WorkTarget { get; set; }

        // Görünen en fazla parça sayısı (sütun × kat).
        public int VisibleCapacity => Columns * Rows;

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
            // Görünmeyen (sınırın üstündeki) parça çıkarsa yığının tepesinden görünür olarak çıkar.
            if (!item.gameObject.activeSelf)
            {
                item.transform.position = SlotPosition(Mathf.Min(index, VisibleCapacity - 1));
                item.gameObject.SetActive(true);
            }
            return item;
        }

        // Sütun sayısı: alttaki (ilk) parçanın genişliğine göre 1-3 (küçük parçalar 3, büyükler 2).
        private int Columns
        {
            get
            {
                ResourceType type = items.Count > 0 && items[0] != null ? items[0].Type : null;
                float width = type != null ? type.StackWidth : config.StackSpacing;
                return Mathf.Clamp(Mathf.RoundToInt(config.StackBlockWidth * 1.3f / Mathf.Max(0.05f, width)), 2, config.StackMaxColumns);
            }
        }

        private float RowHeight
        {
            get
            {
                ResourceType type = items.Count > 0 && items[0] != null ? items[0].Type : null;
                return type != null && type.StackHeight > 0f ? type.StackHeight : config.StackSpacing;
            }
        }

        private int Rows => Mathf.Max(1, Mathf.FloorToInt(config.StackMaxHeight / Mathf.Max(0.05f, RowHeight)) + 1);

        private Vector3 SlotPosition(int index)
        {
            int cols = Columns;
            int col = index % cols, row = Mathf.Min(index / cols, Rows - 1);
            ResourceType type = items.Count > 0 && items[0] != null ? items[0].Type : null;
            float width = type != null && type.StackWidth > 0f ? type.StackWidth : config.StackSpacing;
            float spacing = Mathf.Min(width, config.StackBlockWidth / cols); // geniş parçalar biraz iç içe, blok dar kalır
            float x = (col - (cols - 1) * 0.5f) * spacing;
            return stackRoot.position + stackRoot.right * x + stackRoot.up * (row * RowHeight);
        }

        // Parçanın yığındaki düz (sallanmasız) yeri; uçan parçalar buraya iner.
        private Vector3 IdealSlot(ResourceItem item)
        {
            return SlotPosition(Mathf.Max(0, items.IndexOf(item)));
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            int visible = VisibleCapacity;
            int cols = Columns;
            for (int i = 0; i < items.Count; i++)
            {
                ResourceItem item = items[i];
                if (item == null || item.IsFlying) continue;

                // Görünen sınırın üstündekiler gizli kalır (sayı etiketiyle gösterilir).
                bool show = i < visible;
                if (item.gameObject.activeSelf != show) item.gameObject.SetActive(show);
                if (!show) continue;

                // Her kat kendi düz yerine gider; yukarı çıktıkça biraz daha geç kalır → hafif sallanma.
                int level = i / cols;
                Vector3 ideal = SlotPosition(i);
                float sharpness = config.StackFollowSharpness / (1f + level * config.StackSwayPerLevel);
                float blend = 1f - Mathf.Exp(-sharpness * dt);

                Transform t = item.transform;
                Vector3 pos = Vector3.Lerp(t.position, ideal, blend);

                // Yığın fazla eğilmesin: her kat düz duruştan en fazla belli bir mesafe kayabilir.
                Vector3 offset = pos - ideal;
                float maxLean = config.StackMaxLeanPerLevel * (level + 1);
                if (offset.sqrMagnitude > maxLean * maxLean) pos = ideal + offset.normalized * maxLean;

                t.position = pos;
                t.rotation = Quaternion.Slerp(t.rotation, stackRoot.rotation, blend);
            }
            UpdateLabel(visible);
        }

        private void UpdateLabel(int visible)
        {
            if (countLabel == null) return;
            bool over = items.Count > visible;
            if (countLabel.gameObject.activeSelf != over) countLabel.gameObject.SetActive(over);
            if (!over) return;
            countLabel.transform.position = stackRoot.position + stackRoot.up * (Rows * RowHeight + config.StackLabelGap);
            if (shownCount == items.Count) return;
            shownCount = items.Count;
            countLabel.text = "x" + items.Count;
        }
    }
}
