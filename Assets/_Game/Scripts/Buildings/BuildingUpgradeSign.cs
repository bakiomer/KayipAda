// BuildingUpgradeSign.cs
// Binanın yan tarafındaki küçük ahşap "Yükselt" tabelası. Oyuncu yanına gelince alttan bina yükseltme paneli açılır
// (mevcut seviye, sonraki seviyenin hız/kapasite artışı, fiyat, "Yükselt" butonu); uzaklaşınca kapanır.
// Tabela binayla birlikte görünür (bina kapalıyken gizli). En son seviyede tabelada "MAKS" yazar.

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class BuildingUpgradeSign : InteractionZone
    {
        [SerializeField] private ProcessingBuilding building;
        [Tooltip("Tabelanın üstündeki küçük yazı (\"Yükselt\" / \"MAKS\").")]
        [SerializeField] private TMP_Text label;
        [Tooltip("Satın alınca efektin çıktığı nokta.")]
        [SerializeField] private Transform effectPoint;

        public ProcessingBuilding Building => building;
        public Vector3 EffectPosition => effectPoint != null ? effectPoint.position : transform.position + Vector3.up * 1.5f;

        // Sonraki seviyenin açma tanımı (yoksa en son seviyede).
        public AreaDefinition NextLevelArea
        {
            get
            {
                if (building == null || AreaManager.Instance == null) return null;
                foreach (AreaDefinition a in building.LevelAreas) if (a != null && !AreaManager.Instance.IsUnlocked(a)) return a;
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

        protected override void OnVisitorEnter(CarryStack visitor)
        {
            if (visitor.IsPlayer && BuildingUpgradePanel.Instance != null) BuildingUpgradePanel.Instance.Open(this);
        }

        protected override void OnVisitorExit(CarryStack visitor)
        {
            if (visitor.IsPlayer && BuildingUpgradePanel.Instance != null) BuildingUpgradePanel.Instance.Close(this);
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
