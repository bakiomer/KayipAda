// CampBuilder.cs
// Kamp çevresindeki yapılar ve işçi kartı resimleri:
// - Tayfa Barakası: korsan bayraklı ahşap ev (Quaternius), önünde üstüne girilince işçi panelini açan alan, kapı
//   (işçiler buradan çıkar), varil/sandık süsleri ve ahşap tabela.
// - İşçi portreleri: karakter prefab'ı kendi "Idle" duruşunda, göğüsten yukarı çekilip Art/Generated'a PNG olarak
//   kaydedilir (kartlarda görünür).
// - Liman Pazarı: kampın batısında açık hava çarşısı: 3 tezgah (hasır, kırmızı-krem, mavi-krem tente), büyük "Pazar"
//   tabelası, ürün yığınları, kasa sandığı, satıcı (el sallar / ürün düzenler); satış alanı önde büyük para ikonlu daire.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace KayipAda.EditorTools
{
    public static class CampBuilder
    {
        private const string Folder = PrefabFactory.Folder;

        // ================================================================ Tayfa Barakası

        public static GameObject BuildBarracks(GameConfig config, GameObject flagPrefab)
        {
            GameObject root = new GameObject("CrewBarracks");
            // Oyuncunun girdiği alan: evin önünde, yerde mavi daire.
            CapsuleCollider trigger = root.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;
            trigger.height = 3f;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            PrefabFactory.AddGroundDecal("Decal", root.transform, Vector3.zero, new Vector2(2.8f, 2.8f), new Color(0.45f, 0.85f, 1f, 0.45f),
                                         ArtGenerator.CirclePath, 0f);
            PrefabFactory.AddGroundDecal("Ring", root.transform, Vector3.up * 0.005f, new Vector2(2.9f, 2.9f), new Color(1f, 1f, 1f, 0.8f),
                                         ArtGenerator.RingPath, 0f);

            // Ev: alanın arkasında.
            Transform house = new GameObject("House").transform;
            house.SetParent(root.transform, false);
            house.localPosition = new Vector3(0f, 0f, MapLayout.BarracksDepth);
            house.localRotation = Quaternion.Euler(0f, MapLayout.BarracksHouseYaw, 0f);
            PrefabFactory.CreateModel(ModelCatalog.Barracks, house);
            BuilderUtil.SetVector2(house.gameObject.AddComponent<NavObstacle>(), "size", MapLayout.BarracksObstacle);
            BuilderUtil.SetBool(house.GetComponent<NavObstacle>(), "oriented", true);
            BuilderUtil.SetFloat(house.GetComponent<NavObstacle>(), "cornerRadius", 0.35f);
            Bounds hb = PrefabFactory.WorldBounds(house.gameObject);

            // Korsan bayrağı evin yanında, direkte.
            if (flagPrefab != null)
            {
                GameObject flag = (GameObject)PrefabUtility.InstantiatePrefab(flagPrefab);
                flag.transform.SetParent(root.transform, false);
                flag.transform.localPosition = new Vector3(hb.extents.x + 0.4f, 0f, MapLayout.BarracksDepth - 0.6f);
                flag.transform.localScale = Vector3.one * 1.5f;
            }

            // Kapı: evin ön yüzü (alan tarafı), ortada; işçiler buradan çıkar ve önünde sevinir.
            Transform door = new GameObject("Door").transform;
            door.SetParent(root.transform, false);
            door.localPosition = new Vector3(0f, 0f, MapLayout.BarracksDepth - MapLayout.BarracksObstacle.y * 0.5f - 0.45f);
            door.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Transform cheer = new GameObject("CheerPoint").transform;
            cheer.SetParent(root.transform, false);
            cheer.localPosition = new Vector3(1.9f, 0f, -0.6f);

            Transform fx = new GameObject("EffectPoint").transform;
            fx.SetParent(root.transform, false);
            fx.localPosition = door.localPosition + Vector3.up * 1.2f;

            // Süsler: kapının yanlarında varil ve sandık.
            AddProp(root.transform, "Barrel", new Vector3(-hb.extents.x - 0.2f, 0f, MapLayout.BarracksDepth - 1.2f), 20f);
            AddProp(root.transform, "Barrel", new Vector3(-hb.extents.x + 0.4f, 0f, MapLayout.BarracksDepth - 1.6f), 70f);
            AddProp(root.transform, "Chest", new Vector3(hb.extents.x - 0.2f, 0f, MapLayout.BarracksDepth - 1.7f), -25f);

            PrefabFactory.CreateBoardSign(root.transform, new Vector3(0f, hb.size.y + 0.7f, MapLayout.BarracksDepth), "Tayfa Barakası");

            UpgradeStation station = root.AddComponent<UpgradeStation>();
            BuilderUtil.SetField(station, "config", config);
            BuilderUtil.SetField(station, "effectPoint", fx);
            PrefabFactory.AddPanelHint(station, Vector3.zero, 2.8f, ArtGenerator.StarPath, new Color(0.45f, 0.85f, 1f));
            return PrefabFactorySave(root, Folder + "/World/CrewBarracks.prefab");
        }

        private static void AddProp(Transform parent, string decorKey, Vector3 pos, float yaw)
        {
            foreach (ModelSpec spec in ModelCatalog.Decor)
            {
                if (spec.Key != decorKey) continue;
                Transform t = new GameObject(decorKey).transform;
                t.SetParent(parent, false);
                t.localPosition = pos;
                t.localRotation = Quaternion.Euler(0f, yaw, 0f);
                PrefabFactory.CreateModel(spec, t);
                return;
            }
        }

        private static GameObject PrefabFactorySave(GameObject root, string path)
        {
            BuilderUtil.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ================================================================ İşçi portreleri

        // Karakteri "Idle" duruşunda göğüsten yukarı, hafif yandan çeker; arka plan saydam. Kartın resmi olur.
        public static Sprite RenderPortrait(GameObject characterPrefab, ModelSpec spec)
        {
            string path = ArtGenerator.GeneratedFolder + "/Portrait_" + spec.Key + ".png";
            const int size = 256;
            PreviewRenderUtility preview = new PreviewRenderUtility();
            try
            {
                GameObject go = preview.InstantiatePrefabInScene(characterPrefab);
                foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
                Transform blob = go.transform.Find("BlobShadow");
                if (blob != null) blob.gameObject.SetActive(false);
                Animator animator = go.GetComponentInChildren<Animator>();
                AnimationClip idle = CharacterAnimation.FindClip(spec.Path, "Idle");
                if (animator != null && idle != null) idle.SampleAnimation(animator.gameObject, 0.4f);

                Transform bodyRoot = go.transform.Find("Body");
                Bounds b = new Bounds(go.transform.position + Vector3.up, Vector3.one);
                bool first = true;
                foreach (Renderer r in (bodyRoot != null ? bodyRoot : go.transform).GetComponentsInChildren<Renderer>())
                {
                    if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;
                    if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
                Vector3 target = new Vector3(b.center.x, b.min.y + b.size.y * 0.7f, b.center.z);
                Camera cam = preview.camera;
                cam.fieldOfView = 24f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 50f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                float distance = b.size.y * 1.55f;
                cam.transform.position = target + new Vector3(0.45f, 0.12f, 1f).normalized * distance;
                cam.transform.LookAt(target);
                preview.lights[0].intensity = 1.3f;
                preview.lights[0].color = new Color(1f, 0.96f, 0.88f);
                preview.lights[0].transform.rotation = Quaternion.Euler(35f, 200f, 0f);
                preview.lights[1].intensity = 0.6f;
                preview.ambientColor = new Color(0.55f, 0.55f, 0.6f);

                // Saydam zemin: siyah ve beyaz zeminde iki kez çizilir; farktan her pikselin saydamlığı bulunur.
                Color[] onBlack = RenderPixels(preview, Color.black, size);
                Color[] onWhite = RenderPixels(preview, Color.white, size);
                Color[] pixels = new Color[onBlack.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color k = onBlack[i], w = onWhite[i];
                    float a = Mathf.Clamp01(1f - ((w.r - k.r) + (w.g - k.g) + (w.b - k.b)) / 3f);
                    pixels[i] = a > 0.01f ? new Color(Mathf.Clamp01(k.r / a), Mathf.Clamp01(k.g / a), Mathf.Clamp01(k.b / a), a) : new Color(0f, 0f, 0f, 0f);
                }
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.SetPixels(pixels);
                tex.Apply();
                BuilderUtil.WritePng(tex, path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Kayıp Ada: Portre çizilemedi (" + spec.Key + "): " + e.Message);
            }
            finally
            {
                preview.Cleanup();
            }
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color[] RenderPixels(PreviewRenderUtility preview, Color background, int size)
        {
            preview.camera.backgroundColor = background;
            preview.BeginStaticPreview(new Rect(0, 0, size, size));
            preview.Render(true);
            Texture2D rendered = preview.EndStaticPreview();
            Color[] result = rendered.width == size && rendered.height == size ? rendered.GetPixels() : Resize(rendered, size);
            Object.DestroyImmediate(rendered);
            return result;
        }

        private static Color[] Resize(Texture2D source, int size)
        {
            Color[] result = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    result[y * size + x] = source.GetPixelBilinear((x + 0.5f) / size, (y + 0.5f) / size);
            return result;
        }

        // ================================================================ Liman Pazarı (kampın batısında, açık hava çarşısı)

        // Kök = satış alanı (büyük, para ikonlu daire). Arkasında (yerel +Z) üç tezgah: ortada büyük kırmızı-krem çizgili
        // tenteli ana tezgah (satıcı, kasa sandığı, kereste/halat/bez), solda hasır tenteli meyve tezgahı, sağda mavi-krem
        // kumaş tenteli taş/tuğla/hurda tezgahı. Ortada büyük ahşap "Pazar" tabelası; çevrede sandık, çuval, fıçı, bayrak,
        // fener. Tezgahlardaki yığınlar gerçek kaynak modelleri; satılan türün yığını kısa süre büyür (Market).
        public static GameObject BuildMarket(GameConfig config, ResourceType[] goods, GameObject flagPrefab)
        {
            GameObject root = new GameObject("Market");
            CapsuleCollider trigger = root.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = MapLayout.MarketZoneRadius;
            trigger.height = 3f;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            Color gold = new Color(1f, 0.8f, 0.2f);
            float d = MapLayout.MarketZoneRadius * 2f + 0.3f;
            PrefabFactory.AddGroundDecal("Plaza", root.transform, new Vector3(0f, -0.004f, 1.6f), new Vector2(11.5f, 7.5f), new Color(0.78f, 0.62f, 0.4f, 0.55f),
                                         ArtGenerator.SoftCirclePath, 0f);
            PrefabFactory.AddGroundDecal("Glow", root.transform, Vector3.zero, new Vector2(d, d), new Color(gold.r, gold.g, gold.b, 0.4f), ArtGenerator.CirclePath, 0f);
            PrefabFactory.AddGroundDecal("Ring", root.transform, Vector3.up * 0.004f, new Vector2(d + 0.1f, d + 0.1f), new Color(1f, 0.86f, 0.3f, 0.95f), ArtGenerator.RingPath, 0f);
            PrefabFactory.AddGroundDecal("Coin", root.transform, Vector3.up * 0.008f, new Vector2(1.7f, 1.7f), Color.white, ArtGenerator.CoinIconPath, 0f);

            Material wood = ArtGenerator.GetLit("M_ShipWood", null, MapLayout.ShipWoodColor);
            Material plank = ArtGenerator.GetLit("M_ShipPlank", null, MapLayout.ShipPlankColor);
            Material red = ArtGenerator.GetLit("M_Awning_" + ColorUtility.ToHtmlStringRGB(MapLayout.MarketAwningColor), null, MapLayout.MarketAwningColor);
            Material cream = ArtGenerator.GetLit("M_AwningCream", null, new Color(0.96f, 0.92f, 0.8f));
            Material blue = ArtGenerator.GetLit("M_AwningBlue", null, new Color(0.25f, 0.5f, 0.8f));
            Material straw = ArtGenerator.GetLit("M_Straw", null, new Color(0.88f, 0.74f, 0.42f));
            Material strawDark = ArtGenerator.GetLit("M_StrawDark", null, new Color(0.74f, 0.58f, 0.3f));

            Dictionary<string, ResourceType> byId = new Dictionary<string, ResourceType>();
            foreach (ResourceType g in goods) if (g != null) byId[g.Id] = g;
            List<string> ids = new List<string>();
            List<Object> displays = new List<Object>();

            // Ana tezgah (ortada, büyük).
            Vector2 size = MapLayout.MarketStallSize;
            float counterH = 1.05f;
            Transform main = Stall(root.transform, "Stall", new Vector3(0f, 0f, MapLayout.MarketStallDepth), 0f, size, counterH, 3.0f, 3.35f,
                                   wood, plank, red, cream, false);
            float topY = counterH + 0.08f, front = -size.y * 0.5f + 0.45f;
            AddDisplay(main, byId, "wood", new Vector3(-1.15f, topY, front), ids, displays);
            AddDisplay(main, byId, "plank", new Vector3(-0.45f, topY, front), ids, displays);
            AddDisplay(main, byId, "rope", new Vector3(1.2f, topY, front + 0.05f), ids, displays);
            AddDisplay(main, byId, "cloth", new Vector3(0.55f, topY, front + 0.25f), ids, displays);
            Transform defaultDisplay = displays.Count > 1 ? (Transform)displays[1] : null;

            // Kasa sandığı: ana tezgahın sağ ucunda; kapağı menteşeden açılır, paralar içine uçar.
            Transform cash = new GameObject("CashBox").transform;
            cash.SetParent(main, false);
            cash.localPosition = new Vector3(1.25f, topY, front - 0.25f + 0.55f);
            Material chestWood = ArtGenerator.GetLit("M_ChestWood", null, new Color(0.5f, 0.3f, 0.14f));
            Material chestBand = ArtGenerator.GetLit("M_ChestBand", null, new Color(0.85f, 0.68f, 0.25f));
            Material coinMat = ArtGenerator.GetLit("M_Gold", null, new Color(1f, 0.78f, 0.22f));
            MachineBuilder.Part(cash, PrimitiveType.Cube, "Body", new Vector3(0f, 0.15f, 0f), new Vector3(0.55f, 0.3f, 0.38f), Quaternion.identity, chestWood);
            MachineBuilder.Part(cash, PrimitiveType.Cube, "Band", new Vector3(0f, 0.15f, 0f), new Vector3(0.57f, 0.06f, 0.4f), Quaternion.identity, chestBand);
            MachineBuilder.Part(cash, PrimitiveType.Cylinder, "Coins", new Vector3(0f, 0.29f, 0f), new Vector3(0.42f, 0.02f, 0.28f), Quaternion.identity, coinMat);
            Transform lid = new GameObject("Lid").transform;
            lid.SetParent(cash, false);
            lid.localPosition = new Vector3(0f, 0.3f, 0.19f); // arka kenar (menteşe)
            MachineBuilder.Part(lid, PrimitiveType.Cube, "LidBoard", new Vector3(0f, 0.05f, -0.19f), new Vector3(0.57f, 0.1f, 0.4f), Quaternion.identity, chestWood);
            MachineBuilder.Part(lid, PrimitiveType.Cube, "LidBand", new Vector3(0f, 0.06f, -0.19f), new Vector3(0.6f, 0.06f, 0.08f), Quaternion.identity, chestBand);

            // Yan tezgahlar: içe dönük, biraz geride ve küçük.
            Vector2 sideSize = new Vector2(2.4f, 1.4f);
            Transform left = Stall(root.transform, "StallFruit", new Vector3(-3.5f, 0f, 2.3f), -30f, sideSize, 0.95f, 2.6f, 2.9f, wood, plank, straw, strawDark, true);
            Transform right = Stall(root.transform, "StallStone", new Vector3(3.5f, 0f, 2.3f), 30f, sideSize, 0.95f, 2.6f, 2.9f, wood, plank, blue, cream, false);
            float sideTop = 0.95f + 0.08f, sideFront = -sideSize.y * 0.5f + 0.4f;
            AddDisplay(left, byId, "fruit", new Vector3(-0.5f, sideTop, sideFront), ids, displays);
            AddProp(left, "FruitCrate", new Vector3(0.55f, sideTop, sideFront + 0.05f), -8f, 0.75f);
            AddDisplay(right, byId, "stone", new Vector3(-0.7f, sideTop, sideFront), ids, displays);
            AddDisplay(right, byId, "brick", new Vector3(0.05f, sideTop, sideFront), ids, displays);
            AddDisplay(right, byId, "scrap", new Vector3(0.75f, sideTop, sideFront), ids, displays);
            AddDisplay(right, byId, "nail", new Vector3(0.75f, sideTop, sideFront + 0.45f), ids, displays);
            AddDisplay(left, byId, "fiber", new Vector3(0.6f, sideTop, sideFront + 0.5f), ids, displays);

            // Satıcı: ana tezgahın arkasında; el sallar, yan tezgahlara dönüp ürün düzenler.
            Transform merchant = new GameObject("Merchant").transform;
            merchant.SetParent(main, false);
            merchant.localPosition = new Vector3(-0.15f, 0f, -0.1f);
            merchant.localRotation = Quaternion.Euler(0f, 180f, 0f);
            GameObject model = PrefabFactory.CreateModel(ModelCatalog.Merchant, merchant);
            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = CharacterAnimation.GetClipSet(ModelCatalog.Merchant.Path, new[] { "Wave", "Yes", "Idle" }, "Merchant_Idle");
            animator.applyRootMotion = false;
            PrefabFactory.AddBlobShadow(merchant, 1.1f);
            MerchantIdle idle = merchant.gameObject.AddComponent<MerchantIdle>();
            BuilderUtil.SetField(idle, "config", config);
            BuilderUtil.SetField(idle, "animator", animator);

            // Ortada büyük ahşap "Pazar" tabelası (ana tezgahın üstünde, iki direkli kemer).
            float archH = 4.3f;
            for (int s = -1; s <= 1; s += 2)
                MachineBuilder.Part(root.transform, PrimitiveType.Cube, "ArchPost", new Vector3(s * (size.x * 0.5f + 0.35f), archH * 0.5f, MapLayout.MarketStallDepth + size.y * 0.5f + 0.15f),
                                    new Vector3(0.18f, archH, 0.18f), Quaternion.identity, wood);
            MachineBuilder.Part(root.transform, PrimitiveType.Cube, "ArchBeam", new Vector3(0f, archH - 0.1f, MapLayout.MarketStallDepth + size.y * 0.5f + 0.15f),
                                new Vector3(size.x + 1.1f, 0.18f, 0.22f), Quaternion.identity, wood);
            Transform bigSign = new GameObject("BigSign").transform;
            bigSign.SetParent(root.transform, false);
            bigSign.localPosition = new Vector3(0f, archH + 0.55f, MapLayout.MarketStallDepth + size.y * 0.5f + 0.15f);
            bigSign.localScale = Vector3.one * 1.45f;
            PrefabFactory.CreateBoardSign(bigSign, Vector3.zero, "Pazar");

            // Çevre: sandık, çuval, fıçı, bayrak, fener (satış alanının dışında; büyükler katı).
            AddSolidProp(root.transform, "Barrel", new Vector3(-size.x * 0.5f - 0.5f, 0f, MapLayout.MarketStallDepth - 0.2f), 15f, 0.6f);
            AddSolidProp(root.transform, "Crate", new Vector3(size.x * 0.5f + 0.55f, 0f, MapLayout.MarketStallDepth - 0.3f), -20f, 0.7f);
            AddSolidProp(root.transform, "CrateBottles", new Vector3(-5.3f, 0f, 1.0f), 30f, 0.7f);
            AddSolidProp(root.transform, "Barrel", new Vector3(5.3f, 0f, 1.1f), 50f, 0.6f);
            AddSolidProp(root.transform, "Chest", new Vector3(-4.9f, 0f, 3.9f), 70f, 0.7f);
            AddSolidProp(root.transform, "Crate", new Vector3(5.0f, 0f, 3.9f), 10f, 0.7f);
            Material sack = ArtGenerator.GetLit("M_Sack", null, new Color(0.78f, 0.66f, 0.47f));
            Sack(root.transform, new Vector3(-2.2f, 0f, -0.2f), sack);
            Sack(root.transform, new Vector3(-2.55f, 0f, 0.15f), sack);
            Sack(root.transform, new Vector3(2.3f, 0f, -0.1f), sack);
            Material glass = ArtGenerator.GetLit("M_LanternGlow", null, new Color(1f, 0.85f, 0.45f));
            for (int s = -1; s <= 1; s += 2)
            {
                Lantern(root.transform, new Vector3(s * 2.4f, 0f, -1.6f), wood, glass);
                if (flagPrefab != null)
                {
                    GameObject flag = (GameObject)PrefabUtility.InstantiatePrefab(flagPrefab);
                    flag.transform.SetParent(root.transform, false);
                    flag.transform.localPosition = new Vector3(s * 5.5f, 0f, 2.6f);
                    flag.transform.localScale = Vector3.one * 1.2f;
                }
            }

            Transform drop = new GameObject("DropPoint").transform;
            drop.SetParent(root.transform, false);
            drop.localPosition = new Vector3(0f, counterH + 0.2f, MapLayout.MarketStallDepth - size.y * 0.5f + 0.4f);

            Market market = root.AddComponent<Market>();
            BuilderUtil.SetField(market, "config", config);
            BuilderUtil.SetField(market, "dropPoint", drop);
            BuilderUtil.SetField(market, "cashBox", cash);
            BuilderUtil.SetField(market, "cashLid", lid);
            BuilderUtil.SetField(market, "defaultDisplay", defaultDisplay);
            BuilderUtil.SetArray(market, "displays", displays.ToArray());
            SerializedObject so = new SerializedObject(market);
            SerializedProperty list = so.FindProperty("displayIds");
            list.arraySize = ids.Count;
            for (int i = 0; i < ids.Count; i++) list.GetArrayElementAtIndex(i).stringValue = ids[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabFactorySave(root, Folder + "/World/Market.prefab");
        }

        // Tezgah: tezgah gövdesi + üst tahta + ön çıtalar + 4 direk + öne eğik tente (iki renk çizgili ya da hasır).
        private static Transform Stall(Transform parent, string name, Vector3 pos, float yaw, Vector2 size, float counterH, float frontH, float backH,
                                       Material wood, Material plank, Material a, Material b, bool strawRoof)
        {
            Transform stall = new GameObject(name).transform;
            stall.SetParent(parent, false);
            stall.localPosition = pos;
            stall.localRotation = Quaternion.Euler(0f, yaw, 0f);
            MachineBuilder.Part(stall, PrimitiveType.Cube, "Counter", new Vector3(0f, counterH * 0.5f, -size.y * 0.5f + 0.4f), new Vector3(size.x, counterH, 0.8f), Quaternion.identity, plank);
            MachineBuilder.Part(stall, PrimitiveType.Cube, "CounterTop", new Vector3(0f, counterH + 0.04f, -size.y * 0.5f + 0.4f), new Vector3(size.x + 0.15f, 0.08f, 0.95f), Quaternion.identity, wood);
            int slats = Mathf.RoundToInt(size.x / 0.55f);
            for (int i = 0; i < slats; i++)
                MachineBuilder.Part(stall, PrimitiveType.Cube, "Slat", new Vector3(-size.x * 0.5f + 0.25f + i * (size.x - 0.5f) / Mathf.Max(1, slats - 1), counterH * 0.5f, -size.y * 0.5f - 0.005f),
                                    new Vector3(0.06f, counterH - 0.1f, 0.02f), Quaternion.identity, wood);
            for (int s = -1; s <= 1; s += 2)
            {
                MachineBuilder.Part(stall, PrimitiveType.Cube, "PostFront", new Vector3(s * (size.x * 0.5f - 0.08f), frontH * 0.5f, -size.y * 0.5f + 0.08f), new Vector3(0.14f, frontH, 0.14f), Quaternion.identity, wood);
                MachineBuilder.Part(stall, PrimitiveType.Cube, "PostBack", new Vector3(s * (size.x * 0.5f - 0.08f), backH * 0.5f, size.y * 0.5f - 0.08f), new Vector3(0.14f, backH, 0.14f), Quaternion.identity, wood);
            }
            MachineBuilder.Part(stall, PrimitiveType.Cube, "BackShelf", new Vector3(0f, 1.1f, size.y * 0.5f - 0.12f), new Vector3(size.x - 0.3f, 0.08f, 0.3f), Quaternion.identity, plank);
            Transform roof = new GameObject("Awning").transform;
            roof.SetParent(stall, false);
            roof.localPosition = new Vector3(0f, (frontH + backH) * 0.5f + 0.04f, 0f);
            roof.localRotation = Quaternion.Euler(-Mathf.Atan2(backH - frontH, size.y) * Mathf.Rad2Deg, 0f, 0f);
            if (strawRoof)
            {
                // Hasır: üst üste binen kalın sıralar, saçaklı ön kenar.
                for (int r = 0; r < 4; r++)
                    MachineBuilder.Part(roof, PrimitiveType.Cube, "Thatch", new Vector3(0f, 0.05f - r * 0.03f, size.y * 0.5f + 0.3f - (r + 0.5f) * (size.y + 0.7f) / 4f),
                                        new Vector3(size.x + 0.6f, 0.12f, (size.y + 0.7f) / 4f + 0.12f), Quaternion.Euler(-6f, 0f, 0f), r % 2 == 0 ? a : b);
                for (int i = 0; i < 9; i++)
                    MachineBuilder.Part(roof, PrimitiveType.Cube, "Fringe", new Vector3(-size.x * 0.5f - 0.2f + i * (size.x + 0.4f) / 8f, -0.18f, -size.y * 0.5f - 0.35f),
                                        new Vector3(0.18f, 0.3f, 0.04f), Quaternion.Euler(0f, 0f, (i % 3 - 1) * 6f), b);
            }
            else
            {
                int strips = Mathf.Max(5, Mathf.RoundToInt((size.x + 0.5f) / 0.5f) | 1);
                float stripW = (size.x + 0.5f) / strips;
                for (int i = 0; i < strips; i++)
                {
                    float x = -size.x * 0.5f - 0.25f + stripW * (i + 0.5f);
                    Material m = i % 2 == 0 ? a : b;
                    MachineBuilder.Part(roof, PrimitiveType.Cube, "Strip", new Vector3(x, 0f, 0f), new Vector3(stripW + 0.01f, 0.06f, size.y + 0.7f), Quaternion.identity, m);
                    // Dalgalı saçak: çizgiler sırayla uzun-kısa.
                    MachineBuilder.Part(roof, PrimitiveType.Cube, "Valance", new Vector3(x, i % 2 == 0 ? -0.18f : -0.13f, -size.y * 0.5f - 0.35f),
                                        new Vector3(stripW + 0.01f, i % 2 == 0 ? 0.36f : 0.26f, 0.04f), Quaternion.identity, m);
                }
            }
            NavObstacle ob = stall.gameObject.AddComponent<NavObstacle>();
            BuilderUtil.SetVector2(ob, "size", size);
            BuilderUtil.SetBool(ob, "oriented", true);
            BuilderUtil.SetFloat(ob, "cornerRadius", 0.2f);
            BuilderUtil.SetShadows(stall.gameObject, true);
            return stall;
        }

        // Tezgahta bir kaynağın yığını: gerçek kaynak modellerinden küçük piramit (alt sıra 3, üstte 2, en üstte 1).
        private static void AddDisplay(Transform stall, Dictionary<string, ResourceType> byId, string id, Vector3 pos, List<string> ids, List<Object> displays)
        {
            if (!byId.TryGetValue(id, out ResourceType type) || type.ItemPrefab == null) return;
            Transform pile = new GameObject("Display_" + id).transform;
            pile.SetParent(stall, false);
            pile.localPosition = pos;
            float w = Mathf.Max(0.12f, type.StackWidth) * 0.8f, h = Mathf.Max(0.1f, type.StackHeight) * 0.8f;
            int[] rows = { 3, 2, 1 };
            for (int r = 0; r < rows.Length; r++)
                for (int i = 0; i < rows[r]; i++)
                {
                    GameObject item = Object.Instantiate(type.ItemPrefab.gameObject, pile);
                    item.name = id;
                    foreach (MonoBehaviour mb in item.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
                    item.transform.localPosition = new Vector3((i - (rows[r] - 1) * 0.5f) * w, r * h, (r % 2) * 0.03f);
                    item.transform.localRotation = Quaternion.Euler(0f, (i * 37 + r * 19) % 30 - 15f, 0f);
                    item.transform.localScale = item.transform.localScale * 0.8f;
                }
            BuilderUtil.SetShadows(pile.gameObject, false);
            ids.Add(id);
            displays.Add(pile);
        }

        private static void Sack(Transform parent, Vector3 pos, Material mat)
        {
            MachineBuilder.Part(parent, PrimitiveType.Sphere, "Sack", pos + new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.62f, 0.5f), Quaternion.Euler(0f, pos.x * 40f, 6f), mat);
            MachineBuilder.Part(parent, PrimitiveType.Cylinder, "SackTie", pos + new Vector3(0f, 0.64f, 0f), new Vector3(0.18f, 0.06f, 0.18f), Quaternion.identity, mat);
        }

        private static void Lantern(Transform parent, Vector3 pos, Material wood, Material glow)
        {
            MachineBuilder.Part(parent, PrimitiveType.Cube, "LanternPost", pos + new Vector3(0f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), Quaternion.identity, wood);
            MachineBuilder.Part(parent, PrimitiveType.Cube, "LanternArm", pos + new Vector3(0f, 1.78f, -0.15f), new Vector3(0.06f, 0.06f, 0.36f), Quaternion.identity, wood);
            MachineBuilder.Part(parent, PrimitiveType.Cube, "LanternGlass", pos + new Vector3(0f, 1.58f, -0.3f), new Vector3(0.2f, 0.26f, 0.2f), Quaternion.identity, glow);
            MachineBuilder.Part(parent, PrimitiveType.Cube, "LanternCap", pos + new Vector3(0f, 1.74f, -0.3f), new Vector3(0.26f, 0.06f, 0.26f), Quaternion.identity, wood);
            GameObject blk = new GameObject("LanternBlock");
            blk.transform.SetParent(parent, false);
            blk.transform.localPosition = pos;
            BuilderUtil.SetVector2(blk.AddComponent<NavObstacle>(), "size", new Vector2(0.3f, 0.3f));
        }

        private static void AddProp(Transform parent, string key, Vector3 pos, float yaw, float scale)
        {
            foreach (ModelSpec spec in ModelCatalog.Decor)
            {
                if (spec.Key != key) continue;
                Transform t = new GameObject(key).transform;
                t.SetParent(parent, false);
                t.localPosition = pos;
                t.localRotation = Quaternion.Euler(0f, yaw, 0f);
                t.localScale = Vector3.one * scale;
                PrefabFactory.CreateModel(spec, t);
                return;
            }
        }

        private static void AddSolidProp(Transform parent, string key, Vector3 pos, float yaw, float obstacle)
        {
            AddProp(parent, key, pos, yaw, 1f);
            Transform t = new GameObject(key + "_Block").transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            BuilderUtil.SetVector2(t.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(obstacle, obstacle));
        }
    }
}
