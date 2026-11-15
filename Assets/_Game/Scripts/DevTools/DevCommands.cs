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

        public static bool InfiniteMoney => CurrencyManager.Instance != null && CurrencyManager.Instance.IsInfinite;

        public static void SetInfiniteMoney(GameConfig config, bool on)
        {
            if (!Available || CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.SetInfinite(on, config.DevInfiniteCoins);
        }

        public static void AddMoney(GameConfig config)
        {
            if (!Available || CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.Add(config.DevMoneyAmount);
        }

        // Bölgeler: Kayalık, Meyve Bahçesi, Palmiye Koyu (bina ve işçi kareleri hariç).
        public static void UnlockAllRegions()
        {
            if (!Available) return;
            foreach (UnlockableArea area in Object.FindObjectsByType<UnlockableArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!area.HoldsBuilding) Unlock(area.Area);
            }
        }

        // Binalar (Bıçkıhane, Taş Atölyesi, Halat Atölyesi, Dokuma Tezgahı). Bina kapalı bir bölgenin içindeyse
        // görünsün diye o bölge de açılır.
        public static void UnlockAllBuildings()
        {
            if (!Available) return;
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
        }

        // Her işçi türünün karesini açar (işçi sayısı WorkerType.Count; başlangıçta her türden 1).
        public static void HireAllWorkers()
        {
            if (!Available) return;
            foreach (WorkerSpawner spawner in Object.FindObjectsByType<WorkerSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Unlock(spawner.HireArea);
            }
        }

        private static void Unlock(AreaDefinition area)
        {
            if (area != null && AreaManager.Instance != null && !AreaManager.Instance.IsUnlocked(area)) AreaManager.Instance.Unlock(area);
        }

        // Oyuncunun sırtındaki yığını seçilen malzemeyle doldurur (önce boşaltır).
        public static void FillPlayerStack(ResourceType type)
        {
            if (!Available || type == null) return;
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
            if (!Available || ShipBuilder.Instance == null) return;
            ShipBuilder.Instance.ForceCompleteStage();
        }

        // Kalan tüm aşamaları bitirir; gemi tamamlanınca suya indirme töreni kendiliğinden başlar.
        public static void SkipToCeremony()
        {
            if (!Available || ShipBuilder.Instance == null) return;
            while (!ShipBuilder.Instance.IsComplete) ShipBuilder.Instance.ForceCompleteStage();
        }

        public static void SetSpeed(float speed)
        {
            if (!Available) return;
            Time.timeScale = speed;
        }

        public static void ResetSaveAndRestart()
        {
            if (!Available) return;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.SetInfinite(false, 0);
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
