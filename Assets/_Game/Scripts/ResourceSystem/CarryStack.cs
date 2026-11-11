// CarryStack.cs
// Bir karakterin sırtında taşıdığı kaynak yığını. Farklı türler karışık dizilebilir.
// Şimdilik oyuncu kullanıyor; ileride işçiler de aynı bileşeni kullanacak.
// Kapasiteyi sahibi belirler (oyuncu için PlayerController, GameConfig'ten).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class CarryStack : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Yığının başladığı nokta (karakterin sırtı).")]
        [SerializeField] private Transform stackRoot;

        private readonly List<ResourceItem> items = new List<ResourceItem>();

        public int Capacity { get; set; }
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public bool IsEmpty => items.Count == 0;

        // Yer varsa kaynağı yığının en üstüne ekler.
        public bool TryAdd(ResourceItem item)
        {
            if (IsFull) return false;

            item.transform.SetParent(stackRoot, true); // dünyadaki yerini koruyarak bağla
            items.Add(item);
            RefreshSorting();
            return true;
        }

        // En üstteki kaynağı (türü fark etmeksizin) çıkarır; boşsa null döner.
        public ResourceItem RemoveTop()
        {
            if (IsEmpty) return null;
            return RemoveAt(items.Count - 1);
        }

        // Belirli türden en üstteki kaynağı çıkarır; o türden yoksa null döner.
        // (İleride işleme binaları "sadece odun al" gibi istekler için kullanacak.)
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
            item.transform.SetParent(null, true);
            RefreshSorting();
            return item;
        }

        // Üstteki kaynak her zaman önde çizilsin.
        private void RefreshSorting()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetSortingOrder(SortOrder.CarriedItem + i);
            }
        }

        private void Update()
        {
            // Her kaynağı yığındaki hedef yerine doğru kaydır.
            for (int i = 0; i < items.Count; i++)
            {
                Vector3 target = new Vector3(0f, i * config.StackSpacing, 0f);
                Transform t = items[i].transform;
                t.localPosition = Vector3.Lerp(t.localPosition, target, config.ItemFlySpeed * Time.deltaTime);
            }
        }
    }
}
