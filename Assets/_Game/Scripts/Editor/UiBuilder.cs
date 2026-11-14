// UiBuilder.cs
// Sahne kurma komutunun ekran arayüzü parçaları: geliştirme paneli (alttan açılır), gece arayüzü
// (gemi can çubuğu, büyük duyuru yazısı, "Tekrar dene" paneli) ve kaçış ekranı. Hepsi Canvas'ın içine kurulur.

using System.Collections.Generic;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda.EditorTools
{
    public class NightUi
    {
        public GameObject HealthBar;
        public Image HealthFill;
        public TMP_Text Banner;
        public GameObject DefeatPanel;
        public Button RetryButton;
        public EscapePanel Escape;
    }

    public static class UiBuilder
    {
        public static readonly Color PanelColor = new Color(0.10f, 0.13f, 0.19f, 0.96f);
        public static readonly Color RowColor = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color Green = new Color(0.36f, 0.78f, 0.36f);

        private static Transform Safe => GameObject.Find("Canvas/SafeArea").transform;

        public static Button CreateButton(string name, Transform parent, string text, Vector2 pos, Vector2 size, Color color, float fontSize)
        {
            Image image = UiFactory.CreateImage(name, parent, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), color);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            UiFactory.Size(image, pos, size);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TextMeshProUGUI label = UiFactory.CreateUIText("Label", image.transform, text, fontSize, Color.white);
            UiFactory.Stretch(label.rectTransform);
            return button;
        }

        private static RectTransform Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        // ---------------------------------------------------------------- Geliştirme paneli

        public static UpgradePanel CreateUpgradePanel(GameConfig config, List<Object> types)
        {
            GameObject root = new GameObject("UpgradePanel", typeof(RectTransform));
            root.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)root.transform);

            Image dimmer = UiFactory.CreateImage("Dimmer", root.transform, null, new Color(0f, 0f, 0f, 0.35f));
            dimmer.raycastTarget = true;
            UiFactory.Stretch(dimmer.rectTransform);
            dimmer.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            float rowHeight = 150f, gap = 16f;
            float height = 200f + types.Count * (rowHeight + gap);
            Image sheet = UiFactory.CreateImage("Sheet", root.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            sheet.type = Image.Type.Sliced;
            sheet.raycastTarget = true; // panele dokunmak joystick'i başlatmasın
            Anchor(sheet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-48f, height));

            TextMeshProUGUI title = UiFactory.CreateUIText("Title", sheet.transform, "GELİŞTİRMELER", 60f, UiFactory.Gold);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0f, 90f));

            Button close = CreateButton("Close", sheet.transform, "X", Vector2.zero, new Vector2(100f, 100f), new Color(0.85f, 0.3f, 0.3f), 54f);
            Anchor((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(100f, 100f));

            List<Object> rows = new List<Object>();
            for (int i = 0; i < types.Count; i++)
            {
                rows.Add(CreateUpgradeRow(sheet.transform, (UpgradeType)types[i], -140f - i * (rowHeight + gap), rowHeight));
            }

            UpgradePanel panel = root.AddComponent<UpgradePanel>();
            BuilderUtil.SetField(panel, "config", config);
            BuilderUtil.SetField(panel, "sheet", sheet.rectTransform);
            BuilderUtil.SetField(panel, "dimmer", dimmer.gameObject);
            BuilderUtil.SetField(panel, "closeButton", close);
            BuilderUtil.SetArray(panel, "rows", rows.ToArray());
            return panel;
        }

        private static UpgradeRow CreateUpgradeRow(Transform sheet, UpgradeType type, float y, float height)
        {
            Image bg = UiFactory.CreateImage("Row " + (type != null ? type.Id : "?"), sheet, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), RowColor);
            bg.type = Image.Type.Sliced;
            Anchor(bg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(-48f, height));
            Transform row = bg.transform;

            TextMeshProUGUI name = UiFactory.CreateUIText("Name", row, type != null ? type.DisplayName : "", 44f, Color.white);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            Anchor(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, 28f), new Vector2(560f, 60f));

            TextMeshProUGUI effect = UiFactory.CreateUIText("Effect", row, "", 36f, new Color(0.6f, 1f, 0.6f));
            effect.alignment = TextAlignmentOptions.MidlineLeft;
            Anchor(effect.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, -32f), new Vector2(360f, 50f));

            List<Object> pips = new List<Object>();
            int levels = type != null ? type.MaxLevel : 5;
            for (int i = 0; i < levels; i++)
            {
                Image pip = UiFactory.CreateImage("Pip " + i, row, ArtGenerator.LoadSprite(ArtGenerator.CirclePath), Color.white);
                Anchor(pip.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f + i * 40f, -32f), new Vector2(30f, 30f));
                pips.Add(pip);
            }

            Button buy = CreateButton("Buy", row, "", Vector2.zero, new Vector2(270f, 112f), Green, 46f);
            Anchor((RectTransform)buy.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(270f, 112f));
            TMP_Text price = buy.GetComponentInChildren<TMP_Text>();
            price.rectTransform.offsetMin = new Vector2(80f, 0f);
            price.alignment = TextAlignmentOptions.MidlineLeft;
            Image coin = UiFactory.CreateImage("Coin", buy.transform, ArtGenerator.LoadSprite(ArtGenerator.CoinIconPath), Color.white);
            Anchor(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(56f, 56f));

            UpgradeRow component = bg.gameObject.AddComponent<UpgradeRow>();
            BuilderUtil.SetField(component, "type", type);
            BuilderUtil.SetField(component, "nameLabel", name);
            BuilderUtil.SetField(component, "effectLabel", effect);
            BuilderUtil.SetArray(component, "pips", pips.ToArray());
            BuilderUtil.SetField(component, "buyButton", buy);
            BuilderUtil.SetField(component, "buttonImage", buy.GetComponent<Image>());
            BuilderUtil.SetField(component, "priceLabel", price);
            BuilderUtil.SetField(component, "coinIcon", coin.gameObject);
            return component;
        }

        // ---------------------------------------------------------------- Gece arayüzü ve kaçış

        public static NightUi CreateNightUi()
        {
            NightUi ui = new NightUi();
            Transform safe = Safe;

            // Gemi can çubuğu (para panelinin altında)
            GameObject bar = new GameObject("ShipHealth", typeof(RectTransform));
            bar.transform.SetParent(safe, false);
            Anchor((RectTransform)bar.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(620f, 64f));
            Image back = UiFactory.CreateImage("Back", bar.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0f, 0f, 0f, 0.6f));
            back.type = Image.Type.Sliced;
            UiFactory.Stretch(back.rectTransform);
            Image fill = UiFactory.CreateImage("Fill", bar.transform, ArtGenerator.LoadSprite(ArtGenerator.SquarePath), new Color(0.9f, 0.25f, 0.25f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            UiFactory.Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(10f, 10f);
            fill.rectTransform.offsetMax = new Vector2(-10f, -10f);
            TextMeshProUGUI barLabel = UiFactory.CreateUIText("Label", bar.transform, "GEMİ", 38f, Color.white);
            UiFactory.Stretch(barLabel.rectTransform);
            ui.HealthBar = bar;
            ui.HealthFill = fill;

            // Büyük duyuru yazısı
            TextMeshProUGUI banner = UiFactory.CreateUIText("Banner", safe, "", 84f, UiFactory.Gold);
            Anchor(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 380f), new Vector2(1000f, 260f));
            banner.textWrappingMode = TextWrappingModes.Normal;
            ui.Banner = banner;

            // Kaybetme paneli
            GameObject defeat = new GameObject("DefeatPanel", typeof(RectTransform));
            defeat.transform.SetParent(safe, false);
            UiFactory.Stretch((RectTransform)defeat.transform);
            Image dim = UiFactory.CreateImage("Dim", defeat.transform, null, new Color(0f, 0f, 0f, 0.6f));
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform);
            Image card = UiFactory.CreateImage("Card", defeat.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            card.type = Image.Type.Sliced;
            UiFactory.Size(card, Vector2.zero, new Vector2(860f, 620f));
            TextMeshProUGUI title = UiFactory.CreateUIText("Title", card.transform, "Gemi ağır hasar aldı!", 60f, new Color(1f, 0.55f, 0.45f));
            UiFactory.Size(title, new Vector2(0f, 180f), new Vector2(800f, 100f));
            TextMeshProUGUI info = UiFactory.CreateUIText("Info", card.transform, "Merak etme, ilerlemen korunuyor.\nToplara taş taşı ve tekrar dene!", 38f, Color.white);
            info.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.Size(info, new Vector2(0f, 30f), new Vector2(760f, 160f));
            ui.RetryButton = CreateButton("Retry", card.transform, "TEKRAR DENE", new Vector2(0f, -180f), new Vector2(520f, 130f), Green, 54f);
            ui.DefeatPanel = defeat;

            ui.Escape = CreateEscapePanel(safe);
            return ui;
        }

        private static EscapePanel CreateEscapePanel(Transform safe)
        {
            GameObject holder = new GameObject("EscapePanel", typeof(RectTransform));
            holder.transform.SetParent(safe, false);
            UiFactory.Stretch((RectTransform)holder.transform);

            GameObject root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(holder.transform, false);
            UiFactory.Stretch((RectTransform)root.transform);
            Image bg = UiFactory.CreateImage("Background", root.transform, null, new Color(0.05f, 0.18f, 0.32f, 0.88f));
            bg.raycastTarget = true;
            UiFactory.Stretch(bg.rectTransform);

            TextMeshProUGUI title = UiFactory.CreateUIText("Title", root.transform, "KAÇTIN!", 150f, UiFactory.Gold);
            UiFactory.Size(title, new Vector2(0f, 520f), new Vector2(1000f, 220f));
            TextMeshProUGUI sub = UiFactory.CreateUIText("Subtitle", root.transform, "Kaptan adadan kurtuldu!", 50f, Color.white);
            UiFactory.Size(sub, new Vector2(0f, 360f), new Vector2(1000f, 90f));
            TextMeshProUGUI time = UiFactory.CreateUIText("Time", root.transform, "Süre: 0 dk", 56f, Color.white);
            UiFactory.Size(time, new Vector2(0f, 160f), new Vector2(1000f, 90f));
            TextMeshProUGUI xp = UiFactory.CreateUIText("Xp", root.transform, "+100 Kaptanlık XP", 56f, new Color(0.6f, 1f, 0.6f));
            UiFactory.Size(xp, new Vector2(0f, 0f), new Vector2(1000f, 170f));
            TextMeshProUGUI soon = UiFactory.CreateUIText("Soon", root.transform, "Yeni adalar yakında!", 50f, new Color(0.7f, 0.85f, 1f));
            UiFactory.Size(soon, new Vector2(0f, -200f), new Vector2(1000f, 90f));
            Button replay = CreateButton("Replay", root.transform, "TEKRAR OYNA", new Vector2(0f, -420f), new Vector2(600f, 150f), Green, 60f);

            EscapePanel panel = holder.AddComponent<EscapePanel>();
            BuilderUtil.SetField(panel, "root", root);
            BuilderUtil.SetField(panel, "timeLabel", time);
            BuilderUtil.SetField(panel, "xpLabel", xp);
            BuilderUtil.SetField(panel, "replayButton", replay);
            return panel;
        }
    }
}
