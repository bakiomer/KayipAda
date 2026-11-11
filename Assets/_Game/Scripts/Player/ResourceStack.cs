// ResourceStack.cs
// Oyuncunun sırtında taşıdığı kaynak yığını.
// Eklenen her kaynak, yığındaki yerine doğru uçarak üst üste dizilir.
// Kapasite dolunca yeni kaynak kabul edilmez.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class ResourceStack : MonoBehaviour
    {
        [SerializeField] private int capacity = 10;
        [Tooltip("Yığının başladığı nokta (oyuncunun sırtı).")]
        [SerializeField] private Transform stackRoot;
        [SerializeField] private float itemSpacing = 0.22f;
        [SerializeField] private float flySpeed = 15f;
        [SerializeField] private int baseSortingOrder = 20;

        private readonly List<ResourceItem> items = new List<ResourceItem>();

        public int Count => items.Count;
        public bool IsFull => items.Count >= capacity;
        public bool IsEmpty => items.Count == 0;

        // Yer varsa kaynağı yığının en üstüne ekler.
        public bool TryAdd(ResourceItem item)
        {
            if (IsFull) return false;

            item.transform.SetParent(stackRoot, true); // dünyadaki yerini koruyarak bağla
            item.SetSortingOrder(baseSortingOrder + items.Count);
            items.Add(item);
            return true;
        }

        // En üstteki kaynağı yığından çıkarır ve geri döndürür (boşsa null).
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
            // Her kaynağı yığındaki hedef yerine doğru kaydır.
            for (int i = 0; i < items.Count; i++)
            {
                Vector3 target = new Vector3(0f, i * itemSpacing, 0f);
                Transform t = items[i].transform;
                t.localPosition = Vector3.Lerp(t.localPosition, target, flySpeed * Time.deltaTime);
            }
        }
    }
}
