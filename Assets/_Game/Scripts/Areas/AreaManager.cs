// AreaManager.cs
// Hangi bölgelerin açıldığını ve yarım kalan ödemeleri TEK BİR YERDE tutar.
// Bölge açılınca AreaUnlocked olayını yayınlar; bölgeler ve açma kareleri bunu dinleyip kendini günceller.
// İleride kayıt sistemi (Aşama 8) bu sınıfın verisini kaydedip geri yükleyecek.
// DefaultExecutionOrder: diğer scriptlerin Awake'inden önce hazır olsun diye.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-100)]
    public class AreaManager : MonoBehaviour
    {
        public static AreaManager Instance { get; private set; }

        // Bir bölge açıldığında o bölgeyi gönderir.
        public event Action<AreaDefinition> AreaUnlocked;

        [SerializeField] private GameConfig config;

        private readonly HashSet<string> unlockedIds = new HashSet<string>();
        private readonly Dictionary<string, int> paidAmounts = new Dictionary<string, int>(); // kimlik → ödenen para

        private void Awake()
        {
            Instance = this;
        }

        // Bölge boşsa (null) "her zaman açık" sayılır; böylece "önce şu açılmalı" şartı olmayanlar kolay yazılır.
        public bool IsUnlocked(AreaDefinition area)
        {
            return area == null || unlockedIds.Contains(area.Id);
        }

        public int GetPaid(AreaDefinition area)
        {
            return paidAmounts.TryGetValue(area.Id, out int paid) ? paid : 0;
        }

        // Asıl fiyat (hızlı test modunda indirimli).
        public int PriceOf(AreaDefinition area)
        {
            return config != null ? config.ScalePrice(area.Price) : area.Price;
        }

        public int GetRemaining(AreaDefinition area)
        {
            return Mathf.Max(0, PriceOf(area) - GetPaid(area));
        }

        // Kayıt sistemi için: açık alanların kimlikleri ve yarım ödemeler.
        public IEnumerable<string> UnlockedIds => unlockedIds;
        public IReadOnlyDictionary<string, int> PaidAmounts => paidAmounts;

        // Kayıttan geri yükler (oyun başında, olay yayınlamadan; sahne nesneleri Start'ta kendini günceller).
        public void Restore(IEnumerable<string> ids, IDictionary<string, int> paid)
        {
            unlockedIds.Clear();
            paidAmounts.Clear();
            foreach (string id in ids) unlockedIds.Add(id);
            foreach (KeyValuePair<string, int> pair in paid) paidAmounts[pair.Key] = pair.Value;
        }

        // Bölgeye ödeme ekler; fiyat tamamlanırsa bölgeyi açar. (Paranın düşülmesi çağıranın işidir.)
        public void AddPayment(AreaDefinition area, int amount)
        {
            if (IsUnlocked(area)) return;

            paidAmounts[area.Id] = GetPaid(area) + amount;
            if (GetRemaining(area) == 0)
            {
                Unlock(area);
            }
        }

        public void Unlock(AreaDefinition area)
        {
            if (!unlockedIds.Add(area.Id)) return; // zaten açıktı

            paidAmounts.Remove(area.Id);
            AreaUnlocked?.Invoke(area);
        }
    }
}
