// PrototypeSceneBuilder.cs
// Editör menü komutu: Tools > Kayıp Ada > Prototip Sahnesini Kur
// Sahneyi her seferinde sıfırdan kurar ve Assets/_Game/Scenes/Prototype.unity olarak kaydeder.
// Ayar dosyalarını (GameConfig, kaynak türleri) SADECE yoksa oluşturur; senin değişikliklerin korunur.
// Sahneye yeni nesne eklenecekse önce bu dosya güncellenir (her aşamada).

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace KayipAda.EditorTools
{
    public static class PrototypeSceneBuilder
    {
        // ---------------------------------------------------------------- Dosya yolları
        private const string Root = "Assets/_Game";
        private const string SpritePath = Root + "/Art/WhiteSquare.png";
        private const string ItemPrefabPath = Root + "/Prefabs/ResourceItem.prefab";
        private const string ConfigPath = Root + "/Data/GameConfig.asset";
        private const string ResourceTypesFolder = Root + "/Data/ResourceTypes";
        private const string ScenePath = Root + "/Scenes/Prototype.unity";

        // ---------------------------------------------------------------- Sahne düzeni
        // Bunlar oynanış ayarı değil, haritanın yerleşimi ve görünümüdür.
        private static readonly Vector2 PlayerStart = new Vector2(0f, -2f);
        // Ağaçlar oyuncunun başlangıcı etrafında, ilk ekranda tamamen görünecek şekilde:
        // dar (20:9) telefonlarda da yanlardan taşmaz, üstteki para panelinin altına girmez.
        private static readonly Vector2[] TreePositions = { new Vector2(-2.6f, 1f), new Vector2(0f, 4f), new Vector2(2.6f, 1f) };
        private static readonly Vector2 SellZonePosition = new Vector2(0f, -6f);
        private static readonly Vector2 SellZoneSize = new Vector2(3f, 3f);
        private static readonly Vector2 NodeTriggerSize = new Vector2(2.6f, 2.8f);
        private const float PlayerSize = 0.9f;
        private static readonly Vector3 ItemSize = new Vector3(0.7f, 0.2f, 1f);

        private static readonly Color SeaColor = new Color(0.20f, 0.55f, 0.80f);
        private static readonly Color SandColor = new Color(0.93f, 0.84f, 0.60f);
        private static readonly Color PlayerColor = new Color(0.95f, 0.45f, 0.25f);
        private static readonly Color TrunkColor = new Color(0.45f, 0.30f, 0.15f);
        private static readonly Color LeafColor = new Color(0.25f, 0.65f, 0.30f);
        private static readonly Color SellColor = new Color(1.00f, 0.80f, 0.20f, 0.6f);
        private static readonly Color WoodColor = new Color(0.60f, 0.38f, 0.18f);

        private static Sprite square;

        [MenuItem("Tools/Kayıp Ada/Prototip Sahnesini Kur")]
        public static void Build()
        {
            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sor.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 1) Dosyalar: klasörler, sprite, ayar dosyaları, prefab
            CreateFolders();
            CreateWhiteSquareSprite();
            CreateConfigIfMissing();
            CreateResourceTypeIfMissing("Wood", "wood", "Odun", WoodColor, 5);
            CreateItemPrefab();

            // 2) Temiz bir sahne aç
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Önemli: Yeni sahne açılınca Unity bellekteki varlık referanslarını boşaltabilir.
            // Bu yüzden tüm dosyaları yeni sahneden SONRA diskten yüklüyoruz.
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            ResourceType wood = AssetDatabase.LoadAssetAtPath<ResourceType>(ResourceTypesFolder + "/Wood.asset");
            ResourceItem itemPrefab = AssetDatabase.LoadAssetAtPath<ResourceItem>(ItemPrefabPath);
            if (square == null || config == null || wood == null || itemPrefab == null)
            {
                Debug.LogError("Kayıp Ada: Gerekli dosyalardan biri yüklenemedi. Komutu tekrar çalıştırın.");
                return;
            }

            // 3) Dünya
            CreateLight();
            Vector2 islandSize = (config.MoveBounds + Vector2.one * PlayerSize) * 2f;
            CreateBox("Island", null, Vector2.zero, islandSize, SandColor, SortOrder.Ground);

            GameObject managers = new GameObject("Managers");
            SetField(managers.AddComponent<CurrencyManager>(), "config", config);

            // 4) Arayüz (oyuncu joystick'e ihtiyaç duyduğu için önce kurulur)
            VirtualJoystick joystick = CreateUI(config);

            // 5) Oyuncu ve kamera
            GameObject player = CreatePlayer(config, joystick);
            CreateCamera(config, player.transform);

            // 6) Kaynak noktaları ve satış alanı
            for (int i = 0; i < TreePositions.Length; i++)
            {
                CreateTree("Tree " + (i + 1), TreePositions[i], config, wood, itemPrefab);
            }
            CreateSellZone(config);

            // 7) Kaydet ve proje ayarları
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            Debug.Log("Kayıp Ada: Prototip sahnesi kuruldu → " + ScenePath);
        }

        // ================================================================ Dosyalar

        private static void CreateFolders()
        {
            foreach (string folder in new[] { "Art", "Prefabs", "Data", "Data/ResourceTypes", "Scenes" })
            {
                string path = Root + "/" + folder;
                if (AssetDatabase.IsValidFolder(path)) continue;

                int slash = path.LastIndexOf('/');
                AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
            }
        }

        // 1x1 birimlik beyaz kare sprite üretir; tüm kutular bunu renklendirerek kullanır.
        private static void CreateWhiteSquareSprite()
        {
            const int size = 32;

            if (!System.IO.File.Exists(SpritePath))
            {
                Texture2D tex = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                System.IO.File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(SpritePath);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size; // 32 piksel = 1 birim
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        private static void CreateConfigIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath) != null) return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfig>(), ConfigPath);
            AssetDatabase.SaveAssets();
        }

        private static void CreateResourceTypeIfMissing(string fileName, string id, string displayName, Color color, int sellPrice)
        {
            string path = ResourceTypesFolder + "/" + fileName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<ResourceType>(path) != null) return;

            ResourceType type = ScriptableObject.CreateInstance<ResourceType>();
            type.EditorSetup(id, displayName, color, sellPrice);
            AssetDatabase.CreateAsset(type, path);
            AssetDatabase.SaveAssets();
        }

        // Tüm kaynak türlerinin kullandığı tek prefab. Rengi oyunda ResourceType'tan gelir.
        private static void CreateItemPrefab()
        {
            GameObject item = new GameObject("ResourceItem");
            item.transform.localScale = ItemSize;
            SpriteRenderer sr = item.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            item.AddComponent<ResourceItem>();

            PrefabUtility.SaveAsPrefabAsset(item, ItemPrefabPath);
            Object.DestroyImmediate(item);
        }

        // ================================================================ Dünya nesneleri

        private static void CreateLight()
        {
            // URP 2D'de sprite'ların görünmesi için genel (global) ışık.
            GameObject go = new GameObject("Global Light 2D");
            Light2D light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
        }

        private static void CreateCamera(GameConfig config, Transform target)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(target.position.x, target.position.y + config.CameraOffsetY, -10f);

            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = config.CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SeaColor;

            CameraFollow follow = go.AddComponent<CameraFollow>();
            SetField(follow, "config", config);
            SetField(follow, "target", target);
        }

        private static GameObject CreatePlayer(GameConfig config, VirtualJoystick joystick)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = PlayerStart;

            CreateBox("Body", player.transform, Vector2.zero, Vector2.one * PlayerSize, PlayerColor, SortOrder.Character);

            // Fizik: hareket için Kinematic gövde, trigger alanlarını algılamak için collider.
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            player.AddComponent<CircleCollider2D>().radius = PlayerSize / 2f;

            // Sırttaki yığının başlangıç noktası (gövdenin hemen üstü).
            Transform stackRoot = new GameObject("StackRoot").transform;
            stackRoot.SetParent(player.transform, false);
            stackRoot.localPosition = new Vector3(0f, PlayerSize * 0.66f, 0f);

            CarryStack stack = player.AddComponent<CarryStack>();
            SetField(stack, "config", config);
            SetField(stack, "stackRoot", stackRoot);

            PlayerController controller = player.AddComponent<PlayerController>();
            SetField(controller, "config", config);
            SetField(controller, "joystick", joystick);

            return player;
        }

        private static void CreateTree(string name, Vector2 position, GameConfig config, ResourceType type, ResourceItem itemPrefab)
        {
            GameObject tree = new GameObject(name);
            tree.transform.position = position;

            CreateBox("Trunk", tree.transform, new Vector2(0f, -0.6f), new Vector2(0.4f, 0.8f), TrunkColor, SortOrder.NodeBase);
            GameObject crown = CreateBox("Crown", tree.transform, new Vector2(0f, 0.2f), new Vector2(1.4f, 1.4f), LeafColor, SortOrder.NodeTop);

            // Karakter bu alana girince kaynak almaya başlar.
            BoxCollider2D area = tree.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = NodeTriggerSize;

            ResourceNode node = tree.AddComponent<ResourceNode>();
            SetField(node, "config", config);
            SetField(node, "resourceType", type);
            SetField(node, "itemPrefab", itemPrefab);
            SetField(node, "visual", crown.transform);
        }

        private static void CreateSellZone(GameConfig config)
        {
            GameObject zone = new GameObject("SellZone");
            zone.transform.position = SellZonePosition;

            CreateBox("Visual", zone.transform, Vector2.zero, SellZoneSize, SellColor, SortOrder.Zone);
            CreateWorldLabel("SAT", zone.transform);

            BoxCollider2D area = zone.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = SellZoneSize;

            SetField(zone.AddComponent<SellZone>(), "config", config);
        }

        // Renkli kare sprite'lı basit bir kutu oluşturur.
        private static GameObject CreateBox(string name, Transform parent, Vector2 localPos, Vector2 size, Color color, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        // Dünya içinde duran küçük bir yazı (World Space Canvas).
        private static void CreateWorldLabel(string text, Transform parent)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.01f; // 100 UI birimi = 1 dünya birimi

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = SortOrder.WorldLabel;
            ((RectTransform)go.transform).sizeDelta = new Vector2(300f, 120f);

            Text label = CreateText(go.transform, text, 90);
            label.color = new Color(0.35f, 0.25f, 0.05f);
            Stretch(label.rectTransform);
        }

        // ================================================================ Arayüz

        private static VirtualJoystick CreateUI(GameConfig config)
        {
            // Dokunma/fare olaylarını UI'ya ileten EventSystem (yeni Input System ile).
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // Telefon ekranına göre ölçeklenen Canvas (1080x1920 referans).
            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            VirtualJoystick joystick = CreateJoystick(canvasGo.transform, config); // önce: en arkada kalsın
            CreateCoinsPanel(canvasGo.transform);
            return joystick;
        }

        private static VirtualJoystick CreateJoystick(Transform canvas, GameConfig config)
        {
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // Ekranın tamamını kaplayan görünmez dokunma alanı.
            Image area = CreateImage("JoystickArea", canvas, null, new Color(1f, 1f, 1f, 0f));
            Stretch(area.rectTransform);

            // Halka boyutu joystick menziline göre ayarlanır.
            float ringSize = config.JoystickRange * 2.5f;
            Image bg = CreateImage("Background", area.transform, circle, new Color(1f, 1f, 1f, 0.35f));
            bg.rectTransform.sizeDelta = Vector2.one * ringSize;
            bg.raycastTarget = false;

            Image handle = CreateImage("Handle", bg.transform, circle, new Color(1f, 1f, 1f, 0.85f));
            handle.rectTransform.sizeDelta = Vector2.one * ringSize * 0.43f;
            handle.raycastTarget = false;

            VirtualJoystick joystick = area.gameObject.AddComponent<VirtualJoystick>();
            SetField(joystick, "config", config);
            SetField(joystick, "background", bg.rectTransform);
            SetField(joystick, "handle", handle.rectTransform);
            return joystick;
        }

        private static void CreateCoinsPanel(Transform canvas)
        {
            // Üst ortada yarı saydam koyu panel.
            Image panel = CreateImage("CoinsPanel", canvas, null, new Color(0f, 0f, 0f, 0.45f));
            panel.raycastTarget = false;
            RectTransform rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -60f);
            rt.sizeDelta = new Vector2(360f, 110f);

            // Sarı "para" kutucuğu.
            Image icon = CreateImage("Icon", panel.transform, null, new Color(1f, 0.82f, 0.2f));
            icon.raycastTarget = false;
            icon.rectTransform.anchoredPosition = new Vector2(-110f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(64f, 64f);

            Text text = CreateText(panel.transform, "0", 64);
            text.alignment = TextAnchor.MiddleLeft;
            text.rectTransform.anchoredPosition = new Vector2(50f, 0f);
            text.rectTransform.sizeDelta = new Vector2(220f, 100f);

            SetField(panel.gameObject.AddComponent<CurrencyUI>(), "coinsText", text);
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string value, int fontSize)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        // RectTransform'u ebeveyninin tamamını kaplayacak şekilde ayarlar.
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ================================================================ Yardımcı

        // [SerializeField] private alanlara editörden değer atar (Inspector'da sürükle-bırak yapmak gibi).
        private static void SetField(Object target, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"Kayıp Ada: {target.GetType().Name} içinde '{fieldName}' alanı bulunamadı.");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
