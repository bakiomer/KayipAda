// SaveManager.cs
// Kayıt sistemi: oyun açılınca cihazdaki kaydı yükler, oyun sırasında (GameConfig.AutoSaveInterval saniyede bir),
// uygulama arka plana alınınca ve kapanırken otomatik kaydeder. Dosya: persistentDataPath/kayipada_save.json.
// Yöneticilerden (-100) sonra, diğer her şeyden önce çalışır ki alanlar/bölgeler Awake'te doğru durumu görsün.
// Kaptanlık XP'si "Tekrar oyna"dan sonra da kalır; her kaptanlık seviyesi küçük bir hız/kapasite bonusu verir.

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KayipAda
{
    [DefaultExecutionOrder(-95)]
    public class SaveManager : MonoBehaviour
    {
        public const string FileName = "kayipada_save.json";

        public static SaveManager Instance { get; private set; }

        // Testler kayıt okumadan/yazmadan temiz başlasın diye kapatabilir.
        public static bool Enabled = true;

        [SerializeField] private GameConfig config;

        private SaveData data = new SaveData();
        private bool loaded;
        private float timer;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public float PlayTime => data.PlayTime;
        public int CaptainXp => data.CaptainXp;
        public int CaptainLevel => data.CaptainXp / Mathf.Max(1, config.XpPerCaptainLevel);

        private void Awake()
        {
            Instance = this;
            if (!Enabled) return;
            SaveData saved = Load();
            if (saved == null) return; // ilk açılış: yeni oyun
            data = saved;
            loaded = true;
            RestoreEarly();
        }

        private void Start()
        {
            if (Enabled && loaded) RestoreLate();
        }

        private void Update()
        {
            data.PlayTime += Time.unscaledDeltaTime;
            timer += Time.unscaledDeltaTime;
            if (timer >= config.AutoSaveInterval)
            {
                timer = 0f;
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Kayıp Ada: Kayıt okunamadı, yeni oyun başlıyor. " + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        // Para, alanlar, geliştirmeler: diğer nesnelerin Awake'inden ÖNCE.
        private void RestoreEarly()
        {
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.SetCoins(data.Coins);
            if (AreaManager.Instance != null) AreaManager.Instance.Restore(data.Unlocked, SaveData.ToDictionary(data.Paid));

            UpgradeManager upgrades = UpgradeManager.Instance;
            if (upgrades != null)
            {
                upgrades.CaptainLevel = CaptainLevel;
                Dictionary<string, int> levels = SaveData.ToDictionary(data.Upgrades);
                foreach (UpgradeType type in upgrades.Upgrades)
                {
                    if (type != null && levels.TryGetValue(type.Id, out int level)) upgrades.SetLevel(type, level);
                }
            }
        }

        // Gemi: her şey uyandıktan sonra.
        private void RestoreLate()
        {
            if (ShipBuilder.Instance != null) ShipBuilder.Instance.Restore(data.ShipStage, SaveData.ToDictionary(data.ShipDelivered));
        }

        public void Save()
        {
            if (!Enabled) return;
            Collect();
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Kayıp Ada: Kayıt yazılamadı: " + e.Message);
            }
        }

        private void Collect()
        {
            if (CurrencyManager.Instance != null) data.Coins = CurrencyManager.Instance.Coins;
            if (AreaManager.Instance != null)
            {
                data.Unlocked = new List<string>(AreaManager.Instance.UnlockedIds);
                data.Paid.Clear();
                foreach (KeyValuePair<string, int> p in AreaManager.Instance.PaidAmounts) data.Paid.Add(new SaveData.IdValue(p.Key, p.Value));
            }
            if (UpgradeManager.Instance != null)
            {
                data.Upgrades.Clear();
                foreach (UpgradeType type in UpgradeManager.Instance.Upgrades)
                {
                    if (type != null) data.Upgrades.Add(new SaveData.IdValue(type.Id, UpgradeManager.Instance.GetLevel(type)));
                }
            }
            if (ShipBuilder.Instance != null)
            {
                data.ShipStage = ShipBuilder.Instance.StageIndex;
                data.ShipDelivered.Clear();
                foreach (KeyValuePair<string, int> p in ShipBuilder.Instance.DeliveredCounts) data.ShipDelivered.Add(new SaveData.IdValue(p.Key, p.Value));
            }
        }

        // Kaçış: kaptanlık XP'si kazanılır. Döndürülen değer: kazanılan XP.
        public int RegisterEscape()
        {
            data.CaptainXp += config.EscapeXp;
            data.Escapes++;
            Save();
            return config.EscapeXp;
        }

        // Test Menüsü: kaydı tamamen siler (kaptanlık dahil) ve oyunu baştan başlatır.
        public void DeleteAndRestart()
        {
            Delete();
            enabled = false; // sahne yüklenene kadar otomatik kayıt yazmasın
            data = new SaveData();
            Time.timeScale = 1f;
            PlayerController.InputBlocked = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // "Tekrar oyna": adadaki ilerleme sıfırlanır, kaptanlık kalır; sahne baştan yüklenir.
        public void RestartRun()
        {
            data.ResetRun();
            data.Coins = config.StartingCoins;
            if (Enabled)
            {
                try { File.WriteAllText(FilePath, JsonUtility.ToJson(data)); }
                catch (System.Exception e) { Debug.LogWarning("Kayıp Ada: Kayıt yazılamadı: " + e.Message); }
            }
            PlayerController.InputBlocked = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
