// CelebrationBanner.cs
// Ekranın ortasında kısa kutlama yazısı: "Omurga tamamlandı!" + alt satırda açılan yeni hedef.
// Zıplayarak belirir, süre dolunca küçülerek kaybolur. Gemi aşamaları ve final töreni kullanır.

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

        public bool IsShown => root != null && root.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
        }

        public void Show(string titleText, string subtitleText, float duration)
        {
            title.text = titleText;
            subtitle.text = subtitleText ?? "";
            subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitleText));
            root.SetActive(true);
            Tween.PopIn(root.transform, Vector3.one, 0.4f);
            hideAt = Time.time + duration;
        }

        private void Update()
        {
            if (root == null || !root.activeSelf || hideAt <= 0f || Time.time < hideAt) return;
            hideAt = 0f;
            Tween.PopOut(root.transform, 0.25f, () => root.SetActive(false));
        }
    }
}
