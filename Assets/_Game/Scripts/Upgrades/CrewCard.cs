// CrewCard.cs
// Tayfa Barakası panelinde bir işçi kartı: işçinin resmi, adı, tutulan sayı ("1/3") ve fiyatlı "Tut" tuşu.
// Gereken bina açılmadıysa kart soluk görünür ve kilit yazısı çıkar ("Bıçkıhane gerekli"). Hepsi tutulunca "TAMAM".

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class CrewCard : MonoBehaviour
    {
        [SerializeField] private WorkerType type;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private Button hireButton;
        [SerializeField] private Image buttonImage;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private GameObject coinIcon;
        [SerializeField] private GameObject lockRoot;
        [SerializeField] private TMP_Text lockLabel;
        [SerializeField] private Color buttonOn = new Color(0.36f, 0.78f, 0.36f);
        [SerializeField] private Color buttonOff = new Color(0.45f, 0.45f, 0.5f);

        private UpgradePanel panel;

        public WorkerType Type => type;

        public void Init(UpgradePanel owner)
        {
            panel = owner;
            hireButton.onClick.RemoveAllListeners();
            hireButton.onClick.AddListener(() => panel.Hire(this));
        }

        public void Refresh()
        {
            CrewManager crew = CrewManager.Instance;
            if (crew == null || type == null) return;

            int hired = crew.GetHired(type);
            bool available = crew.IsAvailable(type);
            bool maxed = crew.IsMaxed(type);
            nameLabel.text = type.DisplayName;
            countLabel.text = hired + "/" + type.MaxCount;
            if (portrait != null) portrait.color = available ? Color.white : new Color(0.35f, 0.35f, 0.4f, 0.9f);
            lockRoot.SetActive(!available);
            lockLabel.text = type.LockedText;
            hireButton.gameObject.SetActive(available);
            if (!available) return;

            if (maxed)
            {
                buttonLabel.text = "TAMAM";
                priceLabel.text = "";
                coinIcon.SetActive(false);
                buttonImage.color = buttonOff;
                hireButton.interactable = false;
                return;
            }

            int price = crew.NextPrice(type);
            buttonLabel.text = "TUT";
            priceLabel.text = price.ToString();
            coinIcon.SetActive(true);
            buttonImage.color = CurrencyManager.Instance.Coins >= price ? buttonOn : buttonOff;
            hireButton.interactable = true;
        }
    }
}
