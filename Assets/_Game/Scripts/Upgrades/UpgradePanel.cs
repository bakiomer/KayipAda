// UpgradePanel.cs
// Alttan kayarak açılan geliştirme paneli. Açıkken oyuncu yürümez (PlayerController.InputBlocked).
// Panelin dışına (karartılmış alana) ya da X tuşuna dokununca kapanır. Satın alınca satır zıplar,
// tezgahta parlama efekti ve ses çıkar.

using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Alttan kayan panel gövdesi.")]
        [SerializeField] private RectTransform sheet;
        [Tooltip("Panelin arkasındaki karartma (dokununca kapatır).")]
        [SerializeField] private GameObject dimmer;
        [SerializeField] private Button closeButton;
        [SerializeField] private UpgradeRow[] rows = new UpgradeRow[0];

        private UpgradeStation station;
        private float shownY;
        private float hiddenY;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            shownY = sheet.anchoredPosition.y;
            hiddenY = shownY - sheet.rect.height - 60f;
            foreach (UpgradeRow row in rows) row.Init(this);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimmer != null && dimmer.TryGetComponent(out Button dimButton)) dimButton.onClick.AddListener(Close);
            SetVisible(false);
            sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, hiddenY);
        }

        private void Start()
        {
            CurrencyManager.Instance.CoinsChanged += OnCoinsChanged;
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.CoinsChanged -= OnCoinsChanged;
            PlayerController.InputBlocked = false;
        }

        private void OnCoinsChanged(int coins)
        {
            if (IsOpen) RefreshAll();
        }

        public void Open(UpgradeStation from)
        {
            if (IsOpen) return;
            station = from;
            IsOpen = true;
            PlayerController.InputBlocked = true;
            SetVisible(true);
            RefreshAll();
            AudioManager.Play(SoundId.Button);
            Slide(hiddenY, shownY);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            PlayerController.InputBlocked = false;
            Slide(sheet.anchoredPosition.y, hiddenY, () => { if (!IsOpen) SetVisible(false); });
        }

        public void Buy(UpgradeRow row)
        {
            UpgradeManager upgrades = UpgradeManager.Instance;
            if (upgrades == null || !upgrades.TryBuy(row.Type))
            {
                Tween.Shake(row.transform, 4f, 0.25f); // para yetmedi
                return;
            }

            RefreshAll();
            Tween.PunchScale(row.transform, 0.08f, 0.3f);
            AudioManager.Play(SoundId.Upgrade);
            if (Effects.Instance != null && station != null)
            {
                Vector3 p = station.EffectPoint.position;
                Effects.Instance.Sparkle(p);
                Effects.Instance.ShowText(p + Vector3.up * 1.2f, "Seviye " + upgrades.GetLevel(row.Type) + "!", new Color(0.5f, 1f, 0.5f));
            }
        }

        private void RefreshAll()
        {
            foreach (UpgradeRow row in rows) row.Refresh();
        }

        private void SetVisible(bool visible)
        {
            sheet.gameObject.SetActive(visible);
            if (dimmer != null) dimmer.SetActive(visible);
        }

        private void Slide(float from, float to, System.Action done = null)
        {
            Tween.Value(this, 0.25f, t =>
            {
                float y = Mathf.LerpUnclamped(from, to, Tween.EaseOutBack(t));
                sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, y);
            }, done);
        }
    }
}
