// CurrencyManager.cs
// Oyuncunun parasını tek bir yerde tutar. Para değişince CoinsChanged olayını yayınlar;
// arayüz (CurrencyUI) bu olayı dinler, her karede kontrol etmez.
// Sahnede tek bir tane bulunur, diğer scriptler CurrencyManager.Instance ile erişir.

using System;
using UnityEngine;

namespace KayipAda
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        // Para miktarı değiştiğinde yeni değeri gönderir.
        public event Action<int> CoinsChanged;

        [SerializeField] private GameConfig config;

        public int Coins { get; private set; }

        private void Awake()
        {
            Instance = this;
            Coins = config.StartingCoins;
        }

        public void Add(int amount)
        {
            Coins += amount;
            CoinsChanged?.Invoke(Coins);
        }

        // Yeterli para varsa harcar ve true döner; yoksa hiçbir şey yapmaz.
        public bool TrySpend(int amount)
        {
            if (Coins < amount) return false;

            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }
    }
}
