// DevMenus.cs
// Tools > Kayıp Ada > Test: oyun içi Test Menüsü'ndeki komutların aynısı (sadece Play modunda çalışır).
// Play'e basın, sonra bu menüden komut seçin. Sayılar GameConfig'in "Test Menüsü" bölümündedir.

using UnityEditor;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class DevMenus
    {
        private const string Menu = "Tools/Kayıp Ada/Test/";
        private const string ConfigPath = BuilderUtil.Root + "/Data/GameConfig.asset";
        private const string ResourcesFolder = BuilderUtil.Root + "/Data/ResourceTypes/";

        private static GameConfig Config => BuilderUtil.Load<GameConfig>(ConfigPath);

        private static bool Playing() => Application.isPlaying;

        [MenuItem(Menu + "Sınırsız Para (aç-kapat)", false, 100)]
        private static void InfiniteMoney() => DevCommands.SetInfiniteMoney(Config, !DevCommands.InfiniteMoney);

        [MenuItem(Menu + "+1000 Para", false, 101)]
        private static void AddMoney() => DevCommands.AddMoney(Config);

        [MenuItem(Menu + "Tüm Bölgeleri Aç", false, 120)]
        private static void Regions() => DevCommands.UnlockAllRegions();

        [MenuItem(Menu + "Tüm Binaları Aç", false, 121)]
        private static void Buildings() => DevCommands.UnlockAllBuildings();

        [MenuItem(Menu + "Her İşçiden 1 Tane", false, 122)]
        private static void Workers() => DevCommands.HireAllWorkers();

        [MenuItem(Menu + "+100 Kristal", false, 160)] private static void Crystals() => DevCommands.AddCrystals(Config);
        [MenuItem(Menu + "Tüm Kostümleri Aç", false, 161)] private static void Costumes() => DevCommands.UnlockAllCostumes();
        [MenuItem(Menu + "Sıradaki Kostümü Giy", false, 162)] private static void NextCostume() => DevCommands.WearNextCostume();
        [MenuItem(Menu + "Tüm Sandıkları Göster", false, 163)] private static void Chests() => DevCommands.ToggleShowChests();
        [MenuItem(Menu + "Koleksiyonu Tamamla", false, 164)] private static void Collection() => DevCommands.CompleteCollection();
        [MenuItem(Menu + "Görevleri Tamamla", false, 165)] private static void Quests() => DevCommands.CompleteQuests();
        [MenuItem(Menu + "Köprüleri Tamamla", false, 166)] private static void Bridges() => DevCommands.CompleteBridges();
        [MenuItem(Menu + "Çalılıkları Kaldır-Getir", false, 167)] private static void Thickets() => DevCommands.ToggleThickets();

        [MenuItem(Menu + "Yığını Doldur/Kalas", false, 140)] private static void FillPlank() => Fill("Plank");
        [MenuItem(Menu + "Yığını Doldur/Tuğla", false, 141)] private static void FillBrick() => Fill("Brick");
        [MenuItem(Menu + "Yığını Doldur/Halat", false, 142)] private static void FillRope() => Fill("Rope");
        [MenuItem(Menu + "Yığını Doldur/Yelken Bezi", false, 143)] private static void FillCloth() => Fill("Cloth");
        [MenuItem(Menu + "Yığını Doldur/Meyve", false, 144)] private static void FillFruit() => Fill("Fruit");
        [MenuItem(Menu + "Yığını Doldur/Odun", false, 145)] private static void FillWood() => Fill("Wood");
        [MenuItem(Menu + "Yığını Doldur/Taş", false, 146)] private static void FillStone() => Fill("Stone");
        [MenuItem(Menu + "Yığını Doldur/Lif", false, 147)] private static void FillFiber() => Fill("Fiber");

        private static void Fill(string file) => DevCommands.FillPlayerStack(BuilderUtil.Load<ResourceType>(ResourcesFolder + file + ".asset"));

        [MenuItem(Menu + "Gemi Aşamasını Tamamla", false, 160)]
        private static void Stage() => DevCommands.CompleteShipStage();

        [MenuItem(Menu + "Final Törenine Atla", false, 161)]
        private static void Ceremony() => DevCommands.SkipToCeremony();

        [MenuItem(Menu + "Oyun Hızı x1", false, 180)] private static void Speed1() => DevCommands.SetSpeed(1f);
        [MenuItem(Menu + "Oyun Hızı x2", false, 181)] private static void Speed2() => DevCommands.SetSpeed(2f);
        [MenuItem(Menu + "Oyun Hızı x5", false, 182)] private static void Speed5() => DevCommands.SetSpeed(5f);

        [MenuItem(Menu + "Kaydı Sil ve Yeniden Başlat", false, 200)]
        private static void ResetSave() => DevCommands.ResetSaveAndRestart();

        // Hepsi sadece Play modunda tıklanabilir.
        [MenuItem(Menu + "Sınırsız Para (aç-kapat)", true)] private static bool V1() => Playing();
        [MenuItem(Menu + "+1000 Para", true)] private static bool V2() => Playing();
        [MenuItem(Menu + "Tüm Bölgeleri Aç", true)] private static bool V3() => Playing();
        [MenuItem(Menu + "Tüm Binaları Aç", true)] private static bool V4() => Playing();
        [MenuItem(Menu + "Her İşçiden 1 Tane", true)] private static bool V5() => Playing();
        [MenuItem(Menu + "Yığını Doldur/Kalas", true)] private static bool V6() => Playing();
        [MenuItem(Menu + "Yığını Doldur/Tuğla", true)] private static bool V7() => Playing();
        [MenuItem(Menu + "Yığını Doldur/Halat", true)] private static bool V8() => Playing();
        [MenuItem(Menu + "Yığını Doldur/Yelken Bezi", true)] private static bool V9() => Playing();
        [MenuItem(Menu + "Yığını Doldur/Meyve", true)] private static bool V10() => Playing();
        [MenuItem(Menu + "Yığını Doldur/Odun", true)] private static bool V11() => Playing();
        [MenuItem(Menu + "Yığını Doldur/Taş", true)] private static bool V12() => Playing();
        [MenuItem(Menu + "Yığını Doldur/Lif", true)] private static bool V13() => Playing();
        [MenuItem(Menu + "Gemi Aşamasını Tamamla", true)] private static bool V14() => Playing();
        [MenuItem(Menu + "Final Törenine Atla", true)] private static bool V15() => Playing();
        [MenuItem(Menu + "Oyun Hızı x1", true)] private static bool V16() => Playing();
        [MenuItem(Menu + "Oyun Hızı x2", true)] private static bool V17() => Playing();
        [MenuItem(Menu + "Oyun Hızı x5", true)] private static bool V18() => Playing();
        [MenuItem(Menu + "Kaydı Sil ve Yeniden Başlat", true)] private static bool V19() => Playing();
    }
}
