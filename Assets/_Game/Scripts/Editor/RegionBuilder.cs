// RegionBuilder.cs
// Bölgelerin kimliği ve çevresi (sahne kurma komutu çağırır):
// - Kapalı bölge çalılığı (RegionThicket): sık çalı + sarmaşık, geçilmez; açılınca kesilir.
// - Kayalık = taş ocağı (arkada kaya yüzü, ahşap iskele, raylar + maden arabası, kazma rafı, kırık taş yığınları),
//   Meyve Bahçesi (kemerli ahşap giriş, sulama kuyusu, meyve sepetleri/kasaları, ağaç sıraları, alçak süslü çit),
//   Palmiye Koyu / keten tarlası (kurutma rafları, bağlanmış lif demetleri).
// - Tanıtım tabelası kartı (InfoSign), bina "Yükselt" tabelası, denize uzanan uzun iskele (Pier), gemi kızağı rayları.

using System.Collections.Generic;
using KayipAda;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace KayipAda.EditorTools
{
    public static class RegionBuilder
    {
        private static Material Wood => ArtGenerator.GetLit("M_ShipWood", null, MapLayout.ShipWoodColor);
        private static Material Plank => ArtGenerator.GetLit("M_ShipPlank", null, MapLayout.ShipPlankColor);
        private static Material PlankDark => ArtGenerator.GetLit("M_BridgePlankDark", null, new Color(0.7f, 0.5f, 0.3f));
        private static Material Rope => ArtGenerator.GetLit("M_MachineRope", null, new Color(0.78f, 0.6f, 0.36f));
        private static Material Iron => ArtGenerator.GetLit("M_Steel", null, new Color(0.42f, 0.44f, 0.48f));
        private static Material Lantern => ArtGenerator.GetLit("M_LanternGlow", null, new Color(1f, 0.86f, 0.42f));
        private static Material SignWood => ArtGenerator.GetLit("M_SignWood", null, MapLayout.SignWoodColor);
        private static Material Fiber => ArtGenerator.GetLit("M_FiberBundle", null, MapLayout.ItemFiberColor);
        private static Material Twine => ArtGenerator.GetLit("M_Twine", null, new Color(0.55f, 0.36f, 0.18f));

        private static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material m)
        {
            GameObject go = MachineBuilder.Part(parent, type, name, pos, scale, rot, m);
            return go;
        }

        private static GameObject Prop(Dictionary<string, GameObject> decor, string key, Transform parent, Vector3 worldPos, float yaw, float scale = 1f)
        {
            if (!decor.TryGetValue(key, out GameObject prefab)) return null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static void Block(Transform parent, Vector3 worldPos, Vector2 size)
        {
            GameObject go = new GameObject("Block");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            BuilderUtil.SetVector2(go.AddComponent<NavObstacle>(), "size", size);
        }

        private static float R(System.Random r, float amount) => ((float)r.NextDouble() * 2f - 1f) * amount;

        // Çalılık yaprakları doğal, koyu tonlarda (Kenney paletinin parlak yeşili yerine).
        private static Material LeafMid => ArtGenerator.GetLit("M_ThicketLeaf", null, new Color(0.3f, 0.52f, 0.2f));
        private static Material LeafDark => ArtGenerator.GetLit("M_ThicketLeafDark", null, new Color(0.22f, 0.42f, 0.17f));
        private static Material LeafVine => ArtGenerator.GetLit("M_ThicketVine", null, new Color(0.36f, 0.58f, 0.24f));

        private static void Paint(GameObject go, Material m)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }

        // ================================================================ Kayalık = taş ocağı

        public static void BuildQuarry(Transform parent, Rect rect, Dictionary<string, GameObject> decor)
        {
            Transform root = new GameObject("Quarry").transform;
            root.SetParent(parent, false);
            // Arka kenarda açık gri-bej, makul boyda kaya grupları (içine girilmez, kenarından kayılır).
            Material light = ArtGenerator.GetLit("M_QuarryRockLight", null, new Color(0.78f, 0.75f, 0.68f));
            string[] rocks = { "QuarryRockA", "QuarryRockC", "QuarryRockB", "QuarryRockA", "QuarryRockC", "QuarryRockB" };
            for (int i = 0; i < rocks.Length; i++)
            {
                float x = rect.xMin + 0.8f + (rect.width - 1.6f) * i / (rocks.Length - 1f);
                GameObject c = Prop(decor, rocks[i], root, new Vector3(x, 0f, rect.yMax + 0.4f + (i % 2) * 0.6f), i * 47f, 1f + (i % 3) * 0.15f);
                if (c == null) continue;
                Paint(c, light);
                Bounds cb = PrefabFactory.WorldBounds(c);
                GameObject blk = new GameObject("Block");
                blk.transform.SetParent(root, false);
                blk.transform.position = new Vector3(cb.center.x, 0f, cb.center.z);
                NavObstacle o = blk.AddComponent<NavObstacle>();
                BuilderUtil.SetVector2(o, "size", new Vector2(cb.size.x * 0.9f, cb.size.z * 0.9f));
                BuilderUtil.SetFloat(o, "cornerRadius", Mathf.Min(cb.size.x, cb.size.z) * 0.4f);
            }

            // Raylar + maden arabası (batı kenarı boyunca, kaya yüzüne doğru).
            Transform rails = new GameObject("Rails").transform;
            rails.SetParent(root, false);
            float rx = rect.xMin + 0.55f, z0 = rect.yMin + 3.2f, z1 = rect.yMax + 0.3f;
            for (int s = -1; s <= 1; s += 2)
                Part(rails, PrimitiveType.Cube, "Rail", new Vector3(rx + s * 0.32f, 0.06f, (z0 + z1) * 0.5f), new Vector3(0.06f, 0.06f, z1 - z0), Quaternion.identity, Iron);
            for (float z = z0 + 0.2f; z < z1; z += 0.55f)
                Part(rails, PrimitiveType.Cube, "Sleeper", new Vector3(rx, 0.03f, z), new Vector3(0.95f, 0.05f, 0.16f), Quaternion.identity, PlankDark);
            Transform cart = new GameObject("MineCart").transform;
            cart.SetParent(rails, false);
            cart.position = new Vector3(rx, 0f, z1 - 1.6f);
            Part(cart, PrimitiveType.Cube, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.78f, 0.5f, 1.05f), Quaternion.identity, Wood);
            Part(cart, PrimitiveType.Cube, "Band", new Vector3(0f, 0.72f, 0f), new Vector3(0.82f, 0.08f, 1.09f), Quaternion.identity, Iron);
            for (int i = 0; i < 4; i++)
                Part(cart, PrimitiveType.Cylinder, "Wheel", new Vector3((i % 2 == 0 ? -0.36f : 0.36f), 0.19f, (i < 2 ? -0.32f : 0.32f)),
                     new Vector3(0.32f, 0.04f, 0.32f), Quaternion.Euler(0f, 0f, 90f), Iron);
            Prop(decor, "RockChunk", cart, cart.position + new Vector3(0.05f, 0.75f, 0.1f), 30f, 0.9f);
            Prop(decor, "RockChunk", cart, cart.position + new Vector3(-0.12f, 0.78f, -0.2f), 120f, 0.8f);
            Block(root, cart.position, new Vector2(0.9f, 1.2f));

            // Kazma rafı (doğu kenarı) ve kırık taş yığınları.
            Transform rack = new GameObject("PickaxeRack").transform;
            rack.SetParent(root, false);
            rack.position = new Vector3(rect.xMax - 0.55f, 0f, rect.yMin + 3.4f);
            rack.rotation = Quaternion.Euler(0f, -90f, 0f);
            for (int s = -1; s <= 1; s += 2)
                Part(rack, PrimitiveType.Cube, "Leg", new Vector3(s * 0.65f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.1f), Quaternion.identity, Wood);
            Part(rack, PrimitiveType.Cube, "Bar", new Vector3(0f, 1.1f, 0f), new Vector3(1.45f, 0.08f, 0.1f), Quaternion.identity, Wood);
            Part(rack, PrimitiveType.Cube, "Bar", new Vector3(0f, 0.35f, 0f), new Vector3(1.45f, 0.08f, 0.1f), Quaternion.identity, Wood);
            string[] tools = { "Pickaxe", "Shovel", "Pickaxe" };
            for (int i = 0; i < tools.Length; i++)
            {
                GameObject tool = Prop(decor, tools[i], rack, rack.TransformPoint(new Vector3(-0.42f + i * 0.42f, 0.15f, 0.12f)), 0f);
                if (tool != null) tool.transform.localRotation = Quaternion.Euler(0f, 0f, 85f + i * 4f);
            }
            Block(root, rack.position, new Vector2(0.5f, 1.5f));
            Prop(decor, "StonePile", root, new Vector3(rect.xMin + 1.0f, 0f, rect.yMin + 1.0f), 25f);
            Prop(decor, "StonePile", root, new Vector3(rect.xMax - 0.9f, 0f, rect.yMax - 0.9f), 140f, 0.85f);
            Prop(decor, "RockChunk", root, new Vector3(rect.xMin + 1.8f, 0f, rect.yMin + 0.6f), 70f, 1.1f);
            Prop(decor, "RockChunk", root, new Vector3(rect.xMax - 1.6f, 0f, rect.yMin + 1.2f), 200f, 1.2f);
            Prop(decor, "RockChunk", root, new Vector3(rect.xMax - 0.6f, 0f, rect.yMin + 5.6f), 10f, 0.9f);
            BuilderUtil.MarkStatic(root.gameObject);
        }

        // ================================================================ Meyve Bahçesi

        // Kemerli ahşap giriş kapısı (kapı aralığının üstünde): iki kalın direk, kavisli kemer, küçük tabela, sarmaşık.
        public static void BuildOrchardArch(Transform parent, Vector3 gateCenter, Vector3 along, Dictionary<string, GameObject> decor)
        {
            Transform arch = new GameObject("Arch").transform;
            arch.SetParent(parent, false);
            arch.position = gateCenter;
            arch.rotation = Quaternion.LookRotation(Vector3.Cross(along, Vector3.up));
            float half = MapLayout.GateWidth * 0.5f + 0.2f, h = 2.3f;
            for (int s = -1; s <= 1; s += 2)
                Part(arch, PrimitiveType.Cube, "Post", new Vector3(s * half, h * 0.5f, 0f), new Vector3(0.22f, h, 0.22f), Quaternion.identity, Wood);
            const int segments = 7;
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.PI * i / segments, a1 = Mathf.PI * (i + 1) / segments;
                Vector3 p0 = new Vector3(-Mathf.Cos(a0) * half, h + Mathf.Sin(a0) * 0.55f, 0f), p1 = new Vector3(-Mathf.Cos(a1) * half, h + Mathf.Sin(a1) * 0.55f, 0f);
                Vector3 mid = (p0 + p1) * 0.5f;
                float angle = Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg;
                Part(arch, PrimitiveType.Cube, "Arc", mid, new Vector3(Vector3.Distance(p0, p1) + 0.05f, 0.18f, 0.24f), Quaternion.Euler(0f, 0f, angle), Wood);
            }

            // Kemerin üstünde yapraklar ve birkaç meyve.
            for (int i = 0; i < 4; i++)
            {
                GameObject leaf = Prop(decor, "Vine", arch, arch.TransformPoint(new Vector3(-half + i * half * 0.66f, h + 0.25f, 0.1f)), i * 80f, 0.55f);
                if (leaf != null) leaf.transform.localRotation = Quaternion.Euler(0f, i * 80f, 180f);
            }
            for (int i = 0; i < 3; i++)
                PrefabFactory.CreateFruitModel(arch, 0.16f, i % 2 == 0).transform.localPosition = new Vector3(-0.8f + i * 0.8f, h + 0.42f + (i % 2) * 0.1f, -0.05f);
            for (int s = -1; s <= 1; s += 2) Block(arch, arch.TransformPoint(new Vector3(s * half, 0f, 0f)), new Vector2(0.35f, 0.35f));
        }

        public static void BuildOrchard(Transform parent, Rect rect, Dictionary<string, GameObject> decor)
        {
            Transform root = new GameObject("OrchardProps").transform;
            root.SetParent(parent, false);
            // Sulama kuyusu (kuzey uçta), kova.
            Vector3 well = new Vector3(rect.center.x, 0f, rect.yMax - 1.1f);
            Material stone = ArtGenerator.GetLit("M_WellStone", null, new Color(0.66f, 0.64f, 0.6f));
            Part(root, PrimitiveType.Cylinder, "WellRing", well + Vector3.up * 0.35f, new Vector3(1.2f, 0.35f, 1.2f), Quaternion.identity, stone);
            Part(root, PrimitiveType.Cylinder, "WellWater", well + Vector3.up * 0.68f, new Vector3(0.95f, 0.02f, 0.95f), Quaternion.identity,
                 ArtGenerator.GetLit("M_WellWater", null, new Color(0.3f, 0.6f, 0.75f)));
            for (int s = -1; s <= 1; s += 2)
                Part(root, PrimitiveType.Cube, "WellPost", well + new Vector3(s * 0.62f, 0.95f, 0f), new Vector3(0.1f, 1.2f, 0.1f), Quaternion.identity, Wood);
            Part(root, PrimitiveType.Cube, "WellRoofA", well + new Vector3(0f, 1.62f, -0.28f), new Vector3(1.5f, 0.06f, 0.7f), Quaternion.Euler(-28f, 0f, 0f), Plank);
            Part(root, PrimitiveType.Cube, "WellRoofB", well + new Vector3(0f, 1.62f, 0.28f), new Vector3(1.5f, 0.06f, 0.7f), Quaternion.Euler(28f, 0f, 0f), Plank);
            Part(root, PrimitiveType.Cylinder, "Winch", well + new Vector3(0f, 1.25f, 0f), new Vector3(0.12f, 0.6f, 0.12f), Quaternion.Euler(0f, 0f, 90f), Wood);
            Prop(decor, "Bucket", root, well + new Vector3(0.9f, 0f, -0.3f), 20f);
            Block(root, well, new Vector2(1.4f, 1.4f));
            // Meyve sepetleri ve kasaları (girişin iki yanında, çitin içinde).
            Vector3[] crates = { new Vector3(rect.xMin + 0.7f, 0f, rect.yMin + 0.8f), new Vector3(rect.xMax - 0.7f, 0f, rect.yMin + 0.8f),
                                 new Vector3(rect.xMax - 0.75f, 0f, rect.center.y), new Vector3(rect.xMin + 0.75f, 0f, rect.yMax - 0.9f) };
            for (int i = 0; i < crates.Length; i++)
            {
                GameObject crate = Prop(decor, "FruitCrate", root, crates[i], i * 37f);
                if (crate == null) continue;
                Bounds b = PrefabFactory.WorldBounds(crate);
                for (int k = 0; k < 4; k++)
                {
                    GameObject fruit = PrefabFactory.CreateFruitModel(crate.transform, 0.17f, (k + i) % 2 == 0);
                    fruit.transform.position = new Vector3(b.center.x + (k % 2 - 0.5f) * 0.22f, b.max.y - 0.04f, b.center.z + (k / 2 - 0.5f) * 0.22f);
                }
                Block(root, crates[i], new Vector2(0.7f, 0.7f));
            }
            BuilderUtil.MarkStatic(root.gameObject);
        }

        // ================================================================ Palmiye Koyu (keten tarlası)

        public static void BuildCove(Transform parent, Rect rect, Dictionary<string, GameObject> decor)
        {
            Transform root = new GameObject("CoveProps").transform;
            root.SetParent(parent, false);
            // Kurutma rafları: doğu kenarında iki A-çerçeveli raf, üstte sırık, asılı lif demetleri.
            for (int r = 0; r < 2; r++)
            {
                Transform rack = new GameObject("DryingRack").transform;
                rack.SetParent(root, false);
                rack.position = new Vector3(rect.xMax + 0.6f, 0f, rect.yMin + 1.6f + r * (rect.height - 3.2f));
                float len = 2.4f;
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 end = new Vector3(0f, 0f, s * len * 0.5f);
                    Part(rack, PrimitiveType.Cube, "LegA", end + new Vector3(-0.28f, 0.75f, 0f), new Vector3(0.09f, 1.6f, 0.09f), Quaternion.Euler(0f, 0f, -12f), Wood);
                    Part(rack, PrimitiveType.Cube, "LegB", end + new Vector3(0.28f, 0.75f, 0f), new Vector3(0.09f, 1.6f, 0.09f), Quaternion.Euler(0f, 0f, 12f), Wood);
                }
                Part(rack, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 1.48f, 0f), new Vector3(0.08f, len * 0.5f + 0.15f, 0.08f), Quaternion.Euler(90f, 0f, 0f), Wood);
                for (int i = 0; i < 6; i++)
                {
                    float z = -len * 0.5f + 0.35f + i * (len - 0.7f) / 5f;
                    Part(rack, PrimitiveType.Cylinder, "Bundle", new Vector3(0f, 1.05f, z), new Vector3(0.2f, 0.4f, 0.2f), Quaternion.identity, Fiber);
                    Part(rack, PrimitiveType.Cylinder, "Tie", new Vector3(0f, 1.38f, z), new Vector3(0.12f, 0.05f, 0.12f), Quaternion.identity, Twine);
                }
                Block(root, rack.position, new Vector2(0.9f, len + 0.2f));
            }
            // Bağlanmış lif demetleri (tarlanın köşelerinde): dik duran demet öbekleri.
            Vector3[] stacks = { new Vector3(rect.xMin + 0.5f, 0f, rect.yMin + 0.4f), new Vector3(rect.xMin + 0.5f, 0f, rect.yMax - 0.4f) };
            foreach (Vector3 c in stacks)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 1.25f;
                    Vector3 p = c + new Vector3(Mathf.Cos(a) * 0.22f, 0.35f, Mathf.Sin(a) * 0.22f);
                    Part(root, PrimitiveType.Cylinder, "Sheaf", p, new Vector3(0.16f, 0.36f, 0.16f), Quaternion.Euler(Mathf.Cos(a) * 12f, 0f, Mathf.Sin(a) * 12f), Fiber);
                }
                Part(root, PrimitiveType.Cylinder, "Tie", c + Vector3.up * 0.42f, new Vector3(0.5f, 0.05f, 0.5f), Quaternion.identity, Twine);
            }
            BuilderUtil.MarkStatic(root.gameObject);
        }

        // ================================================================ Tanıtım kartı

        public class InfoRow
        {
            public Sprite Icon, Icon2;
            public string Text;
        }

        // Tabelanın üstünde açılan küçük kart: ad, satırlar (ikon + yazı, istenirse "ikon → ikon"), sandıklar (?), bedel.
        public static InfoSign AddInfoCard(GameObject sign, AreaDefinition area, GameConfig config, string title, List<InfoRow> rows,
                                           int chests, Sprite priceIcon, string priceText, float cardHeight = 4.7f)
        {
            float rowH = 74f, width = 560f;
            int lines = rows.Count + (chests > 0 ? 1 : 0);
            float height = 120f + lines * rowH + 96f;
            Canvas canvas = UiFactory.CreateWorldCanvas("InfoCard", sign.transform, new Vector3(0f, cardHeight, 0f), Quaternion.identity, new Vector2(width, height));
            canvas.gameObject.AddComponent<Billboard>();
            canvas.sortingOrder = 100; // arkadaki dünya yazılarının (KÖPRÜ, Yükselt...) üstünde, opak
            Image bg = UiFactory.CreateImage("Card", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0.16f, 0.1f, 0.05f, 1f));
            bg.type = Image.Type.Sliced;
            UiFactory.Stretch(bg.rectTransform);
            Image border = UiFactory.CreateImage("Border", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0.95f, 0.75f, 0.35f, 0.9f));
            border.type = Image.Type.Sliced;
            UiFactory.Stretch(border.rectTransform);
            border.rectTransform.offsetMin = new Vector2(6f, 6f);
            border.rectTransform.offsetMax = new Vector2(-6f, -6f);
            border.fillCenter = false;
            float y = height * 0.5f - 62f;
            TMPro.TextMeshProUGUI t = UiFactory.CreateUIText("Title", canvas.transform, title, 54f, UiFactory.Gold);
            UiFactory.Size(t, new Vector2(0f, y), new Vector2(width - 40f, 70f));
            y -= 84f;
            foreach (InfoRow row in rows)
            {
                float x = -width * 0.5f + 60f;
                if (row.Icon != null)
                {
                    Image icon = UiFactory.CreateImage("Icon", canvas.transform, row.Icon, Color.white);
                    icon.preserveAspect = true;
                    UiFactory.Size(icon, new Vector2(x, y), new Vector2(60f, 60f));
                    x += 46f;
                }
                if (row.Icon2 != null)
                {
                    TMPro.TextMeshProUGUI arrow = UiFactory.CreateUIText("Arrow", canvas.transform, "→", 40f, UiFactory.Cream);
                    UiFactory.Size(arrow, new Vector2(x + 4f, y), new Vector2(40f, 50f));
                    Image icon2 = UiFactory.CreateImage("Icon2", canvas.transform, row.Icon2, Color.white);
                    icon2.preserveAspect = true;
                    UiFactory.Size(icon2, new Vector2(x + 48f, y), new Vector2(60f, 60f));
                    x += 90f;
                }
                TMPro.TextMeshProUGUI text = UiFactory.CreateUIText("Text", canvas.transform, row.Text, 36f, UiFactory.Cream);
                text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                RectTransform rt = text.rectTransform;
                rt.anchoredPosition = new Vector2(x + 26f + (width * 0.5f - 30f - (x + 26f)) * 0.5f, y);
                rt.sizeDelta = new Vector2(width * 0.5f - 30f - (x + 26f), 56f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                y -= rowH;
            }
            if (chests > 0)
            {
                Image chestIcon = UiFactory.CreateImage("ChestIcon", canvas.transform, IconGenerator.Create("chest"), Color.white);
                chestIcon.preserveAspect = true;
                UiFactory.Size(chestIcon, new Vector2(-width * 0.5f + 60f, y), new Vector2(60f, 60f));
                string q = "";
                for (int i = 0; i < chests; i++) q += i == 0 ? "?" : " ?";
                TMPro.TextMeshProUGUI text = UiFactory.CreateUIText("Chests", canvas.transform, "Gizli sandık  " + q, 36f, UiFactory.Cream);
                text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                UiFactory.Size(text, new Vector2(40f, y), new Vector2(width - 180f, 56f));
                y -= rowH;
            }
            // Bedel: ikon + yazı (ortada).
            Image pi = UiFactory.CreateImage("PriceIcon", canvas.transform, priceIcon, Color.white);
            pi.preserveAspect = true;
            UiFactory.Size(pi, new Vector2(-70f, -height * 0.5f + 56f), new Vector2(64f, 64f));
            TMPro.TextMeshProUGUI price = UiFactory.CreateUIText("Price", canvas.transform, priceText, 50f, UiFactory.Gold);
            price.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            UiFactory.Size(price, new Vector2(80f, -height * 0.5f + 56f), new Vector2(240f, 64f));

            InfoSign info = sign.AddComponent<InfoSign>();
            BuilderUtil.SetField(info, "config", config);
            BuilderUtil.SetField(info, "area", area);
            BuilderUtil.SetField(info, "card", canvas.transform);
            return info;
        }

        // ================================================================ Bina "Yükselt" tabelası

        public static BuildingUpgradeSign AddUpgradeSign(GameObject building, GameConfig config, Vector3 localPos)
        {
            ProcessingBuilding pb = building.GetComponentInChildren<ProcessingBuilding>(true); // Ambar'da yok (null)
            GameObject root = new GameObject("UpgradeSign");
            root.transform.SetParent(building.transform, false);
            root.transform.localPosition = localPos;
            Part(root.transform, PrimitiveType.Cube, "Post", new Vector3(0f, 0.55f, 0.05f), new Vector3(0.12f, 1.1f, 0.12f), Quaternion.identity, Wood);
            Part(root.transform, PrimitiveType.Cube, "Board", new Vector3(0f, 1.05f, 0f), new Vector3(0.95f, 0.42f, 0.07f), Quaternion.identity, SignWood);
            // Yukarı ok (yeşil üçgen) + yazı.
            Material green = ArtGenerator.GetLit("M_UpgradeArrow", null, new Color(0.45f, 0.85f, 0.35f));
            Part(root.transform, PrimitiveType.Cube, "ArrowStem", new Vector3(-0.32f, 1.0f, -0.05f), new Vector3(0.09f, 0.2f, 0.03f), Quaternion.identity, green);
            Part(root.transform, PrimitiveType.Cube, "ArrowHead", new Vector3(-0.32f, 1.13f, -0.05f), new Vector3(0.14f, 0.14f, 0.03f), Quaternion.Euler(0f, 0f, 45f), green);
            TMPro.TextMeshPro label = UiFactory.CreateWorldText("Label", root.transform, new Vector3(0.1f, 1.05f, -0.05f), "Yükselt", 2.4f, Color.white);
            ((RectTransform)label.transform).sizeDelta = new Vector2(0.7f, 0.35f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = 2.6f;
            PrefabFactory.AddGroundDecal("Ring", root.transform, new Vector3(0f, 0.02f, -0.75f), new Vector2(1.5f, 1.5f), new Color(0.6f, 1f, 0.5f, 0.55f), ArtGenerator.RingPath, 0f);
            BuilderUtil.SetShadows(root, false);
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.85f;
            trigger.center = new Vector3(0f, 0.6f, -0.75f);
            GameObject blockGo = new GameObject("Block");
            blockGo.transform.SetParent(root.transform, false);
            BuilderUtil.SetVector2(blockGo.AddComponent<NavObstacle>(), "size", new Vector2(0.3f, 0.3f));
            BuildingUpgradeSign sign = root.AddComponent<BuildingUpgradeSign>();
            BuilderUtil.SetField(sign, "config", config);
            BuilderUtil.SetField(sign, "building", pb);
            BuilderUtil.SetField(sign, "label", label);
            PrefabFactory.AddPanelHint(sign, new Vector3(0f, 0f, -0.75f), 1.5f, ArtGenerator.CoinIconPath, new Color(0.6f, 1f, 0.5f), 1.9f);
            return sign;
        }

        // ================================================================ Ambar (depo)

        // Kapalı, sağlam ahşap depo: taş temel, tahta duvarlar, büyük çift kanatlı kapı, koyu çatı, çatı katı kapağı.
        // Önünde makaralı/halatlı yük asansörü (bırakılınca sandık kalkıp içeri girer, alınınca iner). Çevresinde sandık
        // ve çuval yığınları; doluluk arttıkça dışarıdaki yığın büyür. Solda bırakma, sağda alma alanı.
        // Kök: burnun girişinde (alanların hizası); bina burnun ucuna doğru (yerel +Z) uzanır.
        public static Storage BuildStorage(Vector3 position, float yaw, AreaDefinition area, AreaDefinition[] levels, ResourceType[] types,
                                           GameConfig config, Dictionary<string, GameObject> decor, GameObject flagPrefab)
        {
            GameObject root = new GameObject("Storage");
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            GameObject locked = new GameObject("Locked");
            locked.transform.SetParent(root.transform, false);
            // Kapalıyken: boş zemin + birkaç kırık sandık (binayı ele vermez).
            for (int i = 0; i < 3; i++)
            {
                GameObject broken = Prop(decor, "Crate", locked.transform, root.transform.TransformPoint(new Vector3(-1.2f + i * 1.3f, 0f, 1.4f + (i % 2) * 0.8f)), yaw + i * 50f, 0.75f);
                if (broken != null) broken.transform.localRotation = Quaternion.Euler(i == 1 ? 18f : -12f, i * 50f, i == 0 ? 25f : 0f);
            }
            GameObject content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);

            Material stone = ArtGenerator.GetLit("M_QuarryStoneMid", null, new Color(0.66f, 0.63f, 0.57f));
            Material roof = ArtGenerator.GetLit("M_StorageRoofDark", null, new Color(0.3f, 0.2f, 0.14f));
            Material door = ArtGenerator.GetLit("M_DarkWood", null, new Color(0.38f, 0.24f, 0.13f));
            Transform house = new GameObject("House").transform;
            house.SetParent(content.transform, false);
            house.localPosition = new Vector3(0f, 0f, 1.9f);
            float w = 3.8f, d = 2.8f, h = 2.4f;
            Part(house, PrimitiveType.Cube, "Foundation", new Vector3(0f, 0.15f, 0f), new Vector3(w + 0.3f, 0.3f, d + 0.3f), Quaternion.identity, stone);
            Part(house, PrimitiveType.Cube, "Walls", new Vector3(0f, 0.3f + h * 0.5f, 0f), new Vector3(w, h, d), Quaternion.identity, Plank);
            for (int i = 0; i < 5; i++) // dikey tahta çizgileri (ön yüz)
                Part(house, PrimitiveType.Cube, "Batten", new Vector3(-w * 0.5f + 0.3f + i * (w - 0.6f) / 4f, 0.3f + h * 0.5f, -d * 0.5f - 0.03f), new Vector3(0.09f, h, 0.05f), Quaternion.identity, Wood);
            for (int sx = -1; sx <= 1; sx += 2)
                Part(house, PrimitiveType.Cube, "Corner", new Vector3(sx * w * 0.5f, 0.3f + h * 0.5f, -d * 0.5f), new Vector3(0.22f, h + 0.1f, 0.22f), Quaternion.identity, Wood);
            // Büyük çift kanatlı kapı (kapalı, demir menteşeli).
            Part(house, PrimitiveType.Cube, "DoorFrame", new Vector3(0f, 0.3f + 0.95f, -d * 0.5f - 0.06f), new Vector3(1.9f, 1.9f, 0.08f), Quaternion.identity, Wood);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                Part(house, PrimitiveType.Cube, "DoorLeaf", new Vector3(sx * 0.43f, 0.3f + 0.9f, -d * 0.5f - 0.1f), new Vector3(0.82f, 1.75f, 0.07f), Quaternion.identity, door);
                Part(house, PrimitiveType.Cube, "Brace", new Vector3(sx * 0.43f, 0.3f + 0.9f, -d * 0.5f - 0.14f), new Vector3(0.1f, 1.8f, 0.03f), Quaternion.Euler(0f, 0f, sx * 28f), Wood);
                Part(house, PrimitiveType.Cube, "Hinge", new Vector3(sx * 0.75f, 0.3f + 1.3f, -d * 0.5f - 0.15f), new Vector3(0.22f, 0.07f, 0.03f), Quaternion.identity, Iron);
            }
            // Çatı (iki eğimli) + çatı katı kapağı ve yük kirişi.
            for (int sz = -1; sz <= 1; sz += 2)
                Part(house, PrimitiveType.Cube, "Roof", new Vector3(0f, 0.3f + h + 0.55f, sz * 0.78f), new Vector3(w + 0.6f, 0.16f, d * 0.62f), Quaternion.Euler(sz * 32f, 0f, 0f), roof);
            Part(house, PrimitiveType.Cube, "Gable", new Vector3(0f, 0.3f + h + 0.35f, -d * 0.5f + 0.05f), new Vector3(w * 0.7f, 0.7f, 0.1f), Quaternion.identity, Plank);
            Part(house, PrimitiveType.Cube, "LoftDoor", new Vector3(0f, 0.3f + h + 0.3f, -d * 0.5f - 0.02f), new Vector3(0.8f, 0.55f, 0.06f), Quaternion.identity, door);
            Part(house, PrimitiveType.Cube, "HoistBeam", new Vector3(0f, 0.3f + h + 0.85f, -d * 0.5f - 0.75f), new Vector3(0.22f, 0.22f, 1.7f), Quaternion.identity, Wood);
            Transform pulley = new GameObject("Pulley").transform;
            pulley.SetParent(house, false);
            pulley.localPosition = new Vector3(0f, 0.3f + h + 0.62f, -d * 0.5f - 1.45f);
            Part(pulley, PrimitiveType.Cylinder, "Wheel", Vector3.zero, new Vector3(0.42f, 0.06f, 0.42f), Quaternion.Euler(0f, 0f, 90f), door);
            Part(pulley, PrimitiveType.Cube, "Spoke", Vector3.zero, new Vector3(0.08f, 0.4f, 0.06f), Quaternion.identity, Iron);
            if (flagPrefab != null)
            {
                GameObject flag = (GameObject)PrefabUtility.InstantiatePrefab(flagPrefab);
                flag.transform.SetParent(house, false);
                flag.transform.localPosition = new Vector3(w * 0.5f - 0.2f, 0.3f + h + 0.9f, 0.3f);
                flag.transform.localScale = Vector3.one * 0.55f;
            }
            PrefabFactory.CreateBoardSign(house, new Vector3(0f, 0.3f + h + 1.9f, -0.2f), "Ambar");
            BuilderUtil.SetShadows(house.gameObject, true);
            // Yük asansörü: halat makaradan sarkar, sandık bırakma/alma arasında, kapının önünde.
            Transform low = new GameObject("HoistLow").transform;
            low.SetParent(content.transform, false);
            low.localPosition = new Vector3(0f, 0.25f, 1.9f - d * 0.5f - 1.45f);
            Transform high = new GameObject("HoistHigh").transform;
            high.SetParent(content.transform, false);
            high.localPosition = low.localPosition + Vector3.up * (h + 0.05f);
            Transform crate = new GameObject("HoistCrate").transform;
            crate.SetParent(content.transform, false);
            crate.localPosition = low.localPosition;
            Prop(decor, "Crate", crate, crate.position, yaw, 0.75f);
            GameObject rope = Part(content.transform, PrimitiveType.Cylinder, "HoistRope", low.localPosition + Vector3.up * 1.2f, new Vector3(0.05f, 1.2f, 0.05f), Quaternion.identity, Rope);

            GameObject body = new GameObject("Block");
            body.transform.SetParent(content.transform, false);
            body.transform.localPosition = new Vector3(0f, 0f, 1.9f);
            NavObstacle obstacle = body.AddComponent<NavObstacle>();
            BuilderUtil.SetVector2(obstacle, "size", new Vector2(w + 0.4f, d + 0.4f));
            BuilderUtil.SetBool(obstacle, "oriented", true);
            BuilderUtil.SetFloat(obstacle, "cornerRadius", 0.45f);

            // Dışarıdaki yığınlar (binanın iki yanında): doluluk arttıkça görünür.
            string[] keys = { "Crate", "Barrel", "Crate", "CrateBottles", "Barrel", "Chest", "Crate", "Barrel" };
            List<Transform> fill = new List<Transform>();
            Material sack = ArtGenerator.GetLit("M_Sack", null, new Color(0.8f, 0.7f, 0.5f));
            for (int i = 0; i < keys.Length; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                Vector3 local = new Vector3(side * (w * 0.5f + 0.75f), 0f, 1.0f + (i / 2) * 0.75f);
                Transform slot = new GameObject("Pile " + i).transform;
                slot.SetParent(content.transform, false);
                slot.localPosition = local;
                GameObject item = Prop(decor, keys[i], slot, slot.position, yaw + i * 33f, 0.8f);
                if (i % 3 == 1) // çuvallar
                    Part(slot, PrimitiveType.Sphere, "Sack", new Vector3(0.3f, 0.25f, 0f), new Vector3(0.5f, 0.45f, 0.42f), Quaternion.identity, sack);
                fill.Add(slot);
            }
            GameObject sideBlock = new GameObject("PileBlock");
            sideBlock.transform.SetParent(content.transform, false);
            for (int sd = -1; sd <= 1; sd += 2)
            {
                GameObject pb = new GameObject("PileBlock");
                pb.transform.SetParent(content.transform, false);
                pb.transform.localPosition = new Vector3(sd * (w * 0.5f + 0.75f), 0f, 1.75f);
                NavObstacle po = pb.AddComponent<NavObstacle>();
                BuilderUtil.SetVector2(po, "size", new Vector2(1.0f, 2.4f));
                BuilderUtil.SetBool(po, "oriented", true);
            }
            Object.DestroyImmediate(sideBlock);

            StorageDropZone drop = CreateStorageZone<StorageDropZone>(content.transform, "DropZone", new Vector3(-1.45f, 0f, -0.75f), new Color(0.45f, 0.85f, 1f), true, "Bırak");
            StoragePickZone pick = CreateStorageZone<StoragePickZone>(content.transform, "PickZone", new Vector3(1.45f, 0f, -0.75f), new Color(1f, 0.82f, 0.25f), false, "Al");
            PrefabFactory.AddPanelHint(pick, Vector3.zero, 2.2f, ArtGenerator.StarPath, new Color(1f, 0.82f, 0.25f), 2.4f);
            Transform doorPoint = new GameObject("Door").transform;
            doorPoint.SetParent(content.transform, false);
            doorPoint.localPosition = new Vector3(0f, 1.0f, 0.5f);

            Storage storage = root.AddComponent<Storage>();
            BuilderUtil.SetField(storage, "config", config);
            BuilderUtil.SetField(storage, "area", area);
            BuilderUtil.SetArray(storage, "levelAreas", levels);
            BuilderUtil.SetArray(storage, "types", types);
            BuilderUtil.SetField(storage, "door", doorPoint);
            BuilderUtil.SetArray(storage, "fillVisuals", fill.ToArray());
            BuilderUtil.SetField(storage, "hoistCrate", crate);
            BuilderUtil.SetField(storage, "hoistLow", low);
            BuilderUtil.SetField(storage, "hoistHigh", high);
            BuilderUtil.SetField(storage, "hoistRope", rope.transform);
            BuilderUtil.SetField(storage, "hoistPulley", pulley);
            BuilderUtil.SetField(drop, "config", config);
            BuilderUtil.SetField(drop, "storage", storage);
            BuilderUtil.SetField(pick, "config", config);
            BuilderUtil.SetField(pick, "storage", storage);

            BuildingUpgradeSign sign = AddUpgradeSign(content, config, new Vector3(2.95f, 0f, -0.4f));
            BuilderUtil.SetField(sign, "storage", storage);
            InteractionVisibility vis = content.AddComponent<InteractionVisibility>();
            BuilderUtil.SetField(vis, "config", config);
            List<Object> labels = new List<Object>(), marks = new List<Object>();
            foreach (InteractionZone z in new InteractionZone[] { drop, pick })
            {
                foreach (TMPro.TMP_Text t in z.GetComponentsInChildren<TMPro.TMP_Text>(true)) labels.Add(t);
                foreach (Transform t in z.transform) if (t.name == "Ring" || t.name == "Arrow" || t.name == "Glow") marks.Add(t);
            }
            BuilderUtil.SetArray(vis, "texts", labels.ToArray());
            BuilderUtil.SetArray(vis, "scaled", marks.ToArray());

            content.SetActive(false);
            UnlockableArea unlockable = root.AddComponent<UnlockableArea>();
            BuilderUtil.SetField(unlockable, "config", config);
            BuilderUtil.SetField(unlockable, "area", area);
            BuilderUtil.SetField(unlockable, "content", content);
            BuilderUtil.SetField(unlockable, "lockedVisual", locked);
            return storage;
        }

        private static T CreateStorageZone<T>(Transform parent, string name, Vector3 localPos, Color color, bool inward, string text) where T : InteractionZone
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            PrefabFactory.AddGroundDecal("Glow", go.transform, Vector3.zero, new Vector2(2.1f, 2.1f), new Color(color.r, color.g, color.b, 0.4f), ArtGenerator.CirclePath, 0f);
            PrefabFactory.AddGroundDecal("Ring", go.transform, Vector3.up * 0.004f, new Vector2(2.2f, 2.2f), new Color(1f, 1f, 1f, 0.85f), ArtGenerator.RingPath, 0f);
            PrefabFactory.AddGroundDecal("Arrow", go.transform, Vector3.up * 0.008f, new Vector2(0.9f, 0.9f), new Color(1f, 1f, 1f, 0.9f), ArtGenerator.ArrowPath, inward ? 0f : 180f);
            TMPro.TextMeshPro label = UiFactory.CreateWorldLabel("Label", go.transform, new Vector3(0f, 1.6f, 0f), text, 4.2f, color);
            ((RectTransform)label.transform).sizeDelta = new Vector2(2f, 0.8f);
            SphereCollider trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.0f;
            trigger.center = new Vector3(0f, 0.6f, 0f);
            return go.AddComponent<T>();
        }

        // ================================================================ Kamp (adanın merkezi)

        // Kamp ateşi (taş halka, odunlar, titreşen alev, kıvılcım, duman, çıtırtı), çevresinde kütük tabureler ve dinlenme
        // yerleri; kuzeyde büyüyen barınak: enkazdan çadır → ahşap kulübe → küçük ev (bayrak, fener, sandıklar).
        public static CampSite BuildCamp(Vector3 fire, Vector3 shelter, GameConfig config, Dictionary<string, GameObject> decor, GameObject flagPrefab, GameObject tentPrefab)
        {
            Transform root = new GameObject("Camp").transform;
            root.position = fire;
            Material stone = ArtGenerator.GetLit("M_QuarryStoneMid", null, new Color(0.66f, 0.63f, 0.57f));
            Material flameA = ArtGenerator.GetLit("M_FlameOuter", null, new Color(1f, 0.55f, 0.15f));
            Material flameB = ArtGenerator.GetLit("M_FlameInner", null, new Color(1f, 0.85f, 0.35f));
            Material ember = ArtGenerator.GetLit("M_Ember", null, new Color(1f, 0.5f, 0.15f));
            Transform fireT = new GameObject("Fire").transform;
            fireT.SetParent(root, false);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                Part(fireT, PrimitiveType.Cube, "Stone", new Vector3(Mathf.Cos(a) * 0.7f, 0.12f, Mathf.Sin(a) * 0.7f), new Vector3(0.34f, 0.24f, 0.28f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 8f), stone);
            }
            for (int i = 0; i < 4; i++)
                Part(fireT, PrimitiveType.Cylinder, "Log", new Vector3(0f, 0.18f, 0f), new Vector3(0.14f, 0.45f, 0.14f), Quaternion.Euler(70f, i * 45f, 0f), Wood);
            Part(fireT, PrimitiveType.Cylinder, "Coals", new Vector3(0f, 0.08f, 0f), new Vector3(0.7f, 0.04f, 0.7f), Quaternion.identity, ember);
            Transform flame = new GameObject("Flame").transform;
            flame.SetParent(fireT, false);
            flame.localPosition = new Vector3(0f, 0.2f, 0f);
            Part(flame, PrimitiveType.Sphere, "Outer", new Vector3(0f, 0.38f, 0f), new Vector3(0.55f, 0.8f, 0.55f), Quaternion.identity, flameA);
            Part(flame, PrimitiveType.Sphere, "Inner", new Vector3(0f, 0.3f, 0f), new Vector3(0.32f, 0.55f, 0.32f), Quaternion.identity, flameB);
            BuilderUtil.SetShadows(fireT.gameObject, false);
            ParticleSystem sparks = Particles(fireT, "Sparks", new Vector3(0f, 0.7f, 0f), new Color(1f, 0.8f, 0.3f), new Color(1f, 0.45f, 0.1f), 0.8f, 1.8f, 0.07f, -0.2f, 1.1f, 20, 7f);
            ParticleSystem smoke = Particles(fireT, "Smoke", new Vector3(0f, 1.1f, 0f), new Color(0.6f, 0.6f, 0.62f, 0.35f), new Color(0.75f, 0.75f, 0.78f, 0.25f), 0.3f, 0.6f, 0.45f, -0.04f, 2.2f, 10, 3f);
            ParticleSystem.MainModule sm = sparks.main; sm.playOnAwake = true;
            ParticleSystem.MainModule mm = smoke.main; mm.playOnAwake = true;
            GameObject fireBlock = new GameObject("Block");
            fireBlock.transform.SetParent(root, false);
            BuilderUtil.SetVector2(fireBlock.AddComponent<NavObstacle>(), "size", new Vector2(1.5f, 1.5f));
            BuilderUtil.SetFloat(fireBlock.GetComponent<NavObstacle>(), "cornerRadius", 0.7f);
            AudioSource crackle = fireT.gameObject.AddComponent<AudioSource>();
            crackle.spatialBlend = 1f;
            crackle.rolloffMode = AudioRolloffMode.Linear;
            crackle.minDistance = 2f;
            crackle.maxDistance = 12f;
            crackle.volume = 0.35f;
            crackle.playOnAwake = false;

            // Kütük tabureler ve dinlenme yerleri (ateşin etrafında).
            List<Transform> seats = new List<Transform>();
            float[] angles = { 200f, 250f, 300f, 340f };
            foreach (float deg in angles)
            {
                float a = deg * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Part(root, PrimitiveType.Cylinder, "Stool", p * 1.55f + Vector3.up * 0.2f, new Vector3(0.45f, 0.2f, 0.45f), Quaternion.identity, Wood);
                Transform seat = new GameObject("Seat").transform;
                seat.SetParent(root, false);
                seat.localPosition = p * 2.05f;
                seats.Add(seat);
            }

            // Barınak seviyeleri (aynı yerde, biri görünür).
            Transform shelterRoot = new GameObject("Shelter").transform;
            shelterRoot.SetParent(root, false);
            shelterRoot.position = shelter;
            GameObject tent = new GameObject("Level0_Tent");
            tent.transform.SetParent(shelterRoot, false);
            Transform tentModel = new GameObject("TentModel").transform;
            tentModel.SetParent(tent.transform, false);
            tentModel.localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (tentPrefab != null) ((GameObject)PrefabUtility.InstantiatePrefab(tentPrefab)).transform.SetParent(tentModel, false);
            else PrefabFactory.CreateModel(ModelCatalog.Tent, tentModel);
            Part(tent.transform, PrimitiveType.Cube, "WreckPlank", new Vector3(1.35f, 0.08f, -0.3f), new Vector3(1.1f, 0.1f, 0.28f), Quaternion.Euler(0f, 25f, 8f), PlankDark);
            Part(tent.transform, PrimitiveType.Cube, "WreckPlank", new Vector3(-1.4f, 0.08f, -0.1f), new Vector3(0.9f, 0.1f, 0.25f), Quaternion.Euler(0f, -40f, -6f), Plank);
            Prop(decor, "Barrel", tent.transform, shelterRoot.TransformPoint(new Vector3(-1.5f, 0f, 0.6f)), 0f, 0.8f);

            GameObject hut = new GameObject("Level1_Hut");
            hut.transform.SetParent(shelterRoot, false);
            Part(hut.transform, PrimitiveType.Cube, "Floor", new Vector3(0f, 0.08f, 0.2f), new Vector3(2.6f, 0.16f, 2.0f), Quaternion.identity, PlankDark);
            Part(hut.transform, PrimitiveType.Cube, "Walls", new Vector3(0f, 0.95f, 0.4f), new Vector3(2.4f, 1.6f, 1.6f), Quaternion.identity, Plank);
            Part(hut.transform, PrimitiveType.Cube, "Door", new Vector3(0f, 0.75f, -0.42f), new Vector3(0.7f, 1.2f, 0.05f), Quaternion.identity, Wood);
            for (int sz = -1; sz <= 1; sz += 2)
                Part(hut.transform, PrimitiveType.Cube, "Roof", new Vector3(0f, 2.0f, 0.4f + sz * 0.5f), new Vector3(2.8f, 0.12f, 1.25f), Quaternion.Euler(sz * 30f, 0f, 0f), PlankDark);
            Prop(decor, "Barrel", hut.transform, shelterRoot.TransformPoint(new Vector3(1.6f, 0f, -0.2f)), 0f, 0.8f);

            GameObject house = new GameObject("Level2_House");
            house.transform.SetParent(shelterRoot, false);
            Material roof = ArtGenerator.GetLit("M_RoofTile", null, new Color(0.56f, 0.2f, 0.2f));
            Part(house.transform, PrimitiveType.Cube, "Foundation", new Vector3(0f, 0.15f, 0.3f), new Vector3(3.2f, 0.3f, 2.4f), Quaternion.identity, stone);
            Part(house.transform, PrimitiveType.Cube, "Walls", new Vector3(0f, 1.25f, 0.3f), new Vector3(3.0f, 1.9f, 2.2f), Quaternion.identity, Plank);
            Part(house.transform, PrimitiveType.Cube, "Door", new Vector3(0f, 0.95f, -0.83f), new Vector3(0.8f, 1.35f, 0.05f), Quaternion.identity, Wood);
            for (int sx = -1; sx <= 1; sx += 2)
                Part(house.transform, PrimitiveType.Cube, "Window", new Vector3(sx * 0.95f, 1.35f, -0.82f), new Vector3(0.45f, 0.45f, 0.04f), Quaternion.identity, Lantern);
            for (int sz = -1; sz <= 1; sz += 2)
                Part(house.transform, PrimitiveType.Cube, "Roof", new Vector3(0f, 2.55f, 0.3f + sz * 0.65f), new Vector3(3.5f, 0.14f, 1.6f), Quaternion.Euler(sz * 32f, 0f, 0f), roof);
            Part(house.transform, PrimitiveType.Cube, "LanternPost", new Vector3(1.75f, 1.0f, -0.9f), new Vector3(0.08f, 2.0f, 0.08f), Quaternion.identity, Wood);
            Part(house.transform, PrimitiveType.Cube, "Lantern", new Vector3(1.75f, 2.05f, -0.9f), new Vector3(0.2f, 0.28f, 0.2f), Quaternion.identity, Lantern);
            if (flagPrefab != null)
            {
                GameObject f = (GameObject)PrefabUtility.InstantiatePrefab(flagPrefab);
                f.transform.SetParent(house.transform, false);
                f.transform.localPosition = new Vector3(-1.2f, 2.6f, 0.6f);
                f.transform.localScale = Vector3.one * 0.55f;
            }
            Prop(decor, "Chest", house.transform, shelterRoot.TransformPoint(new Vector3(-1.9f, 0f, -0.3f)), 20f, 0.8f);
            Prop(decor, "Barrel", house.transform, shelterRoot.TransformPoint(new Vector3(1.9f, 0f, 0.6f)), 0f, 0.8f);
            hut.SetActive(false);
            house.SetActive(false);
            BuilderUtil.SetShadows(shelterRoot.gameObject, true);
            GameObject shelterBlock = new GameObject("Block");
            shelterBlock.transform.SetParent(shelterRoot, false);
            shelterBlock.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            BuilderUtil.SetVector2(shelterBlock.AddComponent<NavObstacle>(), "size", new Vector2(3.2f, 2.4f));
            BuilderUtil.SetFloat(shelterBlock.GetComponent<NavObstacle>(), "cornerRadius", 0.5f);

            CampSite camp = root.gameObject.AddComponent<CampSite>();
            BuilderUtil.SetField(camp, "config", config);
            BuilderUtil.SetArray(camp, "levels", new Object[] { tent, hut, house });
            BuilderUtil.SetArray(camp, "seats", seats.ToArray());
            BuilderUtil.SetField(camp, "fire", fireT);
            BuilderUtil.SetField(camp, "flame", flame);
            BuilderUtil.SetField(camp, "crackle", crackle);
            return camp;
        }

        private static ParticleSystem Particles(Transform parent, string name, Vector3 pos, Color a, Color b, float speedMin, float speedMax,
                                                float size, float gravity, float lifetime, int max, float rate)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.maxParticles = max;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = ArtGenerator.GetTransparent("M_Spark", ArtGenerator.LoadSprite(ArtGenerator.SoftCirclePath)?.texture, Color.white);
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        // Görev panosu: iki direk, tahta pano, üstünde kâğıtlar; önündeki alana girince panel açılır.
        public static QuestBoard BuildQuestBoard(Vector3 position, float yaw, GameConfig config)
        {
            GameObject root = new GameObject("QuestBoard");
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Transform board = new GameObject("Board").transform;
            board.SetParent(root.transform, false);
            board.localPosition = new Vector3(0f, 0f, 1.1f);
            Material paper = ArtGenerator.GetLit("M_Paper", null, new Color(0.96f, 0.92f, 0.78f));
            for (int s = -1; s <= 1; s += 2)
                Part(board, PrimitiveType.Cube, "Post", new Vector3(s * 0.75f, 0.9f, 0f), new Vector3(0.14f, 1.8f, 0.14f), Quaternion.identity, Wood);
            Part(board, PrimitiveType.Cube, "Panel", new Vector3(0f, 1.25f, 0f), new Vector3(1.6f, 0.95f, 0.08f), Quaternion.identity, SignWood);
            Part(board, PrimitiveType.Cube, "Top", new Vector3(0f, 1.78f, 0f), new Vector3(1.8f, 0.1f, 0.25f), Quaternion.identity, Wood);
            for (int i = 0; i < 4; i++)
                Part(board, PrimitiveType.Cube, "Note", new Vector3(-0.5f + i * 0.33f, 1.2f + (i % 2) * 0.18f, -0.05f), new Vector3(0.26f, 0.34f, 0.02f), Quaternion.Euler(0f, 0f, (i - 1.5f) * 6f), paper);
            Part(board, PrimitiveType.Cube, "Star", new Vector3(0f, 1.62f, -0.06f), new Vector3(0.16f, 0.16f, 0.02f), Quaternion.Euler(0f, 0f, 45f), ArtGenerator.GetLit("M_Gold", null, new Color(1f, 0.78f, 0.22f)));
            BuilderUtil.SetShadows(board.gameObject, true);
            PrefabFactory.CreateBoardSign(board, new Vector3(0f, 2.45f, 0f), "Görevler");
            GameObject blk = new GameObject("Block");
            blk.transform.SetParent(board, false);
            BuilderUtil.SetVector2(blk.AddComponent<NavObstacle>(), "size", new Vector2(1.8f, 0.4f));
            PrefabFactory.AddGroundDecal("Ring", root.transform, new Vector3(0f, 0.02f, 0f), new Vector2(1.8f, 1.8f), new Color(1f, 0.85f, 0.4f, 0.6f), ArtGenerator.RingPath, 0f);
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.0f;
            trigger.center = new Vector3(0f, 0.6f, 0f);
            QuestBoard qb = root.AddComponent<QuestBoard>();
            BuilderUtil.SetField(qb, "config", config);
            PrefabFactory.AddPanelHint(qb, Vector3.zero, 1.8f, ArtGenerator.StarPath, new Color(1f, 0.85f, 0.4f), 2.0f);
            return qb;
        }

        // ================================================================ Kapalı alan ipuçları ve açılınca yayılan zemin

        public enum HintKind { Quarry, Orchard, Cove, RopeCoil, YarnBall, ColdStones, Logs }

        // Neyin açılacağını ele vermeyen küçük doğal ipuçları (kapalıyken görünür, açılınca toz bulutuyla kaybolur).
        public static void AddHints(Transform locked, HintKind kind, Rect rect, Dictionary<string, GameObject> decor, int seed)
        {
            System.Random r = new System.Random(seed);
            Vector3 c = new Vector3(rect.center.x, 0f, rect.center.y);
            Material dirt = ArtGenerator.GetLit("M_Dirt", null, new Color(0.55f, 0.42f, 0.28f));
            switch (kind)
            {
                case HintKind.Quarry:
                    PrefabFactory.AddGroundDecal("DirtPatch", locked, locked.InverseTransformPoint(c), new Vector2(rect.width * 0.7f, rect.height * 0.6f), new Color(0.55f, 0.42f, 0.28f, 0.45f), ArtGenerator.SoftCirclePath, 0f);
                    for (int i = 0; i < 9; i++)
                        Prop(decor, i % 2 == 0 ? "RockTiny" : "Pebble_A", locked, c + new Vector3(R(r, rect.width * 0.35f), 0f, R(r, rect.height * 0.3f)), (float)r.NextDouble() * 360f, 0.8f + (float)r.NextDouble() * 0.5f);
                    // İnşaat işareti kazığı (çekiç-kürek simgeli küçük levha).
                    Vector3 stake = new Vector3(rect.center.x + 1.2f, 0f, rect.yMin + 1.2f);
                    Part(locked, PrimitiveType.Cube, "Stake", locked.InverseTransformPoint(stake) + Vector3.up * 0.45f, new Vector3(0.1f, 0.9f, 0.1f), Quaternion.identity, Wood);
                    Part(locked, PrimitiveType.Cube, "StakeBoard", locked.InverseTransformPoint(stake) + new Vector3(0f, 0.85f, -0.06f), new Vector3(0.55f, 0.4f, 0.05f), Quaternion.identity, SignWood);
                    Part(locked, PrimitiveType.Cube, "HammerIcon", locked.InverseTransformPoint(stake) + new Vector3(-0.08f, 0.85f, -0.1f), new Vector3(0.05f, 0.3f, 0.02f), Quaternion.Euler(0f, 0f, 35f), Iron);
                    Part(locked, PrimitiveType.Cube, "ShovelIcon", locked.InverseTransformPoint(stake) + new Vector3(0.08f, 0.85f, -0.1f), new Vector3(0.05f, 0.3f, 0.02f), Quaternion.Euler(0f, 0f, -35f), Wood);
                    break;
                case HintKind.Orchard:
                    PrefabFactory.AddGroundDecal("DirtPatch", locked, locked.InverseTransformPoint(c), new Vector2(rect.width * 0.6f, rect.height * 0.55f), new Color(0.55f, 0.42f, 0.28f, 0.4f), ArtGenerator.SoftCirclePath, 0f);
                    for (int i = 0; i < 4; i++) // işlenmemiş tarla sıraları
                        Part(locked, PrimitiveType.Cube, "Furrow", locked.InverseTransformPoint(c + new Vector3(-1.5f + i, 0.03f, 0f)), new Vector3(0.3f, 0.06f, rect.height * 0.45f), Quaternion.Euler(0f, (float)r.NextDouble() * 6f - 3f, 0f), dirt);
                    for (int i = 0; i < 7; i++)
                        Prop(decor, i % 3 == 0 ? "Grass" : "GrassTuft", locked, c + new Vector3(R(r, rect.width * 0.4f), 0f, R(r, rect.height * 0.4f)), (float)r.NextDouble() * 360f, 0.9f);
                    break;
                case HintKind.Cove:
                    Prop(decor, "DecorPalm", locked, c + new Vector3(-1.4f, 0f, 1.6f), 30f, 0.85f);
                    Prop(decor, "DecorPalm", locked, c + new Vector3(1.6f, 0f, -1.2f), 140f, 0.75f);
                    for (int i = 0; i < 3; i++) Prop(decor, "SmallRock", locked, c + new Vector3(R(r, 2f), 0f, R(r, 2.5f)), i * 80f, 1f);
                    // Kapalı geçiş: devrik kütük (karenin tarafında).
                    Transform log = new GameObject("FallenLog").transform;
                    log.SetParent(locked, false);
                    log.position = new Vector3(rect.xMin + 0.3f, 0f, rect.center.y);
                    Part(log, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 1.6f, 0.6f), Quaternion.Euler(90f, 0f, 0f), Wood);
                    Part(log, PrimitiveType.Cylinder, "Cut", new Vector3(0f, 0.3f, -1.6f), new Vector3(0.62f, 0.02f, 0.62f), Quaternion.Euler(90f, 0f, 0f), Plank);
                    BuilderUtil.SetVector2(log.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(0.7f, 3.2f));
                    break;
                case HintKind.RopeCoil:
                    Part(locked, PrimitiveType.Cylinder, "RopeCoil", new Vector3(-0.6f, 0.06f, 0.2f), new Vector3(0.45f, 0.06f, 0.45f), Quaternion.identity, Rope);
                    break;
                case HintKind.YarnBall:
                    Part(locked, PrimitiveType.Sphere, "Yarn", new Vector3(0.4f, 0.12f, 0.3f), new Vector3(0.25f, 0.25f, 0.25f), Quaternion.identity, ArtGenerator.GetLit("M_Yarn0", null, new Color(0.85f, 0.2f, 0.17f)));
                    break;
                case HintKind.ColdStones:
                    for (int i = 0; i < 4; i++) Part(locked, PrimitiveType.Cube, "ColdStone", new Vector3(-0.8f + i * 0.4f, 0.1f, 0.4f + (i % 2) * 0.3f), new Vector3(0.3f, 0.2f, 0.26f), Quaternion.Euler(0f, i * 40f, 0f), ArtGenerator.GetLit("M_ColdStone", null, new Color(0.35f, 0.34f, 0.36f)));
                    break;
                case HintKind.Logs:
                    for (int i = 0; i < 2; i++) Part(locked, PrimitiveType.Cylinder, "Log", new Vector3(-0.5f + i * 0.5f, 0.15f, 0.6f), new Vector3(0.3f, 0.6f, 0.3f), Quaternion.Euler(90f, 20f * i, 0f), Wood);
                    break;
            }
            BuilderUtil.SetShadows(locked.gameObject, false);
        }

        // Açılınca yayılarak belirecek zemin yamaları (içerikte "Ground": kum yol, tarla sıraları, toprak).
        public static void AddGround(Transform content, Rect rect, Color tint, bool rows)
        {
            Transform g = new GameObject("Ground").transform;
            g.SetParent(content, false);
            g.position = new Vector3(rect.center.x, 0f, rect.center.y);
            PrefabFactory.AddGroundDecal("Patch", g, Vector3.up * 0.01f, new Vector2(rect.width * 0.95f, rect.height * 0.95f), tint, ArtGenerator.SoftCirclePath, 0f);
            if (!rows) return;
            Material dirt = ArtGenerator.GetLit("M_TilledRow", null, new Color(0.5f, 0.36f, 0.22f));
            for (int i = 0; i < 3; i++)
                Part(g, PrimitiveType.Cube, "Row", new Vector3(-rect.width * 0.3f + i * rect.width * 0.3f, 0.02f, 0f), new Vector3(0.45f, 0.05f, rect.height * 0.75f), Quaternion.identity, dirt);
        }

        // Alçak süslü çit (Meyve Bahçesi): dikdörtgen boyunca, kapı tarafında geçiş açık (içerikte; açılınca belirir).
        public static void LowFence(Transform parent, Rect rect, Vector3 gate, GameObject fencePrefab)
        {
            Transform root = new GameObject("LowFence").transform;
            root.SetParent(parent, false);
            Vector3[] c =
            {
                new Vector3(rect.xMin, 0f, rect.yMin), new Vector3(rect.xMax, 0f, rect.yMin),
                new Vector3(rect.xMax, 0f, rect.yMax), new Vector3(rect.xMin, 0f, rect.yMax)
            };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = c[i], b = c[(i + 1) % 4];
                float len = Vector3.Distance(a, b);
                int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
                float yaw = Quaternion.LookRotation(b - a).eulerAngles.y - 90f;
                for (int k = 0; k < n; k++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (k + 0.5f) / n);
                    if (Vector3.Distance(p, gate) < MapLayout.GateWidth * 0.6f) continue; // kapı aralığı açık
                    GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab);
                    piece.transform.SetParent(root, false);
                    piece.transform.position = p;
                    piece.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    piece.transform.localScale = new Vector3(len / n / 2f, 0.8f, 1f);
                }
            }
        }

        // ================================================================ Denize uzanan iskele

        // Uzun ahşap iskele (kıyıdan denize): kalın direkler, tahta güverte, halat korkuluklar, fenerler, variller,
        // ucunda yürünebilir alan. Yürünebilirlik Pier bileşeniyle (IslandShape.OpenBridge).
        public static Transform BuildPier(Transform parent, Vector3 start, Vector3 dir, float length, float halfWidth, Dictionary<string, GameObject> decor)
        {
            Transform root = new GameObject("Pier").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(start, Quaternion.LookRotation(dir));
            float deckY = 0.1f;
            // Güverte tahtaları.
            int planks = Mathf.RoundToInt(length / 0.42f);
            for (int i = 0; i < planks; i++)
                Part(root, PrimitiveType.Cube, "Plank", new Vector3(0f, deckY, (i + 0.5f) * length / planks), new Vector3(halfWidth * 2f + 0.2f, 0.09f, length / planks * 0.88f),
                     Quaternion.Euler(0f, (i * 37 % 7 - 3) * 0.6f, 0f), i % 3 == 1 ? PlankDark : Plank);
            for (int s = -1; s <= 1; s += 2)
                Part(root, PrimitiveType.Cube, "Beam", new Vector3(s * (halfWidth - 0.1f), deckY - 0.12f, length * 0.5f), new Vector3(0.18f, 0.18f, length), Quaternion.identity, Wood);
            // Kalın direkler (suya iner) + halat korkuluk.
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2.2f) + 1);
            List<Vector3> tops = new List<Vector3>();
            for (int i = 0; i < posts; i++)
            {
                float z = length * i / (posts - 1);
                for (int s = -1; s <= 1; s += 2)
                {
                    Part(root, PrimitiveType.Cylinder, "Post", new Vector3(s * (halfWidth + 0.05f), deckY - 0.3f, z), new Vector3(0.26f, 1.15f, 0.26f), Quaternion.identity, Wood);
                    Part(root, PrimitiveType.Cylinder, "Knot", new Vector3(s * (halfWidth + 0.05f), deckY + 0.62f, z), new Vector3(0.3f, 0.05f, 0.3f), Quaternion.identity, Rope);
                    tops.Add(new Vector3(s * (halfWidth + 0.05f), deckY + 0.62f, z));
                }
            }
            for (int i = 0; i < posts - 1; i++)
            {
                for (int s = 0; s < 2; s++)
                {
                    Vector3 a = tops[i * 2 + s], b = tops[(i + 1) * 2 + s];
                    Vector3 mid = (a + b) * 0.5f + Vector3.down * 0.1f;
                    Part(root, PrimitiveType.Cylinder, "Rope", mid, new Vector3(0.06f, Vector3.Distance(a, b) * 0.5f, 0.06f), Quaternion.Euler(90f, 0f, 0f), Rope);
                }
            }
            // Fenerler (3 direğin üstünde).
            for (int i = 1; i < posts; i += 2)
            {
                int s = (i / 2) % 2 == 0 ? -1 : 1;
                Vector3 top = new Vector3(s * (halfWidth + 0.05f), deckY + 0.66f, length * i / (posts - 1));
                Part(root, PrimitiveType.Cube, "LanternPole", top + new Vector3(0f, 0.45f, 0f), new Vector3(0.07f, 0.9f, 0.07f), Quaternion.identity, Wood);
                Part(root, PrimitiveType.Cube, "LanternArm", top + new Vector3(-s * 0.15f, 0.88f, 0f), new Vector3(0.34f, 0.05f, 0.05f), Quaternion.identity, Wood);
                Part(root, PrimitiveType.Cube, "LanternCap", top + new Vector3(-s * 0.3f, 0.84f, 0f), new Vector3(0.2f, 0.05f, 0.2f), Quaternion.identity, Iron);
                Part(root, PrimitiveType.Cube, "Lantern", top + new Vector3(-s * 0.3f, 0.68f, 0f), new Vector3(0.16f, 0.24f, 0.16f), Quaternion.identity, Lantern);
            }
            // Variller ve kasa (kenarlarda, yürüme yolunu kapatmadan).
            Prop(decor, "Barrel", root, root.TransformPoint(new Vector3(-halfWidth + 0.35f, deckY, length * 0.35f)), 10f, 0.7f);
            Prop(decor, "Barrel", root, root.TransformPoint(new Vector3(-halfWidth + 0.35f, deckY, length * 0.35f + 0.65f)), 70f, 0.7f);
            Prop(decor, "Crate", root, root.TransformPoint(new Vector3(halfWidth - 0.4f, deckY, length * 0.62f)), 20f, 0.7f);
            Prop(decor, "BucketFish", root, root.TransformPoint(new Vector3(halfWidth - 0.35f, deckY, length - 0.6f)), 0f, 0.9f);
            BuilderUtil.SetShadows(root.gameObject, true);
            BuilderUtil.MarkStatic(root.gameObject);

            Transform a0 = new GameObject("EndA").transform;
            a0.SetParent(root, false);
            a0.localPosition = Vector3.zero;
            Transform b0 = new GameObject("EndB").transform;
            b0.SetParent(root, false);
            b0.localPosition = new Vector3(0f, 0f, length - 0.3f);
            Pier pier = root.gameObject.AddComponent<Pier>();
            BuilderUtil.SetField(pier, "endA", a0);
            BuilderUtil.SetField(pier, "endB", b0);
            BuilderUtil.SetFloat(pier, "halfWidth", halfWidth - 0.15f);
            BuilderUtil.SetFloat(pier, "deckHeight", deckY + 0.05f);
            return root;
        }

        // ================================================================ Gemi kızağı: denize uzanan iki paralel ray

        // İki kalın ahşap ray geminin altından denize doğru (kayma yönünde) uzanır; suya girdikçe alçalır, direklerle
        // desteklenir. Gemi raylar üstündeki beşikte durur (beşik gemiyle birlikte kayar).
        public const float SlipwayDrop = 1.4f; // raylar baştan uca bu kadar alçalır (metre)

        public static void BuildSlipway(Transform parent, Vector3 shipCenter, Vector3 bowDir, Vector3 slideDir, float shipLength,
                                        float landBehind, float seaLength)
        {
            Transform root = new GameObject("Slipway").transform;
            root.SetParent(parent, false);
            Vector3 ground = new Vector3(shipCenter.x, 0f, shipCenter.z);
            Quaternion rot = Quaternion.LookRotation(slideDir);
            float spread = shipLength * 0.26f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 from = ground + bowDir * (s * spread) - slideDir * landBehind;
                Vector3 to = from + slideDir * (landBehind + seaLength) + Vector3.down * SlipwayDrop;
                Vector3 mid = (from + to) * 0.5f + Vector3.up * 0.16f;
                Quaternion along = Quaternion.LookRotation(to - from);
                float len = Vector3.Distance(from, to);
                Part(root, PrimitiveType.Cube, "Rail", Vector3.zero, new Vector3(0.34f, 0.2f, len), along, Plank).transform.position = mid;
                Part(root, PrimitiveType.Cube, "RailCap", Vector3.zero, new Vector3(0.12f, 0.06f, len), along, Iron).transform.position = mid + Vector3.up * 0.12f;
                // Destek direkleri (suda) ve traversler.
                for (float d = 0.6f; d < len; d += 1.5f)
                {
                    Vector3 p = Vector3.Lerp(from, to, d / len) + Vector3.up * 0.16f;
                    GameObject post = Part(root, PrimitiveType.Cylinder, "Post", Vector3.zero, new Vector3(0.22f, 0.7f, 0.22f), Quaternion.identity, Wood);
                    post.transform.position = p + Vector3.down * 0.6f;
                }
            }
            for (float d = 0.4f; d < landBehind * 2f + 0.6f; d += 1.3f) // traversler sadece gemi altında (suda yok)
            {
                float k = d / (landBehind + seaLength);
                Vector3 c = ground - slideDir * landBehind + slideDir * d + Vector3.down * (SlipwayDrop * k) + Vector3.up * 0.02f;
                GameObject tie = Part(root, PrimitiveType.Cube, "Tie", Vector3.zero, new Vector3(spread * 2f + 0.8f, 0.1f, 0.24f), rot, PlankDark);
                tie.transform.position = c;
            }
            BuilderUtil.SetShadows(root.gameObject, false);
            BuilderUtil.MarkStatic(root.gameObject);
        }

        // Beşik: gemiyle birlikte kayan, raylara oturan iki kızak ve gövdeyi tutan V takozlar (gemi kökünün altında).
        public static void BuildCradle(Transform shipRoot, Vector3 shipCenterWorld, Vector3 bowDir, Vector3 slideDir, float shipLength, float shipWidth, float height)
        {
            Transform cradle = new GameObject("Cradle").transform;
            cradle.SetParent(shipRoot, true);
            Vector3 ground = new Vector3(shipCenterWorld.x, 0f, shipCenterWorld.z);
            Quaternion across = Quaternion.LookRotation(slideDir);
            float spread = shipLength * 0.26f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 c = ground + bowDir * (s * spread);
                GameObject runner = Part(cradle, PrimitiveType.Cube, "Runner", Vector3.zero, new Vector3(0.42f, 0.16f, shipWidth * 0.95f), across, Wood);
                runner.transform.position = c + Vector3.up * 0.34f;
                for (int k = -1; k <= 1; k += 2)
                {
                    GameObject chock = Part(cradle, PrimitiveType.Cube, "Chock", Vector3.zero, new Vector3(0.36f, height, 0.5f), across * Quaternion.Euler(k * 24f, 0f, 0f), Plank);
                    chock.transform.position = c + slideDir * (k * shipWidth * 0.3f) + Vector3.up * (0.34f + height * 0.5f);
                }
            }
            BuilderUtil.SetShadows(cradle.gameObject, true);
        }
    }
}
