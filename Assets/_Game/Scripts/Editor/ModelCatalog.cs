// ModelCatalog.cs
// Hangi oyun nesnesinin hangi 3D modeli kullandığı TEK YERDE. Modeli değiştirmek için buradaki yolu değiştirip
// sahne kurma komutunu yeniden çalıştırmak yeterli (prefab'lar yeniden üretilir). Liste Docs/Modeller.md'de de var.
// Boyutlar metredir: modeller farklı ölçeklerde geldiği için prefab üretilirken bu boyuta ölçeklenir.
// Ana stil Quaternius (korsan); Kenney paketleri sadece Quaternius'ta olmayan eşyalar için.

namespace KayipAda.EditorTools
{
    public enum ModelPack { Quaternius, KenneyPirate, KenneyNature, KenneySurvival }

    public enum SizeAxis { Height, Largest }

    public class ModelSpec
    {
        public string Key;
        public ModelPack Pack;
        public string File;      // paket klasöründeki dosya adı (uzantısız)
        public float Size;       // hedef boyut (metre)
        public SizeAxis Axis;
        public bool CenterXZ;    // modelin ortası köke gelsin mi (binalar, eşyalar); ağaçlarda gövde zaten ortada
        public bool CastShadows;
        public string Note;      // Modeller.md'de görünen açıklama

        public string Path
        {
            get
            {
                switch (Pack)
                {
                    case ModelPack.KenneyPirate: return BuilderUtil.ThirdParty + "/Kenny_Pirate/FBX format/" + File + ".fbx";
                    case ModelPack.KenneyNature: return BuilderUtil.ThirdParty + "/Kenny_Nature/FBX format/" + File + ".fbx";
                    case ModelPack.KenneySurvival: return BuilderUtil.ThirdParty + "/Kenny_Survival/FBX format/" + File + ".fbx";
                    default: return BuilderUtil.ThirdParty + "/Quaternius_Pirate/FBX/" + File + ".fbx";
                }
            }
        }
    }

    public static class ModelCatalog
    {
        private static ModelSpec M(string key, ModelPack pack, string file, float size, SizeAxis axis, bool center, bool shadows, string note)
        {
            return new ModelSpec { Key = key, Pack = pack, File = file, Size = size, Axis = axis, CenterXZ = center, CastShadows = shadows, Note = note };
        }

        // ---------------------------------------------------------------- Karakterler (animasyonlu)
        public static readonly ModelSpec Player = M("Player", ModelPack.Quaternius, "Characters_Captain_Barbarossa", 1.9f, SizeAxis.Height, false, true, "Oyuncu (kaptan)");
        public static readonly ModelSpec Lumberjack = M("Worker_Lumberjack", ModelPack.Quaternius, "Characters_Henry", 1.75f, SizeAxis.Height, false, true, "Oduncu işçi");
        public static readonly ModelSpec Carrier = M("Worker_Carrier", ModelPack.Quaternius, "Characters_Anne", 1.7f, SizeAxis.Height, false, true, "Taşıyıcı işçi");

        // İleride düşman olacaklar (şimdilik sahnede kullanılmıyor, prefab'ları hazır).
        public static readonly ModelSpec[] Enemies =
        {
            M("Enemy_Skeleton", ModelPack.Quaternius, "Characters_Skeleton", 1.8f, SizeAxis.Height, false, true, "Düşman: iskelet"),
            M("Enemy_SkeletonHeadless", ModelPack.Quaternius, "Characters_Skeleton_Headless", 1.5f, SizeAxis.Height, false, true, "Düşman: başsız iskelet"),
            M("Enemy_Sharky", ModelPack.Quaternius, "Characters_Sharky", 1.9f, SizeAxis.Height, false, true, "Düşman: köpek balığı korsan"),
            M("Enemy_Mako", ModelPack.Quaternius, "Characters_Mako", 1.9f, SizeAxis.Height, false, true, "Düşman/yedek karakter: Mako"),
            M("Enemy_Shark", ModelPack.Quaternius, "Characters_Shark", 3.5f, SizeAxis.Largest, true, true, "Düşman: köpek balığı (yüzer)"),
            M("Enemy_Tentacle", ModelPack.Quaternius, "Characters_Tentacle", 3f, SizeAxis.Height, true, true, "Düşman: dokunaç")
        };

