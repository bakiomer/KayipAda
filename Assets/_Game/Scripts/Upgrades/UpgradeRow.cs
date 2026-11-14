// UpgradeRow.cs
// Geliştirme panelinde bir satır: ad, etki ("+4 → +6"), 5 seviye noktası ve fiyatlı satın alma tuşu.
// Maksimum seviyede tuşta "MAKS" yazar. Para yetmiyorsa tuş soluk görünür.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class UpgradeRow : MonoBehaviour
    {
        [SerializeField] private UpgradeType type;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text effectLabel;
        [SerializeField] private Image[] pips = new Image[0];
        [SerializeField] private Button buyButton;
        [SerializeField] private Image buttonImage;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private GameObject coinIcon;
        [SerializeField] private Color pipOn = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color pipOff = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color buttonOn = new Color(0.36f, 0.78f, 0.36f);
        [SerializeField] private Color buttonOff = new Color(0.45f, 0.45f, 0.5f);

        private UpgradePanel panel;

        public UpgradeType Type => type;

        public void Init(UpgradePanel owner)
        {
            panel = owner;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => panel.Buy(this));
        }

        public void Refresh()
        {
            UpgradeManager upgrades = UpgradeManager.Instance;
            if (upgrades == null || type == null) return;

            int level = upgrades.GetLevel(type);
            bool maxed = upgrades.IsMaxed(type);
            nameLabel.text = type.DisplayName;
            effectLabel.text = maxed ? type.FormatBonus(level)
                             : (level == 0 ? "" : type.FormatBonus(level) + "  →  ") + type.FormatBonus(level + 1);

            for (int i = 0; i < pips.Length; i++) pips[i].color = i < level ? pipOn : pipOff;

            if (maxed)
            {
                priceLabel.text = "MAKS";
                coinIcon.SetActive(false);
                buttonImage.color = buttonOff;
                buyButton.interactable = false;
                return;
            }

            int price = upgrades.NextPrice(type);
            priceLabel.text = price.ToString();
            coinIcon.SetActive(true);
            bool affordable = CurrencyManager.Instance.Coins >= price;
            buttonImage.color = affordable ? buttonOn : buttonOff;
            buyButton.interactable = true;
        }
    }
}
