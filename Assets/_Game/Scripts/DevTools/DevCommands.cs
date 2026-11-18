// DevCommands.cs
// Test Menüsü komutları (oyun içi Test Menüsü ve Tools > Kayıp Ada > Test menüsü aynı komutları çağırır).
// SADECE Unity Editor'da ve "Development Build" sürümlerinde çalışır; satış sürümünde Available = false olur
// ve hiçbir komut iş yapmaz (oyun içi menü de kendini siler).
// Sayılar (verilen para, hızlar) GameConfig'in "Test Menüsü" bölümündedir.

using UnityEngine;

namespace KayipAda
{
    public static class DevCommands
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool Available => Application.isEditor || Debug.isDebugBuild;
#else
        public static bool Available => false;
#endif

        // Bu oturumda bir test komutu kullanıldı mı? (Kullanıldıysa oyun süresi sayılmaz.)
        public static bool Used { get; private set; }

        private static bool Use()
        {
            if (!Available) return false;
            Used = true;
            return true;
        }

        public static bool InfiniteMoney => CurrencyManager.Instance != null && CurrencyManager.Instance.IsInfinite;

        public static void SetInfiniteMoney(GameConfig config, bool on)
        {
            if (CurrencyManager.Instance == null || !Use()) return;
            CurrencyManager.Instance.SetInfinite(on, config.DevInfiniteCoins);
        }

        public static void AddMoney(GameConfig config)
        {
            if (CurrencyManager.Instance == null || !Use()) return;
            CurrencyManager currency = CurrencyManager.Instance;
            currency.SetCoins(currency.Coins + config.DevMoneyAmount); // kazanılan para sayılmaz
        }

