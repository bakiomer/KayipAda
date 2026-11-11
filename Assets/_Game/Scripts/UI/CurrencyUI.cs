// CurrencyUI.cs
// Ekranın üstündeki para göstergesi.
// CurrencyManager'ın CoinsChanged olayını dinler; sadece para değişince yazıyı günceller.

using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class CurrencyUI : MonoBehaviour
    {
        [SerializeField] private Text coinsText;

        private void Start()
        {
            CurrencyManager.Instance.CoinsChanged += Refresh;
            Refresh(CurrencyManager.Instance.Coins);
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.CoinsChanged -= Refresh;
            }
        }

        private void Refresh(int coins)
        {
            coinsText.text = coins.ToString();
        }
    }
}