        // ---------------------------------------------------------------- Taşınan kaynaklar
        public static readonly ModelSpec Wood = M("Wood", ModelPack.Quaternius, "UI_Wood", 0.7f, SizeAxis.Largest, true, false, "Odun");
        public static readonly ModelSpec Stone = M("Stone", ModelPack.Quaternius, "UI_Rocks", 0.6f, SizeAxis.Largest, true, false, "Taş");
        public static readonly ModelSpec Fruit = M("Fruit", ModelPack.KenneyNature, "crop_melon", 0.5f, SizeAxis.Largest, true, false, "Meyve (kavun) — Quaternius'ta meyve yok");
        public static readonly ModelSpec Plank = M("Plank", ModelPack.KenneySurvival, "resource-planks", 0.75f, SizeAxis.Largest, true, false, "Kalas — Quaternius'ta yok");
        public static readonly ModelSpec Brick = M("Brick", ModelPack.KenneySurvival, "resource-stone", 0.6f, SizeAxis.Largest, true, false, "Tuğla (kesme taş) — Quaternius'ta yok");
        public static readonly ModelSpec Coin = M("Coin", ModelPack.Quaternius, "UI_Gold", 0.45f, SizeAxis.Largest, true, false, "Uçan/zıplayan para");

        // ---------------------------------------------------------------- Kaynak noktaları
        public static readonly ModelSpec[] Palms =
        {
            M("Node_Palm_1", ModelPack.Quaternius, "Environment_PalmTree_1", 5.5f, SizeAxis.Height, false, true, "Odun veren palmiye"),
            M("Node_Palm_2", ModelPack.Quaternius, "Environment_PalmTree_1", 5.2f, SizeAxis.Height, false, true, "Odun veren palmiye"),
            M("Node_Palm_3", ModelPack.Quaternius, "Environment_PalmTree_1", 5.8f, SizeAxis.Height, false, true, "Odun veren palmiye")
        };
        public static readonly ModelSpec[] Rocks =
        {
            M("Node_Rock_1", ModelPack.Quaternius, "Environment_Rock_1", 1.7f, SizeAxis.Height, true, true, "Taş veren kaya"),
            M("Node_Rock_2", ModelPack.Quaternius, "Environment_Rock_2", 1.6f, SizeAxis.Height, true, true, "Taş veren kaya"),
            M("Node_Rock_3", ModelPack.Quaternius, "Environment_Rock_3", 1.8f, SizeAxis.Height, true, true, "Taş veren kaya")
        };
        public static readonly ModelSpec FruitTree = M("Node_FruitTree", ModelPack.KenneyNature, "tree_fat", 3.4f, SizeAxis.Height, true, true, "Meyve ağacı — Quaternius'ta yok");

