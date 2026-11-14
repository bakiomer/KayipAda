// CurrencyUI.cs
// Ekranın üstündeki para paneli (ikon + sayı).
// CurrencyManager'ın CoinsChanged olayını dinler. Sayı yeni değere hızlıca sayarak gider;
// para her arttığında panel hafifçe büyüyüp küçülür.

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class CurrencyUI : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private TMP_Text coinsText;
        [Tooltip("Para artınca zıplayan kısım (ikon + sayı).")]
        [SerializeField] private RectTransform punchTarget;

        private float shown;
        private int target;
        private float punchTimer;

        private void Start()
        {
            CurrencyManager.Instance.CoinsChanged += OnCoinsChanged;
            target = CurrencyManager.Instance.Coins;
            shown = target;
            coinsText.text = target.ToString();
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.CoinsChanged -= OnCoinsChanged;
            }
        }

        private void OnCoinsChanged(int coins)
        {
            if (coins > target) punchTimer = 0.25f;
            target = coins;
        }

        private void Update()
        {
            // Sayı yeni değere kısa sürede sayarak varır (büyük farkta da hızlı).
            if (!Mathf.Approximately(shown, target))
            {
                float step = Mathf.Max(20f, Mathf.Abs(target - shown) * 8f) * Time.deltaTime;
                shown = Mathf.MoveTowards(shown, target, step);
                coinsText.text = Mathf.RoundToInt(shown).ToString();
            }

            if (punchTarget == null) return;
            if (punchTimer > 0f)
            {
                punchTimer -= Time.deltaTime;
                float k = 1f - Mathf.Clamp01(punchTimer / 0.25f);
                punchTarget.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * config.CoinPunch);
            }
            else
            {
                punchTarget.localScale = Vector3.one;
            }
        }
    }
}