        // Bölgeler: Kayalık, Meyve Bahçesi, Palmiye Koyu (bina ve işçi kareleri hariç).
        public static void UnlockAllRegions()
        {
            if (!Use()) return;
            foreach (UnlockableArea area in Object.FindObjectsByType<UnlockableArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!area.HoldsBuilding) Unlock(area.Area);
            }
        }

        // Binalar (Bıçkıhane, Taş Atölyesi, Halat Atölyesi, Dokuma Tezgahı). Bina kapalı bir bölgenin içindeyse
        // görünsün diye o bölge de açılır.
        public static void UnlockAllBuildings()
        {
            if (!Use()) return;
            foreach (UnlockableArea area in Object.FindObjectsByType<UnlockableArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!area.HoldsBuilding) continue;
                for (Transform t = area.transform.parent; t != null; t = t.parent)
                {
                    UnlockableArea outer = t.GetComponent<UnlockableArea>();
                    if (outer != null) Unlock(outer.Area);
                }
                Unlock(area.Area);
            }
            if (Storage.Instance != null) Unlock(Storage.Instance.Area); // Ambar da bir bina
        }

        // Ambarı aç ve her kaynaktan biraz koy (kapasiteye kadar).
        public static void FillStorage()
        {
            if (!Use() || Storage.Instance == null) return;
            Unlock(Storage.Instance.Area);
            foreach (ResourceType t in Storage.Instance.Types) Storage.Instance.Add(t, 10);
        }

        // Her işçi türünden en az 1 tane tutar (gereken bina kapalı olsa da; para harcamaz).
        public static void HireAllWorkers()
        {
            if (!Use() || CrewManager.Instance == null) return;
            foreach (WorkerType type in CrewManager.Instance.Types)
            {
                if (type != null && CrewManager.Instance.GetHired(type) < 1) CrewManager.Instance.SetHired(type, 1);
            }
        }

        // ---------------------------------------------------------------- Keşif komutları

        public static void AddCrystals(GameConfig config)
        {
            if (!Use() || ProfileManager.Instance == null) return;
            ProfileManager.Instance.AddCrystals(config.DevCrystalAmount);
        }

        public static void UnlockAllCostumes()
        {
            if (!Use() || ProfileManager.Instance == null) return;
            ProfileManager.Instance.UnlockAll();
        }

        // Sahip olunan karakter kostümlerinin sıradakini giyer (yelken de bir sonrakine geçer).
        public static string WearNextCostume()
        {
            if (!Use() || ProfileManager.Instance == null) return "";
            CostumeDefinition c = ProfileManager.Instance.EquipNext(CostumeSlot.Character);
            CostumeDefinition s = ProfileManager.Instance.EquipNext(CostumeSlot.Sail);
            return (c != null ? c.DisplayName : "-") + " / " + (s != null ? s.DisplayName : "-");
        }

        public static bool ChestsShown { get; private set; }

        public static void ToggleShowChests()
        {
            if (!Use()) return;
            ChestsShown = !ChestsShown;
            TreasureChest.ShowAll(ChestsShown);
        }

        public static void CompleteCollection()
        {
            if (!Use()) return;
            foreach (Collectible c in new System.Collections.Generic.List<Collectible>(Collectible.All))
                if (c != null && c.Type == Collectible.Kind.Collection && !c.IsTaken) c.Take();
            if (CollectionPopup.Instance != null) CollectionPopup.Instance.Hide();
        }

        public static void CompleteQuests()
        {
            if (!Use() || QuestManager.Instance == null) return;
            QuestManager.Instance.CompleteAllActive();
        }

        // Köprülerin gerektirdiği binayı da açar, köprüleri tamamlar.
        public static void CompleteBridges()
        {
            if (!Use()) return;
            foreach (ConstructionSite site in ConstructionSite.All)
            {
                if (!(site is BridgeBuilder)) continue;
                site.ForceComplete();
            }
        }

        // Kampı bir seviye büyüt (çadır → kulübe → ev).
        public static void GrowCamp()
        {
            if (!Use() || CampSite.Instance == null) return;
            CampSite.Instance.ForceGrow();
        }

        private static void Unlock(AreaDefinition area)
        {
            if (area != null && AreaManager.Instance != null && !AreaManager.Instance.IsUnlocked(area)) AreaManager.Instance.Unlock(area);
        }

        // Oyuncunun sırtındaki yığını seçilen malzemeyle doldurur (önce boşaltır).
        public static void FillPlayerStack(ResourceType type)
        {
            if (type == null || !Use()) return;
            CarryStack stack = PlayerStack();
            if (stack == null) return;
            while (!stack.IsEmpty)
            {
                ResourceItem old = stack.RemoveTop();
                if (old != null) Object.Destroy(old.gameObject);
            }
            while (!stack.IsFull)
            {
                if (!stack.TryAdd(type.Spawn(stack.transform.position + Vector3.up))) break;
            }
        }

        public static CarryStack PlayerStack()
        {
            foreach (CarryStack stack in Object.FindObjectsByType<CarryStack>(FindObjectsSortMode.None))
            {
                if (stack.IsPlayer) return stack;
            }
            return null;
        }

        public static void CompleteShipStage()
        {
            if (ShipBuilder.Instance == null || !Use()) return;
            ShipBuilder.Instance.ForceCompleteStage();
        }

        // Kalan tüm aşamaları bitirir; gemi tamamlanınca suya indirme töreni kendiliğinden başlar.
        public static void SkipToCeremony()
        {
            if (ShipBuilder.Instance == null || !Use()) return;
            while (!ShipBuilder.Instance.IsComplete) ShipBuilder.Instance.ForceCompleteStage();
        }

        public static void SetSpeed(float speed)
        {
            if (!Use()) return;
            Time.timeScale = speed;
        }

        public static void ResetSaveAndRestart()
        {
            if (!Use()) return;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.SetInfinite(false, 0);
            Used = false; // yeni oyun: süre yeniden sayılır (başka test komutu kullanılmazsa)
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteAndRestart();
            }
            else
            {
                SaveManager.Delete();
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
