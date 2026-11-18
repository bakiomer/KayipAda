// UiBuilder.cs
// Sahne kurma komutunun ekran arayüzü parçaları: geliştirme paneli (alttan açılır), "Aşama tamamlandı!"
// kutlama yazısı ve kaçış ekranı. Hepsi Canvas'ın içine kurulur.

using System.Collections.Generic;
using TMPro;
using UnityEditor;
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
            return CreatePanel("UpgradePanel", "GELİŞTİRMELER", config, types, new List<WorkerType>());
        }

        // Tayfa Barakası paneli: üstte işçi kartları (resim, sayı, TUT + fiyat, kilit yazısı), altta işçi geliştirmeleri.
        public static UpgradePanel CreateCrewPanel(GameConfig config, List<WorkerType> workers, List<Object> upgradeTypes)
        {
            return CreatePanel("CrewPanel", "TAYFA BARAKASI", config, upgradeTypes, workers);
        }

        private const float CardHeight = 440f;

        private static UpgradePanel CreatePanel(string panelName, string titleText, GameConfig config, List<Object> types, List<WorkerType> workers)
        {
            GameObject root = new GameObject(panelName, typeof(RectTransform));
            root.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)root.transform);

            Image dimmer = UiFactory.CreateImage("Dimmer", root.transform, null, new Color(0f, 0f, 0f, 0.35f));
            dimmer.raycastTarget = true;
            UiFactory.Stretch(dimmer.rectTransform);
            dimmer.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            float rowHeight = 150f, gap = 16f;
            float cardsHeight = workers.Count > 0 ? CardHeight + gap : 0f;
            float height = 200f + cardsHeight + types.Count * (rowHeight + gap);
            Image sheet = UiFactory.CreateImage("Sheet", root.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            sheet.type = Image.Type.Sliced;
            sheet.raycastTarget = true; // panele dokunmak joystick'i başlatmasın
            Anchor(sheet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-48f, height));

            TextMeshProUGUI title = UiFactory.CreateUIText("Title", sheet.transform, titleText, 60f, UiFactory.Gold);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0f, 90f));

            Button close = CreateButton("Close", sheet.transform, "X", Vector2.zero, new Vector2(100f, 100f), new Color(0.85f, 0.3f, 0.3f), 54f);
            Anchor((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(100f, 100f));

            List<Object> cards = new List<Object>();
            if (workers.Count > 0)
            {
                const float sheetWidth = 1032f, inner = sheetWidth - 48f;
                float cardWidth = (inner - (workers.Count - 1) * gap) / workers.Count;
                for (int i = 0; i < workers.Count; i++)
                {
                    float x = -inner * 0.5f + cardWidth * 0.5f + i * (cardWidth + gap);
                    cards.Add(CreateCrewCard(sheet.transform, workers[i], new Vector2(x, -140f), new Vector2(cardWidth, CardHeight)));
                }
            }
            List<Object> rows = new List<Object>();
            for (int i = 0; i < types.Count; i++)
            {
                rows.Add(CreateUpgradeRow(sheet.transform, (UpgradeType)types[i], -140f - cardsHeight - i * (rowHeight + gap), rowHeight));
            }

            UpgradePanel panel = root.AddComponent<UpgradePanel>();
            BuilderUtil.SetField(panel, "config", config);
            BuilderUtil.SetField(panel, "sheet", sheet.rectTransform);
            BuilderUtil.SetField(panel, "dimmer", dimmer.gameObject);
            BuilderUtil.SetField(panel, "closeButton", close);
            BuilderUtil.SetArray(panel, "rows", rows.ToArray());
            BuilderUtil.SetArray(panel, "cards", cards.ToArray());
            return panel;
        }

        // İşçi kartı: üstte resim (dairesel zemin), ad, "1/3", altta TUT + para + fiyat; kilitliyse kilit yazısı.
        private static CrewCard CreateCrewCard(Transform sheet, WorkerType type, Vector2 topCenter, Vector2 size)
        {
            Image bg = UiFactory.CreateImage("Card " + (type != null ? type.Id : "?"), sheet, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), RowColor);
            bg.type = Image.Type.Sliced;
            Anchor(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), topCenter, size);
            Transform card = bg.transform;

            Image circle = UiFactory.CreateImage("PortraitBack", card, ArtGenerator.LoadSprite(ArtGenerator.CirclePath), new Color(0.35f, 0.62f, 0.85f, 0.55f));
            Anchor(circle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(190f, 190f));
            Image portrait = UiFactory.CreateImage("Portrait", card, type != null ? type.Portrait : null, Color.white);
            portrait.preserveAspect = true;
            Anchor(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(200f, 200f));

            TextMeshProUGUI name = UiFactory.CreateUIText("Name", card, type != null ? type.DisplayName : "", 44f, Color.white);
            Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(-16f, 56f));
            TextMeshProUGUI count = UiFactory.CreateUIText("Count", card, "0/1", 40f, UiFactory.Gold);
            Anchor(count.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(-16f, 50f));

            float buttonWidth = size.x - 28f;
            Button hire = CreateButton("Hire", card, "TUT", Vector2.zero, new Vector2(buttonWidth, 104f), Green, 40f);
            Anchor((RectTransform)hire.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(buttonWidth, 104f));
            TMP_Text buttonLabel = hire.GetComponentInChildren<TMP_Text>();
            buttonLabel.alignment = TextAlignmentOptions.MidlineLeft;
            buttonLabel.rectTransform.offsetMin = new Vector2(18f, 0f);
            Image coin = UiFactory.CreateImage("Coin", hire.transform, ArtGenerator.LoadSprite(ArtGenerator.CoinIconPath), Color.white);
            Anchor(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-buttonWidth * 0.08f, 0f), new Vector2(46f, 46f));
            TextMeshProUGUI price = UiFactory.CreateUIText("Price", hire.transform, "0", 42f, Color.white);
            price.alignment = TextAlignmentOptions.MidlineRight;
            Anchor(price.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(0f, 0f));
            price.rectTransform.offsetMin = new Vector2(10f, 0f);
            price.rectTransform.offsetMax = new Vector2(-14f, 0f);

            // Kilit: düğme yerine koyu şerit + "Bıçkıhane gerekli".
            Image lockBg = UiFactory.CreateImage("Lock", card, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0f, 0f, 0f, 0.45f));
            lockBg.type = Image.Type.Sliced;
            Anchor(lockBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(buttonWidth, 104f));
            TextMeshProUGUI lockText = UiFactory.CreateUIText("Text", lockBg.transform, type != null ? type.LockedText : "", 34f, new Color(1f, 0.75f, 0.45f));
            UiFactory.Stretch(lockText.rectTransform);
            lockText.enableAutoSizing = true;
            lockText.fontSizeMin = 22f;
            lockText.fontSizeMax = 34f;

            CrewCard component = bg.gameObject.AddComponent<CrewCard>();
            BuilderUtil.SetField(component, "type", type);
            BuilderUtil.SetField(component, "portrait", portrait);
            BuilderUtil.SetField(component, "nameLabel", name);
            BuilderUtil.SetField(component, "countLabel", count);
            BuilderUtil.SetField(component, "hireButton", hire);
            BuilderUtil.SetField(component, "buttonImage", hire.GetComponent<Image>());
            BuilderUtil.SetField(component, "buttonLabel", buttonLabel);
            BuilderUtil.SetField(component, "priceLabel", price);
            BuilderUtil.SetField(component, "coinIcon", coin.gameObject);
            BuilderUtil.SetField(component, "lockRoot", lockBg.gameObject);
            BuilderUtil.SetField(component, "lockLabel", lockText);
            return component;
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

        // ---------------------------------------------------------------- Keşif arayüzü

        // Para panelinin altında küçük kristal göstergesi (mor taş + sayı).
        public static CrystalUI CreateCrystalPanel(Sprite crystalIcon)
        {
            GameObject holder = new GameObject("CrystalPanel", typeof(RectTransform));
            holder.transform.SetParent(Safe, false);
            RectTransform rt = (RectTransform)holder.transform;
            Anchor(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(130f, -172f), new Vector2(170f, 64f));
            Image back = UiFactory.CreateImage("Back", holder.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0.1f, 0.13f, 0.19f, 0.85f));
            back.type = Image.Type.Sliced;
            UiFactory.Stretch(back.rectTransform);
            Image icon = UiFactory.CreateImage("Icon", holder.transform, crystalIcon, Color.white);
            UiFactory.Size(icon, new Vector2(-50f, 0f), new Vector2(54f, 54f));
            icon.preserveAspect = true;
            TextMeshProUGUI label = UiFactory.CreateUIText("Count", holder.transform, "0", 44f, new Color(0.88f, 0.78f, 1f));
            label.alignment = TextAlignmentOptions.MidlineLeft;
            UiFactory.Size(label, new Vector2(30f, 2f), new Vector2(110f, 60f));
            CrystalUI ui = holder.AddComponent<CrystalUI>();
            BuilderUtil.SetField(ui, "label", label);
            BuilderUtil.SetField(ui, "punchTarget", rt);
            return ui;
        }

        // Sol üstte (TEST butonunun altında) en fazla iki yan görev kartı.
        public static List<Object> CreateQuestCards(int count, Sprite crystalIcon)
        {
            // Kartlar sol üstte alt alta (küçülen kartın altındaki yukarı kayar).
            GameObject list = new GameObject("QuestCards", typeof(RectTransform));
            list.transform.SetParent(Safe, false);
            Anchor((RectTransform)list.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -118f), new Vector2(404f, 300f));
            VerticalLayoutGroup group = list.AddComponent<VerticalLayoutGroup>();
            group.spacing = 10f;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            Sprite sell = ArtGenerator.LoadSprite(ArtGenerator.CoinIconPath), plank = IconGenerator.Create("plank");
            Sprite chest = IconGenerator.Create("chest"), figure = IconGenerator.Create("figure");
            List<Object> cards = new List<Object>();
            for (int i = 0; i < count; i++)
            {
                GameObject holder = new GameObject("QuestCard " + i, typeof(RectTransform));
                holder.transform.SetParent(list.transform, false);
                LayoutElement layout = holder.AddComponent<LayoutElement>();
                layout.preferredHeight = 132f;
                GameObject bodyGo = new GameObject("Body", typeof(RectTransform));
                bodyGo.transform.SetParent(holder.transform, false);
                RectTransform body = (RectTransform)bodyGo.transform;
                UiFactory.Stretch(body);
                Image bg = UiFactory.CreateImage("Back", body, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0.1f, 0.13f, 0.19f, 0.9f));
                bg.type = Image.Type.Sliced;
                bg.raycastTarget = true; // dokununca küçülür
                UiFactory.Stretch(bg.rectTransform);
                Button toggle = bodyGo.AddComponent<Button>();
                toggle.targetGraphic = bg;
                toggle.transition = Selectable.Transition.None;
                // Görev türü ikonu (solda, net).
                Image kind = UiFactory.CreateImage("Icon", body, sell, Color.white);
                kind.preserveAspect = true;
                Anchor(kind.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(50f, -33f), new Vector2(76f, 76f));
                Image badge = UiFactory.CreateImage("Badge", kind.transform, ArtGenerator.LoadSprite(ArtGenerator.StarPath), UiFactory.Gold);
                Anchor(badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-4f, -4f), new Vector2(26f, 26f));
                TextMeshProUGUI title = UiFactory.CreateUIText("Title", body, "Görev", 40f, Color.white);
                title.alignment = TextAlignmentOptions.MidlineLeft;
                title.enableAutoSizing = true;
                title.fontSizeMin = 26f;
                title.fontSizeMax = 40f;
                title.richText = true;
                Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(98f, -8f), new Vector2(-108f, 50f));
                GameObject details = new GameObject("Details", typeof(RectTransform));
                details.transform.SetParent(body, false);
                UiFactory.Stretch((RectTransform)details.transform);
                Transform d = details.transform;
                Image barBack = UiFactory.CreateImage("BarBack", d, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0f, 0f, 0f, 0.45f));
                barBack.type = Image.Type.Sliced;
                Anchor(barBack.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(98f, -64f), new Vector2(170f, 18f));
                Image fill = UiFactory.CreateImage("Fill", d, ArtGenerator.LoadSprite(ArtGenerator.SquarePath), new Color(0.55f, 0.9f, 0.35f));
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                Anchor(fill.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(102f, -68f), new Vector2(162f, 10f));
                TextMeshProUGUI countText = UiFactory.CreateUIText("Count", d, "0/10", 36f, UiFactory.Gold);
                countText.alignment = TextAlignmentOptions.MidlineLeft;
                Anchor(countText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -52f), new Vector2(120f, 46f));
                Image coin = UiFactory.CreateImage("Coin", d, ArtGenerator.LoadSprite(ArtGenerator.CoinIconPath), Color.white);
                Anchor(coin.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(98f, 8f), new Vector2(38f, 38f));
                TextMeshProUGUI coinText = UiFactory.CreateUIText("CoinText", d, "", 32f, Color.white);
                coinText.alignment = TextAlignmentOptions.MidlineLeft;
                Anchor(coinText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(140f, 6f), new Vector2(100f, 42f));
                Image gem = UiFactory.CreateImage("Crystal", d, crystalIcon, Color.white);
                gem.preserveAspect = true;
                Anchor(gem.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(236f, 8f), new Vector2(38f, 38f));
                TextMeshProUGUI gemText = UiFactory.CreateUIText("CrystalText", d, "", 32f, new Color(0.88f, 0.78f, 1f));
                gemText.alignment = TextAlignmentOptions.MidlineLeft;
                Anchor(gemText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(278f, 6f), new Vector2(100f, 42f));

                QuestCard card = holder.AddComponent<QuestCard>();
                BuilderUtil.SetField(card, "icon", kind);
                BuilderUtil.SetField(card, "details", details);
                BuilderUtil.SetField(card, "layout", layout);
                BuilderUtil.SetField(card, "sellIcon", sell);
                BuilderUtil.SetField(card, "repairIcon", plank);
                BuilderUtil.SetField(card, "chestIcon", chest);
                BuilderUtil.SetField(card, "exploreIcon", ArtGenerator.LoadSprite(ArtGenerator.ArrowPath));
                BuilderUtil.SetField(card, "figureIcon", figure);
                BuilderUtil.SetField(card, "body", body);
                BuilderUtil.SetField(card, "background", bg);
                BuilderUtil.SetField(card, "title", title);
                BuilderUtil.SetField(card, "count", countText);
                BuilderUtil.SetField(card, "fill", fill);
                BuilderUtil.SetField(card, "coinIcon", coin.gameObject);
                BuilderUtil.SetField(card, "coinText", coinText);
                BuilderUtil.SetField(card, "crystalIcon", gem.gameObject);
                BuilderUtil.SetField(card, "crystalText", gemText);
                cards.Add(card);
            }
            return cards;
        }

        // Koleksiyon kartı: ortada dönen 3D görünüş (vitrin kamerası → RenderTexture), ad, "Koleksiyon 2/5".
        public static CollectionPopup CreateCollectionPopup(GameConfig config, RenderTexture texture, Camera showcaseCamera, Transform slot)
        {
            GameObject holder = new GameObject("CollectionPopup", typeof(RectTransform));
            holder.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)holder.transform);
            GameObject root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(holder.transform, false);
            UiFactory.Stretch((RectTransform)root.transform);
            Image dim = UiFactory.CreateImage("Dim", root.transform, null, new Color(0f, 0f, 0f, 0.45f));
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform);
            Button close = dim.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            Image card = UiFactory.CreateImage("Card", root.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            card.type = Image.Type.Sliced;
            UiFactory.Size(card, new Vector2(0f, 80f), new Vector2(720f, 860f));
            TextMeshProUGUI head = UiFactory.CreateUIText("Head", card.transform, "YENİ KOLEKSİYON!", 52f, UiFactory.Gold);
            UiFactory.Size(head, new Vector2(0f, 360f), new Vector2(680f, 80f));
            Image glow = UiFactory.CreateImage("Glow", card.transform, ArtGenerator.LoadSprite(ArtGenerator.SoftCirclePath), new Color(0.8f, 0.65f, 1f, 0.6f));
            UiFactory.Size(glow, new Vector2(0f, 60f), new Vector2(560f, 560f));
            GameObject rawGo = new GameObject("View", typeof(RectTransform));
            rawGo.transform.SetParent(card.transform, false);
            RawImage raw = rawGo.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            ((RectTransform)rawGo.transform).anchoredPosition = new Vector2(0f, 60f);
            ((RectTransform)rawGo.transform).sizeDelta = new Vector2(480f, 480f);
            TextMeshProUGUI name = UiFactory.CreateUIText("Name", card.transform, "Eski Pusula", 58f, Color.white);
            UiFactory.Size(name, new Vector2(0f, -240f), new Vector2(680f, 80f));
            TextMeshProUGUI count = UiFactory.CreateUIText("Count", card.transform, "Koleksiyon 1/5", 46f, new Color(0.88f, 0.78f, 1f));
            UiFactory.Size(count, new Vector2(0f, -320f), new Vector2(680f, 70f));

            CollectionPopup popup = holder.AddComponent<CollectionPopup>();
            BuilderUtil.SetField(popup, "config", config);
            BuilderUtil.SetField(popup, "root", root);
            BuilderUtil.SetField(popup, "card", card.rectTransform);
            BuilderUtil.SetField(popup, "nameLabel", name);
            BuilderUtil.SetField(popup, "countLabel", count);
            BuilderUtil.SetField(popup, "closeButton", close);
            BuilderUtil.SetField(popup, "showcaseCamera", showcaseCamera);
            BuilderUtil.SetField(popup, "showcaseSlot", slot);
            return popup;
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

            TextMeshProUGUI time = UiFactory.CreateUIText("Time", root.transform, "Süre: 0 dk", 54f, Color.white);
            UiFactory.Size(time, new Vector2(0f, 30f), new Vector2(1000f, 76f));
            TextMeshProUGUI xp = UiFactory.CreateUIText("Xp", root.transform, "+0 Kaptanlık XP", 52f, new Color(0.6f, 1f, 0.6f));
            UiFactory.Size(xp, new Vector2(0f, -50f), new Vector2(1000f, 76f));
            TextMeshProUGUI coins = UiFactory.CreateUIText("Coins", root.transform, "Kazanılan para: 0", 52f, UiFactory.Gold);
            UiFactory.Size(coins, new Vector2(0f, -128f), new Vector2(1000f, 76f));
            TextMeshProUGUI crystals = UiFactory.CreateUIText("Crystals", root.transform, "+0 Kristal", 52f, new Color(0.88f, 0.78f, 1f));
            UiFactory.Size(crystals, new Vector2(0f, -206f), new Vector2(1000f, 76f));
            TextMeshProUGUI collection = UiFactory.CreateUIText("Collection", root.transform, "Koleksiyon 0/5    Sandıklar 0/8", 48f, Color.white);
            UiFactory.Size(collection, new Vector2(0f, -284f), new Vector2(1000f, 76f));
            TextMeshProUGUI soon = UiFactory.CreateUIText("Soon", root.transform, "Yeni adalar yakında!", 44f, new Color(0.7f, 0.85f, 1f));
            UiFactory.Size(soon, new Vector2(0f, -362f), new Vector2(1000f, 76f));
            Button replay = CreateButton("Replay", root.transform, "TEKRAR OYNA", new Vector2(0f, -510f), new Vector2(620f, 150f), Green, 64f);
            // Efsanevi gemi rozeti (altın figür takılıysa).
            Image badge = UiFactory.CreateImage("Legendary", root.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0.75f, 0.15f, 0.12f, 0.95f));
            badge.type = Image.Type.Sliced;
            UiFactory.Size(badge, new Vector2(300f, 330f), new Vector2(430f, 96f));
            badge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            TextMeshProUGUI badgeText = UiFactory.CreateUIText("Text", badge.transform, "EFSANEVİ GEMİ!", 50f, UiFactory.Gold);
            UiFactory.Stretch(badgeText.rectTransform);

            EscapePanel panel = holder.AddComponent<EscapePanel>();
            BuilderUtil.SetField(panel, "config", config);
            BuilderUtil.SetField(panel, "root", root);
            BuilderUtil.SetField(panel, "title", title.rectTransform);
            BuilderUtil.SetArray(panel, "stars", stars.ToArray());
            BuilderUtil.SetField(panel, "timeLabel", time);
            BuilderUtil.SetField(panel, "xpLabel", xp);
            BuilderUtil.SetField(panel, "coinLabel", coins);
            BuilderUtil.SetField(panel, "crystalLabel", crystals);
            BuilderUtil.SetField(panel, "collectionLabel", collection);
            BuilderUtil.SetField(panel, "legendaryBadge", badge.gameObject);
            BuilderUtil.SetField(panel, "replayButton", replay);
            return panel;
        }
    
        // ---------------------------------------------------------------- Bina yükseltme paneli (Yükselt tabelası)

        // Alttan kayan, yürümeyi engellemeyen küçük panel: bina adı, seviye noktaları, hız/kapasite artışı, Yükselt butonu.
        public static BuildingUpgradePanel CreateBuildingUpgradePanel(GameConfig config)
        {
            GameObject root = new GameObject("BuildingUpgradePanel", typeof(RectTransform));
            root.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)root.transform);
            Image sheet = UiFactory.CreateImage("Sheet", root.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            sheet.type = Image.Type.Sliced;
            sheet.raycastTarget = true;
            Anchor(sheet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-48f, 420f));

            TextMeshProUGUI title = UiFactory.CreateUIText("Title", sheet.transform, "Bıçkıhane", 60f, UiFactory.Gold);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(40f, -24f), new Vector2(-80f, 80f));
            TextMeshProUGUI level = UiFactory.CreateUIText("Level", sheet.transform, "Seviye 1/3", 40f, UiFactory.Cream);
            level.alignment = TextAlignmentOptions.MidlineRight;
            Anchor(level.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(300f, 60f));
            List<Image> dots = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                Image dot = UiFactory.CreateImage("Dot " + i, sheet.transform, ArtGenerator.LoadSprite(ArtGenerator.CirclePath), Color.white);
                Anchor(dot.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-230f + i * 52f, -112f), new Vector2(40f, 40f));
                dots.Add(dot);
            }
            TextMeshProUGUI speed = UiFactory.CreateUIText("Speed", sheet.transform, "Hız ×1.00 → ×1.35", 46f, Color.white);
            speed.alignment = TextAlignmentOptions.MidlineLeft;
            speed.richText = true;
            Anchor(speed.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(40f, -150f), new Vector2(-80f, 64f));
            TextMeshProUGUI capacity = UiFactory.CreateUIText("Capacity", sheet.transform, "Kapasite +0 → +6", 46f, Color.white);
            capacity.alignment = TextAlignmentOptions.MidlineLeft;
            capacity.richText = true;
            Anchor(capacity.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(40f, -220f), new Vector2(-80f, 64f));

            Button buy = CreateButton("Buy", sheet.transform, "", Vector2.zero, new Vector2(520f, 110f), Green, 50f);
            Anchor((RectTransform)buy.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(560f, 110f));
            TextMeshProUGUI priceText = buy.GetComponentInChildren<TextMeshProUGUI>();
            priceText.text = "Yükselt  350";
            priceText.fontSize = 50f;
            Image coin = UiFactory.CreateImage("Coin", buy.transform, ArtGenerator.LoadSprite(ArtGenerator.CoinIconPath), Color.white);
            Anchor(coin.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(64f, 64f));

            BuildingUpgradePanel panel = root.AddComponent<BuildingUpgradePanel>();
            BuilderUtil.SetField(panel, "config", config);
            BuilderUtil.SetField(panel, "sheet", sheet.rectTransform);
            BuilderUtil.SetField(panel, "title", title);
            BuilderUtil.SetField(panel, "levelText", level);
            BuilderUtil.SetArray(panel, "levelDots", dots.ToArray());
            BuilderUtil.SetField(panel, "speedText", speed);
            BuilderUtil.SetField(panel, "capacityText", capacity);
            BuilderUtil.SetField(panel, "buyButton", buy);
            BuilderUtil.SetField(panel, "priceText", priceText);
            BuilderUtil.SetField(panel, "priceIcon", coin.gameObject);
            return panel;
        }
    
        // ---------------------------------------------------------------- Ambar paneli

        // Alttan kayan, yürümeyi engellemeyen panel: "AMBAR 37/100", 5 × 2 kaynak kutusu (ikon + miktar), "Al" butonu.
        public static StoragePanel CreateStoragePanel(GameConfig config, int slotCount)
        {
            GameObject root = new GameObject("StoragePanel", typeof(RectTransform));
            root.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)root.transform);
            Image sheet = UiFactory.CreateImage("Sheet", root.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            sheet.type = Image.Type.Sliced;
            sheet.raycastTarget = true;
            Anchor(sheet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-48f, 560f));
            TextMeshProUGUI title = UiFactory.CreateUIText("Title", sheet.transform, "AMBAR  0/100", 56f, UiFactory.Gold);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(0f, 76f));
            const int columns = 5;
            const float cell = 176f, gap = 16f;
            float startX = -(columns * cell + (columns - 1) * gap) * 0.5f + cell * 0.5f;
            SerializedObject so;
            List<StoragePanel.Slot> slots = new List<StoragePanel.Slot>();
            for (int i = 0; i < slotCount; i++)
            {
                int cx = i % columns, cy = i / columns;
                Image frame = UiFactory.CreateImage("Slot " + i, sheet.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(1f, 1f, 1f, 0.12f));
                frame.type = Image.Type.Sliced;
                frame.raycastTarget = true;
                Anchor(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(startX + cx * (cell + gap), -190f - cy * (cell * 0.86f + gap)), new Vector2(cell, cell * 0.86f));
                Button button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                button.transition = Selectable.Transition.None;
                Image icon = UiFactory.CreateImage("Icon", frame.transform, null, Color.white);
                icon.preserveAspect = true;
                Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(90f, 90f));
                TextMeshProUGUI count = UiFactory.CreateUIText("Count", frame.transform, "0", 40f, Color.white);
                Anchor(count.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(0f, 48f));
                slots.Add(new StoragePanel.Slot { Button = button, Icon = icon, Count = count, Frame = frame });
            }
            Button take = CreateButton("Take", sheet.transform, "Al", Vector2.zero, new Vector2(560f, 100f), Green, 48f);
            Anchor((RectTransform)take.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(620f, 100f));
            TextMeshProUGUI takeText = take.GetComponentInChildren<TextMeshProUGUI>();

            StoragePanel panel = root.AddComponent<StoragePanel>();
            BuilderUtil.SetField(panel, "config", config);
            BuilderUtil.SetField(panel, "sheet", sheet.rectTransform);
            BuilderUtil.SetField(panel, "title", title);
            BuilderUtil.SetField(panel, "takeButton", take);
            BuilderUtil.SetField(panel, "takeText", takeText);
            so = new SerializedObject(panel);
            SerializedProperty list = so.FindProperty("slots");
            list.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Button").objectReferenceValue = slots[i].Button;
                e.FindPropertyRelative("Icon").objectReferenceValue = slots[i].Icon;
                e.FindPropertyRelative("Count").objectReferenceValue = slots[i].Count;
                e.FindPropertyRelative("Frame").objectReferenceValue = slots[i].Frame;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }
    
        // ---------------------------------------------------------------- Görev panosu paneli (kamp)

        public static QuestBoardPanel CreateQuestBoardPanel()
        {
            GameObject root = new GameObject("QuestBoardPanel", typeof(RectTransform));
            root.transform.SetParent(Safe, false);
            UiFactory.Stretch((RectTransform)root.transform);
            Image sheet = UiFactory.CreateImage("Sheet", root.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            sheet.type = Image.Type.Sliced;
            sheet.raycastTarget = true;
            Anchor(sheet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-48f, 760f));
            TextMeshProUGUI title = UiFactory.CreateUIText("Title", sheet.transform, "GÖREV PANOSU", 54f, UiFactory.Gold);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(0f, 76f));
            TextMeshProUGUI activeHead = UiFactory.CreateUIText("ActiveHead", sheet.transform, "Aktif", 40f, UiFactory.Cream);
            activeHead.alignment = TextAlignmentOptions.MidlineLeft;
            Anchor(activeHead.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(40f, -100f), new Vector2(-80f, 50f));
            TextMeshProUGUI active = UiFactory.CreateUIText("Active", sheet.transform, "", 40f, Color.white);
            active.alignment = TextAlignmentOptions.TopLeft;
            active.textWrappingMode = TextWrappingModes.Normal;
            active.richText = true;
            Anchor(active.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(60f, -155f), new Vector2(-100f, 160f));
            TextMeshProUGUI doneHead = UiFactory.CreateUIText("DoneHead", sheet.transform, "Tamamlanan", 40f, UiFactory.Cream);
            doneHead.alignment = TextAlignmentOptions.MidlineLeft;
            Anchor(doneHead.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(40f, -330f), new Vector2(-80f, 50f));
            TextMeshProUGUI done = UiFactory.CreateUIText("Done", sheet.transform, "", 34f, new Color(0.7f, 0.95f, 0.65f));
            done.alignment = TextAlignmentOptions.TopLeft;
            done.textWrappingMode = TextWrappingModes.Normal;
            Anchor(done.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(60f, -385f), new Vector2(-100f, 360f));
            QuestBoardPanel panel = root.AddComponent<QuestBoardPanel>();
            BuilderUtil.SetField(panel, "sheet", sheet.rectTransform);
            BuilderUtil.SetField(panel, "title", title);
            BuilderUtil.SetField(panel, "activeText", active);
            BuilderUtil.SetField(panel, "doneText", done);
            return panel;
        }
    }
}
