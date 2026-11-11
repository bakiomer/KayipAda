// MoneyUI.cs
// Ekranın üstündeki para göstergesi.
// MoneyManager'ın MoneyChanged olayını dinler, sadece para değişince yazıyı günceller.

using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class MoneyUI : MonoBehaviour
    {
        [SerializeField] private Text moneyText;

        private void Start()
        {
            MoneyManager.Instance.MoneyChanged += Refresh;
            Refresh(MoneyManager.Instance.Money);
        }

        private void OnDestroy()
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.MoneyChanged -= Refresh;
            }
        }

        private void Refresh(int money)
        {
            moneyText.text = money.ToString();
        }
    }
}
