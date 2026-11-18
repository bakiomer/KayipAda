// DiscoveryBuilder.cs
// Keşif içeriğini kurar (sahne kurma komutunun parçası):
// - Adacıklar: Batık Koyu ve Kayıp Tapınak'ın zemin modeli ve kendi dokusu (ayırt edici renkler), NavMesh'e eklenir.
// - Köprüler: köprü başında teslim alanı, tahtalar/halatlar, yanlarda direkler (BridgeBuilder).
// - Sandıklar, koleksiyon eşyaları, altın figür parçaları; tapınak harabesi ve batık gemi; kırık yapılar (onarım).

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace KayipAda.EditorTools
{
    public static class DiscoveryBuilder
    {
        private static Material Wood => ArtGenerator.GetLit("M_ShipWood", null, MapLayout.ShipWoodColor);
        private static Material Plank => ArtGenerator.GetLit("M_ShipPlank", null, MapLayout.ShipPlankColor);
        private static Material PlankDark => ArtGenerator.GetLit("M_BridgePlankDark", null, new Color(0.7f, 0.5f, 0.3f));
        private static Material RopeMat => ArtGenerator.GetLit("M_MachineRope", null, new Color(0.78f, 0.6f, 0.36f));
        public static Material Gold => ArtGenerator.GetLit("M_Gold", null, new Color(1f, 0.78f, 0.22f));
        private static Material Brass => ArtGenerator.GetLit("M_Brass", null, new Color(0.85f, 0.66f, 0.3f));
        private static Material Leather => ArtGenerator.GetLit("M_Leather", null, new Color(0.45f, 0.27f, 0.15f));
        private static Material Paper => ArtGenerator.GetLit("M_Paper", null, new Color(0.96f, 0.92f, 0.78f));
        private static Material Red => ArtGenerator.GetLit("M_RedPaint", null, new Color(0.85f, 0.2f, 0.18f));

        // ================================================================ Adacıklar

        public static List<MeshFilter> BuildIslets(IslandShape island)
        {
            List<MeshFilter> grounds = new List<MeshFilter>();
            for (int i = 0; i < island.IsletCount; i++)
            {
                GameObject go = new GameObject("Islet " + MapLayout.IsletNames[i]);
                Rect rect = IsletRect(island, i);
                go.AddComponent<MeshFilter>().sharedMesh = BuildIsletMesh(island, i, rect);
                MeshRenderer r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = ArtGenerator.GetLit("M_Islet" + i, BuildIsletTexture(island, i, rect), Color.white);
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = true;
                BuilderUtil.MarkStatic(go);
                grounds.Add(go.GetComponent<MeshFilter>());
            }
            return grounds;
        }

        private static Rect IsletRect(IslandShape island, int i)
        {
            Vector4 v = island.Islet(i);
            Vector2 half = new Vector2(v.z, v.w) * 1.15f + Vector2.one * 4f;
            return new Rect(new Vector2(v.x, v.y) - half, half * 2f);
        }

        // Ana adayla aynı yöntem: merkezden kıyıya halkalar + denize inen etek.
        private static Mesh BuildIsletMesh(IslandShape island, int lobe, Rect uvRect)
        {
            const int segments = 96;
            float[] topRings = { 0.3f, 0.6f, 0.82f, 0.94f, 1f };
            float[] skirtOut = { 0.7f, 1.8f, 3.2f };
            float[] skirtY = { -0.28f, -0.75f, -1.5f };
            Vector2 center = island.LobeCenter(lobe);
            int ringCount = topRings.Length + skirtOut.Length;
            Vector3[] vertices = new Vector3[1 + ringCount * segments];
            vertices[0] = new Vector3(center.x, 0f, center.y);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 coast = island.CoastPoint(lobe, angle);
                Vector2 outward = (coast - center).normalized;
                for (int r = 0; r < ringCount; r++)
                {
                    Vector3 v;
                    if (r < topRings.Length)
                    {
                        Vector2 p = Vector2.Lerp(center, coast, topRings[r]);
                        v = new Vector3(p.x, 0f, p.y);
                    }
                    else
                    {
                        int k = r - topRings.Length;
                        Vector2 p = coast + outward * skirtOut[k];
                        v = new Vector3(p.x, skirtY[k], p.y);
                    }
                    vertices[1 + r * segments + i] = v;
                }
            }
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                uvs[i] = new Vector2((vertices[i].x - uvRect.xMin) / uvRect.width, (vertices[i].z - uvRect.yMin) / uvRect.height);
            List<int> tris = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris.Add(0); tris.Add(1 + next); tris.Add(1 + i);
                for (int r = 0; r < ringCount - 1; r++)
                {
                    int a = 1 + r * segments + i, b = 1 + r * segments + next, c = 1 + (r + 1) * segments + i, d = 1 + (r + 1) * segments + next;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }
            Mesh mesh = new Mesh { name = "IsletMesh" + lobe, vertices = vertices, uv = uvs, triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return BuilderUtil.SaveAsset(mesh, ArtGenerator.GeneratedFolder + "/IsletMesh" + lobe + ".asset");
        }

        // Adacığın kendi dokusu: Batık Koyu koyu kum + yosunlu kaya lekeleri, Tapınak sık yeşil + taş döşeme halkası.
        private static Texture2D BuildIsletTexture(IslandShape island, int lobe, Rect rect)
        {
            const int size = 1024;
            string path = ArtGenerator.GeneratedFolder + "/IsletGround" + lobe + ".png";
            Color sand = MapLayout.IsletSand[lobe], wetSand = sand * 0.82f;
            Color grass = MapLayout.IsletGrass[lobe];
            Color rock = new Color(0.5f, 0.48f, 0.44f), moss = new Color(0.36f, 0.48f, 0.3f), stone = new Color(0.72f, 0.7f, 0.62f);
            Vector2 templeC = new Vector2(MapLayout.Temple.x, MapLayout.Temple.z);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(rect.xMin + (x + 0.5f) / size * rect.width, rect.yMin + (y + 0.5f) / size * rect.height);
                    float sd = island.SignedDistance(p);
                    float n1 = ArtGenerator.Fbm(p * 0.3f + new Vector2(lobe * 13f, 5f));
                    float n2 = ArtGenerator.Fbm(p * 1.4f + new Vector2(7f, lobe * 3f));
                    Color sandColor = Color.Lerp(wetSand, sand, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, -1.2f, sd)));
                    float edge = -(MapLayout.IsletSandWidth + (n1 - 0.5f) * 2f);
                    float g = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge + 0.5f, edge - 0.5f, sd));
                    Color grassColor = grass * (0.88f + 0.24f * n2);
                    if (lobe == 0)
                    {
                        // Batık Koyu: kumun üstünde koyu kaya lekeleri.
                        float patch = ArtGenerator.Fbm(p * 0.22f + new Vector2(3f, 31f));
                        sandColor = Color.Lerp(sandColor, rock * (0.9f + 0.2f * n2), Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.72f, patch)) * 0.7f);
                    }
                    else
                    {
                        // Tapınak: yosun lekeleri + harabe çevresinde taş döşeme halkası.
                        grassColor = Color.Lerp(grassColor, moss, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.7f, n1)) * 0.6f);
                        float ring = Mathf.Abs((p - templeC).magnitude - MapLayout.TempleRadius);
                        float tile = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.5f, 0.9f, ring));
                        float center = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.9f, 1.3f, (p - templeC).magnitude));
                        grassColor = Color.Lerp(grassColor, stone * (0.9f + 0.15f * n2), Mathf.Max(tile, center) * 0.85f);
                    }
                    Color c = Color.Lerp(sandColor, grassColor, g);
                    if (sd > 0f) c = Color.Lerp(c, wetSand * 0.8f, Mathf.Clamp01(sd / 2.5f));
                    c *= 0.95f + 0.08f * ArtGenerator.Fbm(p * 4f);
                    c.a = 1f;
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            BuilderUtil.WritePng(tex, path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return BuilderUtil.Load<Texture2D>(path);
        }

        // ================================================================ Köprüler

        public class BridgeSpec
        {
            public string Id, Name;
            public int Islet;
            public AreaDefinition IslandArea, RequiredArea;
            public ResourceType Plank, Rope;
            public int Planks, Ropes;
            public Transform RevealRoot; // karşı kıyının içeriği (köprü bitince zıplayarak belirir)
        }

        public static BridgeBuilder BuildBridge(BridgeSpec spec, IslandShape island, GameConfig config, GameObject signPrefab, List<Vector3> reserved)
        {
            Vector3 mainPoint = MapLayout.BridgeMainEnd[spec.Islet];
            Vector2 isletC = island.LobeCenter(spec.Islet);
            Vector3 dir = new Vector3(isletC.x - mainPoint.x, 0f, isletC.y - mainPoint.z).normalized;
            // Kıyı geçişleri: ana adadan çıkış ve adacığa giriş.
            Vector3 p = mainPoint;
            int guard = 0;
            while (island.SignedDistance(new Vector2(p.x, p.z)) < 0f && guard++ < 400) p += dir * 0.05f;
            Vector3 coastA = p;
            while (island.SignedDistance(new Vector2(p.x, p.z)) >= 0f && guard++ < 1200) p += dir * 0.05f;
            Vector3 coastB = p;
            Vector3 a = coastA - dir * MapLayout.BridgeShoreInset, b = coastB + dir * MapLayout.BridgeShoreInset;
            Quaternion along = Quaternion.LookRotation(dir);
            float length = Vector3.Distance(a, b);

            GameObject root = new GameObject("Bridge " + spec.Name);
            Vector3 head = a - dir * MapLayout.BridgeHeadInland;
            root.transform.SetPositionAndRotation(head, along);
            CapsuleCollider trigger = root.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;
            trigger.height = 3f;
            trigger.center = new Vector3(0f, 1.5f, 0f);

            // Teslim alanı görünüşü: yerde mavi halka, köprüyü gösteren oklar, üstte "Köprü: 0/40".
            GameObject zone = new GameObject("Zone");
            zone.transform.SetParent(root.transform, false);
            Color blue = new Color(0.45f, 0.85f, 1f);
            PrefabFactory.AddGroundDecal("Glow", zone.transform, Vector3.zero, new Vector2(3f, 3f), new Color(blue.r, blue.g, blue.b, 0.35f), ArtGenerator.CirclePath, 0f);
            PrefabFactory.AddGroundDecal("Ring", zone.transform, Vector3.up * 0.004f, new Vector2(3.1f, 3.1f), new Color(1f, 1f, 1f, 0.85f), ArtGenerator.RingPath, 0f);
            for (int i = 0; i < 2; i++)
                PrefabFactory.AddGroundDecal("Arrow " + i, zone.transform, Vector3.forward * (1.9f + i * 0.6f), new Vector2(0.6f, 0.6f), new Color(1f, 1f, 1f, 0.7f), ArtGenerator.ArrowPath, 0f);
            ShipProgressView view = PrototypeSceneBuilder.CreateShipProgressView(zone.transform, 2.7f, 0.7f);

            // Köprü: iki kıyı arasında yan kirişler ve direkler (hep görünür = inşaat), tahtalar ve halatlar sırayla belirir.
            Transform deck = new GameObject("Deck").transform;
            deck.SetParent(root.transform, false);
            deck.SetPositionAndRotation(a, along);
            float w = MapLayout.BridgeHalfWidth;
            for (int s = -1; s <= 1; s += 2)
                MachineBuilder.Part(deck, PrimitiveType.Cube, "Stringer", new Vector3(s * (w - 0.1f), MapLayout.BridgeDeckY - 0.12f, length * 0.5f),
                                    new Vector3(0.16f, 0.16f, length), Quaternion.identity, Wood);
            int postCount = Mathf.Max(2, Mathf.RoundToInt(length / 2.2f) + 1);
            List<Vector3> postTops = new List<Vector3>();
            for (int i = 0; i < postCount; i++)
            {
                float z = length * i / (postCount - 1);
                bool end = i == 0 || i == postCount - 1;
                float thick = end ? 0.34f : 0.24f;
                for (int s = -1; s <= 1; s += 2)
                {
                    MachineBuilder.Part(deck, PrimitiveType.Cylinder, "Post", new Vector3(s * (w + 0.06f), -0.25f, z), new Vector3(thick, 1.1f, thick), Quaternion.identity, Wood);
                    MachineBuilder.Part(deck, PrimitiveType.Cylinder, "Lash", new Vector3(s * (w + 0.06f), 0.72f, z), new Vector3(thick + 0.05f, 0.05f, thick + 0.05f), Quaternion.identity, RopeMat);
                    if (end) // kıyıya çakılı ek kazık
                        MachineBuilder.Part(deck, PrimitiveType.Cylinder, "Stake", new Vector3(s * (w + 0.4f), 0.15f, z + (i == 0 ? -0.35f : 0.35f)), new Vector3(0.2f, 0.4f, 0.2f), Quaternion.identity, Wood);
                    postTops.Add(new Vector3(s * (w + 0.06f), 0.75f, z));
                }
            }
            List<Transform> planks = new List<Transform>();
            int plankCount = Mathf.Max(4, Mathf.FloorToInt(length / MapLayout.BridgePlankSpacing));
            float spacing = length / plankCount;
            System.Random random = new System.Random(spec.Islet * 31 + 7);
            for (int i = 0; i < plankCount; i++)
            {
                float jitter = ((float)random.NextDouble() - 0.5f) * 3f;
                GameObject plank = MachineBuilder.Part(deck, PrimitiveType.Cube, "Plank " + i, new Vector3(0f, MapLayout.BridgeDeckY, spacing * (i + 0.5f)),
                                                       new Vector3(w * 2f + 0.15f, 0.08f, spacing * 0.86f), Quaternion.Euler(0f, jitter, 0f), i % 3 == 1 ? PlankDark : Plank);
                planks.Add(plank.transform);
            }
            List<Transform> ropes = new List<Transform>();
            {
                for (int i = 0; i < postCount - 1; i++)
                {
                    for (int s = 0; s < 2; s++)
                    {
                        Vector3 from = postTops[i * 2 + s], to = postTops[(i + 1) * 2 + s];
                        Vector3 mid = (from + to) * 0.5f + Vector3.down * 0.12f;
                        GameObject rope = MachineBuilder.Part(deck, PrimitiveType.Cylinder, "Rope", mid, new Vector3(0.06f, Vector3.Distance(from, to) * 0.5f, 0.06f),
                                                              Quaternion.Euler(90f, 0f, 0f), RopeMat);
                        if (spec.Ropes > 0) ropes.Add(rope.transform); // halat istenmiyorsa korkuluk baştan hazır
                    }
                }
            }
            BuilderUtil.SetShadows(deck.gameObject, true);

            Transform endA = new GameObject("EndA").transform;
            endA.SetParent(root.transform, false);
            endA.position = a;
            Transform endB = new GameObject("EndB").transform;
            endB.SetParent(root.transform, false);
            endB.position = b;

            BridgeBuilder bridge = root.AddComponent<BridgeBuilder>();
            BuilderUtil.SetField(bridge, "config", config);
            BuilderUtil.SetString(bridge, "id", spec.Id);
            BuilderUtil.SetString(bridge, "displayName", "Köprü");
            SetRequirements(bridge, spec.Plank, spec.Planks, spec.Rope, spec.Ropes);
            BuilderUtil.SetField(bridge, "requiredArea", spec.RequiredArea);
            BuilderUtil.SetField(bridge, "zoneVisual", zone);
            BuilderUtil.SetField(bridge, "progressView", view);
            BuilderUtil.SetField(bridge, "islandArea", spec.IslandArea);
            Transform far = new GameObject("FarShore").transform;
            far.SetParent(root.transform, false);
            far.position = new Vector3(isletC.x, 0f, isletC.y);
            BuilderUtil.SetField(bridge, "farShore", far);
            BuilderUtil.SetField(bridge, "revealRoot", spec.RevealRoot);
            BuilderUtil.SetArray(bridge, "planks", planks.ToArray());
            BuilderUtil.SetArray(bridge, "ropes", ropes.ToArray());
            BuilderUtil.SetField(bridge, "endA", endA);
            BuilderUtil.SetField(bridge, "endB", endB);
            BuilderUtil.SetFloat(bridge, "halfWidth", w);
            reserved.Add(new Vector3(head.x, head.z, 2.6f));
            reserved.Add(new Vector3(a.x, a.z, 2f));
            return bridge;
        }

        public static void SetRequirements(ConstructionSite site, ResourceType first, int firstAmount, ResourceType second, int secondAmount)
        {
            SerializedObject so = new SerializedObject(site);
            SerializedProperty list = so.FindProperty("requirements");
            int count = second != null && secondAmount > 0 ? 2 : 1;
            list.arraySize = count;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("Resource").objectReferenceValue = first;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = firstAmount;
            if (count == 2)
            {
                list.GetArrayElementAtIndex(1).FindPropertyRelative("Resource").objectReferenceValue = second;
                list.GetArrayElementAtIndex(1).FindPropertyRelative("Amount").intValue = secondAmount;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float R(System.Random r, float amount) => ((float)r.NextDouble() * 2f - 1f) * amount;

        // ================================================================ Sandık ve koleksiyon prefab'ları

        public static GameObject BuildChestPrefab(GameConfig config)
        {
            GameObject root = new GameObject("TreasureChest");
            GameObject closed = new GameObject("Closed");
            closed.transform.SetParent(root.transform, false);
            PrefabFactory.CreateModel(ModelCatalog.ChestClosed, closed.transform);
            GameObject open = new GameObject("Open");
            open.transform.SetParent(root.transform, false);
            PrefabFactory.CreateModel(ModelCatalog.ChestGold, open.transform);
            open.SetActive(false);
            BuilderUtil.SetVector2(root.AddComponent<NavObstacle>(), "size", new Vector2(0.9f, 0.7f));

            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(root.transform, false);
            PrefabFactory.AddGroundDecal("Halo", glow.transform, Vector3.zero, new Vector2(2.2f, 2.2f), new Color(1f, 0.85f, 0.3f, 0.45f), ArtGenerator.SoftCirclePath, 0f);
            GameObject sparkle = PrefabFactory.BuildGlintPrefab();
            PrefabUtility.InstantiatePrefab(sparkle, glow.transform);

            GameObject beacon = new GameObject("Beacon");
            beacon.transform.SetParent(root.transform, false);
            GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(column.GetComponent<Collider>());
            column.transform.SetParent(beacon.transform, false);
            column.transform.localPosition = new Vector3(0f, 6f, 0f);
            column.transform.localScale = new Vector3(0.7f, 6f, 0.7f);
            Renderer cr = column.GetComponent<Renderer>();
            cr.sharedMaterial = ArtGenerator.GetTransparent("M_Beacon", null, new Color(1f, 0.85f, 0.25f, 0.4f));
            cr.shadowCastingMode = ShadowCastingMode.Off;

            TreasureChest chest = root.AddComponent<TreasureChest>();
            BuilderUtil.SetField(chest, "config", config);
            BuilderUtil.SetField(chest, "closedModel", closed);
            BuilderUtil.SetField(chest, "openModel", open);
            BuilderUtil.SetField(chest, "glow", glow);
            BuilderUtil.SetField(chest, "beacon", beacon);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFactory.Folder + "/World/TreasureChest.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // Koleksiyon / figür eşyasının modeli (hem sahnedeki eşya hem vitrin için aynı).
        public static GameObject BuildItemModel(string key)
        {
            GameObject root = new GameObject("Model_" + key);
            Transform t = root.transform;
            switch (key)
            {
                case "compass":
                    MachineBuilder.Part(t, PrimitiveType.Cylinder, "Body", Vector3.zero, new Vector3(0.62f, 0.07f, 0.62f), Quaternion.Euler(70f, 0f, 0f), Brass);
                    MachineBuilder.Part(t, PrimitiveType.Cylinder, "Face", new Vector3(0f, 0.015f, -0.065f), new Vector3(0.5f, 0.02f, 0.5f), Quaternion.Euler(70f, 0f, 0f), Paper);
                    MachineBuilder.Part(t, PrimitiveType.Cube, "NeedleN", new Vector3(0f, 0.09f, -0.1f), new Vector3(0.05f, 0.19f, 0.02f), Quaternion.Euler(-20f, 0f, 0f), Red);
                    MachineBuilder.Part(t, PrimitiveType.Cube, "NeedleS", new Vector3(0f, -0.06f, -0.04f), new Vector3(0.05f, 0.17f, 0.02f), Quaternion.Euler(-20f, 0f, 0f), ArtGenerator.GetLit("M_MachineIron", null, new Color(0.3f, 0.3f, 0.32f)));
                    MachineBuilder.Part(t, PrimitiveType.Cylinder, "Ring", new Vector3(0f, 0.33f, 0.12f), new Vector3(0.12f, 0.03f, 0.12f), Quaternion.Euler(0f, 0f, 90f), Brass);
                    break;
                case "bottle":
                    PrefabFactory.CreateModel(ModelCatalog.BottleLetter, t);
                    MachineBuilder.Part(t, PrimitiveType.Cylinder, "Letter", new Vector3(0f, 0.2f, 0f), new Vector3(0.1f, 0.12f, 0.1f), Quaternion.identity, Paper);
                    break;
                case "skull":
                    PaintGold(PrefabFactory.CreateModel(ModelCatalog.GoldenSkull, t));
                    break;
                case "spyglass":
                    MachineBuilder.Part(t, PrimitiveType.Cylinder, "Tube1", new Vector3(0f, 0.12f, 0f), new Vector3(0.16f, 0.2f, 0.16f), Quaternion.Euler(0f, 0f, 70f), Leather);
                    MachineBuilder.Part(t, PrimitiveType.Cylinder, "Tube2", new Vector3(0.36f, 0.25f, 0f), new Vector3(0.13f, 0.17f, 0.13f), Quaternion.Euler(0f, 0f, 70f), Brass);
                    MachineBuilder.Part(t, PrimitiveType.Cylinder, "Tube3", new Vector3(0.66f, 0.36f, 0f), new Vector3(0.1f, 0.15f, 0.1f), Quaternion.Euler(0f, 0f, 70f), Brass);
                    MachineBuilder.Part(t, PrimitiveType.Cylinder, "Lens", new Vector3(-0.2f, 0.005f, 0f), new Vector3(0.19f, 0.03f, 0.19f), Quaternion.Euler(0f, 0f, 70f), Brass);
                    break;
                case "map":
                    PrefabFactory.CreateModel(ModelCatalog.MapPiece, t);
                    break;
                case "figure_head":
                    PaintGold(PrefabFactory.CreateModel(ModelCatalog.StatueHead, t));
                    break;
                case "figure_wing":
                    for (int i = 0; i < 4; i++)
                        MachineBuilder.Part(t, PrimitiveType.Cube, "Feather", new Vector3(0.12f * i, 0.18f + 0.07f * i, 0f), new Vector3(0.14f, 0.5f - i * 0.08f, 0.05f), Quaternion.Euler(0f, 0f, -25f - i * 12f), Gold);
                    break;
                default: // figure_tail
                    MachineBuilder.Part(t, PrimitiveType.Cube, "Stem", new Vector3(0f, 0.2f, 0f), new Vector3(0.14f, 0.4f, 0.1f), Quaternion.identity, Gold);
                    MachineBuilder.Part(t, PrimitiveType.Cube, "FinL", new Vector3(-0.13f, 0.45f, 0f), new Vector3(0.3f, 0.12f, 0.06f), Quaternion.Euler(0f, 0f, 30f), Gold);
                    MachineBuilder.Part(t, PrimitiveType.Cube, "FinR", new Vector3(0.13f, 0.45f, 0f), new Vector3(0.3f, 0.12f, 0.06f), Quaternion.Euler(0f, 0f, -30f), Gold);
                    break;
            }
            BuilderUtil.SetShadows(root, false);
            // Ortala ve yaklaşık 0.8 m boyuta getir.
            Bounds b = PrefabFactory.WorldBounds(root);
            float largest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (largest > 0.001f)
            {
                GameObject holder = new GameObject("Model_" + key);
                root.transform.SetParent(holder.transform, true);
                root.name = "Shape";
                root.transform.position -= b.center;
                holder.transform.localScale = Vector3.one * (0.8f / largest);
                root = holder;
            }
            return root;
        }

        private static void PaintGold(GameObject model)
        {
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = Gold;
                r.sharedMaterials = mats;
            }
        }

        public static GameObject BuildCollectiblePrefab(GameConfig config, string key, Collectible.Kind kind, string id, string displayName, out GameObject showcase)
        {
            BuilderUtil.EnsureFolder(PrefabFactory.Folder + "/Showcase");
            BuilderUtil.EnsureFolder(PrefabFactory.Folder + "/Discovery");
            GameObject showModel = BuildItemModel(key);
            showcase = PrefabUtility.SaveAsPrefabAsset(showModel, PrefabFactory.Folder + "/Showcase/" + key + ".prefab");
            Object.DestroyImmediate(showModel);

            GameObject root = new GameObject("Collectible_" + key);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = Vector3.up * 0.9f;
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(showcase);
            model.transform.SetParent(visual, false);
            model.transform.localScale = Vector3.one * 0.9f;
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(root.transform, false);
            Color c = kind == Collectible.Kind.FigurePart ? new Color(1f, 0.8f, 0.2f, 0.5f) : new Color(0.75f, 0.6f, 1f, 0.5f);
            PrefabFactory.AddGroundDecal("Halo", glow.transform, Vector3.zero, new Vector2(1.8f, 1.8f), c, ArtGenerator.SoftCirclePath, 0f);
            PrefabUtility.InstantiatePrefab(PrefabFactory.BuildGlintPrefab(), glow.transform);

            Collectible item = root.AddComponent<Collectible>();
            BuilderUtil.SetField(item, "config", config);
            BuilderUtil.SetEnum(item, "kind", (int)kind);
            BuilderUtil.SetString(item, "id", id);
            BuilderUtil.SetString(item, "displayName", displayName);
            BuilderUtil.SetField(item, "visual", visual);
            BuilderUtil.SetField(item, "showcaseModel", showcase);
            BuilderUtil.SetField(item, "glow", glow);
            BuilderUtil.EnsureFolder(PrefabFactory.Folder + "/Discovery");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFactory.Folder + "/Discovery/Collectible_" + key + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ================================================================ Tapınak ve batık

        public static void BuildTemple(Transform parent, System.Random random)
        {
            Vector3 c = MapLayout.Temple;
            ModelSpec[] columns = { ModelCatalog.TempleColumn, ModelCatalog.TempleColumnBroken };
            for (int i = 0; i < 7; i++)
            {
                float angle = i * 360f / 7f + 25f;
                if (Mathf.Abs(Mathf.DeltaAngle(angle, 270f)) < 22f) continue; // batı (köprüden gelen yol) açık
                Vector3 p = c + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * MapLayout.TempleRadius;
                Transform t = new GameObject("Column").transform;
                t.SetParent(parent, false);
                t.position = p;
                t.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                PrefabFactory.CreateModel(columns[i % 2], t);
                BuilderUtil.SetVector2(t.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(0.7f, 0.7f));
            }
            Transform ring = new GameObject("AltarRing").transform;
            ring.SetParent(parent, false);
            ring.position = c;
            PrefabFactory.CreateModel(ModelCatalog.TempleRing, ring);
            Transform obelisk = new GameObject("Obelisk").transform;
            obelisk.SetParent(parent, false);
            obelisk.position = c + new Vector3(-0.6f, 0f, 5.2f);
            PrefabFactory.CreateModel(ModelCatalog.TempleObelisk, obelisk);
            BuilderUtil.SetVector2(obelisk.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(1f, 1f));
            foreach (Vector3 off in new[] { new Vector3(4.6f, 0f, -2.2f), new Vector3(-4.2f, 0f, 2.8f), new Vector3(2.4f, 0f, 4.4f) })
            {
                Transform blk = new GameObject("Block").transform;
                blk.SetParent(parent, false);
                blk.position = c + off;
                blk.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                PrefabFactory.CreateModel(ModelCatalog.TempleBlock, blk);
                BuilderUtil.SetVector2(blk.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(0.9f, 0.9f));
            }
            BuilderUtil.MarkStatic(parent.gameObject);
        }

        // ================================================================ Kırık yapılar (yan görev: onar)

        public enum RepairKind { Well, Fence, Pier }

        public static RepairSite BuildRepairSite(RepairKind kind, string id, string displayName, Vector3 position, float yaw,
                                                 ResourceType first, int firstAmount, ResourceType second, int secondAmount,
                                                 AreaDefinition requiredArea, GameConfig config, List<Vector3> reserved)
        {
            GameObject root = new GameObject("Repair " + displayName);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            CapsuleCollider trigger = root.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.3f;
            trigger.height = 3f;
            trigger.center = new Vector3(0f, 1.5f, 0f);

            GameObject zone = new GameObject("Zone");
            zone.transform.SetParent(root.transform, false);
            Color green = new Color(0.55f, 0.95f, 0.5f);
            PrefabFactory.AddGroundDecal("Glow", zone.transform, Vector3.zero, new Vector2(2.6f, 2.6f), new Color(green.r, green.g, green.b, 0.3f), ArtGenerator.CirclePath, 0f);
            PrefabFactory.AddGroundDecal("Ring", zone.transform, Vector3.up * 0.004f, new Vector2(2.7f, 2.7f), new Color(1f, 1f, 1f, 0.8f), ArtGenerator.RingPath, 0f);
            ShipProgressView view = PrototypeSceneBuilder.CreateShipProgressView(zone.transform, 3.0f, 0.7f);

            GameObject broken = new GameObject("Broken");
            broken.transform.SetParent(root.transform, false);
            GameObject fixedGo = new GameObject("Fixed");
            fixedGo.transform.SetParent(root.transform, false);
            Vector3 behind = new Vector3(0f, 0f, 2.1f);
            Material stone = ArtGenerator.GetLit("M_WellStone", null, new Color(0.66f, 0.64f, 0.6f));
            switch (kind)
            {
                case RepairKind.Well:
                    BuildWell(broken.transform, behind, stone, true);
                    BuildWell(fixedGo.transform, behind, stone, false);
                    Transform block = new GameObject("WellBlock").transform;
                    block.SetParent(root.transform, false);
                    block.localPosition = behind;
                    BuilderUtil.SetVector2(block.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(1.5f, 1.5f));
                    break;
                case RepairKind.Fence:
                    BuildFence(broken.transform, behind, true);
                    BuildFence(fixedGo.transform, behind, false);
                    break;
                default:
                    // Sandal: kırıkken kumda yan yatmış (delik, tahtası eksik); onarılınca iskeleye bağlı yüzer.
                    ModelSpec boat = null;
                    foreach (ModelSpec spec in ModelCatalog.Decor) if (spec.Key == "Rowboat") boat = spec;
                    Transform b = new GameObject("Boat").transform;
                    b.SetParent(broken.transform, false);
                    b.localPosition = behind + new Vector3(0.2f, -0.05f, 0.4f);
                    b.localRotation = Quaternion.Euler(0f, 70f, 24f);
                    PrefabFactory.CreateModel(boat, b);
                    MachineBuilder.Part(broken.transform, PrimitiveType.Cube, "LoosePlank", behind + new Vector3(-1.2f, 0.05f, -0.4f), new Vector3(0.9f, 0.06f, 0.2f), Quaternion.Euler(0f, 30f, 0f), Plank);
                    Transform f = new GameObject("Boat").transform;
                    f.SetParent(fixedGo.transform, false);
                    f.localPosition = behind + new Vector3(-1.2f, ArtGenerator.WaterLevel - 0.05f, 2.2f);
                    f.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    PrefabFactory.CreateModel(boat, f);
                    MachineBuilder.Part(fixedGo.transform, PrimitiveType.Cylinder, "Mooring", behind + new Vector3(-1.9f, 0.3f, 1.2f), new Vector3(0.04f, 0.9f, 0.04f), Quaternion.Euler(60f, 0f, 0f), RopeMat);
                    break;
            }
            BuilderUtil.SetShadows(root, true);

            RepairSite site = root.AddComponent<RepairSite>();
            BuilderUtil.SetField(site, "config", config);
            BuilderUtil.SetString(site, "id", id);
            BuilderUtil.SetString(site, "displayName", displayName);
            SetRequirements(site, first, firstAmount, second, secondAmount);
            BuilderUtil.SetField(site, "requiredArea", requiredArea);
            BuilderUtil.SetField(site, "zoneVisual", zone);
            BuilderUtil.SetField(site, "progressView", view);
            BuilderUtil.SetField(site, "brokenVisual", broken);
            BuilderUtil.SetField(site, "fixedVisual", fixedGo);
            reserved.Add(new Vector3(position.x, position.z, 2.2f));
            reserved.Add(new Vector3(root.transform.TransformPoint(behind).x, root.transform.TransformPoint(behind).z, 2f));
            return site;
        }

        private static void BuildWell(Transform parent, Vector3 at, Material stone, bool broken)
        {
            MachineBuilder.Part(parent, PrimitiveType.Cylinder, "Ring", at + Vector3.up * 0.35f, new Vector3(1.3f, 0.35f, 1.3f), Quaternion.identity, stone);
            MachineBuilder.Part(parent, PrimitiveType.Cylinder, "Water", at + Vector3.up * 0.68f, new Vector3(1.0f, 0.02f, 1.0f), Quaternion.identity,
                                ArtGenerator.GetLit("M_WellWater", null, new Color(0.25f, 0.45f, 0.6f)));
            if (broken)
            {
                MachineBuilder.Part(parent, PrimitiveType.Cube, "FallenPost", at + new Vector3(0.9f, 0.1f, 0.3f), new Vector3(0.14f, 0.14f, 1.5f), Quaternion.Euler(0f, 30f, 0f), Wood);
                MachineBuilder.Part(parent, PrimitiveType.Cube, "BrokenPost", at + new Vector3(-0.6f, 0.45f, 0f), new Vector3(0.14f, 0.9f, 0.14f), Quaternion.Euler(0f, 0f, 18f), Wood);
                MachineBuilder.Part(parent, PrimitiveType.Cube, "Plank", at + new Vector3(-0.2f, 0.05f, -0.9f), new Vector3(0.9f, 0.06f, 0.25f), Quaternion.Euler(0f, 60f, 0f), Plank);
                return;
            }
            for (int s = -1; s <= 1; s += 2)
                MachineBuilder.Part(parent, PrimitiveType.Cube, "Post", at + new Vector3(s * 0.6f, 1.05f, 0f), new Vector3(0.14f, 1.4f, 0.14f), Quaternion.identity, Wood);
            MachineBuilder.Part(parent, PrimitiveType.Cylinder, "Axle", at + Vector3.up * 1.45f, new Vector3(0.1f, 0.62f, 0.1f), Quaternion.Euler(0f, 0f, 90f), Wood);
            MachineBuilder.Part(parent, PrimitiveType.Cube, "RoofL", at + new Vector3(-0.38f, 1.95f, 0f), new Vector3(0.9f, 0.06f, 1.1f), Quaternion.Euler(0f, 0f, 35f), Plank);
            MachineBuilder.Part(parent, PrimitiveType.Cube, "RoofR", at + new Vector3(0.38f, 1.95f, 0f), new Vector3(0.9f, 0.06f, 1.1f), Quaternion.Euler(0f, 0f, -35f), Plank);
            MachineBuilder.Part(parent, PrimitiveType.Cylinder, "Bucket", at + new Vector3(0f, 1.0f, 0f), new Vector3(0.28f, 0.14f, 0.28f), Quaternion.identity, Wood);
        }

        private static void BuildFence(Transform parent, Vector3 at, bool broken)
        {
            for (int i = 0; i < 3; i++)
            {
                Transform t = new GameObject("Fence").transform;
                t.SetParent(parent, false);
                t.localPosition = at + new Vector3((i - 1) * 1.9f, 0f, 0f);
                if (broken && i != 0) t.localRotation = Quaternion.Euler(i == 1 ? 70f : 0f, i == 2 ? 25f : 0f, i == 2 ? 35f : 0f);
                PrefabFactory.CreateModel(ModelCatalog.Fence, t);
            }
        }

        // ================================================================ Altın figür (geminin burnu)

        public static GameObject BuildFigurehead(Transform parent, Vector3 localPos, Quaternion localRot)
        {
            GameObject root = new GameObject("GoldenFigurehead");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localRotation = localRot;
            MachineBuilder.Part(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.35f, 0f), new Vector3(0.36f, 0.42f, 0.3f), Quaternion.identity, Gold);
            Transform head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.localPosition = new Vector3(0f, 0.75f, 0.05f);
            PrefabFactory.CreateModel(ModelCatalog.StatueHead, head);
            head.localScale = Vector3.one * 0.55f;
            foreach (Renderer r in head.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Gold;
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 3; i++)
                    MachineBuilder.Part(root.transform, PrimitiveType.Cube, "Feather", new Vector3(s * (0.22f + i * 0.1f), 0.45f + i * 0.08f, -0.05f),
                                        new Vector3(0.1f, 0.42f - i * 0.08f, 0.04f), Quaternion.Euler(0f, 0f, s * (35f + i * 14f)), Gold);
            }
            BuilderUtil.SetShadows(root, true);
            return root;
        }
    }
}
