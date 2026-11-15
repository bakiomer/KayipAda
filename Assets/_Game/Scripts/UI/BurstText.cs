// BurstText.cs
// Ekranın ortasında büyük, esneyerek gelen kutlama yazısı ("AÇILDI!"): önce ince-uzun belirir, yaylanıp
// normal boyuna oturur, kısa süre durur ve kaybolur. Alt satırda neyin açıldığı yazar.
// Süreler GameConfig'te (BurstTextDuration).

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class BurstText : MonoBehaviour
    {
        public static BurstText Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;

        private float age = -1f;

        public bool IsShown => root != null && root.gameObject.activeSelf;

        private void Awake()
        {
            Instance = this;
            root.gameObject.SetActive(false);
        }

        public void Show(string titleText, string subtitleText)
        {
            title.text = titleText;
            subtitle.text = subtitleText ?? "";
            root.gameObject.SetActive(true);
            age = 0f;
            Apply(0f);
        }

        private void Update()
        {
            if (age < 0f) return;
            age += Time.unscaledDeltaTime;
            float t = age / Mathf.Max(0.1f, config.BurstTextDuration);
            if (t >= 1f)
            {
                age = -1f;
                root.gameObject.SetActive(false);
                return;
            }
            Apply(t);
        }

        // Esneme: ilk %25'te ince-uzundan geniş-basığa yaylanıp oturur; son %25'te büyüyerek kaybolur.
        private void Apply(float t)
        {
            Vector3 scale;
            float alpha = 1f;
            if (t < 0.25f)
            {
                float k = t / 0.25f;
                float spring = 1f + Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * 0.45f;
                scale = new Vector3(Mathf.Lerp(0.3f, 1f, Tween.EaseOutBack(k)) * spring, Mathf.Lerp(1.8f, 1f, k) / spring, 1f);
            }
            else if (t > 0.75f)
            {
                float k = (t - 0.75f) / 0.25f;
                scale = Vector3.one * (1f + k * 0.25f);
                alpha = 1f - k;
            }
            else
            {
                scale = Vector3.one;
            }
            root.localScale = scale;
            title.alpha = alpha;
            subtitle.alpha = alpha;
        }
    }
}
