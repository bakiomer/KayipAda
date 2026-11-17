// EscapePanel.cs
// Kaçış ekranı: "KAÇTIN!" başlığı zıplayarak gelir, süreye göre 3 yıldız tek tek dolar, kazanılan kaptanlık
// XP'si ve para sayarak artar, arkada konfeti yağar, "TEKRAR OYNA" butonu nabız gibi atar.
// Test modunda (Hızlı Test Modu ya da Test Menüsü kullanıldıysa) süre sayılmaz: "test modu" yazar, 3 yıldız verilir.
// Yıldız süreleri ve animasyon sayıları GameConfig'te. "Tekrar oyna" adayı sıfırlar, kaptanlık seviyesi kalır.

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda
{
    public class EscapePanel : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform title;
        [Tooltip("Dolu yıldızlar (soldan sağa); boş yıldızlar arkalarında durur.")]
        [SerializeField] private Image[] stars = new Image[0];
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text xpLabel;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private TMP_Text crystalLabel;
        [Tooltip("\"Koleksiyon 3/5    Sandıklar 6/8\"")]
        [SerializeField] private TMP_Text collectionLabel;
        [Tooltip("Altın figür takılıysa görünen \"EFSANEVİ GEMİ!\" rozeti.")]
        [SerializeField] private GameObject legendaryBadge;
        [SerializeField] private Button replayButton;

        public bool IsShown => root != null && root.activeSelf;
        public int StarCount { get; private set; }

        private float shownAt;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (legendaryBadge != null) legendaryBadge.SetActive(false);
            if (replayButton != null) replayButton.onClick.AddListener(Replay);
        }

        public void Show()
        {
            PlayerController.InputBlocked = true;
            SaveManager save = SaveManager.Instance;
            bool testMode = config.QuickTestMode || DevCommands.Used;
            float seconds = save != null ? save.PlayTime : Time.timeSinceLevelLoad;
            int xp = save != null ? save.RegisterEscape() : config.EscapeXp;
            int level = save != null ? save.CaptainLevel : 0;
            int coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.TotalEarned : 0;
            bool legendary = QuestManager.Instance != null && QuestManager.Instance.HasLegendaryFigure;
            int crystals = save != null ? save.RegisterEscapeCrystals(legendary) : config.EscapeCrystals;
            if (legendaryBadge != null) legendaryBadge.SetActive(legendary);
            if (crystalLabel != null) crystalLabel.text = "+" + crystals + " Kristal";
            if (collectionLabel != null && ProfileManager.Instance != null)
                collectionLabel.text = "Koleksiyon " + ProfileManager.Instance.CollectionCount + "/" + Collectible.CountOfKind(Collectible.Kind.Collection)
                                       + "    Sandıklar " + ProfileManager.Instance.ChestCount + "/" + TreasureChest.All.Count;

            StarCount = testMode ? 3 : seconds <= config.ThreeStarSeconds ? 3 : seconds <= config.TwoStarSeconds ? 2 : 1;
            timeLabel.text = testMode
                ? "Süre: test modu (sayılmadı)"
                : "Süre: " + Mathf.FloorToInt(seconds / 60f) + " dk " + Mathf.FloorToInt(seconds % 60f) + " sn";

            root.SetActive(true);
            shownAt = Time.unscaledTime;
            foreach (Image star in stars) star.transform.localScale = Vector3.zero;
            AudioManager.Play(SoundId.Victory);
            Haptics.Vibrate();
            if (Effects.Instance != null) Effects.Instance.ScreenConfetti(config.EscapeConfettiDuration);
            StartCoroutine(Animate(xp, level, coins));
        }

        private IEnumerator Animate(int xp, int level, int coins)
        {
            // Başlık zıplayarak gelir.
            title.localScale = Vector3.zero;
            yield return Grow(title, 0.5f);

            // Yıldızlar tek tek dolar.
            for (int i = 0; i < stars.Length; i++)
            {
                if (i >= StarCount) break;
                yield return new WaitForSecondsRealtime(config.StarDelay);
                AudioManager.Play(SoundId.Upgrade);
                Haptics.Vibrate();
                yield return Grow(stars[i].transform, 0.35f);
            }

            // XP ve para sayarak artar.
            float duration = config.CountUpDuration;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Tween.EaseOutQuad(Mathf.Clamp01(t / duration));
                xpLabel.text = "+" + Mathf.RoundToInt(xp * k) + " Kaptanlık XP   (Seviye " + level + ")";
                coinLabel.text = "Kazanılan para: " + Mathf.RoundToInt(coins * k);
                yield return null;
            }
            xpLabel.text = "+" + xp + " Kaptanlık XP   (Seviye " + level + ")";
            coinLabel.text = "Kazanılan para: " + coins;
        }

        private static IEnumerator Grow(Transform target, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * Tween.EaseOutBack(Mathf.Clamp01(t / duration));
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // Tekrar oyna butonu nabız gibi atar; başlık hafifçe sallanır.
        private void Update()
        {
            if (!IsShown) return;
            float t = Time.unscaledTime - shownAt;
            if (replayButton != null) replayButton.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * config.PulseSpeed) * config.PulseAmount);
            if (t > 0.6f) title.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 2.5f);
        }

        private void Replay()
        {
            AudioManager.Play(SoundId.Button);
            if (SaveManager.Instance != null) SaveManager.Instance.RestartRun();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
