// CurrencyManager.cs
// Oyuncunun parasını tek bir yerde tutar. Para değişince CoinsChanged olayını yayınlar;
// arayüz (CurrencyUI) bu olayı dinler, her karede kontrol etmez.
// Sahnede tek bir tane bulunur, diğer scriptler CurrencyManager.Instance ile erişir.
// DefaultExecutionOrder: diğer scriptlerin Awake'inden önce hazır olsun diye.

using System;
using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-100)]
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        // Para miktarı değiştiğinde yeni değeri gönderir.
        public event Action<int> CoinsChanged;

        [SerializeField] private GameConfig config;

        public int Coins { get; private set; }

        // Bu adada kazanılan toplam para (satışlar). Kaçış ekranında sayarak gösterilir; kayıtta saklanır.
        public int TotalEarned { get; set; }

        // Test Menüsü: sınırsız para açıkken harcama parayı azaltmaz (sadece Editor/geliştirme sürümü).
        public bool IsInfinite { get; private set; }
        private int coinsBeforeInfinite;

        public void SetInfinite(bool on, int displayAmount)
        {
            if (on == IsInfinite) return;
            IsInfinite = on;
            if (on)
            {
                coinsBeforeInfinite = Coins;
                Coins = Mathf.Max(Coins, displayAmount);
            }
            else
            {
                Coins = coinsBeforeInfinite;
            }
            CoinsChanged?.Invoke(Coins);
        }

        private void Awake()
        {
            Instance = this;
            Coins = config.StartingCoins;
        }

        // Kayıttan yükleme.
        public void SetCoins(int amount)
        {
            Coins = Mathf.Max(0, amount);
            CoinsChanged?.Invoke(Coins);
        }

        public void Add(int amount)
        {
            Coins += amount;
            if (amount > 0) TotalEarned += amount;
            CoinsChanged?.Invoke(Coins);
        }

        // Yeterli para varsa harcar ve true döner; yoksa hiçbir şey yapmaz.
        public bool TrySpend(int amount)
        {
            if (IsInfinite) return true;
            if (Coins < amount) return false;

            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }
    }
}
