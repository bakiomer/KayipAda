// UnlockPad.cs
// Açma karesi: Yerde duran, üstünde bölgenin adı ve kalan fiyatı yazan kare.
// Oyuncu üstünde durunca parası küçük parçalar halinde kareye akar ve kalan fiyat azalır.
// Oyuncu çıkarsa ödenen kısım kaybolmaz (AreaManager'da saklanır). Fiyat bitince bölge açılır, kare kaybolur.
// "Gereken alan" atanmışsa kare, o alan açılana kadar gizli kalır (sırayla açılan bölgeler için).

using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class UnlockPad : InteractionZone
    {
        [SerializeField] private AreaDefinition area;
        [Tooltip("Boş değilse: bu kare ancak bu alan açıldıktan sonra görünür.")]
        [SerializeField] private AreaDefinition requiredArea;
        [SerializeField] private Text label;
        [Tooltip("Oyuncudan kareye uçan küçük para görseli.")]
        [SerializeField] private FlyingVisual coinPrefab;

        private float timer;

        private void Awake()
        {
            if (area == null || label == null || coinPrefab == null)
            {
                Debug.LogError(name + ": Area, Label veya Coin Prefab atanmamış!", this);
                enabled = false;
                return;
            }

            // Kare kendini gizlese bile olayı dinlemeye devam etsin diye Awake/OnDestroy kullanılır.
            AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
            Refresh();
        }

        private void OnDestroy()
        {
            if (AreaManager.Instance != null)
            {
                AreaManager.Instance.AreaUnlocked -= OnAreaUnlocked;
            }
        }

        private void OnAreaUnlocked(AreaDefinition unlocked)
        {
            Refresh();
        }

        // Kare görünmeli mi? (Kendi alanı henüz açılmamış ve gereken alan açılmışsa.)
        private void Refresh()
        {
            AreaManager areas = AreaManager.Instance;
            bool show = !areas.IsUnlocked(area) && areas.IsUnlocked(requiredArea);
            gameObject.SetActive(show);
            UpdateLabel();
        }

        protected override void OnVisitorEnter()
        {
            timer = config.UnlockStartDelay; // basar basmaz değil, kısa bir beklemeden sonra ödemeye başla
        }

        private void Update()
        {
            if (Visitor == null || !VisitorIsPlayer) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.UnlockPayInterval;

            PayOnce();
        }

        // Bir parça para öder. Parça boyutu, fiyatın tamamı UnlockPayDuration saniyede ödenecek şekilde hesaplanır.
        private void PayOnce()
        {
            int ticks = Mathf.Max(1, Mathf.RoundToInt(config.UnlockPayDuration / config.UnlockPayInterval));
            int chunk = Mathf.Max(1, Mathf.CeilToInt((float)area.Price / ticks));

            int remaining = AreaManager.Instance.GetRemaining(area);
            int amount = Mathf.Min(chunk, remaining, CurrencyManager.Instance.Coins);
            if (amount <= 0) return; // para yok

            CurrencyManager.Instance.TrySpend(amount);

            FlyingVisual coin = Instantiate(coinPrefab);
            coin.Launch(Visitor.transform.position, transform.position, config.CoinFlyDuration);

            // Bu çağrı fiyatı tamamlarsa bölge açılır ve bu kare kendini gizler.
            AreaManager.Instance.AddPayment(area, amount);
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (label == null || area == null) return;
            label.text = area.DisplayName + "\n" + AreaManager.Instance.GetRemaining(area);
        }
    }
}
