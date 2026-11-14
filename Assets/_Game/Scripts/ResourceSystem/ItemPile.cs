// ItemPile.cs
// Yerde duran kaynak yığını (örn. bina girişinde bekleyen odunlar, çıkışta biriken kalaslar), 3D.
// Parçalar sütun x satır x kat düzeninde dizilir; eklenen parça yerine kavis çizerek uçar.
// Kapasiteyi sahibi belirler (örn. ProcessingBuilding, BuildingType'tan).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class ItemPile : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Bir katta yan yana kaç parça (X).")]
        [SerializeField, Min(1)] private int columns = 2;
        [Tooltip("Bir katta arka arkaya kaç parça (Z).")]
        [SerializeField, Min(1)] private int rows = 2;
        [Tooltip("Parçalar arası aralık: X = yan, Y = kat yüksekliği, Z = arka.")]
        [SerializeField] private Vector3 spacing = new Vector3(0.55f, 0.28f, 0.45f);

        private readonly List<ResourceItem> items = new List<ResourceItem>();

        public int Capacity { get; set; }
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public bool IsEmpty => items.Count == 0;

        // Yer varsa parçayı yığına ekler (yerine uçar).
        public bool TryAdd(ResourceItem item)
        {
            if (IsFull || item == null) return false;

            item.transform.SetParent(transform, true);
            items.Add(item);
            item.FlyTo(() => transform.TransformPoint(SlotPosition(Mathf.Max(0, items.IndexOf(item)))),
                       config.ItemFlyHeight, config.ItemFlyDuration);
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

        private void LateUpdate()
        {
            float blend = 1f - Mathf.Exp(-20f * Time.deltaTime);
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsFlying) continue;
                Transform t = items[i].transform;
                t.localPosition = Vector3.Lerp(t.localPosition, SlotPosition(i), blend);
                t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.identity, blend);
            }
        }

        // i. parçanın yığın içindeki yeri: önce bir katı doldurur, sonra üstüne çıkar.
        private Vector3 SlotPosition(int index)
        {
            int perLayer = columns * rows;
            int layer = index / perLayer;
            int inLayer = index % perLayer;
            int column = inLayer % columns;
            int row = inLayer / columns;
            float x = (column - (columns - 1) / 2f) * spacing.x;
            float z = (row - (rows - 1) / 2f) * spacing.z;
            return new Vector3(x, layer * spacing.y, z);
        }
    }
}
