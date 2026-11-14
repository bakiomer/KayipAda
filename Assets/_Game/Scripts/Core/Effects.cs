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
        [Tooltip("Gülle patlaması.")]
        [SerializeField] private ParticleSystem explosionPrefab;
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

        // Gülle patlaması.
        public void Explosion(Vector3 position)
        {
            if (explosionPrefab == null) return;
            ParticleSystem ps = Instantiate(explosionPrefab, position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 3f);
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
            FloatingText free = null;
            foreach (FloatingText candidate in texts)
            {
                if (!candidate.IsBusy) { free = candidate; break; }
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
