// PrototypeSceneBuilder.cs
// Editör menü komutu: Tools > Kayıp Ada > Prototip Sahnesini Kur
// 3D sahneyi her seferinde sıfırdan kurar ve Assets/_Game/Scenes/Prototype.unity olarak kaydeder. Sırası:
//   1) Proje ayarları (URP 3D, TextMeshPro)        → ProjectSetup3D
//   2) Ayar dosyaları (Data/) — SADECE yoksa oluşturulur; senin değişikliklerin korunur
//   3) Üretilen görseller (ada, su, arayüz dokuları) → ArtGenerator
//   4) Prefab'lar (karakter, kaynak, bina, süs...)   → PrefabFactory (modeller ModelCatalog'da)
//   5) Sahne: ışık, kamera, ada, su, oyun nesneleri (üretim zincirleri, kızaktaki gemi, final töreni), süsler, arayüz
//   6) Docs/Modeller.md yeniden yazılır
// Harita yerleşimi MapLayout'tadır.

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace KayipAda.EditorTools
{
    public static class PrototypeSceneBuilder
    {
        private const string Root = BuilderUtil.Root;
        private const string ConfigPath = Root + "/Data/GameConfig.asset";
        private const string ResourceTypesFolder = Root + "/Data/ResourceTypes";
        // Test Menüsü'nde yığını doldurma seçenekleri (sırayla).
        public static readonly string[] DevResourceOrder = { "Plank", "Brick", "Rope", "Cloth", "Fruit", "Wood", "Stone", "Fiber" };
        private const string NodeTypesFolder = Root + "/Data/NodeTypes";
        private const string AreasFolder = Root + "/Data/Areas";
        private const string BuildingTypesFolder = Root + "/Data/Buildings";
        private const string WorkerTypesFolder = Root + "/Data/Workers";
        private const string SoundLibraryPath = Root + "/Data/SoundLibrary.asset";
        private const string UpgradesFolder = Root + "/Data/Upgrades";
        private const string ShipConfigPath = Root + "/Data/Ship.asset";
        private const string PostProcessPath = ProjectSetup3D.SettingsFolder + "/PostProcess.asset";
        private const string ScenePath = Root + "/Scenes/Prototype.unity";
        private const string AudioFolder = BuilderUtil.ThirdParty + "/Audio";

        // Renkler (ışık, deniz, arayüz)
        private static readonly Color SunColor = new Color(1f, 0.93f, 0.80f);
        private static readonly Color SeaColor = new Color(0.10f, 0.42f, 0.64f);
        private static readonly Color FogColor = new Color(0.36f, 0.66f, 0.82f);
        private static readonly Color PanelColor = new Color(0.10f, 0.13f, 0.19f, 0.86f);

        // Kaynak türlerinin ilk değerleri (dosya zaten varsa dokunulmaz; sonradan Inspector'dan değiştir).
        private static readonly Color WoodColor = new Color(0.60f, 0.38f, 0.18f);
        private static readonly Color StoneColor = new Color(0.62f, 0.62f, 0.65f);
        private static readonly Color FruitColor = new Color(1.00f, 0.55f, 0.10f);
        private static readonly Color PlankColor = new Color(0.90f, 0.72f, 0.45f);
        private static readonly Color BrickColor = new Color(0.75f, 0.28f, 0.20f);

        [MenuItem("Tools/Kayıp Ada/Prototip Sahnesini Kur")]
        public static void Build()
        {
            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sor.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!ProjectSetup3D.Run())
            {
                // TextMeshPro kaynakları ekleniyor; bitince kurulum kendiliğinden devam eder.
                AssetDatabase.importPackageCompleted += OnTmpImported;
                return;
            }
            BuildNow();
        }

        private static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            EditorApplication.delayCall += BuildNow;
        }

        public static void BuildNow()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Kayıp Ada", "Ayar dosyaları...", 0.05f);
                CreateDataAssets();
                EnsureModelsImported();

                EditorUtility.DisplayProgressBar("Kayıp Ada", "Görseller üretiliyor...", 0.15f);
                PrefabFactory.Reset();
                ArtGenerator.GenerateSprites();

                EditorUtility.DisplayProgressBar("Kayıp Ada", "Prefab'lar üretiliyor...", 0.3f);
                Prefabs prefabs = BuildPrefabs();

                EditorUtility.DisplayProgressBar("Kayıp Ada", "Sahne kuruluyor...", 0.6f);
                BuildScene(prefabs);

                DocsWriter.Write(PrefabFactory.MissingModels);
                AssetDatabase.SaveAssets();

                if (PrefabFactory.MissingModels.Count > 0)
                {
                    Debug.LogWarning("Kayıp Ada: Bulunamayan modeller (yerine pembe kutu kondu):\n" + string.Join("\n", PrefabFactory.MissingModels));
                }
                Debug.Log("Kayıp Ada: 3D prototip sahnesi kuruldu → " + ScenePath);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ================================================================ Ayar dosyaları

        private static void CreateDataAssets()
        {
            BuilderUtil.EnsureFolder(ResourceTypesFolder);
            BuilderUtil.EnsureFolder(NodeTypesFolder);
            BuilderUtil.EnsureFolder(AreasFolder);
            BuilderUtil.EnsureFolder(BuildingTypesFolder);
            BuilderUtil.EnsureFolder(WorkerTypesFolder);
            BuilderUtil.EnsureFolder(UpgradesFolder);
            BuilderUtil.EnsureFolder(Root + "/Scenes");

            if (BuilderUtil.Load<GameConfig>(ConfigPath) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfig>(), ConfigPath);
            }
            //                          dosya     kimlik   ad       renk        fiyat
            CreateResourceTypeIfMissing("Wood",  "wood",  "Odun",  WoodColor,  5);
            CreateResourceTypeIfMissing("Stone", "stone", "Taş",   StoneColor, 8);
            CreateResourceTypeIfMissing("Fruit", "fruit", "Meyve", FruitColor, 4);
            CreateResourceTypeIfMissing("Plank", "plank", "Kalas", PlankColor, 14); // 2 odun (10) → 1 kalas (14)
            CreateResourceTypeIfMissing("Brick", "brick", "Tuğla", BrickColor, 22); // 2 taş (16) → 1 tuğla (22)
            CreateResourceTypeIfMissing("Fiber", "fiber", "Lif",   new Color(0.86f, 0.76f, 0.46f), 3);
            CreateResourceTypeIfMissing("Rope",  "rope",  "Halat", new Color(0.74f, 0.55f, 0.32f), 14);       // 3 lif (9) → 1 halat (14)
            CreateResourceTypeIfMissing("Cloth", "cloth", "Yelken Bezi", new Color(0.96f, 0.94f, 0.86f), 20); // 4 lif (12) → 1 bez (20)
            //                      dosya        kaynak   stok  toplama aralığı  yeniden dolma (sn)
            CreateNodeTypeIfMissing("Tree",      "Wood",  8,    0.35f,           10f);
            CreateNodeTypeIfMissing("Rock",      "Stone", 6,    0.6f,            15f);
            CreateNodeTypeIfMissing("FruitBush", "Fruit", 5,    0.4f,            12f);
            CreateNodeTypeIfMissing("FiberPalm", "Fiber", 8,    0.35f,           10f);
            //                  dosya             kimlik             ad               fiyat
            CreateAreaIfMissing("Rocky",          "rocky",           "Kayalık",       150);
            CreateAreaIfMissing("Orchard",        "orchard",         "Meyve Bahçesi", 600);
            CreateAreaIfMissing("Sawmill",        "sawmill",         "Bıçkıhane",     250);
            CreateAreaIfMissing("StoneWorkshop",  "stone_workshop",  "Taş Atölyesi",  800);
            CreateAreaIfMissing("HireLumberjack", "hire_lumberjack", "Oduncu",        400);
            CreateAreaIfMissing("HireCarrier",    "hire_carrier",    "Taşıyıcı",      600);
            CreateAreaIfMissing("Cove",           "cove",            "Palmiye Koyu",  500);
            CreateAreaIfMissing("RopeWorkshop",   "rope_workshop",   "Halat Atölyesi", 700);
            CreateAreaIfMissing("Loom",           "loom",            "Dokuma Tezgahı", 900);
            CreateAreaIfMissing("HireFiber",      "hire_fiber",      "Lifçi",         500);
            // Gemi aşaması kilitleri (kareleri yok; aşama bitince kod açar, sonraki hedefin karesi bunu bekler)
            CreateAreaIfMissing("ShipStage1",     "ship_stage_1",    "Omurga bitti",  1);
            CreateAreaIfMissing("ShipStage2",     "ship_stage_2",    "Gövde bitti",   1);
            CreateAreaIfMissing("ShipStage3",     "ship_stage_3",    "Direk bitti",   1);
            CreateAreaIfMissing("ShipStage4",     "ship_stage_4",    "Yelken bitti",  1);
            //                          dosya            ad              girdi    adet  çıktı    adet  süre  giriş kap. çıkış kap.
            CreateBuildingTypeIfMissing("Sawmill",       "Bıçkıhane",    "Wood",  2,    "Plank", 1,    2f,   10,        10);
            CreateBuildingTypeIfMissing("StoneWorkshop", "Taş Atölyesi", "Stone", 2,    "Brick", 1,    3f,   10,        10);
            CreateBuildingTypeIfMissing("RopeWorkshop",  "Halat Atölyesi", "Fiber", 3,  "Rope",  1,    3f,   12,        10);
            CreateBuildingTypeIfMissing("Loom",          "Dokuma Tezgahı", "Fiber", 4,  "Cloth", 1,    4f,   12,        10);
            //                        dosya         ad          hız   kapasite  sayı
            CreateWorkerTypeIfMissing("Lumberjack", "Oduncu",   3.2f, 6,        1);
            CreateWorkerTypeIfMissing("Carrier",    "Taşıyıcı", 3.6f, 8,        1);
            CreateWorkerTypeIfMissing("FiberGatherer", "Lifçi", 3.2f, 6,        1);
            //                     dosya             kimlik             ad                    etkilediği                      fiyatlar (5 seviye)                  toplam etki (5 seviye)
            CreateUpgradeIfMissing("PlayerCapacity", "player_capacity", "Taşıma Kapasitesi", UpgradeStat.PlayerCapacity, new[] { 120, 280, 520, 840, 1300 },  new[] { 2f, 4f, 6f, 9f, 12f });
            CreateUpgradeIfMissing("PlayerSpeed",    "player_speed",    "Koşu Hızı",         UpgradeStat.PlayerSpeed,    new[] { 140, 320, 600, 960, 1440 },  new[] { 0.08f, 0.16f, 0.24f, 0.32f, 0.4f });
            CreateUpgradeIfMissing("WorkerSpeed",    "worker_speed",    "İşçi Hızı",         UpgradeStat.WorkerSpeed,    new[] { 180, 400, 720, 1120, 1640 },  new[] { 0.12f, 0.24f, 0.36f, 0.5f, 0.65f });
            CreateUpgradeIfMissing("WorkerCapacity", "worker_capacity", "İşçi Kapasitesi",   UpgradeStat.WorkerCapacity, new[] { 180, 400, 720, 1120, 1640 },  new[] { 1f, 2f, 3f, 5f, 7f });
            CreateUpgradeIfMissing("BuildingSpeed",  "building_speed",  "Bina Hızı",         UpgradeStat.BuildingSpeed,  new[] { 220, 480, 840, 1300, 1900 }, new[] { 0.15f, 0.3f, 0.5f, 0.7f, 1f });
            CreateShipConfigIfMissing();
            CreateSoundLibraryIfMissing();
            AssetDatabase.SaveAssets();
        }

        private static void CreateUpgradeIfMissing(string file, string id, string displayName, UpgradeStat stat, int[] prices, float[] bonuses)
        {
            string path = UpgradesFolder + "/" + file + ".asset";
            if (BuilderUtil.Load<UpgradeType>(path) != null) return;
            UpgradeType type = ScriptableObject.CreateInstance<UpgradeType>();
            type.EditorSetup(id, displayName, stat, prices, bonuses);
            AssetDatabase.CreateAsset(type, path);
        }

        private static readonly string[] UpgradeFiles = { "PlayerCapacity", "PlayerSpeed", "WorkerSpeed", "WorkerCapacity", "BuildingSpeed" };

        // Gemi aşamaları: her biri bitince bir sonraki hedefin kilidini açar.
        private static void CreateShipConfigIfMissing()
        {
            if (BuilderUtil.Load<ShipConfig>(ShipConfigPath) != null) return;
            ResourceType plank = Data<ResourceType>(ResourceTypesFolder, "Plank");
            ResourceType brick = Data<ResourceType>(ResourceTypesFolder, "Brick");
            ResourceType rope = Data<ResourceType>(ResourceTypesFolder, "Rope");
            ResourceType cloth = Data<ResourceType>(ResourceTypesFolder, "Cloth");
            ResourceType fruit = Data<ResourceType>(ResourceTypesFolder, "Fruit");
            ShipConfig ship = ScriptableObject.CreateInstance<ShipConfig>();
            ship.Stages = new[]
            {
                Stage("Omurga", 0.25f, "ShipStage1", "Yeni: Palmiye Koyu açılabilir!", Req(plank, 40)),
                Stage("Gövde", 0.55f, "ShipStage2", "Yeni: Halat Atölyesi açılabilir!", Req(plank, 40), Req(brick, 30)),
                Stage("Direk", 0.8f, "ShipStage3", "Yeni: Dokuma Tezgahı ve Lifçi işçi!", Req(plank, 30), Req(rope, 20)),
                Stage("Yelken", 1f, "ShipStage4", "Yeni: Meyve Bahçesi açılabilir!", Req(cloth, 20), Req(rope, 15)),
                Stage("Erzak", 1f, null, "Suya indirme zamanı!", Req(fruit, 40))
            };
            AssetDatabase.CreateAsset(ship, ShipConfigPath);
        }

        private static ShipConfig.Stage Stage(string name, float reveal, string unlockFile, string message, params ShipConfig.Requirement[] reqs)
        {
            return new ShipConfig.Stage
            {
                Name = name, RevealAtEnd = reveal, Requirements = reqs, UnlockMessage = message,
                UnlockOnComplete = unlockFile != null ? Data<AreaDefinition>(AreasFolder, unlockFile) : null
            };
        }

        private static ShipConfig.Requirement Req(ResourceType type, int amount)
        {
            return new ShipConfig.Requirement { Resource = type, Amount = amount };
        }

        private static T Data<T>(string folder, string file) where T : Object => BuilderUtil.Load<T>(folder + "/" + file + ".asset");

        private static void CreateResourceTypeIfMissing(string file, string id, string displayName, Color color, int price)
        {
            string path = ResourceTypesFolder + "/" + file + ".asset";
            if (BuilderUtil.Load<ResourceType>(path) != null) return;
            ResourceType type = ScriptableObject.CreateInstance<ResourceType>();
            type.EditorSetup(id, displayName, color, price);
            AssetDatabase.CreateAsset(type, path);
        }

        private static void CreateNodeTypeIfMissing(string file, string resource, int stock, float interval, float regrow)
        {
            string path = NodeTypesFolder + "/" + file + ".asset";
            if (BuilderUtil.Load<ResourceNodeType>(path) != null) return;
            ResourceNodeType type = ScriptableObject.CreateInstance<ResourceNodeType>();
            type.EditorSetup(Data<ResourceType>(ResourceTypesFolder, resource), stock, interval, regrow);
            AssetDatabase.CreateAsset(type, path);
        }

        private static void CreateAreaIfMissing(string file, string id, string displayName, int price)
        {
            string path = AreasFolder + "/" + file + ".asset";
            if (BuilderUtil.Load<AreaDefinition>(path) != null) return;
            AreaDefinition area = ScriptableObject.CreateInstance<AreaDefinition>();
            area.EditorSetup(id, displayName, price);
            AssetDatabase.CreateAsset(area, path);
        }

        private static void CreateBuildingTypeIfMissing(string file, string displayName, string input, int inputAmount,
                                                        string output, int outputAmount, float time, int inCap, int outCap)
        {
            string path = BuildingTypesFolder + "/" + file + ".asset";
            if (BuilderUtil.Load<BuildingType>(path) != null) return;
            BuildingType type = ScriptableObject.CreateInstance<BuildingType>();
            type.EditorSetup(displayName, Data<ResourceType>(ResourceTypesFolder, input), inputAmount,
                             Data<ResourceType>(ResourceTypesFolder, output), outputAmount, time, inCap, outCap);
            AssetDatabase.CreateAsset(type, path);
        }

        private static void CreateWorkerTypeIfMissing(string file, string displayName, float speed, int capacity, int count)
        {
            string path = WorkerTypesFolder + "/" + file + ".asset";
            if (BuilderUtil.Load<WorkerType>(path) != null) return;
            WorkerType type = ScriptableObject.CreateInstance<WorkerType>();
            type.EditorSetup(displayName, speed, capacity, count);
            AssetDatabase.CreateAsset(type, path);
        }

        // Ses seçimi (Docs/Modeller.md'de de listelenir). Dosya varsa sadece eksik ses türleri eklenir.
        private static void CreateSoundLibraryIfMissing()
        {
            SoundLibrary.Entry[] defaults =
            {
                Sound(SoundId.Collect, 0.6f, "Interface/pluck_001", "Interface/pluck_002"),
                Sound(SoundId.Chop, 0.5f, "RPG/chop"),
                Sound(SoundId.Sell, 0.7f, "RPG/handleCoins", "RPG/handleCoins2"),
                Sound(SoundId.Coin, 0.5f, "Interface/tick_001", "Interface/tick_002", "Interface/tick_004"),
                Sound(SoundId.Unlock, 0.9f, "Interface/confirmation_002"),
                Sound(SoundId.Button, 0.7f, "Interface/click_001"),
                Sound(SoundId.Footstep, 0.5f, "Impact/footstep_grass_000", "Impact/footstep_grass_001", "Impact/footstep_grass_002",
                      "Impact/footstep_grass_003", "Impact/footstep_grass_004"),
                Sound(SoundId.Drop, 0.4f, "Impact/impactWood_light_000", "Impact/impactWood_light_001", "Impact/impactWood_light_002"),
                Sound(SoundId.Produce, 0.5f, "Impact/impactPlank_medium_000", "Impact/impactPlank_medium_001"),
                Sound(SoundId.Upgrade, 0.9f, "Interface/maximize_006"),
                Sound(SoundId.Build, 0.6f, "Impact/impactWood_medium_000", "Impact/impactWood_medium_001", "Impact/impactWood_medium_002"),
                Sound(SoundId.Victory, 1f, "Interface/confirmation_004"),
                Sound(SoundId.Mine, 0.6f, "Impact/impactMining_000", "Impact/impactMining_001", "Impact/impactMining_002",
                      "Impact/impactMining_003"),
                Sound(SoundId.StageComplete, 1f, "Interface/maximize_008"),
                Sound(SoundId.Splash, 1f, "Impact/impactSoft_heavy_000", "Impact/impactSoft_heavy_001"),
                Sound(SoundId.Firework, 0.6f, "Impact/impactGlass_light_000", "Impact/impactGlass_light_001", "Impact/impactGlass_light_002")
            };

            SoundLibrary existing = BuilderUtil.Load<SoundLibrary>(SoundLibraryPath);
            if (existing != null)
            {
                existing.EditorAddMissing(defaults);
                EditorUtility.SetDirty(existing);
                return;
            }
            SoundLibrary library = ScriptableObject.CreateInstance<SoundLibrary>();
            library.EditorSetup(defaults, null);
            AssetDatabase.CreateAsset(library, SoundLibraryPath);
        }

        private static SoundLibrary.Entry Sound(SoundId id, float volume, params string[] files)
        {
            List<AudioClip> clips = new List<AudioClip>();
            foreach (string file in files)
            {
                AudioClip clip = BuilderUtil.Load<AudioClip>(AudioFolder + "/" + file + ".ogg");
                if (clip != null) clips.Add(clip);
                else Debug.LogWarning("Kayıp Ada: Ses bulunamadı (atlandı): " + file);
            }
            return new SoundLibrary.Entry { Id = id, Clips = clips.ToArray(), Volume = volume };
        }

        // Model içe aktarma ayarları uygulanmamışsa (ör. ilk açılış) ThirdParty klasörünü yeniden içe aktarır.
        private static void EnsureModelsImported()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelCatalog.Player.Path) as ModelImporter;
            if (importer == null) return;
            if (importer.animationType == ModelImporterAnimationType.Generic && !importer.importCameras && !importer.importLights) return;

            EditorUtility.DisplayProgressBar("Kayıp Ada", "Modeller yeniden içe aktarılıyor (bir kez)...", 0.1f);
            AssetDatabase.ImportAsset(BuilderUtil.ThirdParty, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        // ================================================================ Prefab'lar

        private class Prefabs
        {
            public GameObject Player, Lumberjack, Carrier, Market, UnlockPad, Camp, Confetti, Coin, FloatingText;
            public GameObject[] Palms, Rocks;
            public GameObject FruitTree, Sawmill, Workshop;
            public GameObject Sparkle, Splash, Firework, UpgradeStation, FiberWorker, RopeWorkshop, Loom;
            public GameObject[] FiberPalms;
            public readonly Dictionary<string, GameObject> Decor = new Dictionary<string, GameObject>();
        }

        private static Prefabs BuildPrefabs()
        {
            GameConfig config = BuilderUtil.Load<GameConfig>(ConfigPath);
            Prefabs p = new Prefabs();

            // Kaynak modelleri: her kaynak türü kendi prefab'ını bilir.
            LinkItem("Wood", PrefabFactory.BuildItem(ModelCatalog.Wood));
            LinkItem("Stone", PrefabFactory.BuildItem(ModelCatalog.Stone));
            LinkItem("Fruit", PrefabFactory.BuildItem(ModelCatalog.Fruit));
            LinkItem("Plank", PrefabFactory.BuildItem(ModelCatalog.Plank));
            LinkItem("Brick", PrefabFactory.BuildItem(ModelCatalog.Brick));
            LinkItem("Fiber", PrefabFactory.BuildItem(ModelCatalog.Fiber));
            LinkItem("Rope", PrefabFactory.BuildRopeItem());
            LinkItem("Cloth", PrefabFactory.BuildItem(ModelCatalog.Cloth));
            p.Coin = PrefabFactory.BuildCoin();

            // Efektler
            GameObject dust = PrefabFactory.BuildDust();
            p.Confetti = PrefabFactory.BuildConfetti();
            GameObject smoke = PrefabFactory.BuildWorkEffect("Smoke", false);
            GameObject sawdust = PrefabFactory.BuildWorkEffect("Sawdust", true);
            p.FloatingText = PrefabFactory.BuildFloatingText();
            p.Sparkle = PrefabFactory.BuildSparkle();
            p.Splash = PrefabFactory.BuildSplash();
            p.Firework = PrefabFactory.BuildFirework();

            // Karakterler (ortak denetleyici oyuncu modelinin klipleriyle kurulur)
            UnityEditor.Animations.AnimatorController baseController = CharacterAnimation.GetBaseController(ModelCatalog.Player.Path);
            p.Player = PrefabFactory.BuildCharacter(ModelCatalog.Player,
                CharacterAnimation.GetOverride(baseController, ModelCatalog.Player.Path, ModelCatalog.Player.Key), config, dust, true);
            p.Lumberjack = PrefabFactory.BuildCharacter(ModelCatalog.Lumberjack,
                CharacterAnimation.GetOverride(baseController, ModelCatalog.Lumberjack.Path, ModelCatalog.Lumberjack.Key), config, dust, false);
            p.Carrier = PrefabFactory.BuildCharacter(ModelCatalog.Carrier,
                CharacterAnimation.GetOverride(baseController, ModelCatalog.Carrier.Path, ModelCatalog.Carrier.Key), config, dust, false);
            p.FiberWorker = PrefabFactory.BuildCharacter(ModelCatalog.FiberWorker,
                CharacterAnimation.GetOverride(baseController, ModelCatalog.FiberWorker.Path, ModelCatalog.FiberWorker.Key), config, dust, false);

            foreach (ModelSpec enemy in ModelCatalog.Enemies)
            {
                RuntimeAnimatorController controller;
                if (enemy.File == "Characters_Skeleton") controller = CharacterAnimation.GetEnemyController(enemy.Path, "Enemy_Skeleton");
                else if (enemy.File == "Characters_Shark") controller = CharacterAnimation.GetSingleClip(enemy.Path, "Swim", enemy.Key);
                else if (enemy.File == "Characters_Tentacle") controller = CharacterAnimation.GetSingleClip(enemy.Path, "Tentacle_Idle", enemy.Key);
                else controller = CharacterAnimation.GetOverride(baseController, enemy.Path, enemy.Key);
                PrefabFactory.BuildEnemy(enemy, controller);
            }

            // Kaynak noktaları
            ResourceNodeType tree = Data<ResourceNodeType>(NodeTypesFolder, "Tree");
            ResourceNodeType rock = Data<ResourceNodeType>(NodeTypesFolder, "Rock");
            ResourceNodeType bush = Data<ResourceNodeType>(NodeTypesFolder, "FruitBush");
            // Vuruş sesleri (oynanış sayısı değil, her seferinde ayarlanır).
            rock.EditorSetHitSound(SoundId.Mine);
            bush.EditorSetHitSound(SoundId.Collect);
            EditorUtility.SetDirty(rock);
            EditorUtility.SetDirty(bush);
            p.Palms = new GameObject[ModelCatalog.Palms.Length];
            for (int i = 0; i < p.Palms.Length; i++)
                p.Palms[i] = PrefabFactory.BuildNode(ModelCatalog.Palms[i], tree, config, 1.6f, new Vector2(0.6f, 0.6f), 1.1f,
                                                     null, 0, null, ModelCatalog.Stump);
            p.Rocks = new GameObject[ModelCatalog.Rocks.Length];
            for (int i = 0; i < p.Rocks.Length; i++)
                p.Rocks[i] = PrefabFactory.BuildNode(ModelCatalog.Rocks[i], rock, config, 1.9f, new Vector2(1.3f, 1.3f), 1.5f,
                                                     null, 0, new Color(0.62f, 0.6f, 0.58f), ModelCatalog.Rubble);
            p.FruitTree = PrefabFactory.BuildNode(ModelCatalog.FruitTree, bush, config, 1.7f, new Vector2(0.8f, 0.8f), 1.2f,
                                                  ModelCatalog.Fruit, 6, null, ModelCatalog.Stump);
            ResourceNodeType fiberPalm = Data<ResourceNodeType>(NodeTypesFolder, "FiberPalm");
            p.FiberPalms = new[]
            {
                PrefabFactory.BuildNode(ModelCatalog.FiberPalm, fiberPalm, config, 1.6f, new Vector2(0.6f, 0.6f), 1.1f,
                                        ModelCatalog.Fiber, 4, new Color(0.86f, 0.76f, 0.46f), ModelCatalog.Stump)
            };

            // Binalar ve alanlar
            p.Sawmill = PrefabFactory.BuildBuilding(ModelCatalog.Sawmill, Data<BuildingType>(BuildingTypesFolder, "Sawmill"), config,
                                                    sawdust, FindDecor("LogPile"), new Vector3(-2.7f, 0f, 1.2f));
            p.Workshop = PrefabFactory.BuildBuilding(ModelCatalog.Workshop, Data<BuildingType>(BuildingTypesFolder, "StoneWorkshop"), config,
                                                     smoke, FindDecor("Workbench"), new Vector3(-2.6f, 0f, 0.8f));
            p.RopeWorkshop = PrefabFactory.BuildBuilding(ModelCatalog.RopeWorkshop, Data<BuildingType>(BuildingTypesFolder, "RopeWorkshop"), config,
                                                         sawdust, FindDecor("Bucket"), new Vector3(-2.6f, 0f, 0.8f));
            p.Loom = PrefabFactory.BuildBuilding(ModelCatalog.Loom, Data<BuildingType>(BuildingTypesFolder, "Loom"), config,
                                                 sawdust, FindDecor("Crate"), new Vector3(-2.6f, 0f, 0.8f));
            p.Market = PrefabFactory.BuildMarket(config);
            p.UnlockPad = PrefabFactory.BuildUnlockPad(config);
            p.Camp = PrefabFactory.BuildCamp();

            foreach (ModelSpec spec in ModelCatalog.Decor) p.Decor[spec.Key] = PrefabFactory.BuildDecor(spec);
            foreach (ModelSpec spec in new[] { ModelCatalog.Fence, ModelCatalog.Dock, ModelCatalog.DockPole, ModelCatalog.ShipWreck })
                p.Decor[spec.Key] = PrefabFactory.BuildDecor(spec);

            p.UpgradeStation = PrefabFactory.BuildUpgradeStation(config);
            return p;
        }

        private static ModelSpec FindDecor(string key)
        {
            foreach (ModelSpec spec in ModelCatalog.Decor) if (spec.Key == key) return spec;
            return null;
        }

        private static void LinkItem(string resourceFile, ResourceItem prefab)
        {
            ResourceType type = Data<ResourceType>(ResourceTypesFolder, resourceFile);
            if (type == null) return;
            type.EditorSetPrefab(prefab);
            EditorUtility.SetDirty(type);
        }

        // ================================================================ Sahne

        private static void BuildScene(Prefabs p)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Önemli: Yeni sahne açılınca Unity bellekteki varlık referanslarını boşaltabilir (özellikle bu çalıştırmada
            // yeni oluşturulan dosyalar). Bu yüzden ayar dosyası yeni sahneden SONRA diskten yüklenir.
            GameConfig config = BuilderUtil.Load<GameConfig>(ConfigPath);

            // Ada şekli (zemin, su ve yerleşim bunu kullanır)
            GameObject islandGo = new GameObject("Island");
            IslandShape island = islandGo.AddComponent<IslandShape>();
            island.EditorSetup(MapLayout.IslandCenter, MapLayout.IslandRadius);

            CreateLighting();
            CreateGroundAndWater(islandGo, island);

            // Yöneticiler
            GameObject managers = new GameObject("Managers");
            managers.AddComponent<AppSettings>();
            BuilderUtil.SetField(managers.AddComponent<CurrencyManager>(), "config", config);
            BuilderUtil.SetField(managers.AddComponent<AreaManager>(), "config", config);
            UpgradeManager upgrades = managers.AddComponent<UpgradeManager>();
            BuilderUtil.SetField(upgrades, "config", config);
            List<Object> upgradeTypes = new List<Object>();
            foreach (string file in UpgradeFiles) upgradeTypes.Add(Data<UpgradeType>(UpgradesFolder, file));
            BuilderUtil.SetArray(upgrades, "upgrades", upgradeTypes.ToArray());
            BuilderUtil.SetField(managers.AddComponent<SaveManager>(), "config", config);
            Effects effects = managers.AddComponent<Effects>();
            BuilderUtil.SetField(effects, "sparklePrefab", p.Sparkle.GetComponent<ParticleSystem>());
            BuilderUtil.SetField(effects, "splashPrefab", p.Splash.GetComponent<ParticleSystem>());
            BuilderUtil.SetField(effects, "fireworkPrefab", p.Firework.GetComponent<ParticleSystem>());
            BuilderUtil.SetField(effects, "config", config);
            BuilderUtil.SetField(effects, "confettiPrefab", p.Confetti.GetComponent<ParticleSystem>());
            BuilderUtil.SetField(effects, "coinPrefab", p.Coin);
            BuilderUtil.SetField(effects, "floatingTextPrefab", p.FloatingText.GetComponent<FloatingText>());
            AudioManager audio = managers.AddComponent<AudioManager>();
            BuilderUtil.SetField(audio, "config", config);
            BuilderUtil.SetField(audio, "library", BuilderUtil.Load<SoundLibrary>(SoundLibraryPath));

            VirtualJoystick joystick = CreateUI(config);
            List<Vector3> reserved = new List<Vector3>(); // süs konmayacak yerler: (x, z, yarıçap)

            // Veriler
            AreaDefinition rocky = Data<AreaDefinition>(AreasFolder, "Rocky");
            AreaDefinition orchard = Data<AreaDefinition>(AreasFolder, "Orchard");
            AreaDefinition sawmillArea = Data<AreaDefinition>(AreasFolder, "Sawmill");
            AreaDefinition workshopArea = Data<AreaDefinition>(AreasFolder, "StoneWorkshop");
            AreaDefinition hireLumberjack = Data<AreaDefinition>(AreasFolder, "HireLumberjack");
            AreaDefinition hireCarrier = Data<AreaDefinition>(AreasFolder, "HireCarrier");
            AreaDefinition cove = Data<AreaDefinition>(AreasFolder, "Cove");
            AreaDefinition ropeArea = Data<AreaDefinition>(AreasFolder, "RopeWorkshop");
            AreaDefinition loomArea = Data<AreaDefinition>(AreasFolder, "Loom");
            AreaDefinition hireFiber = Data<AreaDefinition>(AreasFolder, "HireFiber");
            AreaDefinition stage1 = Data<AreaDefinition>(AreasFolder, "ShipStage1");
            AreaDefinition stage2 = Data<AreaDefinition>(AreasFolder, "ShipStage2");
            AreaDefinition stage3 = Data<AreaDefinition>(AreasFolder, "ShipStage3");
            AreaDefinition stage4 = Data<AreaDefinition>(AreasFolder, "ShipStage4");

            // Başlangıç sahili: palmiyeler, Pazar, Bıçkıhane
            Transform beach = new GameObject("Beach").transform;
            ResourceNode[] palms = new ResourceNode[MapLayout.Palms.Length];
            System.Random treeRandom = new System.Random(MapLayout.TreeSeed);
            for (int i = 0; i < palms.Length; i++)
            {
                GameObject palm = Place(p.Palms[i % p.Palms.Length], beach, MapLayout.Palms[i], 0f);
                VaryTree(palm, treeRandom);
                palms[i] = palm.GetComponent<ResourceNode>();
                Reserve(reserved, MapLayout.Palms[i], 2.2f);
            }
            Market market = Place(p.Market, beach, MapLayout.Market, 0f).GetComponent<Market>();
            Reserve(reserved, MapLayout.Market + new Vector3(0f, 0f, 1f), 3.6f);

            GameObject sawmill = PlaceBuilding("Sawmill", beach, MapLayout.Sawmill, p.Sawmill, sawmillArea, config);
            Reserve(reserved, MapLayout.Sawmill, 3.8f);

            // Kayalık: kayalar + Taş Atölyesi (kendi karesiyle açılır)
            Transform rockyContent = CreateArea("Area Rocky", rocky, MapLayout.RockyRect, p.Decor[ModelCatalog.Fence.Key], config);
            for (int i = 0; i < MapLayout.Rocks.Length; i++)
            {
                Place(p.Rocks[i % p.Rocks.Length], rockyContent, MapLayout.Rocks[i], i * 110f);
            }
            GameObject workshop = PlaceBuilding("StoneWorkshop", rockyContent, MapLayout.Workshop, p.Workshop, workshopArea, config);

            // Meyve bahçesi
            Transform orchardContent = CreateArea("Area Orchard", orchard, MapLayout.OrchardRect, p.Decor[ModelCatalog.Fence.Key], config);
            foreach (Vector3 pos in MapLayout.FruitTrees) VaryTree(Place(p.FruitTree, orchardContent, pos, 0f), treeRandom);

            // Palmiye Koyu: lif veren palmiyeler. Kuzeyinde Halat Atölyesi ve Dokuma Tezgahı.
            Transform coveContent = CreateArea("Area Cove", cove, MapLayout.CoveRect, p.Decor[ModelCatalog.Fence.Key], config);
            ResourceNode[] fiberPalms = new ResourceNode[MapLayout.FiberPalms.Length];
            for (int i = 0; i < fiberPalms.Length; i++)
            {
                GameObject palm = Place(p.FiberPalms[0], coveContent, MapLayout.FiberPalms[i], 0f);
                VaryTree(palm, treeRandom);
                fiberPalms[i] = palm.GetComponent<ResourceNode>();
            }
            GameObject ropeWorkshop = PlaceBuilding("RopeWorkshop", null, MapLayout.RopeWorkshop, p.RopeWorkshop, ropeArea, config);
            GameObject loom = PlaceBuilding("Loom", null, MapLayout.Loom, p.Loom, loomArea, config);
            Reserve(reserved, MapLayout.RopeWorkshop, 3.8f);
            Reserve(reserved, MapLayout.Loom, 3.8f);

            // Açma kareleri: bölge girişlerinde ve binaların yerinde.
            Transform pads = new GameObject("UnlockPads").transform;
            CreatePad(p.UnlockPad, pads, "Pad Rocky", MapLayout.RockyPad, rocky, null, reserved);
            CreatePad(p.UnlockPad, pads, "Pad Orchard", MapLayout.OrchardPad, orchard, stage4, reserved);
            CreatePad(p.UnlockPad, pads, "Pad Sawmill", MapLayout.Sawmill + MapLayout.BuildingPadOffset, sawmillArea, null, reserved);
            CreatePad(p.UnlockPad, pads, "Pad StoneWorkshop", MapLayout.Workshop + MapLayout.BuildingPadOffset, workshopArea, rocky, reserved);
            CreatePad(p.UnlockPad, pads, "Pad Lumberjack", MapLayout.LumberjackPad, hireLumberjack, sawmillArea, reserved);
            CreatePad(p.UnlockPad, pads, "Pad Carrier", MapLayout.CarrierPad, hireCarrier, hireLumberjack, reserved);
            CreatePad(p.UnlockPad, pads, "Pad Cove", MapLayout.CovePad, cove, stage1, reserved);
            CreatePad(p.UnlockPad, pads, "Pad RopeWorkshop", MapLayout.RopeWorkshop + MapLayout.BuildingPadOffset, ropeArea, stage2, reserved);
            CreatePad(p.UnlockPad, pads, "Pad Loom", MapLayout.Loom + MapLayout.BuildingPadOffset, loomArea, stage3, reserved);
            CreatePad(p.UnlockPad, pads, "Pad FiberWorker", MapLayout.FiberWorkerPad, hireFiber, stage3, reserved);

            // Kamp, geliştirme tezgahı ve işçiler
            Place(p.Camp, null, MapLayout.Camp, 0f);
            Reserve(reserved, MapLayout.Camp, 3.8f);
            UpgradePanel upgradePanel = UiBuilder.CreateUpgradePanel(config, upgradeTypes);
            GameObject station = Place(p.UpgradeStation, null, MapLayout.UpgradeStation, 0f);
            BuilderUtil.SetField(station.GetComponent<UpgradeStation>(), "panel", upgradePanel);
            Reserve(reserved, MapLayout.UpgradeStation, 2.6f);
            Transform workers = new GameObject("Workers").transform;

            LumberjackWorker lumberjack = CreateWorkerTemplate<LumberjackWorker>(p.Lumberjack, Data<WorkerType>(WorkerTypesFolder, "Lumberjack"));
            BuilderUtil.SetArray(lumberjack, "trees", palms);
            BuilderUtil.SetField(lumberjack, "buildingInput", sawmill.GetComponentInChildren<BuildingInput>(true));
            CreateSpawner("Lumberjacks", workers, hireLumberjack, Data<WorkerType>(WorkerTypesFolder, "Lumberjack"), lumberjack, config);

            CarrierWorker carrier = CreateWorkerTemplate<CarrierWorker>(p.Carrier, Data<WorkerType>(WorkerTypesFolder, "Carrier"));
            BuilderUtil.SetArray(carrier, "sources", new Object[]
            {
                sawmill.GetComponentInChildren<BuildingOutput>(true), workshop.GetComponentInChildren<BuildingOutput>(true),
                ropeWorkshop.GetComponentInChildren<BuildingOutput>(true), loom.GetComponentInChildren<BuildingOutput>(true)
            });
            BuilderUtil.SetField(carrier, "market", market);
            CreateSpawner("Carriers", workers, hireCarrier, Data<WorkerType>(WorkerTypesFolder, "Carrier"), carrier, config);

            // Lifçi: koydaki palmiyelerden lif toplar, Halat Atölyesi'ne götürür (Oduncu ile aynı davranış).
            WorkerType fiberType = Data<WorkerType>(WorkerTypesFolder, "FiberGatherer");
            LumberjackWorker fiberWorker = CreateWorkerTemplate<LumberjackWorker>(p.FiberWorker, fiberType);
            BuilderUtil.SetArray(fiberWorker, "trees", fiberPalms);
            BuilderUtil.SetField(fiberWorker, "buildingInput", ropeWorkshop.GetComponentInChildren<BuildingInput>(true));
            CreateSpawner("FiberGatherers", workers, hireFiber, fiberType, fiberWorker, config);

            // Oyuncu ve kamera
            GameObject player = Place(p.Player, null, MapLayout.PlayerStart, 0f);
            player.name = "Player";
            BuilderUtil.SetField(player.GetComponent<PlayerController>(), "joystick", joystick);
            Reserve(reserved, MapLayout.PlayerStart, 1.5f);
            CameraFollow follow = CreateCamera(config, player.transform);

            // İskele: gemi inşası, final töreni ve kaçış; Test Menüsü
            UiBuilder.CreateCelebrationBanner();
            ResourceType[] devResources = new ResourceType[DevResourceOrder.Length];
            for (int i = 0; i < devResources.Length; i++) devResources[i] = Data<ResourceType>(ResourceTypesFolder, DevResourceOrder[i]);
            UiBuilder.CreateDevMenu(config, devResources);
            CreateShipyard(p, island, config, follow, reserved);
            CreateGuide(config, follow.GetComponent<Camera>());

            // Süsler (yolları ve oyun alanlarını kapatmadan)
            foreach (Vector3 rock in MapLayout.Rocks) Reserve(reserved, rock, 2.4f);
            foreach (Vector3 tree in MapLayout.FruitTrees) Reserve(reserved, tree, 2.3f);
            foreach (Vector3 palm in MapLayout.FiberPalms) Reserve(reserved, palm, 2.2f);
            Reserve(reserved, MapLayout.Workshop, 3.9f);
            ScatterDecor(p, island, reserved);

            // Kaydet ve proje ayarları
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // Ağaçlar doğal görünsün: rastgele yöne bakar, hafif (±TreeTiltMax derece) eğilir, boyu biraz farklıdır.
        // Eğim ve boy gövde dibinin etrafında uygulanır: dip zeminde kalır. Kaynak noktasında "Visual" alt
        // nesnesine uygulanır (ResourceNode sallanmayı bunun üstüne ekler).
        private static void VaryTree(GameObject tree, System.Random random)
        {
            Transform target = tree.transform.Find("Visual");
            if (target == null) target = tree.transform;
            float yaw = (float)random.NextDouble() * 360f;
            float tilt = ((float)random.NextDouble() * 2f - 1f) * MapLayout.TreeTiltMax;
            float tiltDir = (float)random.NextDouble() * 360f;
            float scale = 1f + ((float)random.NextDouble() * 2f - 1f) * MapLayout.TreeSizeJitter;
            target.localRotation = Quaternion.Euler(0f, tiltDir, 0f) * Quaternion.Euler(tilt, 0f, 0f) *
                                   Quaternion.Euler(0f, yaw - tiltDir, 0f) * target.localRotation;
            target.localScale *= scale;
        }

        private static void Reserve(List<Vector3> reserved, Vector3 pos, float radius)
        {
            reserved.Add(new Vector3(pos.x, pos.z, radius));
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 position, float yaw)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        // ---------------------------------------------------------------- Işık ve ortam

        private static void CreateLighting()
        {
            GameObject sun = new GameObject("Sun");
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            RenderSettings.sun = light;

            // Hafif renkli ortam ışığı: gökten mavimsi, yandan sıcak, yerden kumlu.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.86f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.80f, 0.70f);
            RenderSettings.ambientGroundColor = new Color(0.52f, 0.47f, 0.40f);

            // Uzak denize hafif sis: ufuk yumuşasın.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 120f;

            // Mobil uyumlu post-processing: hafif bloom, renk düzenleme, hafif vignette.
            GameObject volumeGo = new GameObject("PostProcess");
            Volume volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = GetPostProcessProfile();
        }

        private static VolumeProfile GetPostProcessProfile()
        {
            VolumeProfile existing = BuilderUtil.Load<VolumeProfile>(PostProcessPath);
            if (existing != null) return existing; // senin ayarların korunur

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PostProcessPath);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.15f);
            bloom.scatter.Override(0.65f);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.05f);
            color.contrast.Override(6f);
            color.saturation.Override(2f); // göz yormasın: hafif

            WhiteBalance white = profile.Add<WhiteBalance>(true);
            white.temperature.Override(8f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);

            foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void CreateGroundAndWater(GameObject islandGo, IslandShape island)
        {
            Mesh islandMesh = ArtGenerator.BuildIslandMesh(island);
            Texture2D groundTexture = ArtGenerator.BuildIslandTexture(island);
            islandGo.AddComponent<MeshFilter>().sharedMesh = islandMesh;
            MeshRenderer ground = islandGo.AddComponent<MeshRenderer>();
            ground.sharedMaterial = ArtGenerator.GetLit("M_Island", groundTexture, Color.white);
            ground.shadowCastingMode = ShadowCastingMode.Off;
            ground.receiveShadows = true;
            BuilderUtil.MarkStatic(islandGo);

            GameObject water = new GameObject("Water");
            water.transform.position = new Vector3(0f, ArtGenerator.WaterLevel, 0f);
            water.AddComponent<MeshFilter>().sharedMesh = ArtGenerator.BuildWaterMesh(island);
            MeshRenderer renderer = water.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ArtGenerator.GetWater(island, ArtGenerator.BuildShoreTexture(island));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static CameraFollow CreateCamera(GameConfig config, Transform target)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Quaternion rotation = Quaternion.Euler(config.CameraPitch, config.CameraYaw, 0f);
            Vector3 focus = target.position + Quaternion.Euler(0f, config.CameraYaw, 0f) * Vector3.forward * config.CameraFocusOffset;
            go.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * config.CameraDistance, rotation);

            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = config.CameraFov;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 150f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SeaColor;
            go.AddComponent<AudioListener>();

            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // MSAA 2x URP ayarında

            CameraFollow follow = go.AddComponent<CameraFollow>();
            BuilderUtil.SetField(follow, "config", config);
            BuilderUtil.SetField(follow, "target", target);
            return follow;
        }

        // ---------------------------------------------------------------- Bölgeler ve binalar

        // Açılabilir bölge: kapalıyken çit + yerde ad; içerik (Content) açılınca zıplayarak belirir.
        private static Transform CreateArea(string name, AreaDefinition area, Rect rect, GameObject fencePrefab, GameConfig config)
        {
            GameObject root = new GameObject(name);
            root.transform.position = new Vector3(rect.center.x, 0f, rect.center.y);

            GameObject locked = new GameObject("Locked");
            locked.transform.SetParent(root.transform, false);
            CreateFence(locked.transform, rect, fencePrefab);
            UiFactory.CreateWorldLabel("Name", locked.transform, new Vector3(0f, 1.6f, 0f), area.DisplayName, 7f, Color.white);

            GameObject content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);
            content.SetActive(false); // UnlockableArea açık olup olmadığına göre gösterir

            UnlockableArea unlockable = root.AddComponent<UnlockableArea>();
            BuilderUtil.SetField(unlockable, "config", config);
            BuilderUtil.SetField(unlockable, "area", area);
            BuilderUtil.SetField(unlockable, "content", content);
            BuilderUtil.SetField(unlockable, "lockedVisual", locked);
            return content.transform;
        }

        // Dikdörtgenin dört kenarına çit parçaları dizer (parent dikdörtgenin ortasında).
        private static void CreateFence(Transform parent, Rect rect, GameObject fencePrefab)
        {
            float halfW = rect.width / 2f, halfD = rect.height / 2f;
            FenceLine(parent, fencePrefab, new Vector3(-halfW, 0f, -halfD), new Vector3(halfW, 0f, -halfD));
            FenceLine(parent, fencePrefab, new Vector3(halfW, 0f, -halfD), new Vector3(halfW, 0f, halfD));
            FenceLine(parent, fencePrefab, new Vector3(halfW, 0f, halfD), new Vector3(-halfW, 0f, halfD));
            FenceLine(parent, fencePrefab, new Vector3(-halfW, 0f, halfD), new Vector3(-halfW, 0f, -halfD));
        }

        private static void FenceLine(Transform parent, GameObject fencePrefab, Vector3 from, Vector3 to)
        {
            float length = Vector3.Distance(from, to);
            int count = Mathf.Max(1, Mathf.RoundToInt(length / ModelCatalog.Fence.Size));
            float yaw = Quaternion.LookRotation(to - from).eulerAngles.y - 90f; // çit modeli X yönünde uzanır
            for (int i = 0; i < count; i++)
            {
                GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab);
                piece.transform.SetParent(parent, false);
                piece.transform.localPosition = Vector3.Lerp(from, to, (i + 0.5f) / count);
                piece.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        // Bina da bir açılabilir alandır: kapalıyken silik zemin (üstünde açma karesi), açılınca bina zıplayarak belirir.
        private static GameObject PlaceBuilding(string name, Transform parent, Vector3 position, GameObject prefab,
                                                AreaDefinition area, GameConfig config)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            GameObject locked = new GameObject("Locked");
            locked.transform.SetParent(root.transform, false);
            PrefabFactory.AddGroundDecal("Footprint", locked.transform, new Vector3(0f, 0f, 0.3f), new Vector2(4.6f, 4.4f),
                                         new Color(1f, 1f, 1f, 0.22f));

            GameObject content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);
            GameObject building = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            building.transform.SetParent(content.transform, false);
            content.SetActive(false);

            UnlockableArea unlockable = root.AddComponent<UnlockableArea>();
            BuilderUtil.SetField(unlockable, "config", config);
            BuilderUtil.SetField(unlockable, "area", area);
            BuilderUtil.SetField(unlockable, "content", content);
            BuilderUtil.SetField(unlockable, "lockedVisual", locked);
            return building;
        }

        private static void CreatePad(GameObject prefab, Transform parent, string name, Vector3 position,
                                      AreaDefinition area, AreaDefinition requiredArea, List<Vector3> reserved)
        {
            GameObject pad = Place(prefab, parent, position, 0f);
            pad.name = name;
            UnlockPad component = pad.GetComponent<UnlockPad>();
            BuilderUtil.SetField(component, "area", area);
            BuilderUtil.SetField(component, "requiredArea", requiredArea);
            Transform nameLabel = pad.transform.Find("Canvas/Name"); // editörde de doğru ad görünsün
            if (nameLabel != null) nameLabel.GetComponent<TMPro.TextMeshProUGUI>().text = area.DisplayName;
            Reserve(reserved, position, 1.9f);
        }

        // ---------------------------------------------------------------- İşçiler

        // Gizli hazır işçi: WorkerSpawner bunu kopyalayıp kamptan çıkarır.
        private static T CreateWorkerTemplate<T>(GameObject prefab, WorkerType type) where T : Worker
        {
            GameObject go = Place(prefab, null, MapLayout.CampSpawn, 180f);
            T worker = go.AddComponent<T>();
            BuilderUtil.SetField(worker, "type", type);
            return worker;
        }

        private static void CreateSpawner(string name, Transform parent, AreaDefinition hireArea, WorkerType type, Worker template, GameConfig config)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = MapLayout.CampSpawn;

            template.transform.SetParent(go.transform, true);
            template.gameObject.SetActive(false);

            WorkerSpawner spawner = go.AddComponent<WorkerSpawner>();
            BuilderUtil.SetField(spawner, "config", config);
            BuilderUtil.SetField(spawner, "hireArea", hireArea);
            BuilderUtil.SetField(spawner, "workerType", type);
            BuilderUtil.SetField(spawner, "template", template);
            BuilderUtil.SetField(spawner, "spawnPoint", go.transform);
        }

        // ---------------------------------------------------------------- Tersane: kızaktaki gemi ve final töreni

        private static void CreateShipyard(Prefabs p, IslandShape island, GameConfig config, CameraFollow follow, List<Vector3> reserved)
        {
            ShipConfig shipConfig = BuilderUtil.Load<ShipConfig>(ShipConfigPath);
            Transform root = new GameObject("Shipyard").transform;
            Vector3 start = MapLayout.DockStart(island);

            // İskele: uzun kenarı denize (+Z), güverte suyun biraz üstünde.
            GameObject dock = Place(p.Decor[ModelCatalog.Dock.Key], root, start, 0f);
            Bounds b = PrefabFactory.WorldBounds(dock);
            if (b.size.x > b.size.z)
            {
                dock.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                b = PrefabFactory.WorldBounds(dock);
            }
            dock.transform.position += new Vector3(start.x - b.center.x, -b.size.y * 0.3f, start.z - b.min.z);
            BuilderUtil.MarkStatic(dock);

            GameObject wreck = Place(p.Decor[ModelCatalog.ShipWreck.Key], root, start + MapLayout.WreckOffset, 35f);
            wreck.transform.position += Vector3.down * (PrefabFactory.WorldBounds(wreck).size.y * 0.3f);
            BuilderUtil.MarkStatic(wreck);
            Place(p.Decor["Anchor"], root, start + new Vector3(-2.4f, 0f, -0.6f), 20f);
            Place(p.Decor["BucketFish"], root, start + new Vector3(-1.6f, 0f, -1.6f), 0f);

            // Geminin yeri: iskelenin doğusunda, ortası kıyıdan ShipInland metre içeride (pruva suya uzanır).
            float shipZ = start.z;
            for (int i = 0; i < 200; i++)
            {
                if (island.SignedDistance(new Vector2(MapLayout.ShipX, shipZ)) <= -MapLayout.ShipInland) break;
                shipZ -= 0.1f;
            }
            GameObject shipRoot = new GameObject("Ship");
            shipRoot.transform.SetParent(root, false);
            shipRoot.transform.position = new Vector3(MapLayout.ShipX, 0f, shipZ);

            Transform hull = new GameObject("Hull").transform;
            hull.SetParent(shipRoot.transform, false);
            GameObject model = PrefabFactory.CreateModel(ModelCatalog.Ship, hull);
            OrientShip(model, hull, shipRoot.transform);
            Bounds sb = PrefabFactory.WorldBounds(model);

            Material reveal = ArtGenerator.GetMaterial("M_ShipReveal", "KayipAda/BuildReveal", m =>
            {
                m.SetTexture("_BaseMap", BuilderUtil.Load<Texture2D>(BuilderUtil.ThirdParty + "/Quaternius_Pirate/Textures/Atlas_Pirate.png"));
                m.SetColor("_BaseColor", Color.white);
            });
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = reveal;
                r.sharedMaterials = mats;
            }

            // Kızak: geminin altında iki kütük rayı, denize doğru uzanır.
            GameObject slipway = new GameObject("Slipway");
            slipway.transform.SetParent(root, false);
            float railStart = sb.min.z - 0.5f, railEnd = sb.max.z + 4f;
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = railStart; z < railEnd; z += 2.4f)
                {
                    Vector2 flat = new Vector2(sb.center.x + side * sb.extents.x * 0.35f, z + 1.2f);
                    float sd = island.SignedDistance(flat);
                    float y = sd > 0f ? -sd * 0.35f : 0.02f;
                    GameObject log = Place(p.Decor[ModelCatalog.DockPole.Key], slipway.transform, new Vector3(flat.x, y, flat.y), 0f);
                    Bounds lb = PrefabFactory.WorldBounds(log);
                    if (lb.size.x > lb.size.z) log.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }
            BuilderUtil.MarkStatic(slipway);

            // Gemi karadayken karakterler içinden geçmesin (tören başlayınca kapanır).
            NavObstacle obstacle = shipRoot.AddComponent<NavObstacle>();
            BuilderUtil.SetVector2(obstacle, "size", new Vector2(sb.size.x + 0.4f, sb.size.z + 0.4f));

            // Erzak: güvertede beliren variller ve sandıklar.
            Transform supplies = new GameObject("Supplies").transform;
            supplies.SetParent(shipRoot.transform, false);
            float deckY = sb.min.y + sb.size.y * 0.4f;
            string[] supplyKeys = { "Barrel", "Chest", "Barrel", "Crate", "Barrel", "Chest" };
            for (int i = 0; i < supplyKeys.Length; i++)
            {
                float along = (i / (supplyKeys.Length - 1f) - 0.5f) * sb.size.z * 0.45f;
                Vector3 world = new Vector3(sb.center.x + (i % 2 == 0 ? -0.4f : 0.4f), deckY, sb.center.z + along);
                GameObject item = Place(p.Decor[supplyKeys[i]], supplies, world, i * 50f);
                item.transform.localScale = Vector3.one * 0.8f;
            }

            // İnşa iskelesi: deniz tarafında ve pruvada direkler (gemi bitince kaybolur).
            GameObject scaffolding = new GameObject("Scaffolding");
            scaffolding.transform.SetParent(shipRoot.transform, false);
            Vector3[] poles =
            {
                new Vector3(sb.max.x + 0.6f, 0.02f, sb.center.z - sb.extents.z * 0.5f),
                new Vector3(sb.max.x + 0.6f, 0.02f, sb.center.z + sb.extents.z * 0.5f),
                new Vector3(sb.min.x - 0.6f, 0.02f, sb.center.z + sb.extents.z * 0.5f)
            };
            for (int k = 0; k < poles.Length; k++) Place(p.Decor[ModelCatalog.DockPole.Key], scaffolding.transform, poles[k], k * 40f);

            Transform target = new GameObject("DeliveryTarget").transform;
            target.SetParent(shipRoot.transform, false);
            target.position = new Vector3(sb.center.x, sb.min.y + sb.size.y * 0.45f, sb.center.z);

            // Teslim alanı (geminin kara tarafında): mavi halka ve "Gövde (2/5) Kalas: 12/40" göstergesi.
            GameObject delivery = new GameObject("ShipDelivery");
            delivery.transform.SetParent(root, false);
            delivery.transform.position = shipRoot.transform.position + MapLayout.DeliveryFromShip;
            CapsuleCollider trigger = delivery.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;
            trigger.height = 3f;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            ProgressRing ring = CreateFlatRing(delivery.transform, new Color(0.45f, 0.85f, 1f));
            CreateDeliveryMarker(delivery.transform, shipRoot.transform.position, config);
            TMPro.TextMeshPro label = UiFactory.CreateWorldLabel("Progress", delivery.transform, new Vector3(0f, 4.6f, 0f), "", 4.2f, Color.white);
            ShipDelivery shipDelivery = delivery.AddComponent<ShipDelivery>();

            ShipBuilder builder = shipRoot.AddComponent<ShipBuilder>();
            BuilderUtil.SetField(builder, "ship", shipConfig);
            BuilderUtil.SetField(builder, "config", config);
            BuilderUtil.SetField(builder, "shipModel", hull);
            BuilderUtil.SetField(builder, "suppliesRoot", supplies);
            BuilderUtil.SetField(builder, "scaffolding", scaffolding);
            BuilderUtil.SetField(builder, "progressLabel", label);
            BuilderUtil.SetField(builder, "progressRing", ring);
            BuilderUtil.SetField(builder, "deliveryTarget", target);
            BuilderUtil.SetField(builder, "delivery", shipDelivery);
            BuilderUtil.SetField(shipDelivery, "config", config);
            BuilderUtil.SetField(shipDelivery, "builder", builder);
            Reserve(reserved, start, 4.5f);
            Reserve(reserved, shipRoot.transform.position, Mathf.Max(sb.extents.x, sb.extents.z) + 1.5f);
            Reserve(reserved, delivery.transform.position, 2.2f);

            // Final töreni: işçiler teslim alanının çevresinde toplanır, gemi denize (+Z) kayar.
            Transform gatherRoot = new GameObject("GatherPoints").transform;
            gatherRoot.SetParent(root, false);
            List<Object> gatherPoints = new List<Object>();
            for (int i = 0; i < 4; i++)
            {
                Transform t = new GameObject("Gather " + i).transform;
                t.SetParent(gatherRoot, false);
                t.position = delivery.transform.position + new Vector3(-1.6f + i * 1.1f, 0f, -2.2f);
                gatherPoints.Add(t);
            }

            GameObject ceremonyGo = new GameObject("LaunchCeremony");
            LaunchCeremony ceremony = ceremonyGo.AddComponent<LaunchCeremony>();
            BuilderUtil.SetField(ceremony, "ship", shipConfig);
            BuilderUtil.SetField(ceremony, "builder", builder);
            BuilderUtil.SetField(ceremony, "shipRoot", shipRoot.transform);
            BuilderUtil.SetField(ceremony, "shipObstacle", obstacle);
            BuilderUtil.SetVector3(ceremony, "slideDirection", Vector3.forward);
            BuilderUtil.SetArray(ceremony, "gatherPoints", gatherPoints.ToArray());
            BuilderUtil.SetField(ceremony, "cameraFollow", follow);
            BuilderUtil.SetField(ceremony, "escapePanel", UiBuilder.CreateEscapePanel());
        }

        // Gemi teslim alanı uzaktan görünsün: yerde nabız atan parlak daire, gemiyi gösteren oklar ve üstte
        // aşağıyı gösteren zıplayan ok.
        private static void CreateDeliveryMarker(Transform delivery, Vector3 shipPosition, GameConfig config)
        {
            Color blue = new Color(0.45f, 0.85f, 1f);
            Transform glow = new GameObject("GroundMark").transform;
            glow.SetParent(delivery, false);
            PrefabFactory.AddGroundDecal("Glow", glow, Vector3.down * 0.01f, new Vector2(3.8f, 3.8f), new Color(blue.r, blue.g, blue.b, 0.35f),
                                         ArtGenerator.SoftCirclePath, 0f);

            Vector3 toShip = shipPosition - delivery.position;
            toShip.y = 0f;
            toShip.Normalize();
            float yaw = Mathf.Atan2(toShip.x, toShip.z) * Mathf.Rad2Deg;
            for (int i = 0; i < 3; i++)
            {
                PrefabFactory.AddGroundDecal("ShipArrow " + i, delivery, toShip * (1.7f + i * 0.65f), new Vector2(0.7f, 0.7f),
                                             new Color(1f, 1f, 1f, 0.55f + i * 0.15f), ArtGenerator.ArrowPath, yaw);
            }

            Transform arrow = new GameObject("Arrow").transform;
            arrow.SetParent(delivery, false);
            arrow.localPosition = new Vector3(0f, 2.4f, 0f);
            arrow.gameObject.AddComponent<Billboard>();
            SpriteRenderer sprite = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            sprite.transform.SetParent(arrow, false);
            sprite.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // aşağıyı göstersin
            sprite.transform.localScale = Vector3.one * 0.75f;
            sprite.sprite = ArtGenerator.LoadSprite(ArtGenerator.ArrowPath);
            sprite.color = UiFactory.Gold;
            sprite.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            sprite.shadowCastingMode = ShadowCastingMode.Off;

            DeliveryMarker marker = delivery.gameObject.AddComponent<DeliveryMarker>();
            BuilderUtil.SetField(marker, "config", config);
            BuilderUtil.SetField(marker, "arrow", arrow);
            BuilderUtil.SetField(marker, "groundMark", glow);
        }

        // Gemi modeli yan yatık ya da yan dönük gelebiliyor: direkleri yukarı, boyunu Z eksenine (denize) çevirir,
        // gemiyi köke ortalar ve tabanını kızağın üstüne oturtur.
        private static void OrientShip(GameObject model, Transform hull, Transform shipRoot)
        {
            Bounds sb = PrefabFactory.WorldBounds(model);
            Quaternion yaw = sb.size.x > sb.size.z ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            hull.localRotation = yaw;
            sb = PrefabFactory.WorldBounds(model);
            if (sb.size.x > sb.size.y)
            {
                // Model pivotu omurgadadır, görsel merkez direklere doğru kayar: o tarafı yukarı çevir.
                float side = sb.center.x - model.transform.position.x;
                hull.localRotation = yaw * Quaternion.Euler(0f, 0f, side >= 0f ? 90f : -90f);
                sb = PrefabFactory.WorldBounds(model);
                if (sb.center.y < model.transform.position.y) hull.localRotation = yaw * Quaternion.Euler(0f, 0f, side >= 0f ? -90f : 90f);
                sb = PrefabFactory.WorldBounds(model);
            }
            if (sb.size.x > sb.size.z) hull.localRotation = Quaternion.Euler(0f, 90f, 0f) * hull.localRotation; // boyu denize
            sb = PrefabFactory.WorldBounds(model);
            hull.position -= new Vector3(sb.center.x - shipRoot.position.x, sb.min.y - shipRoot.position.y - 0.05f, sb.center.z - shipRoot.position.z);
        }

        // Yerde yatan, dolan halka (gemi teslim alanı).
        private static ProgressRing CreateFlatRing(Transform parent, Color color)
        {
            Canvas canvas = UiFactory.CreateWorldCanvas("Ring", parent, new Vector3(0f, 0.04f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector2(260f, 260f));
            Image back = UiFactory.CreateImage("Back", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.CirclePath), new Color(0.05f, 0.08f, 0.12f, 0.45f));
            UiFactory.Stretch(back.rectTransform);
            Image ringBack = UiFactory.CreateImage("RingBack", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.RingPath), new Color(1f, 1f, 1f, 0.5f));
            UiFactory.Stretch(ringBack.rectTransform);
            Image fill = UiFactory.CreateImage("Fill", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.RingPath), color);
            UiFactory.Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillAmount = 0f;
            ProgressRing ring = canvas.gameObject.AddComponent<ProgressRing>();
            BuilderUtil.SetField(ring, "fill", fill);
            return ring;
        }

        // Açılabilir nesne (top gibi): kapalıyken silik zemin, açılınca zıplayarak belirir.
        private static GameObject PlaceUnlockable(string name, Transform parent, Vector3 position, GameObject prefab, AreaDefinition area,
                                                  GameConfig config, Vector2 footprint, Vector3 footprintOffset)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            GameObject locked = new GameObject("Locked");
            locked.transform.SetParent(root.transform, false);
            PrefabFactory.AddGroundDecal("Footprint", locked.transform, footprintOffset, footprint, new Color(1f, 1f, 1f, 0.22f));
            GameObject content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(content.transform, false);
            content.SetActive(false);
            UnlockableArea unlockable = root.AddComponent<UnlockableArea>();
            BuilderUtil.SetField(unlockable, "config", config);
            BuilderUtil.SetField(unlockable, "area", area);
            BuilderUtil.SetField(unlockable, "content", content);
            BuilderUtil.SetField(unlockable, "lockedVisual", locked);
            return instance;
        }

        // ---------------------------------------------------------------- Yönlendirme oku

        private static void CreateGuide(GameConfig config, Camera cam)
        {
            GameObject guide = new GameObject("ProgressionGuide");
            GameObject arrow = new GameObject("WorldArrow");
            arrow.transform.SetParent(guide.transform, false);
            arrow.AddComponent<Billboard>();
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "Arrow";
            quad.transform.SetParent(arrow.transform, false);
            quad.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // doku yukarıyı gösterir, ok aşağı baksın
            quad.transform.localScale = Vector3.one * 1.3f;
            Renderer r = quad.GetComponent<Renderer>();
            r.sharedMaterial = ArtGenerator.GetTransparent("M_GuideArrow", ArtGenerator.LoadSprite(ArtGenerator.ArrowPath)?.texture, UiFactory.Gold);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Transform safe = GameObject.Find("Canvas/SafeArea").transform;
            Image screenArrow = UiFactory.CreateImage("GuideScreenArrow", safe, ArtGenerator.LoadSprite(ArtGenerator.ArrowPath), UiFactory.Gold);
            screenArrow.rectTransform.sizeDelta = new Vector2(110f, 110f);
            screenArrow.gameObject.SetActive(false);

            ProgressionGuide component = guide.AddComponent<ProgressionGuide>();
            BuilderUtil.SetField(component, "config", config);
            BuilderUtil.SetField(component, "worldArrow", arrow.transform);
            BuilderUtil.SetField(component, "screenArrow", screenArrow.rectTransform);
            BuilderUtil.SetField(component, "cam", cam);
        }

        // ---------------------------------------------------------------- Süsler

        private static void ScatterDecor(Prefabs p, IslandShape island, List<Vector3> reserved)
        {
            Transform root = new GameObject("Decor").transform;
            System.Random random = new System.Random(42);
            List<Vector3> placed = new List<Vector3>(reserved);

            // Pazar ve bıçkıhane çevresinde liman eşyaları
            PlaceNear(p, root, "Barrel", MapLayout.Market + new Vector3(-2.6f, 0f, 1.6f), placed);
            PlaceNear(p, root, "Crate", MapLayout.Market + new Vector3(-2.9f, 0f, 0.4f), placed);
            PlaceNear(p, root, "CrateBottles", MapLayout.Market + new Vector3(2.9f, 0f, 2.6f), placed);
            PlaceNear(p, root, "Chest", MapLayout.Camp + new Vector3(-1.6f, 0f, 1.8f), placed);
            PlaceNear(p, root, "Barrel", MapLayout.Camp + new Vector3(1.8f, 0f, 1.6f), placed);
            PlaceNear(p, root, "Crate", MapLayout.Sawmill + new Vector3(3.1f, 0f, 1.8f), placed);

            // Kumsal (kıyı şeridi): çakıllar, taşlar, kemikler, süs palmiyeleri
            Scatter(p, root, island, random, placed, "Pebble_A", 12, -2.8f, -0.8f, 0.8f);
            Scatter(p, root, island, random, placed, "Pebble_B", 10, -2.8f, -0.8f, 0.8f);
            Scatter(p, root, island, random, placed, "SandRocks_A", 4, -2.6f, -0.6f, 1.6f);
            Scatter(p, root, island, random, placed, "SandRocks_B", 4, -2.6f, -0.6f, 1.6f);
            Scatter(p, root, island, random, placed, "SandRocks_C", 3, -2.6f, -0.6f, 1.6f);
            Scatter(p, root, island, random, placed, "Bones", 1, -2.8f, -1.6f, 3f);
            Scatter(p, root, island, random, placed, "Skulls", 1, -2.8f, -1.2f, 1.5f);
            Scatter(p, root, island, random, placed, "BrokenDock", 2, -1.6f, -0.4f, 3f);
            Scatter(p, root, island, random, placed, "DecorPalm", 8, -3.2f, -1.6f, 3.2f);
            Scatter(p, root, island, random, placed, "SmallRock", 6, -6f, -2.5f, 1.2f);

            // Çimenlik iç kısım: otlar, çiçekler, çalılar
            Scatter(p, root, island, random, placed, "GrassTuft", 22, -40f, -3.6f, 0.9f);
            Scatter(p, root, island, random, placed, "Grass", 18, -40f, -3.6f, 0.8f);
            Scatter(p, root, island, random, placed, "FlowerYellow", 10, -40f, -4f, 0.8f);
            Scatter(p, root, island, random, placed, "FlowerRed", 8, -40f, -4f, 0.8f);
            Scatter(p, root, island, random, placed, "Bush", 8, -40f, -4f, 1.6f);
            Scatter(p, root, island, random, placed, "BigRock", 4, -40f, -4f, 2f);

            // Bölgelerin içi: kayalıkta çakıl ve küçük taşlar, bahçede çiçek ve çalı (oyun alanlarına değmeden).
            Scatter(p, root, island, random, placed, "SmallRock", 7, -40f, -1f, 0.9f, MapLayout.RockyRect);
            Scatter(p, root, island, random, placed, "Pebble_A", 10, -40f, -1f, 0.5f, MapLayout.RockyRect);
            Scatter(p, root, island, random, placed, "SandRocks_B", 3, -40f, -1f, 1.2f, MapLayout.RockyRect);
            Scatter(p, root, island, random, placed, "FlowerYellow", 10, -40f, -1f, 0.6f, MapLayout.OrchardRect);
            Scatter(p, root, island, random, placed, "FlowerRed", 10, -40f, -1f, 0.6f, MapLayout.OrchardRect);
            Scatter(p, root, island, random, placed, "Bush", 4, -40f, -1f, 1.2f, MapLayout.OrchardRect);
            Scatter(p, root, island, random, placed, "Pebble_B", 8, -40f, -0.8f, 0.5f, MapLayout.CoveRect);
            Scatter(p, root, island, random, placed, "GrassTuft", 6, -40f, -0.8f, 0.7f, MapLayout.CoveRect);
            BuilderUtil.MarkStatic(root.gameObject);
        }

        private static void PlaceNear(Prefabs p, Transform parent, string key, Vector3 position, List<Vector3> placed)
        {
            if (!p.Decor.TryGetValue(key, out GameObject prefab)) return;
            Place(prefab, parent, position, position.x * 37f % 360f);
            placed.Add(new Vector3(position.x, position.z, 0.8f));
        }

        // Kıyıya uzaklığı [minSd, maxSd] arasında (içeride eksi), yollardan ve diğer nesnelerden uzak rastgele yerlere koyar.
        private static void Scatter(Prefabs p, Transform parent, IslandShape island, System.Random random, List<Vector3> placed,
                                    string key, int count, float minSd, float maxSd, float radius)
        {
            Scatter(p, parent, island, random, placed, key, count, minSd, maxSd, radius, null);
        }

        // inside verilirse sadece o dikdörtgenin içine (bölge süsleri) koyar; verilmezse bölgelerin dışına.
        private static void Scatter(Prefabs p, Transform parent, IslandShape island, System.Random random, List<Vector3> placed,
                                    string key, int count, float minSd, float maxSd, float radius, Rect? inside)
        {
            if (count <= 0 || !p.Decor.TryGetValue(key, out GameObject prefab)) return;
            Vector2 c = island.Center, r = island.Radius;
            int done = 0;
            for (int attempt = 0; attempt < count * 60 && done < count; attempt++)
            {
                Vector2 pos = inside.HasValue
                    ? new Vector2(inside.Value.xMin + 0.6f + (float)random.NextDouble() * (inside.Value.width - 1.2f),
                                  inside.Value.yMin + 0.6f + (float)random.NextDouble() * (inside.Value.height - 1.2f))
                    : new Vector2(c.x + ((float)random.NextDouble() * 2f - 1f) * r.x * 1.1f,
                                  c.y + ((float)random.NextDouble() * 2f - 1f) * r.y * 1.1f);
                float sd = island.SignedDistance(pos);
                if (sd < minSd || sd > maxSd) continue;
                if (MapLayout.DistanceToPaths(pos) < MapLayout.PathWidth * 0.5f + radius * 0.6f) continue;
                if (!inside.HasValue && (InsideRect(pos, MapLayout.RockyRect, 1f) || InsideRect(pos, MapLayout.OrchardRect, 1f)
                                         || InsideRect(pos, MapLayout.CoveRect, 1f))) continue;

                bool free = true;
                foreach (Vector3 other in placed)
                {
                    float min = other.z + radius;
                    if ((new Vector2(other.x, other.y) - pos).sqrMagnitude < min * min) { free = false; break; }
                }
                if (!free) continue;

                GameObject go = Place(prefab, parent, new Vector3(pos.x, 0f, pos.y), (float)random.NextDouble() * 360f);
                go.transform.localScale = Vector3.one * (0.85f + (float)random.NextDouble() * 0.3f);
                if (key == "DecorPalm") VaryTree(go, random); // dik palmiye + hafif rastgele eğim
                placed.Add(new Vector3(pos.x, pos.y, radius));
                done++;
            }
        }

        private static bool InsideRect(Vector2 p, Rect rect, float margin)
        {
            return p.x > rect.xMin - margin && p.x < rect.xMax + margin && p.y > rect.yMin - margin && p.y < rect.yMax + margin;
        }

        // ================================================================ Arayüz

        private static VirtualJoystick CreateUI(GameConfig config)
        {
            // Dokunma/fare olaylarını UI'ya ileten EventSystem (yeni Input System ile).
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            VirtualJoystick joystick = CreateJoystick(canvasGo.transform, config); // önce: en arkada kalsın

            GameObject safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(canvasGo.transform, false);
            UiFactory.Stretch((RectTransform)safe.transform);
            safe.AddComponent<SafeArea>();
            CreateCoinPanel(safe.transform, config);
            return joystick;
        }

        private static VirtualJoystick CreateJoystick(Transform canvas, GameConfig config)
        {
            // Ekranın tamamını kaplayan görünmez dokunma alanı.
            Image area = UiFactory.CreateImage("JoystickArea", canvas, null, new Color(1f, 1f, 1f, 0f));
            area.raycastTarget = true;
            UiFactory.Stretch(area.rectTransform);

            float ringSize = config.JoystickRange * 2.5f;
            Image bg = UiFactory.CreateImage("Background", area.transform, ArtGenerator.LoadSprite(ArtGenerator.RingPath), new Color(1f, 1f, 1f, 0.35f));
            bg.rectTransform.sizeDelta = Vector2.one * ringSize;
            Image handle = UiFactory.CreateImage("Handle", bg.transform, ArtGenerator.LoadSprite(ArtGenerator.CirclePath), new Color(1f, 1f, 1f, 0.6f));
            handle.rectTransform.sizeDelta = Vector2.one * ringSize * 0.42f;

            VirtualJoystick joystick = area.gameObject.AddComponent<VirtualJoystick>();
            BuilderUtil.SetField(joystick, "config", config);
            BuilderUtil.SetField(joystick, "background", bg.rectTransform);
            BuilderUtil.SetField(joystick, "handle", handle.rectTransform);
            return joystick;
        }

        // Üstte ortada yuvarlak köşeli, gölgeli para paneli: altın ikon + büyük sayı.
        private static void CreateCoinPanel(Transform parent, GameConfig config)
        {
            Vector2 size = new Vector2(430f, 128f);
            GameObject holder = new GameObject("CoinPanel", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)holder.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -36f);
            rt.sizeDelta = size;

            Image shadow = UiFactory.CreateImage("Shadow", holder.transform, ArtGenerator.LoadSprite(ArtGenerator.PanelShadowPath), new Color(0f, 0f, 0f, 0.55f));
            shadow.type = Image.Type.Sliced;
            UiFactory.Size(shadow, new Vector2(0f, -8f), size + new Vector2(48f, 48f));

            Image panel = UiFactory.CreateImage("Panel", holder.transform, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath), PanelColor);
            panel.type = Image.Type.Sliced;
            UiFactory.Size(panel, Vector2.zero, size);

            Image icon = UiFactory.CreateImage("CoinIcon", panel.transform, ArtGenerator.LoadSprite(ArtGenerator.CoinIconPath), Color.white);
            UiFactory.Size(icon, new Vector2(-135f, 0f), new Vector2(86f, 86f));

            TMPro.TextMeshProUGUI text = UiFactory.CreateUIText("Coins", panel.transform, "0", 72f, Color.white);
            text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            UiFactory.Size(text, new Vector2(45f, 0f), new Vector2(270f, 110f));

            CurrencyUI ui = holder.AddComponent<CurrencyUI>();
            BuilderUtil.SetField(ui, "config", config);
            BuilderUtil.SetField(ui, "coinsText", text);
            BuilderUtil.SetField(ui, "punchTarget", rt);
        }
    }
}
