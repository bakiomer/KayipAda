// ItemPile.cs
// Yerde duran kaynak yığını (örn. bina girişinde bekleyen odunlar, çıkışta biriken kalaslar).
// Parçaları birkaç sütunlu düzgün bir yığın halinde dizer; eklenen parça yerine doğru kayar.
// Kapasiteyi sahibi belirler (örn. ProcessingBuilding, BuildingType'tan).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class ItemPile : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Yığın kaç sütun halinde dizilir.")]
        [SerializeField, Min(1)] private int columns = 2;
        [Tooltip("Parçalar arası yatay (x) ve dikey (y) aralık.")]
        [SerializeField] private Vector2 spacing = new Vector2(0.4f, 0.2f);

        private readonly List<ResourceItem> items = new List<ResourceItem>();

        public int Capacity { get; set; }
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public bool IsEmpty => items.Count == 0;

        // Yer varsa parçayı yığına ekler.
        public bool TryAdd(ResourceItem item)
        {
            if (IsFull) return false;

            item.transform.SetParent(transform, true); // dünyadaki yerini koruyarak bağla, sonra yerine kayar
            item.SetSortingOrder(SortOrder.PileItem + items.Count);
            items.Add(item);
            return true;
        }

        // En son eklenen parçayı çıkarır; boşsa null döner.
        public ResourceItem RemoveTop()
        {
            if (IsEmpty) return null;

            int last = items.Count - 1;
            ResourceItem item = items[last];
            items.RemoveAt(last);
            item.transform.SetParent(null, true);
            return item;
        }

        private void Update()
        {
            for (int i = 0; i < items.Count; i++)
            {
                Transform t = items[i].transform;
                t.localPosition = Vector3.Lerp(t.localPosition, SlotPosition(i), config.ItemFlySpeed * Time.deltaTime);
            }
        }

        // i. parçanın yığın içindeki yeri: alttan yukarı, soldan sağa.
        private Vector3 SlotPosition(int index)
        {
            int column = index % columns;
            int row = index / columns;
            float x = (column - (columns - 1) / 2f) * spacing.x;
            return new Vector3(x, row * spacing.y, 0f);
        }
    }
}
