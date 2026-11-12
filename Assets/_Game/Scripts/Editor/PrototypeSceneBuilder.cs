// PrototypeSceneBuilder.cs
// Editör menü komutu: Tools > Kayıp Ada > Prototip Sahnesini Kur
// Sahneyi her seferinde sıfırdan kurar ve Assets/_Game/Scenes/Prototype.unity olarak kaydeder.
// Ayar dosyalarını (Data/ altındakiler) SADECE yoksa oluşturur; senin değişikliklerin korunur.
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
        private const string CoinPrefabPath = Root + "/Prefabs/Coin.prefab";
        private const string ConfigPath = Root + "/Data/GameConfig.asset";
        private const string ResourceTypesFolder = Root + "/Data/ResourceTypes";
        private const string NodeTypesFolder = Root + "/Data/NodeTypes";
        private const string AreasFolder = Root + "/Data/Areas";
        private const string ScenePath = Root + "/Scenes/Prototype.unity";

        // ---------------------------------------------------------------- Harita yerleşimi
        // Bunlar oynanış ayarı değil, haritanın yerleşimi ve görünümüdür.
        //
        //            ~~~~ deniz ~~~~
        //               [iskele]            y 13..17 (denize uzanan tahta)
        //   ┌──────── İSKELE ALANI ────────┐ y 10.8..12.8 (gemi için, ileride)
        //   │  KAYALIK      │ MEYVE BAHÇESİ │ y 4..10 (başta çitle kapalı)
        //   └───────────────┴──────────────┘
        //      [kare 1]          [kare 2]     y 2.9
        //    ağaç      ağaç      ağaç         BAŞLANGIÇ SAHİLİ
        //             oyuncu                  y -3
        //             PAZAR                   y -7
        //
        // Kamera oyuncuyu takip eder; harita tek ekrana sığmak zorunda değil. Başlangıç ekranında
        // (kamera y≈-1, görünür y≈-9..7) ağaçlar, Pazar ve 1. kare tamamen görünür.
        // Dar (20:9) telefonlarda yarım ekran genişliği ~3.6 birim → başlangıç nesneleri x = -3.3..3.3 içinde.
        private static readonly Rect IslandRect = new Rect(-6f, -10f, 12f, 23f);  // x -6..6, y -10..13
        private const float WalkMargin = 0.5f;                                      // oyuncu adanın kenarına kadar değil
        private static readonly Vector2 PlayerStart = new Vector2(0f, -3f);

        // Başlangıç sahili (her zaman açık)
        private static readonly Vector2[] TreePositions = { new Vector2(-2.6f, 0f), new Vector2(0f, 1.2f), new Vector2(2.6f, 0f) };
        private static readonly Vector2 MarketPosition = new Vector2(0f, -7f);
        private static readonly Vector2 MarketSize = new Vector2(3f, 3f);

        // Açılabilir bölgeler
        private static readonly Rect RockyRect = new Rect(-5.6f, 4f, 5.2f, 6f);    // x -5.6..-0.4
        private static readonly Rect OrchardRect = new Rect(0.4f, 4f, 5.2f, 6f);   // x 0.4..5.6
        private static readonly Vector2[] RockPositions = { new Vector2(-4f, 5.8f), new Vector2(-2f, 8.2f) };
        private static readonly Vector2[] BushPositions = { new Vector2(2f, 5.8f), new Vector2(4f, 8.2f) };
        private static readonly Vector2 RockyPadPosition = new Vector2(-2.4f, 2.9f);
        private static readonly Vector2 OrchardPadPosition = new Vector2(2.4f, 2.9f);
        private static readonly Vector2 PadSize = new Vector2(1.8f, 1.8f);
        private const float FenceThickness = 0.15f;

        // İskele (ileride gemi burada inşa edilecek)
        private static readonly Rect DockAreaRect = new Rect(-2.5f, 10.8f, 5f, 2f);
        private static readonly Rect PierRect = new Rect(-1f, 13f, 2f, 4f);

        private static readonly Vector2 NodeTriggerSize = new Vector2(2.2f, 2.2f); // komşu noktaların alanları çakışmaz
        private const float PlayerSize = 0.9f;
        private static readonly Vector3 ItemSize = new Vector3(0.7f, 0.2f, 1f);
        private const float CoinSize = 0.3f;
        private const float TopBarHeight = 150f; // UI birimi (1080x1920 referansında)

        // Renkler
        private static readonly Color SeaColor = new Color(0.20f, 0.55f, 0.80f);
        private static readonly Color SandColor = new Color(0.93f, 0.84f, 0.60f);
        private static readonly Color RockyGroundColor = new Color(0.78f, 0.74f, 0.66f);
        private static readonly Color OrchardGroundColor = new Color(0.66f, 0.80f, 0.48f);
        private static readonly Color DockColor = new Color(0.55f, 0.40f, 0.25f);
        private static readonly Color FenceColor = new Color(0.40f, 0.26f, 0.12f);
        private static readonly Color PlayerColor = new Color(0.85f, 0.20f, 0.35f); // meyveyle karışmasın diye kırmızımsı
        private static readonly Color TrunkColor = new Color(0.45f, 0.30f, 0.15f);
        private static readonly Color LeafColor = new Color(0.25f, 0.65f, 0.30f);
        private static readonly Color RockDarkColor = new Color(0.45f, 0.45f, 0.48f);
        private static readonly Color RockLightColor = new Color(0.65f, 0.65f, 0.68f);
        private static readonly Color BushColor = new Color(0.18f, 0.50f, 0.25f);
        private static readonly Color MarketColor = new Color(1.00f, 0.80f, 0.20f, 0.6f);
        private static readonly Color PadColor = new Color(1.00f, 1.00f, 1.00f, 0.55f);
        private static readonly Color CoinColor = new Color(1.00f, 0.82f, 0.20f);
        private static readonly Color LabelColor = new Color(0.30f, 0.20f, 0.05f);
        private static readonly Color TopBarColor = new Color(0.12f, 0.16f, 0.22f);

        // Kaynak türlerinin ilk değerleri (dosya zaten varsa dokunulmaz; sonradan Inspector'dan değiştir).
        private static readonly Color WoodColor = new Color(0.60f, 0.38f, 0.18f);
        private static readonly Color StoneColor = new Color(0.62f, 0.62f, 0.65f);
        private static readonly Color FruitColor = new Color(1.00f, 0.55f, 0.10f);

        private static Sprite square;

        [MenuItem("Tools/Kayıp Ada/Prototip Sahnesini Kur")]
        public static void Build()
        {
            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sor.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 1) Dosyalar: klasörler, sprite, ayar dosyaları, prefab'lar
            CreateFolders();
            CreateWhiteSquareSprite();
            CreateConfigIfMissing();
            //                          dosya     kimlik   ad       renk        fiyat
            CreateResourceTypeIfMissing("Wood",  "wood",  "Odun",  WoodColor,  5);
            CreateResourceTypeIfMissing("Stone", "stone", "Taş",   StoneColor, 8);
            CreateResourceTypeIfMissing("Fruit", "fruit", "Meyve", FruitColor, 4);
            //                      dosya        kaynak   stok  toplama aralığı  yeniden dolma (sn)
            CreateNodeTypeIfMissing("Tree",      "Wood",  8,    0.35f,           10f);
            CreateNodeTypeIfMissing("Rock",      "Stone", 6,    0.6f,            15f); // ağaçtan yavaş
            CreateNodeTypeIfMissing("FruitBush", "Fruit", 5,    0.4f,            12f);
            //                  dosya      kimlik     ad               fiyat
            CreateAreaIfMissing("Rocky",   "rocky",   "Kayalık",       60);
            CreateAreaIfMissing("Orchard", "orchard", "Meyve Bahçesi", 150);
            CreateItemPrefab();
            CreateCoinPrefab();

            // 2) Temiz bir sahne aç
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Önemli: Yeni sahne açılınca Unity bellekteki varlık referanslarını boşaltabilir.
            // Bu yüzden tüm dosyaları yeni sahneden SONRA diskten yüklüyoruz.
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            ResourceNodeType treeType = Load<ResourceNodeType>(NodeTypesFolder, "Tree");
            ResourceNodeType rockType = Load<ResourceNodeType>(NodeTypesFolder, "Rock");
            ResourceNodeType bushType = Load<ResourceNodeType>(NodeTypesFolder, "FruitBush");
            AreaDefinition rocky = Load<AreaDefinition>(AreasFolder, "Rocky");
            AreaDefinition orchard = Load<AreaDefinition>(AreasFolder, "Orchard");
            ResourceItem itemPrefab = AssetDatabase.LoadAssetAtPath<ResourceItem>(ItemPrefabPath);
            FlyingVisual coinPrefab = AssetDatabase.LoadAssetAtPath<FlyingVisual>(CoinPrefabPath);
            if (square == null || config == null || treeType == null || rockType == null || bushType == null
                || rocky == null || orchard == null || itemPrefab == null || coinPrefab == null)
            {
                Debug.LogError("Kayıp Ada: Gerekli dosyalardan biri yüklenemedi. Komutu tekrar çalıştırın.");
                return;
            }

            // 3) Yöneticiler ve ışık
            CreateLight();
            GameObject managers = new GameObject("Managers");
            SetField(managers.AddComponent<CurrencyManager>(), "config", config);
            managers.AddComponent<AreaManager>();

            // 4) Arayüz (oyuncu joystick'e, kamera üst çubuğa ihtiyaç duyduğu için önce kurulur)
            VirtualJoystick joystick = CreateUI(config, out RectTransform topBar);

            // 5) Ada zemini
            CreateBox("Island", null, IslandRect.center, IslandRect.size, SandColor, SortOrder.Ground);

            // 6) Başlangıç sahili: ağaçlar ve Pazar
            Transform beach = new GameObject("Beach").transform;
            for (int i = 0; i < TreePositions.Length; i++)
            {
                CreateNode("Tree " + (i + 1), beach, TreePositions[i], treeType, config, itemPrefab, BuildTreeVisual);
            }
            CreateMarket(beach, config);

            // 7) Açılabilir bölgeler ve açma kareleri
            Transform rockyContent = CreateArea("Area Rocky", rocky, RockyRect, RockyGroundColor);
            for (int i = 0; i < RockPositions.Length; i++)
            {
                CreateNode("Rock " + (i + 1), rockyContent, RockPositions[i], rockType, config, itemPrefab, BuildRockVisual);
            }

            Transform orchardContent = CreateArea("Area Orchard", orchard, OrchardRect, OrchardGroundColor);
            for (int i = 0; i < BushPositions.Length; i++)
            {
                CreateNode("FruitBush " + (i + 1), orchardContent, BushPositions[i], bushType, config, itemPrefab, BuildBushVisual);
            }

            // 1. kare kayalığı açar; 2. kare kayalık açılınca görünür ve meyve bahçesini açar.
            CreateUnlockPad("Pad Rocky", RockyPadPosition, rocky, null, config, coinPrefab);
            CreateUnlockPad("Pad Orchard", OrchardPadPosition, orchard, rocky, config, coinPrefab);

            // 8) İskele alanı (şimdilik boş)
            CreateDock();

            // 9) Oyuncu ve kamera
            Rect walkArea = new Rect(IslandRect.xMin + WalkMargin, IslandRect.yMin + WalkMargin,
                                     IslandRect.width - WalkMargin * 2f, IslandRect.height - WalkMargin * 2f);
            GameObject player = CreatePlayer(config, joystick, walkArea);
            CreateCamera(config, player.transform, topBar);

            // 10) Kaydet ve proje ayarları
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            Debug.Log("Kayıp Ada: Prototip sahnesi kuruldu → " + ScenePath);
        }

        // ================================================================ Dosyalar

        private static void CreateFolders()
        {
            string[] folders = { "Art", "Prefabs", "Data", "Data/ResourceTypes", "Data/NodeTypes", "Data/Areas", "Scenes" };
            foreach (string folder in folders)
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

        private static void CreateNodeTypeIfMissing(string fileName, string resourceFile, int stock, float gatherInterval, float regrowTime)
        {
            string path = NodeTypesFolder + "/" + fileName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<ResourceNodeType>(path) != null) return;

            ResourceNodeType nodeType = ScriptableObject.CreateInstance<ResourceNodeType>();
            nodeType.EditorSetup(Load<ResourceType>(ResourceTypesFolder, resourceFile), stock, gatherInterval, regrowTime);
            AssetDatabase.CreateAsset(nodeType, path);
            AssetDatabase.SaveAssets();
        }

        private static void CreateAreaIfMissing(string fileName, string id, string displayName, int price)
        {
            string path = AreasFolder + "/" + fileName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<AreaDefinition>(path) != null) return;

            AreaDefinition area = ScriptableObject.CreateInstance<AreaDefinition>();
            area.EditorSetup(id, displayName, price);
            AssetDatabase.CreateAsset(area, path);
            AssetDatabase.SaveAssets();
        }

        private static T Load<T>(string folder, string fileName) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(folder + "/" + fileName + ".asset");
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

        // Açma karesine akan küçük para parçası.
        private static void CreateCoinPrefab()
        {
            GameObject coin = new GameObject("Coin");
            coin.transform.localScale = Vector3.one * CoinSize;
            SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            sr.color = CoinColor;
            sr.sortingOrder = SortOrder.Effect;
            coin.AddComponent<FlyingVisual>();

            PrefabUtility.SaveAsPrefabAsset(coin, CoinPrefabPath);
            Object.DestroyImmediate(coin);
        }

        // ================================================================ Dünya nesneleri

        private static void CreateLight()
        {
            // URP 2D'de sprite'ların görünmesi için genel (global) ışık.
            GameObject go = new GameObject("Global Light 2D");
            Light2D light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
        }

        private static void CreateCamera(GameConfig config, Transform target, RectTransform topBar)
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
            SetField(follow, "topBar", topBar);
        }

        private static GameObject CreatePlayer(GameConfig config, VirtualJoystick joystick, Rect walkArea)
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
            SetRect(controller, "moveArea", walkArea);

            return player;
        }

        // Her türlü kaynak noktası (ağaç, kaya, çalı) bununla kurulur; sadece görünümü ve türü farklıdır.
        //   Kök: trigger alanı + ResourceNode
        //   └ Visual: kutulardan oluşan görünüm (küçülme/solma/zıplama buna uygulanır)
        private static void CreateNode(string name, Transform parent, Vector2 position, ResourceNodeType nodeType,
                                       GameConfig config, ResourceItem itemPrefab, System.Action<Transform> buildVisual)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            buildVisual(visual);

            // Karakter bu alana girince kaynak almaya başlar.
            BoxCollider2D area = root.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = NodeTriggerSize;

            ResourceNode node = root.AddComponent<ResourceNode>();
            SetField(node, "config", config);
            SetField(node, "nodeType", nodeType);
            SetField(node, "itemPrefab", itemPrefab);
            SetField(node, "visual", visual);
        }

        private static void BuildTreeVisual(Transform visual)
        {
            CreateBox("Trunk", visual, new Vector2(0f, -0.6f), new Vector2(0.4f, 0.8f), TrunkColor, SortOrder.NodeBase);
            CreateBox("Crown", visual, new Vector2(0f, 0.2f), new Vector2(1.4f, 1.4f), LeafColor, SortOrder.NodeTop);
        }

        private static void BuildRockVisual(Transform visual)
        {
            CreateBox("Base", visual, new Vector2(0f, -0.1f), new Vector2(1.3f, 0.9f), RockDarkColor, SortOrder.NodeBase);
            CreateBox("Top", visual, new Vector2(-0.15f, 0.15f), new Vector2(0.7f, 0.35f), RockLightColor, SortOrder.NodeTop);
        }

        private static void BuildBushVisual(Transform visual)
        {
            CreateBox("Leaves", visual, Vector2.zero, new Vector2(1.1f, 0.9f), BushColor, SortOrder.NodeBase);
            Vector2 fruitSize = new Vector2(0.22f, 0.22f);
            CreateBox("Fruit 1", visual, new Vector2(-0.25f, 0.15f), fruitSize, FruitColor, SortOrder.NodeTop);
            CreateBox("Fruit 2", visual, new Vector2(0.25f, 0.2f), fruitSize, FruitColor, SortOrder.NodeTop);
            CreateBox("Fruit 3", visual, new Vector2(0.05f, -0.2f), fruitSize, FruitColor, SortOrder.NodeTop);
        }

        private static void CreateMarket(Transform parent, GameConfig config)
        {
            GameObject market = new GameObject("Market");
            market.transform.SetParent(parent, false);
            market.transform.position = MarketPosition;

            CreateBox("Visual", market.transform, Vector2.zero, MarketSize, MarketColor, SortOrder.Zone);
            CreateWorldLabel("PAZAR", market.transform, Vector2.zero, new Vector2(3f, 1.2f), 80);

            BoxCollider2D area = market.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = MarketSize;

            SetField(market.AddComponent<Market>(), "config", config);
        }

        // Açılabilir bölge kurar ve içeriğin (kaynak noktaları) ekleneceği "Content" nesnesini döndürür.
        //   Area X (UnlockableArea)
        //   ├ Ground  : bölgenin zemini (her zaman görünür)
        //   ├ Locked  : çit + bölge adı (kapalıyken görünür)
        //   └ Content : bölge açılınca görünen nesneler (başta gizli)
        private static Transform CreateArea(string name, AreaDefinition area, Rect rect, Color groundColor)
        {
            GameObject root = new GameObject(name);

            CreateBox("Ground", root.transform, rect.center, rect.size, groundColor, SortOrder.GroundPatch);

            GameObject locked = new GameObject("Locked");
            locked.transform.SetParent(root.transform, false);
            CreateFence(locked.transform, rect);
            Text title = CreateWorldLabel(area.DisplayName, locked.transform, rect.center, new Vector2(rect.width, 1.2f), 60);
            title.color = new Color(LabelColor.r, LabelColor.g, LabelColor.b, 0.5f);

            GameObject content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);
            content.SetActive(false); // UnlockableArea açık olup olmadığına göre gösterir

            UnlockableArea unlockable = root.AddComponent<UnlockableArea>();
            SetField(unlockable, "area", area);
            SetField(unlockable, "content", content);
            SetField(unlockable, "lockedVisual", locked);

            return content.transform;
        }

        // Dikdörtgenin dört kenarına ince çit kutuları koyar.
        private static void CreateFence(Transform parent, Rect rect)
        {
            float t = FenceThickness;
            CreateBox("Fence Bottom", parent, new Vector2(rect.center.x, rect.yMin), new Vector2(rect.width, t), FenceColor, SortOrder.Fence);
            CreateBox("Fence Top", parent, new Vector2(rect.center.x, rect.yMax), new Vector2(rect.width, t), FenceColor, SortOrder.Fence);
            CreateBox("Fence Left", parent, new Vector2(rect.xMin, rect.center.y), new Vector2(t, rect.height), FenceColor, SortOrder.Fence);
            CreateBox("Fence Right", parent, new Vector2(rect.xMax, rect.center.y), new Vector2(t, rect.height), FenceColor, SortOrder.Fence);
        }

        private static void CreateUnlockPad(string name, Vector2 position, AreaDefinition area, AreaDefinition requiredArea,
                                            GameConfig config, FlyingVisual coinPrefab)
        {
            GameObject pad = new GameObject(name);
            pad.transform.position = position;

            CreateBox("Visual", pad.transform, Vector2.zero, PadSize, PadColor, SortOrder.Zone);
            Text label = CreateWorldLabel(area.DisplayName + "\n" + area.Price, pad.transform, Vector2.zero, PadSize, 30);

            BoxCollider2D trigger = pad.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = PadSize;

            UnlockPad unlockPad = pad.AddComponent<UnlockPad>();
            SetField(unlockPad, "config", config);
            SetField(unlockPad, "area", area);
            SetField(unlockPad, "requiredArea", requiredArea);
            SetField(unlockPad, "label", label);
            SetField(unlockPad, "coinPrefab", coinPrefab);
        }

        private static void CreateDock()
        {
            GameObject dock = new GameObject("Dock");
            CreateBox("Dock Area", dock.transform, DockAreaRect.center, DockAreaRect.size, DockColor, SortOrder.GroundPatch);
            CreateBox("Pier", dock.transform, PierRect.center, PierRect.size, DockColor, SortOrder.GroundPatch);
            Text label = CreateWorldLabel("İSKELE (yakında)", dock.transform, DockAreaRect.center, new Vector2(DockAreaRect.width, 1f), 44);
            label.color = new Color(1f, 1f, 1f, 0.8f);
        }

        // Renkli kare sprite'lı basit bir kutu oluşturur. localPos ebeveyne göredir (ebeveyn yoksa dünya konumu).
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

        // Dünya içinde duran yazı (World Space Canvas). size dünya birimidir; 1 birim = 100 UI birimi.
        private static Text CreateWorldLabel(string text, Transform parent, Vector2 localPos, Vector2 size, int fontSize)
        {
            const float unitsToUi = 100f;

            GameObject go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one / unitsToUi;

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = SortOrder.WorldLabel;
            ((RectTransform)go.transform).sizeDelta = size * unitsToUi;

            Text label = CreateText(go.transform, text, fontSize);
            label.color = LabelColor;
            Stretch(label.rectTransform);
            return label;
        }

        // ================================================================ Arayüz

        private static VirtualJoystick CreateUI(GameConfig config, out RectTransform topBar)
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
            topBar = CreateTopBar(canvasGo.transform);
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

        // Ekranın en üstünde, tam genişlikte koyu çubuk. Kamera görüntüsü bunun altında kalır,
        // böylece çubuk hiçbir dünya nesnesini kapatmaz. Para göstergesi bunun içindedir.
        private static RectTransform CreateTopBar(Transform canvas)
        {
            Image bar = CreateImage("TopBar", canvas, null, TopBarColor);
            bar.raycastTarget = false;
            RectTransform rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, TopBarHeight);

            // Para göstergesi: sarı kutucuk + sayı, çubuğun ortasında.
            Image icon = CreateImage("CoinIcon", rt, null, CoinColor);
            icon.raycastTarget = false;
            icon.rectTransform.anchoredPosition = new Vector2(-80f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(64f, 64f);

            Text text = CreateText(rt, "0", 64);
            text.alignment = TextAnchor.MiddleLeft;
            text.rectTransform.anchoredPosition = new Vector2(80f, 0f);
            text.rectTransform.sizeDelta = new Vector2(220f, 100f);

            SetField(bar.gameObject.AddComponent<CurrencyUI>(), "coinsText", text);
            return rt;
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
            text.verticalOverflow = VerticalWrapMode.Overflow; // uzun yazı alt satıra geçerse kesilmesin
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
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRect(Object target, string fieldName, Rect value)
        {
            SerializedProperty property = FindProperty(target, fieldName, out SerializedObject so);
            if (property == null) return;
            property.rectValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(Object target, string fieldName, out SerializedObject so)
        {
            so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"Kayıp Ada: {target.GetType().Name} içinde '{fieldName}' alanı bulunamadı.");
            }
            return property;
        }
    }
}
