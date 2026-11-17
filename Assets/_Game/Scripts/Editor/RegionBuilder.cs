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

        // ================================================================ Çalılık

        public static RegionThicket BuildThicket(string name, Rect rect, AreaDefinition area, GameConfig config, Vector3 entrance,
                                                 Dictionary<string, GameObject> decor, int seed)
        {
            GameObject root = new GameObject("Thicket " + name);
            Vector3 center = new Vector3(rect.center.x, 0f, rect.center.y);
            root.transform.position = center;
            System.Random r = new System.Random(seed);
            string[] bushKeys = { "ThicketBushA", "ThicketBushB", "ThicketBushC", "ThicketBushA" };
            List<Transform> bushes = new List<Transform>();
            const float step = 1.55f;
            int nx = Mathf.Max(2, Mathf.RoundToInt(rect.width / step)), nz = Mathf.Max(2, Mathf.RoundToInt(rect.height / step));
            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    bool edge = ix == 0 || iz == 0 || ix == nx - 1 || iz == nz - 1;
                    // İç kısımda arada boşluk: içerisi kısmen görünür.
                    if (!edge && r.NextDouble() < 0.28) continue;
                    float fx = (ix + 0.5f) / nx, fz = (iz + 0.5f) / nz;
                    Vector3 pos = new Vector3(rect.xMin + fx * rect.width + R(r, 0.35f), 0f, rect.yMin + fz * rect.height + R(r, 0.35f));
                    string key = bushKeys[r.Next(bushKeys.Length)];
                    float s = edge ? 1f + R(r, 0.12f) : 0.75f + R(r, 0.15f); // kenarlar daha yüksek ve sık
                    GameObject bush = Prop(decor, key, root.transform, pos, (float)r.NextDouble() * 360f, s);
                    if (bush == null) continue;
                    bush.name = "Bush";
                    bushes.Add(bush.transform);
                    // Sarmaşık: bazı çalıların üstüne yapraklı dal / yosun.
                    if (edge && r.NextDouble() < 0.45)
                    {
                        GameObject vine = Prop(decor, r.NextDouble() < 0.5 ? "Vine" : "VineMoss", root.transform,
                                               pos + new Vector3(R(r, 0.3f), 0f, R(r, 0.3f)), (float)r.NextDouble() * 360f, 0.9f + R(r, 0.15f));
                        if (vine != null) { vine.name = "Vine"; bushes.Add(vine.transform); }
                    }
                }
            }
            BuilderUtil.SetShadows(root, false);
            foreach (Transform b in bushes) BuilderUtil.SetShadows(b.gameObject, b.name == "Bush" && b.localScale.x > 0.95f);

            // Engel: bölgenin tamamı (kapalıyken kimse giremez).
            GameObject blocker = new GameObject("Blocker");
            blocker.transform.SetParent(root.transform, false);
            NavObstacle obstacle = blocker.AddComponent<NavObstacle>();
            BuilderUtil.SetVector2(obstacle, "size", rect.size + Vector2.one * 0.3f);
            BuilderUtil.SetFloat(obstacle, "cornerRadius", 0.6f);

            // Kesilince saçılan yapraklar.
            GameObject leavesGo = (GameObject)PrefabUtility.InstantiatePrefab(BuilderUtil.Load<GameObject>("Assets/_Game/Prefabs/FX/LeafBurst.prefab"));
            leavesGo.name = "Leaves";
            leavesGo.transform.SetParent(root.transform, false);
            ParticleSystem leaves = leavesGo.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = leaves.main;
            main.maxParticles = 160;
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.ShapeModule shape = leaves.shape;
            shape.radius = 0.5f;

            Transform entranceT = new GameObject("Entrance").transform;
            entranceT.SetParent(root.transform, false);
            entranceT.position = entrance;

            RegionThicket thicket = root.AddComponent<RegionThicket>();
            BuilderUtil.SetField(thicket, "config", config);
            BuilderUtil.SetField(thicket, "area", area);
            BuilderUtil.SetArray(thicket, "bushes", bushes.ToArray());
            BuilderUtil.SetArray(thicket, "blockers", new Object[] { obstacle });
            BuilderUtil.SetField(thicket, "leaves", leaves);
            BuilderUtil.SetField(thicket, "entrance", entranceT);
            return thicket;
        }

        private static float R(System.Random r, float amount) => ((float)r.NextDouble() * 2f - 1f) * amount;

        // ================================================================ Kayalık = taş ocağı

        public static void BuildQuarry(Transform parent, Rect rect, Dictionary<string, GameObject> decor)
        {
            Transform root = new GameObject("Quarry").transform;
            root.SetParent(parent, false);
            // Arka planda kaya yüzü / uçurum: bölgenin kuzey kenarı boyunca, birbirine yaslanan kayalıklar.
            string[] cliffs = { "QuarryCliffA", "QuarryCliffB", "QuarryCliffC", "QuarryCliffA", "QuarryCliffB" };
            for (int i = 0; i < cliffs.Length; i++)
            {
                float x = rect.xMin - 0.6f + (rect.width + 1.2f) * i / (cliffs.Length - 1f);
                GameObject c = Prop(decor, cliffs[i], root, new Vector3(x, 0f, rect.yMax + 1.1f + (i % 2) * 0.5f), 180f + i * 23f, 1f + (i % 3) * 0.12f);
                if (c != null) Block(root, c.transform.position, new Vector2(2.6f, 1.6f));
            }
            // Ahşap iskele kaya yüzüne dayalı: direkler + iki kat tahta + merdiven.
            Transform scaffold = new GameObject("Scaffold").transform;
            scaffold.SetParent(root, false);
            Vector3 s0 = new Vector3(rect.xMin + 1.4f, 0f, rect.yMax - 0.2f);
            for (int i = 0; i < 3; i++)
                for (int k = 0; k < 2; k++)
                    Part(scaffold, PrimitiveType.Cube, "Post", s0 + new Vector3(i * 1.2f, 1.25f, k * 0.9f), new Vector3(0.14f, 2.5f, 0.14f), Quaternion.identity, Wood);
            for (int level = 1; level <= 2; level++)
            {
                Part(scaffold, PrimitiveType.Cube, "Deck", s0 + new Vector3(1.2f, level * 1.1f, 0.45f), new Vector3(2.7f, 0.09f, 1.15f), Quaternion.identity, Plank);
                Part(scaffold, PrimitiveType.Cube, "Rail", s0 + new Vector3(1.2f, level * 1.1f + 0.55f, -0.05f), new Vector3(2.6f, 0.07f, 0.07f), Quaternion.identity, Wood);
            }
            for (int i = 0; i < 5; i++)
                Part(scaffold, PrimitiveType.Cube, "Rung", s0 + new Vector3(-0.35f, 0.25f + i * 0.42f, -0.1f), new Vector3(0.5f, 0.06f, 0.06f), Quaternion.identity, Wood);
            Block(root, s0 + new Vector3(1.2f, 0f, 0.45f), new Vector2(2.8f, 1.3f));

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
            Part(arch, PrimitiveType.Cube, "Board", new Vector3(0f, h + 0.25f, -0.15f), new Vector3(1.5f, 0.42f, 0.06f), Quaternion.identity, SignWood);
            TMPro.TextMeshPro text = UiFactory.CreateWorldText("Title", arch, new Vector3(0f, h + 0.25f, -0.19f), "Bahçe", 2.6f, Color.white);
            ((RectTransform)text.transform).sizeDelta = new Vector2(1.4f, 0.4f);
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
                                           int chests, Sprite priceIcon, string priceText)
        {
            float rowH = 74f, width = 560f;
            int lines = rows.Count + (chests > 0 ? 1 : 0);
            float height = 120f + lines * rowH + 96f;
            Canvas canvas = UiFactory.CreateWorldCanvas("InfoCard", sign.transform, new Vector3(0f, 3.4f, 0f), Quaternion.identity, new Vector2(width, height));
            canvas.gameObject.AddComponent<Billboard>();
            canvas.sortingOrder = 5;
            Image bg = UiFactory.CreateImage("Card", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), new Color(0.16f, 0.1f, 0.05f, 0.92f));
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
            ProcessingBuilding pb = building.GetComponentInChildren<ProcessingBuilding>(true);
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
            return sign;
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
                Vector3 to = from + slideDir * (landBehind + seaLength) + Vector3.down * 1.4f;
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
            for (float d = 0.4f; d < landBehind + seaLength; d += 1.3f)
            {
                float k = d / (landBehind + seaLength);
                Vector3 c = ground - slideDir * landBehind + slideDir * d + Vector3.down * (1.4f * k) + Vector3.up * 0.02f;
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
