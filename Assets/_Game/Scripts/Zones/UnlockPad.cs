// UnlockPad.cs
// Açma karesi (bölge, bina ve işçi tutma için): yerde yuvarlak bir halka, üstünde ad ve büyük fiyat yazısı.
// Oyuncu üstünde durunca parası küçük parçalar halinde kareye uçar; halkanın kenarı ödendikçe dolar.
// Oyuncu çıkarsa ödenen kısım kaybolmaz (AreaManager'da saklanır). Fiyat bitince konfeti patlar, alan açılır,
// kare küçülerek kaybolur. "Gereken alan" atanmışsa kare, o alan açılana kadar gizli kalır; açılınca zıplayarak belirir.

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class UnlockPad : InteractionZone
    {
        [SerializeField] private AreaDefinition area;
        [Tooltip("Boş değilse: bu kare ancak bu alan açıldıktan sonra görünür.")]
        [SerializeField] private AreaDefinition requiredArea;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private ProgressRing ring;

        public AreaDefinition Area => area;

        [Tooltip("Sonraki hedef olunca nabız gibi büyüyüp küçülen görsel (halka).")]
        [SerializeField] private Transform pulseVisual;

        private float timer;
        private bool hiding;
        private bool highlighted;
        private Vector3 baseScale;
        private Vector3 pulseBaseScale;

        // Açılmayı bekleyen, görünür ve kapanmakta olmayan kare mi? (Yönlendirme okunun adayı.)
        public bool IsAvailable => gameObject.activeInHierarchy && !hiding && !AreaManager.Instance.IsUnlocked(area);

        // Yönlendirme sistemi sonraki hedefi işaretler: halka nabız gibi atar.
        public void SetHighlighted(bool value)
        {
            highlighted = value;
            if (!value && pulseVisual != null) pulseVisual.localScale = pulseBaseScale;
        }

        private void Awake()
        {
            if (area == null || priceLabel == null || ring == null)
            {
                Debug.LogError(name + ": Area, Price Label veya Ring atanmamış!", this);
                enabled = false;
                return;
            }

            baseScale = transform.localScale;
            if (pulseVisual != null) pulseBaseScale = pulseVisual.localScale;
            if (nameLabel != null) nameLabel.text = area.DisplayName;

            // Kare kendini gizlese bile olayı dinlemeye devam etsin diye Awake/OnDestroy kullanılır.
            AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
            gameObject.SetActive(ShouldShow());
            UpdateLabel();
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
            if (unlocked == area)
            {
                // Bu kare tamamlandı: konfeti, ses, küçülerek kaybol.
                if (Effects.Instance != null) Effects.Instance.Confetti(transform.position + Vector3.up * 0.5f);
                AudioManager.Play(SoundId.Unlock);
                hiding = true;
                Tween.PopOut(transform, 0.3f, () => gameObject.SetActive(false));
                return;
            }

            // Gereken alan açıldıysa zıplayarak belir.
            if (!gameObject.activeSelf && ShouldShow())
            {
                gameObject.SetActive(true);
                Tween.PopIn(transform, baseScale, config.PopInDuration, 0.3f);
                UpdateLabel();
            }
        }

        // Kare görünmeli mi? (Kendi alanı henüz açılmamış ve gereken alan açılmışsa.)
        private bool ShouldShow()
        {
            AreaManager areas = AreaManager.Instance;
            return !areas.IsUnlocked(area) && areas.IsUnlocked(requiredArea);
        }

        protected override void OnVisitorEnter(CarryStack visitor)
        {
            // Basar basmaz değil, kısa bir beklemeden sonra ödemeye başla. İşçiler para ödemez.
            if (visitor.IsPlayer) timer = config.UnlockStartDelay;
        }

        private void Update()
        {
            if (hiding) return;
            if (highlighted && pulseVisual != null)
            {
                float pulse = 1f + 0.08f * (0.5f + 0.5f * Mathf.Sin(Time.time * config.GuidePulseSpeed));
                pulseVisual.localScale = pulseBaseScale * pulse;
            }
            CarryStack player = FindPlayer();
            if (player == null) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.UnlockPayInterval;

            PayOnce(player);
        }

        private CarryStack FindPlayer()
        {
            for (int i = 0; i < Visitors.Count; i++)
            {
                if (Visitors[i] != null && Visitors[i].IsPlayer) return Visitors[i];
            }
            return null;
        }

        // Bir parça para öder. Parça boyutu, fiyatın tamamı UnlockPayDuration saniyede ödenecek şekilde hesaplanır.
        private void PayOnce(CarryStack player)
        {
            int ticks = Mathf.Max(1, Mathf.RoundToInt(config.UnlockPayDuration / config.UnlockPayInterval));
            int chunk = Mathf.Max(1, Mathf.CeilToInt((float)AreaManager.Instance.PriceOf(area) / ticks));

            int remaining = AreaManager.Instance.GetRemaining(area);
            int amount = Mathf.Min(chunk, remaining, CurrencyManager.Instance.Coins);
            if (amount <= 0) return; // para yok

            CurrencyManager.Instance.TrySpend(amount);
            if (Effects.Instance != null)
            {
                Effects.Instance.FlyCoin(player.transform.position + Vector3.up * 1.2f, transform.position + Vector3.up * 0.2f);
            }
            AudioManager.Play(SoundId.Coin, 0.5f);

            // Bu çağrı fiyatı tamamlarsa alan açılır ve OnAreaUnlocked bu kareyi gizler.
            AreaManager.Instance.AddPayment(area, amount);
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            int remaining = AreaManager.Instance.GetRemaining(area);
            priceLabel.text = remaining.ToString();
            ring.SetProgress(1f - (float)remaining / Mathf.Max(1, AreaManager.Instance.PriceOf(area)));
        }
    }
}
