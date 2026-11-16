// CrewManager.cs
// Tayfa Barakası'nda tutulan işçi sayılarını TEK YERDE tutar (kayıt sistemi bunu kaydeder). Her işçi türü sırayla
// tutulur (fiyatlar WorkerType.HirePrices'ta); türün gerektirdiği bina açılmadan tutulamaz. WorkerSpawner'lar
// buradaki sayıya bakıp yeni işçiyi barakanın kapısından çıkarır.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-100)]
    public class CrewManager : MonoBehaviour
    {
        public static CrewManager Instance { get; private set; }

        // Bir türden işçi tutulunca (yeni sayı).
        public event Action<WorkerType> Changed;

        [SerializeField] private GameConfig config;
        [SerializeField] private WorkerType[] types = new WorkerType[0];

        private readonly Dictionary<string, int> hired = new Dictionary<string, int>();

        public IReadOnlyList<WorkerType> Types => types;

        private void Awake()
        {
            Instance = this;
        }

        public int GetHired(WorkerType type)
        {
            return type != null && hired.TryGetValue(Key(type), out int n) ? n : 0;
        }

        public void SetHired(WorkerType type, int count)
        {
            if (type == null) return;
            hired[Key(type)] = Mathf.Clamp(count, 0, type.MaxCount);
            Changed?.Invoke(type);
        }

        public bool IsMaxed(WorkerType type) => GetHired(type) >= type.MaxCount;

        // Gereken bina açık mı?
        public bool IsAvailable(WorkerType type)
        {
            return type.RequiredArea == null || (AreaManager.Instance != null && AreaManager.Instance.IsUnlocked(type.RequiredArea));
        }

        // Sıradaki işçinin fiyatı (hızlı test modunda indirimli). Hepsi tutulduysa 0.
        public int NextPrice(WorkerType type)
        {
            int n = GetHired(type);
            if (n >= type.MaxCount) return 0;
            return config.ScalePrice(type.HirePrices[n]);
        }

        public bool TryHire(WorkerType type)
        {
            if (type == null || IsMaxed(type) || !IsAvailable(type)) return false;
            if (!CurrencyManager.Instance.TrySpend(NextPrice(type))) return false;
            SetHired(type, GetHired(type) + 1);
            return true;
        }

        // Kayıt sistemi: id → sayı.
        public void Restore(Dictionary<string, int> counts)
        {
            foreach (WorkerType type in types)
            {
                if (type == null) continue;
                if (counts.TryGetValue(Key(type), out int n)) SetHired(type, n);
                // Eski kayıt: açma karesiyle tutulmuş işçi → 1.
                else if (type.LegacyHireArea != null && AreaManager.Instance != null && AreaManager.Instance.IsUnlocked(type.LegacyHireArea)) SetHired(type, 1);
            }
        }

        public IEnumerable<KeyValuePair<string, int>> Counts => hired;

        private static string Key(WorkerType type) => string.IsNullOrEmpty(type.Id) ? type.name : type.Id;
    }
}
