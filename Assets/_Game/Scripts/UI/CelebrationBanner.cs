// CelebrationBanner.cs
// Ekranın üstünden kayarak gelen geniş kutlama bandı: "OMURGA TAMAMLANDI!" + alt satırda açılan yeni hedef.
// Yukarıdan iner, süre dolunca yukarı çekilip kaybolur. Gemi aşamaları ve final töreni kullanır.

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class CelebrationBanner : MonoBehaviour
    {
        public static CelebrationBanner Instance { get; private set; }

        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;

        private float hideAt;
        private float shownAt = -1f;
        private RectTransform rect;
        private Vector2 restPosition;
        private const float SlideTime = 0.35f;
        private const float SlideHeight = 320f; // band ekranın dışından (yukarıdan) bu kadar kayar

        public bool IsShown => root != null && root.activeSelf;

        private void Awake()
        {
            Instance = this;
            rect = (RectTransform)root.transform;
            restPosition = rect.anchoredPosition;
            if (root != null) root.SetActive(false);
        }

        public void Show(string titleText, string subtitleText, float duration)
        {
            title.text = titleText;
            subtitle.text = subtitleText ?? "";
            subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitleText));
            root.SetActive(true);
            shownAt = Time.unscaledTime;
            hideAt = Time.unscaledTime + duration;
            Slide(0f);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            float now = Time.unscaledTime;
            if (now < hideAt) Slide(Mathf.Clamp01((now - shownAt) / SlideTime));
            else if (now < hideAt + SlideTime) Slide(1f - (now - hideAt) / SlideTime);
            else root.SetActive(false);
        }

        // 0 = ekranın üstünde (görünmez), 1 = yerinde. İnerken hafifçe aşıp oturur.
        private void Slide(float k)
        {
            rect.anchoredPosition = restPosition + Vector2.up * (SlideHeight * (1f - Tween.EaseOutBack(k)));
        }
    }
}
