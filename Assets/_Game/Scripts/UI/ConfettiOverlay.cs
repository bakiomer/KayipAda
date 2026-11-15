// ConfettiOverlay.cs
// Ekranı kaplayan konfeti (final töreni ve kaçış ekranı): arayüzün en üstünde çizilen, yukarıdan dönerek düşen
// renkli küçük kareler. Parçalar oyun başında bir kez oluşturulur ve tekrar kullanılır (havuz).
// Play(saniye) süresince parçalar yeniden yukarıdan doğar; süre bitince kalanlar düşüp çıkar.

using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class ConfettiOverlay : MonoBehaviour
    {
        public static ConfettiOverlay Instance { get; private set; }

        [SerializeField] private Sprite pieceSprite;
        [Tooltip("Aynı anda ekrandaki konfeti parçası sayısı.")]
        [SerializeField] private int pieceCount = 90;

        private static readonly Color[] Colors =
        {
            new Color(1f, 0.85f, 0.2f), new Color(1f, 0.35f, 0.45f), new Color(0.35f, 0.75f, 1f),
            new Color(0.45f, 0.95f, 0.45f), new Color(0.85f, 0.5f, 1f), new Color(1f, 0.6f, 0.2f)
        };

        private RectTransform[] pieces;
        private Vector2[] velocity;
        private float[] spin;
        private float[] phase;
        private float playUntil = -1f;
        private bool running;

        public bool IsPlaying => running;

        private void Awake()
        {
            Instance = this;
            pieces = new RectTransform[pieceCount];
            velocity = new Vector2[pieceCount];
            spin = new float[pieceCount];
            phase = new float[pieceCount];
            for (int i = 0; i < pieceCount; i++)
            {
                GameObject go = new GameObject("Piece", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                Image image = go.GetComponent<Image>();
                image.sprite = pieceSprite;
                image.color = Colors[i % Colors.Length];
                image.raycastTarget = false;
                pieces[i] = (RectTransform)go.transform;
                pieces[i].sizeDelta = new Vector2(Random.Range(18f, 30f), Random.Range(10f, 18f));
                go.SetActive(false);
            }
        }

        public void Play(float seconds)
        {
            playUntil = Time.unscaledTime + seconds;
            if (running) return;
            running = true;
            RectTransform area = (RectTransform)transform;
            for (int i = 0; i < pieces.Length; i++) Respawn(i, area, Random.Range(0f, area.rect.height)); // ilk dalga ekrana yayılmış
        }

        private void Respawn(int i, RectTransform area, float extraHeight)
        {
            RectTransform p = pieces[i];
            p.gameObject.SetActive(true);
            p.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * area.rect.width, area.rect.height * 0.5f + 40f + extraHeight * 0.5f);
            p.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            velocity[i] = new Vector2(Random.Range(-60f, 60f), -Random.Range(280f, 520f));
            spin[i] = Random.Range(-360f, 360f);
            phase[i] = Random.Range(0f, 10f);
        }

        private void Update()
        {
            if (!running) return;
            RectTransform area = (RectTransform)transform;
            float dt = Time.unscaledDeltaTime;
            bool anyAlive = false;
            bool spawning = Time.unscaledTime < playUntil;
            for (int i = 0; i < pieces.Length; i++)
            {
                RectTransform p = pieces[i];
                if (!p.gameObject.activeSelf) continue;
                Vector2 pos = p.anchoredPosition + velocity[i] * dt;
                pos.x += Mathf.Sin(Time.unscaledTime * 3f + phase[i]) * 40f * dt; // salınarak düşer
                p.anchoredPosition = pos;
                p.localRotation *= Quaternion.Euler(spin[i] * 0.3f * dt, 0f, spin[i] * dt);
                if (pos.y < -area.rect.height * 0.5f - 40f)
                {
                    if (spawning) Respawn(i, area, 0f);
                    else p.gameObject.SetActive(false);
                }
                anyAlive |= p.gameObject.activeSelf;
            }
            if (!anyAlive && !spawning) running = false;
        }
    }
}
