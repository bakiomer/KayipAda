// CampBuilder.cs
// Kamp çevresindeki yapılar ve işçi kartı resimleri:
// - Tayfa Barakası: korsan bayraklı ahşap ev (Quaternius), önünde üstüne girilince işçi panelini açan alan, kapı
//   (işçiler buradan çıkar), varil/sandık süsleri ve ahşap tabela.
// - İşçi portreleri: karakter prefab'ı kendi "Idle" duruşunda, göğüsten yukarı çekilip Art/Generated'a PNG olarak
//   kaydedilir (kartlarda görünür).
// - Liman Pazarı: açık, çizgili tenteli ahşap tezgah; önünde sandık/variller, arkasında el sallayan satıcı,
//   üstünde "Pazar" tabelası; satış alanı tezgahın önünde büyük para ikonlu daire. Engel tam tezgah kadar.

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

        // ================================================================ Liman Pazarı

        public static GameObject BuildMarket(GameConfig config)
        {
            GameObject root = new GameObject("Market");
            // Satış alanı: tezgahın önünde büyük daire (para ikonlu).
            CapsuleCollider trigger = root.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            trigger.height = 3f;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            Color gold = new Color(1f, 0.8f, 0.2f);
            PrefabFactory.AddGroundDecal("Glow", root.transform, Vector3.zero, new Vector2(3.2f, 3.2f), new Color(gold.r, gold.g, gold.b, 0.35f),
                                         ArtGenerator.CirclePath, 0f);
            PrefabFactory.AddGroundDecal("Ring", root.transform, Vector3.up * 0.004f, new Vector2(3.3f, 3.3f), new Color(1f, 0.86f, 0.3f, 0.95f),
                                         ArtGenerator.RingPath, 0f);
            PrefabFactory.AddGroundDecal("Coin", root.transform, Vector3.up * 0.008f, new Vector2(1.5f, 1.5f), Color.white,
                                         ArtGenerator.CoinIconPath, 0f);

            Material wood = ArtGenerator.GetLit("M_ShipWood", null, MapLayout.ShipWoodColor);
            Material plank = ArtGenerator.GetLit("M_ShipPlank", null, MapLayout.ShipPlankColor);
            Material red = ArtGenerator.GetLit("M_Awning_" + ColorUtility.ToHtmlStringRGB(MapLayout.MarketAwningColor), null, MapLayout.MarketAwningColor);
            Material cream = ArtGenerator.GetLit("M_AwningCream", null, new Color(0.96f, 0.92f, 0.8f));

            // Tezgah: ön kenarı alanın hemen arkasında.
            Transform stall = new GameObject("Stall").transform;
            stall.SetParent(root.transform, false);
            stall.localPosition = new Vector3(0f, 0f, MapLayout.MarketStallDepth);
            Vector2 size = MapLayout.MarketStallSize; // genişlik (X) × derinlik (Z)
            float counterH = 1.05f;
            MachineBuilder.Part(stall, PrimitiveType.Cube, "Counter", new Vector3(0f, counterH * 0.5f, -size.y * 0.5f + 0.4f),
                                new Vector3(size.x, counterH, 0.8f), Quaternion.identity, plank);
            MachineBuilder.Part(stall, PrimitiveType.Cube, "CounterTop", new Vector3(0f, counterH + 0.04f, -size.y * 0.5f + 0.4f),
                                new Vector3(size.x + 0.15f, 0.08f, 0.95f), Quaternion.identity, wood);
            // Ön yüzde dikey tahta çizgiler.
            for (int i = 0; i < 6; i++)
                MachineBuilder.Part(stall, PrimitiveType.Cube, "Slat", new Vector3(-size.x * 0.5f + 0.25f + i * (size.x - 0.5f) / 5f, counterH * 0.5f, -size.y * 0.5f - 0.005f),
                                    new Vector3(0.06f, counterH - 0.1f, 0.02f), Quaternion.identity, wood);
            // Direkler: önde iki (tezgahın köşelerinde), arkada iki (daha uzun → tente öne eğik).
            float frontH = 3.0f, backH = 3.35f;
            for (int s = -1; s <= 1; s += 2)
            {
                MachineBuilder.Part(stall, PrimitiveType.Cube, "PostFront", new Vector3(s * (size.x * 0.5f - 0.08f), frontH * 0.5f, -size.y * 0.5f + 0.08f),
                                    new Vector3(0.14f, frontH, 0.14f), Quaternion.identity, wood);
                MachineBuilder.Part(stall, PrimitiveType.Cube, "PostBack", new Vector3(s * (size.x * 0.5f - 0.08f), backH * 0.5f, size.y * 0.5f - 0.08f),
                                    new Vector3(0.14f, backH, 0.14f), Quaternion.identity, wood);
            }
            // Arka raf (satıcının arkası) ve çizgili tente.
            MachineBuilder.Part(stall, PrimitiveType.Cube, "BackShelf", new Vector3(0f, 1.1f, size.y * 0.5f - 0.12f), new Vector3(size.x - 0.3f, 0.08f, 0.3f), Quaternion.identity, plank);
            Transform roof = new GameObject("Awning").transform;
            roof.SetParent(stall, false);
            roof.localPosition = new Vector3(0f, (frontH + backH) * 0.5f + 0.04f, 0f);
            roof.localRotation = Quaternion.Euler(-Mathf.Atan2(backH - frontH, size.y) * Mathf.Rad2Deg, 0f, 0f);
            int strips = 7;
            float stripW = (size.x + 0.5f) / strips;
            for (int i = 0; i < strips; i++)
            {
                float x = -size.x * 0.5f - 0.25f + stripW * (i + 0.5f);
                Material m = i % 2 == 0 ? red : cream;
                MachineBuilder.Part(roof, PrimitiveType.Cube, "Strip", new Vector3(x, 0f, 0f), new Vector3(stripW + 0.01f, 0.06f, size.y + 0.7f), Quaternion.identity, m);
                MachineBuilder.Part(roof, PrimitiveType.Cube, "Valance", new Vector3(x, -0.16f, -size.y * 0.5f - 0.35f), new Vector3(stripW + 0.01f, 0.32f, 0.04f), Quaternion.identity, m);
            }
            // Tezgahın üstünde birkaç ürün (meyve kasası, şişeler).
            AddProp(stall, "CrateBottles", new Vector3(-0.8f, counterH + 0.08f, -size.y * 0.5f + 0.45f), 10f, 0.55f);
            AddProp(stall, "Bucket", new Vector3(0.75f, counterH + 0.08f, -size.y * 0.5f + 0.45f), 0f, 0.8f);
            AddProp(stall, "Bottle", new Vector3(1.2f, counterH + 0.08f, -size.y * 0.5f + 0.3f), 0f, 1f);
            BuilderUtil.SetVector2(stall.gameObject.AddComponent<NavObstacle>(), "size", size);

            // Önde yanlarda kasa ve variller (alanın dışında, kendi küçük engelleriyle).
            AddSolidProp(root.transform, "Barrel", new Vector3(-size.x * 0.5f - 0.35f, 0f, MapLayout.MarketStallDepth - size.y * 0.5f - 0.1f), 15f, 0.6f);
            AddSolidProp(root.transform, "Crate", new Vector3(size.x * 0.5f + 0.4f, 0f, MapLayout.MarketStallDepth - size.y * 0.5f - 0.05f), -20f, 0.7f);
            AddSolidProp(root.transform, "Barrel", new Vector3(size.x * 0.5f + 0.5f, 0f, MapLayout.MarketStallDepth + 0.15f), 50f, 0.6f);

            // Satıcı: tezgahın arkasında, el sallar.
            Transform merchant = new GameObject("Merchant").transform;
            merchant.SetParent(stall, false);
            merchant.localPosition = new Vector3(-0.15f, 0f, -0.1f);
            merchant.localRotation = Quaternion.Euler(0f, 180f, 0f);
            GameObject model = PrefabFactory.CreateModel(ModelCatalog.Merchant, merchant);
            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = CharacterAnimation.GetSingleClip(ModelCatalog.Merchant.Path, "Wave", "Merchant_Wave");
            animator.applyRootMotion = false;
            PrefabFactory.AddBlobShadow(merchant, 1.1f);

            Transform drop = new GameObject("DropPoint").transform;
            drop.SetParent(root.transform, false);
            drop.localPosition = new Vector3(0f, counterH + 0.2f, MapLayout.MarketStallDepth - size.y * 0.5f + 0.4f);

            PrefabFactory.CreateBoardSign(stall, new Vector3(0f, backH + 0.75f, 0f), "Pazar");
            BuilderUtil.SetShadows(stall.gameObject, true);

            Market market = root.AddComponent<Market>();
            BuilderUtil.SetField(market, "config", config);
            BuilderUtil.SetField(market, "dropPoint", drop);
            return PrefabFactorySave(root, Folder + "/World/Market.prefab");
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