        // ---------------------------------------------------------------- Binalar ve alanlar
        public static readonly ModelSpec Sawmill = M("Sawmill", ModelPack.Quaternius, "Environment_Sawmill", 3.8f, SizeAxis.Largest, true, true, "Bıçkıhane");
        public static readonly ModelSpec Workshop = M("StoneWorkshop", ModelPack.Quaternius, "Environment_House3", 3.8f, SizeAxis.Largest, true, true, "Taş Atölyesi (ev modeli)");
        public static readonly ModelSpec MarketStall = M("MarketStall", ModelPack.KenneySurvival, "structure-canvas", 3.2f, SizeAxis.Largest, true, true, "Pazar tezgahı (bez çatılı) — Quaternius'ta yok");
        public static readonly ModelSpec MarketChest = M("MarketChest", ModelPack.Quaternius, "Prop_Chest_Gold", 0.9f, SizeAxis.Largest, true, true, "Pazardaki altın sandığı");
        public static readonly ModelSpec Tent = M("CampTent", ModelPack.KenneySurvival, "tent-canvas", 2.6f, SizeAxis.Largest, true, true, "Kamp çadırı — Quaternius'ta yok");
        public static readonly ModelSpec Campfire = M("Campfire", ModelPack.KenneySurvival, "campfire-pit", 1.1f, SizeAxis.Largest, true, false, "Kamp ateşi");
        public static readonly ModelSpec Fence = M("Fence", ModelPack.KenneySurvival, "fence", 2f, SizeAxis.Largest, true, true, "Kapalı bölge çiti — Quaternius'ta yok");
        public static readonly ModelSpec Dock = M("Dock", ModelPack.Quaternius, "Environment_Dock", 7f, SizeAxis.Largest, true, true, "İskele");
        public static readonly ModelSpec DockPole = M("DockPole", ModelPack.Quaternius, "Environment_Dock_Pole", 2.6f, SizeAxis.Largest, true, true, "İskele direği / inşa iskelesi");
        public static readonly ModelSpec UpgradeBench = M("UpgradeBench", ModelPack.KenneySurvival, "workbench", 1.9f, SizeAxis.Largest, true, true, "Geliştirme tezgahı (kampta) — Quaternius'ta yok");
        public static readonly ModelSpec Ship = M("Ship", ModelPack.Quaternius, "Ship_Large", 9f, SizeAxis.Largest, true, true, "İnşa edilen gemi (aşağıdan yukarı belirir)");
        public static readonly ModelSpec ShipWreck = M("ShipWreck", ModelPack.KenneyPirate, "ship-wreck", 10f, SizeAxis.Largest, true, true, "Yarım kalmış gemi iskeleti (dekor)");

