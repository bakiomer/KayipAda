// BuildingUpgradePanel.cs
// Bina yükseltme paneli: oyuncu binanın yanındaki "Yükselt" tabelasına gelince alttan kayarak açılır, uzaklaşınca
// kapanır. Oyuncunun yürümesini engellemez. Gösterilenler: bina adı, seviye (●●○), sonraki seviyenin hız ve kapasite
// artışı, fiyat ve "Yükselt" butonu (para yetmezse soluk; en son seviyede "MAKS SEVİYE").

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class BuildingUpgradePanel : MonoBehaviour
    {
        public static BuildingUpgradePanel Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private RectTransform sheet;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image[] levelDots = new Image[0];
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private GameObject priceIcon;
        [SerializeField] private Color dotOn = new Color(1f, 0.82f, 0.25f);
        [SerializeField] private Color dotOff = new Color(1f, 1f, 1f, 0.25f);

        private BuildingUpgradeSign sign;
        private float shownY, hiddenY;

        public bool IsOpen { get; private set; }
        public BuildingUpgradeSign Sign => sign;
        public string SpeedLine => speedText != null ? speedText.text : "";
        public string CapacityLine => capacityText != null ? capacityText.text : "";

        private void Awake()
        {
            Instance = this;
            shownY = sheet.anchoredPosition.y;
            hiddenY = shownY - sheet.rect.height - 80f;
            buyButton.onClick.AddListener(Buy);
            sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, hiddenY);
            sheet.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.CoinsChanged += OnCoins;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.CoinsChanged -= OnCoins;
        }

        private void OnCoins(int coins)
        {
            if (IsOpen) Refresh();
        }

        // Tabela gizlenirse (bina kapandı, tören) panel de kapansın.
        private void Update()
        {
            if (IsOpen && (sign == null || !sign.isActiveAndEnabled || PlayerController.InputBlocked)) Close(sign);
        }

        public void Open(BuildingUpgradeSign from)
        {
            sign = from;
            Refresh();
            if (IsOpen) return;
            IsOpen = true;
            sheet.gameObject.SetActive(true);
            AudioManager.Play(SoundId.Button);
            Slide(hiddenY, shownY);
        }

        public void Close(BuildingUpgradeSign from)
        {
            if (!IsOpen || (from != null && from != sign)) return;
            IsOpen = false;
            Slide(sheet.anchoredPosition.y, hiddenY, () => { if (!IsOpen) sheet.gameObject.SetActive(false); });
        }

        public void Buy()
        {
            if (sign == null) return;
            if (!sign.TryUpgrade())
            {
                Tween.Shake(buyButton.transform, 5f, 0.25f); // para yetmedi
                return;
            }
            Tween.PunchScale(sheet, 0.04f, 0.25f);
            Refresh();
        }

        public void Refresh()
        {
            if (sign == null || sign.Building == null) return;
            ProcessingBuilding b = sign.Building;
            int level = b.Level, max = b.MaxLevel;
            title.text = b.Type.DisplayName;
            levelText.text = "Seviye " + level + "/" + max;
            for (int i = 0; i < levelDots.Length; i++)
            {
                levelDots[i].gameObject.SetActive(i < max);
                levelDots[i].color = i < level ? dotOn : dotOff;
            }
            float speedNow = Value(config.BuildingLevelSpeed, level), capNow = Value(config.BuildingLevelCapacity, level);
            bool maxed = sign.NextLevelArea == null;
            if (maxed)
            {
                speedText.text = "Hız ×" + speedNow.ToString("0.00");
                capacityText.text = "Kapasite +" + capNow.ToString("0");
                priceText.text = "MAKS SEVİYE";
                if (priceIcon != null) priceIcon.SetActive(false);
                buyButton.interactable = false;
                return;
            }
            float speedNext = Value(config.BuildingLevelSpeed, level + 1), capNext = Value(config.BuildingLevelCapacity, level + 1);
            speedText.text = "Hız ×" + speedNow.ToString("0.00") + "  →  <color=#8CF266>×" + speedNext.ToString("0.00") + "</color>";
            capacityText.text = "Kapasite +" + capNow.ToString("0") + "  →  <color=#8CF266>+" + capNext.ToString("0") + "</color>";
            int price = sign.NextPrice;
            priceText.text = "Yükselt  " + price;
            if (priceIcon != null) priceIcon.SetActive(true);
            bool afford = CurrencyManager.Instance != null && (CurrencyManager.Instance.IsInfinite || CurrencyManager.Instance.Coins >= price);
            buyButton.interactable = true;
            ((Graphic)buyButton.targetGraphic).color = afford ? new Color(0.36f, 0.75f, 0.3f) : new Color(0.45f, 0.45f, 0.45f);
        }

        private static float Value(float[] values, int level)
        {
            if (values == null || values.Length == 0) return 0f;
            return values[Mathf.Clamp(level - 1, 0, values.Length - 1)];
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
