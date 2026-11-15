// UiBuilder.cs
// Sahne kurma komutunun ekran arayüzü parçaları: geliştirme paneli (alttan açılır), "Aşama tamamlandı!"
// kutlama yazısı ve kaçış ekranı. Hepsi Canvas'ın içine kurulur.

using System.Collections.Generic;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda.EditorTools
{
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

        // ---------------------------------------------------------------- Kutlama ve kaçış

        // Ekranın üst-ortasında yarı saydam kart: büyük altın başlık + beyaz alt yazı.
        // Test Menüsü (sadece Editor ve geliştirme sürümü): sol üstte "TEST" butonu, butonlar oyunda kodla kurulur.
        // Arayüzün en üstünde olsun diye SafeArea'nın son çocuğu.
        public static DevMenu CreateDevMenu(GameConfig config, ResourceType[] resources)
        {
            GameObject go = new GameObject("DevMenu", typeof(RectTransform));
            go.transform.SetParent(Safe, false);
            go.transform.SetAsLastSibling();
            UiFactory.Stretch((RectTransform)go.transform);
            DevMenu menu = go.AddComponent<DevMenu>();
            BuilderUtil.SetField(menu, "config", config);
            BuilderUtil.SetArray(menu, "resources", resources);
            BuilderUtil.SetField(menu, "font", UiFactory.Font);
            BuilderUtil.SetField(menu, "fontMaterial", UiFactory.OutlineMaterial);
            BuilderUtil.SetField(menu, "roundedSprite", ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath));
            return menu;
        }

        // Üstten kayarak gelen geniş kutlama bandı ("OMURGA TAMAMLANDI!" + açılan yeni hedef).
        public static CelebrationBanner CreateCelebrationBanner()
        {
            GameObject holder = new GameObject("CelebrationBanner", typeof(RectTransform));
            holder.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)holder.transform);

            Image band = UiFactory.CreateImage("Root", holder.transform, ArtGenerator.LoadSprite(ArtGenerator.SquarePath), new Color(0.6f, 0.17f, 0.1f, 0.94f));
            Anchor(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(0f, 230f));
            Image stripeTop = UiFactory.CreateImage("StripeTop", band.transform, ArtGenerator.LoadSprite(ArtGenerator.SquarePath), UiFactory.Gold);
            Anchor(stripeTop.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));
            Image stripeBottom = UiFactory.CreateImage("StripeBottom", band.transform, ArtGenerator.LoadSprite(ArtGenerator.SquarePath), UiFactory.Gold);
            Anchor(stripeBottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 10f));
            TextMeshProUGUI title = UiFactory.CreateUIText("Title", band.transform, "OMURGA TAMAMLANDI!", 86f, UiFactory.Gold);
            title.enableAutoSizing = true;
            title.fontSizeMin = 50f;
            title.fontSizeMax = 86f;
            UiFactory.Size(title, new Vector2(0f, 30f), new Vector2(1000f, 120f));
            TextMeshProUGUI subtitle = UiFactory.CreateUIText("Subtitle", band.transform, "", 44f, Color.white);
            subtitle.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.Size(subtitle, new Vector2(0f, -58f), new Vector2(980f, 70f));

            CelebrationBanner banner = holder.AddComponent<CelebrationBanner>();
            BuilderUtil.SetField(banner, "root", band.gameObject);
            BuilderUtil.SetField(banner, "title", title);
            BuilderUtil.SetField(banner, "subtitle", subtitle);
            return banner;
        }

        // Ekranın ortasında esneyerek gelen büyük "AÇILDI!" yazısı.
        public static BurstText CreateBurstText(GameConfig config)
        {
            GameObject holder = new GameObject("BurstText", typeof(RectTransform));
            holder.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)holder.transform);
            RectTransform root = (RectTransform)new GameObject("Root", typeof(RectTransform)).transform;
            root.SetParent(holder.transform, false);
            root.anchoredPosition = new Vector2(0f, 260f);
            root.sizeDelta = new Vector2(1000f, 320f);
            TextMeshProUGUI title = UiFactory.CreateUIText("Title", root, "AÇILDI!", 170f, UiFactory.Gold);
            UiFactory.Size(title, new Vector2(0f, 40f), new Vector2(1000f, 220f));
            TextMeshProUGUI subtitle = UiFactory.CreateUIText("Subtitle", root, "", 60f, Color.white);
            UiFactory.Size(subtitle, new Vector2(0f, -100f), new Vector2(1000f, 90f));
            BurstText burst = holder.AddComponent<BurstText>();
            BuilderUtil.SetField(burst, "config", config);
            BuilderUtil.SetField(burst, "root", root);
            BuilderUtil.SetField(burst, "title", title);
            BuilderUtil.SetField(burst, "subtitle", subtitle);
            return burst;
        }

        // Sağ üstte küçük ayar (dişli) butonu: titreşim ve ses aç/kapa.
        public static SettingsPanel CreateSettings()
        {
            GameObject holder = new GameObject("Settings", typeof(RectTransform));
            holder.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)holder.transform);
            Button toggle = CreateButton("SettingsButton", holder.transform, "AYAR", Vector2.zero, Vector2.zero, new Color(0.18f, 0.22f, 0.3f, 0.9f), 30f);
            Anchor((RectTransform)toggle.transform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -24f), new Vector2(130f, 74f));

            Image panel = UiFactory.CreateImage("Panel", holder.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = true;
            Anchor(panel.rectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -110f), new Vector2(420f, 220f));
            Button vibration = CreateButton("Vibration", panel.transform, "Titreşim: AÇIK", new Vector2(0f, 50f), new Vector2(380f, 80f), Green, 34f);
            Button sound = CreateButton("Sound", panel.transform, "Ses: AÇIK", new Vector2(0f, -50f), new Vector2(380f, 80f), Green, 34f);

            SettingsPanel settings = holder.AddComponent<SettingsPanel>();
            BuilderUtil.SetField(settings, "toggleButton", toggle);
            BuilderUtil.SetField(settings, "panel", panel.gameObject);
            BuilderUtil.SetField(settings, "vibrationButton", vibration);
            BuilderUtil.SetField(settings, "soundButton", sound);
            return settings;
        }

        // Ekranı kaplayan konfeti katmanı: arayüzün en üstünde (kaçış ekranının da önünde).
        public static ConfettiOverlay CreateConfettiOverlay()
        {
            GameObject go = new GameObject("ConfettiOverlay", typeof(RectTransform));
            go.transform.SetParent(Safe, false);
            go.transform.SetAsLastSibling();
            UiFactory.Stretch((RectTransform)go.transform);
            ConfettiOverlay overlay = go.AddComponent<ConfettiOverlay>();
            BuilderUtil.SetField(overlay, "pieceSprite", ArtGenerator.LoadSprite(ArtGenerator.SquarePath));
            return overlay;
        }

        public static EscapePanel CreateEscapePanel(GameConfig config)
        {
            GameObject holder = new GameObject("EscapePanel", typeof(RectTransform));
            holder.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)holder.transform);

            GameObject root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(holder.transform, false);
            UiFactory.Stretch((RectTransform)root.transform);
            Image bg = UiFactory.CreateImage("Background", root.transform, null, new Color(0.05f, 0.18f, 0.32f, 0.62f));
            bg.raycastTarget = true;
            UiFactory.Stretch(bg.rectTransform);

            TextMeshProUGUI title = UiFactory.CreateUIText("Title", root.transform, "KAÇTIN!", 170f, UiFactory.Gold);
            UiFactory.Size(title, new Vector2(0f, 560f), new Vector2(1000f, 240f));
            TextMeshProUGUI sub = UiFactory.CreateUIText("Subtitle", root.transform, "Kaptan adadan kurtuldu!", 54f, Color.white);
            UiFactory.Size(sub, new Vector2(0f, 400f), new Vector2(1000f, 90f));

            // 3 yıldız: arkada koyu boş yıldız, önde dolan altın yıldız.
            List<Object> stars = new List<Object>();
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2((i - 1) * 230f, i == 1 ? 210f : 180f);
                float size = i == 1 ? 210f : 170f;
                Image empty = UiFactory.CreateImage("StarBack " + i, root.transform, ArtGenerator.LoadSprite(ArtGenerator.StarPath), new Color(0f, 0f, 0f, 0.35f));
                UiFactory.Size(empty, pos, new Vector2(size, size));
                Image star = UiFactory.CreateImage("Star " + i, root.transform, ArtGenerator.LoadSprite(ArtGenerator.StarPath), UiFactory.Gold);
                UiFactory.Size(star, pos, new Vector2(size, size));
                stars.Add(star);
            }

            TextMeshProUGUI time = UiFactory.CreateUIText("Time", root.transform, "Süre: 0 dk", 56f, Color.white);
            UiFactory.Size(time, new Vector2(0f, 20f), new Vector2(1000f, 80f));
            TextMeshProUGUI xp = UiFactory.CreateUIText("Xp", root.transform, "+0 Kaptanlık XP", 56f, new Color(0.6f, 1f, 0.6f));
            UiFactory.Size(xp, new Vector2(0f, -80f), new Vector2(1000f, 80f));
            TextMeshProUGUI coins = UiFactory.CreateUIText("Coins", root.transform, "Kazanılan para: 0", 56f, UiFactory.Gold);
            UiFactory.Size(coins, new Vector2(0f, -170f), new Vector2(1000f, 80f));
            TextMeshProUGUI soon = UiFactory.CreateUIText("Soon", root.transform, "Yeni adalar yakında!", 50f, new Color(0.7f, 0.85f, 1f));
            UiFactory.Size(soon, new Vector2(0f, -290f), new Vector2(1000f, 90f));
            Button replay = CreateButton("Replay", root.transform, "TEKRAR OYNA", new Vector2(0f, -470f), new Vector2(620f, 160f), Green, 66f);

            EscapePanel panel = holder.AddComponent<EscapePanel>();
            BuilderUtil.SetField(panel, "config", config);
            BuilderUtil.SetField(panel, "root", root);
            BuilderUtil.SetField(panel, "title", title.rectTransform);
            BuilderUtil.SetArray(panel, "stars", stars.ToArray());
            BuilderUtil.SetField(panel, "timeLabel", time);
            BuilderUtil.SetField(panel, "xpLabel", xp);
            BuilderUtil.SetField(panel, "coinLabel", coins);
            BuilderUtil.SetField(panel, "replayButton", replay);
            return panel;
        }
    }
}
