// MoneyManager.cs
// Oyuncunun parasını tutar. Para değişince MoneyChanged olayını yayınlar,
// böylece arayüz (MoneyUI) her karede kontrol etmek zorunda kalmaz.
// Sahnede tek bir tane bulunur, diğer scriptler MoneyManager.Instance ile erişir.

using System;
using UnityEngine;

namespace KayipAda
{
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        // Para miktarı değiştiğinde yeni değeri gönderir.
        public event Action<int> MoneyChanged;

        [SerializeField] private int money = 0;

        public int Money => money;

        private void Awake()
        {
            Instance = this;
        }

        public void Add(int amount)
        {
            money += amount;
            MoneyChanged?.Invoke(money);
        }

        // Yeterli para varsa harcar ve true döner; yoksa hiçbir şey yapmaz.
        public bool TrySpend(int amount)
        {
            if (money < amount) return false;

            money -= amount;
            MoneyChanged?.Invoke(money);
            return true;
        }
    }
}
