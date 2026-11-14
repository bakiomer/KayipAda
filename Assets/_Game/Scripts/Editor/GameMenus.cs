// GameMenus.cs
// Test kolaylığı için menü komutları:
// - Tools > Kayıp Ada > Hızlı Test Modu: fiyatlar ve gemi malzemeleri çok düşer, oyun 3-5 dakikada biter.
//   Asıl değerler değişmez; tekrar tıklayınca kapanır ve normal değerlere dönülür (menüde tik işareti görünür).
// - Tools > Kayıp Ada > Kaydı Sil: cihazdaki oyun kaydını siler (yeni oyun başlar).

using UnityEditor;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class GameMenus
    {
        private const string QuickTestMenu = "Tools/Kayıp Ada/Hızlı Test Modu";
        private const string ConfigPath = BuilderUtil.Root + "/Data/GameConfig.asset";

        [MenuItem(QuickTestMenu, false, 20)]
        public static void ToggleQuickTest()
        {
            GameConfig config = BuilderUtil.Load<GameConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogWarning("Kayıp Ada: GameConfig bulunamadı. Önce sahne kurma komutunu çalıştırın.");
                return;
            }
            config.EditorSetQuickTest(!config.QuickTestMode);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("Kayıp Ada: Hızlı Test Modu " + (config.QuickTestMode ? "AÇIK (fiyatlar ve malzemeler çok düşük)" : "KAPALI (normal değerler)"));
        }

        [MenuItem(QuickTestMenu, true)]
        public static bool ToggleQuickTestValidate()
        {
            GameConfig config = BuilderUtil.Load<GameConfig>(ConfigPath);
            Menu.SetChecked(QuickTestMenu, config != null && config.QuickTestMode);
            return config != null;
        }

        [MenuItem("Tools/Kayıp Ada/Kaydı Sil", false, 21)]
        public static void DeleteSave()
        {
            SaveManager.Delete();
            Debug.Log("Kayıp Ada: Oyun kaydı silindi → " + SaveManager.FilePath);
        }
    }
}
