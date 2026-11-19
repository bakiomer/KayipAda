// CollectionPopup.cs
// Koleksiyon eşyası bulununca ekranın ortasında açılan kart: eşyanın dönen 3D görünüşü (ayrı bir vitrin kamerası
// eşyayı ışıklı arka planda çeker), adı ve "Koleksiyon 2/5". Dokununca ya da birkaç saniye sonra kapanır.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class CollectionPopup : MonoBehaviour
    {
        public static CollectionPopup Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private Button closeButton;
        [Tooltip("Vitrin kamerası (RenderTexture'a çizer) ve eşyanın konduğu nokta.")]
        [SerializeField] private Camera showcaseCamera;
        [SerializeField] private Transform showcaseSlot;

        private GameObject model;
        private float shownAt;

        public bool IsShown => root.activeSelf;

        private void Awake()
        {
            Instance = this;
            root.SetActive(false);
            if (showcaseCamera != null) showcaseCamera.enabled = false;
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void Show(Collectible item, int found, int total)
        {
            nameLabel.text = item.DisplayName;
            countLabel.text = "Koleksiyon " + found + "/" + total;
            if (model != null) Destroy(model);
            if (item.ShowcaseModel != null && showcaseSlot != null)
            {
                model = Instantiate(item.ShowcaseModel, showcaseSlot, false);
                foreach (Transform t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = showcaseSlot.gameObject.layer;
            }
            if (showcaseCamera != null) showcaseCamera.enabled = true;
            root.SetActive(true);
            shownAt = Time.unscaledTime;
            card.localScale = Vector3.zero;
        }

        public void Hide()
        {
            if (!IsShown) return;
            root.SetActive(false);
            if (showcaseCamera != null) showcaseCamera.enabled = false;
            if (model != null) Destroy(model);
        }

        private void Update()
        {
            if (!IsShown) return;
            float t = Time.unscaledTime - shownAt;
            card.localScale = Vector3.one * Tween.EaseOutBack(Mathf.Clamp01(t / 0.35f));
            if (showcaseSlot != null) showcaseSlot.localRotation = Quaternion.Euler(-12f, t * 80f, 0f);
            if (t > config.CollectionPopupTime) Hide();
        }
    }
}
