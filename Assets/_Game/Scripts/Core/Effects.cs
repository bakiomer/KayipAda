// Effects.cs
// Görsel efektlerin tek giriş noktası: konfeti, zıplayan para, uçan para, "+5" yazıları.
// Sahnede tek bir tane bulunur (Effects.Instance). Sık kullanılan nesneler (yazılar, paralar)
// havuzda tutulur; her seferinde yeniden oluşturulmaz. Prefab'lar sahne kurma komutundan gelir.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-90)]
    public class Effects : MonoBehaviour
    {
        public static Effects Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [Tooltip("Alan açılınca patlayan konfeti (tek seferlik partikül).")]
        [SerializeField] private ParticleSystem confettiPrefab;
        [Tooltip("Uçan/zıplayan para modeli.")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private FloatingText floatingTextPrefab;
        [Tooltip("Küçük parlama (geliştirme, gemi teslimi).")]
        [SerializeField] private ParticleSystem sparklePrefab;
        [Tooltip("Gemi suya inince su sıçraması.")]
        [SerializeField] private ParticleSystem splashPrefab;
        [Tooltip("Final törenindeki havai fişek.")]
        [SerializeField] private ParticleSystem fireworkPrefab;
        [Tooltip("Gemi parçası yerine oturunca kalkan toz bulutu (tek seferlik).")]
        [SerializeField] private ParticleSystem puffPrefab;
        [Tooltip("Gemi aşaması bitince altın ışık patlaması.")]
        [SerializeField] private ParticleSystem goldBurstPrefab;
        [Tooltip("Final töreninde ekranı kaplayan konfeti (kameranın önünde).")]
        [SerializeField] private ParticleSystem screenConfettiPrefab;
        [Tooltip("'+5' yazılarının rengi.")]
        [SerializeField] private Color coinTextColor = new Color(1f, 0.85f, 0.2f);

        private readonly List<FloatingText> texts = new List<FloatingText>();
        private readonly Stack<GameObject> coinPool = new Stack<GameObject>();

        private void Awake()
        {
            Instance = this;
        }

        // Küçük parlama (geliştirme satın alma, gemiye teslim): konfetinin küçük hali.
        public void Sparkle(Vector3 position)
        {
            if (sparklePrefab == null) { Confetti(position); return; }
            ParticleSystem ps = Instantiate(sparklePrefab, position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 3f);
        }

        public void Splash(Vector3 position) => PlayOnce(splashPrefab, position, 3f);

        // Küçük sıçrama (balık): normal sıçramanın küçültülmüş hali.
        public void SmallSplash(Vector3 position)
        {
            if (splashPrefab == null) return;
            ParticleSystem ps = Instantiate(splashPrefab, position, Quaternion.identity);
            ps.transform.localScale = Vector3.one * 0.3f;
            ParticleSystem.MainModule main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSizeMultiplier *= 0.35f;
            main.startSpeedMultiplier *= 0.4f;
            ps.Play();
            Destroy(ps.gameObject, 2f);
        }

        // Havai fişek: yerden yükselen, iz bırakan roket gökyüzünde renkli patlar.
        public void Firework(Vector3 position) => PlayOnce(fireworkPrefab, position, 5f);

        public void Dust(Vector3 position) => PlayOnce(puffPrefab, position, 2f);

        public void GoldBurst(Vector3 position) => PlayOnce(goldBurstPrefab, position, 3f);

        // Ekranı kaplayan konfeti: kameranın önüne yapışık, verilen süre boyunca yağar.
        public void ScreenConfetti(float seconds)
        {
            // Önce arayüz katmanı (her şeyin önünde, her zaman görünür); yoksa kameraya bağlı parçacık.
            if (ConfettiOverlay.Instance != null)
            {
                ConfettiOverlay.Instance.Play(seconds);
                return;
            }
            Camera cam = Camera.main;
            if (screenConfettiPrefab == null || cam == null) return;
            ParticleSystem ps = Instantiate(screenConfettiPrefab, cam.transform);
            ps.transform.localPosition = new Vector3(0f, 3f, 8f);
            ps.transform.localRotation = Quaternion.identity;
            ParticleSystem.MainModule main = ps.main;
            main.duration = Mathf.Max(0.5f, seconds);
            ps.Play();
            Destroy(ps.gameObject, seconds + main.startLifetime.constantMax + 0.5f);
        }

        private void PlayOnce(ParticleSystem prefab, Vector3 position, float life)
        {
            if (prefab == null) return;
            ParticleSystem ps = Instantiate(prefab, position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, life);
        }

        public void Confetti(Vector3 position)
        {
            if (confettiPrefab == null) return;
            ParticleSystem ps = Instantiate(confettiPrefab, position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 4f);
        }

        // Pazarda satış: yerden para fırlar, dönerek zıplar ve kaybolur; üstünde "+5" yazısı çıkar.
        public void CoinPop(Vector3 position, int amount)
        {
            ShowText(position + Vector3.up * 1.6f, "+" + amount, coinTextColor);

            GameObject coin = GetCoin();
            if (coin == null) return;
            Transform t = coin.transform;
            Vector3 start = position + Vector3.up * 0.4f;
            Vector3 side = new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), 0f, UnityEngine.Random.Range(-0.4f, 0.4f));
            Tween.Value(t, 0.6f, k =>
            {
                t.position = start + side * k + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.5f);
                t.rotation = Quaternion.Euler(0f, k * 720f, 0f);
                t.localScale = Vector3.one * (k < 0.8f ? 1f : 1f - (k - 0.8f) / 0.2f);
            }, () => ReleaseCoin(coin));
        }

        // Para from'dan to'ya kavis çizerek uçar (açma karesine ödeme).
        public void FlyCoin(Vector3 from, Vector3 to, Action onArrive = null)
        {
            GameObject coin = GetCoin();
            if (coin == null)
            {
                onArrive?.Invoke();
                return;
            }
            coin.transform.localScale = Vector3.one;
            Tween.MoveArc(coin.transform, from, () => to, 1.5f, config.CoinFlyDuration, () =>
            {
                ReleaseCoin(coin);
                onArrive?.Invoke();
            });
        }

        public void ShowText(Vector3 position, string text, Color color)
        {
            if (floatingTextPrefab == null) return;
            float near = config.FloatingTextMergeDistance;

            // Aynı yazı yakında hâlâ görünüyorsa yenisini açma, onu baştan oynat.
            foreach (FloatingText active in texts)
            {
                if (active.IsBusy && active.Text == text && (active.Anchor - position).sqrMagnitude < near * near)
                {
                    active.Restart();
                    return;
                }
            }

            // Farklı yazı: yakındaki eski yazılar yukarı kayar, yenisi alttan çıkar.
            FloatingText free = null;
            foreach (FloatingText candidate in texts)
            {
                if (candidate.IsBusy)
                {
                    if ((candidate.Anchor - position).sqrMagnitude < near * near) candidate.Bump(config.FloatingTextStackGap);
                }
                else if (free == null)
                {
                    free = candidate;
                }
            }
            if (free == null)
            {
                free = Instantiate(floatingTextPrefab, transform);
                texts.Add(free);
            }
            free.Play(position, text, color, config.FloatingTextDuration);
        }

        private GameObject GetCoin()
        {
            if (coinPrefab == null) return null;
            GameObject coin = coinPool.Count > 0 ? coinPool.Pop() : Instantiate(coinPrefab, transform);
            coin.SetActive(true);
            return coin;
        }

        private void ReleaseCoin(GameObject coin)
        {
            if (coin == null) return;
            coin.SetActive(false);
            coinPool.Push(coin);
        }
    }
}