        // ---------------------------------------------------------------- Süsler (Prefabs/Decor)
        public static readonly ModelSpec[] Decor =
        {
            M("Barrel", ModelPack.Quaternius, "Prop_Barrel", 0.95f, SizeAxis.Height, true, true, "Varil"),
            M("Chest", ModelPack.Quaternius, "Prop_Chest_Closed", 0.85f, SizeAxis.Largest, true, true, "Sandık"),
            M("Anchor", ModelPack.Quaternius, "Prop_Anchor", 1.5f, SizeAxis.Height, true, true, "Çapa"),
            M("Bucket", ModelPack.Quaternius, "Prop_Bucket", 0.5f, SizeAxis.Height, true, false, "Kova"),
            M("BucketFish", ModelPack.Quaternius, "Prop_Bucket_Fishes", 0.55f, SizeAxis.Height, true, false, "Balık kovası"),
            M("Cannon", ModelPack.Quaternius, "Prop_Cannon", 1.7f, SizeAxis.Largest, true, true, "Top"),
            M("CannonBall", ModelPack.Quaternius, "Prop_CannonBall", 0.3f, SizeAxis.Largest, true, false, "Gülle"),
            M("Bottle", ModelPack.Quaternius, "Prop_Bottle_1", 0.35f, SizeAxis.Height, true, false, "Şişe"),
            M("Skulls", ModelPack.Quaternius, "Environment_Skulls", 0.7f, SizeAxis.Largest, true, false, "Kafatasları"),
            M("Bones", ModelPack.Quaternius, "Environment_LargeBones", 2.6f, SizeAxis.Largest, true, false, "Büyük kemikler (kumda)"),
            M("Crate", ModelPack.KenneyPirate, "crate", 0.8f, SizeAxis.Height, true, true, "Kasa"),
            M("CrateBottles", ModelPack.KenneyPirate, "crate-bottles", 0.8f, SizeAxis.Height, true, true, "Şişe kasası"),
            M("SandRocks_A", ModelPack.KenneyPirate, "rocks-sand-a", 1.1f, SizeAxis.Largest, true, false, "Kumda taşlar"),
            M("SandRocks_B", ModelPack.KenneyPirate, "rocks-sand-b", 1.0f, SizeAxis.Largest, true, false, "Kumda taşlar"),
            M("SandRocks_C", ModelPack.KenneyPirate, "rocks-sand-c", 0.9f, SizeAxis.Largest, true, false, "Kumda taşlar"),
            M("Pebble_A", ModelPack.KenneyNature, "stone_smallFlatA", 0.35f, SizeAxis.Largest, true, false, "Küçük çakıl (kabuk yerine)"),
            M("Pebble_B", ModelPack.KenneyNature, "stone_smallFlatB", 0.3f, SizeAxis.Largest, true, false, "Küçük çakıl (kabuk yerine)"),
            M("GrassTuft", ModelPack.KenneyNature, "grass_leafs", 0.6f, SizeAxis.Height, true, false, "Ot öbeği"),
            M("GrassPatch", ModelPack.KenneyNature, "grass_leafsLarge", 0.9f, SizeAxis.Largest, true, false, "Büyük ot öbeği"),
            M("Grass", ModelPack.KenneyNature, "grass_large", 0.6f, SizeAxis.Height, true, false, "Uzun ot"),
            M("FlowerYellow", ModelPack.KenneyNature, "flower_yellowA", 0.45f, SizeAxis.Height, true, false, "Sarı çiçek"),
            M("FlowerRed", ModelPack.KenneyNature, "flower_redA", 0.45f, SizeAxis.Height, true, false, "Kırmızı çiçek"),
            M("Bush", ModelPack.KenneyNature, "plant_bushSmall", 0.9f, SizeAxis.Height, true, true, "Küçük çalı"),
            M("DecorPalm", ModelPack.Quaternius, "Environment_PalmTree_1", 5f, SizeAxis.Height, false, true, "Süs palmiyesi (kenarlarda)"),
            M("Cliff", ModelPack.Quaternius, "Environment_Cliff1", 3.2f, SizeAxis.Height, true, true, "Kayalık arkasında yamaç"),
            M("BigRock", ModelPack.Quaternius, "Environment_Rock_4", 1.3f, SizeAxis.Height, true, true, "Süs kayası"),
            M("SmallRock", ModelPack.Quaternius, "Environment_Rock_5", 0.7f, SizeAxis.Height, true, false, "Küçük süs kayası"),
            M("BrokenDock", ModelPack.Quaternius, "Environment_Dock_Broken", 3f, SizeAxis.Largest, true, false, "Kırık gemi/iskele parçası"),
            M("Signpost", ModelPack.KenneySurvival, "signpost", 1.5f, SizeAxis.Height, true, true, "Tabela"),
            M("Workbench", ModelPack.KenneySurvival, "workbench-anvil", 1.1f, SizeAxis.Largest, true, true, "Örs (atölye süsü)"),
            M("LogPile", ModelPack.KenneyNature, "log_stack", 1.4f, SizeAxis.Largest, true, true, "Kütük yığını (bıçkıhane süsü)")
        };

        // Modeller.md'deki "pakette olmayan" listesi.
        public static readonly string[] NotInPacks =
        {
            "Kumda deniz kabuğu: hiçbir pakette yok → küçük yassı çakıllar kullanıldı (Pebble_A/B).",
            "Taşıma animasyonu: Quaternius karakterlerinde yok → kodla taşıma pozu (CharacterVisual).",
            "Meyve: Quaternius'ta yok → Kenney Nature kavun (crop_melon).",
            "Tuğla: hiçbir pakette tuğla yok → Kenney Survival kesme taş (resource-stone).",
            "Dalga/deniz ortam sesi: Audio klasöründe yok → AudioManager yumuşak dalga sesini kendisi üretir.",
            "Quaternius renk dokusu (Atlas_Pirate.png): indirilen FBX klasöründe yoktu → Quaternius'un resmi paylaşımından eklendi (CC0)."
        };
    }
}
