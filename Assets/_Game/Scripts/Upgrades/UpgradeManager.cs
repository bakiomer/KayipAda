// UpgradeManager.cs
// Geliştirme seviyelerini TEK YERDE tutar (kayıt sistemi bunu kaydeder). Satın alma, seviye sorgulama ve
// oyun sistemlerinin kullandığı toplam etkiler buradan gelir: Multiplier(stat) hızlar için, Extra(stat) kapasite için.
// Kaptanlık seviyesinin küçük kalıcı bonusu da (Bölüm D) bu çarpanlara eklenir.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-100)]
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        // Bir geliştirmenin seviyesi değişince.
        public event Action<UpgradeType> Changed;

        [SerializeField] private GameConfig config;
        [SerializeField] private UpgradeType[] upgrades = new UpgradeType[0];

        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public IReadOnlyList<UpgradeType> Upgrades => upgrades;

        // Kaptanlık seviyesi (kaçıştan sonra kalıcı; kayıt sistemi ayarlar).
        public int CaptainLevel { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public int GetLevel(UpgradeType type)
        {
            return levels.TryGetValue(type.Id, out int level) ? level : 0;
        }

        public void SetLevel(UpgradeType type, int level)
        {
            levels[type.Id] = Mathf.Clamp(level, 0, type.MaxLevel);
            Changed?.Invoke(type);
        }

        public bool IsMaxed(UpgradeType type) => GetLevel(type) >= type.MaxLevel;

        // Bir sonraki seviyenin fiyatı (hızlı test modunda indirimli). Maksimumdaysa 0.
        public int NextPrice(UpgradeType type)
        {
            int level = GetLevel(type);
            if (level >= type.MaxLevel) return 0;
            return config.ScalePrice(type.Prices[level]);
        }

        public bool TryBuy(UpgradeType type)
        {
            if (IsMaxed(type)) return false;
            if (!CurrencyManager.Instance.TrySpend(NextPrice(type))) return false;
            SetLevel(type, GetLevel(type) + 1);
            return true;
        }

        private float Bonus(UpgradeStat stat)
        {
            float total = 0f;
            foreach (UpgradeType type in upgrades)
            {
                if (type != null && type.Stat == stat) total += type.BonusAt(GetLevel(type));
            }
            return total;
        }

        // Kaptanlık bonusu (oran): seviye başına config'teki kadar, en fazla config'teki seviyeye kadar.
        private float CaptainBonus => Mathf.Min(CaptainLevel, config.CaptainMaxLevel) * config.CaptainBonusPerLevel;

        // Hızlar için çarpan (1 = değişmez). Geliştirme yöneticisi yoksa 1.
        public static float Multiplier(UpgradeStat stat)
        {
            if (Instance == null) return 1f;
            return 1f + Instance.Bonus(stat) + Instance.CaptainBonus;
        }

        // Kapasite için ek adet. Kaptanlık bonusu temel kapasitenin yüzdesi olarak eklenir.
        public static int Extra(UpgradeStat stat, int baseValue)
        {
            if (Instance == null) return 0;
            return Mathf.RoundToInt(Instance.Bonus(stat)) + Mathf.RoundToInt(baseValue * Instance.CaptainBonus);
        }
    }
}
