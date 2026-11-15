// DevMenu.cs
// Oyun içi Test Menüsü: sol üst köşede küçük "TEST" butonu; basınca komut listesi açılır.
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

        public bool IsOpen => panel != null && panel.activeSelf;
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

        public void Open()
        {
            Refresh();
            panel.SetActive(true);
            PlayerController.InputBlocked = true;
        }

        public void Close()
        {
            panel.SetActive(false);
            PlayerController.InputBlocked = false;
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
            Place((RectTransform)corner.transform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(150f, 74f));

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup)).gameObject;
            panel.transform.SetParent(root, false);
            Image back = panel.GetComponent<Image>();
            back.sprite = roundedSprite;
            back.type = Image.Type.Sliced;
            back.color = PanelColor;
            RectTransform rt = (RectTransform)panel.transform;
            Place(rt, new Vector2(0f, 1f), new Vector2(24f, -112f), new Vector2(620f, 0f));
            ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Transform p = panel.transform;
            CreateLabel(p, "TEST MENÜSÜ", 40f, new Color(1f, 0.8f, 0.2f));
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
            CreateButton(row, "<", ButtonColor, () => ChangeResource(-1), 90f);
            resourceLabel = CreateLabel(row, "", 34f, Color.white);
            resourceLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
            CreateButton(row, ">", ButtonColor, () => ChangeResource(1), 90f);
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

            CreateButton(p, "Kaydı Sil ve Yeniden Başlat", DangerColor, DevCommands.ResetSaveAndRestart);
            CreateButton(p, "Kapat", new Color(0.3f, 0.3f, 0.34f, 1f), Close);
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
            row.GetComponent<LayoutElement>().preferredHeight = 74f;
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
            le.preferredHeight = 74f;
            if (width > 0f) le.preferredWidth = width;

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());

            TMP_Text label = CreateLabel(go.transform, value, 32f, Color.white);
            RectTransform lr = (RectTransform)label.transform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            Destroy(label.GetComponent<LayoutElement>());
            return button;
        }
    }
}
