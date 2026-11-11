// PrototypeSceneBuilder.cs
// Editör menü komutu: Tools > Kayıp Ada > Prototip Sahnesini Kur
// Görselsiz (renkli kutulu) prototip sahnesini sıfırdan oluşturur:
// kamera, ada, oyuncu, 3 ağaç, satış alanı, joystick ve para göstergesi.
// Sahne Assets/_Game/Scenes/Prototype.unity olarak kaydedilir. Komut tekrar çalıştırılabilir.

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
        private const string Root = "Assets/_Game";
        private const string SpritePath = Root + "/Art/WhiteSquare.png";
        private const string WoodPrefabPath = Root + "/Prefabs/Wood.prefab";
        private const string ScenePath = Root + "/Scenes/Prototype.unity";

        // Renk paleti
        private static readonly Color SeaColor = new Color(0.20f, 0.55f, 0.80f);
        private static readonly Color SandColor = new Color(0.93f, 0.84f, 0.60f);
        private static readonly Color PlayerColor = new Color(0.95f, 0.45f, 0.25f);
        private static readonly Color TrunkColor = new Color(0.45f, 0.30f, 0.15f);
        private static readonly Color LeafColor = new Color(0.25f, 0.65f, 0.30f);
        private static readonly Color WoodColor = new Color(0.60f, 0.38f, 0.18f);
        private static readonly Color SellColor = new Color(1.00f, 0.80f, 0.20f, 0.6f);

        private static Sprite square;

        [MenuItem("Tools/Kayıp Ada/Prototip Sahnesini Kur")]
        public static void Build()
        {
            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sor.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateFolders();
            square = CreateWhiteSquareSprite();
            CreateWoodPrefab();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Önemli: Yeni sahne açılınca Unity bellekteki varlık referanslarını boşaltabilir.
            // Bu yüzden sprite ve prefab'ı yeni sahneden SONRA diskten tekrar yüklüyoruz.
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            ResourceItem woodPrefab = AssetDatabase.LoadAssetAtPath<ResourceItem>(WoodPrefabPath);
            if (square == null || woodPrefab == null)
            {
                Debug.LogError("Kayıp Ada: Sprite veya Wood prefab'ı yüklenemedi. Komutu tekrar çalıştırın.");
                return;
            }

            // --- Dünya ---
            CreateLight();
            CreateBox("Island", null, Vector2.zero, new Vector2(16f, 24f), SandColor, 0);
            new GameObject("MoneyManager").AddComponent<MoneyManager>();

            // --- Arayüz (oyuncu joystick'e ihtiyaç duyduğu için önce kurulur) ---
            VirtualJoystick joystick = CreateUI();

            // --- Oyuncu ve kamera ---
            GameObject player = CreatePlayer(joystick);
            CreateCamera(player.transform);

            // --- Ağaçlar ve satış alanı ---
            CreateTree("Tree 1", new Vector2(-3.5f, 5f), woodPrefab);
            CreateTree("Tree 2", new Vector2(0f, 7.5f), woodPrefab);
            CreateTree("Tree 3", new Vector2(3.5f, 5f), woodPrefab);
            CreateSellZone(new Vector2(0f, -6f));

            // --- Kaydet ve ayarlar ---
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            Debug.Log("Kayıp Ada: Prototip sahnesi kuruldu → " + ScenePath);
        }

        // ---------------------------------------------------------------- Varlıklar

        private static void CreateFolders()
        {
            foreach (string folder in new[] { "Art", "Prefabs", "Scenes" })
            {
                if (!AssetDatabase.IsValidFolder(Root + "/" + folder))
                {
                    AssetDatabase.CreateFolder(Root, folder);
                }
            }
        }

        // 1x1 birimlik beyaz kare sprite üretir; tüm kutular bunu renklendirerek kullanır.
        private static Sprite CreateWhiteSquareSprite()
        {
            if (!System.IO.File.Exists(SpritePath))
            {
                const int size = 32;
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
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static void CreateWoodPrefab()
        {
            GameObject wood = new GameObject("Wood");
            wood.transform.localScale = new Vector3(0.7f, 0.2f, 1f);
            SpriteRenderer sr = wood.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = WoodColor;
            wood.AddComponent<ResourceItem>();

            PrefabUtility.SaveAsPrefabAsset(wood, WoodPrefabPath);
            Object.DestroyImmediate(wood);
        }

        // ---------------------------------------------------------------- Dünya nesneleri

        private static void CreateLight()
        {
            // URP 2D'de sprite'ların görünmesi için genel (global) ışık.
            GameObject go = new GameObject("Global Light 2D");
            Light2D light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        private static void CreateCamera(Transform target)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(target.position.x, target.position.y, -10f);

            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SeaColor;

            CameraFollow follow = go.AddComponent<CameraFollow>();
            SetField(follow, "target", target);
        }

        private static GameObject CreatePlayer(VirtualJoystick joystick)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, -2f, 0f);

            CreateBox("Body", player.transform, Vector2.zero, new Vector2(0.9f, 0.9f), PlayerColor, 10);

            // Fizik: hareket için Kinematic gövde, trigger'ları algılamak için collider.
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            player.AddComponent<CircleCollider2D>().radius = 0.45f;

            // Sırttaki yığının başlangıç noktası.
            Transform stackRoot = new GameObject("StackRoot").transform;
            stackRoot.SetParent(player.transform, false);
            stackRoot.localPosition = new Vector3(0f, 0.6f, 0f);

            ResourceStack stack = player.AddComponent<ResourceStack>();
            SetField(stack, "stackRoot", stackRoot);

            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            SetField(movement, "joystick", joystick);

            return player;
        }

        private static void CreateTree(string name, Vector2 position, ResourceItem woodPrefab)
        {
            GameObject tree = new GameObject(name);
            tree.transform.position = position;

            CreateBox("Trunk", tree.transform, new Vector2(0f, -0.6f), new Vector2(0.4f, 0.8f), TrunkColor, 2);
            GameObject crown = CreateBox("Crown", tree.transform, new Vector2(0f, 0.2f), new Vector2(1.4f, 1.4f), LeafColor, 3);

            // Oyuncu bu alana girince odun almaya başlar.
            BoxCollider2D area = tree.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(2.6f, 2.8f);

            ResourceTree resourceTree = tree.AddComponent<ResourceTree>();
            SetField(resourceTree, "woodPrefab", woodPrefab);
            SetField(resourceTree, "visual", crown.transform);
        }

        private static void CreateSellZone(Vector2 position)
        {
            GameObject zone = new GameObject("SellZone");
            zone.transform.position = position;

            Vector2 size = new Vector2(3f, 3f);
            CreateBox("Visual", zone.transform, Vector2.zero, size, SellColor, 1);
            CreateWorldLabel("SAT", zone.transform);

            BoxCollider2D area = zone.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = size;

            zone.AddComponent<SellZone>();
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
            go.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            ((RectTransform)go.transform).sizeDelta = new Vector2(300f, 120f);

            Text label = CreateText(go.transform, text, 90);
            label.color = new Color(0.35f, 0.25f, 0.05f);
            Stretch(label.rectTransform);
        }

        // ---------------------------------------------------------------- Arayüz

        private static VirtualJoystick CreateUI()
        {
            // Dokunma/fare olaylarını UI'ya ileten EventSystem (yeni Input System ile).
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // Telefon ekranına göre ölçeklenen Canvas.
            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            VirtualJoystick joystick = CreateJoystick(canvasGo.transform); // önce: en arkada kalsın
            CreateMoneyPanel(canvasGo.transform);
            return joystick;
        }

        private static VirtualJoystick CreateJoystick(Transform canvas)
        {
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // Ekranın tamamını kaplayan görünmez dokunma alanı.
            Image area = CreateImage("JoystickArea", canvas, null, new Color(1f, 1f, 1f, 0f));
            Stretch(area.rectTransform);

            Image bg = CreateImage("Background", area.transform, circle, new Color(1f, 1f, 1f, 0.35f));
            bg.rectTransform.sizeDelta = new Vector2(280f, 280f);
            bg.raycastTarget = false;

            Image handle = CreateImage("Handle", bg.transform, circle, new Color(1f, 1f, 1f, 0.85f));
            handle.rectTransform.sizeDelta = new Vector2(120f, 120f);
            handle.raycastTarget = false;

            VirtualJoystick joystick = area.gameObject.AddComponent<VirtualJoystick>();
            SetField(joystick, "background", bg.rectTransform);
            SetField(joystick, "handle", handle.rectTransform);
            return joystick;
        }

        private static void CreateMoneyPanel(Transform canvas)
        {
            // Üst ortada yarı saydam koyu panel.
            Image panel = CreateImage("MoneyPanel", canvas, null, new Color(0f, 0f, 0f, 0.45f));
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

            MoneyUI moneyUI = panel.gameObject.AddComponent<MoneyUI>();
            SetField(moneyUI, "moneyText", text);
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

        // ---------------------------------------------------------------- Yardımcı

        // [SerializeField] private alanlara editörden değer atar (Inspector'da sürükle-bırak yapmak gibi).
        private static void SetField(Object target, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
