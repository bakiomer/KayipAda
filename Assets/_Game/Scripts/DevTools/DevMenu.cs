// DevMenu.cs
// Oyun içi Test Menüsü: sol üst köşede küçük "TEST" butonu; basınca soldan dar bir yan panel kayarak açılır
// (ekranın ortasını kapatmaz). Tekrar basınca kayarak kapanır.
// SADECE Unity Editor'da ve "Development Build" sürümlerinde görünür. Satış sürümünde ilk karede kendini siler.
// Butonlar oyun başlarken kodla kurulur (sahnede sadece bu boş nesne durur). Komutlar DevCommands'tadır.
// Menü açıkken oyuncu yürümez (PlayerController.InputBlocked).

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class DevMenu : MonoBehaviour
    {
        public static DevMenu Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [Tooltip("Yığını doldurma seçenekleri (sırayla değiştirilir).")]
        [SerializeField] private ResourceType[] resources;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Material fontMaterial;
        [SerializeField] private Sprite roundedSprite;

        private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.13f, 0.92f);
        private static readonly Color ButtonColor = new Color(0.22f, 0.36f, 0.52f, 1f);
        private static readonly Color ActiveColor = new Color(0.95f, 0.65f, 0.15f, 1f);
        private static readonly Color DangerColor = new Color(0.72f, 0.24f, 0.2f, 1f);

        private GameObject panel;
        private TMP_Text infiniteLabel;
        private Image infiniteButton;
        private TMP_Text resourceLabel;
        private readonly List<Image> speedButtons = new List<Image>();
        private int resourceIndex;

        public bool IsOpen => panel != null && panel.activeSelf && slideTarget > 0f;
        public GameConfig Config => config;

        private void Awake()
        {
            if (!DevCommands.Available)
            {
                Destroy(gameObject); // satış sürümü: menü hiç yok
                return;
            }
            Instance = this;
            Build();
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private const float PanelWidth = 400f;
        private const float RowHeight = 56f;
        private const float FontSize = 25f;
        private const float SlideTime = 0.2f;
        private float slide;      // 0 = kapalı (ekran dışında), 1 = açık
        private float slideTarget;

        public void Open()
        {
            Refresh();
            panel.SetActive(true);
            slideTarget = 1f;
            PlayerController.InputBlocked = true;
        }

        public void Close()
        {
            slideTarget = 0f;
            PlayerController.InputBlocked = false;
        }

        private void Update()
        {
            if (panel == null || Mathf.Approximately(slide, slideTarget)) return;
            slide = Mathf.MoveTowards(slide, slideTarget, Time.unscaledDeltaTime / SlideTime);
            RectTransform rt = (RectTransform)panel.transform;
            rt.anchoredPosition = new Vector2(Mathf.Lerp(-PanelWidth - 30f, 16f, Tween.EaseOutQuad(slide)), rt.anchoredPosition.y);
            if (slide <= 0f) panel.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        // ---------------------------------------------------------------- Kurulum

        private void Build()
        {
            RectTransform root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            // Köşedeki küçük buton.
            Button corner = CreateButton(root, "TEST", ButtonColor, Toggle);
            Place((RectTransform)corner.transform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(130f, 70f));

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup)).gameObject;
            panel.transform.SetParent(root, false);
            Image back = panel.GetComponent<Image>();
            back.sprite = roundedSprite;
            back.type = Image.Type.Sliced;
            back.color = PanelColor;
            RectTransform rt = (RectTransform)panel.transform;
            Place(rt, new Vector2(0f, 1f), new Vector2(-PanelWidth - 30f, -108f), new Vector2(PanelWidth, 0f));
            ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Transform p = panel.transform;
            CreateLabel(p, "TEST MENÜSÜ", 30f, new Color(1f, 0.8f, 0.2f));
            infiniteButton = CreateButton(p, "", ButtonColor, () =>
            {
                DevCommands.SetInfiniteMoney(config, !DevCommands.InfiniteMoney);
                Refresh();
            }).GetComponent<Image>();
            infiniteLabel = infiniteButton.GetComponentInChildren<TMP_Text>();
            CreateButton(p, "+" + config.DevMoneyAmount + " Para", ButtonColor, () => DevCommands.AddMoney(config));
            CreateButton(p, "Tüm Bölgeleri Aç", ButtonColor, DevCommands.UnlockAllRegions);
            CreateButton(p, "Tüm Binaları Aç", ButtonColor, DevCommands.UnlockAllBuildings);
            CreateButton(p, "Her İşçiden 1 Tane", ButtonColor, DevCommands.HireAllWorkers);

            // Malzeme seçimi: <  Kalas  >   ve doldurma butonu.
            Transform row = CreateRow(p);
            CreateButton(row, "<", ButtonColor, () => ChangeResource(-1), 70f);
            resourceLabel = CreateLabel(row, "", FontSize, Color.white);
            resourceLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
            CreateButton(row, ">", ButtonColor, () => ChangeResource(1), 70f);
            CreateButton(p, "Sırttaki Yığını Doldur", ButtonColor, () =>
            {
                if (resources.Length > 0) DevCommands.FillPlayerStack(resources[resourceIndex]);
            });

            CreateButton(p, "Gemi Aşamasını Tamamla", ButtonColor, DevCommands.CompleteShipStage);
            CreateButton(p, "Final Törenine Atla", ButtonColor, () =>
            {
                Close();
                DevCommands.SkipToCeremony();
            });

            Transform speeds = CreateRow(p);
            foreach (float speed in config.DevSpeeds)
            {
                float s = speed;
                Button b = CreateButton(speeds, "x" + s, ButtonColor, () =>
                {
                    DevCommands.SetSpeed(s);
                    Refresh();
                });
                b.GetComponent<LayoutElement>().flexibleWidth = 1f;
                speedButtons.Add(b.GetComponent<Image>());
            }

            // Keşif: ikişerli satırlar (menü ekrana sığsın).
            Transform k1 = CreateRow(p);
            Flex(CreateButton(k1, "+" + config.DevCrystalAmount + " Kristal", ButtonColor, () => DevCommands.AddCrystals(config)));
            Flex(CreateButton(k1, "Tüm Kostümleri Aç", ButtonColor, DevCommands.UnlockAllCostumes));
            Transform k2 = CreateRow(p);
            Flex(CreateButton(k2, "Sıradaki Kostümü Giy", ButtonColor, () => DevCommands.WearNextCostume()));
            Flex(CreateButton(k2, "Tüm Sandıkları Göster", ButtonColor, DevCommands.ToggleShowChests));
            Transform k3 = CreateRow(p);
            Flex(CreateButton(k3, "Koleksiyonu Tamamla", ButtonColor, DevCommands.CompleteCollection));
            Flex(CreateButton(k3, "Görevleri Tamamla", ButtonColor, DevCommands.CompleteQuests));
            Transform k4 = CreateRow(p);
            Flex(CreateButton(k4, "Köprüleri Tamamla", ButtonColor, DevCommands.CompleteBridges));
            Flex(CreateButton(k4, "Bulutları Kaldır/Getir", ButtonColor, DevCommands.ToggleClouds));

            CreateButton(p, "Kaydı Sil ve Yeniden Başlat", DangerColor, DevCommands.ResetSaveAndRestart);
            CreateButton(p, "Kapat", new Color(0.3f, 0.3f, 0.34f, 1f), Close);
        }

        private static void Flex(Button b)
        {
            b.GetComponent<LayoutElement>().flexibleWidth = 1f;
            b.GetComponentInChildren<TMP_Text>().enableAutoSizing = true;
            b.GetComponentInChildren<TMP_Text>().fontSizeMin = 16f;
        }

        private void ChangeResource(int step)
        {
            if (resources.Length == 0) return;
            resourceIndex = (resourceIndex + step + resources.Length) % resources.Length;
            Refresh();
        }

        private void Refresh()
        {
            bool infinite = DevCommands.InfiniteMoney;
            infiniteLabel.text = "Sınırsız Para: " + (infinite ? "AÇIK" : "KAPALI");
            infiniteButton.color = infinite ? ActiveColor : ButtonColor;
            resourceLabel.text = resources.Length > 0 ? resources[resourceIndex].DisplayName : "-";
            for (int i = 0; i < speedButtons.Count; i++)
            {
                speedButtons[i].color = Mathf.Approximately(Time.timeScale, config.DevSpeeds[i]) ? ActiveColor : ButtonColor;
            }
        }

        // ---------------------------------------------------------------- Arayüz yardımcıları

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private Transform CreateRow(Transform parent)
        {
            GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
            return row.transform;
        }

        private TMP_Text CreateLabel(Transform parent, string value, float size, Color color)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            if (fontMaterial != null) text.fontSharedMaterial = fontMaterial;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            go.GetComponent<LayoutElement>().preferredHeight = size * 1.5f;
            return text;
        }

        private Button CreateButton(Transform parent, string value, Color color, Action onClick, float width = -1f)
        {
            GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredHeight = RowHeight;
            if (width > 0f) le.preferredWidth = width;

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                AudioManager.Play(SoundId.Button);
                onClick();
            });

            TMP_Text label = CreateLabel(go.transform, value, FontSize, Color.white);
            RectTransform lr = (RectTransform)label.transform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            Destroy(label.GetComponent<LayoutElement>());
            return button;
        }
    }
}
