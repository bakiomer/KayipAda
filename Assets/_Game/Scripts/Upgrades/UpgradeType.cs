// UpgradeType.cs
// Bir geliştirmenin ayar dosyası (Data/Upgrades): ad, hangi özelliği etkilediği, seviye fiyatları ve etkileri.
// Seviye sayısı = Prices dizisinin uzunluğu (varsayılan 5). Bonuses[i] = (i+1). seviyedeki TOPLAM etki:
// kapasitede adet (+2), hızlarda oran (0.1 = %10 daha hızlı).

using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Kayıp Ada/Geliştirme")]
    public class UpgradeType : ScriptableObject
    {
        [field: Tooltip("Kayıtta kullanılan benzersiz kimlik. Örn: player_capacity")]
        [field: SerializeField] public string Id { get; private set; } = "new_upgrade";

        [field: Tooltip("Panelde görünen ad. Örn: Taşıma Kapasitesi")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Geliştirme";

        [field: SerializeField] public UpgradeStat Stat { get; private set; }

        [field: Tooltip("Her seviyenin fiyatı (sırayla 1., 2., ... seviye).")]
        [field: SerializeField] public int[] Prices { get; private set; } = { 50, 100, 200, 400, 800 };

        [field: Tooltip("Her seviyedeki TOPLAM etki. Kapasite: adet, hız: oran (0.1 = %10).")]
        [field: SerializeField] public float[] Bonuses { get; private set; } = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

        public int MaxLevel => Prices.Length;
        public bool IsPercent => Stat == UpgradeStat.PlayerSpeed || Stat == UpgradeStat.WorkerSpeed || Stat == UpgradeStat.BuildingSpeed;

        public float BonusAt(int level)
        {
            if (level <= 0 || Bonuses.Length == 0) return 0f;
            return Bonuses[Mathf.Min(level, Bonuses.Length) - 1];
        }

        // Paneldeki kısa açıklama: "+4" ya da "%20".
        public string FormatBonus(int level)
        {
            float b = BonusAt(level);
            return IsPercent ? "+%" + Mathf.RoundToInt(b * 100f) : "+" + Mathf.RoundToInt(b);
        }

#if UNITY_EDITOR
        public void EditorSetup(string id, string displayName, UpgradeStat stat, int[] prices, float[] bonuses)
        {
            Id = id;
            DisplayName = displayName;
            Stat = stat;
            Prices = prices;
            Bonuses = bonuses;
        }
#endif
    }
}
