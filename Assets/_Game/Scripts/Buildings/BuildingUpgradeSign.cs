// BuildingUpgradeSign.cs
// Binanın yan tarafındaki küçük ahşap "Yükselt" tabelası. Oyuncu yanında durunca (PanelZone) alttan bina yükseltme paneli açılır
// (mevcut seviye, sonraki seviyenin hız/kapasite artışı, fiyat, "Yükselt" butonu); uzaklaşınca kapanır.
// Tabela binayla birlikte görünür (bina kapalıyken gizli). En son seviyede tabelada "MAKS" yazar. Ambar'da da aynı
// tabela kapasiteyi yükseltir.

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class BuildingUpgradeSign : PanelZone
    {
        [SerializeField] private ProcessingBuilding building;
        [Tooltip("Bina yerine Ambar (kapasite yükseltmesi).")]
        [SerializeField] private Storage storage;
        [Tooltip("Tabelanın üstündeki küçük yazı (\"Yükselt\" / \"MAKS\").")]
        [SerializeField] private TMP_Text label;
        [Tooltip("Satın alınca efektin çıktığı nokta.")]
        [SerializeField] private Transform effectPoint;

        public ProcessingBuilding Building => building;
        public Storage Storage => storage;
        public string Title => building != null ? building.Type.DisplayName : "Ambar";
        public int Level => building != null ? building.Level : storage != null ? storage.Level : 1;
        public int MaxLevel => building != null ? building.MaxLevel : storage != null ? storage.MaxLevel : 1;
        private AreaDefinition[] Levels => building != null ? building.LevelAreas : storage != null ? storage.LevelAreas : new AreaDefinition[0];

        // Panelde gösterilen iki satır: şimdiki → sonraki değer (en son seviyede sadece şimdiki).
        public void Lines(GameConfig cfg, out string first, out string second)
        {
            int level = Level;
            bool maxed = NextLevelArea == null;
            const string arrow = "  →  <color=#8CF266>";
            if (building == null && storage != null)
            {
                first = "Kapasite " + storage.CapacityAt(level) + (maxed ? "" : arrow + storage.CapacityAt(level + 1) + "</color>");
                second = "Daha çok kaynak saklar";
                return;
            }
            float s0 = Value(cfg.BuildingLevelSpeed, level), c0 = Value(cfg.BuildingLevelCapacity, level);
            float s1 = Value(cfg.BuildingLevelSpeed, level + 1), c1 = Value(cfg.BuildingLevelCapacity, level + 1);
            first = "Hız ×" + s0.ToString("0.00") + (maxed ? "" : arrow + "×" + s1.ToString("0.00") + "</color>");
            second = "Kapasite +" + c0.ToString("0") + (maxed ? "" : arrow + "+" + c1.ToString("0") + "</color>");
        }

        private static float Value(float[] values, int level)
        {
            if (values == null || values.Length == 0) return 0f;
            return values[Mathf.Clamp(level - 1, 0, values.Length - 1)];
        }
        public Vector3 EffectPosition => effectPoint != null ? effectPoint.position : transform.position + Vector3.up * 1.5f;

        // Sonraki seviyenin açma tanımı (yoksa en son seviyede).
        public AreaDefinition NextLevelArea
        {
            get
            {
                if (AreaManager.Instance == null) return null;
                foreach (AreaDefinition a in Levels) if (a != null && !AreaManager.Instance.IsUnlocked(a)) return a;
                return null;
            }
        }

        public int NextPrice
        {
            get
            {
                AreaDefinition next = NextLevelArea;
                return next != null && AreaManager.Instance != null ? AreaManager.Instance.GetRemaining(next) : 0;
            }
        }

        private void Start()
        {
            Refresh();
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked += OnUnlocked;
        }

        private void OnDestroy()
        {
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked -= OnUnlocked;
        }

        private void OnUnlocked(AreaDefinition area) => Refresh();

        public void Refresh()
        {
            if (label != null) label.text = NextLevelArea == null ? "MAKS" : "Yükselt";
        }

        protected override void OpenPanel(CarryStack visitor)
        {
            if (BuildingUpgradePanel.Instance != null) BuildingUpgradePanel.Instance.Open(this);
        }

        protected override void ClosePanel()
        {
            if (BuildingUpgradePanel.Instance != null) BuildingUpgradePanel.Instance.Close(this);
        }

        // Satın al: para yeterse sonraki seviye açılır (bina büyür, hızlanır).
        public bool TryUpgrade()
        {
            AreaDefinition next = NextLevelArea;
            if (next == null || CurrencyManager.Instance == null || AreaManager.Instance == null) return false;
            int price = AreaManager.Instance.GetRemaining(next);
            if (!CurrencyManager.Instance.TrySpend(price)) return false;
            AreaManager.Instance.Unlock(next);
            AudioManager.Play(SoundId.Upgrade);
            if (Effects.Instance != null) Effects.Instance.Sparkle(EffectPosition);
            Tween.PunchScale(transform, 0.12f, 0.3f);
            Refresh();
            return true;
        }
    }
}
